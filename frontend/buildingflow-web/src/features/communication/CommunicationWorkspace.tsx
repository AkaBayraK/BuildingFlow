"use client";
import {useEffect,useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {Megaphone,FileText,Plus,Eye,Paperclip,LayoutGrid} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useSearch} from "@/features/people/useRows";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Badge} from "@/components/ui/badge";
import {Label} from "@/components/ui/label";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,ListToolbar,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {FilterChips} from "@/components/management/FilterChips";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {AnnouncementPanel,DocumentPanel} from "./CommunicationPanels";
import {AnnouncementCategoryBadge,AnnouncementCategoryIcon} from "./AnnouncementCategory";
import {categoryStyle} from "./category-style";
import {announcementCategories,readAnnouncement,readDocument,readContentPage,type ContentPage,type Announcement,type DocumentRecord} from "./model";
export function CommunicationWorkspace({propertyId,section}:{propertyId:string;section:"announcements"|"documents"}){
 const t=useTranslations("communication");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const identity=useIdentity();const scope=usePropertyScope();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));const writable=manager&&scope.property?.id===propertyId&&scope.property.isEnabled&&!scope.error;
 const [rows,setRows]=useState<ContentPage<Announcement|DocumentRecord>|null>(null);const [loading,setLoading]=useState(true);const [error,setError]=useState("");const [revision,setRevision]=useState(0);const [term,setTerm]=useState("");const search=useSearch(term);const [status,setStatus]=useState("");const [category,setCategory]=useState("");const [page,setPage]=useState(1);const [pageSize,setPageSize]=useState(10);const [open,setOpen]=useState(false);const [record,setRecord]=useState<Announcement|DocumentRecord|undefined>();
 const announcements=section==="announcements";const query=new URLSearchParams({page:String(page),pageSize:String(pageSize),search});if(announcements&&manager&&status)query.set("status",status);if(announcements&&category)query.set("category",category);const path=`/api/v1/properties/${propertyId}/${section}?${query}`;
 useEffect(()=>setPage(1),[search,status,category,pageSize,section]);
 useEffect(()=>{const controller=new AbortController();setRows(null);setLoading(true);setError("");void api<unknown>(path,{signal:controller.signal}).then(value=>{const result=readContentPage<Announcement|DocumentRecord>(value,row=>announcements?readAnnouncement(row,propertyId):readDocument(row,propertyId));if(!controller.signal.aborted)setRows(result);}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});return()=>controller.abort();},[api,path,propertyId,announcements,locale,revision]);
 useEffect(()=>{if(rows&&page>Math.max(1,Math.ceil(rows.totalCount/pageSize)))setPage(Math.max(1,Math.ceil(rows.totalCount/pageSize)));},[rows,page,pageSize]);
 const show=(value:Announcement|DocumentRecord)=>{setRecord(value);setOpen(true);};
 const columns=useMemo<ColumnDef<Announcement|DocumentRecord>[]>(()=>[
  {id:"name",header:t(announcements?"title":"file"),cell:({row})=>"category" in row.original?<div className="flex max-w-md items-start gap-3"><AnnouncementCategoryIcon category={row.original.category} label={t(`category_${row.original.category}`)}/><div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><strong className="line-clamp-2 break-words">{row.original.title}</strong><AnnouncementCategoryBadge category={row.original.category} label={t(`category_${row.original.category}`)}/></div><p className="mt-1 line-clamp-2 break-words text-xs text-muted-foreground">{row.original.content}</p></div></div>:<div className="max-w-80"><strong className="line-clamp-2 break-words">{row.original.fileName}</strong></div>},
  {id:"audience",header:t("audience"),cell:({row})=>"announcementId" in row.original&&row.original.announcementId?t("attachment"):t(row.original.audience)},
  {id:"state",header:announcements?ui("status"):t("size"),cell:({row})=>"status" in row.original?<Badge variant="outline">{t(row.original.status)}</Badge>:<span>{(row.original.size/1024).toLocaleString(locale,{maximumFractionDigits:1})} KiB</span>},
  {id:"date",header:t("createdAt"),cell:({row})=>formatDateTime(row.original.createdAt,locale)},
  {id:"actions",header:ui("actions"),cell:({row})=><Button variant="view" size="icon" aria-label={`${t("view")} · ${"title" in row.original?row.original.title:row.original.fileName}`} onClick={()=>show(row.original)}><Eye/></Button>}
 ],[t,ui,locale,announcements]);
 const changed=()=>{setRevision(n=>n+1);scope.refresh();};const Icon=announcements?Megaphone:FileText;
 return <div className="py-8"><WorkspaceHeading title={t(section)} description={t(announcements?"announcementHelp":"documentHelp")} scope={scope.property?.name??ui("loading")} icon={Icon} action={manager?<Button disabled={!writable} onClick={()=>{setRecord(undefined);setOpen(true);}}><Plus/>{t(announcements?"newAnnouncement":"uploadDocument")}</Button>:undefined}/>
  {scope.error&&<MessageBanner action={<Button variant="view" onClick={scope.refresh}>{ui("tryAgain")}</Button>}>{scope.error}</MessageBanner>}{scope.property&&!scope.property.isEnabled&&<MessageBanner kind="warning">{t("inactive")}</MessageBanner>}
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><ListToolbar term={term} onTerm={setTerm} searchLabel={t(announcements?"searchAnnouncements":"searchDocuments")} placeholder={t(announcements?"searchAnnouncements":"searchDocuments")} loading={loading} onRefresh={()=>setRevision(n=>n+1)} onReset={()=>{setTerm("");setStatus("");setCategory("");setPage(1);}} resetEnabled={!!term||!!status||!!category}/>
   {announcements&&<div className="border-b p-4 sm:px-5"><FilterChips label={t("categoryFilter")} value={category} onChange={setCategory} disabled={loading&&!rows} items={[{value:"",label:t("allCategories"),count:rows?.categoryCounts?.reduce((a,c)=>a+c.count,0),icon:LayoutGrid},...announcementCategories.map(c=>({value:c,label:t(`category_${c}`),count:rows?.categoryCounts?.find(x=>x.category===c)?.count,icon:categoryStyle[c].icon,tone:categoryStyle[c].tone}))]}/></div>}
   {announcements&&manager&&<div className="border-b p-4"><Label htmlFor="announcement-status-filter">{ui("status")}</Label><select id="announcement-status-filter" value={status} className={toolbarSelectClass+" w-full sm:w-56"} onChange={e=>setStatus(e.target.value)}><option value="">{t("allStatuses")}</option>{["DRAFT","PUBLISHED","ARCHIVED"].map(value=><option value={value} key={value}>{t(value)}</option>)}</select></div>}
   <RecordList compact data={rows} loading={loading} error={error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} footerControls={<select className={toolbarSelectClass} aria-label={ui("recordsPerPage")} value={pageSize} onChange={e=>setPageSize(Number(e.target.value))}>{[10,20,50].map(n=><option value={n} key={n}>{n}</option>)}</select>} empty={<><Icon className="mx-auto mb-3 size-8"/><strong>{t(announcements?"emptyAnnouncements":"emptyDocuments")}</strong><p className="mt-2">{t("emptyHelp")}</p></>} card={value=><>{"category" in value&&<div className="mb-2"><AnnouncementCategoryBadge category={value.category} label={t(`category_${value.category}`)}/></div>}<strong className="block break-words">{"title" in value?value.title:value.fileName}</strong><div className="my-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">{"status" in value?<Badge variant="outline">{t(value.status)}</Badge>:<Paperclip className="size-4"/>}{t(value.audience)}</div><p className="mb-3 text-xs text-muted-foreground">{formatDateTime(value.createdAt,locale)}</p><Button variant="view" size="sm" onClick={()=>show(value)}><Eye/>{t("view")}</Button></>}/>
  </section>
  {open&&(announcements?<AnnouncementPanel propertyId={propertyId} target={record as Announcement|undefined} canWrite={!!writable} onClose={()=>setOpen(false)} onSaved={changed}/>:<DocumentPanel propertyId={propertyId} record={record as DocumentRecord|undefined} canWrite={!!writable} onClose={()=>setOpen(false)} onSaved={changed}/>)}
 </div>;
}
