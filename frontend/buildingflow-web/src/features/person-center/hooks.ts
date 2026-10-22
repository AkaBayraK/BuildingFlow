"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useLocale} from "next-intl";
import {useIdentity} from "@/auth/AuthGate";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {submissionKey,type SubmissionKey} from "@/features/dues/model";
export function useCenterRecord<T>(path:string,decode:(v:unknown)=>T,revision=0){
 const api=useAuthorizedApi(),locale=useLocale(),identity=useIdentity();const actor=identity.subject??identity.tokenParsed?.sub??"";
 const [state,setState]=useState<{path:string;actor:string;value:T|null;error:string;loading:boolean}>({path:"",actor:"",value:null,error:"",loading:true});
 useEffect(()=>{const controller=new AbortController();setState({path,actor,value:null,error:"",loading:true});void api<unknown>(path,{signal:controller.signal}).then(v=>{const value=decode(v);if(!controller.signal.aborted)setState({path,actor,value,error:"",loading:false});}).catch(e=>{if(!controller.signal.aborted)setState({path,actor,value:null,error:errorMessage(e,locale),loading:false});});return()=>controller.abort();},[path,actor,api,locale,decode,revision]);
 return state.path===path&&state.actor===actor?state:{path,actor,value:null,error:"",loading:true};
}
export function useCenterMutation(onDone:()=>void){
 const api=useAuthorizedApi(),locale=useLocale();const [busy,setBusy]=useState(false),[error,setError]=useState(""),[fields,setFields]=useState<Record<string,string[]>>({}),[conflict,setConflict]=useState(false),[ambiguous,setAmbiguous]=useState(false);
 const lock=useRef(false),attempt=useRef<SubmissionKey|null>(null),controller=useRef<AbortController|null>(null);
 useEffect(()=>()=>controller.current?.abort(),[]);
 const reset=useCallback(()=>{setError("");setFields({});setConflict(false);setAmbiguous(false);attempt.current=null;},[]);
 async function submit<T>(path:string,method:"POST"|"PUT",payload:Record<string,unknown>,parse:(v:unknown)=>T){
  if(lock.current)return;lock.current=true;setBusy(true);setError("");setFields({});setConflict(false);
  attempt.current=submissionKey(attempt.current,JSON.stringify({path,method,payload}),()=>crypto.randomUUID());controller.current=new AbortController();
  try{const result=parse(await api<unknown>(path,{method,body:JSON.stringify({...payload,operationId:attempt.current.id}),signal:controller.current.signal}));if(!controller.current.signal.aborted){setAmbiguous(false);onDone();}return result;}
  catch(e){if(!controller.current.signal.aborted){setError(errorMessage(e,locale));setFields(e instanceof ApiError?e.errors:{});setConflict(e instanceof ApiError&&e.status===409&&!e.feedback?.code?.startsWith("BF_PERSON_DUPLICATE_"));setAmbiguous(!(e instanceof ApiError)||e.status>=500);}}
  finally{lock.current=false;if(!controller.current?.signal.aborted)setBusy(false);}
 }
 return{submit,busy,error,fields,conflict,ambiguous,reset};
}
