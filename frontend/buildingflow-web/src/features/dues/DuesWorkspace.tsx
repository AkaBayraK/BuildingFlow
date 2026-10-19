"use client";
import {useCallback,useEffect,useMemo,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {ReceiptText,Plus,RefreshCw,Search,TriangleAlert,CircleX,Eye,FilterX} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useSearch} from "@/features/people/useRows";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {formatCurrency,formatDateOnly} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Badge} from "@/components/ui/badge";
import {RecordList} from "@/components/management/RecordList";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {ChargeStatusBadge} from "./ChargeStatusBadge";
import {PaymentEditor,type UnitSelection} from "@/features/payments/PaymentDialogs";
import {AssessmentEditor,ChargeDetail} from "./DuesDialogs";
import {readDuesPage,type DuesPage,type DuesCharge,type DuesPersonChoice} from "./model";
import {ChargePeople,DuesPersonFilter} from "./ChargePeople";

export function DuesWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("dues");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const identity=useIdentity();const scope=usePropertyScope();
 const allowed=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(role=>identity.hasRealmRole(role));
 const peopleAllowed=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
 const [person,setPerson]=useState<DuesPersonChoice|null>(null);const [partyRole,setPartyRole]=useState('');
 const [revision,setRevision]=useState(0);const [data,setData]=useState<DuesPage|null>(null);const [loading,setLoading]=useState(true);const [error,setError]=useState("");
 const [term,setTerm]=useState("");const search=useSearch(term);const [page,setPage]=useState(1);const [pageSize,setPageSize]=useState(10);
 const [period,setPeriod]=useState("");const [status,setStatus]=useState("");const [dueFrom,setDueFrom]=useState("");const [dueTo,setDueTo]=useState("");const [sort,setSort]=useState("dueDate:desc");
 const [unit,setUnit]=useState<{id:string;name:string}|null>(null);const [creating,setCreating]=useState(false);const [detail,setDetail]=useState<DuesCharge|null>(null);
 const [payUnit,setPayUnit]=useState<UnitSelection|null>(null);const detailTrigger=useRef<HTMLElement|null>(null);
 const root="/api/v1/properties/"+encodeURIComponent(propertyId)+"/dues";
 const [sortBy,sortDirection]=sort.split(":");
 const query=new URLSearchParams({page:String(page),pageSize:String(pageSize),search,sortBy,sortDirection});
 if(period)query.set("period",period);if(status)query.set("status",status);if(dueFrom)query.set("dueFrom",dueFrom);if(dueTo)query.set("dueTo",dueTo);if(unit)query.set("unitId",unit.id);
 if(peopleAllowed&&person)query.set('personId',person.id);if(peopleAllowed&&partyRole)query.set('partyRole',partyRole);
 const url=root+"?"+query;
 const refresh=()=>setRevision(n=>n+1);
 const changed=()=>{setPage(1);refresh();scope.refresh();};
 useEffect(()=>setPage(1),[search,period,status,dueFrom,dueTo,sort,pageSize,unit?.id,person?.id,partyRole]);
 useEffect(()=>{
  const controller=new AbortController();setData(null);setError("");setLoading(allowed);
  if(allowed)void api<unknown>(url,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setData(readDuesPage(value,propertyId));})
   .catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
  return()=>controller.abort();
 },[api,url,revision,allowed,propertyId,locale]);
 const enabled=allowed&&scope.property?.id===propertyId&&scope.property.isEnabled&&!scope.error;
 const name=useCallback((value:DuesCharge)=>(value.buildingName?value.buildingName+" · ":"")+value.unitNumber,[]);
 const actions=useCallback((charge:DuesCharge)=><div className="flex flex-wrap gap-2"><Button variant="view" size="sm" onClick={event=>{detailTrigger.current=event.currentTarget;setDetail(charge);}}><Eye className="size-4"/>{ui("details")}</Button><Button variant="outline" size="sm" onClick={()=>setUnit({id:charge.unitId,name:name(charge)})}>{t("filterUnit")}</Button></div>,[name,t,ui]);
 const columns=useMemo<ColumnDef<DuesCharge>[]>(()=>[
  {id:"unit",header:ui("unit"),cell:({row})=><strong>{name(row.original)}</strong>},
  {id:"period",header:t("period"),cell:({row})=>row.original.period},
  ...(peopleAllowed?[{id:'people',header:t('chargePeople'),cell:({row}:{row:{original:DuesCharge}})=><ChargePeople charge={row.original}/>}]:[]),
  {id:"amount",header:t("amountBalance"),cell:({row})=><div><strong>{formatCurrency(Number(row.original.amount),locale)}</strong><span className="block text-xs text-muted-foreground">{t("balance")}: {formatCurrency(Number(row.original.balance),locale)}</span></div>},
  {id:"dueDate",header:t("dueDate"),cell:({row})=>formatDateOnly(row.original.dueDate,locale)},
  {id:"status",header:ui("status"),cell:({row})=><ChargeStatusBadge charge={row.original}/>},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}
 ],[actions,name,t,ui,locale,peopleAllowed]);
 if(!allowed)return <div className="py-10"><h1>{t("title")}</h1><MessageBanner>{ui("financeRoleRequired")}</MessageBanner></div>;
 return <div className="py-10">
  <div className="mb-6 flex flex-wrap items-center justify-between gap-4"><div><span className="text-xs font-semibold text-primary">{scope.property?.name??ui("loading")}</span><h1 className="mt-2 text-3xl font-bold">{t("title")}</h1><p className="mt-2 text-sm text-muted-foreground">{t("description")}</p></div><Button disabled={!enabled} onClick={()=>setCreating(true)}><Plus/>{t("newAssessment")}</Button></div>
  {scope.error&&<MessageBanner>{scope.error}<Button variant="view" onClick={scope.refresh}>{ui("tryAgain")}</Button></MessageBanner>}
  {scope.property&&!scope.property.isEnabled&&<MessageBanner kind="warning">{t("inactiveProperty")}</MessageBanner>}
  <MessageBanner kind="info" className="mb-6">{t("phaseBoundary")}</MessageBanner>
  <div className="mb-6 grid gap-4 sm:grid-cols-3">{([
   ["outstandingAmount","outstanding",ReceiptText],["overdueAmount","overdue",TriangleAlert],["cancelledAmount","cancelled",CircleX]
  ] as const).map(([key,label,Icon])=><section key={key} className="min-w-0 rounded-2xl border bg-card p-5 shadow-sm"><div className="flex items-center gap-2 text-sm text-muted-foreground"><Icon aria-hidden="true" className="size-5"/>{t(label)}</div><p className="mt-3 wrap-anywhere text-2xl font-semibold" aria-live="polite">{loading?ui("loading"):data?formatCurrency(Number(data.summary[key]),locale):"—"}</p><p className="mt-2 text-xs text-muted-foreground">{t("filteredSummary")}</p></section>)}</div>
  <section aria-label={t("list")} className="overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="flex flex-wrap items-center justify-between gap-3 border-b p-5"><h2 className="flex items-center gap-2 font-semibold"><ReceiptText className="size-5 text-primary"/>{t("list")}</h2><Button variant="view" disabled={loading} onClick={refresh}><RefreshCw className="size-4"/>{ui("refresh")}</Button></div>
   <div className="grid gap-3 p-5 sm:grid-cols-2 lg:grid-cols-4">
    {peopleAllowed&&<DuesPersonFilter root={root} propertyId={propertyId} selected={person} onChange={setPerson} revision={revision}/>}
    {peopleAllowed&&<div><Label htmlFor="dues-party-role">{t('partyRoleFilter')}</Label><select id="dues-party-role" className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={partyRole} onChange={e=>setPartyRole(e.target.value)}><option value="">{t('allRoles')}</option>{(['TENANT','OWNER','RESIDENT'] as const).map(role=><option key={role} value={role}>{t('party_'+role)}</option>)}</select></div>}
    <div className="relative sm:col-span-2"><Search aria-hidden="true" className="absolute left-3 top-3 size-4 text-muted-foreground"/><Input className="pl-9" value={term} maxLength={200} aria-label={t("search")} placeholder={t("search")} onChange={event=>setTerm(event.target.value)}/></div>
    <div><Label htmlFor="dues-period">{t("period")}</Label><Input id="dues-period" type="month" min="2000-01" value={period} onInput={event=>setPeriod(event.currentTarget.value)}/></div>
    <div><Label htmlFor="dues-status">{ui("status")}</Label><select id="dues-status" className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={status} onChange={event=>setStatus(event.target.value)}><option value="">{t("allStatuses")}</option>{(["OPEN","OVERDUE","PARTIALLY_PAID","PAID","CANCELLED"] as const).map(s=><option key={s} value={s}>{t(s)}</option>)}</select></div>
    <div><Label htmlFor="dues-from">{t("dueFrom")}</Label><Input id="dues-from" type="date" value={dueFrom} onInput={event=>setDueFrom(event.currentTarget.value)}/></div>
    <div><Label htmlFor="dues-to">{t("dueTo")}</Label><Input id="dues-to" type="date" value={dueTo} onInput={event=>setDueTo(event.currentTarget.value)}/></div>
    <div><Label htmlFor="dues-sort">{t("sorting")}</Label><select id="dues-sort" className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={sort} onChange={event=>setSort(event.target.value)}><option value="dueDate:desc">{t("latestDue")}</option><option value="dueDate:asc">{t("earliestDue")}</option><option value="period:desc">{t("latestPeriod")}</option><option value="amount:desc">{t("largestAmount")}</option><option value="createdAt:desc">{ui("newestFirst")}</option></select></div>
    <div><Label htmlFor="dues-page-size">{t("pageSize")}</Label><select id="dues-page-size" className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={pageSize} onChange={event=>setPageSize(Number(event.target.value))}>{[10,20,50].map(n=><option key={n} value={n}>{n}</option>)}</select></div>
   </div>
   {unit&&<div className="px-5 pb-4"><Badge variant="secondary">{ui("unit")}: {unit.name}</Badge><Button variant="outline" size="sm" onClick={()=>setUnit(null)}><FilterX className="size-4"/>{t("clearUnit")}</Button></div>}
   <RecordList data={data} loading={loading} error={error} columns={columns} page={page} onPage={setPage} onRetry={refresh} empty={<><ReceiptText className="mx-auto mb-3 size-8"/><strong>{t("empty")}</strong><p className="mt-2">{t("emptyHelp")}</p></>} card={charge=><><div className="mb-3 flex items-start justify-between gap-2"><strong className="wrap-anywhere">{name(charge)}</strong><ChargeStatusBadge charge={charge}/></div><p className="mb-2 text-sm">{charge.period} · {formatDateOnly(charge.dueDate,locale)}</p><p className="mb-3 text-lg font-semibold">{formatCurrency(Number(charge.balance),locale)}</p>{peopleAllowed&&<div className="mb-3"><ChargePeople charge={charge}/></div>}{actions(charge)}</>}/>
  </section>
  {creating&&<AssessmentEditor root={root} propertyId={propertyId} allowParties={peopleAllowed} onClose={()=>setCreating(false)} onSaved={changed}/>}
  {detail&&<ChargeDetail root={root} propertyId={propertyId} target={detail} parentEnabled={!!enabled} onPayment={charge=>{setDetail(null);setPayUnit({id:charge.unitId,name:name(charge)});}} onClose={()=>setDetail(null)} onSaved={changed}/>}
  {payUnit&&<PaymentEditor propertyId={propertyId} initialUnit={payUnit} focusReturnTarget={detailTrigger.current} onClose={()=>setPayUnit(null)} onSaved={changed}/>}
 </div>;
}
