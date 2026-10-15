import * as SQLite from 'expo-sqlite';
import * as SecureStore from 'expo-secure-store';
import * as Crypto from 'expo-crypto';
import {privateFiles,clearPrivateFiles} from '../profile/files.native';
import type {PrivateFiles} from '../profile/files';
import { CacheQueue } from './queue';
import type { Session } from '../auth/contracts';
import { migrate, readSnapshot, writeSelection, writeSnapshot, type Snapshot } from './database';
const pointerKey = 'buildingflow.cache.pointer.v1';
const options = { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY };
type Pointer = { userId: string; name: string; keyId: string; scope: string };
const queue = new CacheQueue();
async function pointer(): Promise<Pointer | null> {
  const raw = await SecureStore.getItemAsync(pointerKey);
  if (!raw) return null;
  const p = JSON.parse(raw) as Pointer;
  if (typeof p.scope !== 'string' || !p.scope || typeof p.userId !== 'string' || !p.userId || !/^bf-[0-9a-f-]{36}\.db$/.test(p.name) || !/^buildingflow\.cache\.key\.[0-9a-f-]{36}$/.test(p.keyId)) throw new Error('Yerel depo bilgisi geçersiz.');
  return p;
}
async function open(p: Pointer) {
  const key = await SecureStore.getItemAsync(p.keyId);
  if (!key || !/^[0-9a-f]{64}$/.test(key)) throw new Error('Yerel veri anahtarı bulunamadı.');
  const db = await SQLite.openDatabaseAsync(p.name, { useNewConnection: true });
  try {
    // Only generated hex is interpolated; user data is always bound.
    await db.execAsync(`PRAGMA key = '${key}';`);
    const cipher = await db.getFirstAsync<{ cipher_version: string }>('PRAGMA cipher_version');
    if (!cipher?.cipher_version) throw new Error('Şifreli SQLite build gerekli.');
    await db.execAsync('PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA secure_delete=ON;');
    await migrate(db);
    return db;
  } catch (error) { await db.closeAsync(); throw error; }
}
async function destroy() {
  const p = await pointer();
  if (p) {
    // Remove the encryption key before removing the locator or files.
    await SecureStore.deleteItemAsync(p.keyId);
    await SecureStore.deleteItemAsync(pointerKey);
    try { clearPrivateFiles(p.name); } catch { /* Remaining ciphertext has no key. */ }
    try { await SQLite.deleteDatabaseAsync(p.name); } catch { /* Any remaining file is encrypted with a destroyed key. */ }
  }
}
export function clearCache(): Promise<void> { return queue.clear(destroy); }
export function saveCache(session: Session, verifiedAt: number, selectedKey: string | null, scope: string): Promise<boolean> {
  return queue.write(async () => {
    let p = await pointer();
    if (p && (p.userId !== session.userId || p.scope !== scope)) { await destroy(); p = null; }
    if (!p) {
      const uuid = Crypto.randomUUID();
      p = { scope, userId: session.userId, name: `bf-${uuid}.db`, keyId: `buildingflow.cache.key.${uuid}` };
      const key = Array.from(Crypto.getRandomBytes(32), b => b.toString(16).padStart(2, '0')).join('');
      await SecureStore.setItemAsync(p.keyId, key, options);
      try { await SecureStore.setItemAsync(pointerKey, JSON.stringify(p), options); }
      catch (error) { await SecureStore.deleteItemAsync(p.keyId); throw error; }
    }
    const db = await open(p);
    try { await writeSnapshot(db, session, verifiedAt, Crypto.randomUUID(), selectedKey); }
    finally { await db.closeAsync(); }
    return true;
  }, false);
}
export function loadCache(scope: string): Promise<Snapshot | null> {
  return queue.run(async () => { const p = await pointer(); if (!p || p.scope !== scope) return null; const db = await open(p); try { return await readSnapshot(db, p.userId, Date.now()); } finally { await db.closeAsync(); } });
}
export function saveSelection(key: string, scope: string): Promise<void> {
  return queue.write(async () => {
    const p = await pointer(); if (!p || p.scope !== scope) return;
    const db = await open(p);
    try { const snapshot = await readSnapshot(db, p.userId, Date.now()); if (snapshot) await writeSelection(db, snapshot, key); }
    finally { await db.closeAsync(); }
  }, undefined);
}

function withAccountCache<T>(scope:string,userId:string,task:(db:SQLite.SQLiteDatabase,p:Pointer)=>Promise<T>,active:()=>boolean):Promise<T> {
 return queue.run(async()=>{
  if(!active())throw Error('İşlem iptal edildi.');
  const p=await pointer();if(!p || p.scope!==scope || p.userId!==userId)throw Error('Şifreli yerel depo hazır değil.');
  const db=await open(p);
  try{if(!active())throw Error('İşlem iptal edildi.');return await task(db,p);}finally{await db.closeAsync();}
 });
}
export function withSyncCache<T>(scope:string,userId:string,task:(db:SQLite.SQLiteDatabase)=>Promise<T>,active:()=>boolean):Promise<T> {return withAccountCache(scope,userId,(db)=>task(db),active);}
export function withProfileCache<T>(scope:string,userId:string,task:(db:SQLite.SQLiteDatabase,files:PrivateFiles)=>Promise<T>,active:()=>boolean):Promise<T> {return withAccountCache(scope,userId,(db,p)=>task(db,privateFiles(p)),active);}
