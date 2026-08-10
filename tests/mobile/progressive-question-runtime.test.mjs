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
const artifactDir = path.join(root, 'dashboard/runtime-vs17');
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

function initialQuestions() {
  return [
    {
      questionKey: 'generic.primary-channel',
      targetContextKey: 'primarychannels',
      prompt: 'How do customers usually buy from you?',
      helper: 'This helps Atlas keep suggestions practical for the way you operate.',
      answerType: 'multi-choice',
      options: ['In person', 'Phone/message', 'Own website/app'],
      maxSelections: 2,
      maxLength: null,
    },
    {
      questionKey: 'generic.primary-constraint',
      targetContextKey: 'constraints',
      prompt: 'What limits the business most right now?',
      helper: 'Choose the constraint that most changes what is practical today.',
      answerType: 'single-choice',
      options: ['Time', 'Staffing', 'Capacity'],
      maxSelections: 1,
      maxLength: null,
    },
  ];
}

function questionSet(questions) {
  return { catalogueKey: 'progressive-onboarding', catalogueVersion: '1', questions };
}

function createApiFixture() {
  const state = {
    questions: initialQuestions(),
    requests: [],
    answerCount: 0,
    skipCount: 0,
    answerBody: null,
    skipBody: null,
    failGet: false,
  };

  const server = http.createServer(async (req, res) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'authorization,content-type');
    res.setHeader('Access-Control-Allow-Methods', 'GET,POST,OPTIONS');
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

    const prefix = '/api/v1/businesses/dev-business/progressive-questions';
    if (req.method === 'GET' && url.pathname === prefix) {
      await delay(220);
      if (state.failGet) {
        res.statusCode = 503;
        res.end(JSON.stringify({ code: 'progressive_questions_unavailable', message: 'Runtime optional questions unavailable.' }));
        return;
      }
      res.end(JSON.stringify(questionSet(state.questions)));
      return;
    }

    if (req.method === 'POST' && url.pathname === `${prefix}/generic.primary-channel/answer`) {
      state.answerCount += 1;
      state.answerBody = parsedBody;
      assert.deepEqual(parsedBody, { catalogueVersion: '1', selections: ['In person'], text: null });
      state.questions = state.questions.filter(question => question.questionKey !== 'generic.primary-channel');
      await delay(220);
      res.end(JSON.stringify({
        status: 'answered',
        questionKey: 'generic.primary-channel',
        catalogueVersion: '1',
        remaining: questionSet(state.questions),
      }));
      return;
    }

    if (req.method === 'POST' && url.pathname === `${prefix}/generic.primary-constraint/skip`) {
      state.skipCount += 1;
      state.skipBody = parsedBody;
      assert.deepEqual(parsedBody, { catalogueVersion: '1' });
      state.questions = state.questions.filter(question => question.questionKey !== 'generic.primary-constraint');
      await delay(220);
      res.end(JSON.stringify({
        status: 'skipped',
        questionKey: 'generic.primary-constraint',
        catalogueVersion: '1',
        remaining: questionSet(state.questions),
      }));
      return;
    }

    // Today may request other optional endpoints after the runtime leaves onboarding.
    res.statusCode = 404;
    res.end(JSON.stringify({ message: 'Not found.' }));
  });

  return { server, state };
}

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

function contentType(file) {
  const ext = path.extname(file).toLowerCase();
  return ({
    '.html': 'text/html; charset=utf-8',
    '.js': 'text/javascript; charset=utf-8',
    '.mjs': 'text/javascript; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.svg': 'image/svg+xml',
    '.png': 'image/png',
    '.ico': 'image/x-icon',
    '.woff2': 'font/woff2',
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
  const child = spawn('npx', ['expo', 'export', '--clear', '--platform', 'web', '--output-dir', outputDir], {
    cwd: mobileRoot,
    env: {
      ...process.env,
      CI: 'true',
      EXPO_NO_TELEMETRY: '1',
      EXPO_PUBLIC_API_URL: apiUrl,
      EXPO_PUBLIC_AUTH_ISSUER: 'https://auth.runtime.invalid',
      EXPO_PUBLIC_AUTH_CLIENT_ID: 'runtime-client',
    },
    stdio: ['ignore', 'pipe', 'pipe'],
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
  const debugPort = 21000 + (process.pid % 1000);
  const child = spawn(binary, [
    '--headless=new', '--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage', '--disable-background-networking',
    '--disable-default-apps', '--disable-extensions', '--no-first-run', '--no-default-browser-check',
    `--remote-debugging-port=${debugPort}`, `--user-data-dir=${userDataDir}`, `${appOrigin}/`,
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
  const clicked = await cdp.evaluate(`(() => {
    const element = document.querySelector('[aria-label=${JSON.stringify(label)}]');
    if (!element) return false;
    element.click();
    return true;
  })()`);
  assert.equal(clicked, true, `Could not find interactive element labelled ${label}`);
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

test('VS-17 progressive questions complete in authentic Expo Web runtime', { skip: !runRuntime, timeout: 180000 }, async t => {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'atlas-vs17-runtime-'));
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
  await cdp.navigate(`${appOrigin}/progressive-questions`);
  await cdp.waitFor('document.body.innerText.includes("How do customers usually buy from you?")', 'first progressive question', 12000);
  await cdp.waitFor('document.body.innerText.includes("Question 1 of 2")', 'first question progress', 3000);
  assert.equal(await cdp.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, '390px progressive question layout has horizontal overflow.');

  const firstQuestionScreenshot = path.join(artifactDir, 'progressive-390x844-question-1.png');
  await cdp.screenshot(firstQuestionScreenshot);

  const enabledTargets = await cdp.evaluate(`Array.from(document.querySelectorAll('[role="button"], [role="checkbox"], [role="radio"]')).map(element => ({ label: element.getAttribute('aria-label'), height: element.getBoundingClientRect().height, disabled: element.getAttribute('aria-disabled') === 'true' }))`);
  const undersized = enabledTargets.filter(item => !item.disabled && item.height > 0 && item.height < 44);
  assert.deepEqual(undersized, [], `Enabled progressive targets below 44px: ${JSON.stringify(undersized)}`);

  await cdp.setViewport(768, 1024, 1);
  assert.equal(await cdp.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, '768px progressive question layout has horizontal overflow.');
  const tabletScreenshot = path.join(artifactDir, 'progressive-768x1024-question-1.png');
  await cdp.screenshot(tabletScreenshot);
  await cdp.setViewport(390, 844, 2);

  await clickByLabel(cdp, 'In person');
  await cdp.waitFor(`document.querySelector('[aria-label="In person"]')?.getAttribute('aria-checked') === 'true'`, 'selected multi-choice state', 3000);
  const selectedScreenshot = path.join(artifactDir, 'progressive-390x844-selected.png');
  await cdp.screenshot(selectedScreenshot);

  await clickByLabel(cdp, 'Continue with this answer');
  await cdp.waitFor('document.body.innerText.includes("What limits the business most right now?")', 'second progressive question', 10000);
  await cdp.waitFor('document.body.innerText.includes("Question 2 of 2")', 'second question progress', 3000);
  assert.equal(fixture.state.answerCount, 1, 'Progressive answer boundary should be exercised exactly once.');
  assert.deepEqual(fixture.state.answerBody, { catalogueVersion: '1', selections: ['In person'], text: null });

  await clickByLabel(cdp, 'Skip for now');
  await cdp.waitFor('document.body.innerText.includes("That’s enough to get started.")', 'progressive completion state', 10000);
  assert.equal(fixture.state.skipCount, 1, 'Progressive skip boundary should be exercised exactly once.');
  assert.deepEqual(fixture.state.skipBody, { catalogueVersion: '1' });
  const completionScreenshot = path.join(artifactDir, 'progressive-390x844-complete.png');
  await cdp.screenshot(completionScreenshot);

  await clickByLabel(cdp, 'Continue to Today');
  await cdp.waitFor('!location.pathname.includes("progressive-questions")', 'Today handoff', 7000);

  // Prove optional enrichment never traps the owner if its load boundary is unavailable.
  fixture.state.failGet = true;
  fixture.state.questions = initialQuestions();
  const mutationCountBeforeDegraded = fixture.state.answerCount + fixture.state.skipCount;
  await cdp.navigate(`${appOrigin}/progressive-questions`);
  await cdp.waitFor('document.body.innerText.includes("These questions are unavailable right now.")', 'optional load failure', 12000);
  const degradedScreenshot = path.join(artifactDir, 'progressive-390x844-unavailable.png');
  await cdp.screenshot(degradedScreenshot);
  await clickByLabel(cdp, 'Continue for now');
  await cdp.waitFor('!location.pathname.includes("progressive-questions")', 'optional bypass to Today', 7000);
  assert.equal(fixture.state.answerCount + fixture.state.skipCount, mutationCountBeforeDegraded, 'Optional bypass must not fabricate an answer or skip mutation.');

  const authenticatedRequests = fixture.state.requests.filter(request => request.path.includes('/progressive-questions'));
  assert.ok(authenticatedRequests.length >= 4, 'Runtime did not exercise the progressive-question API boundary.');
  assert.equal(authenticatedRequests.every(request => request.authorization === 'Bearer runtime-token'), true, 'Runtime progressive request escaped seeded authentication boundary.');

  const screenshots = [firstQuestionScreenshot, selectedScreenshot, completionScreenshot, degradedScreenshot, tabletScreenshot];
  const summary = {
    headSha: prHeadSha(),
    workflowSha: process.env.GITHUB_SHA ?? null,
    route: '/progressive-questions',
    browser: chromeBinary,
    viewports: ['390x844@2x', '768x1024@1x'],
    assertions: {
      oneQuestionAtATime: true,
      progressVisible: true,
      tapFirstChoice: true,
      answerBoundary: true,
      skipWithoutFakeAnswer: true,
      completionHandoff: true,
      optionalLoadFailureBypass: true,
      authenticatedBusinessBoundary: true,
      noHorizontalOverflow: true,
      minimumInteractiveTargetPx: 44,
    },
    answerCount: fixture.state.answerCount,
    skipCount: fixture.state.skipCount,
    requestCount: authenticatedRequests.length,
    screenshots: screenshots.map(file => ({ file: path.basename(file), sha256: sha256(file) })),
  };
  fs.writeFileSync(path.join(artifactDir, 'runtime-summary.json'), `${JSON.stringify(summary, null, 2)}\n`);
});
