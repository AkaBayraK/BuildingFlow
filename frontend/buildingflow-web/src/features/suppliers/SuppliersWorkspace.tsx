"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useTranslations} from "next-intl";
import {BriefcaseBusiness,Plus,Eye,Pencil,LayoutGrid} from "lucide-react";
import type {ColumnDef} from "@tanstack/react-table";
import {useIdentity} from "@/auth/AuthGate";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {RecordList} from "@/components/management/RecordList";
import {FilterChips} from "@/components/management/FilterChips";
import {WorkspaceHeading,ListToolbar,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {useRows,useSearch} from "@/features/people/useRows";
import {Choices} from "@/features/person-center/Choices";
import {workspace,type Workspace} from "@/features/person-center/model";
import {readSupplierPage,statuses,type Supplier} from "./model";
import {statusStyle} from "./supplier-style";
import {SupplierPanel} from "./SupplierPanel";
import {SupplierBadge} from "./SupplierBadge";
import {SupplierAlerts,SupplierPerformance} from "./SupplierInsights";
import {supplierViews} from "./supplier-style";
export function SuppliersWorkspace(){const identity=useIdentity(),t=useTranslations("suppliers");if(!["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r)))return <MessageBanner>{t("accessRequired")}</MessageBanner>;return <WorkspaceBody key={(identity.tokenParsed?.iss??"")+":"+(identity.subject??identity.tokenParsed?.sub??"")}/>;}
function WorkspaceBody(){
 const t=useTranslations("suppliers"),[tenant,setTenant]=useState<Workspace|null>(null),[view,setView]=useState<keyof typeof supplierViews>("suppliers");
 const body=!tenant?null:view==="alerts"?<SupplierAlerts key={"a"+tenant.id} tenant={tenant}/>:view==="performance"?<SupplierPerformance key={"p"+tenant.id} tenant={tenant}/>:<SupplierList key={tenant.id} tenant={tenant}/>;
 return <div className="page-enter"><WorkspaceHeading title={t("title")} description={t("help")} scope={tenant?.name??t("workspace")} icon={BriefcaseBusiness}/><div className="mb-6 max-w-md"><Choices path="/api/v2/supplier-workspaces" value={tenant?.id??""} onChange={setTenant} parse={workspace} id="supplier-workspace" label={t("workspace")}/></div>{tenant?<><div className="print-hidden mb-4"><FilterChips label={t("supplierView")} value={view} onChange={v=>setView(v as keyof typeof supplierViews)} items={(Object.keys(supplierViews) as (keyof typeof supplierViews)[]).map(v=>({value:v,label:t(`view_${v}`),icon:supplierViews[v].icon}))}/></div>{body}</>:<p className="rounded-xl border bg-card p-6 text-sm text-muted-foreground">{t("chooseWorkspace")}</p>}</div>;
}
function SupplierList({tenant}:{tenant:Workspace}){
 const returnTarget=useRef<HTMLElement|null>(null),newButton=useRef<HTMLButtonElement>(null);
 const t=useTranslations("suppliers"),ui=useTranslations("ui"),[revision,setRevision]=useState(0),[term,setTerm]=useState(""),[status,setStatus]=useState(""),[sort,setSort]=useState("name:asc"),[page,setPage]=useState(1),[size,setSize]=useState(10),[panel,setPanel]=useState<{record?:Supplier;edit:boolean}|null>(null);const search=useSearch(term),[sortBy,sortDirection]=sort.split(":");
 const query=new URLSearchParams({page:String(page),pageSize:String(size),search,sortBy,sortDirection,...(status?{status}:{})});const decode=useCallback((v:unknown)=>readSupplierPage(v,tenant.id),[tenant.id]);
 const rows=useRows<Supplier>(`/api/v2/tenants/${tenant.id}/suppliers?${query}`,revision,true,decode);
 useEffect(()=>setPage(1),[search,status,sort,size]);useEffect(()=>{if(rows.data&&page>Math.max(1,Math.ceil(rows.data.totalCount/size)))setPage(Math.max(1,Math.ceil(rows.data.totalCount/size)));},[rows.data,page,size]);
 const refresh=()=>setRevision(n=>n+1);const action=(r:Supplier)=><div className="flex flex-wrap gap-2"><Button variant="view" size="sm" onClick={e=>{returnTarget.current=e.currentTarget;setPanel({record:r,edit:false});}}><Eye aria-hidden="true"/>{t("view")}</Button><Button variant="edit" size="sm" onClick={e=>{returnTarget.current=e.currentTarget;setPanel({record:r,edit:true});}}><Pencil aria-hidden="true"/>{t("edit")}</Button></div>;
 const columns:ColumnDef<Supplier>[]=[{id:"name",header:t("name"),cell:({row})=><strong className="break-words">{row.original.name}</strong>},{id:"kind",header:t("kind"),cell:({row})=><SupplierBadge kind={row.original.kind}/>},{id:"status",header:ui("status"),cell:({row})=><SupplierBadge status={row.original.status}/>},{id:"actions",header:ui("actions"),cell:({row})=>action(row.original)}];
 return <><div className="mb-4 flex justify-end"><Button ref={newButton} onClick={e=>{returnTarget.current=e.currentTarget;setPanel({edit:true});}}><Plus aria-hidden="true"/>{t("create")}</Button></div><section className="overflow-hidden rounded-xl border bg-card"><FilterChips label={ui("status")} value={status} onChange={setStatus} items={[{value:"",label:t("all"),icon:LayoutGrid},...statuses.map(s=>({value:s,label:t(s),icon:statusStyle[s].icon}))]}/><ListToolbar term={term} onTerm={setTerm} searchLabel={t("search")} placeholder={t("search")} loading={rows.loading} onRefresh={refresh} onReset={()=>{setTerm("");setStatus("");setSort("name:asc");}} resetEnabled={!!term||!!status||sort!=="name:asc"}><select className={toolbarSelectClass} value={sort} aria-label={t("sort")} onChange={e=>setSort(e.target.value)}><option value="name:asc">{t("nameAsc")}</option><option value="name:desc">{t("nameDesc")}</option><option value="createdAt:desc">{t("newest")}</option></select></ListToolbar><RecordList compact {...rows} columns={columns} page={page} onPage={setPage} onRetry={refresh} footerControls={<select className={toolbarSelectClass} value={size} aria-label={ui("recordsPerPage")} onChange={e=>setSize(Number(e.target.value))}>{[10,20,50].map(v=><option key={v} value={v}>{v}</option>)}</select>} empty={<><BriefcaseBusiness aria-hidden="true" className="mx-auto mb-3 size-8"/><strong>{search||status?t("noMatches"):t("empty")}</strong><p className="mt-2">{t("emptyHelp")}</p></>} card={r=><><div className="mb-3 flex flex-wrap justify-between gap-2"><strong className="break-words">{r.name}</strong><SupplierBadge status={r.status}/></div><div className="mb-4"><SupplierBadge kind={r.kind}/></div>{action(r)}</>}/></section>{panel&&<SupplierPanel tenant={tenant.id} target={panel.record} editing={panel.edit} focusTarget={returnTarget.current} fallbackTarget={newButton.current} onClose={()=>setPanel(null)} onSaved={refresh}/>}</>;
}
