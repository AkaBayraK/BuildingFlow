"use client";
import Link from "next/link";
import {useRouter} from "next/navigation";
import {useCallback,useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Users,Plus} from "lucide-react";
import type {ColumnDef} from "@tanstack/react-table";
import {useIdentity} from "@/auth/AuthGate";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,ListToolbar,StatusBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {formatDateTime} from "@/lib/format";
import {useRows,useSearch} from "@/features/people/useRows";
import {PersonEditor} from "./PersonEditor";
import {Choices} from "./Choices";
import {useCenterRecord} from "./hooks";
import {pageOf,workspace,summary,personRoot,type Summary} from "./model";
export function CenterList({tenant}:{tenant?:string}){
 const identity=useIdentity(),ui=useTranslations("ui");
 if(!["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r)))return <MessageBanner>{ui("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner>;
 return <List key={(identity.subject??"")+":"+(tenant??"")} tenant={tenant}/>;
}
function List({tenant}:{tenant?:string}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui"),locale=useLocale(),router=useRouter();const [revision,setRevision]=useState(0),[term,setTerm]=useState(""),[page,setPage]=useState(1),[size,setSize]=useState(10),[sort,setSort]=useState("name:asc"),[editor,setEditor]=useState(false);const search=useSearch(term);const [sortBy,sortDirection]=sort.split(":");
 const decode=useCallback((v:unknown)=>pageOf(v,row=>summary(row,tenant??"")),[tenant]);const rows=useRows<Summary>(tenant?`${personRoot(tenant)}?${new URLSearchParams({page:String(page),pageSize:String(size),search,sortBy,sortDirection})}`:"",revision,!!tenant,decode);
 useEffect(()=>setPage(1),[search,sort,size]);useEffect(()=>{if(rows.data&&page>Math.max(1,Math.ceil(rows.data.totalCount/size)))setPage(Math.max(1,Math.ceil(rows.data.totalCount/size)));},[rows.data,page,size]);
 const refresh=()=>setRevision(n=>n+1);
 const action=(r:Summary)=><Button className="min-h-11" asChild variant="outline"><Link href={`/people/${r.tenantId}/${r.id}`}>{t("open")}</Link></Button>;
 const columns:ColumnDef<Summary>[]=[{id:"name",header:ui("fullName"),cell:({row})=><Link className="font-medium text-primary underline-offset-4 hover:underline" href={`/people/${row.original.tenantId}/${row.original.id}`}>{row.original.displayName}</Link>},{id:"kind",header:ui("personKind"),cell:({row})=>ui(row.original.kind==="LEGAL"?"personLegal":"personNatural")},{id:"status",header:ui("status"),cell:({row})=><StatusBadge enabled={row.original.isEnabled}/>},{id:"created",header:t("created"),cell:({row})=>formatDateTime(row.original.createdAt,locale)},{id:"actions",header:ui("actions"),cell:({row})=>action(row.original)}];
 return <div className="py-8"><WorkspaceHeading scope={t("workspace")} title={t("title")} description={t("description")} icon={Users} action={tenant?<Button onClick={()=>setEditor(true)}><Plus/>{ui("newPerson")}</Button>:undefined}/><div className="mb-6 max-w-xl"><Choices path="/api/v2/person-workspaces" id="center-tenant" label={t("workspace")} parse={workspace} value={tenant??""} onChange={v=>{if(v)router.push(`/people/${v.id}`);}}/>{tenant&&<TenantName key={tenant} tenant={tenant}/>}</div>{tenant?<section aria-label={t("title")} className="overflow-hidden rounded-2xl border bg-card"><ListToolbar term={term} onTerm={setTerm} searchLabel={ui("searchPeople")} placeholder={ui("personSearch")} loading={rows.loading} onRefresh={refresh} onReset={()=>{setTerm("");setSort("name:asc");setPage(1);}} resetEnabled={!!term||sort!=="name:asc"}><select className={toolbarSelectClass} aria-label={ui("peopleSorting")} value={sort} onChange={e=>setSort(e.target.value)}><option value="name:asc">{ui("lastNameAZ")}</option><option value="name:desc">{ui("lastNameZA")}</option><option value="createdAt:desc">{ui("newestFirst")}</option></select></ListToolbar><RecordList {...rows} columns={columns} page={page} onPage={setPage} onRetry={refresh} empty={t("empty")} card={r=><><p className="font-semibold">{r.displayName}</p><p className="my-3"><StatusBadge enabled={r.isEnabled}/></p>{action(r)}</>} footerControls={<select className={toolbarSelectClass} aria-label={ui("peoplePerPage")} value={size} onChange={e=>setSize(Number(e.target.value))}>{[10,20,50].map(n=><option key={n}>{n}</option>)}</select>}/></section>:<MessageBanner kind="info">{t("chooseWorkspace")}</MessageBanner>}{editor&&tenant&&<PersonEditor tenant={tenant} target={null} onClose={()=>setEditor(false)} onSaved={refresh}/>}</div>;
}
function TenantName({tenant}:{tenant:string}){const data=useCenterRecord(`/api/v2/tenants/${tenant}/person-workspace`,workspace);return data.error?<MessageBanner>{data.error}</MessageBanner>:<p className="mt-2 text-sm text-muted-foreground">{data.value?.name}</p>;}
