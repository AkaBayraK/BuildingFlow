import {createContext,useCallback,useContext,useEffect,useMemo,useRef,useState,type ReactNode} from 'react';
import {AppState,Platform} from 'react-native';
import Constants from 'expo-constants';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {publishAvatar} from '../profile/avatar';
import {parseCommand,parseList,parseResult,wire,type Action,type ConsentList,type Feature} from './contracts';
import {flushConsents,refreshConsents,verifyList,ConsentError,type Ports,type Reply} from './engine';
import * as store from './store';
import {consentChanged,digestText,onConsentChange} from './events';
import {ConsentSheet} from './ConsentSheet';
type Held=Record<Feature,number>;
type Value={items:store.Effective[];loaded:boolean;busy:boolean;message:string;checkedAt:number|null;held:Held;
 refresh:()=>Promise<store.Effective[]>;flush:()=>Promise<void>;decide:(entry:store.Effective,action:Action)=>Promise<void>;
 retry:(operationId:string)=>Promise<void>;discard:(operationId:string)=>Promise<void>};
const Context=createContext<Value|null>(null);
const noHeld:Held={PROFILE_PHOTO:0,REQUEST_PHOTO:0};
const appVersion=()=>{const v=Constants.expoConfig?.version;return typeof v==='string'&&v.length<=40?v:null;};
// İŞ18: one place that keeps consent texts, queued decisions and the launch gate for the signed-in account.
export function ConsentProvider({children}:{children:ReactNode}){
 const auth=useAuth(),user=auth.session?.userId??null,web=Platform.OS==='web';
 const [list,setList]=useState<ConsentList|null>(null),[queue,setQueue]=useState<store.Job[]>([]),[seen,setSeen]=useState<Set<string>>(new Set()),[held,setHeld]=useState<Held>(noHeld);
 const [checkedAt,setCheckedAt]=useState<number|null>(null),[loaded,setLoaded]=useState(false),[busy,setBusy]=useState(false),[message,setMessage]=useState(''),[gateBusy,setGateBusy]=useState(false),[gateError,setGateError]=useState('');
 const ref=useRef(auth);useEffect(()=>{ref.current=auth;});
 const owner=useRef(user);useEffect(()=>{owner.current=user;},[user]);
 const ports=useCallback((id:string):Ports=>({
  store:task=>ref.current.profileCache(task),
  request:async(path,body)=>{const r=await ref.current.profileRequest(path,body);const seconds=Number(r.headers.get('Retry-After'));return {status:r.status,json:()=>r.json(),retryAfter:Number.isFinite(seconds)&&seconds>0?seconds*1000:undefined} satisfies Reply;},
  now:Date.now,active:()=>owner.current===id,digest:digestText}),[]);
 // Reads the encrypted local copy; the browser preview keeps consent data only in memory.
 const load=useCallback(async(id:string)=>{
  if(web)return;
  const value=await ref.current.profileCache(async db=>({cached:await store.cachedList(db,id),jobs:await store.jobs(db,id),seen:await store.seen(db,id),held:{PROFILE_PHOTO:await store.heldCount(db,id,'PROFILE_PHOTO'),REQUEST_PHOTO:await store.heldCount(db,id,'REQUEST_PHOTO')}}));
  if(owner.current!==id)return null;
  setList(value.cached?.list??null);setCheckedAt(value.cached?.checkedAt??null);setQueue(value.jobs);setSeen(value.seen);setHeld(value.held);setLoaded(true);
  return store.effective(value.cached?.list??null,value.jobs);
 },[web]);
 const lock=useRef<Promise<unknown>|null>(null),recovered=useRef<string|null>(null);
 const flush=useCallback(async()=>{
  if(!user||web||ref.current.offline)return;const id=user;
  // One flush at a time from the screens; AuthProvider's request sync takes the same oldest-first queue.
  while(lock.current)await lock.current.catch(()=>{});
  const run=flushConsents(ports(id),id);lock.current=run;
  try{const result=await run;if(result.released.includes('REQUEST_PHOTO'))void ref.current.synchronizeNow();if(result.sent||result.refreshed)consentChanged();}
  finally{if(lock.current===run)lock.current=null;}
 },[user,web,ports]);
 const refresh=useCallback(async():Promise<store.Effective[]>=>{
  if(!user)return [];const id=user;setBusy(true);
  try{
   if(web){
    if(ref.current.offline)return store.effective(list,[]);
    const r=await ref.current.profileRequest('/api/v1/mobile/consents');if(!r.ok)throw new ConsentError(r.status,'Rıza bilgileri merkezden alınamadı.');
    const fresh=await verifyList({digest:digestText},parseList(await r.json(),id));
    if(owner.current!==id)return [];setList(fresh);setCheckedAt(Date.now());setLoaded(true);setMessage('');return store.effective(fresh,[]);
   }
   if(recovered.current!==id){try{await ref.current.profileCache(db=>store.recover(db,id,Date.now()));recovered.current=id;}catch{/* load reports storage problems */}}
   if(!ref.current.offline){
    try{await flush();await refreshConsents(ports(id),id);setMessage('');}
    catch(error){if(owner.current===id)setMessage(error instanceof ConsentError&&error.status===-1?error.message:'Rıza bilgileri merkezden alınamadı. Cihazdaki kopya gösteriliyor.');}
   }
   return (await load(id))??[];
  }catch(error){if(owner.current===id){setLoaded(true);setMessage(error instanceof Error?error.message:'Rıza bilgileri okunamadı.');}return [];}
  finally{if(owner.current===id)setBusy(false);}
 },[user,web,list,flush,ports,load]);
 const refreshRef=useRef(refresh);useEffect(()=>{refreshRef.current=refresh;});
 useEffect(()=>{
  // The provider only lives while signed in; logout unmounts it, so an account change always starts from empty state.
  if(!user)return;
  const listener=AppState.addEventListener('change',s=>{if(s==='active')void refreshRef.current();});
  const off=onConsentChange(()=>{void load(user).catch(()=>{});});
  return()=>{listener.remove();off();};
 },[user,load]);
 // First read for the account, and again whenever the connection comes back so queued decisions are sent.
 useEffect(()=>{if(user)void refreshRef.current();},[user,auth.offline]);
 const decide=useCallback(async(entry:store.Effective,action:Action)=>{
  if(!user)throw Error('Oturum gerekli.');const id=user,text=entry.item.currentText,feature=entry.item.status.feature;
  if(action==='APPROVE'&&!text)throw Error('Onaylanacak metin yok.');
  const command=parseCommand({operationId:Crypto.randomUUID(),action,typeCode:entry.item.status.typeCode,textId:action==='APPROVE'?text!.id:null,bodySha256:action==='APPROVE'?text!.bodySha256:null,appVersion:appVersion()});
  if(web){
   if(ref.current.offline)throw Error('Tarayıcı önizlemesinde rıza yalnız çevrimiçi kaydedilir.');
   const r=await ref.current.profileRequest('/api/v1/mobile/consents/commands',wire(command));
   if(!r.ok){let code:string|null=null;try{code=((await r.json()) as {code?:string}).code??null;}catch{/* no body */}await refresh();throw Error(code==='BF_CONSENT_TEXT_OUTDATED'?'Metin güncellendi. Yeni sürümü okuyup yeniden onaylayın.':'Rıza kaydedilemedi.');}
   const result=parseResult(await r.json(),command);
   setList(current=>current&&{...current,items:current.items.map(i=>i.status.typeCode===result.status.typeCode&&i.status.currentTextId===result.status.currentTextId?{...i,status:result.status}:i)});
   if(action==='WITHDRAW'&&feature==='PROFILE_PHOTO')publishAvatar(id,null);
   return;
  }
  await ref.current.profileCache(async(db,files)=>{await store.enqueue(db,id,command,Date.now());if(action==='WITHDRAW'&&feature)await store.dropLocalPhotos(db,files,id,feature);});
  if(action==='WITHDRAW'&&feature==='PROFILE_PHOTO')publishAvatar(id,null);
  await load(id);
  if(!ref.current.offline)try{await flush();}catch{/* stays queued and is retried */}
  await load(id);
 },[user,web,refresh,flush,load]);
 const change=useCallback(async(operation:string,retry:boolean)=>{
  if(!user||web)return;const id=user;
  await ref.current.profileCache(db=>retry?store.retry(db,id,operation,Date.now()):store.discard(db,id,operation));
  await load(id);if(retry)void flush().catch(()=>{});
 },[user,web,load,flush]);
 const items=useMemo(()=>store.effective(list,queue),[list,queue]);
 // Launch gate: revised or unread notices and revised consents, one at a time.
 const gate=loaded?store.attention(items,seen)[0]??null:null;
 const close=async(entry:store.Effective,action:Action|null)=>{
  if(!user)return;const id=user,text=entry.item.currentText;setGateBusy(true);setGateError('');
  try{
   if(action)await decide(entry,action);
   else if(entry.problem&&store.canDiscard(entry.problem)&&!web)await ref.current.profileCache(db=>store.discard(db,id,entry.problem!.operation_id));
   if(text){if(web)setSeen(s=>new Set(s).add(text.id));else{await ref.current.profileCache(db=>store.markSeen(db,id,text.id,Date.now()));await load(id);}}
  }catch(error){setGateError(error instanceof Error?error.message:'Kaydedilemedi.');}
  finally{setGateBusy(false);}
 };
 const value:Value={items,loaded,busy,message,checkedAt,held,refresh,flush,decide,retry:op=>change(op,true),discard:op=>change(op,false)};
 return <Context.Provider value={value}>{children}{gate&&<ConsentSheet key={gate.item.currentText!.id} entry={gate} mode="gate" offline={auth.offline} busy={gateBusy} error={gateError} onApprove={()=>close(gate,'APPROVE')} onWithdraw={()=>close(gate,'WITHDRAW')} onClose={()=>void close(gate,null)}/>}</Context.Provider>;
}
export function useOptionalConsents(){return useContext(Context);}
export function useConsents(){const v=useContext(Context);if(!v)throw Error('ConsentProvider gerekli.');return v;}
// Opens the consent text before a photo picker. Resolves true when the picker may open, false when the person declined.
export function useConsentPrompt(){
 const consents=useConsents(),auth=useAuth();
 const [open,setOpen]=useState<{entry:store.Effective;resolve:(ok:boolean)=>void;result?:boolean}|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const pending=useRef<{resolve:(ok:boolean)=>void;ok:boolean}|null>(null);
 const ask=useCallback(async(feature:Feature):Promise<boolean>=>{
  let entry=store.forFeature(consents.items,feature);
  if(!entry&&!auth.offline)entry=store.forFeature(await consents.refresh(),feature);
  if(store.allows(entry))return true;
  if(!entry)throw Error(auth.offline?'Rıza metni bu cihazda yok. Fotoğraf eklemek için internete bağlanın; fotoğrafsız devam edebilirsiniz.':'Fotoğraf rızası bilgisi alınamadı. Fotoğrafsız devam edebilirsiniz.');
  if(!entry.item.currentText)throw Error('Fotoğraf ekleme için rıza metni henüz yayınlanmadı. Fotoğrafsız devam edebilirsiniz.');
  setError('');const chosen=entry;
  return new Promise<boolean>(resolve=>setOpen({entry:chosen,resolve}));
 },[consents,auth.offline]);
 // The caller opens the photo picker right after this resolves. iOS cannot present the picker while the sheet is still
 // animating away (the picker never appears and its promise never settles), so the answer is released only after dismissal.
 const settle=useCallback(()=>{const p=pending.current;pending.current=null;if(p){setOpen(null);p.resolve(p.ok);}},[]);
 const finish=(ok:boolean)=>{if(!open||pending.current)return;pending.current={resolve:open.resolve,ok};setOpen({...open,result:ok});setError('');
  // onDismiss is iOS-only; other platforms and a missed iOS event fall back to a short delay after the close animation.
  setTimeout(settle,Platform.OS==='ios'?1200:350);};
 const approve=async()=>{if(!open)return;setBusy(true);setError('');try{await consents.decide(open.entry,'APPROVE');finish(true);}catch(e){setError(e instanceof Error?e.message:'Rıza kaydedilemedi.');}finally{setBusy(false);}};
 const element=open?<ConsentSheet entry={open.entry} mode="approve" visible={open.result===undefined} onDismiss={settle} offline={auth.offline} busy={busy} error={error} onApprove={approve} onClose={()=>{if(!busy)finish(false);}}/>:null;
 return {ask,element};
}
