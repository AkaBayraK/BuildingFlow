import {useCallback,useEffect,useRef,useState} from 'react';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {listPath,parseList,permitted,type Command,type WorkOrder} from './contracts';
import {readCopy,saveCopy,enqueue,visibleJobs,discard,retry,purgeUnauthorized,flush,FlushStopped,type Job} from './queue';
export function useSupplierWork(page:number){
 const a=useAuth(),c=a.selected,user=a.session?.userId,allowed=permitted(c);
 const read=useScopedRead({scope:allowed&&user?JSON.stringify([user,contextKey(c!),'supplier-work',page]):null,offline:a.offline,
  read:()=>a.supplierWorkCache(db=>readCopy(db,user!,c!,page,Date.now())).catch(()=>null),
  request:async()=>{const r=await a.supplierWorkRequest(listPath(c!,page),'GET');return{status:r.status===404?403:r.status,json:()=>r.json()} as Response;},
  parse:v=>parseList(v,user!,c!,page,new Date().toISOString()),save:v=>a.supplierWorkCache(db=>saveCopy(db,user!,c!,v,Date.now())),
  purge:()=>a.supplierWorkCache((db,files)=>purgeUnauthorized(db,files,{...a.session!,contexts:a.session!.contexts.filter(x=>contextKey(x)!==contextKey(c!))})),
  error:'Tedarikçi işleri güncellenemedi. Kopya eski olabilir; bağlantı kurup yenileyin.',denied:'Bu yönetim alanındaki tedarikçi işlerine erişiminiz artık geçerli değil.'});
 const [jobs,setJobs]=useState<Job[]>([]),[message,setMessage]=useState(''),[sending,setSending]=useState(false);const alive=useRef(true);
 useEffect(()=>()=>{alive.current=false;},[]);
 const fetchJobs=useCallback(async()=>{if(!allowed||!user)return [];try{return await a.supplierWorkCache(db=>visibleJobs(db,user,c!));}catch{return [];}},[a,allowed,user,c]);
 const loadJobs=useCallback(async()=>{const rows=await fetchJobs();if(alive.current)setJobs(rows);},[fetchJobs]);
 const send=useCallback(async()=>{
  await Promise.resolve();if(!allowed||!a.session||a.offline||sending||!alive.current)return;setSending(true);setMessage('');
  try{
   await a.supplierWorkCache((db,files)=>purgeUnauthorized(db,files,a.session!));
   const sent=await flush({db:task=>a.supplierWorkCache(db=>task(db)),files:{read:name=>a.supplierWorkCache((_db,f)=>f.read(name)),drop:name=>a.supplierWorkCache((_db,f)=>f.drop(name))},
    send:async(path,method,body,photo)=>{const r=await a.supplierWorkRequest(path,method,body,photo);return {status:r.status,retryAfter:Number(r.headers.get('Retry-After'))*1000||undefined};},now:Date.now,active:()=>alive.current},a.session,c!);
   if(sent>0){setMessage(sent+' işlem sunucuya iletildi.');read.refresh();}
  }catch(e){setMessage(e instanceof FlushStopped&&e.status===401?'Oturum yenilenmeli; işlemler cihazda bekliyor.':'Bekleyen işlemler gönderilemedi; cihazda saklanıyor.');}
  finally{if(alive.current){setSending(false);await loadJobs();}}
 },[a,allowed,c,sending,loadJobs,read]);
 useEffect(()=>{let live=true;void fetchJobs().then(rows=>{if(live)setJobs(rows);});return()=>{live=false;};},[fetchJobs]);
 useEffect(()=>{if(!a.offline&&read.data)void send();},[a.offline,read.data]);// eslint-disable-line react-hooks/exhaustive-deps
 const queue=async(input:Omit<Command,'operationId'|'photoFile'>,current:WorkOrder,photo:string|null)=>{
  if(!a.session)return;const operationId=Crypto.randomUUID(),photoFile=photo?'supplier-photo-'+Crypto.randomUUID()+'.enc':null;
  await a.supplierWorkCache(async(db,files)=>{if(photoFile&&photo)await files.write(photoFile,photo);try{await enqueue(db,a.session!,c!,{...input,operationId,photoFile},current,Date.now());}catch(e){if(photoFile)await files.drop(photoFile);throw e;}});
  await loadJobs();if(!a.offline)void send();
 };
 const change=async(id:string,action:'discard'|'retry')=>{await a.supplierWorkCache((db,files)=>action==='discard'?discard(db,files,user!,c!,id,Date.now()):retry(db,user!,c!,id,Date.now()));await loadJobs();if(action==='retry')void send();};
 return {...read,allowed,jobs,message,sending,queue,send,change};
}
