import {useEffect,useRef,useState} from 'react';
import {Platform} from 'react-native';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {MAX_REQUEST_PHOTOS} from '../sync/contracts';
import {unitRequest} from '../owner/request';
import {selectRequestPhoto} from '../profile/picker';
import {emptyDraft,photoFileName,type Draft} from './contracts';
import * as q from './outbox';
export function useRequestDraft(onQueued:()=>Promise<void>){
 const auth=useAuth(),context=auth.selected,user=auth.session?.userId,key=context?contextKey(context):null;
 const [draft,setDraft]=useState<Draft>(emptyDraft),[previews,setPreviews]=useState<Record<string,string>>({}),[busy,setBusy]=useState(false),[message,setMessage]=useState(''),[loaded,setLoaded]=useState(false);
 const scope=useRef(key),lock=useRef(false),timer=useRef<ReturnType<typeof setTimeout>|null>(null),latest=useRef(draft);
 const native=Platform.OS!=='web';
 const store=auth.profileCache;const storeRef=useRef(store);useEffect(()=>{storeRef.current=store;});
 useEffect(()=>{
  // Callers key this hook by account/context, so a scope change remounts with an empty draft.
  scope.current=key;
  if(!native||!user||!key)return;
  void (async()=>{
   try{
    const value=await storeRef.current(async(db,files)=>{const d=await q.readDraft(db,user,key);const images:Record<string,string>={};for(const f of d?.photos??[]){try{images[f]='data:image/jpeg;base64,'+await files.read(f);}catch{/* A missing private file is dropped below. */}}return {d,images};});
    if(scope.current!==key)return;
    const d=value.d?{...value.d,photos:value.d.photos.filter(f=>value.images[f])}:emptyDraft();
    latest.current=d;setDraft(d);setPreviews(value.images);
   }catch{if(scope.current===key)setMessage('Kayıtlı taslak açılamadı.');}
   finally{if(scope.current===key)setLoaded(true);}
  })();
 },[key,user,native]);
 const persist=(next:Draft,immediate=false)=>{
  latest.current=next;setDraft(next);
  if(!native||!user||!key)return;
  if(timer.current)clearTimeout(timer.current);
  const write=()=>{if(scope.current===key)void storeRef.current(db=>q.saveDraft(db,user,key,latest.current,Date.now())).catch(()=>setMessage('Taslak cihaza kaydedilemedi.'));};
  if(immediate)write();else timer.current=setTimeout(write,600);
 };
 useEffect(()=>()=>{if(timer.current)clearTimeout(timer.current);},[]);
 const update=(patch:Partial<Draft>)=>persist({...latest.current,...patch});
 const addPhoto=async(camera:boolean)=>{
  if(!user||!key||lock.current)return;
  if(latest.current.photos.length>=MAX_REQUEST_PHOTOS){setMessage(`En fazla ${MAX_REQUEST_PHOTOS} fotoğraf ekleyebilirsiniz.`);return;}
  lock.current=true;setBusy(true);setMessage('');
  try{
   const base64=await selectRequestPhoto(camera);if(!base64||scope.current!==key)return;
   const file=photoFileName(Crypto.randomUUID());
   await store(async(_,files)=>files.write(file,base64));
   setPreviews(p=>({...p,[file]:'data:image/jpeg;base64,'+base64}));
   persist({...latest.current,photos:[...latest.current.photos,file]},true);
  }catch(e){setMessage(e instanceof Error?e.message:'Fotoğraf eklenemedi.');}
  finally{lock.current=false;setBusy(false);}
 };
 const removePhoto=async(file:string)=>{
  if(!user||!key)return;
  persist({...latest.current,photos:latest.current.photos.filter(f=>f!==file)},true);
  setPreviews(p=>{const n={...p};delete n[file];return n;});
  try{await store((db,files)=>q.dropDraftPhoto(db,files,file));}catch{/* Unreferenced ciphertext is removed with the account store. */}
 };
 const clear=async()=>{
  if(!user||!key)return;
  if(timer.current)clearTimeout(timer.current);
  try{await store((db,files)=>q.discardDraft(db,files,user,key));}catch{setMessage('Taslak silinemedi.');return;}
  latest.current=emptyDraft();setDraft(latest.current);setPreviews({});setMessage('Taslak silindi.');
 };
 const submit=async()=>{
  if(lock.current||!context||!auth.session||!user||!key)return;
  const d=latest.current;
  if(!d.description.trim()||d.description.trim().length>2000){setMessage('Talep açıklaması 1–2000 karakter olmalı.');return;}
  lock.current=true;setBusy(true);setMessage('');
  try{
   if(timer.current)clearTimeout(timer.current);
   const command=unitRequest(auth.session,context,d,Crypto.randomUUID(),Crypto.randomUUID());
   await auth.queueRequest(command,d,d.photos.map(()=>Crypto.randomUUID()));
   if(scope.current!==key)return;
   latest.current=emptyDraft();setDraft(latest.current);setPreviews({});
   setMessage(auth.offline?'Talep şifreli kuyruğa kaydedildi. Bağlantı gelince önce talep, sonra fotoğraflar gönderilecek.':'Talep kuyruğa alındı. Gönderim durumunu aşağıdaki eşitleme alanından izleyebilirsiniz.');
   await onQueued();
  }catch{setMessage('Talep kuyruğa kaydedilemedi. Taslağınız ve fotoğraflarınız korunuyor.');}
  finally{lock.current=false;setBusy(false);}
 };
 return {draft,previews,busy,message,loaded,native,update,addPhoto,removePhoto,clear,submit};
}
