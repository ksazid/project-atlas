import assert from 'node:assert/strict';
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
  const state = { todayCount: 0, detailCount: 0, requests: [] };
  const opportunity = {
    id: 'runtime-opportunity',
    title: 'Review takeaway ordering path',
    whyItMatters: 'A clear ordering path can reduce avoidable customer friction.',
    whyNow: 'Takeaway is an owner-confirmed primary channel.',
    expectedImpact: 'Directional improvement in ordering clarity',
    effort: 'Low',
    confidence: 'Medium',
    evidenceSummary: '1 evidence item; priority goal #1: Increase revenue; restaurant-cafe v1.0.',
    status: 'available',
    expiresAt: '2026-08-18T12:00:00Z',
    knowledgePackKey: 'restaurant-cafe',
    knowledgePackVersion: '1.0',
    version: 1,
  };

  const detail = {
    id: opportunity.id,
    title: opportunity.title,
    status: 'available',
    goalAlignment: 'Aligned to priority #1: Increase revenue',
    goalTitle: 'Increase revenue',
    reason: opportunity.whyItMatters,
    whyNow: opportunity.whyNow,
    confidence: opportunity.confidence,
    expectedImpact: opportunity.expectedImpact,
    effort: opportunity.effort,
    evidence: [{ category: 'context', label: 'Primary channels', value: 'Takeaway', source: 'owner' }],
    assumptions: ['The owner-confirmed context remains accurate.'],
    limitations: ['Expected impact is directional, not guaranteed.'],
    sourceCategories: ['context'],
    actionSummary: 'Review the current takeaway ordering path before changing anything externally.',
    executionKitAvailable: true,
    createdAt: '2026-08-11T12:00:00Z',
    expiresAt: opportunity.expiresAt,
    isExpired: false,
    knowledgePackKey: opportunity.knowledgePackKey,
    knowledgePackVersion: opportunity.knowledgePackVersion,
    version: 1,
  };

  const server = http.createServer(async (req, res) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'authorization,content-type');
    res.setHeader('Access-Control-Allow-Methods', 'GET,POST,PUT,OPTIONS');
    res.setHeader('Content-Type', 'application/json');
    if (req.method === 'OPTIONS') {
      res.statusCode = 204;
      res.end();
      return;
    }

    const url = new URL(req.url ?? '/', 'http://runtime.invalid');
    state.requests.push({ method: req.method, path: url.pathname, authorization: req.headers.authorization ?? null });

    if (req.method === 'GET' && url.pathname === '/api/v1/businesses/runtime-business/progressive-questions') {
      res.end(JSON.stringify({ catalogueKey: 'restaurant-cafe', catalogueVersion: '1.0', questions: [] }));
      return;
    }

    if (req.method === 'GET' && url.pathname === '/api/v1/businesses/runtime-business/today-focus') {
      state.todayCount += 1;
      if (state.todayCount === 1) {
        res.end(JSON.stringify({ state: 'no-focus', code: 'opportunity_no_eligible_candidate', message: 'No evidence-qualified recommendation is available yet.' }));
        return;
      }
      if (state.todayCount === 2) {
        res.end(JSON.stringify({ state: 'degraded', code: 'bundle_temporarily_unavailable', message: 'Atlas could not safely prepare a recommendation right now.' }));
        return;
      }
      res.end(JSON.stringify({ state: 'ready', opportunity }));
      return;
    }

    if (req.method === 'GET' && url.pathname === '/api/v1/businesses/runtime-business/opportunities/runtime-opportunity') {
      state.detailCount += 1;
      res.end(JSON.stringify(detail));
      return;
    }

    if (req.method === 'GET' && url.pathname.endsWith('/action-decisions')) {
      res.end(JSON.stringify({ opportunityId: opportunity.id, currentStatus: 'available', version: 1, decisions: [] }));
      return;
    }

    if (req.method === 'GET' && url.pathname.endsWith('/outcome')) {
      res.statusCode = 404;
      res.end(JSON.stringify({ message: 'No outcome yet.' }));
      return;
    }

    res.statusCode = 404;
    res.end(JSON.stringify({ message: 'Not found.' }));
  });
  return { server, state };
}

function contentType(file) {
  const ext = path.extname(file).toLowerCase();
  return ({ '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8', '.json': 'application/json; charset=utf-8', '.css': 'text/css; charset=utf-8', '.svg': 'image/svg+xml', '.png': 'image/png', '.ico': 'image/x-icon', '.woff2': 'font/woff2' })[ext] ?? 'application/octet-stream';
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
      const fallback = path.join(exportRoot, 'index.html');
      if (fs.existsSync(fallback)) {
        res.statusCode = 200;
        res.setHeader('Content-Type', 'text/html; charset=utf-8');
        res.setHeader('Cache-Control', 'no-store');
        fs.createReadStream(fallback).pipe(res);
        return;
      }
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
  const child = spawn('npx', ['expo', 'export', '--clear', '--platform', 'web', '--output-dir', outputDir], {
    cwd: mobileRoot,
    env: { ...process.env, CI: 'true', EXPO_NO_TELEMETRY: '1', EXPO_PUBLIC_API_URL: apiUrl, EXPO_PUBLIC_AUTH_ISSUER: 'https://auth.runtime.invalid', EXPO_PUBLIC_AUTH_CLIENT_ID: 'runtime-client' },
    stdio: ['ignore', 'pipe', 'pipe']
  });
  let stdout = '';
  let stderr = '';
  child.stdout.on('data', chunk => { stdout += chunk; });
  child.stderr.on('data', chunk => { stderr += chunk; });
  const code = await new Promise((resolve, reject) => { child.once('error', reject); child.once('close', resolve); });
  assert.equal(code, 0, `Expo Web export failed.\nSTDOUT:\n${stdout}\nSTDERR:\n${stderr}`);
}

async function waitForJson(url, timeoutMs = 20000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url);
      if (response.ok) return response.json();
    } catch {}
    await delay(100);
  }
  throw new Error(`Timed out waiting for ${url}`);
}

class CdpClient {
  constructor(url) { this.url = url; this.nextId = 1; this.pending = new Map(); }
  async connect() {
    this.socket = new WebSocket(this.url);
    await new Promise((resolve, reject) => { this.socket.addEventListener('open', resolve, { once: true }); this.socket.addEventListener('error', reject, { once: true }); });
    this.socket.addEventListener('message', event => {
      const message = JSON.parse(event.data);
      if (!message.id) return;
      const pending = this.pending.get(message.id);
      if (!pending) return;
      this.pending.delete(message.id);
      if (message.error) pending.reject(new Error(message.error.message)); else pending.resolve(message.result ?? {});
    });
  }
  async send(method, params = {}) {
    const id = this.nextId++;
    const result = new Promise((resolve, reject) => this.pending.set(id, { resolve, reject }));
    this.socket.send(JSON.stringify({ id, method, params }));
    return result;
  }
  async evaluate(expression) {
    const result = await this.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true, userGesture: true });
    if (result.exceptionDetails) throw new Error(result.exceptionDetails.text ?? 'Runtime evaluation failed.');
    return result.result?.value;
  }
  async waitFor(expression, label, timeoutMs = 12000) {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      if (await this.evaluate(`Boolean(${expression})`)) return;
      await delay(80);
    }
    const body = await this.evaluate('document.body?.innerText ?? ""');
    throw new Error(`Timed out waiting for ${label}. Body:\n${body}`);
  }
  async navigate(url) { await this.send('Page.navigate', { url }); await this.waitFor('document.readyState === "complete"', `page load ${url}`, 15000); }
  async setViewport(width, height, deviceScaleFactor = 1) { await this.send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor, mobile: true, screenWidth: width, screenHeight: height }); }
  close() { this.socket?.close(); }
}

async function launchChrome(binary, appOrigin, userDataDir) {
  const debugPort = 22000 + (process.pid % 1000);
  const child = spawn(binary, ['--headless=new', '--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage', '--disable-background-networking', '--disable-default-apps', '--disable-extensions', '--no-first-run', `--remote-debugging-port=${debugPort}`, `--user-data-dir=${userDataDir}`, `${appOrigin}/`], { stdio: ['ignore', 'ignore', 'pipe'] });
  const targets = await waitForJson(`http://127.0.0.1:${debugPort}/json/list`);
  const page = targets.find(target => target.type === 'page');
  assert.ok(page?.webSocketDebuggerUrl, 'Chrome page target missing.');
  return { child, page };
}

async function clickByText(cdp, text) {
  const clicked = await cdp.evaluate(`(() => { const elements = [...document.querySelectorAll('[role="button"],button')]; const element = elements.find(item => (item.innerText ?? item.textContent ?? '').includes(${JSON.stringify(text)})); if (!element) return false; element.click(); return true; })()`);
  assert.equal(clicked, true, `Could not find button containing ${text}`);
}

test('VS-34 Today states and Best-move-to-detail path render in authentic Expo Web runtime', { skip: !runRuntime, timeout: 180000 }, async t => {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'atlas-vs34-runtime-'));
  const exportRoot = path.join(tempRoot, 'web');
  const chromeProfile = path.join(tempRoot, 'chrome');

  await delay(500);
  const ownsSessionShim = !fs.existsSync(sessionWebPath);
  if (ownsSessionShim) fs.writeFileSync(sessionWebPath, sessionWebShim);
  else assert.equal(fs.readFileSync(sessionWebPath, 'utf8'), sessionWebShim);

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
  assert.ok(chromeBinary, 'GitHub runner does not provide Chrome/Chromium.');
  chrome = await launchChrome(chromeBinary, appOrigin, chromeProfile);
  cdp = new CdpClient(chrome.page.webSocketDebuggerUrl);
  await cdp.connect();
  await cdp.send('Page.enable');
  await cdp.send('Runtime.enable');
  await cdp.setViewport(390, 844, 2);

  // Ensure Chrome is on the app origin before touching localStorage; blank/opaque origins can reject storage access.
  await cdp.navigate(`${appOrigin}/`);
  await cdp.evaluate(`localStorage.setItem('atlas.access-token','runtime-token'); localStorage.setItem('atlas.business-id','runtime-business'); true`);
  await cdp.navigate(`${appOrigin}/`);
  await cdp.waitFor('document.body.innerText.includes("Nothing strong enough to recommend yet.")', 'no-focus state');
  let text = await cdp.evaluate('document.body.innerText');
  assert.doesNotMatch(text, /I’ll do this|Bolt Food|Wolt|Google Places/i);

  await cdp.navigate(`${appOrigin}/`);
  await cdp.waitFor('document.body.innerText.includes("Today couldn’t refresh safely.")', 'degraded state');
  text = await cdp.evaluate('document.body.innerText');
  assert.match(text, /Try again/);
  assert.doesNotMatch(text, /I’ll do this|Bolt Food|Wolt|Google Places/i);

  await clickByText(cdp, 'Try again');
  await cdp.waitFor('document.body.innerText.includes("Review takeaway ordering path") && document.body.innerText.includes("BEST MOVE") && document.body.innerText.includes("Updated just now")', 'ready state');
  text = await cdp.evaluate('document.body.innerText');
  assert.match(text, /I’ll do this/);
  assert.match(text, /Why this\?/);
  assert.doesNotMatch(text, /RECOMMENDED MOVE|One action\. Clear reason\. Measurable outcome\./i);

  await clickByText(cdp, 'Why this?');
  await cdp.waitFor('document.body.innerText.includes("OPPORTUNITY DETAIL") && document.body.innerText.includes("Primary channels") && document.body.innerText.includes("Open Execution Kit")', 'Opportunity Detail route', 15000);

  text = await cdp.evaluate('document.body.innerText');
  assert.match(text, /Increase revenue/);
  assert.match(text, /Takeaway/);
  assert.doesNotMatch(text, /Bolt Food|Wolt|Google Places/i);
  assert.equal(fixture.state.todayCount, 3);
  assert.equal(fixture.state.detailCount, 1);
  assert.equal(fixture.state.requests.every(request => request.authorization === 'Bearer runtime-token'), true);
});
