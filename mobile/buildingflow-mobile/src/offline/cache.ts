import type {PrivateFiles} from '../profile/files';
import type { SyncDb } from '../sync/outbox';
// Browser preview intentionally has no persistent sensitive cache.
import type { Session } from '../auth/contracts';
import type { Snapshot } from './database';
export async function saveCache(_session: Session, _verifiedAt: number, _selectedKey: string | null, _scope: string): Promise<boolean> { return false; }
export async function loadCache(_scope: string): Promise<Snapshot | null> { return null; }
export async function clearCache(): Promise<void> {}
export async function saveSelection(_key: string, _scope: string): Promise<void> {}

export async function withSyncCache<T>(_scope:string,_userId:string,_task:(db:SyncDb)=>Promise<T>,_active:()=>boolean):Promise<T> { throw Error('Kalıcı eşitleme yalnız mobil development build içinde kullanılabilir.'); }

export async function withProfileCache<T>(_scope:string,_userId:string,_task:(db:SyncDb,files:PrivateFiles)=>Promise<T>,_active:()=>boolean):Promise<T>{throw Error("Tarayıcı profil taslağını yalnız bellekte tutar.");}
