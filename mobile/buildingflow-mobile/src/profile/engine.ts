import { parseProfile, parseReceipt, bytesBase64, MAX_PHOTO_BYTES, type Profile } from './contracts.ts';
import * as queue from './outbox.ts';
import type {SyncDb} from '../sync/outbox';
import type {PrivateFiles} from './files';
export type Ports={store:<T>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>)=>Promise<T>;request:(path:string,body?:unknown)=>Promise<Response>;active:()=>boolean;now:()=>number;digest:(bytes:Uint8Array)=>Promise<string>};
export class ProfileError extends Error {status:number;constructor(status:number,message:string){super(message);this.status=status;}}
const path='/api/v1/mobile/profile';
export async function synchronizeProfile(p:Ports,user:string):Promise<Profile> {
 const job=await p.store(db=>queue.claim(db,user,p.now()));
 if(job && p.active()) {
  let receipt:Profile|undefined;
  try {
   const command=await p.store((_,files)=>queue.loadCommand(files,job));
   const response=await p.request(path+'/commands',command);
   if(!p.active())throw Error('Hesap değişti.');
   if(response.status===403){await p.store((db,files)=>queue.purge(db,files,user));throw new ProfileError(403,'Profil erişimi kaldırıldı.');}
   if(response.status===409){await p.store(db=>queue.finish(db,job,'conflict',409,p.now()));throw new ProfileError(409,'Profil başka bir cihazda değişti. Merkezdeki bilgileri alıp yeniden düzenleyin.');}
   if(!response.ok){const status=response.status;await p.store(db=>queue.finish(db,job,(status===429||status>=500)&&job.attempts<6?'retry':'failed',status,p.now(),Math.max(Math.min(120000,Math.max(0,Number(response.headers.get('Retry-After'))||0)*1000),Math.min(60000,2000*2**Math.min(job.attempts,5)))));throw new ProfileError(status,'Profil gönderilemedi. Taslak korunuyor.');}
   try {receipt=parseReceipt(await response.json(),command,user);}catch{await p.store(db=>queue.finish(db,job,'failed',-1,p.now()));throw new ProfileError(-1,'Merkez makbuzu doğrulanamadı. Aynı işlem yeniden denenebilir.');}
  }catch(error){
   if(!(error instanceof ProfileError) && p.active())await p.store(db=>queue.finish(db,job,job.attempts>=6?'failed':'retry',0,p.now(),Math.min(60000,2000*2**Math.min(job.attempts,5))));
   throw error;
  }
  // Once acknowledged, a later photo-download failure must never resend the command.
  if(receipt && p.active())await p.store((db,files)=>queue.acknowledge(db,files,job,receipt!,p.now()));
 }
 if(!p.active())throw Error('Hesap değişti.');
 const response=await p.request(path);
 if(response.status===403){await p.store((db,files)=>queue.purge(db,files,user));throw new ProfileError(403,'Profil erişimi kaldırıldı.');}
 if(!response.ok)throw new ProfileError(response.status,'Profil merkezden alınamadı.');
 const profile=parseProfile(await response.json(),user);
 if(!p.active())throw Error('Hesap değişti.');
 const old=await p.store(db=>queue.cachedProfile(db,user));
 if(old && old.profile.version>profile.version)throw Error('Eski profil yanıtı reddedildi.');
 await p.store(db=>queue.cacheProfile(db,profile,p.now()));
 if(old?.photoFile && old.profile.photoId!==profile.photoId)await p.store((_,files)=>files.drop(old.photoFile!));
 if(profile.photoId && (!old?.photoFile || old.profile.photoId!==profile.photoId)) {
  const photo=await p.request(path+'/photo/'+profile.photoId);
  if(!photo.ok)throw new ProfileError(photo.status,'Profil kaydedildi; fotoğraf indirilemedi. Eşitlemeyi yeniden deneyin.');
  if(Number(photo.headers.get('Content-Length'))>MAX_PHOTO_BYTES)throw Error('Fotoğraf boyutu geçersiz.');
  const bytes=new Uint8Array(await photo.arrayBuffer());
  if(bytes.length>MAX_PHOTO_BYTES || !bytes.length || await p.digest(bytes)!==profile.photoSha256)throw Error('Fotoğraf bütünlüğü doğrulanamadı.');
  if(!p.active())throw Error('Hesap değişti.');
  const file='avatar-'+profile.photoId+'.enc';
  await p.store(async(db,files)=>{await files.write(file,bytesBase64(bytes));await queue.cacheProfile(db,profile,p.now(),file);});
 }
 return profile;
}
