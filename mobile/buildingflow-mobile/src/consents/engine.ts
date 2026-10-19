import type {SyncDb} from '../sync/outbox';
import type {PrivateFiles} from '../profile/files';
import {parseList,parseResult,textOutdatedCode,wire,type ConsentList,type Command,type Feature} from './contracts.ts';
import * as store from './store.ts';
export type Reply={status:number;json:()=>Promise<unknown>;retryAfter?:number};
export type Ports={store:<T>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>)=>Promise<T>;request:(path:string,body?:unknown)=>Promise<Reply>;now:()=>number;active:()=>boolean;digest:(text:string)=>Promise<string>};
export class ConsentError extends Error{status:number;code:string|null;constructor(status:number,message:string,code:string|null=null){super(message);this.status=status;this.code=code;}}
const path='/api/v1/mobile/consents';
async function code(reply:Reply){try{const body=await reply.json() as {code?:unknown};return typeof body?.code==='string'?body.code:null;}catch{return null;}}
const backoff=(attempts:number,retryAfter?:number)=>Math.min(120000,Math.max(retryAfter??0,2000*2**Math.min(attempts,5)));
function stop(p:Ports){if(!p.active())throw new ConsentError(0,'Hesap değişti.');}

// Reads the current texts and states; a body whose SHA-256 differs from the published hash is never stored or shown for approval.
export async function verifyList(p:Pick<Ports,'digest'>,list:ConsentList){
 for(const i of list.items)if(i.currentText&&await p.digest(i.currentText.body)!==i.currentText.bodySha256)throw new ConsentError(-1,'Rıza metni bütünlüğü doğrulanamadı. Metin onaya sunulmadı.');
 return list;
}
export async function refreshConsents(p:Ports,user:string):Promise<ConsentList>{
 stop(p);const reply=await p.request(path);stop(p);
 if(reply.status!==200)throw new ConsentError(reply.status,reply.status===401?'Oturum sona erdi.':'Rıza bilgileri merkezden alınamadı.');
 let list:ConsentList;
 try{list=parseList(await reply.json(),user);}catch{throw new ConsentError(-1,'Rıza yanıtı doğrulanamadı.');}
 await verifyList(p,list);stop(p);
 await p.store(db=>store.cacheList(db,list,p.now()));
 return list;
}
export type Flush={sent:number;released:Feature[];refreshed:boolean;outdated:boolean};
// Sends queued decisions one at a time in creation order. Callers run this before profile and request photo commands.
export async function flushConsents(p:Ports,user:string):Promise<Flush>{
 const result:Flush={sent:0,released:[],refreshed:false,outdated:false};let reload=false;
 for(let n=0;n<10;n++){
  stop(p);const job=await p.store(db=>store.claim(db,user,p.now()));if(!job)break;
  let command:Command;
  try{command=store.loadCommand(job);}catch{await p.store(db=>store.finish(db,job,'failed','INVALID',p.now()));continue;}
  let reply:Reply;
  try{reply=await p.request(path+'/commands',wire(command));}
  catch{if(p.active())await p.store(db=>store.finish(db,job,job.attempts>=6?'failed':'retry','NETWORK',p.now(),backoff(job.attempts)));break;}
  if(!p.active()){await p.store(db=>store.finish(db,job,'retry',null,p.now()));throw new ConsentError(0,'Hesap değişti.');}
  if(reply.status===200){
   let receipt;
   try{receipt=parseResult(await reply.json(),command);}catch{await p.store(db=>store.finish(db,job,'failed','RECEIPT',p.now()));continue;}
   const feature=receipt.status.feature;
   const applied=await p.store(async(db,files)=>{
    await db.execAsync('BEGIN IMMEDIATE');
    let ok:boolean;
    try{
     ok=await store.applyState(db,user,receipt.status);await store.remove(db,job);
     if(feature&&command.action==='APPROVE'&&receipt.status.state==='ACTIVE')await store.releaseHeld(db,user,feature,p.now());
     await db.execAsync('COMMIT');
    }catch(error){await db.execAsync('ROLLBACK');throw error;}
    if(feature&&command.action==='WITHDRAW')await store.dropLocalPhotos(db,files,user,feature);
    return ok;
   });
   result.sent++;if(feature&&command.action==='APPROVE'&&receipt.status.state==='ACTIVE')result.released.push(feature);if(!applied)reload=true;
   continue;
  }
  if(reply.status===401){await p.store(db=>store.finish(db,job,'retry',null,p.now()));throw new ConsentError(401,'Oturum sona erdi.');}
  if(reply.status===409){
   const error=await code(reply);
   // The text changed after it was read: keep the decision visible as a conflict and ask the person again with the new text.
   await p.store(db=>store.finish(db,job,error===textOutdatedCode?'conflict':'failed',error??'HTTP_409',p.now()));
   if(error===textOutdatedCode){reload=true;result.outdated=true;}
   continue;
  }
  if(reply.status===429||reply.status>=500){await p.store(db=>store.finish(db,job,job.attempts>=6?'failed':'retry','HTTP_'+reply.status,p.now(),backoff(job.attempts,reply.retryAfter)));break;}
  const error=(await code(reply))??'HTTP_'+reply.status;
  await p.store(db=>store.finish(db,job,'failed',error,p.now()));
 }
 if(reload){try{await refreshConsents(p,user);result.refreshed=true;}catch(error){if(error instanceof ConsentError&&error.status===401)throw error;}}
 return result;
}
