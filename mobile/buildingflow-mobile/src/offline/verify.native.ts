// Opt-in development verification. Uses isolated disposable fixture files and keys only.
import * as SQLite from 'expo-sqlite';
import * as SecureStore from 'expo-secure-store';
import {File,Directory,Paths} from 'expo-file-system';
import {privateFiles,clearPrivateFiles} from '../profile/files.native';
import * as Crypto from 'expo-crypto';
import { enqueue, jobs } from '../sync/outbox';
import { synchronize } from '../sync/engine';
import { migrate, writeSnapshot, readSnapshot, MAX_CACHE_AGE_MS } from './database';
let result: Promise<Record<string, boolean>> | null = null;
export function verifyNativeCache() {
  if (!__DEV__) return Promise.resolve(null);
  return result ??= verify();
}
async function verify(): Promise<Record<string, boolean>> {
  const id = Crypto.randomUUID(); const name = `bf-check-${id}.db`; const keyId = `buildingflow.check.${id}`;
  const key = Array.from(Crypto.getRandomBytes(32), b => b.toString(16).padStart(2, '0')).join('');
  let db: SQLite.SQLiteDatabase | null = null;
  try {
    await SecureStore.setItemAsync(keyId, key, { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY });
    if (await SecureStore.getItemAsync(keyId) !== key) throw Error('SecureStore fixture');
    db = await SQLite.openDatabaseAsync(name, { useNewConnection: true });
    await db.execAsync(`PRAGMA key='${key}'; PRAGMA foreign_keys=ON;`);
    const cipher = await db.getFirstAsync<{ cipher_version: string }>('PRAGMA cipher_version');
    if (!cipher?.cipher_version) throw Error('SQLCipher unavailable');
    await migrate(db);
    const now = Date.now();
    const context = { propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Fixture',unitId:null,unitNumber:null,role:'MANAGER' as const };
    const session = { userId: 'phase05-fixture', username: 'Fixture', hasMore: false, contexts: [context] };
    await writeSnapshot(db, session, now, Crypto.randomUUID(), null); await migrate(db);
    if ((await readSnapshot(db, session.userId, now))?.session.username !== 'Fixture') throw Error('Read fixture');
    if (await readSnapshot(db, 'other-fixture', now)) throw Error('Account isolation fixture');
    if (await readSnapshot(db, session.userId, now + MAX_CACHE_AGE_MS + 1)) throw Error('Expiry fixture');
    const keyScope=context.propertyId+':-:MANAGER';
    const command={operationId:Crypto.randomUUID(),recordId:Crypto.randomUUID(),propertyId:context.propertyId,unitId:null,role:'MANAGER',action:'CREATE' as const,expectedVersion:0,description:'Fixture'};
    await enqueue(db,session,keyScope,command,now);
    const record={id:command.recordId,propertyId:context.propertyId,unitId:null,role:'MANAGER',description:'Fixture',version:1,deleted:false,modifiedAt:new Date(now).toISOString()};
    let committed=false;let creates=0;let clock=now;
    const local=db;
    const ports={db:<T>(task:(value:SQLite.SQLiteDatabase)=>Promise<T>)=>task(local),now:()=>clock,active:()=>true,
      request:async(path:string)=>{
        if(path==='/api/v1/mobile/session')return {status:200,json:async()=>session};
        if(path.includes('/requests?'))return {status:200,json:async()=>({userId:session.userId,contextKey:keyScope,hasMore:false,records:committed?[record]:[]})};
        if(!committed){committed=true;creates++;throw Error('Lost fixture response');}
        return {status:200,json:async()=>({operationId:command.operationId,record,replayed:true})};
      }};
    await synchronize(ports,session,context);clock=now+4000;await synchronize(ports,session,context);
    if(creates!==1 || (await jobs(db,session.userId,keyScope))[0]?.status!=='sent')throw Error('Native queue replay fixture');
    const privateName='job-'+id+'.enc';const files=privateFiles({name,keyId});
    await files.write(privateName,'private-profile-fixture');
    if(await files.read(privateName)!=='private-profile-fixture')throw Error('AES profile round-trip fixture');
    const raw=await new File(new Directory(Paths.document,'profile-'+name),privateName).bytes();
    if(Array.from(raw).map(x=>String.fromCharCode(x)).join('').includes('private-profile-fixture'))throw Error('Plaintext profile file fixture');
    const otherName='job-'+Crypto.randomUUID()+'.enc';
    const copied=new File(new Directory(Paths.document,'profile-'+name),otherName);copied.create();copied.write(raw);
    let aadRefused=false;try{await files.read(otherName);}catch{aadRefused=true;}
    if(!aadRefused)throw Error('AES filename binding fixture');
    await SecureStore.setItemAsync(keyId,'00'.repeat(32),{keychainAccessible:SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY});
    let keyRefused=false;try{await files.read(privateName);}catch{keyRefused=true;}
    if(!keyRefused)throw Error('AES wrong-key fixture');
    await SecureStore.setItemAsync(keyId,key,{keychainAccessible:SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY});
    clearPrivateFiles(name);
    await db.closeAsync(); db = null;
    db = await SQLite.openDatabaseAsync(name, { useNewConnection: true });
    await db.execAsync("PRAGMA key='wrong-fixture-key';");
    let refused = false;
    try { await db.getFirstAsync('SELECT * FROM account'); } catch { refused = true; }
    if (!refused) throw Error('Wrong-key fixture');
    await db.closeAsync(); db = null;
    await SecureStore.deleteItemAsync(keyId);
    if (await SecureStore.getItemAsync(keyId) !== null) throw Error('Key cleanup fixture');
    await SQLite.deleteDatabaseAsync(name);
    return { sqlCipher: true, secureStore: true, migrations: true, accountIsolation: true, expires: true, wrongKeyRejected: true, nativeQueueReplay: true, profileFileEncryption: true, profileFileBinding: true, profileWrongKeyRejected: true, cleanup: true };
  } finally { clearPrivateFiles(name); if (db) await db.closeAsync(); await SecureStore.deleteItemAsync(keyId); await SQLite.deleteDatabaseAsync(name).catch(() => undefined); }
}
