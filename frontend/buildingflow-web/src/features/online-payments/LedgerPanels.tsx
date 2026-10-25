"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {AlertTriangle,ArrowDownLeft,ArrowUpRight,BookOpenText,CircleCheck,FileDown,ListChecks,LoaderCircle,Play,ReceiptText,RefreshCw,Scale} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {useRows} from "@/features/people/useRows";
import {toCsv} from "@/features/late-penalties/model";
import {businessToday,formatDateOnly,formatDateTime,type Locale} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {RecordList} from "@/components/management/RecordList";
import {SummaryCard,ToneBadge} from "@/components/management/WorkspacePresentation";
import {FilterChips} from "@/components/management/FilterChips";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {issueStatuses,readIssue,readIssuePage,readLedgerPage,readRun,readSettlement,type LedgerEntry,type LedgerPage,type ReconciliationIssue,type SettlementReport} from "./model";
import {issueStatusStyle,issueStyle,ledgerStyle} from "./payment-style";
import {Money,ReceiptDialog} from "./OnlinePaymentsPanel";

const rootOf=(propertyId:string)=>`/api/v1/properties/${encodeURIComponent(propertyId)}`;
function download(name:string,content:string){const url=URL.createObjectURL(new Blob([content],{type:"text/csv;charset=utf-8"}));const link=document.createElement("a");link.href=url;link.download=name;link.click();URL.revokeObjectURL(url);}

export function LedgerPanel({propertyId,revision}:{propertyId:string;revision:number}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;
 const [from,setFrom]=useState("");const [to,setTo]=useState("");const [page,setPage]=useState(1);const [local,setLocal]=useState(0);const [receipt,setReceipt]=useState<string|null>(null);
 useEffect(()=>setPage(1),[from,to]);
 const invalid=!!(from&&to&&from>to);
 const query=new URLSearchParams({page:String(page),pageSize:"20"});if(from)query.set("from",from);if(to)query.set("to",to);
 const rows=useRows<LedgerEntry>(rootOf(propertyId)+"/payment-ledger?"+query,revision+local,!invalid,readLedgerPage);
 const data=rows.data as LedgerPage|null;
 const exportCsv=()=>data&&download(`defter-${propertyId}-${businessToday()}.csv`,toCsv([[t("occurredOn"),ui("unit"),t("entryType"),t("method"),t("amount"),t("receiptNumber")],
  ...data.items.map(e=>[e.occurredOn,e.unitNumber,t("ledger_"+e.type),t("method_"+e.method),e.amount,e.receiptNumber??""])]));
 const columns:ColumnDef<LedgerEntry>[]=[
  {id:"date",header:t("occurredOn"),cell:({row})=><div className="text-sm">{formatDateOnly(row.original.occurredOn,locale)}<span className="block text-xs text-muted-foreground">{formatDateTime(row.original.createdAt,locale)}</span></div>},
  {id:"unit",header:ui("unit"),cell:({row})=><strong>{row.original.unitNumber}</strong>},
  {id:"type",header:t("entryType"),cell:({row})=>{const style=ledgerStyle[row.original.type];return <ToneBadge tone={style.tone} icon={style.icon}>{t("ledger_"+row.original.type)}</ToneBadge>;}},
  {id:"method",header:t("method"),cell:({row})=>t("method_"+row.original.method)},
  {id:"amount",header:t("amount"),cell:({row})=><strong className={row.original.amount.startsWith("-")?"text-destructive":undefined}><Money value={row.original.amount}/></strong>},
  {id:"receipt",header:t("receiptNumber"),cell:({row})=>row.original.receiptNumber&&row.original.paymentId?<Button variant="view" size="sm" onClick={()=>setReceipt(row.original.paymentId)}><ReceiptText/>{row.original.receiptNumber}</Button>:"—"}
 ];
 return <div className="space-y-6">
  <div className="grid gap-4 sm:grid-cols-3">
   <SummaryCard title={t("credits")} value={rows.loading?"…":data?<Money value={data.totals.credits}/>:"—"} note={t("ledgerScope")} icon={ArrowDownLeft}/>
   <SummaryCard title={t("debits")} value={rows.loading?"…":data?<Money value={data.totals.debits}/>:"—"} note={t("ledgerScope")} icon={ArrowUpRight}/>
   <SummaryCard title={t("net")} value={rows.loading?"…":data?<Money value={data.totals.net}/>:"—"} note={t("ledgerScope")} icon={Scale}/>
  </div>
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-labelledby="ledger-title">
   <div className="flex flex-wrap items-end justify-between gap-3 border-b p-5"><h2 id="ledger-title" className="flex items-center gap-2 font-semibold"><BookOpenText className="size-5 text-primary"/>{t("ledger")}</h2>
    <div className="flex flex-wrap items-end gap-2"><div><Label htmlFor="ledger-from">{t("from")}</Label><Input id="ledger-from" type="date" value={from} onInput={event=>setFrom(event.currentTarget.value)}/></div><div><Label htmlFor="ledger-to">{t("to")}</Label><Input id="ledger-to" type="date" min={from||undefined} value={to} aria-invalid={invalid} onInput={event=>setTo(event.currentTarget.value)}/></div><Button variant="outline" disabled={!data?.items.length} onClick={exportCsv}><FileDown/>{t("exportCsv")}</Button></div></div>
   {invalid&&<div className="px-5 pt-3"><FieldError>{t("dateRangeInvalid")}</FieldError></div>}
   <p className="px-5 pt-3 text-xs text-muted-foreground">{t("ledgerHelp")}</p>
   <RecordList compact data={data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setLocal(n=>n+1)} empty={<><BookOpenText className="mx-auto mb-3 size-8"/><strong>{t("noLedger")}</strong></>}
    card={entry=>{const style=ledgerStyle[entry.type];return <><div className="mb-2 flex flex-wrap justify-between gap-2"><strong>{entry.unitNumber}</strong><ToneBadge tone={style.tone} icon={style.icon}>{t("ledger_"+entry.type)}</ToneBadge></div><p className="text-sm">{formatDateOnly(entry.occurredOn,locale)} · {t("method_"+entry.method)}</p><p className="my-2 text-lg font-semibold"><Money value={entry.amount}/></p>{entry.receiptNumber&&entry.paymentId&&<Button variant="view" size="sm" onClick={()=>setReceipt(entry.paymentId)}><ReceiptText/>{entry.receiptNumber}</Button>}</>;}}/>
  </section>
  {receipt&&<ReceiptDialog propertyId={propertyId} paymentId={receipt} onClose={()=>setReceipt(null)}/>}
 </div>;
}

export function ReconciliationPanel({propertyId,enabled,canPropose,revision,onChanged}:{propertyId:string;enabled:boolean;canPropose:boolean;revision:number;onChanged:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [status,setStatus]=useState("OPEN");const [page,setPage]=useState(1);const [local,setLocal]=useState(0);const [date,setDate]=useState(businessToday);const [running,setRunning]=useState(false);
 const [from,setFrom]=useState("");const [to,setTo]=useState("");const [settlement,setSettlement]=useState<SettlementReport|null>(null);const [settlementError,setSettlementError]=useState("");
 const [resolving,setResolving]=useState<string|null>(null);const [note,setNote]=useState("");const [noteError,setNoteError]=useState("");const [busy,setBusy]=useState(false);const lock=useRef(false);
 useEffect(()=>setPage(1),[status]);
 const query=new URLSearchParams({page:String(page),pageSize:"20"});if(status)query.set("status",status);
 const rows=useRows<ReconciliationIssue>(rootOf(propertyId)+"/payment-reconciliation/items?"+query,revision+local,true,readIssuePage);
 useEffect(()=>{
  if(from&&to&&from>to)return;const controller=new AbortController();setSettlementError("");const params=new URLSearchParams();if(from)params.set("from",from);if(to)params.set("to",to);
  void api<unknown>(rootOf(propertyId)+"/payment-reconciliation/settlement?"+params,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setSettlement(readSettlement(value,propertyId));}).catch(err=>{if(!controller.signal.aborted)setSettlementError(errorMessage(err,locale));});
  return()=>controller.abort();
 },[api,propertyId,from,to,locale,revision,local]);
 async function run(){
  if(lock.current)return;if(!date||date>businessToday()){notice.error(t("runDateInvalid"));return;}lock.current=true;setRunning(true);
  try{const result=readRun(await api<unknown>(rootOf(propertyId)+"/payment-reconciliation/run",{method:"POST",body:JSON.stringify({date})}));notice.success(t("runDone",{checked:result.checked,settled:result.settled,expired:result.expired,issues:result.issuesOpened}));setLocal(n=>n+1);onChanged();}
  catch(err){notice.error(errorMessage(err,locale));}finally{lock.current=false;setRunning(false);}
 }
 async function resolve(event:React.FormEvent,issue:ReconciliationIssue){
  event.preventDefault();if(lock.current)return;if(!note.trim()){setNoteError(t("reasonRequired"));return;}lock.current=true;setBusy(true);
  try{readIssue(await api<unknown>(rootOf(propertyId)+"/payment-reconciliation/items/"+issue.id+"/resolve",{method:"PUT",body:JSON.stringify({note:note.trim(),version:issue.version})}));notice.success(t("issueResolved"));setResolving(null);setNote("");setLocal(n=>n+1);onChanged();}
  catch(err){notice.error(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)setLocal(n=>n+1);}finally{lock.current=false;setBusy(false);}
 }
 const actions=(issue:ReconciliationIssue)=>issue.status==="OPEN"&&canPropose?(resolving===issue.id?<form noValidate className="min-w-56 space-y-2" onSubmit={event=>void resolve(event,issue)}><Label htmlFor={"issue-note-"+issue.id}>{t("resolutionNote")}</Label><Textarea id={"issue-note-"+issue.id} maxLength={500} value={note} aria-invalid={!!noteError} onChange={event=>{setNote(event.target.value);setNoteError("");}}/>{noteError&&<FieldError>{noteError}</FieldError>}<div className="flex gap-2"><Button type="button" size="sm" variant="outline" disabled={busy} onClick={()=>setResolving(null)}>{ui("cancel")}</Button><Button type="submit" size="sm" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<CircleCheck/>}{t("resolve")}</Button></div></form>
  :<Button size="sm" variant="view" disabled={!enabled} onClick={()=>{setResolving(issue.id);setNote("");setNoteError("");}}><CircleCheck/>{t("resolve")}</Button>):issue.resolutionNote?<span className="block max-w-56 wrap-anywhere text-xs text-muted-foreground">{issue.resolutionNote}</span>:null;
 const columns:ColumnDef<ReconciliationIssue>[]=[
  {id:"type",header:t("issueType"),cell:({row})=>{const style=issueStyle[row.original.type];return <ToneBadge tone={style.tone} icon={style.icon}>{t("issue_"+row.original.type)}</ToneBadge>;}},
  {id:"detail",header:t("detail"),cell:({row})=><span className="block max-w-80 wrap-anywhere text-sm">{row.original.detail}</span>},
  {id:"created",header:t("openedAt"),cell:({row})=><span className="text-sm">{formatDateTime(row.original.createdAt,locale)}</span>},
  {id:"status",header:ui("status"),cell:({row})=>{const style=issueStatusStyle[row.original.status];return <ToneBadge tone={style.tone} icon={style.icon}>{t("issueStatus_"+row.original.status)}</ToneBadge>;}},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}
 ];
 const chips=[{value:"",label:t("allStatuses"),icon:ListChecks},...issueStatuses.map(value=>({value,label:t("issueStatus_"+value),icon:issueStatusStyle[value].icon}))];
 const sum=(key:"collected"|"fees"|"refunded"|"chargedBack"|"net")=>settlement?settlement.days.reduce((total,day)=>total+BigInt(day[key].replace(".","")),BigInt(0)):null;
 const cents=(value:bigint|null)=>{if(value===null)return "—";const negative=value<BigInt(0);const abs=negative?-value:value;return <Money value={`${negative?"-":""}${abs/BigInt(100)}.${(abs%BigInt(100)).toString().padStart(2,"0")}`}/>;};
 return <div className="space-y-6">
  <section className="rounded-2xl border bg-card p-5 shadow-sm" aria-labelledby="settlement-title">
   <div className="flex flex-wrap items-end justify-between gap-3"><h2 id="settlement-title" className="flex items-center gap-2 font-semibold"><Scale className="size-5 text-primary"/>{t("settlement")}</h2>
    <div className="flex flex-wrap items-end gap-2"><div><Label htmlFor="settlement-from">{t("from")}</Label><Input id="settlement-from" type="date" value={from} onInput={event=>setFrom(event.currentTarget.value)}/></div><div><Label htmlFor="settlement-to">{t("to")}</Label><Input id="settlement-to" type="date" min={from||undefined} max={businessToday()} value={to} onInput={event=>setTo(event.currentTarget.value)}/></div></div></div>
   {settlementError&&<MessageBanner action={<Button variant="view" onClick={()=>setLocal(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{settlementError}</MessageBanner>}
   {settlement&&<><p className="mt-2 text-xs text-muted-foreground">{t("settlementRange",{from:formatDateOnly(settlement.from,locale),to:formatDateOnly(settlement.to,locale)})}</p>
    <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">{(["collected","fees","refunded","chargedBack","net"] as const).map(key=><div key={key} className="rounded-xl border p-3"><p className="text-xs text-muted-foreground">{t("settle_"+key)}</p><p className="mt-1 text-lg font-semibold">{cents(sum(key))}</p></div>)}</div>
    {settlement.openIssues>0&&<MessageBanner kind="warning" presentation="inline"><AlertTriangle className="inline size-4" aria-hidden="true"/> {t("openIssuesWarning",{count:settlement.openIssues})}</MessageBanner>}
    {settlement.days.length>0&&<div className="mt-4 overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">{t("settlement")}</caption><thead className="text-xs text-muted-foreground"><tr><th className="py-1 pr-3">{t("date")}</th><th className="pr-3">{t("count")}</th>{(["collected","fees","refunded","chargedBack","net"] as const).map(key=><th key={key} className="pr-3">{t("settle_"+key)}</th>)}</tr></thead>
     <tbody>{settlement.days.map(day=><tr key={day.date} className="border-t"><td className="py-1 pr-3">{formatDateOnly(day.date,locale)}</td><td className="pr-3">{day.paymentCount}</td>{(["collected","fees","refunded","chargedBack","net"] as const).map(key=><td key={key} className="pr-3"><Money value={day[key]}/></td>)}</tr>)}</tbody></table></div>}
   </>}
  </section>
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-labelledby="issues-title">
   <div className="flex flex-wrap items-end justify-between gap-3 border-b p-5"><h2 id="issues-title" className="flex items-center gap-2 font-semibold"><ListChecks className="size-5 text-primary"/>{t("issues")}</h2>
    {canPropose&&<div className="flex flex-wrap items-end gap-2"><div><Label htmlFor="reconcile-date">{t("reconcileDate")}</Label><Input id="reconcile-date" type="date" min="2000-01-01" max={businessToday()} value={date} onInput={event=>setDate(event.currentTarget.value)}/></div><Button disabled={!enabled||running} onClick={()=>void run()}>{running?<LoaderCircle className="animate-spin"/>:<Play/>}{t("runReconciliation")}</Button></div>}</div>
   <div className="space-y-2 p-5 pb-0"><FilterChips label={ui("status")} items={chips} value={status} onChange={setStatus}/><p className="text-xs text-muted-foreground">{t("reconciliationHelp")}</p></div>
   <RecordList compact data={rows.data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setLocal(n=>n+1)} empty={<><CircleCheck className="mx-auto mb-3 size-8"/><strong>{t("noIssues")}</strong></>}
    card={issue=>{const style=issueStyle[issue.type];return <><div className="mb-2 flex flex-wrap gap-2"><ToneBadge tone={style.tone} icon={style.icon}>{t("issue_"+issue.type)}</ToneBadge></div><p className="mb-2 wrap-anywhere text-sm">{issue.detail}</p><p className="mb-3 text-xs text-muted-foreground">{formatDateTime(issue.createdAt,locale)}</p>{actions(issue)}</>;}}/>
  </section>
 </div>;
}
