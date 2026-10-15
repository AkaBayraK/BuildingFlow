import type {SyncDb} from '../sync/outbox';
import type {PrivateFiles} from './files';
import {parseCommand,parseProfile,type Command,type Profile} from './contracts.ts';
export type Job={operation_id:string;user_id:string;body_file:string;status:'pending'|'sending'|'retry'|'conflict'|'failed';attempts:number;next_attempt_at:number;error_code:number|null};
export async function currentJob(db:SyncDb,user:string){return db.getFirstAsync<Job>('SELECT * FROM profile_jobs WHERE user_id=? LIMIT 1',user);}
export async function enqueue(db:SyncDb,files:PrivateFiles,user:string,value:Command,now:number) {
 const command=parseCommand(value);if(await currentJob(db,user))throw Error('Bekleyen profil işlemini önce tamamlayın.');
 const owner=await db.getFirstAsync<{user_id:string}>('SELECT user_id FROM account LIMIT 1');if(owner?.user_id!==user)throw Error('Profil hesabı eşleşmiyor.');
 const file='job-'+command.operationId+'.enc';await files.write(file,JSON.stringify(command));
 try {await db.runAsync("INSERT INTO profile_jobs(operation_id,user_id,body_file,status,attempts,next_attempt_at) VALUES (?,?,?,'pending',0,?)",command.operationId,user,file,now);}
 catch(error){await files.drop(file);throw error;}
}
export async function loadCommand(files:PrivateFiles,job:Job):Promise<Command>{const c=parseCommand(JSON.parse(await files.read(job.body_file)));if(c.operationId!==job.operation_id)throw Error('Taslak kimliği eşleşmiyor.');return c;}
export async function recover(db:SyncDb,user:string,now:number){await db.runAsync("UPDATE profile_jobs SET status='retry',next_attempt_at=? WHERE user_id=? AND status='sending'",now,user);}
export async function claim(db:SyncDb,user:string,now:number){return db.getFirstAsync<Job>("UPDATE profile_jobs SET status='sending',attempts=attempts+1 WHERE operation_id=(SELECT operation_id FROM profile_jobs WHERE user_id=? AND status IN ('pending','retry') AND next_attempt_at<=? LIMIT 1) RETURNING *",user,now);}
export async function finish(db:SyncDb,job:Job,status:Job['status'],error:number,now:number,delay=0){await db.runAsync("UPDATE profile_jobs SET status=?,error_code=?,next_attempt_at=? WHERE user_id=? AND operation_id=? AND status='sending'",status,error,now+delay,job.user_id,job.operation_id);}
export async function acknowledge(db:SyncDb,files:PrivateFiles,job:Job,profile:Profile,now:number){
 const old=await cachedProfile(db,job.user_id);
 await db.execAsync('BEGIN IMMEDIATE');try{await cacheProfile(db,profile,now);await db.runAsync("DELETE FROM profile_jobs WHERE user_id=? AND operation_id=? AND status='sending'",job.user_id,job.operation_id);await db.execAsync('COMMIT');}catch(error){await db.execAsync('ROLLBACK');throw error;}await files.drop(job.body_file);if(old?.photoFile && old.profile.photoId!==profile.photoId)await files.drop(old.photoFile);
}
export async function cacheProfile(db:SyncDb,p:Profile,now:number,photoFile?:string|null) {
 const old=await cachedProfile(db,p.userId);if(old && old.profile.version>p.version)return;
 await db.runAsync('INSERT INTO profile_cache(user_id,payload,checked_at,photo_file) VALUES (?,?,?,?) ON CONFLICT(user_id) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at,photo_file=excluded.photo_file',p.userId,JSON.stringify(p),now,photoFile===undefined?(old?.profile.photoId===p.photoId?old.photoFile:null):photoFile);
}
export async function cachedProfile(db:SyncDb,user:string){const r=await db.getFirstAsync<{payload:string;checked_at:number;photo_file:string|null}>('SELECT * FROM profile_cache WHERE user_id=?',user);return r?{profile:parseProfile(JSON.parse(r.payload),user),checkedAt:r.checked_at,photoFile:r.photo_file}:null;}
export function canRetry(job:Job){return job.status==='retry' || job.status==='failed'&&(job.error_code===0 || job.error_code===429 || job.error_code===-1 || (job.error_code??0)>=500);}
export function canDiscard(job:Job){return job.status==='pending'&&job.attempts===0 || job.status==='conflict' || job.status==='failed'&&[400,413,415,422].includes(job.error_code??0);}
export async function retry(db:SyncDb,user:string,now:number){const j=await currentJob(db,user);if(!j || !canRetry(j))throw Error('Bu işlem yeniden gönderilemez.');await db.runAsync("UPDATE profile_jobs SET status='retry',next_attempt_at=? WHERE user_id=? AND operation_id=?",now,user,j.operation_id);}
export async function discard(db:SyncDb,files:PrivateFiles,user:string){const j=await currentJob(db,user);if(!j || !canDiscard(j))throw Error('Gönderimi belirsiz işlem iptal edilemez.');await db.runAsync('DELETE FROM profile_jobs WHERE user_id=? AND operation_id=?',user,j.operation_id);await files.drop(j.body_file);}
export async function purge(db:SyncDb,files:PrivateFiles,user:string){const c=await cachedProfile(db,user),j=await currentJob(db,user);await db.runAsync('DELETE FROM profile_cache WHERE user_id=?',user);await db.runAsync('DELETE FROM profile_jobs WHERE user_id=?',user);if(c?.photoFile)await files.drop(c.photoFile);if(j)await files.drop(j.body_file);}
export async function showPhoto(db:SyncDb,user:string,value?:boolean){if(value!==undefined)await db.runAsync('INSERT INTO profile_settings(user_id,show_photo) VALUES (?,?) ON CONFLICT(user_id) DO UPDATE SET show_photo=excluded.show_photo',user,value?1:0);return(await db.getFirstAsync<{show_photo:number}>('SELECT show_photo FROM profile_settings WHERE user_id=?',user))?.show_photo!==0;}
