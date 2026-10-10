"use client";
import {useLocale} from "next-intl";
import {useEffect,useState} from "react";
import {errorMessage,useAuthorizedApi,type PagedRows} from "@/services/api";
export function useRows<T>(url:string,revision:number,enabled=true,decode?:(value:unknown)=>PagedRows<T>){
  const locale=useLocale();const api=useAuthorizedApi();const [data,setData]=useState<PagedRows<T>|null>(null);const [loading,setLoading]=useState(true);const [error,setError]=useState("");
  useEffect(()=>{
    const controller=new AbortController();setData(null);setError("");setLoading(enabled);
    if(enabled)void api<PagedRows<T>>(url,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setData(decode?decode(value):value);}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[api,url,revision,enabled,decode,locale]);
  return {data,loading,error};
}
export function useSearch(term:string){
  const [search,setSearch]=useState("");
  useEffect(()=>{const timer=setTimeout(()=>setSearch(term.trim()),300);return()=>clearTimeout(timer);},[term]);return search;
}
