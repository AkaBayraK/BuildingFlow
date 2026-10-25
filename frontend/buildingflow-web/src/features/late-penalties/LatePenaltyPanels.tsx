"use client";
import {useCallback,useEffect,useMemo,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {BadgeCheck,Ban,CalendarX2,Eye,FileDown,Gavel,HandCoins,Hourglass,Landmark,LoaderCircle,Play,Plus,Printer,RefreshCw,Save,Scale,Search,ShieldOff,ToggleLeft,ToggleRight} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {useRows} from "@/features/people/useRows";
import {readContentPage,readDocument,type DocumentRecord} from "@/features/communication/model";
import {submissionKey,type SubmissionKey} from "@/features/dues/model";
import {businessToday,formatDateOnly,formatDateTime,formatDecimalCurrency,type Locale} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {RecordList} from "@/components/management/RecordList";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {accrualStyle,policyStyle} from "./penalty-style";
import {normalizeRate,readAccrual,readChargeDetail,readPenaltyPage,readPolicy,readReport,readRunResult,readSettings,readSwitch,toCsv,
 type PenaltyChargeDetail,type PenaltyMethod,type PenaltyPage,type PenaltyPolicy,type PenaltyReport,type PenaltyRow,type PenaltySegment,type PenaltySettings} from "./model";

const root=(propertyId:string)=>`/api/v1/properties/${encodeURIComponent(propertyId)}/late-penalties`;
function download(name:string,content:string){const url=URL.createObjectURL(new Blob([content],{type:"text/csv;charset=utf-8"}));const link=document.createElement("a");link.href=url;link.download=name;link.click();URL.revokeObjectURL(url);}
function Money({value}:{value:string}){const locale=useLocale() as Locale;return <>{formatDecimalCurrency(value,locale)}</>;}

function useSettings(propertyId:string,revision:number){
 const api=useAuthorizedApi();const locale=useLocale();const [data,setData]=useState<PenaltySettings|null>(null);const [error,setError]=useState("");
 useEffect(()=>{const controller=new AbortController();setError("");void api<unknown>(root(propertyId)+"/settings",{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setData(readSettings(value,propertyId));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));});return()=>controller.abort();},[api,propertyId,revision,locale]);
 return {data,error};
}

function SegmentTable({segments}:{segments:PenaltySegment[]}){
 const t=useTranslations("latePenalties");const locale=useLocale() as Locale;
 if(!segments.length)return <p className="text-sm text-muted-foreground">{t("noSegments")}</p>;
 return <div className="overflow-x-auto"><table className="w-full text-left text-xs"><thead><tr className="text-muted-foreground"><th className="py-1 pr-2">{t("range")}</th><th className="pr-2">{t("days")}</th><th className="pr-2">{t("principal")}</th><th className="pr-2">{t("rate")}</th><th className="pr-2">{t("method")}</th><th>{t("amount")}</th></tr></thead>
  <tbody>{segments.map(s=><tr key={s.from} className="border-t"><td className="py-1 pr-2">{formatDateOnly(s.from,locale)} – {formatDateOnly(s.to,locale)}</td><td className="pr-2">{s.days}</td><td className="pr-2"><Money value={s.principal}/></td><td className="pr-2">%{s.monthlyRate}{s.policyId?"":" · "+t("statutoryShort")}</td><td className="pr-2">{t("method_"+s.method)}</td><td>{s.amount}</td></tr>)}</tbody></table></div>;
}

export function ChargePenaltyPanel({propertyId,chargeId,canWaive,onClose,onChanged}:{propertyId:string;chargeId:string;canWaive:boolean;onClose:()=>void;onChanged:()=>void}){
 const t=useTranslations("latePenalties");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [detail,setDetail]=useState<PenaltyChargeDetail|null>(null);const [error,setError]=useState("");const [busy,setBusy]=useState(true);const [revision,setRevision]=useState(0);
 const [waiving,setWaiving]=useState<string|null>(null);const [reason,setReason]=useState("");const [reasonError,setReasonError]=useState("");const lock=useRef(false);
 useEffect(()=>{const controller=new AbortController();setBusy(true);setError("");void api<unknown>(root(propertyId)+"/charges/"+chargeId,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setDetail(readChargeDetail(value,chargeId));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setBusy(false);});return()=>controller.abort();},[api,propertyId,chargeId,revision,locale]);
 async function waive(event:React.FormEvent,id:string,version:number){
  event.preventDefault();if(lock.current)return;if(!reason.trim()){setReasonError(t("reasonRequired"));return;}
  lock.current=true;setBusy(true);setError("");
  try{readAccrual(await api<unknown>(root(propertyId)+"/accruals/"+id+"/waive",{method:"PUT",body:JSON.stringify({version,reason:reason.trim()})}));notice.success(t("waived"));setWaiving(null);setReason("");setRevision(n=>n+1);onChanged();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError)setReasonError(err.errors.reason?.join(" ")??"");}
  finally{lock.current=false;setBusy(false);}
 }
 const rows:[string,string][]=detail?[["principal",detail.principal],["principalPaid",detail.principalPaid],["principalBalance",detail.principalBalance],["penaltyAccrued",detail.penaltyAccrued],["penaltyPaid",detail.penaltyPaid],["penaltyWaived",detail.penaltyWaived],["penaltyBalance",detail.penaltyBalance],["pendingPenalty",detail.pendingPenalty],["totalDue",detail.totalDue]]:[];
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><Hourglass className="size-8 text-primary"/><DialogTitle>{t("detailTitle")}</DialogTitle><DialogDescription>{detail?`${detail.unitNumber} · ${detail.period} · ${t("dueDate")}: ${formatDateOnly(detail.dueDate,locale)}`:ui("loading")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner action={<Button variant="view" disabled={busy} onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
  {busy&&!detail&&<p role="status">{ui("loading")}</p>}
  {detail&&<>
   <ToneBadge tone={detail.appliedToday?"success":"neutral"} icon={detail.appliedToday?ToggleRight:ToggleLeft}>{t(detail.appliedToday?"applied":"notApplied")}</ToneBadge>
   <dl className="grid grid-cols-2 gap-2 text-sm">{rows.map(([key,value])=><div key={key} className="contents"><dt className="text-muted-foreground">{t(key)}</dt><dd className={key==="totalDue"?"font-semibold":undefined}><Money value={value}/></dd></div>)}</dl>
   <MessageBanner kind="info" presentation="inline">{t("pendingHelp",{date:formatDateOnly(detail.asOf,locale)})}</MessageBanner>
   <section className="space-y-3"><h3 className="text-base font-semibold">{t("accruals")}</h3>{detail.accruals.length?detail.accruals.map(accrual=>{const style=accrualStyle[accrual.status];return <article key={accrual.id} className="space-y-2 rounded-xl border p-3">
    <div className="flex flex-wrap items-center justify-between gap-2"><strong className="text-sm">{formatDateOnly(accrual.from,locale)} – {formatDateOnly(accrual.to,locale)}</strong><ToneBadge tone={style.tone} icon={style.icon}>{t("accrual_"+accrual.status)}</ToneBadge></div>
    <p className="text-sm">{t("amount")}: <Money value={accrual.amount}/> · {t("paid")}: <Money value={accrual.paid}/> · {t("balance")}: <Money value={accrual.balance}/></p>
    {accrual.statusReason&&<p className="wrap-anywhere text-xs text-muted-foreground">{accrual.statusReason}{accrual.statusChangedAt?" · "+formatDateTime(accrual.statusChangedAt,locale):""}</p>}
    <SegmentTable segments={accrual.segments}/>
    {canWaive&&accrual.status==="ACTIVE"&&accrual.paid==="0.00"&&(waiving===accrual.id?<form noValidate className="space-y-2" onSubmit={event=>void waive(event,accrual.id,accrual.version)}><Label htmlFor={"waive-"+accrual.id}>{t("waiveReason")}</Label><Textarea id={"waive-"+accrual.id} maxLength={500} value={reason} aria-invalid={!!reasonError} aria-describedby={reasonError?"waive-error":undefined} onChange={event=>{setReason(event.target.value);setReasonError("");}}/>{reasonError&&<FieldError id="waive-error">{reasonError}</FieldError>}<div className="flex gap-2"><Button type="button" variant="outline" size="sm" disabled={busy} onClick={()=>setWaiving(null)}>{ui("cancel")}</Button><Button type="submit" variant="destructive" size="sm" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<ShieldOff/>}{t("confirmWaive")}</Button></div></form>
     :<Button variant="warn" size="sm" disabled={busy} onClick={()=>{setWaiving(accrual.id);setReason("");setReasonError("");}}><ShieldOff/>{t("waive")}</Button>)}
   </article>;}):<p className="text-sm text-muted-foreground">{t("noAccruals")}</p>}</section>
   <section className="space-y-2"><h3 className="text-base font-semibold">{t("pendingSegments")}</h3><SegmentTable segments={detail.pendingSegments}/></section>
  </>}
  <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}

export function OverduePanel({propertyId,enabled,canWaive,revision,onChanged}:{propertyId:string;enabled:boolean;canWaive:boolean;revision:number;onChanged:()=>void}){
 const t=useTranslations("latePenalties");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [page,setPage]=useState(1);const [year,setYear]=useState("");const [term,setTerm]=useState("");const [local,setLocal]=useState(0);const [detail,setDetail]=useState<string|null>(null);
 const [through,setThrough]=useState(businessToday);const [running,setRunning]=useState(false);const lock=useRef(false);
 useEffect(()=>setPage(1),[year,term]);
 const query=new URLSearchParams({page:String(page),pageSize:"20"});if(/^\d{4}$/.test(year))query.set("year",year);if(term.trim())query.set("search",term.trim());
 const rows=useRows<PenaltyRow>(root(propertyId)+"/overdue?"+query,revision+local,true,readPenaltyPage);
 const data=rows.data as PenaltyPage|null;
 async function run(){
  if(lock.current)return;if(!through||through>businessToday()){notice.error(t("throughInvalid"));return;}lock.current=true;setRunning(true);
  try{const result=readRunResult(await api<unknown>(root(propertyId)+"/accruals/run",{method:"POST",body:JSON.stringify({through})}));notice.success(t("runDone",{count:result.accrualCount,amount:formatDecimalCurrency(result.amount,locale)}));setLocal(n=>n+1);onChanged();}
  catch(err){notice.error(errorMessage(err,locale));}finally{lock.current=false;setRunning(false);}
 }
 const exportCsv=()=>data&&download(`gecikme-${propertyId}-${businessToday()}.csv`,toCsv([[t("unit"),t("period"),t("dueDate"),t("daysOverdue"),t("principalBalance"),t("penaltyBalance"),t("pendingPenalty"),t("totalDue")],
  ...data.items.map(r=>[(r.buildingName?r.buildingName+" · ":"")+r.unitNumber,r.period,r.dueDate,r.daysOverdue,r.principalBalance,r.penaltyBalance,r.pendingPenalty,r.totalDue])]));
 const actions=useCallback((row:PenaltyRow)=><Button variant="view" size="sm" onClick={()=>setDetail(row.chargeId)}><Eye className="size-4"/>{ui("details")}</Button>,[ui]);
 const columns=useMemo<ColumnDef<PenaltyRow>[]>(()=>[
  {id:"unit",header:t("unit"),cell:({row})=><strong>{(row.original.buildingName?row.original.buildingName+" · ":"")+row.original.unitNumber}</strong>},
  {id:"period",header:t("period"),cell:({row})=><div>{row.original.period}<span className="block text-xs text-muted-foreground">{formatDateOnly(row.original.dueDate,locale)} · {t("daysCount",{count:row.original.daysOverdue})}</span></div>},
  {id:"principal",header:t("principalBalance"),cell:({row})=><Money value={row.original.principalBalance}/>},
  {id:"penalty",header:t("penaltyColumn"),cell:({row})=><div><Money value={row.original.penaltyBalance}/><span className="block text-xs text-muted-foreground">+ <Money value={row.original.pendingPenalty}/> {t("pendingShort")}</span></div>},
  {id:"total",header:t("totalDue"),cell:({row})=><strong><Money value={row.original.totalDue}/></strong>},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}
 ],[actions,t,ui,locale]);
 return <div className="space-y-6">
  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{([["principalOverdue",CalendarX2],["penaltyOutstanding",Hourglass],["penaltyCollected",HandCoins],["penaltyWaived",ShieldOff]] as const).map(([key,Icon])=><section key={key} className="min-w-0 rounded-2xl border bg-card p-5 shadow-sm"><div className="flex items-center gap-2 text-sm text-muted-foreground"><Icon aria-hidden="true" className="size-5"/>{t(key)}</div><p className="mt-3 wrap-anywhere text-2xl font-semibold" aria-live="polite">{rows.loading?ui("loading"):data?<Money value={data.totals[key]}/>:"—"}</p></section>)}</div>
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-label={t("overdueList")}>
   <div className="flex flex-wrap items-end justify-between gap-3 border-b p-5"><h2 className="flex items-center gap-2 font-semibold"><Hourglass className="size-5 text-primary"/>{t("overdueList")}</h2>
    <div className="flex flex-wrap items-end gap-2"><div><Label htmlFor="penalty-through">{t("through")}</Label><Input id="penalty-through" type="date" min="2000-01-01" max={businessToday()} value={through} onInput={event=>setThrough(event.currentTarget.value)}/></div><Button disabled={!enabled||running} onClick={()=>void run()}>{running?<LoaderCircle className="animate-spin"/>:<Play/>}{t("runAccrual")}</Button><Button variant="outline" disabled={!data?.items.length} onClick={exportCsv}><FileDown/>{t("exportCsv")}</Button></div></div>
   <div className="grid gap-3 p-5 sm:grid-cols-3"><div className="relative sm:col-span-2"><Search aria-hidden="true" className="absolute left-3 top-3 size-4 text-muted-foreground"/><Input className="pl-9" value={term} maxLength={200} aria-label={t("search")} placeholder={t("search")} onChange={event=>setTerm(event.target.value)}/></div><div><Label htmlFor="penalty-year">{t("year")}</Label><Input id="penalty-year" inputMode="numeric" maxLength={4} value={year} placeholder="2026" onChange={event=>setYear(event.target.value.replace(/\D/g,""))}/></div></div>
   <p className="px-5 pb-3 text-xs text-muted-foreground">{t("overdueHelp")}</p>
   <RecordList data={data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setLocal(n=>n+1)} empty={<><Hourglass className="mx-auto mb-3 size-8"/><strong>{t("emptyOverdue")}</strong></>} card={row=><><div className="mb-2 flex items-start justify-between gap-2"><strong>{(row.buildingName?row.buildingName+" · ":"")+row.unitNumber}</strong><span className="text-xs">{t("daysCount",{count:row.daysOverdue})}</span></div><p className="mb-2 text-sm">{row.period} · {formatDateOnly(row.dueDate,locale)}</p><p className="mb-1 text-sm">{t("principalBalance")}: <Money value={row.principalBalance}/></p><p className="mb-3 text-sm">{t("penaltyColumn")}: <Money value={row.penaltyBalance}/> + <Money value={row.pendingPenalty}/></p><p className="mb-3 text-lg font-semibold"><Money value={row.totalDue}/></p>{actions(row)}</>}/>
  </section>
  {detail&&<ChargePenaltyPanel propertyId={propertyId} chargeId={detail} canWaive={canWaive&&enabled} onClose={()=>setDetail(null)} onChanged={()=>{setLocal(n=>n+1);onChanged();}}/>}
 </div>;
}

function RateChart({policies,statutory}:{policies:PenaltyPolicy[];statutory:string}){
 const t=useTranslations("latePenalties");const locale=useLocale() as Locale;
 const approved=policies.filter(p=>p.status==="APPROVED").sort((a,b)=>a.validFrom.localeCompare(b.validFrom));
 return <figure className="space-y-2"><figcaption className="text-sm font-semibold">{t("rateChart")}</figcaption>
  {approved.length?<ol className="flex items-end gap-2 overflow-x-auto pb-2" aria-label={t("rateChart")}>{approved.map(p=><li key={p.id} className="flex min-w-24 flex-col items-center gap-1 text-xs"><span>%{p.monthlyRate}</span><span aria-hidden="true" className="w-10 rounded-t bg-primary" style={{height:`${Math.max(8,Number(p.monthlyRate)/5*96)}px`}}/><span className="text-center text-muted-foreground">{formatDateOnly(p.validFrom,locale)}<br/>{p.validTo?formatDateOnly(p.validTo,locale):t("openEnded")}</span></li>)}</ol>
   :<p className="text-sm text-muted-foreground">{t("noApprovedPolicy",{rate:statutory})}</p>}
 </figure>;
}

type PolicyAction={policy:PenaltyPolicy;kind:"approve"|"cancel"|"end"};
export function PoliciesPanel({propertyId,enabled,revision,onChanged}:{propertyId:string;enabled:boolean;revision:number;onChanged:()=>void}){
 const t=useTranslations("latePenalties");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [local,setLocal]=useState(0);const settings=useSettings(propertyId,revision+local);const s=settings.data;
 const [apply,setApply]=useState(true);const [effectiveFrom,setEffectiveFrom]=useState(businessToday);const [reason,setReason]=useState("");const [errors,setErrors]=useState<Record<string,string[]>>({});
 const [busy,setBusy]=useState(false);const lock=useRef(false);const lastSwitch=useRef<SubmissionKey|null>(null);const [editing,setEditing]=useState<PenaltyPolicy|"new"|null>(null);const [action,setAction]=useState<PolicyAction|null>(null);
 const changed=()=>{setLocal(n=>n+1);onChanged();};
 async function saveSwitch(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;const validation:Record<string,string[]>={};
  if(!effectiveFrom||effectiveFrom<businessToday())validation.effectiveFrom=[t("effectiveFromInvalid")];if(!reason.trim())validation.reason=[t("reasonRequired")];
  setErrors(validation);if(Object.keys(validation).length)return;
  const payload={apply,effectiveFrom,reason:reason.trim()};lastSwitch.current=submissionKey(lastSwitch.current,JSON.stringify({propertyId,...payload}),()=>crypto.randomUUID());
  lock.current=true;setBusy(true);
  try{readSwitch(await api<unknown>(root(propertyId)+"/switches",{method:"POST",body:JSON.stringify({requestId:lastSwitch.current.id,...payload})}));notice.success(t("switchSaved"));setReason("");lastSwitch.current=null;changed();}
  catch(err){notice.error(errorMessage(err,locale));if(err instanceof ApiError)setErrors(err.errors);}finally{lock.current=false;setBusy(false);}
 }
 if(settings.error)return <MessageBanner action={<Button variant="view" onClick={()=>setLocal(n=>n+1)}>{ui("tryAgain")}</Button>}>{settings.error}</MessageBanner>;
 if(!s)return <p role="status">{ui("loading")}</p>;
 const field=(key:string)=>errors[key]?.length?<FieldError id={"switch-error-"+key}>{errors[key].join(" ")}</FieldError>:null;
 return <div className="space-y-6">
  <section className="space-y-4 rounded-2xl border bg-card p-5 shadow-sm"><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="flex items-center gap-2 font-semibold">{s.appliedToday?<ToggleRight className="size-5 text-primary"/>:<ToggleLeft className="size-5"/>}{t("switchTitle")}</h2><ToneBadge tone={s.appliedToday?"success":"neutral"} icon={s.appliedToday?ToggleRight:ToggleLeft}>{t(s.appliedToday?"applied":"notApplied")}</ToneBadge></div>
   <MessageBanner kind="info" presentation="inline"><Landmark aria-hidden="true" className="inline size-4"/> {t("legalNote",{rate:s.statutoryRate,reference:s.statutoryReference,method:t("method_"+s.defaultMethod)})}</MessageBanner>
   {s.crystallizedThrough&&<p className="text-xs text-muted-foreground">{t("crystallizedThrough",{date:formatDateOnly(s.crystallizedThrough,locale)})}</p>}
   {s.canApprove&&<form noValidate onSubmit={saveSwitch}><fieldset disabled={busy||!enabled} className="grid gap-3 border-0 p-0 sm:grid-cols-3">
    <div><Label htmlFor="switch-apply">{t("switchState")}</Label><select id="switch-apply" className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={apply?"on":"off"} onChange={event=>setApply(event.target.value==="on")}><option value="on">{t("applyOn")}</option><option value="off">{t("applyOff")}</option></select></div>
    <div><Label htmlFor="switch-from">{t("effectiveFrom")}</Label><Input id="switch-from" type="date" min={businessToday()} value={effectiveFrom} aria-invalid={!!errors.effectiveFrom?.length} aria-describedby={errors.effectiveFrom?.length?"switch-error-effectiveFrom":undefined} onInput={event=>setEffectiveFrom(event.currentTarget.value)}/>{field("effectiveFrom")}</div>
    <div className="sm:col-span-3"><Label htmlFor="switch-reason">{t("switchReason")}</Label><Textarea id="switch-reason" maxLength={500} value={reason} aria-invalid={!!errors.reason?.length} aria-describedby={errors.reason?.length?"switch-error-reason":undefined} onChange={event=>setReason(event.target.value)}/>{field("reason")}</div>
    <div className="sm:col-span-3"><Button type="submit">{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("saveSwitch")}</Button></div>
   </fieldset></form>}
   <ul className="divide-y text-sm" aria-label={t("switchHistory")}>{s.switches.length?s.switches.map(item=><li key={item.id} className="flex flex-wrap justify-between gap-2 py-2"><span>{formatDateOnly(item.effectiveFrom,locale)} · {t(item.apply?"applyOn":"applyOff")}</span><span className="wrap-anywhere text-muted-foreground">{item.reason} · {formatDateTime(item.createdAt,locale)}</span></li>):<li className="py-2 text-muted-foreground">{t("noSwitch")}</li>}</ul>
  </section>
  <section className="space-y-4 rounded-2xl border bg-card p-5 shadow-sm"><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="flex items-center gap-2 font-semibold"><Scale className="size-5 text-primary"/>{t("policiesTitle")}</h2>{s.canDraft&&<Button disabled={!enabled} onClick={()=>setEditing("new")}><Plus/>{t("newPolicy")}</Button>}</div>
   <RateChart policies={s.policies} statutory={s.statutoryRate}/>
   {s.policies.length?<ul className="space-y-3">{s.policies.map(p=>{const style=policyStyle[p.status];return <li key={p.id} className="space-y-2 rounded-xl border p-3 text-sm">
    <div className="flex flex-wrap items-center justify-between gap-2"><strong>{p.year} · %{p.monthlyRate} / {t("month")}</strong><ToneBadge tone={style.tone} icon={style.icon}>{t("policy_"+p.status)}</ToneBadge></div>
    <p>{formatDateOnly(p.validFrom,locale)} – {p.validTo?formatDateOnly(p.validTo,locale):t("openEnded")} · {t("method_"+p.method)}</p>
    <p className="wrap-anywhere text-muted-foreground"><Gavel aria-hidden="true" className="inline size-4"/> {t("basis_"+p.legalBasis)} · {p.legalReference}</p>
    {p.approvedAt&&<p className="text-xs text-muted-foreground">{t("approvedAt",{date:formatDateTime(p.approvedAt,locale)})}</p>}{p.statusReason&&<p className="wrap-anywhere text-xs text-muted-foreground">{p.statusReason}</p>}
    <div className="flex flex-wrap gap-2">{p.status==="DRAFT"&&s.canDraft&&<Button size="sm" variant="edit" disabled={!enabled} onClick={()=>setEditing(p)}>{t("editDraft")}</Button>}{p.status==="DRAFT"&&s.canApprove&&<Button size="sm" disabled={!enabled} onClick={()=>setAction({policy:p,kind:"approve"})}><BadgeCheck/>{t("approve")}</Button>}{p.status==="DRAFT"&&s.canDraft&&<Button size="sm" variant="danger" disabled={!enabled} onClick={()=>setAction({policy:p,kind:"cancel"})}><Ban/>{t("cancelDraft")}</Button>}{p.status==="APPROVED"&&s.canApprove&&<Button size="sm" variant="warn" disabled={!enabled} onClick={()=>setAction({policy:p,kind:"end"})}><CalendarX2/>{t("endPolicy")}</Button>}</div>
   </li>;})}</ul>:<p className="text-sm text-muted-foreground">{t("noPolicies")}</p>}
  </section>
  {editing&&<PolicyEditor propertyId={propertyId} policy={editing==="new"?null:editing} onClose={()=>setEditing(null)} onSaved={changed}/>}
  {action&&<PolicyActionPanel propertyId={propertyId} action={action} onClose={()=>setAction(null)} onSaved={changed}/>}
 </div>;
}

function PolicyEditor({propertyId,policy,onClose,onSaved}:{propertyId:string;policy:PenaltyPolicy|null;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("latePenalties");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [validFrom,setValidFrom]=useState(policy?.validFrom??`${new Date().getFullYear()+1}-01-01`);const [validTo,setValidTo]=useState(policy?.validTo??"");const rate="5";const method:PenaltyMethod="DAILY_30";const basis="KMK_20";const [reference,setReference]=useState(policy?.legalReference??"634 sayılı Kat Mülkiyeti Kanunu m.20");
 const [documentId,setDocumentId]=useState(policy?.approvalDocumentId??"");const [documents,setDocuments]=useState<DocumentRecord[]>([]);
 const [errors,setErrors]=useState<Record<string,string[]>>({});const [error,setError]=useState("");const [busy,setBusy]=useState(false);const lock=useRef(false);const last=useRef<SubmissionKey|null>(null);
 useEffect(()=>{const controller=new AbortController();void api<unknown>(`/api/v1/properties/${encodeURIComponent(propertyId)}/documents?pageSize=100`,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setDocuments(readContentPage(value,row=>readDocument(row,propertyId)).items);}).catch(()=>{if(!controller.signal.aborted)setDocuments([]);});return()=>controller.abort();},[api,propertyId]);
 const normalized=normalizeRate(rate);const statutory=normalized!==null&&Number(normalized)===5&&basis==="KMK_20";
 const example=normalized?((Number(normalized)*30).toFixed(2)):null;
 async function save(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;const validation:Record<string,string[]>={};
  if(!validFrom||validFrom<"2000-01-01")validation.validFrom=[t("dateInvalid")];if(validTo&&validTo<validFrom)validation.validTo=[t("dateInvalid")];if(!normalized)validation.monthlyRate=[t("rateInvalid")];if(!reference.trim())validation.legalReference=[t("referenceRequired")];
  setErrors(validation);setError("");if(Object.keys(validation).length)return;
  const payload={validFrom,validTo:validTo||null,monthlyRate:normalized,method,legalBasis:basis,legalReference:reference.trim(),approvalDocumentId:documentId||null};
  last.current=submissionKey(last.current,JSON.stringify({propertyId,...payload}),()=>crypto.randomUUID());lock.current=true;setBusy(true);
  try{readPolicy(await api<unknown>(root(propertyId)+"/policies"+(policy?"/"+policy.id:""),{method:policy?"PUT":"POST",body:JSON.stringify({requestId:policy?"00000000-0000-0000-0000-000000000000":last.current.id,version:policy?.version??0,...payload})}));notice.success(t("policySaved"));onSaved();onClose();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError)setErrors(err.errors);}finally{lock.current=false;setBusy(false);}
 }
 const field=(key:string)=>errors[key]?.length?<FieldError id={"policy-error-"+key}>{errors[key].join(" ")}</FieldError>:null;
 const select="h-11 w-full rounded-md border bg-card px-3 text-sm";
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><Scale className="size-8 text-primary"/><DialogTitle>{t(policy?"editPolicy":"newPolicy")}</DialogTitle><DialogDescription>{t("policyHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}
  <form noValidate onSubmit={save}><fieldset disabled={busy} className="space-y-4 border-0 p-0">
   <div className="grid gap-3 sm:grid-cols-2"><div><Label htmlFor="policy-from">{t("validFrom")}</Label><Input id="policy-from" type="date" min="2000-01-01" value={validFrom} aria-invalid={!!errors.validFrom?.length} onInput={event=>setValidFrom(event.currentTarget.value)}/>{field("validFrom")}</div><div><Label htmlFor="policy-to">{t("validTo")}</Label><Input id="policy-to" type="date" min={validFrom} value={validTo} aria-invalid={!!errors.validTo?.length} onInput={event=>setValidTo(event.currentTarget.value)}/>{field("validTo")}</div></div>
   <dl className="grid grid-cols-2 gap-2 rounded-xl border bg-muted p-3 text-sm"><dt>{t("monthlyRate")}</dt><dd>%{rate}</dd><dt>{t("method")}</dt><dd>{t("method_"+method)}</dd><dt>{t("legalBasis")}</dt><dd>{t("basis_"+basis)}</dd></dl>{field("monthlyRate")}<p className="text-xs text-muted-foreground">{t("rateHelp")}</p>
   <div><Label htmlFor="policy-reference">{t("legalReference")}</Label><Input id="policy-reference" maxLength={500} value={reference} aria-invalid={!!errors.legalReference?.length} onChange={event=>setReference(event.target.value)}/>{field("legalReference")}</div>
   <div><Label htmlFor="policy-document">{t("approvalDocument")}</Label><select id="policy-document" className={select} value={documentId} aria-invalid={!!errors.approvalDocumentId?.length} onChange={event=>setDocumentId(event.target.value)}><option value="">{t("noDocument")}</option>{documents.map(d=><option key={d.id} value={d.id}>{d.fileName}</option>)}</select>{field("approvalDocumentId")}<p className="mt-1 text-xs text-muted-foreground">{t("documentHelp")}</p></div>
   <MessageBanner kind={statutory?"info":"warning"} presentation="inline">{statutory?t("statutoryPolicy"):t("nonStatutoryPolicy")}{example&&<span className="block">{t("previewExample",{amount:formatDecimalCurrency(example,locale)})}</span>}</MessageBanner>
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit">{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("saveDraft")}</Button></DialogFooter>
  </fieldset></form>
 </ManagementPanel></Dialog>;
}

function PolicyActionPanel({propertyId,action,onClose,onSaved}:{propertyId:string;action:PolicyAction;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("latePenalties");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const {policy,kind}=action;const [reason,setReason]=useState("");const [validTo,setValidTo]=useState(businessToday);const [error,setError]=useState("");const [invalid,setInvalid]=useState("");const [busy,setBusy]=useState(false);const lock=useRef(false);
 async function submit(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;if(kind!=="approve"&&!reason.trim()){setInvalid(t("reasonRequired"));return;}
  lock.current=true;setBusy(true);setError("");
  try{readPolicy(await api<unknown>(root(propertyId)+`/policies/${policy.id}/${kind}`,{method:"PUT",body:JSON.stringify({version:policy.version,reason:reason.trim()||null,validTo:kind==="end"?validTo:null})}));notice.success(t("action_"+kind+"_done"));onSaved();onClose();}
  catch(err){setError(errorMessage(err,locale));}finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><Gavel className="size-8 text-primary"/><DialogTitle>{t("action_"+kind)}</DialogTitle><DialogDescription>{policy.year} · %{policy.monthlyRate} · {formatDateOnly(policy.validFrom,locale)} – {policy.validTo?formatDateOnly(policy.validTo,locale):t("openEnded")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner kind="warning">{error}</MessageBanner>}
  <p className="wrap-anywhere text-sm"><Gavel aria-hidden="true" className="inline size-4"/> {t("basis_"+policy.legalBasis)} · {policy.legalReference}</p>
  <MessageBanner kind="info" presentation="inline">{t("action_"+kind+"_help")}</MessageBanner>
  <form noValidate onSubmit={submit}><fieldset disabled={busy} className="space-y-3 border-0 p-0">
   {kind==="end"&&<div><Label htmlFor="policy-end">{t("validTo")}</Label><Input id="policy-end" type="date" min={policy.validFrom} value={validTo} onInput={event=>setValidTo(event.currentTarget.value)}/></div>}
   {kind!=="approve"&&<div><Label htmlFor="policy-reason">{t("reason")}</Label><Textarea id="policy-reason" maxLength={500} value={reason} aria-invalid={!!invalid} aria-describedby={invalid?"policy-reason-error":undefined} onChange={event=>{setReason(event.target.value);setInvalid("");}}/>{invalid&&<FieldError id="policy-reason-error">{invalid}</FieldError>}</div>}
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" variant={kind==="approve"?"default":"destructive"}>{busy?<LoaderCircle className="animate-spin"/>:kind==="approve"?<BadgeCheck/>:kind==="end"?<CalendarX2/>:<Ban/>}{t("action_"+kind)}</Button></DialogFooter>
  </fieldset></form>
 </ManagementPanel></Dialog>;
}

export function ReportPanel({propertyId,revision}:{propertyId:string;revision:number}){
 const t=useTranslations("latePenalties");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();
 const [report,setReport]=useState<PenaltyReport|null>(null);const [error,setError]=useState("");const [local,setLocal]=useState(0);
 useEffect(()=>{const controller=new AbortController();setError("");void api<unknown>(root(propertyId)+"/report",{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setReport(readReport(value,propertyId));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));});return()=>controller.abort();},[api,propertyId,revision,local,locale]);
 if(error)return <MessageBanner action={<Button variant="view" onClick={()=>setLocal(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>;
 if(!report)return <p role="status">{ui("loading")}</p>;
 const exportCsv=()=>download(`gecikme-rapor-${propertyId}.csv`,toCsv([[t("year"),t("accrued"),t("penaltyCollected"),t("penaltyWaived"),t("penaltyOutstanding")],...report.years.map(y=>[y.year,y.accrued,y.collected,y.waived,y.outstanding]),[],
  [t("validFrom"),t("validTo"),t("rate"),t("method"),t("legalBasis"),t("legalReference")],...report.rateHistory.map(p=>[p.validFrom,p.validTo??"",p.monthlyRate,p.method,p.legalBasis,p.legalReference])]));
 return <div className="space-y-6">
  <div className="flex flex-wrap justify-end gap-2 print:hidden"><Button variant="outline" onClick={exportCsv}><FileDown/>{t("exportCsv")}</Button><Button variant="outline" onClick={()=>window.print()}><Printer/>{t("printPdf")}</Button></div>
  <section className="overflow-x-auto rounded-2xl border bg-card p-5 shadow-sm"><h2 className="mb-3 flex items-center gap-2 font-semibold"><HandCoins className="size-5 text-primary"/>{t("yearlyReport")}</h2>
   {report.years.length?<table className="w-full text-left text-sm"><thead><tr className="text-muted-foreground"><th className="py-2 pr-3">{t("year")}</th><th className="pr-3">{t("accrued")}</th><th className="pr-3">{t("penaltyCollected")}</th><th className="pr-3">{t("penaltyWaived")}</th><th>{t("penaltyOutstanding")}</th></tr></thead>
    <tbody>{report.years.map(y=><tr key={y.year} className="border-t"><td className="py-2 pr-3 font-semibold">{y.year}</td><td className="pr-3"><Money value={y.accrued}/></td><td className="pr-3"><Money value={y.collected}/></td><td className="pr-3"><Money value={y.waived}/></td><td><Money value={y.outstanding}/></td></tr>)}</tbody></table>
    :<p className="text-sm text-muted-foreground">{t("noReport")}</p>}
  </section>
  <section className="rounded-2xl border bg-card p-5 shadow-sm"><RateChart policies={report.rateHistory} statutory="5.00"/></section>
  <p className="text-xs text-muted-foreground">{t("reportHelp")}</p>
 </div>;
}
