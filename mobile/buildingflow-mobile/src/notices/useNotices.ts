import {useEffect,useRef,useState} from 'react';
import {AppState,Platform} from 'react-native';
import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {parseNotice,parsePreference,readResult,type Kind,type Query} from './contracts';
import {readNotices,saveNotices,purgeNotices,noticeJobs,enqueueRead,flushReads,retryReads,type ReadJob} from './store';
import {invalidateUnread} from './unread';
export function useNotices(q:Query){
 const a=useAuth(),ref=useRef(a);useEffect(()=>{ref.current=a;});const user=a.session?.userId,c=a.selected,key=c?contextKey(c):null;
 const view=useScopedRead({scope:user&&c?JSON.stringify([user,key,q.kind,q.page,q.itemId]):null,offline:a.offline,read:()=>a.noticeCache(db=>readNotices(db,user!,c!,q,Date.now())),request:()=>a.noticeRequest(q),parse:v=>parseNotice(v,user!,c!,q),save:v=>a.noticeCache(db=>saveNotices(db,user!,c!,q,v,Date.now())),purge:()=>a.noticeCache(db=>purgeNotices(db,user!,c!)),error:'Bildirimler alınamadı. Yeniden deneyebilirsiniz.',denied:'Bu mülkün bildirimlerine erişiminiz artık geçerli değil.'});
 const [pending,setPending]=useState<ReadJob[]>([]),[mutating,setMutating]=useState(false),[message,setMessage]=useState('');const lock=useRef(false),runner=useRef(async()=>{}),viewRef=useRef(view),mounted=useRef(true);
 useEffect(()=>{viewRef.current=view;});
 useEffect(()=>{mounted.current=true;return()=>{mounted.current=false;};},[]);
 const sync=async()=>{
  if(lock.current)return;const auth=ref.current,u=auth.session?.userId,context=auth.selected;if(!u||!context)return;
  const k=contextKey(context),active=()=>mounted.current&&ref.current.session?.userId===u&&!!ref.current.selected&&contextKey(ref.current.selected)===k;
  lock.current=true;if(active())setMutating(true);
  try{
   if(Platform.OS!=='web'){
    if(!auth.offline)await flushReads({db:task=>auth.noticeCache(task),request:(kind,id)=>auth.noticeRead(kind,id),now:Date.now,active},u,context);
    const queue=await auth.noticeCache(db=>noticeJobs(db,u,context));if(active())setPending(queue);
   }
   if(active()){viewRef.current.refresh();invalidateUnread();}
  }catch{if(active())setMessage('Okundu kayıtları eşitlenemedi; bekleyen işlemler korunuyor.');}
  finally{lock.current=false;if(active())setMutating(false);}
 };
 useEffect(()=>{runner.current=sync;});
 useEffect(()=>{const timer=setTimeout(()=>void runner.current(),0),interval=setInterval(()=>{if(AppState.currentState==='active')void runner.current();},30000);const listener=AppState.addEventListener('change',s=>{if(s==='active')void runner.current();});return()=>{clearTimeout(timer);clearInterval(interval);listener.remove();};},[user,key,a.offline]);
 const mark=async(kind:Kind,id:string)=>{
  if(!user||!c||lock.current)return;
  try{if(Platform.OS==='web'){const response=await a.noticeRead(kind,id);if(!response.ok)throw Error();readResult(await response.json(),kind,id);view.refresh();setMessage('Okundu durumu merkezde kaydedildi.');return;}
   await a.noticeCache(db=>enqueueRead(db,user,c,kind,id,Date.now()));setMessage(a.offline?'Okundu işareti cihazda bekliyor; bağlantı gelince gönderilecek.':'Okundu işareti kuyruğa alındı.');await sync();
  }catch{setMessage('Okundu işareti kaydedilemedi. Yeniden deneyin.');}
 };
 const preference=async(enabled:boolean)=>{
  if(lock.current||a.offline||!view.data)return;lock.current=true;setMutating(true);
  try{const response=await a.noticePreference(enabled,view.data.preference.version);if(response.status===409){setMessage('Tercih başka cihazda değişmiş. Güncel bilgiyi alın ve yeniden deneyin.');view.refresh();return;}if(!response.ok)throw Error();const result=parsePreference(await response.json());if(result.highlightsEnabled!==enabled)throw Error();setMessage('Bildirim görünüm tercihi merkez hesabınıza kaydedildi.');view.refresh();}
  catch{setMessage('Tercih kaydedilemedi. Mevcut tercih korunuyor.');}
  finally{lock.current=false;if(mounted.current)setMutating(false);}
 };
 const retry=async()=>{if(!user||!c||a.offline||lock.current)return;await a.noticeCache(db=>retryReads(db,user,c,Date.now()));await sync();};
 return {...view,pending,mutating,message,mark,preference,sync,retry};
}
