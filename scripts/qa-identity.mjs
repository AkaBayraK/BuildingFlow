// QA FAZ 05 identity checks against a throwaway realm created by scripts/qa-identity.sh; only that realm is touched.
import assert from 'node:assert/strict';
import { createHash, randomBytes } from 'node:crypto';
import { writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';

const keycloak = `http://localhost:${process.env.KEYCLOAK_PORT ?? 8080}`;
const realmName = process.env.BF_QA_REALM, web = `http://localhost:${process.env.WEB_PORT ?? 3003}`, api = `http://localhost:${process.env.API_PORT ?? 5005}`;
const users = JSON.parse(process.env.BF_QA_USERS ?? '[]'), runId = process.env.BF_QA_RUN_ID;
assert.ok(realmName?.startsWith('bf-qa-') && users.length && runId, 'Run bash scripts/qa-identity.sh');
const realm = `${keycloak}/realms/${realmName}`;
const results = [];
const record = (action, expected, actual, passed) => { results.push({ action, expected, actual, status: passed ? 'PASS' : 'FAIL' }); console.log(`[${passed ? 'PASS' : 'FAIL'}] ${action}`); };

const decode = value => value.replaceAll('&amp;', '&').replaceAll('&#39;', "'").replaceAll('&quot;', '"');
const attr = (tag, name) => decode(tag.match(new RegExp(`\\b${name}=["']([^"']*)["']`))?.[1] ?? '');
async function login(username, password) {
  const cookies = new Map();
  const request = async (url, options = {}) => {
    const response = await fetch(url, { ...options, redirect: 'manual', headers: { Cookie: [...cookies].map(([k, v]) => `${k}=${v}`).join('; ') } });
    for (const cookie of response.headers.getSetCookie()) { const [pair] = cookie.split(';'); const i = pair.indexOf('='); cookies.set(pair.slice(0, i), pair.slice(i + 1)); }
    return { location: response.headers.get('location'), html: await response.text(), url };
  };
  const verifier = randomBytes(32).toString('base64url'), state = randomBytes(16).toString('hex');
  const query = new URLSearchParams({ client_id: 'buildingflow-web', redirect_uri: web + '/', response_type: 'code', response_mode: 'query', scope: 'openid', state,
    code_challenge: createHash('sha256').update(verifier).digest('base64url'), code_challenge_method: 'S256' });
  const page = await request(`${realm}/protocol/openid-connect/auth?${query}`);
  const tag = page.html.match(/<form\b[^>]*>/i)?.[0];
  if (!tag) return { error: 'form' };
  const result = await request(new URL(attr(tag, 'action'), page.url).href, { method: 'POST', body: new URLSearchParams({ username, password, credentialId: '' }) });
  if (!result.location?.startsWith(web + '/')) return { error: 'credentials' };
  const callback = new URL(result.location);
  const exchange = async withVerifier => fetch(`${realm}/protocol/openid-connect/token`, { method: 'POST', body: new URLSearchParams({ grant_type: 'authorization_code', client_id: 'buildingflow-web',
    redirect_uri: web + '/', code: callback.searchParams.get('code'), ...(withVerifier ? { code_verifier: verifier } : {}) }) });
  return { state: callback.searchParams.get('state') === state, exchange };
}
const claims = token => JSON.parse(Buffer.from(token.split('.')[1], 'base64url').toString());

for (const user of users) {
  const session = await login(user.username, user.password);
  const response = session.exchange ? await session.exchange(true) : null;
  const token = response?.status === 200 ? (await response.json()).access_token : null;
  const roles = token ? (claims(token).realm_access?.roles ?? []).filter(role => role === role.toUpperCase()).sort() : [];
  record(`${user.roles.join('+')} kullanıcısı PKCE S256 ile giriş yaptı`, `token, state eşleşir, roller ${user.roles.join(',')}`, token ? `roller ${roles.join(',')}` : `giriş başarısız (${session.error ?? response?.status})`,
    !!token && session.state && JSON.stringify(roles) === JSON.stringify([...user.roles].sort()));
  if (token && user === users[0]) {
    const status = (await fetch(api + '/api/v1/status', { headers: { Authorization: `Bearer ${token}` } })).status;
    record('Ayrı QA realm token\'ı BuildingFlow API\'ye gönderildi', '401 (issuer farklı, realm izolasyonu)', `HTTP ${status}`, status === 401);
  }
}
const first = users[0];
const wrong = await login(first.username, 'Wrong-password-A1x');
record('Yanlış parola ile giriş denendi', 'kod verilmez', wrong.error ?? 'kod verildi', wrong.error === 'credentials');
const noVerifier = await login(first.username, first.password);
const missing = noVerifier.exchange ? (await noVerifier.exchange(false)).status : 0;
record('PKCE verifier olmadan kod değişimi denendi', '400 invalid_grant', `HTTP ${missing}`, missing === 400);
const password = await fetch(`${realm}/protocol/openid-connect/token`, { method: 'POST', body: new URLSearchParams({ grant_type: 'password', client_id: 'buildingflow-web', username: first.username, password: first.password }) });
record('Password grant denendi', '400/401 (kapalı)', `HTTP ${password.status}`, password.status === 400 || password.status === 401);

const out = join('artifacts/qa', runId, 'identity');
mkdirSync(out, { recursive: true });
writeFileSync(join(out, 'report.json'), JSON.stringify({ runId, realm: realmName, results }, null, 1));
const failed = results.filter(item => item.status === 'FAIL').length;
console.log(`[QA KİMLİK] ${results.length - failed}/${results.length} PASS; rapor ${join(out, 'report.json')}`);
process.exitCode = failed ? 1 : 0;
