"use client";
import {useEffect,useRef,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {Camera,LoaderCircle,Trash2} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {AccountAvatar} from "@/components/AccountAvatar";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Dialog,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {accountChanged} from "@/features/account/useAccount";
import {photoTypes,preparePhoto} from "@/features/account/photo";
import {displayName,readUserPhoto,readUserPhotoResult,type ManagedUser,type UserPhotoState} from "./model";

type Command={operationId:string;expectedVersion:number;photoAction:"REPLACE"|"REMOVE";photoBase64?:string};
export function UserPhotoDialog({user,onClose,onSaved}:{user:ManagedUser;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("users"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification(),identity=useIdentity();
 const root=`/api/v1/users/${encodeURIComponent(user.id)}/photo`;
 const [state,setState]=useState<UserPhotoState|null>(null),[reload,setReload]=useState(0),[loading,setLoading]=useState(true),[loadFailed,setLoadFailed]=useState(false);
 const [picture,setPicture]=useState<Awaited<ReturnType<typeof preparePhoto>>|null>(null),[action,setAction]=useState<"KEEP"|"REPLACE"|"REMOVE">("KEEP"),[busy,setBusy]=useState(false),[stale,setStale]=useState(false);
 const namesReady=state?.version!==0||!!(user.firstName?.trim()&&user.lastName?.trim()&&user.firstName.trim().length<=80&&user.lastName.trim().length<=80);
 const input=useRef<HTMLInputElement>(null),lock=useRef(false),pending=useRef<Command|null>(null);
 useEffect(()=>{const c=new AbortController();setLoading(true);setLoadFailed(false);setState(null);
  void api<unknown>(root+"/state",{signal:c.signal},"json",[403]).then(v=>{if(!c.signal.aborted)setState(readUserPhoto(v,user.id));}).catch(e=>{if(!c.signal.aborted){setLoadFailed(true);notice.error(errorMessage(e,locale));}}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();
 },[api,root,user.id,reload,locale,notice]);
 useEffect(()=>()=>{if(picture)URL.revokeObjectURL(picture.previewUrl);},[picture]);
 function refresh(){if(lock.current)return;setPicture(null);setAction("KEEP");pending.current=null;setStale(false);setReload(n=>n+1);onSaved();}
 async function choose(file:File){
  if(lock.current||!state?.canReplace||!namesReady||stale)return;lock.current=true;setBusy(true);
  try{const prepared=await preparePhoto(file);setPicture(prepared);setAction("REPLACE");pending.current=null;}
  catch{notice.error(t("photoInvalid"));}finally{lock.current=false;setBusy(false);}
 }
 async function save(){
  if(lock.current||!state||stale||action==="KEEP"||(action==="REPLACE"&&(!picture||!state.canReplace)))return;
  lock.current=true;setBusy(true);
  try{
   // Keep this exact UUID and payload after an uncertain response; changing a selection creates a new command.
   pending.current??={operationId:crypto.randomUUID(),expectedVersion:state.version,photoAction:action,...(action==="REPLACE"?{photoBase64:picture!.base64}:{})};
   const command=pending.current;
   readUserPhotoResult(await api<unknown>(root+"/commands",{method:"POST",body:JSON.stringify(command)},"json",[403,409]),user.id,command.operationId);
   notice.success(t("photoSaved"));if(identity.subject===user.id)accountChanged();onSaved();onClose();
  }catch(e){
   if(e instanceof ApiError&&e.status===409){setStale(true);notice.error(t("photoStale"));}
   else if(e instanceof ApiError&&e.feedback?.code==="BF_CONSENT_REQUIRED"){setStale(true);notice.error(t("photoConsentMissing"));}
   else notice.error(errorMessage(e,locale));
  }finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!lock.current)onClose();}}><ManagementPanel showCloseButton={!busy} className="overflow-y-auto sm:max-w-lg" onCloseAutoFocus={e=>{e.preventDefault();requestAnimationFrame(()=>{const button=Array.from(document.querySelectorAll<HTMLButtonElement>("button[data-user-photo]")).find(b=>b.dataset.userPhoto===user.id&&b.getClientRects().length>0);button?.focus();});}} onInteractOutside={e=>{if(lock.current)e.preventDefault();}} onEscapeKeyDown={e=>{if(lock.current)e.preventDefault();}}>
  <DialogHeader><DialogTitle>{t("photoTitle",{name:user.username})}</DialogTitle><DialogDescription>{t("photoHelp")}</DialogDescription></DialogHeader>
  {loading?<p role="status" className="flex items-center gap-2 text-sm"><LoaderCircle className="size-4 animate-spin"/>{ui("loading")}</p>:loadFailed?<p className="text-sm text-muted-foreground">{t("photoLoadFailed")}</p>:state&&<>
   <div className="flex flex-col items-center gap-4 rounded-xl border bg-muted/30 p-5">
    {picture&&action==="REPLACE"?<img src={picture.previewUrl} alt={t("photoPreview")} className="size-32 rounded-full border object-cover"/>:<AccountAvatar key={state.photoId??"none"} path={action!=="REMOVE"&&state.photoId?root:null} name={displayName(user)} className="size-32 text-3xl"/>}
    <strong className="text-center">{displayName(user)}</strong>
    <Input ref={input} type="file" accept={photoTypes.join(",")} className="hidden" aria-hidden="true" tabIndex={-1} aria-label={t("photoChoose")} disabled={busy||stale||!state.canReplace||!namesReady} onChange={e=>{const file=e.currentTarget.files?.[0];e.currentTarget.value="";if(file)void choose(file);}}/>
    <div className="flex flex-wrap justify-center gap-2"><Button type="button" variant="outline" disabled={busy||stale||!state.canReplace||!namesReady} onClick={()=>input.current?.click()}><Camera/>{t("photoChoose")}</Button><Button type="button" variant="outline" disabled={busy||stale||(!state.photoId&&!picture)||action==="REMOVE"} onClick={()=>{setPicture(null);setAction("REMOVE");pending.current=null;}}><Trash2/>{t("photoRemove")}</Button></div>
   </div>
   <p className="text-xs leading-relaxed text-muted-foreground">{t("photoRules")}</p>
   {!namesReady&&<p className="rounded-lg border bg-muted p-3 text-sm">{t("photoNamesRequired")}</p>}
   {!state.canReplace&&<div className="rounded-lg border bg-muted p-3 text-sm"><p>{t("photoConsentMissing")}</p>{identity.subject===user.id&&<Button asChild variant="link" className="px-0"><Link href="/account">{t("photoOwnConsent")}</Link></Button>}</div>}
   {action==="REMOVE"&&<p role="status" className="text-sm font-medium">{t("photoRemovePending")}</p>}
  </>}
  {busy&&<p role="status" className="flex items-center gap-2 text-sm"><LoaderCircle className="size-4 animate-spin"/>{t("photoPreparing")}</p>}
  {stale&&<p role="status" className="text-sm">{t("photoStale")}</p>}
  {(stale||loadFailed)&&<Button variant="outline" disabled={busy} onClick={refresh}>{t("photoReload")}</Button>}
  <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button><Button disabled={busy||loading||!state||stale||action==="KEEP"} onClick={()=>void save()}>{ui("save")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}
