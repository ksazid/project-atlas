import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import { spawn, spawnSync } from 'node:child_process';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../..');
const mobileRoot = path.join(root, 'apps/mobile');
const sessionWebPath = path.join(mobileRoot, 'src/auth/session.web.ts');
const artifactDir = path.join(root, 'dashboard/runtime-vs16');
const runRuntime = process.env.CI === 'true' && process.env.GITHUB_ACTIONS === 'true';

const sessionWebShim = `const ACCESS_TOKEN_KEY = 'atlas.access-token';
const BUSINESS_ID_KEY = 'atlas.business-id';
export type Session = { accessToken: string; businessId?: string };
export async function loadSession(): Promise<Session | null> {
  const accessToken = window.localStorage.getItem(ACCESS_TOKEN_KEY);
  if (!accessToken) return null;
  const businessId = window.localStorage.getItem(BUSINESS_ID_KEY);
  return { accessToken, businessId: businessId ?? undefined };
}
export async function saveSession(session: Session): Promise<void> {
  window.localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
  if (session.businessId) window.localStorage.setItem(BUSINESS_ID_KEY, session.businessId);
}
export async function clearSession(): Promise<void> {
  window.localStorage.removeItem(ACCESS_TOKEN_KEY);
  window.localStorage.removeItem(BUSINESS_ID_KEY);
}
`;

const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
const sha256 = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

function commandPath(names) {
  for (const name of names) {
    const result = spawnSync('bash', ['-lc', `command -v ${name}`], { encoding: 'utf8' });
    if (result.status === 0 && result.stdout.trim()) return result.stdout.trim();
  }
  return null;
}

async function listen(server) {
  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', resolve);
  });
  return server.address().port;
}

async function closeServer(server) {
  if (!server?.listening) return;
  await new Promise(resolve => server.close(resolve));
}

function createApiFixture() {
  const state = { requests: [], discoveryCount: 0, createCount: 0, createBody: null };
  const facts = [
    ['name', 'Harbour Coffee', 'high'],
    ['category', 'restaurant-cafe', 'high'],
    ['subcategory', 'cafe', 'high'],
    ['primaryLocation', '1 Republic Street, Valletta, MT', 'high'],
    ['country', 'MT', 'high'],
    ['description', 'Independent coffee shop and bakery', 'medium'],
  ].map(([key, value, confidence]) => ({
    key,
    value,
    source: 'website',
    sourceUrl: 'https://harbour.example',
    observedAt: '2026-08-09T20:00:00Z',
    confidence,
    evidenceClass: 'public-observed',
    ownerConfirmed: false,
  }));

  const server = http.createServer(async (req, res) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'authorization,content-type');
    res.setHeader('Access-Control-Allow-Methods', 'POST,OPTIONS');
    res.setHeader('Content-Type', 'application/json');
    if (req.method === 'OPTIONS') {
      res.statusCode = 204;
      res.end();
      return;
    }

    const url = new URL(req.url ?? '/', 'http://runtime.invalid');
    let body = '';
    for await (const chunk of req) body += chunk;
    const parsedBody = body ? JSON.parse(body) : null;
    state.requests.push({ method: req.method, path: url.pathname, authorization: req.headers.authorization ?? null, body: parsedBody });

    if (req.method === 'POST' && url.pathname === '/api/v1/business-discovery') {
      state.discoveryCount += 1;
      assert.equal(parsedBody?.url, 'https://harbour.example');
      await delay(250);
      res.end(JSON.stringify({
        snapshotId: 'runtime-snapshot',
        provider: 'website',
        sourceUrl: 'https://harbour.example',
        observedAt: '2026-08-09T20:00:00Z',
        facts,
      }));
      return;
    }

    if (req.method === 'POST' && url.pathname === '/api/v1/businesses/from-discovery') {
      state.createCount += 1;
      state.createBody = parsedBody;
      await delay(250);
      res.statusCode = 201;
      res.end(JSON.stringify({ id: 'runtime-business' }));
      return;
    }

    res.statusCode = 404;
    res.end(JSON.stringify({ message: 'Not found.' }));
  });
  return { server, state };
}

function contentType(file) {
  const ext = path.extname(file).toLowerCase();
  return ({
    '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8',
    '.json': 'application/json; charset=utf-8', '.css': 'text/css; charset=utf-8', '.svg': 'image/svg+xml', '.png': 'image/png',
    '.ico': 'image/x-icon', '.woff2': 'font/woff2'
  })[ext] ?? 'application/octet-stream';
}

function createStaticServer(exportRoot) {
  return http.createServer((req, res) => {
    const url = new URL(req.url ?? '/', 'http://runtime.invalid');
    const pathname = decodeURIComponent(url.pathname).replace(/^\/+/, '');
    const candidates = pathname
      ? [path.join(exportRoot, pathname), path.join(exportRoot, `${pathname}.html`), path.join(exportRoot, pathname, 'index.html')]
      : [path.join(exportRoot, 'index.html')];
    const file = candidates.find(candidate => fs.existsSync(candidate) && fs.statSync(candidate).isFile());
    if (!file || !path.resolve(file).startsWith(path.resolve(exportRoot))) {
      res.statusCode = 404;
      res.end('Not found');
      return;
    }
    res.statusCode = 200;
    res.setHeader('Content-Type', contentType(file));
    res.setHeader('Cache-Control', 'no-store');
    fs.createReadStream(file).pipe(res);
  });
}

async function runExpoExport(outputDir, apiUrl) {
  const child = spawn('npx', ['expo', 'export', '--platform', 'web', '--output-dir', outputDir], {
    cwd: mobileRoot,
    env: {
      ...process.env,
      CI: 'true',
      EXPO_NO_TELEMETRY: '1',
      EXPO_PUBLIC_API_URL: apiUrl,
      EXPO_PUBLIC_AUTH_ISSUER: 'https://auth.runtime.invalid',
      EXPO_PUBLIC_AUTH_CLIENT_ID: 'runtime-client'
    },
    stdio: ['ignore', 'pipe', 'pipe']
  });
  let stdout = '';
  let stderr = '';
  child.stdout.on('data', chunk => { stdout += chunk; });
  child.stderr.on('data', chunk => { stderr += chunk; });
  const code = await new Promise((resolve, reject) => {
    child.once('error', reject);
    child.once('close', resolve);
  });
  assert.equal(code, 0, `Expo Web export failed.\nSTDOUT:\n${stdout}\nSTDERR:\n${stderr}`);
  assert.ok(fs.existsSync(path.join(outputDir, 'index.html')), 'Expo Web export did not create index.html');
}

async function waitForJson(url, timeoutMs = 20000) {
  const deadline = Date.now() + timeoutMs;
  let lastError;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url);
      if (response.ok) return response.json();
    } catch (error) {
      lastError = error;
    }
    await delay(100);
  }
  throw lastError ?? new Error(`Timed out waiting for ${url}`);
}

class CdpClient {
  constructor(url) {
    this.url = url;
    this.nextId = 1;
    this.pending = new Map();
  }
  async connect() {
    this.socket = new WebSocket(this.url);
    await new Promise((resolve, reject) => {
      this.socket.addEventListener('open', resolve, { once: true });
      this.socket.addEventListener('error', reject, { once: true });
    });
    this.socket.addEventListener('message', event => {
      const message = JSON.parse(event.data);
      if (!message.id) return;
      const pending = this.pending.get(message.id);
      if (!pending) return;
      this.pending.delete(message.id);
      if (message.error) pending.reject(new Error(`${pending.method}: ${message.error.message}`));
      else pending.resolve(message.result ?? {});
    });
  }
  async send(method, params = {}) {
    const id = this.nextId++;
    const result = new Promise((resolve, reject) => this.pending.set(id, { resolve, reject, method }));
    this.socket.send(JSON.stringify({ id, method, params }));
    return result;
  }
  async evaluate(expression) {
    const result = await this.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true, userGesture: true });
    if (result.exceptionDetails) throw new Error(result.exceptionDetails.text ?? 'Runtime evaluation failed.');
    return result.result?.value;
  }
  async waitFor(expression, label, timeoutMs = 10000) {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      if (await this.evaluate(`Boolean(${expression})`)) return;
      await delay(80);
    }
    const body = await this.evaluate('document.body?.innerText ?? ""');
    throw new Error(`Timed out waiting for ${label}. Body:\n${body}`);
  }
  async navigate(url) {
    await this.send('Page.navigate', { url });
    await this.waitFor('document.readyState === "complete"', `page load ${url}`, 15000);
  }
  async setViewport(width, height, deviceScaleFactor = 1) {
    await this.send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor, mobile: true, screenWidth: width, screenHeight: height });
  }
  async screenshot(file) {
    const result = await this.send('Page.captureScreenshot', { format: 'png', fromSurface: true, captureBeyondViewport: false });
    fs.writeFileSync(file, Buffer.from(result.data, 'base64'));
  }
  close() { this.socket?.close(); }
}

async function launchChrome(binary, appOrigin, userDataDir) {
  const debugPort = 20000 + (process.pid % 1000);
  const child = spawn(binary, [
    '--headless=new', '--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage', '--disable-background-networking',
    '--disable-default-apps', '--disable-extensions', '--no-first-run', '--no-default-browser-check',
    `--remote-debugging-port=${debugPort}`, `--user-data-dir=${userDataDir}`, `${appOrigin}/`
  ], { stdio: ['ignore', 'ignore', 'pipe'] });
  let stderr = '';
  child.stderr.on('data', chunk => { stderr += chunk; });
  const targets = await waitForJson(`http://127.0.0.1:${debugPort}/json/list`).catch(error => {
    child.kill('SIGKILL');
    throw new Error(`Chrome did not expose DevTools. ${error.message}\n${stderr}`);
  });
  const page = targets.find(target => target.type === 'page');
  assert.ok(page?.webSocketDebuggerUrl, `Chrome page target missing. ${stderr}`);
  return { child, page };
}

async function clickByLabel(cdp, label) {
  const clicked = await cdp.evaluate(`(() => { const element = document.querySelector('[aria-label=${JSON.stringify(label)}]'); if (!element) return false; element.click(); return true; })()`);
  assert.equal(clicked, true, `Could not find interactive element labelled ${label}`);
}

async function setInputByLabel(cdp, label, value) {
  const updated = await cdp.evaluate(`(() => {
    const element = document.querySelector('[aria-label=${JSON.stringify(label)}]');
    if (!element) return false;
    const descriptor = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(element), 'value');
    if (!descriptor?.set) return false;
    descriptor.set.call(element, ${JSON.stringify(value)});
    element.dispatchEvent(new Event('input', { bubbles: true }));
    element.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
  })()`);
  assert.equal(updated, true, `Could not update input labelled ${label}`);
}

function prHeadSha() {
  try {
    if (!process.env.GITHUB_EVENT_PATH) return process.env.GITHUB_SHA ?? null;
    const payload = JSON.parse(fs.readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
    return payload.pull_request?.head?.sha ?? process.env.GITHUB_SHA ?? null;
  } catch {
    return process.env.GITHUB_SHA ?? null;
  }
}

test('VS-16 URL-first discovery completes in authentic Expo Web runtime', { skip: !runRuntime, timeout: 180000 }, async t => {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'atlas-vs16-runtime-'));
  const exportRoot = path.join(tempRoot, 'web');
  const chromeProfile = path.join(tempRoot, 'chrome');
  fs.mkdirSync(artifactDir, { recursive: true });

  // VS-15 uses the same temporary web-session seam. Give it the first setup turn
  // when the full suite runs in parallel, then safely reuse the identical shim.
  await delay(500);
  const ownsSessionShim = !fs.existsSync(sessionWebPath);
  if (ownsSessionShim) fs.writeFileSync(sessionWebPath, sessionWebShim);
  else assert.equal(fs.readFileSync(sessionWebPath, 'utf8'), sessionWebShim, 'Existing session.web.ts is not the expected temporary runtime shim.');

  const fixture = createApiFixture();
  const apiPort = await listen(fixture.server);
  const apiUrl = `http://127.0.0.1:${apiPort}`;
  let staticServer;
  let chrome;
  let cdp;

  t.after(async () => {
    cdp?.close();
    if (chrome?.child && !chrome.child.killed) chrome.child.kill('SIGKILL');
    await closeServer(staticServer);
    await closeServer(fixture.server);
    if (ownsSessionShim && fs.existsSync(sessionWebPath)) fs.unlinkSync(sessionWebPath);
    fs.rmSync(tempRoot, { recursive: true, force: true });
  });

  await runExpoExport(exportRoot, apiUrl);
  staticServer = createStaticServer(exportRoot);
  const appPort = await listen(staticServer);
  const appOrigin = `http://127.0.0.1:${appPort}`;
  const chromeBinary = commandPath(['google-chrome', 'google-chrome-stable', 'chromium', 'chromium-browser']);
  assert.ok(chromeBinary, 'GitHub runner does not provide a headless Chrome/Chromium binary.');
  chrome = await launchChrome(chromeBinary, appOrigin, chromeProfile);
  cdp = new CdpClient(chrome.page.webSocketDebuggerUrl);
  await cdp.connect();
  await cdp.send('Page.enable');
  await cdp.send('Runtime.enable');
  await cdp.send('Network.enable');
  await cdp.send('Network.setBlockedURLs', { urls: ['*upload.wikimedia.org*'] });
  await cdp.setViewport(390, 844, 2);
  await cdp.navigate(`${appOrigin}/`);
  await cdp.evaluate(`localStorage.setItem('atlas.access-token','runtime-token'); localStorage.removeItem('atlas.business-id'); true`);
  await cdp.navigate(`${appOrigin}/create-business`);
  await cdp.waitFor('document.body.innerText.includes("Discovering your")', 'discovery entry screen', 10000);
  assert.equal(await cdp.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, '390px discovery layout has horizontal overflow.');

  await setInputByLabel(cdp, 'Business page URL', 'https://harbour.example');
  await cdp.waitFor(`document.querySelector('[aria-label="Business page URL"]')?.value === 'https://harbour.example'`, 'business URL input', 3000);
  await clickByLabel(cdp, 'Discover my business');
  await cdp.waitFor('document.body.innerText.includes("We found your business!")', 'discovery confirmation', 10000);
  assert.equal(fixture.state.discoveryCount, 1, 'Discovery API should be called once.');
  const confirmText = await cdp.evaluate('document.body.innerText');
  assert.match(confirmText, /Harbour Coffee/);
  assert.match(confirmText, /Restaurant Cafe|Restaurant Café/);
  assert.match(confirmText, /A few details are still needed/);
  assert.match(confirmText, /Timezone/);
  assert.match(confirmText, /Currency/);
  assert.doesNotMatch(confirmText, /12,847|4\.6|6:00 AM|10:00 PM/);
  const confirmScreenshot = path.join(artifactDir, 'discovery-390x844-confirm.png');
  await cdp.screenshot(confirmScreenshot);

  const confirmActionHeight = await cdp.evaluate(`document.querySelector('[aria-label="Complete missing details"]')?.getBoundingClientRect().height ?? 0`);
  assert.ok(confirmActionHeight >= 44, `Complete-details action must be at least 44px high, got ${confirmActionHeight}`);
  await clickByLabel(cdp, 'Complete missing details');
  await cdp.waitFor('document.body.innerText.includes("Fill only what Atlas still needs.")', 'missing details screen', 5000);
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Business name"]')?.value`), 'Harbour Coffee');
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Country"]')?.value`), 'MT');
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Timezone"]')?.value`), '', 'Timezone must not be silently inferred.');
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Currency"]')?.value`), '', 'Currency must not be silently inferred.');
  await setInputByLabel(cdp, 'Timezone', 'Europe/Malta');
  await setInputByLabel(cdp, 'Currency', 'EUR');
  await cdp.waitFor(`document.querySelector('[aria-label="Timezone"]')?.value === 'Europe/Malta' && document.querySelector('[aria-label="Currency"]')?.value === 'EUR'`, 'owner-provided missing fields', 3000);
  const missingScreenshot = path.join(artifactDir, 'discovery-390x844-missing-details.png');
  await cdp.screenshot(missingScreenshot);

  await clickByLabel(cdp, 'Review details');
  await cdp.waitFor('document.body.innerText.includes("We found your business!") && document.body.innerText.includes("Confirm and continue")', 'completed confirmation', 5000);
  await cdp.setViewport(768, 1024, 1);
  assert.equal(await cdp.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, '768px confirmation layout has horizontal overflow.');
  const tabletScreenshot = path.join(artifactDir, 'discovery-768x1024-confirm.png');
  await cdp.screenshot(tabletScreenshot);
  await cdp.setViewport(390, 844, 2);

  await clickByLabel(cdp, 'Confirm and continue');
  await cdp.waitFor(`localStorage.getItem('atlas.business-id') === 'runtime-business'`, 'created business session', 10000);
  assert.equal(fixture.state.createCount, 1, 'Business creation should consume discovery once.');
  assert.equal(fixture.state.createBody?.snapshotId, 'runtime-snapshot');
  assert.equal(fixture.state.createBody?.name, 'Harbour Coffee');
  assert.equal(fixture.state.createBody?.country, 'MT');
  assert.equal(fixture.state.createBody?.timezone, 'Europe/Malta');
  assert.equal(fixture.state.createBody?.currency, 'EUR');
  assert.equal(fixture.state.createBody?.ownerConfirmed, true);
  assert.equal(fixture.state.createBody?.phone, '', 'Missing phone must remain unknown.');
  assert.equal(fixture.state.createBody?.businessHours, '', 'Missing hours must remain unknown.');

  const authenticatedRequests = fixture.state.requests.filter(request => request.path.startsWith('/api/v1/business'));
  assert.equal(authenticatedRequests.length >= 2, true, 'Runtime did not exercise discovery and create API boundaries.');
  assert.equal(authenticatedRequests.every(request => request.authorization === 'Bearer runtime-token'), true, 'Runtime request escaped seeded authentication boundary.');

  const screenshots = [confirmScreenshot, missingScreenshot, tabletScreenshot];
  const summary = {
    headSha: prHeadSha(),
    workflowSha: process.env.GITHUB_SHA ?? null,
    route: '/create-business',
    browser: chromeBinary,
    viewports: ['390x844@2x', '768x1024@1x'],
    assertions: {
      urlFirstDiscovery: true,
      realObservedFactsOnly: true,
      missingTimezoneCurrencyRemainUnknown: true,
      ownerCompletesOnlyMissingFields: true,
      exactSnapshotConsumed: true,
      ownerConfirmationPersisted: true,
      authenticatedApiBoundary: true,
      noHorizontalOverflow: true,
      minimumPrimaryTargetPx: 44,
    },
    discoveryCount: fixture.state.discoveryCount,
    createCount: fixture.state.createCount,
    screenshots: screenshots.map(file => ({ file: path.basename(file), sha256: sha256(file) }))
  };
  fs.writeFileSync(path.join(artifactDir, 'runtime-summary.json'), `${JSON.stringify(summary, null, 2)}\n`);
});
