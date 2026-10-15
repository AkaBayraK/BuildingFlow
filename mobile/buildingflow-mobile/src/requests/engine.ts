import {contextKey,type Context,type Session} from '../auth/contracts.ts';
import type {SyncDb} from '../sync/outbox.ts';
import type {PrivateFiles} from '../profile/files';
import {parsePhotoReceipt,type PhotoCommand} from './contracts.ts';
import * as q from './outbox.ts';
export type PhotoPorts={store:<T>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>)=>Promise<T>;request:(path:string,body:PhotoCommand)=>Promise<{status:number;json:()=>Promise<unknown>;retryAfter?:number}>;now:()=>number;active:()=>boolean};
export class PhotoSyncError extends Error {status:number;constructor(status:number){super('Fotoğraf eşitlemesi tamamlanamadı.');this.status=status;}}
const backoff=(attempts:number)=>Math.min(60000,1000*2**Math.min(attempts,6));
export async function synchronizePhotos(p:PhotoPorts,session:Session,context:Context){
 if(context.role==='ACCOUNTANT')return;
 const key=contextKey(context),user=session.userId,active=()=>{if(!p.active())throw new PhotoSyncError(0);};
 active();
 await p.store((db,files)=>q.purgeUnauthorized(db,files,session));
 await p.store((db,files)=>q.reconcile(db,files,user,key));
 await p.store(db=>q.recover(db,user,key,p.now()));
 for(let n=0;n<5;n++){
  active();const job=await p.store(db=>q.claim(db,user,key,p.now()));if(!job)break;
  let body:PhotoCommand;
  try{body={operationId:job.operation_id,propertyId:context.propertyId,unitId:context.unitId,role:context.role,photoBase64:await p.store((_,files)=>files.read(job.body_file))};}
  catch{await p.store(db=>q.finish(db,job,'failed',p.now(),-2));continue;}
  let response;
  try{response=await p.request('/api/v1/mobile/sync/requests/'+job.record_id+'/photos',body);}
  catch{active();await p.store(db=>q.finish(db,job,job.attempts>=6?'failed':'retry',p.now(),0,backoff(job.attempts)));break;}
  active();
  if(response.status===200){
   try{parsePhotoReceipt(await response.json(),job.operation_id,job.record_id);}catch{await p.store(db=>q.finish(db,job,'failed',p.now(),-1));continue;}
   active();await p.store((db,files)=>q.complete(db,files,job));
  }
  else if(response.status===401){await p.store(db=>q.finish(db,job,'retry',p.now(),401));throw new PhotoSyncError(401);}
  else if(response.status===409)await p.store(db=>q.finish(db,job,'conflict',p.now(),409));
  else if(response.status===403 || response.status===404)await p.store(db=>q.finish(db,job,'blocked',p.now(),response.status));
  else if(response.status===429 || response.status>=500){await p.store(db=>q.finish(db,job,job.attempts>=6?'failed':'retry',p.now(),response.status,Math.min(120000,Math.max(1000,response.retryAfter??backoff(job.attempts)))));break;}
  else await p.store(db=>q.finish(db,job,'failed',p.now(),response.status));
 }
}
