import { networkInterfaces } from 'node:os';
import { readFileSync, writeFileSync } from 'node:fs';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { resolve, dirname } from 'node:path';
import { createConnection } from 'node:net';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
let children = []; let changing = false; let stopping = false; let host;
function lanAddress() {
  if (process.env.BUILDINGFLOW_MOBILE_HOST) return process.env.BUILDINGFLOW_MOBILE_HOST;
  const entries = Object.entries(networkInterfaces()).filter(([name]) => /^en\d+$/.test(name)).sort(([a], [b]) => a === 'en0' ? -1 : b === 'en0' ? 1 : a.localeCompare(b));
  return entries.flatMap(([, values]) => values ?? []).find(v => v.family === 'IPv4' && !v.internal)?.address;
}
function configure(address) {
  if (!address || !/^(10\.|192\.168\.|172\.(1[6-9]|2\d|3[01])\.)/.test(address) || !/^\d{1,3}(\.\d{1,3}){3}$/.test(address) || address.split('.').some(n => +n > 255)) throw Error('Mac için özel LAN adresi bulunamadı. Wi-Fi bağlantısını kontrol et.');
  const path = resolve(root, '.env');
  let text = readFileSync(path, 'utf8');
  const values = { EXPO_PUBLIC_API_URL: `http://${address}:5006`, EXPO_PUBLIC_AUTH_ISSUER: `http://${address}:8080/realms/buildingflow` };
  for (const [key, value] of Object.entries(values)) { const regex = new RegExp(`^${key}=.*$`, 'm'); text = regex.test(text) ? text.replace(regex, `${key}=${value}`) : text.trimEnd() + `\n${key}=${value}\n`; }
  writeFileSync(path, text);
}
function occupied(port) { return new Promise(resolve => { const s = createConnection({host:'127.0.0.1',port}); s.once('connect',()=>{s.destroy();resolve(true);}); s.once('error',()=>resolve(false)); s.setTimeout(1000,()=>{s.destroy();resolve(false);}); }); }
function start(address) {
  configure(address); host = address;
  const env = {...process.env, BUILDINGFLOW_MOBILE_HOST:address, REACT_NATIVE_PACKAGER_HOSTNAME:address};
  console.log(`[BuildingFlow Mobil] Ağ adresi: ${address}. Telefonda Expo Go yerine BuildingFlow uygulamasını aç.`);
  const api = spawn('bash', ['scripts/mobile-api.sh'], {cwd:root, env, stdio:'inherit', detached:true});
  const metro = spawn(process.execPath, ['node_modules/expo/bin/cli','start','--dev-client','--lan','--port','8081'], {cwd:root, env, stdio:'inherit', detached:true});
  children = [api,metro];
  for (const child of children) { child.on('error',()=>{console.error('[BuildingFlow Mobil] Geliştirme servisi başlatılamadı.');void stop(1);}); child.on('exit',code=>{if(!changing&&!stopping){console.error(`[BuildingFlow Mobil] Servis durdu (${code ?? 'sinyal'}).`);void stop(1);}}); }
}
async function stopChildren() {
  const current = children; children = [];
  await Promise.all(current.map(child => new Promise(resolve => {
    if(child.exitCode !== null || child.signalCode !== null) return resolve();
    const timeout = setTimeout(()=>{try{process.kill(-child.pid,'SIGTERM');}catch{} resolve();},5000);
    child.once('exit',()=>{clearTimeout(timeout);resolve();});
    try { process.kill(-child.pid,'SIGINT'); } catch {clearTimeout(timeout);resolve();}
  })));
}
async function stop(code=0) {if(stopping)return;stopping=true;clearInterval(watcher);await stopChildren();process.exit(code);}
let watcher;
try {
  if (await occupied(8081) || await occupied(5006)) throw Error('8081 veya5006 zaten açık. Mevcut mobil geliştirme sürecini durdurup tekrar çalıştır.');
  start(lanAddress());
  watcher=setInterval(async()=>{if(changing||stopping)return;const next=lanAddress();if(!next||next===host)return;changing=true;console.log('[BuildingFlow Mobil] Ağ adresi değişti; mobil servisler güncelleniyor.');try{await stopChildren();start(next);}catch{console.error('[BuildingFlow Mobil] Ağ güncellemesi başarısız.');await stop(1);}finally{changing=false;}},10000);
  process.on('SIGINT',()=>void stop());process.on('SIGTERM',()=>void stop());
} catch(error) { console.error(`[BuildingFlow Mobil] ${error.message}`); process.exit(1); }
