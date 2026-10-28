// QA FAZ 04 browser driver: system Chrome over the DevTools Protocol (no new package). Run it through scripts/qa-web.sh.
import { spawn } from 'node:child_process';
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { setTimeout as sleep } from 'node:timers/promises';
import { PAGE_AUDIT, VIEWPORTS, judge, plan, recordingPlayer, reportLine, slug } from './qa-web-lib.mjs';

const CHROME = process.env.BF_QA_CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const WEB = `http://localhost:${process.env.WEB_PORT ?? 3003}`;
const API = `http://localhost:${process.env.API_PORT ?? 5005}`;
const runId = process.env.BF_QA_RUN_ID;
const username = process.env.BF_QA_WEB_USERNAME, password = process.env.BF_QA_WEB_PASSWORD;
if (!runId || !username || !password) throw new Error('Run bash scripts/qa-web.sh');
const out = join('artifacts/qa', runId, 'web');
mkdirSync(out, { recursive: true });

class Cdp {
  constructor(url) { this.ws = new WebSocket(url); this.id = 0; this.pending = new Map(); this.listeners = []; }
  async open() {
    await new Promise((resolve, reject) => { this.ws.onopen = resolve; this.ws.onerror = reject; });
    this.ws.onmessage = ({ data }) => {
      const message = JSON.parse(data);
      if (message.id && this.pending.has(message.id)) { const { resolve, reject } = this.pending.get(message.id); this.pending.delete(message.id); message.error ? reject(new Error(message.error.message)) : resolve(message.result); }
      else for (const listener of this.listeners) listener(message);
    };
  }
  send(method, params = {}) { const id = ++this.id; this.ws.send(JSON.stringify({ id, method, params })); return new Promise((resolve, reject) => this.pending.set(id, { resolve, reject })); }
  on(listener) { this.listeners.push(listener); }
  async value(expression) { const r = await this.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true }); if (r.exceptionDetails) throw new Error('evaluate failed'); return r.result.value; }
  async until(expression, timeout = 15000) { const end = Date.now() + timeout; while (Date.now() < end) { if (await this.value(expression).catch(() => false)) return true; await sleep(200); } return false; }
}

const profile = mkdtempSync(join(tmpdir(), 'bf-qa-chrome-'));
const chrome = spawn(CHROME, ['--headless=new', '--remote-debugging-port=0', `--user-data-dir=${profile}`, '--no-first-run', '--no-default-browser-check', '--disable-gpu', '--lang=tr-TR', 'about:blank'], { stdio: 'ignore' });
const items = [];
let exitCode = 1;
try {
  const portFile = join(profile, 'DevToolsActivePort');
  for (let i = 0; i < 100 && !existsSync(portFile); i++) await sleep(100);
  const port = readFileSync(portFile, 'utf8').split('\n')[0];
  const page = (await (await fetch(`http://127.0.0.1:${port}/json/list`)).json()).find(target => target.type === 'page');
  const cdp = new Cdp(page.webSocketDebuggerUrl); await cdp.open();
  await Promise.all(['Page.enable', 'Runtime.enable', 'Network.enable'].map(method => cdp.send(method)));
  const state = { exceptions: [], serverErrors: [], clientErrors: [], inflight: new Set(), documentStatus: null };
  cdp.on(({ method, params }) => {
    if (method === 'Runtime.exceptionThrown') state.exceptions.push(params.exceptionDetails.exception?.className ?? 'Error');
    if (method === 'Network.requestWillBeSent' && params.request.url.startsWith(API)) state.inflight.add(params.requestId);
    if (method === 'Network.loadingFinished' || method === 'Network.loadingFailed') state.inflight.delete(params.requestId);
    if (method === 'Network.responseReceived') {
      const { url, status } = params.response, path = new URL(url).pathname;
      if (params.type === 'Document' && url.startsWith(WEB)) state.documentStatus = status;
      if (url.startsWith(API) && status >= 500) state.serverErrors.push(`${status} ${path}`);
      else if (url.startsWith(API) && status >= 400) state.clientErrors.push(`${status} ${path}`);
    }
  });
  const reset = () => { state.exceptions = []; state.serverErrors = []; state.clientErrors = []; state.documentStatus = null; };
  const settle = async () => {
    await cdp.until('document.readyState === "complete"');
    let quiet = 0;
    // Settled = no API request in flight and no loading text for 800 ms, capped at 10 s so a polling screen cannot stall the run.
    for (let i = 0; i < 50 && quiet < 4; i++) { await sleep(200); quiet = state.inflight.size === 0 && !(await cdp.value('/Yükleniyor|Güvenli giriş hazırlanıyor/.test(document.querySelector("main")?.innerText ?? document.body.innerText)').catch(() => true)) ? quiet + 1 : 0; }
  };
  const shot = async name => { const file = join(out, `${name}.png`); writeFileSync(file, Buffer.from((await cdp.send('Page.captureScreenshot', { format: 'png' })).data, 'base64')); return file; };
  const viewport = v => cdp.send('Emulation.setDeviceMetricsOverride', { width: v.width, height: v.height, deviceScaleFactor: 1, mobile: v.mobile });
  const click = text => cdp.value(`(() => { const el = [...document.querySelectorAll('button,[role=tab],a')].find(e => e.textContent.trim().includes(${JSON.stringify(text)}) && !e.disabled); if (!el) return false; el.click(); return true; })()`);

  await viewport(VIEWPORTS[0]);
  await cdp.send('Page.navigate', { url: `${WEB}/dashboard` });
  const loginShown = await cdp.until('location.port === "8080" && !!document.querySelector("#username")', 30000);
  if (loginShown) await cdp.value(`(() => { const set = (id, v) => { const el = document.getElementById(id); el.value = v; el.dispatchEvent(new Event('input', { bubbles: true })); }; set('username', ${JSON.stringify(username)}); set('password', ${JSON.stringify(password)}); document.querySelector('#kc-form-login').submit(); return true; })()`);
  const signedIn = loginShown && await cdp.until(`location.origin === ${JSON.stringify(WEB)} && !/Güvenli giriş/.test(document.body.innerText)`, 30000);
  items.push({ route: '/login', viewport: 'desktop', action: 'Geçici SUPER_ADMIN ile PKCE girişi yapıldı', expected: 'Keycloak formu ve uygulamaya dönüş', actual: signedIn ? 'uygulamaya dönüldü' : loginShown ? 'giriş sonrası dönüş yok' : 'Keycloak formu açılmadı', status: signedIn ? 'PASS' : 'FAIL', evidence: await shot('login') });
  if (!signedIn) throw new Error('login failed');

  await cdp.send('Page.navigate', { url: `${WEB}/properties` }); await settle();
  const propertyId = await cdp.value(`(() => { const link = [...document.querySelectorAll('a[href^="/properties/"]')].map(a => a.getAttribute('href').match(/^\\/properties\\/([0-9a-f-]{36})/i)).find(Boolean); return link ? link[1] : null; })()`);
  const inventory = JSON.parse(readFileSync('qa-center/inventory/inventory.json', 'utf8'));
  const only = process.env.BF_QA_ROUTES ? process.env.BF_QA_ROUTES.split(',') : null;
  for (const target of plan(inventory.webPages, propertyId, { sessionContexts: process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED === 'true' }, only)) {
    if (!target.path) { items.push({ route: target.route, viewport: '-', action: 'Ekran atlandı', expected: 'açılabilir örnek kayıt', actual: target.skip, status: 'SKIPPED', evidence: '' }); continue; }
    for (const v of VIEWPORTS) {
      await viewport(v); reset();
      await cdp.send('Page.navigate', { url: WEB + target.path }); await settle();
      const audit = await cdp.value(PAGE_AUDIT);
      const finalPath = await cdp.value('location.pathname');
      const result = judge({ documentStatus: state.documentStatus, finalPath, expectedPath: target.expectedPath, exceptions: state.exceptions, serverErrors: state.serverErrors, audit, viewport: v.name });
      const notes = [...result.problems, ...(state.clientErrors.length ? [`API 4xx: ${[...new Set(state.clientErrors)].join(', ')}`] : []), ...(audit.alerts.length ? [`uyarı: ${audit.alerts.join(' | ')}`] : [])];
      items.push({ route: target.route, path: target.path, viewport: v.name, action: 'Ekran açıldı ve denetlendi', expected: 'HTTP 200, yönlendirme/JS/5xx yok, başlık var, mobilde taşma yok, a11y kuralları temiz',
        actual: notes.length ? notes.join('; ') : 'tüm kontroller temiz', status: result.status, evidence: await shot(`${slug(target.route)}-${v.name}`) });
    }
  }

  if (only && !only.includes('/system-settings/qa-center')) { exitCode = items.some(item => item.status === 'FAIL') ? 1 : 0; throw Object.assign(new Error('subset'), { subset: true }); }
  await viewport(VIEWPORTS[0]); reset();
  // Screen recording without a video encoder: CDP screencast frames plus a self-contained HTML player.
  const recording = join(out, 'qa-center-recording'); mkdirSync(recording, { recursive: true });
  const frames = [];
  cdp.on(({ method, params }) => {
    if (method !== 'Page.screencastFrame') return;
    if (frames.length < 300) { const file = `frame-${String(frames.length).padStart(4, '0')}.jpg`; writeFileSync(join(recording, file), Buffer.from(params.data, 'base64')); frames.push({ file, at: params.metadata.timestamp }); }
    cdp.send('Page.screencastFrameAck', { sessionId: params.sessionId }).catch(() => {});
  });
  await cdp.send('Page.startScreencast', { format: 'jpeg', quality: 60, everyNthFrame: 2 });
  await cdp.send('Page.navigate', { url: `${WEB}/system-settings/qa-center` }); await settle();
  const started = await click('Test Senaryoları') && await cdp.until(`[...document.querySelectorAll('h2')].some(h => h.textContent.includes('Veritabanı Migration'))`)
    && await cdp.value(`(() => { const card = [...document.querySelectorAll('h2')].find(h => h.textContent.includes('Veritabanı Migration')).closest('div.flex-col'); const b = card && [...card.querySelectorAll('button')].find(x => x.textContent.includes('Testi Başlat')); if (!b) return false; b.click(); return true; })()`);
  const passed = started && await cdp.until(`document.querySelectorAll('[role=tabpanel] ol li').length === 2 && /Geçti/.test(document.querySelector('[role=tabpanel]').innerText) && !/Çalışıyor|Kuyrukta/.test(document.querySelector('[role=tabpanel]').innerText)`, 30000);
  const steps = await cdp.value(`[...document.querySelectorAll('[role=tabpanel] ol li')].map(li => li.innerText.replace(/\\s+/g, ' ').trim())`);
  await cdp.send('Page.stopScreencast');
  writeFileSync(join(recording, 'player.html'), recordingPlayer(frames));
  items.push({ route: '/system-settings/qa-center', viewport: 'desktop', action: `Akış ekran kaydına alındı (${frames.length} kare)`, expected: 'en az 1 kare ve oynatıcı',
    actual: frames.length ? `${frames.length} kare, ${((frames.at(-1).at - frames[0].at) || 0).toFixed(1)} sn` : 'kare yok', status: frames.length ? 'PASS' : 'FAIL', evidence: join(recording, 'player.html') });
  items.push({ route: '/system-settings/qa-center', viewport: 'desktop', action: 'Veritabanı Migration senaryosu UI üzerinden başlatıldı', expected: 'koşum Geçti, 2 adım (database.connect, database.pendingMigrations=0)',
    actual: passed ? `Geçti; ${steps.join(' / ')}` : `beklenen sonuca ulaşılmadı; adımlar: ${steps.join(' / ') || 'yok'}`, status: passed ? 'PASS' : 'FAIL', evidence: await shot('qa-center-run-desktop') });
  await viewport(VIEWPORTS[1]); await settle();
  const mobileAudit = await cdp.value(PAGE_AUDIT);
  items.push({ route: '/system-settings/qa-center', viewport: 'mobile', action: 'Koşum sonucu 375px görünümde denetlendi', expected: 'yatay taşma yok', actual: `taşma ${mobileAudit.overflow}px`, status: mobileAudit.overflow > 1 ? 'FAIL' : 'PASS', evidence: await shot('qa-center-run-mobile') });
  exitCode = items.some(item => item.status === 'FAIL') ? 1 : 0;
} catch (error) {
  if (!error.subset) items.push({ route: '-', viewport: '-', action: 'Tarayıcı koşumu', expected: 'tamamlanır', actual: `durdu: ${error.message}`, status: 'FAIL', evidence: '' });
} finally {
  const closed = new Promise(resolve => chrome.once('exit', resolve)); chrome.kill(); await closed;
  rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
  const count = status => items.filter(item => item.status === status).length;
  const summary = `PASS ${count('PASS')} / FAIL ${count('FAIL')} / SKIPPED ${count('SKIPPED')}`;
  writeFileSync(join(out, 'report.json'), JSON.stringify({ runId, summary, items }, null, 1));
  writeFileSync(join(out, 'report.md'), `# QA FAZ 04 web koşumu ${runId}\n\n${summary}\n\n${items.map((item, i) => reportLine(i + 1, item)).join('\n')}\n`);
  console.log(`[QA WEB] ${runId}: ${summary}; rapor ${join(out, 'report.md')}`);
  process.exitCode = exitCode;
}
