import {useEffect,useRef,useState} from 'react';
import {AppState,Platform} from 'react-native';
import {loadScoped} from './scopedRead';
type Options<T>={scope:string|null;offline:boolean;read:()=>Promise<T|null>;request:()=>Promise<Response>;parse:(v:unknown)=>T;save:(v:T)=>Promise<unknown>;purge:()=>Promise<unknown>;error:string;denied:string};
export function useScopedRead<T>(options:Options<T>){
 const ref=useRef(options);useEffect(()=>{ref.current=options;});
 const {scope,offline}=options;
 const [tick,refresh]=useState(0),[result,setResult]=useState<{scope:string;value:T;cached:boolean}|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 useEffect(()=>{let active=true;const o=ref.current;
  async function load(){await Promise.resolve();if(!active||!scope)return;setResult(null);setError('');setBusy(true);
   await loadScoped({...o,active:()=>active,cache:Platform.OS!=='web',value:(value,cached)=>setResult(value===null?null:{scope,value,cached}),error:denied=>setError(denied?o.denied:o.error)});
   if(active)setBusy(false);
  }
  void load();return()=>{active=false;};
 },[scope,offline,tick]);
 useEffect(()=>{const subscription=AppState.addEventListener('change',s=>{if(s==='active')refresh(n=>n+1);});return()=>subscription.remove();},[]);
 const current=result?.scope===scope?result:null;
 return {data:current?.value??null,busy,error,offline:offline||!!current?.cached,refresh:()=>refresh(n=>n+1)};
}
