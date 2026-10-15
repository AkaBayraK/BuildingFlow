import {useEffect,useRef,useState} from 'react';
import {AppState,Platform} from 'react-native';
type Options<T>={scope:string|null;offline:boolean;read:()=>Promise<T|null>;request:()=>Promise<Response>;parse:(v:unknown)=>T;save:(v:T)=>Promise<unknown>;purge:()=>Promise<unknown>;error:string;denied:string};
// Shared lifecycle for authorized paged snapshots. A changed account/context/query hides old data immediately.
export function useScopedRead<T>(options:Options<T>){
 const ref=useRef(options);useEffect(()=>{ref.current=options;});
 const {scope,offline}=options;
 const [tick,refresh]=useState(0),[result,setResult]=useState<{scope:string;value:T}|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 useEffect(()=>{let active=true;const o=ref.current;
  async function load(){
   await Promise.resolve();if(!active||!scope)return;
   setResult(null);setError('');setBusy(true);
   try{
    if(Platform.OS!=='web'){
     try{const cached=await o.read();if(active&&cached)setResult({scope,value:cached});}
     catch{await o.purge();if(offline)throw Error();}
    }
    if(!active||offline)return;
    const response=await o.request();if(!active)return;
    if(response.status===403){setResult(null);if(Platform.OS!=='web')await o.purge();if(active)setError(o.denied);return;}
    if(!response.ok)throw Error();
    const value=o.parse(await response.json());if(!active)return;
    if(Platform.OS!=='web')await o.save(value);
    if(active)setResult({scope,value});
   }catch{if(active)setError(o.error);}
   finally{if(active)setBusy(false);}
  }
  void load();return()=>{active=false;};
 },[scope,offline,tick]);
 useEffect(()=>{const subscription=AppState.addEventListener('change',s=>{if(s==='active')refresh(n=>n+1);});return()=>subscription.remove();},[]);
 return {data:result?.scope===scope?result.value:null,busy,error,offline,refresh:()=>refresh(n=>n+1)};
}
