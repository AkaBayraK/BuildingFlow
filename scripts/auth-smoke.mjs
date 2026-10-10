// Real Keycloak protocol integration tests. Only a disposable local test account is changed.
import assert from "node:assert/strict";
import { randomBytes, createHash } from "node:crypto";
const realm = `http://localhost:${process.env.KEYCLOAK_PORT ?? 8080}/realms/buildingflow`;
const web = `http://localhost:${process.env.WEB_PORT ?? 3003}`;
const api = process.env.BF_AUTH_API_URL ?? `http://localhost:${process.env.API_PORT ?? 5005}`;
const mail = `http://localhost:${process.env.MAILPIT_PORT ?? 8025}`;
const username = process.env.BF_AUTH_TEST_USERNAME;
const email = process.env.BF_AUTH_TEST_EMAIL;
const password = process.env.BF_AUTH_TEST_PASSWORD;
assert.ok(username && email && password, "Run bash scripts/test-auth.sh");
let checks = 0;
function pass(name) { checks++; console.log(`PASS ${name}`); }
const decode = value => value.replaceAll("&amp;", "&").replaceAll("&#39;", "'").replaceAll("&quot;", '"');
function attr(tag, name) { return decode(tag.match(new RegExp(`\\b${name}=["']([^"']*)["']`))?.[1] ?? ""); }
function form(html) {
  const tag = html.match(/<form\b[^>]*>/i)?.[0];
  assert.ok(tag, "Expected Keycloak form");
  const fields = new URLSearchParams();
  for (const input of html.matchAll(/<input\b[^>]*>/gi)) if (attr(input[0], "type") === "hidden") fields.set(attr(input[0], "name"), attr(input[0], "value"));
  return { action: attr(tag, "action"), fields };
}
class Session {
  cookies = new Map();
  async request(url, options = {}) {
    url = new URL(url, realm).href;
    const response = await fetch(url, { ...options, redirect: "manual", headers: { Cookie: [...this.cookies].map(([key,value]) => `${key}=${value}`).join("; "), ...options.headers } });
    for (const cookie of response.headers.getSetCookie()) {
      const [pair] = cookie.split(";"); const i = pair.indexOf("=");
      this.cookies.set(pair.slice(0, i), pair.slice(i + 1));
    }
    return { status: response.status, location: response.headers.get("location"), html: await response.text(), url };
  }
  async follow(result) {
    for (let i=0; result.location && i<10; i++) {
      const url = new URL(result.location, result.url).href;
      if (url.startsWith(web + "/")) return { ...result, callback: url };
      result = await this.request(url);
    }
    return result;
  }
  async post(html, fields) {
    const data = form(html);
    for (const [name,value] of Object.entries(fields)) data.fields.set(name,value);
    return this.follow(await this.request(data.action, {method:"POST", body:data.fields}));
  }
}
async function login(secret, exchange = true) {
  const session = new Session();
  const verifier = randomBytes(32).toString("base64url");
  const state = randomBytes(16).toString("hex");
  const query = new URLSearchParams({client_id:"buildingflow-web", redirect_uri:web+"/", response_type:"code", response_mode:"query", scope:"openid", state, code_challenge:createHash("sha256").update(verifier).digest("base64url"), code_challenge_method:"S256", ui_locales:"tr", prompt:"login"});
  const page = await session.follow(await session.request(`${realm}/protocol/openid-connect/auth?${query}`));
  assert.match(page.html, /bf-art/);
  const result = secret === null ? page : await session.post(page.html, {username, password:secret, credentialId:""});
  if (!exchange) return { session, page, result };
  assert.ok(result.callback, "Login should return an authorization code");
  const callback = new URL(result.callback);
  assert.equal(callback.searchParams.get("state"), state);
  const response = await fetch(`${realm}/protocol/openid-connect/token`, {method:"POST", body:new URLSearchParams({grant_type:"authorization_code",client_id:"buildingflow-web",redirect_uri:web+"/",code:callback.searchParams.get("code"),code_verifier:verifier})});
  assert.equal(response.status,200);
  return { session, tokens:await response.json() };
}
assert.equal((await fetch(api+"/api/v1/status")).status,401); pass("anonymous API request rejected");
const wrong = await login("Wrong-password-A1",false);
assert.ok(!wrong.result.callback); assert.match(wrong.result.html,/kc-form-login/); pass("incorrect password rejected");
const initial = await login(password); pass("Authorization Code + S256 PKCE login");
assert.equal((await fetch(api+"/api/v1/status",{headers:{Authorization:`Bearer ${initial.tokens.access_token}`}})).status,200); pass("real Keycloak token accepted by API");
const preflight = await fetch(api+"/api/v1/status",{method:"OPTIONS",headers:{Origin:web,"Access-Control-Request-Method":"GET","Access-Control-Request-Headers":"authorization"}});
assert.equal(preflight.headers.get("access-control-allow-origin"),web); pass("frontend CORS preflight");
const refresh = await fetch(`${realm}/protocol/openid-connect/token`,{method:"POST",body:new URLSearchParams({grant_type:"refresh_token",client_id:"buildingflow-web",refresh_token:initial.tokens.refresh_token})});
assert.equal(refresh.status,200); const refreshed=await refresh.json(); pass("token renewal");
const logout = await initial.session.follow(await initial.session.request(`${realm}/protocol/openid-connect/logout?${new URLSearchParams({id_token_hint:initial.tokens.id_token,post_logout_redirect_uri:web+"/login",client_id:"buildingflow-web"})}`));
assert.equal(logout.callback,web+"/login"); pass("logout returns to signed-out page");
assert.equal((await fetch(`${realm}/protocol/openid-connect/token`,{method:"POST",body:new URLSearchParams({grant_type:"refresh_token",client_id:"buildingflow-web",refresh_token:refreshed.refresh_token})})).status,400); pass("logged-out session cannot renew token");
const reset = await login(null,false);
const resetUrl = decode(reset.page.html.match(/href=["']([^"']*reset-credentials[^"']*)["']/)?.[1] ?? "");
assert.ok(resetUrl); const resetPage = await reset.session.follow(await reset.session.request(resetUrl));
const sent = await reset.session.post(resetPage.html,{username:email});
assert.match(sent.html,/kc-info-message|kc-feedback-text/); pass("password reset request accepted");
let message;
for(let i=0;i<20;i++) {
  const messages=await (await fetch(mail+"/api/v1/messages")).json();
  message=messages.messages.find(m=>m.To.some(to=>to.Address===email));
  if(message) break; await new Promise(resolve=>setTimeout(resolve,250));
}
assert.ok(message,"Reset email should arrive in Mailpit");
const content=await (await fetch(mail+"/api/v1/message/"+message.ID)).json();
const resetLink=decode(content.HTML.match(/href=["']([^"']*action-token[^"']*)["']/)?.[1] ?? "");
assert.ok(resetLink); pass("email contains password reset link");
const linkSession=new Session();
let update=await linkSession.follow(await linkSession.request(resetLink));
assert.match(update.html,/password-new/); pass("email link opens new-password form");
const newPassword="A1"+randomBytes(16).toString("hex");
const mismatch=await linkSession.post(update.html,{"password-new":newPassword,"password-confirm":newPassword+"x"});
assert.match(mismatch.html,/password-new/); assert.match(mismatch.html,/aria-invalid="true"|input-error/); pass("password confirmation mismatch rejected");
const weak=await linkSession.post(mismatch.html,{"password-new":"short","password-confirm":"short"});
assert.match(weak.html,/password-new/); assert.match(weak.html,/alert-error|input-error/); pass("weak password rejected");
const updated=await linkSession.post(weak.html,{"password-new":newPassword,"password-confirm":newPassword});
assert.ok(!updated.html.includes('id="password-new"')); pass("password changed through email link");
const replay=await new Session().follow(await new Session().request(resetLink));
assert.ok(!replay.html.includes('id="password-new"')); pass("used reset link cannot be reused");
const old=await login(password,false); assert.ok(!old.result.callback); pass("old password rejected after reset");
await login(newPassword); pass("new password can log in");
console.log(`Authentication smoke: ${checks} checks passed. Disposable test account removed by wrapper.`);
