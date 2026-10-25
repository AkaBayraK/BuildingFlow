"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Mail,Phone,RefreshCw,Send,ShieldCheck,X} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {accountChanged} from "./useAccount";
import {contactStyle} from "./contact-style";
import {challengeExpired,phoneCommand,challengeOpen,contactReturnUri,emailAction,maskContact,parseContact,parseOtpResult,type Contact,type Outcome} from "./contact-verification";
const root="/api/v1/account/contact-verification";
const handledStatuses=[400,401,403,404,409,429,500,502,503,504] as const;
export function ContactVerification({profileVersion,onEmail}:{profileVersion:number;onEmail?:(email:string|null)=>void}){
 const t=useTranslations("verification"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const [contact,setContact]=useState<Contact|null>(null),[busy,setBusy]=useState(false),[loading,setLoading]=useState(true),[error,setError]=useState(""),[errorKind,setErrorKind]=useState<"error"|"warning">("error"),[outcome,setOutcome]=useState<Outcome|null>(null),[code,setCode]=useState(""),[remaining,setRemaining]=useState(0),[expired,setExpired]=useState(false);
 const lock=useRef(false),sequence=useRef(0),mounted=useRef(false),codeInput=useRef<HTMLInputElement>(null),expiryRead=useRef("");
 // Only memory: a network retry uses the exact UUID/body, including VERIFY, never a new send.
 const retry=useRef<{action:string;code:string;body:string;id:string}|null>(null);
 const apply=useCallback((value:Contact)=>{setContact(value);setRemaining(value.phone.retryAfterSeconds);setExpired(challengeExpired(value.phone));},[]);
 const refresh=useCallback(async()=>{
  if(lock.current)return;const ticket=++sequence.current;setLoading(true);
  try{const value=parseContact(await api<unknown>(root,{},"json",handledStatuses));if(mounted.current&&ticket===sequence.current){apply(value);setError("");setOutcome(null);}}
  catch(e){if(mounted.current&&ticket===sequence.current){setErrorKind("error");setError(errorMessage(e,locale));setContact(null);}}
  finally{if(mounted.current&&ticket===sequence.current)setLoading(false);}
 },[api,apply,locale]);
 useEffect(()=>{mounted.current=true;void refresh();const focus=()=>{if(document.visibilityState==="visible"){setCode("");void refresh();accountChanged();}};window.addEventListener("focus",focus);document.addEventListener("visibilitychange",focus);
  return()=>{mounted.current=false;sequence.current++;retry.current=null;window.removeEventListener("focus",focus);document.removeEventListener("visibilitychange",focus);};},[refresh]);
 useEffect(()=>{void refresh();},[profileVersion,refresh]);
 useEffect(()=>{if(!contact)return;const end=Date.now()+contact.phone.retryAfterSeconds*1000;const timer=window.setInterval(()=>{
  setRemaining(Math.max(0,Math.ceil((end-Date.now())/1000)));
  const at=contact.phone.expiresAt;if(at&&challengeOpen(contact.phone)&&Date.parse(at)<=Date.now()){
   setExpired(true);if(expiryRead.current!==at){expiryRead.current=at;void refresh();}
  }
 },1000);return()=>clearInterval(timer);},[contact,refresh]);
 useEffect(()=>{setCode("");if(contact?.phone.status==="VERIFIED")retry.current=null;},[contact?.phone.challengeId,contact?.phone.status]);
 async function command(action:"SEND"|"VERIFY"|"CANCEL"){
  if(lock.current||!contact)return;if(action==="VERIFY"&&!/^[0-9]{6}$/.test(code)){codeInput.current?.focus();return;}
  lock.current=true;sequence.current++;setLoading(false);setBusy(true);setError("");setOutcome(null);
  const previous=retry.current,entered=action==="VERIFY"?code:"";
  const id=previous?.action===action&&previous.code===entered?previous.id:crypto.randomUUID();
  const body=previous?.id===id?previous.body:JSON.stringify(phoneCommand(contact,id,action,entered));
  retry.current={action,code:entered,body,id};
  try{
   const result=parseOtpResult(await api<unknown>(root+"/phone/commands",{method:"POST",body},"json",handledStatuses),id);
   retry.current=null;if(!mounted.current)return;
   apply({...result.contact,phone:{...result.contact.phone,retryAfterSeconds:Math.max(result.retryAfterSeconds,result.contact.phone.retryAfterSeconds)}});setOutcome(result.outcome);setCode("");
   if(["VERIFIED","ALREADY_VERIFIED"].includes(result.outcome))accountChanged();
   if(["ACCEPTED","UNKNOWN","REJECTED"].includes(result.outcome))requestAnimationFrame(()=>codeInput.current?.focus());
  }catch(e){if(!mounted.current)return;setCode("");
   if(e instanceof ApiError&&e.status<500)retry.current=null;
   if(e instanceof ApiError&&e.status===409){lock.current=false;await refresh();setErrorKind("warning");setError(t("conflict"));}
   else{setErrorKind(e instanceof ApiError&&e.status===429?"warning":"error");setError(errorMessage(e,locale));}
  }
  finally{lock.current=false;if(mounted.current)setBusy(false);}
 }
 async function email(){if(lock.current||!contact?.email.available)return;lock.current=true;setBusy(true);setError("");
  try{await identity.login({action:emailAction(contact.email.status),redirectUri:contactReturnUri(window.location.origin),locale});}
  catch{setError(t("identityUnavailable"));}finally{lock.current=false;if(mounted.current)setBusy(false);}
 }
 useEffect(()=>{onEmail?.(contact?.email.available?contact.email.value:null);},[contact,onEmail]);
 const p=contact?.phone,open=p&&challengeOpen(p)&&!expired;
 return <section aria-labelledby="contact-verification-title" className="rounded-2xl border bg-card p-5 shadow-sm lg:col-span-2" aria-busy={busy||loading}>
  <div className="flex flex-wrap items-center justify-between gap-3"><h2 id="contact-verification-title" className="flex items-center gap-2 text-base font-semibold"><ShieldCheck aria-hidden className="size-4"/>{t("title")}</h2><Button type="button" variant="outline" size="sm" disabled={busy||loading} onClick={()=>{void refresh();accountChanged();}}><RefreshCw aria-hidden/>{t("refresh")}</Button></div>
  <p className="mt-2 text-sm text-muted-foreground">{t("hint")}</p>
  {loading&&!contact&&<p role="status" className="mt-3">{t("loading")}</p>}
  {error&&<MessageBanner kind={errorKind}>{error}</MessageBanner>}
  {contact&&<div className="mt-4 grid min-w-0 gap-5 md:grid-cols-2">
   <div className="min-w-0 space-y-3 rounded-xl border p-4"><h3 className="flex items-center gap-2 font-medium"><Mail aria-hidden className="size-4"/>{t("email")}</h3><ToneBadge {...contactStyle[contact.email.status]}>{t("state_"+contact.email.status)}</ToneBadge><p className="break-all font-medium">{maskContact(contact.email.value,"email")}</p><p className="text-sm text-muted-foreground">{t("emailNative")}</p>
    <Button type="button" variant="outline" disabled={busy||loading||!contact.email.available} onClick={()=>void email()}><Mail aria-hidden/>{t(contact.email.status==="UNVERIFIED"?"verifyEmail":"changeEmail")}</Button>
    <p className="text-xs text-muted-foreground">{t("checked",{date:formatDateTime(contact.email.checkedAt,locale)})}</p>
   </div>
   <div className="min-w-0 space-y-3 rounded-xl border p-4"><h3 className="flex items-center gap-2 font-medium"><Phone aria-hidden className="size-4"/>{t("phone")}</h3><ToneBadge {...contactStyle[p!.status]}>{t("state_"+p!.status)}</ToneBadge>
    <dl className="space-y-2 text-sm"><div><dt className="text-muted-foreground">{t("verifiedTarget")}</dt><dd className="font-medium">{maskContact(p!.verifiedValue,"phone")}</dd></div><div><dt className="text-muted-foreground">{t("pendingTarget")}</dt><dd className="font-medium">{maskContact(p!.pendingValue,"phone")}</dd></div></dl>
    <p className="text-sm text-muted-foreground">{t("phoneHint")}</p>
    {!p!.canSendCode&&p!.status!=="VERIFIED"&&<p className="text-sm text-muted-foreground">{t("unavailable")}</p>}
    <div className="flex flex-wrap gap-2"><Button type="button" disabled={busy||loading||remaining>0||!p!.canSendCode} onClick={()=>void command("SEND")}><Send aria-hidden/>{t(p!.challengeId?"resend":"send")}</Button>{p!.challengeId&&["ACCEPTED","UNKNOWN"].includes(p!.deliveryStatus)&&<Button type="button" variant="outline" disabled={busy||loading} onClick={()=>void command("CANCEL")}><X aria-hidden/>{t("cancel")}</Button>}</div>
    {remaining>0&&<p className="text-sm text-muted-foreground" role="timer">{t("retry",{seconds:remaining})}</p>}
    {p!.expiresAt&&<p className="text-xs text-muted-foreground">{t("expires",{date:formatDateTime(p!.expiresAt,locale)})}</p>}
    {p!.attemptsRemaining!==null&&open&&<p className="text-sm">{t("attempts",{count:p!.attemptsRemaining})}</p>}
    {open&&<form className="space-y-2" onSubmit={e=>{e.preventDefault();void command("VERIFY");}}><Label htmlFor="contact-code">{t("code")}</Label><div className="flex flex-wrap gap-2"><Input ref={codeInput} id="contact-code" className="min-w-0 flex-1" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} required value={code} disabled={busy||loading} onChange={e=>setCode(e.target.value.replace(/[^0-9]/g,"").slice(0,6))} aria-describedby="contact-code-help"/><Button type="submit" disabled={busy||loading||code.length!==6}><ShieldCheck aria-hidden/>{t("verify")}</Button></div><p id="contact-code-help" className="text-xs text-muted-foreground">{t("codeHelp")}</p></form>}
    {(outcome||expired)&&<MessageBanner kind={outcome==="VERIFIED"||outcome==="ALREADY_VERIFIED"?"success":outcome==="ACCEPTED"||outcome==="CANCELLED"?"info":"warning"}>{t("outcome_"+(outcome??"EXPIRED"))}</MessageBanner>}
   </div>
  </div>}
 </section>;
}
