import {useEffect,useState} from 'react';
import {AppState} from 'react-native';
import {useOptionalAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {parseNotice,type Kind} from './contracts';
import {readNotices} from './store';
type Entry={key:string;count:number;at:number};
const TTL=30000;let entry:Entry|null=null;let inflight:string|null=null;const listeners=new Set<()=>void>();
function publish(next:Entry){entry=next;listeners.forEach(l=>l());}
export function invalidateUnread(){entry=null;listeners.forEach(l=>l());}
export function useUnreadCount():number|null{
 const auth=useOptionalAuth();const user=auth?.session?.userId;const c=auth?.selected;const key=user&&c?user+'|'+contextKey(c):null;const [version,tick]=useState(0);
 useEffect(()=>{const l=()=>tick(v=>v+1);listeners.add(l);return()=>{listeners.delete(l);};},[]);
 useEffect(()=>{
  if(!auth||!user||!c||!key)return;
  const load=async()=>{
   if(inflight===key||entry?.key===key&&Date.now()-entry.at<TTL)return;inflight=key;
   try{
    let total=0;
    for(const kind of ['ANNOUNCEMENT','MESSAGE'] as Kind[]){
     const q={kind,page:1,itemId:null};
     if(auth.offline){const cached=await auth.noticeCache(db=>readNotices(db,user,c,q,Date.now()));total+=cached?.unread??0;continue;}
     const response=await auth.noticeRequest(q);if(!response.ok)return;total+=parseNotice(await response.json(),user,c,q).unread;
    }
    publish({key,count:total,at:Date.now()});
   }catch{/* The badge stays hidden when counts cannot be verified. */}finally{if(inflight===key)inflight=null;}
  };
  void load();const timer=setInterval(()=>{if(AppState.currentState==='active')void load();},TTL);const sub=AppState.addEventListener('change',s=>{if(s==='active')void load();});
  return()=>{clearInterval(timer);sub.remove();};
 },[auth,user,c,key,version]);
 return entry&&entry.key===key?entry.count:null;
}
