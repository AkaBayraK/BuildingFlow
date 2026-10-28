"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useLocale} from "next-intl";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";

export const parkingRoot=(propertyId:string)=>`/api/v1/properties/${propertyId}/parking`;

/** One validated GET per path/revision; aborted on change so a late answer never overwrites a newer screen. */
export function useParkingRead<T>(path:string|null,read:(value:unknown)=>T,revision:number){
 const api=useAuthorizedApi(),locale=useLocale();
 const [data,setData]=useState<T|null>(null),[loading,setLoading]=useState(!!path),[error,setError]=useState("");
 const reader=useRef(read);reader.current=read;
 useEffect(()=>{if(!path){setData(null);setLoading(false);return;}const c=new AbortController();setData(null);setLoading(true);setError("");
  void api<unknown>(path,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(reader.current(v));}).catch(e=>{if(!c.signal.aborted){setData(null);setError(errorMessage(e,locale));}}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();},[api,path,locale,revision]);
 return {data,loading,error};
}

/** Mutation with a submit lock; 409 marks the record stale, coded errors are already shown by the API layer. No blind retry. */
export function useParkingAction(){
 const api=useAuthorizedApi(),locale=useLocale(),notice=useNotification();
 const lock=useRef(false);const [busy,setBusy]=useState(false),[stale,setStale]=useState(false);
 const run=useCallback(async<T,>(path:string,method:string,body:unknown,read:(v:unknown)=>T,success?:string):Promise<T|null>=>{
  if(lock.current)return null;lock.current=true;setBusy(true);setStale(false);
  try{const value=read(await api<unknown>(path,{method,body:body===undefined?undefined:JSON.stringify(body)}));if(success)notice.success(success);return value;}
  catch(e){if(e instanceof ApiError&&e.status===409)setStale(true);if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));return null;}
  finally{lock.current=false;setBusy(false);}
 },[api,locale,notice]);
 return {run,busy,stale,setStale};
}

/** Stable UUID per unchanged form content: a retried submit replays, an edited form gets a new id. */
export function useRequestId(content:string){
 const ref=useRef<{content:string;id:string}|null>(null);
 if(!ref.current||ref.current.content!==content)ref.current={content,id:crypto.randomUUID()};
 return ref.current.id;
}
