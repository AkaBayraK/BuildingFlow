"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {BellRing,BellOff,Wrench,BookOpenCheck,Building2,CheckCircle2,CircleDashed,History,Smartphone,Undo2,XCircle,type LucideIcon} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {ToneBadge,type Tone} from "@/components/management/WorkspacePresentation";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {formatDateTime} from "@/lib/format";
import {readPage} from "@/features/sms/model";
import {approveCommand,readOperational,readPreferenceItem,readPreferences,readRecord,textMatches,withdrawCommand,type ConsentRecord,type PreferenceItem,type Preferences} from "@/features/sms/compliance";
import {sha256Hex} from "./photo";

const root="/api/v1/account/communication-preferences";
const statusStyle:Record<PreferenceItem["status"],{tone:Tone;icon:LucideIcon}>={APPROVED:{tone:"success",icon:CheckCircle2},REJECTED:{tone:"error",icon:XCircle},NONE:{tone:"info",icon:CircleDashed}};

// Marketing SMS approval is separate from KVKK consents and optional; service SMS (dues, maintenance, security) does not depend on it.
export function CommunicationPreferences(){
 const t=useTranslations("account"),locale=useLocale(),api=useAuthorizedApi(),notify=useNotification();
 const [data,setData]=useState<Preferences|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0),[busy,setBusy]=useState(false),[approve,setApprove]=useState<PreferenceItem|null>(null),[history,setHistory]=useState<PreferenceItem|null>(null);
 const operations=useRef(new Map<string,{key:string;id:string}>());
 useEffect(()=>{const c=new AbortController();setError("");void api<unknown>(root,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPreferences(v));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,revision,locale]);
 // One UUID per sender and decision, so a retried uncertain request is a replay rather than a second decision.
 function operation(sender:string,key:string){const last=operations.current.get(sender);if(last?.key===key)return last.id;const id=crypto.randomUUID();operations.current.set(sender,{key,id});return id;}
 async function decide(item:PreferenceItem,approved:boolean){if(!data||busy)return false;setBusy(true);
  try{const id=operation(item.senderId,approved?"APPROVE:"+data.text.bodySha256:"WITHDRAW");
   const saved=readPreferenceItem(await api(`${root}/${item.senderId}`,{method:"PUT",body:JSON.stringify(approved?approveCommand(data.text,id):withdrawCommand(id))}));
   operations.current.delete(item.senderId);setData({...data,items:data.items.map(i=>i.senderId===saved.senderId?saved:i)});notify.success(t(approved?"marketingApproved":"marketingWithdrawn"));return true;}
  catch(reason){notify.error(errorMessage(reason,locale));setRevision(n=>n+1);return false;}finally{setBusy(false);}}
 async function operational(enabled:boolean){if(!data||busy)return;setBusy(true);
  try{const saved=readOperational(await api(`${root}/operational`,{method:"PUT",body:JSON.stringify({enabled})}));setData({...data,operationalSmsEnabled:saved});notify.success(t(saved?"operationalOn":"operationalOff"));}
  catch(reason){notify.error(errorMessage(reason,locale));setRevision(n=>n+1);}finally{setBusy(false);}}
 return <section aria-labelledby="account-communication" className="rounded-2xl border bg-card p-5 shadow-sm lg:col-span-2">
  <h2 id="account-communication" className="flex items-center gap-2 text-base font-semibold"><BellRing className="size-4" aria-hidden="true"/>{t("communicationTitle")}</h2>
  <p className="mt-2 text-sm text-muted-foreground">{t("communicationHint")}</p>
  {error&&<div className="mt-3"><MessageBanner presentation="inline" action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{t("retry")}</Button>}>{error}</MessageBanner></div>}
  {!data&&!error&&<Skeleton className="mt-3 h-16"/>}
  {data&&<>{!data.verifiedPhoneMasked?<div className="mt-3"><MessageBanner presentation="inline">{t("marketingNeedsPhone")}</MessageBanner></div>:<p className="mt-3 flex items-center gap-2 text-sm"><Smartphone className="size-4" aria-hidden="true"/>{t("marketingPhone",{phone:data.verifiedPhoneMasked})}</p>}
   <div className="mt-3 flex flex-wrap items-center justify-between gap-2 rounded-lg border p-3"><span className="flex items-center gap-2"><Wrench className="size-4" aria-hidden="true"/><span><span className="block font-medium">{t("operationalTitle")}</span><small className="text-muted-foreground">{t("operationalHint")}</small></span></span>
    <span className="flex items-center gap-2"><ToneBadge tone={data.operationalSmsEnabled?"success":"warning"} icon={data.operationalSmsEnabled?CheckCircle2:BellOff}>{t(data.operationalSmsEnabled?"operationalEnabled":"operationalDisabled")}</ToneBadge>
     <Button size="sm" variant={data.operationalSmsEnabled?"warn":"default"} disabled={busy||!data.verifiedPhoneMasked} onClick={()=>void operational(!data.operationalSmsEnabled)}>{data.operationalSmsEnabled?<><BellOff/>{t("operationalTurnOff")}</>:<><BellRing/>{t("operationalTurnOn")}</>}</Button></span></div>
   {data.items.length===0?<p className="mt-3 text-sm text-muted-foreground">{t("marketingNoSenders")}</p>:<ul className="mt-3 grid gap-2 md:grid-cols-2">{data.items.map(item=>{const s=statusStyle[item.status];
    return <li key={item.senderId} className="flex flex-col gap-2 rounded-lg border p-3">
     <span className="flex flex-wrap items-center justify-between gap-2"><span className="flex items-center gap-2 font-medium"><Building2 className="size-4" aria-hidden="true"/>{item.senderTitle}</span><ToneBadge tone={s.tone} icon={s.icon}>{t(`marketing_${item.status}`)}</ToneBadge></span>
     <small className="text-muted-foreground">{t(item.scope==="PLATFORM"?"marketingScopePlatform":"marketingScopeProperty")}{item.occurredAt?" · "+formatDateTime(item.occurredAt,locale):""}</small>
     <span className="flex flex-wrap gap-2">{item.status!=="APPROVED"&&<Button size="sm" disabled={busy||!data.verifiedPhoneMasked} onClick={()=>setApprove(item)}><BookOpenCheck/>{t("marketingApprove")}</Button>}
      {item.status==="APPROVED"&&<Button size="sm" variant="danger" disabled={busy} onClick={()=>void decide(item,false)}><Undo2/>{t("withdraw")}</Button>}
      {item.status!=="NONE"&&<Button size="sm" variant="view" disabled={busy} onClick={()=>setHistory(item)}><History/>{t("marketingHistory")}</Button>}</span>
    </li>;})}</ul>}</>}
  {approve&&data&&<ApproveDialog text={data.text} busy={busy} sender={approve.senderTitle} onApprove={()=>void decide(approve,true).then(ok=>{if(ok)setApprove(null);})} onClose={()=>{if(!busy)setApprove(null);}}/>}
  {history&&<HistoryDialog item={history} onClose={()=>setHistory(null)}/>}
 </section>;
}

function ApproveDialog({text,sender,busy,onApprove,onClose}:{text:Preferences["text"];sender:string;busy:boolean;onApprove:()=>void;onClose:()=>void}){
 const t=useTranslations("account");const [checked,setChecked]=useState(false),[intact,setIntact]=useState<boolean|null>(null);
 useEffect(()=>{let live=true;void textMatches(text,sha256Hex).then(ok=>{if(live)setIntact(ok);}).catch(()=>{if(live)setIntact(false);});return()=>{live=false;};},[text]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><DialogContent className="sm:max-w-xl"><DialogHeader><DialogTitle>{text.title}</DialogTitle><DialogDescription>{t("marketingTextVersion",{version:text.version,sender})}</DialogDescription></DialogHeader>
  {intact===false&&<MessageBanner presentation="inline">{t("textNotVerified")}</MessageBanner>}
  <p className="whitespace-pre-wrap rounded-xl border p-4 text-sm">{text.body}</p>
  {intact&&<label className="flex items-start gap-3 text-sm font-medium"><input type="checkbox" className="mt-1 size-4" checked={checked} disabled={busy} onChange={e=>setChecked(e.target.checked)}/>{t("marketingTick")}</label>}
  <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{t("close")}</Button><Button disabled={!checked||!intact||busy} onClick={onApprove}>{busy?t("saving"):t("approve")}</Button></DialogFooter></DialogContent></Dialog>;
}

function HistoryDialog({item,onClose}:{item:PreferenceItem;onClose:()=>void}){
 const t=useTranslations("account"),sms=useTranslations("sms"),locale=useLocale(),api=useAuthorizedApi();const [rows,setRows]=useState<ConsentRecord[]|null>(null),[error,setError]=useState("");
 useEffect(()=>{const c=new AbortController();void api<unknown>(`${root}/${item.senderId}/history?page=1&pageSize=20`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setRows(readPage(v,readRecord).items);}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,item.senderId,locale]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><DialogContent className="sm:max-w-xl"><DialogHeader><DialogTitle>{t("marketingHistory")}</DialogTitle><DialogDescription>{item.senderTitle}</DialogDescription></DialogHeader>
  {error&&<MessageBanner presentation="inline">{error}</MessageBanner>}{!rows&&!error&&<Skeleton className="h-16"/>}
  {rows&&<ol className="space-y-2">{rows.map(r=>{const s=statusStyle[r.status];return <li key={r.recordId} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border p-3 text-sm"><ToneBadge tone={s.tone} icon={s.icon}>{t(`marketing_${r.status}`)}</ToneBadge><span className="text-muted-foreground">{formatDateTime(r.occurredAt,locale)} · {sms(`source_${r.source}`)}</span></li>;})}</ol>}
  <DialogFooter><Button variant="outline" onClick={onClose}>{t("close")}</Button></DialogFooter></DialogContent></Dialog>;
}
