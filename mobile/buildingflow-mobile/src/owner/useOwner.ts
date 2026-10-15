import {useEffect,useRef,useState} from 'react';
import {AppState,Platform} from 'react-native';
import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {parseSnapshot,type Snapshot} from './contracts';
import {readOwner,writeOwner,purgeOwner} from './cache';
export function useOwner(){
 const auth=useAuth(),user=auth.session?.userId,c=auth.selected;
 const key=c?contextKey(c):null;const [data,setData]=useState<Snapshot|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const lock=useRef(false),alive=useRef(true),epoch=useRef(0),runner=useRef(async()=>{});
 const refresh=async()=>{
  if(!user||!c||!['OWNER','TENANT'].includes(c.role)||lock.current||!alive.current)return;
  const generation=epoch.current;const active=()=>alive.current&&generation===epoch.current;
  lock.current=true;setBusy(true);
  try{
   if(Platform.OS!=='web'){const cached=await auth.residentCache(db=>readOwner(db,user,c,Date.now()));if(active())setData(cached);}
   if(auth.offline){if(active())setError('');return;}
   const response=await auth.residentRequest();if(!active())return;
   if(response.status===403){setData(null);if(Platform.OS!=='web')await auth.residentCache(db=>purgeOwner(db,user,c));throw Error('Bu daire için erişiminiz artık geçerli değil.');}
   if(!response.ok)throw Error('Mülk bilgileri alınamadı. Bağlantınızı kontrol edip yeniden deneyin.');
   const fresh=parseSnapshot(await response.json(),user,c);if(!active())return;
   if(Platform.OS!=='web')await auth.residentCache(db=>writeOwner(db,user,c,fresh,Date.now()));
   if(active()){setData(fresh);setError('');}
  }catch(e){if(active())setError(e instanceof Error?e.message:'Mülk bilgileri güncellenemedi.');}
  finally{lock.current=false;if(alive.current)setBusy(false);}
 };
 useEffect(()=>{runner.current=refresh;});
 useEffect(()=>{alive.current=true;epoch.current++;void runner.current();const timer=setInterval(()=>{if(AppState.currentState==='active')void runner.current();},30000);const listener=AppState.addEventListener('change',s=>{if(s==='active')void runner.current();});return()=>{alive.current=false;epoch.current++;clearInterval(timer);listener.remove();};},[key,user,auth.offline]);
 return {data:data&&data.userId===user&&data.contextKey===key?data:null,busy,error,refresh,offline:auth.offline};
}
