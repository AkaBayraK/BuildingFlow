"use client";
import {useCallback,useEffect,useState,useSyncExternalStore} from "react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi} from "@/services/api";
import {parseAccount,type Account} from "./model";
// One revision counter for the page: the header avatar and "Hesap bilgilerim" reload together after a change.
let revision=0;const listeners=new Set<()=>void>();
export function accountChanged(){revision++;listeners.forEach(l=>l());}
const subscribe=(l:()=>void)=>{listeners.add(l);return()=>{listeners.delete(l);};};
export function useAccount(){
 const api=useAuthorizedApi(),identity=useIdentity(),userId=identity.subject??"";
 const current=useSyncExternalStore(subscribe,()=>revision,()=>0);
 const [account,setAccount]=useState<Account|null>(null),[failed,setFailed]=useState<unknown>(null);
 useEffect(()=>{if(!userId)return;const c=new AbortController();
  void api<unknown>("/api/v1/account",{signal:c.signal},"json",[401,403]).then(v=>{if(!c.signal.aborted){setAccount(parseAccount(v,userId));setFailed(null);}}).catch(e=>{if(!c.signal.aborted)setFailed(e);});
  return()=>c.abort();},[api,userId,current]);
 const reload=useCallback(()=>accountChanged(),[]);
 return {account,failed,reload};
}
