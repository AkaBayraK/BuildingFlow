"use client";
import {useEffect,useMemo,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {Ban,CheckCircle2,Eye,Pencil,Plus,RefreshCw,ShieldCheck,XCircle,type LucideIcon} from "lucide-react";
import {PhoneInput} from "@/components/forms/ContactInputs";
import {formatPhone,validPhone} from "@/lib/contact";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {DetailField,ToneBadge,toolbarSelectClass,type Tone} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {readPage,type Page} from "./model";
import {readPreview,readRecord,readSender,readSenderView,senderKeyword,senderMersis,senderShortCode,type ConsentRecord,type LegalSender,type SmsPreview} from "./compliance";

type ConsentRow=ConsentRecord&{id:string};
const consentRow=(v:unknown):ConsentRow=>{const r=readRecord(v);return {...r,id:r.recordId};};
export const decisionStyle:Record<ConsentRecord["status"],{tone:Tone;icon:LucideIcon}>={APPROVED:{tone:"success",icon:CheckCircle2},REJECTED:{tone:"error",icon:XCircle}};

function useBusy(){const lock=useRef(false);const [busy,setBusy]=useState(false);
 async function run(action:()=>Promise<void>){if(lock.current)return;lock.current=true;setBusy(true);try{await action();}finally{lock.current=false;setBusy(false);}}
 return {busy,run};}

export function SenderTab({root,propertyId,admin}:{root:string;propertyId:string;admin:boolean}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [sender,setSender]=useState<LegalSender|null|undefined>(undefined),[error,setError]=useState(""),[revision,setRevision]=useState(0),[edit,setEdit]=useState(false);
 useEffect(()=>{const c=new AbortController();setError("");void api<unknown>(root+"/legal-sender",{signal:c.signal}).then(v=>{if(!c.signal.aborted)setSender(readSenderView(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,propertyId,revision,locale]);
 if(error)return <MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>;
 if(sender===undefined)return <p role="status">{ui("loading")}</p>;
 return <section className="rounded-2xl border bg-card p-5 shadow-sm"><div className="flex flex-wrap items-center justify-between gap-2"><h2 className="flex items-center gap-2 font-semibold"><ShieldCheck className="size-4" aria-hidden="true"/>{t("senderTitle")}</h2>
   {admin&&<Button variant={sender?"edit":"default"} onClick={()=>setEdit(true)}>{sender?<><Pencil/>{t("edit")}</>:<><Plus/>{t("senderDefine")}</>}</Button>}</div>
  <p className="mt-2 text-sm text-muted-foreground">{t("senderHelp")}</p>
  {!sender?<MessageBanner className="mt-4" presentation="inline">{t("senderMissing")}</MessageBanner>:<dl className="mt-4 grid gap-4 sm:grid-cols-2">
   <DetailField label={t("senderLegalTitle")}>{sender.title}</DetailField><DetailField label={t("senderMersis")}>{sender.mersisNumber??"—"}</DetailField>
   <DetailField label={t("senderContact")}>{sender.contactInformation}</DetailField><DetailField label={t("senderOptOut")}>{sender.optOutEnabled?t("senderOptOutValue",{keyword:sender.optOutKeyword,code:sender.optOutShortCode}):t("senderOptOutDisabled")}</DetailField>
   <DetailField label={t("senderIys")}>{sender.iysIntegrationEnabled?t("senderIysOn",{brand:sender.iysBrandCode??""}):t("senderIysOff")}</DetailField><DetailField label={ui("status")}>{sender.isEnabled?ui("active"):ui("inactive")}</DetailField></dl>}
  {edit&&<SenderDialog root={root} propertyId={propertyId} sender={sender} onClose={()=>setEdit(false)} onSaved={()=>setRevision(n=>n+1)}/>}
 </section>;
}

function SenderDialog({root,propertyId,sender,onClose,onSaved}:{root:string;propertyId:string;sender:LegalSender|null;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useBusy();
 const [title,setTitle]=useState(sender?.title??""),[mersis,setMersis]=useState(sender?.mersisNumber??""),[contact,setContact]=useState(sender?.contactInformation??""),[keyword,setKeyword]=useState(sender?.optOutKeyword??"RET "),[code,setCode]=useState(sender?.optOutShortCode??""),[optOut,setOptOut]=useState(sender?.optOutEnabled??true),[iys,setIys]=useState(sender?.iysIntegrationEnabled??false),[brand,setBrand]=useState(sender?.iysBrandCode??""),[enabled,setEnabled]=useState(sender?.isEnabled??true),[error,setError]=useState("");
 function save(e:React.FormEvent){e.preventDefault();setError("");const k=keyword.trim().toUpperCase();
  if(!title.trim()||!contact.trim()||!senderMersis(mersis.trim())||!senderKeyword(k)||!senderShortCode(code.trim())||iys&&!brand.trim()){setError(t("senderInvalid"));return;}
  void run(async()=>{try{readSender(await api(root+"/legal-sender",{method:"PUT",body:JSON.stringify({title:title.trim(),mersisNumber:mersis.trim()||null,contactInformation:contact.trim(),optOutKeyword:k,optOutShortCode:code.trim(),optOutEnabled:optOut,iysIntegrationEnabled:iys,iysBrandCode:brand.trim()||null,isEnabled:enabled,version:sender?.version??0})}),propertyId);notice.success(t("senderSaved"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("senderTitle")}</DialogTitle><DialogDescription>{t("senderHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-sender-title">{t("senderLegalTitle")} *</Label><Input id="sms-sender-title" maxLength={200} value={title} onChange={e=>setTitle(e.target.value)}/>
   <Label htmlFor="sms-sender-mersis">{t("senderMersis")}</Label><Input id="sms-sender-mersis" inputMode="numeric" maxLength={16} value={mersis} onChange={e=>setMersis(e.target.value)} aria-describedby="sms-sender-mersis-help"/><p id="sms-sender-mersis-help" className="text-xs text-muted-foreground">{t("senderMersisHelp")}</p>
   <Label htmlFor="sms-sender-contact">{t("senderContact")} *</Label><Input id="sms-sender-contact" maxLength={200} value={contact} onChange={e=>setContact(e.target.value)}/>
   <div className="grid grid-cols-2 gap-3"><div className="space-y-2"><Label htmlFor="sms-sender-keyword">{t("senderKeyword")} *</Label><Input id="sms-sender-keyword" maxLength={20} value={keyword} onChange={e=>setKeyword(e.target.value.toUpperCase())}/></div><div className="space-y-2"><Label htmlFor="sms-sender-code">{t("senderShortCode")} *</Label><Input id="sms-sender-code" inputMode="numeric" maxLength={15} value={code} onChange={e=>setCode(e.target.value)}/></div></div>
   <p className="text-xs text-muted-foreground">{t("senderOptOutHelp")}</p>
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={optOut} onChange={e=>setOptOut(e.target.checked)}/>{t("senderOptOutEnabled")}</label>
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={iys} onChange={e=>setIys(e.target.checked)}/>{t("senderIysEnabled")}</label>
   {iys&&<><Label htmlFor="sms-sender-brand">{t("senderIysBrand")} *</Label><Input id="sms-sender-brand" maxLength={20} value={brand} onChange={e=>setBrand(e.target.value)}/></>}
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={enabled} onChange={e=>setEnabled(e.target.checked)}/>{ui("active")}</label>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

export function PreviewTab({root}:{root:string}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();const {busy,run}=useBusy();
 const [type,setType]=useState("MARKETING"),[body,setBody]=useState(""),[phone,setPhone]=useState(""),[result,setResult]=useState<SmsPreview|null>(null),[error,setError]=useState("");
 function check(e:React.FormEvent){e.preventDefault();setError("");const number=phone.trim()?formatPhone(phone):"";if(!body.trim()||number&&!validPhone(number)){setError(t("previewInvalid"));return;}
  void run(async()=>{try{setResult(readPreview(await api(root+"/preview",{method:"POST",body:JSON.stringify({typeCode:type,body:body.trim(),phoneNumber:number||null})})));}catch(reason){setResult(null);setError(errorMessage(reason,locale));}});}
 return <div className="grid gap-4 lg:grid-cols-2"><section className="rounded-2xl border bg-card p-5 shadow-sm"><h2 className="flex items-center gap-2 font-semibold"><Eye className="size-4" aria-hidden="true"/>{t("previewTitle")}</h2><p className="mt-2 text-sm text-muted-foreground">{t("previewHelp")}</p>
  <form onSubmit={check} className="mt-4 space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-preview-type">{t("type")}</Label><select id="sms-preview-type" className={toolbarSelectClass+" w-full"} value={type} onChange={e=>{setType(e.target.value);setResult(null);}}>{["MARKETING","MANUAL"].map(code=><option key={code} value={code}>{t(`type_${code}`)}</option>)}</select>
   <Label htmlFor="sms-preview-body">{t("message")} *</Label><Textarea id="sms-preview-body" rows={4} maxLength={1000} value={body} onChange={e=>{setBody(e.target.value);setResult(null);}}/>
   <Label htmlFor="sms-preview-phone">{t("previewPhone")}</Label><PhoneInput id="sms-preview-phone" autoComplete="off" value={phone} onValueChange={v=>{setPhone(v);setResult(null);}}/>
  </fieldset><Button type="submit" disabled={busy}><Eye/>{busy?ui("loading"):t("previewCheck")}</Button></form></section>
  <section aria-live="polite" className="rounded-2xl border bg-card p-5 shadow-sm"><h2 className="font-semibold">{t("preview")}</h2>
   {!result?<p className="mt-2 text-sm text-muted-foreground">{t("previewEmpty")}</p>:<div className="mt-3 space-y-3">
    <ToneBadge tone={result.allowed?"success":"error"} icon={result.allowed?CheckCircle2:Ban}>{result.allowed?t("previewAllowed"):t.has(`blocked_${result.blockedCode}`)?t(`blocked_${result.blockedCode}`):result.blockedCode}</ToneBadge>
    <p className="whitespace-pre-wrap break-words rounded-lg border p-3 text-sm">{result.body}</p>
    <p className="text-xs text-muted-foreground">{t("segmentInfo",{encoding:result.encoding==="GSM7"?"GSM-7":"UCS-2",length:result.characters,count:result.segments})}</p>
    {result.footer===""&&type!=="MARKETING"&&<p className="text-xs text-muted-foreground">{t("previewNoFooter")}</p>}
   </div>}</section></div>;
}

export function ConsentsTab({root}:{root:string}){
 const t=useTranslations("sms"),locale=useLocale(),api=useAuthorizedApi();const [data,setData]=useState<Page<ConsentRow>|null>(null),[page,setPage]=useState(1),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0),[optOut,setOptOut]=useState(false);
 useEffect(()=>{const c=new AbortController();setLoading(true);setError("");void api<unknown>(`${root}/commercial-consents?page=${page}&pageSize=10`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,consentRow));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,root,page,retry,locale]);
 const columns=useMemo<ColumnDef<ConsentRow>[]>(()=>[{id:"date",header:t("date"),cell:({row})=>formatDateTime(row.original.occurredAt,locale)},{id:"phone",header:t("phone"),cell:({row})=><span className="font-mono">{row.original.recipientMasked}</span>},{id:"status",header:t("consentDecision"),cell:({row})=><Decision record={row.original}/>},{id:"source",header:t("consentSource"),cell:({row})=>t(`source_${row.original.source}`)},{id:"iys",header:t("senderIys"),cell:({row})=>t(`iys_${row.original.iysStatus}`)}],[t,locale]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="flex flex-wrap items-center justify-between gap-2 p-4"><p className="text-sm text-muted-foreground">{t("consentsHelp")}</p><div className="flex gap-2"><Button variant="view" onClick={()=>setRetry(n=>n+1)}><RefreshCw/>{t("refresh")}</Button><Button variant="warn" onClick={()=>setOptOut(true)}><Ban/>{t("optOutRecord")}</Button></div></div>
  <RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t("noConsents")} card={r=><><div className="flex items-center justify-between gap-2"><span className="font-mono">{r.recipientMasked}</span><Decision record={r}/></div><p className="text-xs">{formatDateTime(r.occurredAt,locale)} · {t(`source_${r.source}`)}</p></>}/>
  {optOut&&<OptOutDialog root={root} onClose={()=>setOptOut(false)} onSaved={()=>{setPage(1);setRetry(n=>n+1);}}/>}</section>;
}

function Decision({record}:{record:ConsentRecord}){const t=useTranslations("sms");const s=decisionStyle[record.status];return <ToneBadge tone={s.tone} icon={s.icon}>{t(`decision_${record.status}`)}</ToneBadge>;}

function OptOutDialog({root,onClose,onSaved}:{root:string;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useBusy();
 const [phone,setPhone]=useState(""),[error,setError]=useState("");const request=useRef<{key:string;id:string}|null>(null);
 function save(e:React.FormEvent){e.preventDefault();setError("");const number=formatPhone(phone);if(!validPhone(number)){setError(t("sendInvalid"));return;}
  if(request.current?.key!==number)request.current={key:number,id:crypto.randomUUID()};const id=request.current.id;
  void run(async()=>{try{readRecord(await api(root+"/opt-outs",{method:"POST",body:JSON.stringify({operationId:id,phoneNumber:number})}));notice.success(t("optOutSaved"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("optOutRecord")}</DialogTitle><DialogDescription>{t("optOutHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-optout-phone">{t("phone")} *</Label><PhoneInput id="sms-optout-phone" autoComplete="off" required value={phone} onValueChange={setPhone}/>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" variant="destructive" disabled={busy}><Ban/>{busy?ui("saving"):t("optOutRecord")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}
