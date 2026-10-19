"use client";
import {useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Copy,KeyRound} from "lucide-react";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {parsePerson} from "@/services/management-contracts";
import {formatDate} from "@/lib/format";
import type {Person} from "./types";
export function PersonCodeValue({person}:{person:Person}){
 const t=useTranslations("ui");const locale=useLocale();
 if(!person.personCodeHint)return <>{t(person.linkedUserId?"personCodeUsed":"personCodeMissing")}</>;
 return <span className="flex flex-col"><span className="font-mono tracking-widest" aria-label={`${t("personCode")}: •••• ${person.personCodeHint}`}>•••• {person.personCodeHint}</span>{person.personCodeIssuedAt&&<small className="text-muted-foreground">{t("personCodeIssuedAt",{date:formatDate(person.personCodeIssuedAt,locale)})}</small>}</span>;
}
export function RegistrationSmsNotice({sms}:{sms:NonNullable<Person["registrationSms"]>}){
 const t=useTranslations("ui");const key=sms.status==="QUEUED"?"personCodeSmsQueued":sms.status==="SKIPPED"?"personCodeSmsSkipped":sms.errorCode==="NO_ACCOUNT"?"personCodeSmsNoAccount":sms.errorCode==="INSUFFICIENT_CREDIT"?"personCodeSmsNoCredit":sms.errorCode==="PHONE_INVALID"?"personCodeSmsPhoneInvalid":"personCodeSmsFailed";
 return <p role="status" className={sms.status==="QUEUED"?"text-sm text-muted-foreground":"rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900"}>{t(key)}</p>;
}
export function IssuedPersonCode({code}:{code:string}){
 const t=useTranslations("ui");const notification=useNotification();
 async function copy(){try{await navigator.clipboard.writeText(code);notification.success(t("personCodeCopied"));}catch{}}
 return <div className="space-y-3"><p className="text-sm text-muted-foreground">{t("personCodeIssuedHelp")}</p><div className="flex items-center justify-between gap-3 rounded-xl border bg-muted/40 p-4"><output className="font-mono text-3xl font-semibold tracking-[0.3em]" aria-live="polite">{code}</output><Button type="button" variant="view" onClick={copy}><Copy/>{t("personCodeCopy")}</Button></div></div>;
}
export function IssuedPersonCodeDialog({code,sms,onClose}:{code:string;sms?:Person["registrationSms"];onClose:()=>void}){
 const t=useTranslations("ui");
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><DialogContent className="rounded-2xl bg-card"><DialogHeader><DialogTitle>{t("personCodeIssuedTitle")}</DialogTitle><DialogDescription className="sr-only">{t("personCodeIssuedHelp")}</DialogDescription></DialogHeader><IssuedPersonCode code={code}/>{sms&&<RegistrationSmsNotice sms={sms}/>}<DialogFooter><Button onClick={onClose}>{t("close")}</Button></DialogFooter></DialogContent></Dialog>;
}
export function RegeneratePersonCodeDialog({person,propertyId,onClose,onRenewed}:{person:Person;propertyId:string;onClose:()=>void;onRenewed:(person:Person)=>void}){
 const t=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notification=useNotification();
 const [busy,setBusy]=useState(false);const [error,setError]=useState("");const [code,setCode]=useState<string|null>(null);const [sendSms,setSendSms]=useState(!!person.phoneNumber);const [sms,setSms]=useState<Person["registrationSms"]>(null);const lock=useRef(false);
 async function regenerate(){if(lock.current)return;lock.current=true;setBusy(true);setError("");
  try{const renewed=parsePerson(await api<unknown>(`/api/v1/properties/${propertyId}/people/${person.id}/code`,{method:"POST",body:JSON.stringify({version:person.version,sendSms})}),propertyId,person.id);
   if(!renewed.issuedPersonCode)throw new Error("Missing issued code");setCode(renewed.issuedPersonCode);setSms(renewed.registrationSms??null);notification.success(t("personCodeRenewed"));onRenewed({...renewed,issuedPersonCode:null,registrationSms:null});}
  catch(reason){setError(errorMessage(reason,locale));}finally{lock.current=false;setBusy(false);}}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><DialogContent showCloseButton={!busy} className="rounded-2xl bg-card"><DialogHeader><DialogTitle>{code?t("personCodeIssuedTitle"):t("personCodeRegenerate")}</DialogTitle><DialogDescription>{code?"":t("personCodeRegenerateConfirm")}</DialogDescription></DialogHeader>{error&&<MessageBanner>{error}</MessageBanner>}{code&&<IssuedPersonCode code={code}/>}{code&&sms&&<RegistrationSmsNotice sms={sms}/>}{!code&&<label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={sendSms} disabled={busy} onChange={e=>setSendSms(e.target.checked)}/>{t("personCodeSendSms")}</label>}<DialogFooter>{code?<Button onClick={onClose}>{t("close")}</Button>:<><Button variant="outline" disabled={busy} onClick={onClose}>{t("cancel")}</Button><Button disabled={busy} onClick={regenerate}><KeyRound/>{busy?t("saving"):t("personCodeRegenerate")}</Button></>}</DialogFooter></DialogContent></Dialog>;
}
