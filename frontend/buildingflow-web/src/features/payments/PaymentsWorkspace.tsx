"use client";
import {useCallback,useEffect,useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {WalletCards,Plus,RefreshCw,ReceiptText,CircleX,ListFilter} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useSearch} from "@/features/people/useRows";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {formatCurrency,formatDateOnly,formatNumber} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,SummaryCard,ListToolbar,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {PaymentEditor,PaymentDetail,PaymentStatus,type UnitSelection} from "./PaymentDialogs";
import {readPaymentPage,type PaymentPage,type PaymentRecord} from "./model";

export function PaymentsWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("payments");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const identity=useIdentity();const scope=usePropertyScope();
 const allowed=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(role=>identity.hasRealmRole(role));
 const [data,setData]=useState<PaymentPage|null>(null);const [loading,setLoading]=useState(true);const [error,setError]=useState("");const [revision,setRevision]=useState(0);
 const [term,setTerm]=useState("");const search=useSearch(term);const [page,setPage]=useState(1);const [pageSize,setPageSize]=useState(10);const [status,setStatus]=useState("");const [method,setMethod]=useState("");const [from,setFrom]=useState("");const [to,setTo]=useState("");const [sort,setSort]=useState("paidDate:desc");const [unit,setUnit]=useState<UnitSelection|null>(null);
 const [creating,setCreating]=useState(false);const [detail,setDetail]=useState<PaymentRecord|null>(null);const root=`/api/v1/properties/${encodeURIComponent(propertyId)}/payments`;
 const [sortBy,sortDirection]=sort.split(":");const query=new URLSearchParams({page:String(page),pageSize:String(pageSize),search,sortBy,sortDirection});if(status)query.set("status",status);if(method)query.set("method",method);if(from)query.set("from",from);if(to)query.set("to",to);if(unit)query.set("unitId",unit.id);const url=root+"?"+query;
 useEffect(()=>setPage(1),[search,status,method,from,to,sort,pageSize,unit?.id]);
 useEffect(()=>{const controller=new AbortController();setData(null);setError("");setLoading(allowed);if(allowed)void api<unknown>(url,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setData(readPaymentPage(value,propertyId));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});return()=>controller.abort();},[api,url,propertyId,locale,allowed,revision]);
 useEffect(()=>{if(data&&page>Math.max(1,Math.ceil(data.totalCount/pageSize)))setPage(Math.max(1,Math.ceil(data.totalCount/pageSize)));},[data,page,pageSize]);
 const enabled=allowed&&scope.property?.id===propertyId&&scope.property.isEnabled&&!scope.error;
 const changed=()=>{setPage(1);setRevision(n=>n+1);scope.refresh();};
 const name=useCallback((record:PaymentRecord)=>(record.buildingName?record.buildingName+" · ":"")+record.unitNumber,[]);
 const actions=useCallback((record:PaymentRecord)=><div className="flex gap-1"><Button variant="view" size="icon" aria-label={t("viewReceipt",{name:name(record)})} onClick={()=>setDetail(record)}><ReceiptText className="size-4"/></Button><Button variant="view" size="icon" aria-label={t("filterUnit",{name:name(record)})} onClick={()=>setUnit({id:record.unitId,name:name(record)})}><ListFilter className="size-4"/></Button></div>,[name,t]);
 const columns=useMemo<ColumnDef<PaymentRecord>[]>(()=>[{id:"unit",header:ui("unit"),cell:({row})=><strong>{name(row.original)}</strong>},{id:"date",header:t("date"),cell:({row})=>formatDateOnly(row.original.paidDate,locale)},{id:"amount",header:t("total"),cell:({row})=><strong className="tabular-nums">{formatCurrency(Number(row.original.amount),locale)}</strong>},{id:"method",header:t("method"),cell:({row})=><div>{t(row.original.method)}<span className="block max-w-40 truncate text-xs text-muted-foreground" title={row.original.reference??undefined}>{row.original.reference??"—"}</span></div>},{id:"status",header:ui("status"),cell:({row})=><PaymentStatus record={row.original}/>},{id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}],[actions,name,t,ui,locale]);
 function reset(){setTerm("");setStatus("");setMethod("");setFrom("");setTo("");setUnit(null);setSort("paidDate:desc");setPage(1);}
 if(!allowed)return <div className="py-10"><h1>{t("title")}</h1><MessageBanner>{ui("financeRoleRequired")}</MessageBanner></div>;
 return <div className="py-8"><WorkspaceHeading title={t("title")} description={t("description")} scope={scope.property?.name??ui("loading")} icon={WalletCards} action={<Button variant="view" disabled={!enabled} onClick={()=>setCreating(true)}><Plus/>{t("newPayment")}</Button>}/>
  {scope.error&&<MessageBanner action={<Button variant="view" onClick={scope.refresh}>{ui("tryAgain")}</Button>}>{scope.error}</MessageBanner>}{scope.property&&!scope.property.isEnabled&&<MessageBanner kind="warning">{t("inactiveProperty")}</MessageBanner>}
  <div className="mb-6 grid gap-4 sm:grid-cols-3"><SummaryCard title={t("recordedTotal")} value={loading?"…":data?formatCurrency(Number(data.summary.recordedAmount),locale):"—"} note={t("filteredSummary")} icon={WalletCards}/><SummaryCard title={t("cancelledTotal")} value={loading?"…":data?formatCurrency(Number(data.summary.cancelledAmount),locale):"—"} note={t("filteredSummary")} icon={CircleX}/><SummaryCard title={t("recordCount")} value={data?formatNumber(data.totalCount,locale):"—"} note={t("recordedCount",{count:data?.summary.recordedCount??0})} icon={ReceiptText}/></div>
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-label={t("list")}><ListToolbar term={term} onTerm={setTerm} searchLabel={t("search")} placeholder={t("search")} loading={loading} onRefresh={()=>setRevision(n=>n+1)} onReset={reset} resetEnabled={!!term||!!status||!!method||!!from||!!to||!!unit||sort!=="paidDate:desc"}/>
   <div className="grid gap-3 border-b p-4 sm:grid-cols-2 lg:grid-cols-5"><div><Label htmlFor="payments-from">{t("from")}</Label><Input id="payments-from" type="date" value={from} onInput={event=>setFrom(event.currentTarget.value)}/></div><div><Label htmlFor="payments-to">{t("to")}</Label><Input id="payments-to" type="date" value={to} onInput={event=>setTo(event.currentTarget.value)}/></div><div><Label htmlFor="payments-method">{t("method")}</Label><select id="payments-method" className={toolbarSelectClass+" w-full"} value={method} onChange={event=>setMethod(event.target.value)}><option value="">{t("allMethods")}</option><option value="CASH">{t("CASH")}</option><option value="BANK_TRANSFER">{t("BANK_TRANSFER")}</option></select></div><div><Label htmlFor="payments-status">{ui("status")}</Label><select id="payments-status" className={toolbarSelectClass+" w-full"} value={status} onChange={event=>setStatus(event.target.value)}><option value="">{t("allStatuses")}</option><option value="RECORDED">{t("RECORDED")}</option><option value="CANCELLED">{t("CANCELLED")}</option></select></div><div><Label htmlFor="payments-sort">{t("sorting")}</Label><select id="payments-sort" className={toolbarSelectClass+" w-full"} value={sort} onChange={event=>setSort(event.target.value)}><option value="paidDate:desc">{t("latestDate")}</option><option value="paidDate:asc">{t("earliestDate")}</option><option value="amount:desc">{t("largestAmount")}</option><option value="createdAt:desc">{ui("newestFirst")}</option></select></div></div>
   {unit&&<p className="px-5 pt-4 text-sm">{ui("unit")}: <strong>{unit.name}</strong><Button variant="outline" size="sm" onClick={()=>setUnit(null)}>{ui("close")}</Button></p>}
   <RecordList compact data={data} loading={loading} error={error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} footerControls={<select className={toolbarSelectClass} aria-label={ui("recordsPerPage")} value={pageSize} onChange={event=>setPageSize(Number(event.target.value))}>{[10,20,50].map(n=><option value={n} key={n}>{n}</option>)}</select>} empty={<><ReceiptText className="mx-auto mb-3 size-8"/><strong>{t("empty")}</strong><p className="mt-2">{t("emptyHelp")}</p></>} card={record=><><div className="mb-3 flex flex-wrap justify-between gap-2"><strong>{name(record)}</strong><PaymentStatus record={record}/></div><p className="text-sm">{formatDateOnly(record.paidDate,locale)} · {t(record.method)}</p><p className="my-3 text-lg font-semibold">{formatCurrency(Number(record.amount),locale)}</p>{actions(record)}</>}/>
  </section>
  {creating&&<PaymentEditor propertyId={propertyId} onClose={()=>setCreating(false)} onSaved={changed}/>} {detail&&<PaymentDetail propertyId={propertyId} target={detail} parentEnabled={!!enabled} onClose={()=>setDetail(null)} onSaved={changed}/>}
 </div>;
}
