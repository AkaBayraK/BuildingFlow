import {changed,subscribe} from '../contact-verification/events';
import {useEffect,useRef,useState} from 'react';
import {AppState,Platform} from 'react-native';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {parseCommand,parseProfile,parseReceipt,bytesBase64,MAX_PHOTO_BYTES,type Profile,type Command} from './contracts';
import * as queue from './outbox';
import {synchronizeProfile} from './engine';
import {publishAvatar,fullName} from './avatar';
import {useOptionalConsents} from '../consents/ConsentProvider';
export function useProfile(){
 const auth=useAuth(),user=auth.session?.userId,consents=useOptionalConsents(),consentRef=useRef(consents);useEffect(()=>{consentRef.current=consents;});
 const [profile,setProfile]=useState<Profile|null>(null),[photo,setPhoto]=useState<string|null>(null),[job,setJob]=useState<queue.Job|null>(null),[busy,setBusy]=useState(false),[message,setMessage]=useState(''),[visible,setVisible]=useState(true);
 const webDraft=useRef<Command|null>(null);
 const cache=useRef(auth.profileCache);
 const owner=useRef(user),lock=useRef(false),runner=useRef(async()=>{});
 const active=(id:string)=>owner.current===id;
 const digest=async(bytes:Uint8Array)=>Array.from(new Uint8Array(await Crypto.digest(Crypto.CryptoDigestAlgorithm.SHA256,new Uint8Array(bytes).buffer)),x=>x.toString(16).padStart(2,'0')).join('');
 const readLocal=async(id:string)=>{
  const value=await auth.profileCache(async(db,files)=>{const c=await queue.cachedProfile(db,id);return {c,job:await queue.currentJob(db,id),visible:await queue.showPhoto(db,id),photo:c?.photoFile?await files.read(c.photoFile):null};});
  if(active(id)){const uri=value.photo?'data:image/jpeg;base64,'+value.photo:null;setProfile(value.c?.profile??null);setJob(value.job);setVisible(value.visible);setPhoto(uri);publishAvatar(id,value.visible?uri:null,fullName(value.c?.profile));}
 };
 const webJob=useRef<queue.Job|null>(null);useEffect(()=>{webJob.current=job;});
 const refresh=async()=>{
  if(!user || lock.current)return;const id=user;lock.current=true;setBusy(true);
  try {
   if(Platform.OS!=='web')await readLocal(id);
   if(auth.offline)return;
   if(Platform.OS==='web'){
    const job=webJob.current;
    if(webDraft.current && job && ['pending','retry','sending'].includes(job.status)) {
     const command=webDraft.current;setJob({...job,status:'sending',attempts:job.attempts+1});
     try {
      const sent=await auth.profileRequest('/api/v1/mobile/profile/commands',command);
      if(!sent.ok){setJob({...job,attempts:job.attempts+1,status:sent.status===409?'conflict':'failed',error_code:sent.status});throw Error(sent.status===409?'Profil başka cihazda değişti. Merkezdeki bilgileri alıp yeniden düzenleyin.':'Profil gönderilemedi. Taslak bu sekmenin belleğinde korunuyor.');}
      let receipt:Profile;try{receipt=parseReceipt(await sent.json(),command,id);}catch{setJob({...job,status:'failed',error_code:-1});throw Error('Profil makbuzu doğrulanamadı. Aynı işlemi yeniden deneyin.');}
      if(!active(id))return;setProfile(receipt);webDraft.current=null;setJob(null);
     }catch(e){if(!(e instanceof Error && /Profil/.test(e.message)))setJob({...job,status:'failed',error_code:0,attempts:job.attempts+1});throw e;}
    }
    const r=await auth.profileRequest('/api/v1/mobile/profile');if(!r.ok)throw Error('Profil alınamadı.');const p=parseProfile(await r.json(),id);if(!active(id))return;setProfile(p);publishAvatar(id,null,fullName(p));setPhoto(null);
    if(p.photoId){const r=await auth.profileRequest('/api/v1/mobile/profile/photo/'+p.photoId);if(!r.ok)throw Error('Fotoğraf alınamadı.');const b=new Uint8Array(await r.arrayBuffer());if(b.length>MAX_PHOTO_BYTES||await digest(b)!==p.photoSha256)throw Error('Fotoğraf doğrulanamadı.');if(active(id)){const uri='data:image/jpeg;base64,'+bytesBase64(b);setPhoto(uri);publishAvatar(id,uri);}}
   }else{
    // İŞ18: a photo consent approved offline is sent before the profile command that needs it.
    try{await consentRef.current?.flush();}catch{/* a held photo waits for consent */}
    await synchronizeProfile({store:auth.profileCache,request:auth.profileRequest,active:()=>active(id),now:Date.now,digest},id);await readLocal(id);}
   if(active(id))setMessage('');
  }catch(e){if(active(id)){setMessage(e instanceof Error?e.message:'Profil eşitlenemedi.');if(Platform.OS!=='web')try{await readLocal(id);}catch{/* cache failure stays visible */}}}
  finally{lock.current=false;if(active(id))setBusy(false);}
 };
 useEffect(()=>{runner.current=refresh;cache.current=auth.profileCache;});
 useEffect(()=>{owner.current=user;return()=>{owner.current=undefined;};},[user]);
 useEffect(()=>{if(!user)return;void (async()=>{if(Platform.OS!=='web')try{await cache.current(db=>queue.recover(db,user,Date.now()));}catch{/* refresh reports storage failures */}await runner.current();})();const timer=setInterval(()=>{if(AppState.currentState==='active')void runner.current();},15000);const listener=AppState.addEventListener('change',s=>{if(s==='active')void runner.current();});return()=>{clearInterval(timer);listener.remove();};},[user,auth.offline]);
 const save=async(value:Omit<Command,'operationId'>)=>{
  if(!user||lock.current)throw Error('Profil işlemi sürüyor.');const id=user;const command=parseCommand({...value,operationId:Crypto.randomUUID()});
  lock.current=true;setBusy(true);
  try{
   if(Platform.OS==='web'){webDraft.current=command;webJob.current={operation_id:command.operationId,user_id:id,body_file:'memory-only',status:'pending',attempts:0,next_attempt_at:Date.now(),error_code:null};setJob(webJob.current);}
   else {await auth.profileCache((db,files)=>queue.enqueue(db,files,id,command,Date.now()));await readLocal(id);}
   if(active(id))setMessage(auth.offline?'Şifreli taslak kaydedildi. Bağlantı gelince merkeze gönderilecek.':'Profil merkeze gönderiliyor.');
  }finally{lock.current=false;if(active(id))setBusy(false);}
  void runner.current();
 };
 useEffect(()=>{if(profile?.userId===user&&user)changed(user,'CONTACT');},[profile?.version,profile?.userId,user]);
 useEffect(()=>subscribe(event=>{if(event.user===user&&event.kind==='PROFILE')void runner.current();}),[user]);
 const changeJob=async(retry:boolean)=>{if(!user||lock.current)return;
  if(Platform.OS==='web'){const current=webJob.current;if(!current || (retry?!queue.canRetry(current):!queue.canDiscard(current)))throw Error('İşlem değiştirilemez.');if(retry){webJob.current={...current,status:'retry'};setJob(webJob.current);}else{webDraft.current=null;webJob.current=null;setJob(null);}}
  else{await auth.profileCache((db,files)=>retry?queue.retry(db,user,Date.now()):queue.discard(db,files,user));await readLocal(user);}void runner.current();};
 const toggle=async(value:boolean)=>{if(!user)return;if(Platform.OS!=='web')await auth.profileCache(db=>queue.showPhoto(db,user,value));setVisible(value);publishAvatar(user,value?photo:null);};
 return {profile:profile?.userId===user?profile:null,photo,job,busy,message,visible,refresh,save,toggle,retry:()=>changeJob(true),discard:()=>changeJob(false)};
}
