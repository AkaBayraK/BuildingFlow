"use client";
import {useEffect,useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {CalendarCheck2,Eye,LayoutGrid,Pencil,Plus} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useSearch} from "@/features/people/useRows";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {RecordList} from "@/components/management/RecordList";
import {FilterChips} from "@/components/management/FilterChips";
import {WorkspaceHeading,ListToolbar,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateOnly} from "@/lib/format";
import {MaintenancePanel} from "./MaintenancePanel";
import {MaintenanceStatusBadge,MaintenanceType} from "./MaintenanceType";
import {typeStyle} from "./type-style";
import {maintenanceStatuses,maintenanceTypes,readPlanPage,type MaintenancePage,type MaintenancePlan,type MaintenanceType as Type} from "./model";

type Locale="tr"|"en";
export function MaintenanceWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("maintenance"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),identity=useIdentity(),scope=usePropertyScope();
 const allowed=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(r=>identity.hasRealmRole(r)),manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r));
 const canWrite=manager&&scope.property?.id===propertyId&&!!scope.property.isEnabled&&!scope.error;
 const [data,setData]=useState<MaintenancePage|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 const [term,setTerm]=useState(""),search=useSearch(term),[type,setType]=useState(""),[status,setStatus]=useState(""),[from,setFrom]=useState(""),[to,setTo]=useState(""),[page,setPage]=useState(1),[pageSize,setPageSize]=useState(10);
 const [panel,setPanel]=useState<{plan?:MaintenancePlan;edit:boolean}|null>(null);
 const rangeInvalid=!!from&&!!to&&from>to;
 const query=new URLSearchParams({page:String(page),pageSize:String(pageSize)});if(search)query.set("search",search);if(type)query.set("type",type);if(status)query.set("status",status);if(from)query.set("from",from);if(to)query.set("to",to);
 const path=`/api/v1/properties/${propertyId}/maintenance-plans?${query}`;
 useEffect(()=>setPage(1),[search,type,status,from,to,pageSize]);
 useEffect(()=>{if(!allowed||rangeInvalid)return;const c=new AbortController();setLoading(true);setError("");setData(null);
  void api<unknown>(path,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPlanPage(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();},[api,path,propertyId,locale,revision,allowed,rangeInvalid]);
 useEffect(()=>{if(data&&page>Math.max(1,Math.ceil(data.totalCount/pageSize)))setPage(Math.max(1,Math.ceil(data.totalCount/pageSize)));},[data,page,pageSize]);
 const refresh=()=>setRevision(n=>n+1);
 const counts=data?.typeCounts;const total=counts?.reduce((a,c)=>a+c.count,0);
 const chips=[{value:"",label:t("all"),count:total,icon:LayoutGrid},...maintenanceTypes.map(v=>({value:v,label:t(`type_${v}`),count:counts?.find(c=>c.type===v)?.count,icon:typeStyle[v].icon,tone:typeStyle[v].tone}))];
 const columns=useMemo<ColumnDef<MaintenancePlan>[]>(()=>[
  {id:"date",header:t("date"),cell:({row})=><time dateTime={row.original.plannedDate} className="whitespace-nowrap">{formatDateOnly(row.original.plannedDate,locale)}</time>},
  {id:"title",header:t("title"),cell:({row})=><div className="max-w-72"><strong className="line-clamp-2 break-words">{row.original.title}</strong>{row.original.description&&<p className="mt-1 line-clamp-1 break-words text-xs text-muted-foreground">{row.original.description}</p>}</div>},
  {id:"type",header:t("type"),cell:({row})=><MaintenanceType type={row.original.type} label={t(`type_${row.original.type}`)}/>},
  {id:"location",header:t("location"),cell:({row})=>row.original.location??"—"},
  {id:"responsible",header:t("responsible"),cell:({row})=>row.original.responsible??"—"},
  {id:"status",header:ui("status"),cell:({row})=><MaintenanceStatusBadge status={row.original.status} label={t(`status_${row.original.status}`)}/>},
  {id:"actions",header:ui("actions"),cell:({row})=><div className="flex gap-2"><Button variant="view" size="icon" aria-label={`${t("view")} · ${row.original.title}`} onClick={()=>setPanel({plan:row.original,edit:false})}><Eye/></Button>{canWrite&&<Button variant="edit" size="icon" aria-label={`${t("edit")} · ${row.original.title}`} onClick={()=>setPanel({plan:row.original,edit:true})}><Pencil/></Button>}</div>},
 ],[t,ui,locale,canWrite]);
 if(!allowed)return <div className="py-8"><MessageBanner>{ui("financeRoleRequired")}</MessageBanner></div>;
 return <div className="py-8">
  <WorkspaceHeading title={t("title_page")} description={t("help")} scope={scope.property?.name??ui("loading")} icon={CalendarCheck2} action={manager?<Button disabled={!canWrite} onClick={()=>setPanel({edit:true})}><Plus/>{t("new")}</Button>:undefined}/>
  {scope.error&&<MessageBanner action={<Button variant="view" onClick={scope.refresh}>{ui("tryAgain")}</Button>}>{scope.error}</MessageBanner>}
  {scope.property&&!scope.property.isEnabled&&<MessageBanner kind="warning">{t("inactive")}</MessageBanner>}
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm">
   <div className="border-b p-4 sm:px-5"><FilterChips label={t("typeFilter")} items={chips} value={type} onChange={setType} disabled={loading&&!data}/></div>
   <ListToolbar term={term} onTerm={setTerm} searchLabel={t("search")} placeholder={t("search")} loading={loading} onRefresh={refresh} onReset={()=>{setTerm("");setType("");setStatus("");setFrom("");setTo("");}} resetEnabled={!!term||!!type||!!status||!!from||!!to}>
    <div className="flex flex-wrap items-center gap-2"><Label htmlFor="plan-from" className="sr-only">{t("from")}</Label><Input id="plan-from" type="date" className="h-10 w-auto bg-card" min="2000-01-01" max="2100-12-31" value={from} aria-invalid={rangeInvalid} title={t("from")} onChange={e=>setFrom(e.target.value)}/><span aria-hidden="true">–</span><Label htmlFor="plan-to" className="sr-only">{t("to")}</Label><Input id="plan-to" type="date" className="h-10 w-auto bg-card" min="2000-01-01" max="2100-12-31" value={to} aria-invalid={rangeInvalid} title={t("to")} onChange={e=>setTo(e.target.value)}/></div>
    <select aria-label={ui("status")} className={toolbarSelectClass} value={status} onChange={e=>setStatus(e.target.value)}><option value="">{t("allStatuses")}</option>{maintenanceStatuses.map(s=><option key={s} value={s}>{t(`status_${s}`)}</option>)}</select>
   </ListToolbar>
   {rangeInvalid?<div className="p-4"><MessageBanner kind="warning">{t("rangeInvalid")}</MessageBanner></div>:
   <RecordList compact data={data} loading={loading} error={error} columns={columns} page={page} onPage={setPage} onRetry={refresh}
    footerControls={<select className={toolbarSelectClass} aria-label={ui("recordsPerPage")} value={pageSize} onChange={e=>setPageSize(Number(e.target.value))}>{[10,20,50].map(n=><option value={n} key={n}>{n}</option>)}</select>}
    empty={<><CalendarCheck2 className="mx-auto mb-3 size-8"/><strong>{t("empty")}</strong><p className="mt-2">{t("emptyHelp")}</p></>}
    card={p=><><div className="flex items-start justify-between gap-3"><strong className="break-words">{p.title}</strong><MaintenanceStatusBadge status={p.status} label={t(`status_${p.status}`)}/></div>
     <div className="my-3 flex flex-wrap items-center gap-3 text-xs text-muted-foreground"><MaintenanceType type={p.type} label={t(`type_${p.type}`)}/><time dateTime={p.plannedDate}>{formatDateOnly(p.plannedDate,locale)}</time></div>
     <p className="mb-3 text-xs text-muted-foreground">{[p.location,p.responsible].filter(Boolean).join(" · ")||"—"}</p>
     <div className="flex flex-wrap gap-2"><Button variant="view" size="sm" onClick={()=>setPanel({plan:p,edit:false})}><Eye/>{t("view")}</Button>{canWrite&&<Button variant="edit" size="sm" onClick={()=>setPanel({plan:p,edit:true})}><Pencil/>{t("edit")}</Button>}</div></>}/>}
  </section>
  {panel&&<MaintenancePanel propertyId={propertyId} target={panel.plan} editing={panel.edit} canWrite={canWrite} initialType={(type||undefined) as Type|undefined} onClose={()=>setPanel(null)} onSaved={refresh}/>}
 </div>;
}
