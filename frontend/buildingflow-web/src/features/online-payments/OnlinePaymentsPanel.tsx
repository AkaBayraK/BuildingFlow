"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {BadgeCheck,CircleX,CreditCard,Eye,Hourglass,LoaderCircle,Printer,ReceiptText,RefreshCw,Reply,ShieldAlert,Undo2,WalletCards} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {useRows} from "@/features/people/useRows";
import {submissionKey,type SubmissionKey} from "@/features/dues/model";
import {ChargeKindBadge} from "@/features/dues/ChargeKindBadge";
import {formatDateOnly,formatDateTime,formatDecimalCurrency,type Locale} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {SummaryCard,ToneBadge} from "@/components/management/WorkspacePresentation";
import {FilterChips} from "@/components/management/FilterChips";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {canRefund,intentStatuses,readOnlinePayment,readOnlinePaymentPage,readReceipt,readRefund,type OnlinePayment,type OnlinePaymentPage,type Receipt,type Refund} from "./model";
import {allIcon,intentStyle,issueStatusStyle,issueStyle,refundStyle} from "./payment-style";

const rootOf=(propertyId:string)=>`/api/v1/properties/${encodeURIComponent(propertyId)}`;
export function Money({value}:{value:string}){const locale=useLocale() as Locale;return <span className="tabular-nums">{formatDecimalCurrency(value,locale)}</span>;}
const unitName=(row:{buildingName:string|null;unitNumber:string})=>(row.buildingName?row.buildingName+" · ":"")+row.unitNumber;
type Access={canPropose:boolean;canApprove:boolean};

export function IntentStatusBadge({status}:{status:OnlinePayment["status"]}){
 const t=useTranslations("onlinePayments");const style=intentStyle[status];
 return <ToneBadge tone={style.tone} icon={style.icon}>{t("intent_"+status)}</ToneBadge>;
}

export function OnlinePaymentsPanel({propertyId,enabled,access,revision,onChanged}:{propertyId:string;enabled:boolean;access:Access;revision:number;onChanged:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;
 const [status,setStatus]=useState("");const [from,setFrom]=useState("");const [to,setTo]=useState("");const [page,setPage]=useState(1);const [local,setLocal]=useState(0);const [detail,setDetail]=useState<string|null>(null);
 useEffect(()=>setPage(1),[status,from,to]);
 const query=new URLSearchParams({page:String(page),pageSize:"10"});if(status)query.set("status",status);if(from)query.set("from",from);if(to)query.set("to",to);
 const rows=useRows<OnlinePayment>(rootOf(propertyId)+"/online-payments?"+query,revision+local,!(from&&to&&from>to),readOnlinePaymentPage);
 const data=rows.data as OnlinePaymentPage|null;
 const actions=(row:OnlinePayment)=><Button variant="view" size="sm" onClick={()=>setDetail(row.id)}><Eye className="size-4"/>{ui("details")}</Button>;
 const columns:ColumnDef<OnlinePayment>[]=[
  {id:"unit",header:ui("unit"),cell:({row})=><strong>{unitName(row.original)}</strong>},
  {id:"created",header:t("startedAt"),cell:({row})=><div className="text-sm">{formatDateTime(row.original.createdAt,locale)}<span className="block text-xs text-muted-foreground">{row.original.providerCode}</span></div>},
  {id:"amount",header:t("amount"),cell:({row})=><div><strong><Money value={row.original.amount}/></strong>{row.original.feeAmount&&<span className="block text-xs text-muted-foreground">{t("fee")}: <Money value={row.original.feeAmount}/></span>}</div>},
  {id:"lines",header:t("debts"),cell:({row})=><span className="text-sm">{t("lineCount",{count:row.original.lines.length})}</span>},
  {id:"status",header:ui("status"),cell:({row})=><div className="space-y-1"><IntentStatusBadge status={row.original.status}/>{row.original.receiptNumber&&<span className="block font-mono text-xs">{row.original.receiptNumber}</span>}</div>},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}
 ];
 const chips=[{value:"",label:t("allStatuses"),icon:allIcon},...intentStatuses.map(value=>({value,label:t("intent_"+value),icon:intentStyle[value].icon}))];
 return <div className="space-y-6">
  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
   <SummaryCard title={t("succeededTotal")} value={rows.loading?"…":data?<Money value={data.summary.succeededAmount}/>:"—"} note={t("filteredSummary")} icon={BadgeCheck}/>
   <SummaryCard title={t("reviewTotal")} value={rows.loading?"…":data?<Money value={data.summary.reviewAmount}/>:"—"} note={t("reviewCount",{count:data?.summary.reviewCount??0})} icon={ShieldAlert}/>
   <SummaryCard title={t("refundedTotal")} value={rows.loading?"…":data?<Money value={data.summary.refundedAmount}/>:"—"} note={t("filteredSummary")} icon={Undo2}/>
   <SummaryCard title={t("feeTotal")} value={rows.loading?"…":data?<Money value={data.summary.feeAmount}/>:"—"} note={t("pendingCount",{count:data?.summary.pendingCount??0})} icon={Hourglass}/>
  </div>
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-labelledby="online-payments-title">
   <div className="flex flex-wrap items-end justify-between gap-3 border-b p-5"><h2 id="online-payments-title" className="flex items-center gap-2 font-semibold"><CreditCard className="size-5 text-primary"/>{t("onlinePayments")}</h2>
    <div className="flex flex-wrap items-end gap-2"><div><Label htmlFor="online-from">{t("from")}</Label><Input id="online-from" type="date" value={from} onInput={event=>setFrom(event.currentTarget.value)}/></div><div><Label htmlFor="online-to">{t("to")}</Label><Input id="online-to" type="date" min={from||undefined} value={to} aria-invalid={!!(from&&to&&from>to)} onInput={event=>setTo(event.currentTarget.value)}/></div><Button variant="outline" onClick={()=>setLocal(n=>n+1)}><RefreshCw/>{ui("refresh")}</Button></div></div>
   <div className="space-y-2 p-5 pb-0"><FilterChips label={ui("status")} items={chips} value={status} onChange={setStatus}/>{from&&to&&from>to&&<FieldError>{t("dateRangeInvalid")}</FieldError>}<p className="text-xs text-muted-foreground">{t("onlineHelp")}</p></div>
   <RecordList compact data={data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setLocal(n=>n+1)} empty={<><CreditCard className="mx-auto mb-3 size-8"/><strong>{t("noOnlinePayments")}</strong><p className="mt-2">{t("noOnlinePaymentsHelp")}</p></>}
    card={row=><><div className="mb-2 flex flex-wrap justify-between gap-2"><strong>{unitName(row)}</strong><IntentStatusBadge status={row.status}/></div><p className="text-sm">{formatDateTime(row.createdAt,locale)}</p><p className="my-2 text-lg font-semibold"><Money value={row.amount}/></p>{actions(row)}</>}/>
  </section>
  {detail&&<OnlinePaymentDetail propertyId={propertyId} intentId={detail} enabled={enabled} access={access} onClose={()=>setDetail(null)} onChanged={()=>{setLocal(n=>n+1);onChanged();}}/>}
 </div>;
}

function OnlinePaymentDetail({propertyId,intentId,enabled,access,onClose,onChanged}:{propertyId:string;intentId:string;enabled:boolean;access:Access;onClose:()=>void;onChanged:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [record,setRecord]=useState<OnlinePayment|null>(null);const [error,setError]=useState("");const [busy,setBusy]=useState(true);const [revision,setRevision]=useState(0);const [receipt,setReceipt]=useState(false);
 const [refunding,setRefunding]=useState(false);const [reason,setReason]=useState("");const [fieldError,setFieldError]=useState("");const [deciding,setDeciding]=useState<{refund:Refund;decision:"APPROVE"|"REJECT"}|null>(null);const [note,setNote]=useState("");
 const lock=useRef(false);const last=useRef<SubmissionKey|null>(null);const url=rootOf(propertyId)+"/online-payments/"+intentId;
 useEffect(()=>{const controller=new AbortController();setBusy(true);setError("");void api<unknown>(url,{signal:controller.signal}).then(value=>{const read=readOnlinePayment(value);if(read.id!==intentId)throw new Error("Invalid identity");if(!controller.signal.aborted)setRecord(read);}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setBusy(false);});return()=>controller.abort();},[api,url,intentId,locale,revision]);
 async function run(action:()=>Promise<void>){
  if(lock.current)return;lock.current=true;setBusy(true);setError("");
  try{await action();setRevision(n=>n+1);onChanged();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)setRevision(n=>n+1);}
  finally{lock.current=false;setBusy(false);}
 }
 const settle=()=>record&&void run(async()=>{readOnlinePayment(await api<unknown>(url+"/settle",{method:"PUT",body:JSON.stringify({version:record.version})}));notice.success(t("settled"));});
 function requestRefund(event:React.FormEvent){
  event.preventDefault();if(!reason.trim()){setFieldError(t("reasonRequired"));return;}
  last.current=submissionKey(last.current,JSON.stringify({intentId,reason:reason.trim()}),()=>crypto.randomUUID());const requestId=last.current.id;
  void run(async()=>{const refund=readRefund(await api<unknown>(url+"/refunds",{method:"POST",body:JSON.stringify({requestId,reason:reason.trim()})}));notice.success(t(refund.replayed?"refundReplayed":"refundRequested"));setRefunding(false);setReason("");});
 }
 function decide(event:React.FormEvent){
  event.preventDefault();if(!deciding)return;if(deciding.decision==="REJECT"&&!note.trim()){setFieldError(t("reasonRequired"));return;}
  const {refund,decision}=deciding;
  void run(async()=>{const result=readRefund(await api<unknown>(rootOf(propertyId)+"/payment-refunds/"+refund.id+"/decision",{method:"PUT",body:JSON.stringify({decision,note:note.trim()||null,version:refund.version})}));notice[result.status==="FAILED"?"warning":"success"](t("refund_"+result.status));setDeciding(null);setNote("");});
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><CreditCard className="size-8 text-primary"/><DialogTitle>{t("onlinePaymentDetail")}</DialogTitle><DialogDescription>{record?unitName(record)+" · "+formatDateTime(record.createdAt,locale):ui("loading")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner action={<Button variant="view" disabled={busy} onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("loadCurrentRecord")}</Button>}>{error}</MessageBanner>}
  {busy&&!record&&<p role="status">{ui("loading")}</p>}
  {record&&<>
   <div className="flex flex-wrap gap-2"><IntentStatusBadge status={record.status}/>{record.receiptNumber&&<ToneBadge tone="info" icon={ReceiptText}>{record.receiptNumber}</ToneBadge>}</div>
   <dl className="grid grid-cols-2 gap-2 text-sm"><dt className="text-muted-foreground">{t("amount")}</dt><dd className="font-semibold"><Money value={record.amount}/></dd><dt className="text-muted-foreground">{t("fee")}</dt><dd>{record.feeAmount?<Money value={record.feeAmount}/>:"—"}</dd><dt className="text-muted-foreground">{t("provider")}</dt><dd>{record.providerCode}</dd><dt className="text-muted-foreground">{t("paidDate")}</dt><dd>{record.paidDate?formatDateOnly(record.paidDate,locale):"—"}</dd><dt className="text-muted-foreground">{t("expiresAt")}</dt><dd>{formatDateTime(record.expiresAt,locale)}</dd><dt className="text-muted-foreground">{t("statusChangedAt")}</dt><dd>{formatDateTime(record.statusChangedAt,locale)}</dd>{record.failureCode&&<><dt className="text-muted-foreground">{t("failureCode")}</dt><dd className="font-mono text-xs">{record.failureCode}</dd></>}</dl>
   {record.status==="REVIEW"&&<MessageBanner kind="warning" presentation="inline">{t("reviewHelp")}</MessageBanner>}
   <section className="space-y-2"><h3 className="text-base font-semibold">{t("distribution")}</h3><ul className="divide-y">{record.lines.map((line,index)=><li key={line.chargeId+line.kind+index} className="flex items-start justify-between gap-3 py-2 text-sm"><span className="space-y-1">{line.period} <ChargeKindBadge propertyId={propertyId} kind={line.chargeKind} category={line.chargeCategory}/>{line.kind==="PENALTY"&&<span className="block text-xs text-muted-foreground">{t("penaltyLine")}</span>}</span><strong><Money value={line.amount}/></strong></li>)}</ul></section>
   {!!record.events?.length&&<section className="space-y-2"><h3 className="text-base font-semibold">{t("events")}</h3><ol className="space-y-1 text-xs">{record.events.map((event,index)=><li key={index} className="rounded-lg border p-2"><strong>{event.eventType}</strong>{event.outcome&&" · "+event.outcome}<span className="block text-muted-foreground">{formatDateTime(event.occurredAt,locale)}{event.processedAt?" → "+formatDateTime(event.processedAt,locale):" · "+t("notProcessed")}</span></li>)}</ol></section>}
   {!!record.refunds?.length&&<section className="space-y-2"><h3 className="text-base font-semibold">{t("refunds")}</h3>{record.refunds.map(refund=>{const style=refundStyle[refund.status];return <article key={refund.id} className="space-y-2 rounded-xl border p-3 text-sm"><div className="flex flex-wrap justify-between gap-2"><strong><Money value={refund.amount}/></strong><ToneBadge tone={style.tone} icon={style.icon}>{t("refundStatus_"+refund.status)}</ToneBadge></div><p className="wrap-anywhere">{refund.reason}</p><p className="text-xs text-muted-foreground">{formatDateTime(refund.createdAt,locale)}{refund.decidedAt?" · "+formatDateTime(refund.decidedAt,locale):""}{refund.decisionNote?" · "+refund.decisionNote:""}{refund.failureCode?" · "+refund.failureCode:""}</p>
    {refund.status==="REQUESTED"&&access.canApprove&&!refund.requestedByMe&&<div className="flex gap-2"><Button size="sm" variant="view" disabled={busy||!enabled} onClick={()=>{setDeciding({refund,decision:"APPROVE"});setNote("");setFieldError("");}}><BadgeCheck/>{t("approve")}</Button><Button size="sm" variant="danger" disabled={busy||!enabled} onClick={()=>{setDeciding({refund,decision:"REJECT"});setNote("");setFieldError("");}}><CircleX/>{t("reject")}</Button></div>}
    {refund.status==="REQUESTED"&&refund.requestedByMe&&<p className="text-xs text-muted-foreground">{t("awaitingSecondApprover")}</p>}
    {deciding?.refund.id===refund.id&&<form noValidate className="space-y-2" onSubmit={decide}><MessageBanner kind={deciding.decision==="APPROVE"?"warning":"info"} presentation="inline">{t(deciding.decision==="APPROVE"?"refundApproveHelp":"refundRejectHelp")}</MessageBanner><Label htmlFor="refund-note">{t(deciding.decision==="APPROVE"?"noteOptional":"reason")}</Label><Textarea id="refund-note" maxLength={500} value={note} aria-invalid={!!fieldError} onChange={event=>{setNote(event.target.value);setFieldError("");}}/>{fieldError&&<FieldError>{fieldError}</FieldError>}<div className="flex gap-2"><Button type="button" size="sm" variant="outline" disabled={busy} onClick={()=>setDeciding(null)}>{ui("cancel")}</Button><Button type="submit" size="sm" variant="destructive" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<Undo2/>}{t("confirm_"+deciding.decision+"_REFUND")}</Button></div></form>}
   </article>;})}</section>}
   {!!record.issues?.length&&<section className="space-y-2"><h3 className="text-base font-semibold">{t("issues")}</h3>{record.issues.map(issue=>{const style=issueStyle[issue.type];const state=issueStatusStyle[issue.status];return <article key={issue.id} className="space-y-1 rounded-xl border p-3 text-sm"><div className="flex flex-wrap gap-2"><ToneBadge tone={style.tone} icon={style.icon}>{t("issue_"+issue.type)}</ToneBadge><ToneBadge tone={state.tone} icon={state.icon}>{t("issueStatus_"+issue.status)}</ToneBadge></div><p className="wrap-anywhere text-xs text-muted-foreground">{issue.detail}</p></article>;})}</section>}
   {refunding?<form noValidate className="space-y-2 rounded-xl border p-3" onSubmit={requestRefund}><MessageBanner kind="warning" presentation="inline">{t("refundHelp")}</MessageBanner><Label htmlFor="refund-reason">{t("reason")}</Label><Textarea id="refund-reason" maxLength={500} value={reason} aria-invalid={!!fieldError} aria-describedby={fieldError?"refund-reason-error":undefined} onChange={event=>{setReason(event.target.value);setFieldError("");}}/>{fieldError&&<FieldError id="refund-reason-error">{fieldError}</FieldError>}<div className="flex gap-2"><Button type="button" variant="outline" size="sm" disabled={busy} onClick={()=>setRefunding(false)}>{ui("cancel")}</Button><Button type="submit" variant="destructive" size="sm" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<Reply/>}{t("requestRefund")}</Button></div></form>:null}
  </>}
  <DialogFooter className="flex-wrap">
   <Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button>
   {record?.paymentId&&<Button variant="view" disabled={busy} onClick={()=>setReceipt(true)}><ReceiptText/>{t("viewReceipt")}</Button>}
   {record?.status==="REVIEW"&&access.canPropose&&<Button disabled={busy||!enabled} onClick={settle}><WalletCards/>{t("settleReview")}</Button>}
   {record&&canRefund(record)&&access.canPropose&&!refunding&&<Button variant="warn" disabled={busy||!enabled} onClick={()=>{setRefunding(true);setReason("");setFieldError("");}}><Reply/>{t("requestRefund")}</Button>}
  </DialogFooter>
  {receipt&&record?.paymentId&&<ReceiptDialog propertyId={propertyId} paymentId={record.paymentId} onClose={()=>setReceipt(false)}/>}
 </ManagementPanel></Dialog>;
}

export function ReceiptDialog({propertyId,paymentId,onClose}:{propertyId:string;paymentId:string;onClose:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();
 const [receipt,setReceipt]=useState<Receipt|null>(null);const [error,setError]=useState("");const [revision,setRevision]=useState(0);
 useEffect(()=>{const controller=new AbortController();setError("");void api<unknown>(rootOf(propertyId)+"/payments/"+paymentId+"/receipt",{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setReceipt(readReceipt(value));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));});return()=>controller.abort();},[api,propertyId,paymentId,locale,revision]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><ReceiptText className="size-8 text-primary"/><DialogTitle>{t("receiptTitle")}</DialogTitle><DialogDescription>{receipt?receipt.number:ui("loading")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
  {!receipt&&!error&&<p role="status">{ui("loading")}</p>}
  {receipt&&<article className="space-y-3" aria-label={t("receiptTitle")}>
   <dl className="grid grid-cols-2 gap-2 text-sm"><dt className="text-muted-foreground">{t("receiptNumber")}</dt><dd className="font-mono">{receipt.number}</dd><dt className="text-muted-foreground">{t("property")}</dt><dd>{receipt.propertyName}</dd><dt className="text-muted-foreground">{ui("unit")}</dt><dd>{unitName(receipt)}</dd><dt className="text-muted-foreground">{t("paidDate")}</dt><dd>{formatDateOnly(receipt.paidDate,locale)}</dd><dt className="text-muted-foreground">{t("method")}</dt><dd>{t("method_"+receipt.method)}</dd><dt className="text-muted-foreground">{t("issuedAt")}</dt><dd>{formatDateTime(receipt.issuedAt,locale)}</dd><dt className="text-muted-foreground">{ui("status")}</dt><dd>{t("receiptStatus_"+receipt.status)}</dd></dl>
   <ul className="divide-y">{receipt.lines.map((line,index)=><li key={index} className="flex items-start justify-between gap-3 py-2 text-sm"><span>{line.period} <ChargeKindBadge propertyId={propertyId} kind={line.chargeKind} category={line.chargeCategory}/>{line.kind==="PENALTY"&&<span className="block text-xs text-muted-foreground">{t("penaltyLine")}</span>}</span><strong><Money value={line.amount}/></strong></li>)}</ul>
   <p className="flex justify-between border-t pt-2 font-semibold"><span>{t("amount")}</span><Money value={receipt.amount}/></p>
   <MessageBanner kind="info" presentation="inline">{t("receiptNotFiscal")}</MessageBanner>
  </article>}
  <DialogFooter><Button variant="outline" onClick={onClose}>{ui("close")}</Button><Button variant="view" disabled={!receipt} onClick={()=>window.print()}><Printer/>{t("print")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}
