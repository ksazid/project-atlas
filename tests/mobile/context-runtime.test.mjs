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
const artifactDir = path.join(root, 'dashboard/runtime-vs15');
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
  const state = {
    failGet: true,
    failPut: false,
    delayGetMs: 650,
    delayPutMs: 0,
    putCount: 0,
    requests: [],
    entries: [
      { key: 'customers', value: 'Local commuters and nearby office teams', source: 'public', ownerConfirmed: false },
      { key: 'busyperiods', value: 'Weekday mornings', source: 'owner', ownerConfirmed: true },
      { key: 'constraints', value: 'Two-person morning team', source: 'owner', ownerConfirmed: true },
      { key: 'currentpriorities', value: 'Reduce morning queue time', source: 'owner', ownerConfirmed: true },
      { key: 'seasonalnotes', value: 'Summer footfall rises', source: 'public', ownerConfirmed: false }
    ]
  };

  const server = http.createServer(async (req, res) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'authorization,content-type');
    res.setHeader('Access-Control-Allow-Methods', 'GET,PUT,OPTIONS');
    res.setHeader('Content-Type', 'application/json');
    if (req.method === 'OPTIONS') {
      res.statusCode = 204;
      res.end();
      return;
    }

    const url = new URL(req.url ?? '/', 'http://runtime.invalid');
    state.requests.push({ method: req.method, path: url.pathname, authorization: req.headers.authorization ?? null });
    const prefix = '/api/v1/businesses/dev-business/context';
    if (!url.pathname.startsWith(prefix)) {
      res.statusCode = 404;
      res.end(JSON.stringify({ message: 'Not found.' }));
      return;
    }

    if (req.method === 'GET' && url.pathname === prefix) {
      if (state.delayGetMs) await delay(state.delayGetMs);
      if (state.failGet) {
        res.statusCode = 503;
        res.end(JSON.stringify({ message: 'Runtime fixture unavailable.' }));
        return;
      }
      res.end(JSON.stringify(state.entries));
      return;
    }

    if (req.method === 'PUT' && url.pathname.startsWith(`${prefix}/`)) {
      if (state.delayPutMs) await delay(state.delayPutMs);
      if (state.failPut) {
        res.statusCode = 503;
        res.end(JSON.stringify({ message: 'Runtime fixture save failed.' }));
        return;
      }
      let body = '';
      for await (const chunk of req) body += chunk;
      const input = JSON.parse(body);
      const key = decodeURIComponent(url.pathname.slice(prefix.length + 1)).trim().toLowerCase();
      const next = { key, value: String(input.value ?? '').trim(), source: input.source, ownerConfirmed: Boolean(input.ownerConfirmed) };
      const index = state.entries.findIndex(entry => entry.key.trim().toLowerCase() === key);
      if (index >= 0) state.entries[index] = next;
      else state.entries.push(next);
      state.putCount += 1;
      res.end(JSON.stringify(next));
      return;
    }

    res.statusCode = 405;
    res.end(JSON.stringify({ message: 'Method not allowed.' }));
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
  const debugPort = 19000 + (process.pid % 1000);
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

test('VS-15 Context renders and recovers in authentic Expo Web runtime', { skip: !runRuntime, timeout: 180000 }, async t => {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'atlas-vs15-runtime-'));
  const exportRoot = path.join(tempRoot, 'web');
  const chromeProfile = path.join(tempRoot, 'chrome');
  fs.mkdirSync(artifactDir, { recursive: true });
  assert.equal(fs.existsSync(sessionWebPath), false, 'Tracked session.web.ts would invalidate the temporary runtime boundary.');
  fs.writeFileSync(sessionWebPath, sessionWebShim);

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
    if (fs.existsSync(sessionWebPath)) fs.unlinkSync(sessionWebPath);
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
  await cdp.evaluate(`localStorage.setItem('atlas.access-token','runtime-token'); localStorage.setItem('atlas.business-id','dev-business'); true`);

  fixture.state.failGet = true;
  await cdp.navigate(`${appOrigin}/context`);
  await cdp.waitFor('document.body.innerText.includes("Loading your business context")', 'Context loading state', 5000);
  await cdp.waitFor('document.body.innerText.includes("Context is unavailable")', 'Context recoverable load error', 10000);
  const retryHeight = await cdp.evaluate(`document.querySelector('[aria-label="Try loading business context again"]')?.getBoundingClientRect().height ?? 0`);
  assert.ok(retryHeight >= 44, `Retry action must be at least 44px high, got ${retryHeight}`);
  const errorScreenshot = path.join(artifactDir, 'context-390x844-load-error.png');
  await cdp.screenshot(errorScreenshot);

  fixture.state.failGet = false;
  fixture.state.delayGetMs = 450;
  const getCountBeforeRetry = fixture.state.requests.filter(request => request.method === 'GET' && request.path.endsWith('/context')).length;
  await clickByLabel(cdp, 'Try loading business context again');
  const retryRequestDeadline = Date.now() + 1500;
  while (Date.now() < retryRequestDeadline && fixture.state.requests.filter(request => request.method === 'GET' && request.path.endsWith('/context')).length <= getCountBeforeRetry) await delay(25);
  const getCountAfterRetry = fixture.state.requests.filter(request => request.method === 'GET' && request.path.endsWith('/context')).length;
  assert.ok(getCountAfterRetry > getCountBeforeRetry, `Retry click did not reach the Context GET boundary. GET count remained ${getCountAfterRetry}.`);
  await cdp.waitFor('document.body.innerText.includes("Loading your business context")', 'Context retry loading state', 3000);
  await cdp.waitFor('document.body.innerText.includes("Help Atlas understand how your business works.")', 'Context ready state', 10000);
  assert.equal(await cdp.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, '390px Context layout has horizontal overflow.');
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Busy periods context"]')?.value`), 'Weekday mornings');
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Current priorities context"]')?.value`), 'Reduce morning queue time');
  assert.equal(await cdp.evaluate('document.body.innerText.includes("ADDITIONAL SAVED CONTEXT")'), true, 'Unknown saved Context entry is not visible.');
  const readyScreenshot = path.join(artifactDir, 'context-390x844-ready.png');
  await cdp.screenshot(readyScreenshot);

  const initialPutCount = fixture.state.putCount;
  await clickByLabel(cdp, 'Save business context');
  await cdp.waitFor('document.body.innerText.includes("Confirm the public Customers context before saving.")', 'public provenance validation', 3000);
  assert.equal(fixture.state.putCount, initialPutCount, 'Unconfirmed public context reached the PUT boundary.');

  const uncheckedCount = await cdp.evaluate(`document.querySelectorAll('[role="checkbox"][aria-checked="false"]').length`);
  assert.equal(uncheckedCount, 2, 'Expected both public Context entries to require owner confirmation.');
  await cdp.evaluate(`Array.from(document.querySelectorAll('[role="checkbox"][aria-checked="false"]')).forEach(element => element.click()); true`);
  await cdp.waitFor(`document.querySelectorAll('[role="checkbox"][aria-checked="false"]').length === 0`, 'owner confirmation controls', 3000);

  await setInputByLabel(cdp, 'Customers context', 'Draft customer groups');
  await cdp.waitFor(`document.querySelector('[aria-label="Customers context"]')?.value === 'Draft customer groups'`, 'customer draft update', 3000);
  fixture.state.failPut = true;
  fixture.state.delayPutMs = 250;
  await clickByLabel(cdp, 'Save business context');
  await cdp.waitFor('document.body.innerText.includes("Could not save context. Your changes are still here.")', 'draft-safe save failure', 7000);
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Customers context"]')?.value`), 'Draft customer groups', 'Save failure discarded the Context draft.');
  await cdp.evaluate(`document.querySelector('[aria-label="Save business context"]')?.scrollIntoView({ block: 'center' }); true`);
  const failureScreenshot = path.join(artifactDir, 'context-390x844-save-failure.png');
  await cdp.screenshot(failureScreenshot);

  fixture.state.failPut = false;
  fixture.state.delayPutMs = 500;
  await clickByLabel(cdp, 'Save business context');
  await cdp.waitFor(`document.querySelector('[aria-label="Saving business context"]')?.getAttribute('aria-busy') === 'true'`, 'saving busy state', 3000);
  assert.equal(await cdp.evaluate('document.body.innerText.includes("Saving…")'), true, 'Visible saving state is missing.');
  await cdp.evaluate(`document.querySelector('[aria-label="Saving business context"]')?.scrollIntoView({ block: 'center' }); true`);
  const savingScreenshot = path.join(artifactDir, 'context-390x844-saving.png');
  await cdp.screenshot(savingScreenshot);
  await cdp.waitFor('document.body.innerText.includes("Context saved.")', 'Context save success', 12000);
  assert.equal(await cdp.evaluate(`document.querySelector('[aria-label="Customers context"]')?.value`), 'Draft customer groups');
  const savedScreenshot = path.join(artifactDir, 'context-390x844-save-success.png');
  await cdp.screenshot(savedScreenshot);

  const authenticatedRequests = fixture.state.requests.filter(request => request.path.startsWith('/api/v1/businesses/dev-business/context'));
  assert.ok(authenticatedRequests.length >= 3, 'Runtime did not exercise the Context API boundary.');
  assert.equal(authenticatedRequests.every(request => request.authorization === 'Bearer runtime-token'), true, 'Runtime Context request escaped the seeded authentication boundary.');

  await cdp.setViewport(768, 1024, 1);
  await cdp.evaluate('window.scrollTo(0, 0); true');
  await delay(150);
  assert.equal(await cdp.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, '768px Context layout has horizontal overflow.');
  const tabletScreenshot = path.join(artifactDir, 'context-768x1024-ready.png');
  await cdp.screenshot(tabletScreenshot);

  const interactiveHeights = await cdp.evaluate(`Array.from(document.querySelectorAll('[role="button"], [role="checkbox"]')).map(element => ({ label: element.getAttribute('aria-label'), height: element.getBoundingClientRect().height, disabled: element.getAttribute('aria-disabled') === 'true' }))`);
  const undersized = interactiveHeights.filter(item => !item.disabled && item.height > 0 && item.height < 44);
  assert.deepEqual(undersized, [], `Interactive Context targets below 44px: ${JSON.stringify(undersized)}`);

  const screenshots = [errorScreenshot, readyScreenshot, failureScreenshot, savingScreenshot, savedScreenshot, tabletScreenshot];
  const summary = {
    headSha: prHeadSha(),
    workflowSha: process.env.GITHUB_SHA ?? null,
    route: '/context',
    browser: chromeBinary,
    viewports: ['390x844@2x', '768x1024@1x'],
    assertions: {
      loading: true,
      recoverableLoadError: true,
      retry: true,
      normalizedServerKeysVisible: true,
      unknownServerEntryPreserved: true,
      publicOwnerConfirmation: true,
      validationBlocksUnconfirmedSave: true,
      saveFailurePreservesDraft: true,
      savingBusyState: true,
      saveSuccess: true,
      authenticatedBusinessBoundary: true,
      noHorizontalOverflow: true,
      minimumInteractiveTargetPx: 44
    },
    requestCount: authenticatedRequests.length,
    putCount: fixture.state.putCount,
    screenshots: screenshots.map(file => ({ file: path.basename(file), sha256: sha256(file) }))
  };
  fs.writeFileSync(path.join(artifactDir, 'runtime-summary.json'), `${JSON.stringify(summary, null, 2)}\n`);
});
