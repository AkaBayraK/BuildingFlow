"use client";

import {useLocale,useTranslations} from "next-intl";
import {useEffect,useRef,useState} from "react";
import Link from "next/link";
import {Users,Plus,Pencil,Trash2,DoorOpen,Eye,Phone,Mail,UserRoundCheck,Send} from "lucide-react";
import type {ColumnDef} from "@tanstack/react-table";
import {useIdentity} from "@/auth/AuthGate";
import {errorMessage,useAuthorizedApi,type Property} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Badge} from "@/components/ui/badge";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {RecordList} from "@/components/management/RecordList";
import {PersonActionsPanel} from "./PersonActionsPanel";
import {parsePerson,parseManagementPage} from "@/services/management-contracts";
import {useCallback} from "react";
import {RecordDetails} from "@/features/hierarchy/RecordDetails";
import {WorkspaceHeading,SummaryCard,IdentityMark,ListToolbar,StatusBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {EntityEditor,DeleteEntity} from "@/features/hierarchy/EntityEditor";
import {formatNumber} from "@/lib/format";
import {personName,occupancyTypeNames,type Person} from "./types";
import {useRows,useSearch} from "./useRows";

export function PeopleWorkspace({propertyId}:{propertyId:string}){
  const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const api=useAuthorizedApi();const identity=useIdentity();
  const canManage=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
  const [property,setProperty]=useState<Property|null>(null);const [propertyError,setPropertyError]=useState("");const [revision,setRevision]=useState(0);
  const [term,setTerm]=useState("");const search=useSearch(term);const [page,setPage]=useState(1);const [pageSize,setPageSize]=useState(10);const [sort,setSort]=useState("name:asc");
  const [editor,setEditor]=useState<{target:Person|null;focusReturnTarget?:HTMLElement|null}|null>(null);const [deleting,setDeleting]=useState<Person|null>(null);const [details,setDetails]=useState<Person|null>(null);const [notice,setNotice]=useState("");
  const detailTrigger=useRef<HTMLElement|null>(null);
  function openDetails(value:Person){detailTrigger.current=document.activeElement instanceof HTMLElement?document.activeElement:null;setDetails(value);}
  const [personActions,setPersonActions]=useState<Person|null>(null);
  const decode=useCallback((value:unknown)=>parseManagementPage(value,row=>parsePerson(row,propertyId)),[propertyId]);
  const root=`/api/v1/properties/${encodeURIComponent(propertyId)}`;const [sortBy,sortDirection]=sort.split(":");
  const rows=useRows<Person>(`${root}/people?${new URLSearchParams({page:String(page),pageSize:String(pageSize),search,sortBy,sortDirection})}`,revision,canManage,decode);
  useEffect(()=>setPage(1),[search,sort,pageSize]);
  useEffect(()=>{if(rows.data){const last=Math.max(1,Math.ceil(rows.data.totalCount/pageSize));if(page>last)setPage(last);}},[rows.data,pageSize,page]);
  useEffect(()=>{
    const controller=new AbortController();setProperty(null);setPropertyError("");
    if(canManage)void api<Property>(root,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setProperty(value);}).catch(reason=>{if(!controller.signal.aborted)setPropertyError(errorMessage(reason,locale));});
    return()=>controller.abort();
  },[api,root,revision,canManage,locale]);
  const enabled=!!(property?.id===propertyId&&property.isEnabled&&!propertyError);
  const refresh=()=>setRevision(value=>value+1);
  const changed=(message:string)=>{setNotice(message);refresh();};
  const identityCell=(person:Person)=><div className="flex items-center gap-3"><IdentityMark>{person.firstName.slice(0,1)}{person.lastName.slice(0,1)}</IdentityMark><div className="min-w-0"><Button variant="link" className="h-auto max-w-60 justify-start truncate p-0 font-semibold text-foreground" onClick={()=>openDetails(person)}>{personName(person)}</Button><span className="mt-1 block text-xs text-muted-foreground">{t("ongoingCount",{count:person.activeOccupancyCount})}</span></div></div>;
  const contacts=(person:Person)=><div className="space-y-1.5 text-xs text-muted-foreground"><span className="flex items-start gap-2"><Phone aria-hidden="true" className="size-3.5 shrink-0"/><span className="wrap-anywhere">{person.phoneNumber??t("noPhone")}</span></span><span className="flex items-start gap-2"><Mail aria-hidden="true" className="size-3.5 shrink-0"/><span className="wrap-anywhere">{person.email??t("noEmail")}</span></span></div>;
  const actions=(person:Person)=><div className="flex flex-wrap justify-end gap-1"><Button variant="ghost" size="icon-sm" aria-label={m("viewRecord",{name:personName(person)})} onClick={()=>openDetails(person)}><Eye/></Button><Button asChild variant="ghost" size="icon-sm" aria-label={m("managePersonRelationships",{name:personName(person)})}><Link href={`/properties/${propertyId}/people/${person.id}`}><DoorOpen/></Link></Button><Button variant="ghost" size="icon-sm" aria-label={t("editRecord",{name:personName(person)})} disabled={!enabled} onClick={()=>setEditor({target:person})}><Pencil/></Button><Button variant="ghost" size="icon-sm" aria-label={t("personActions")} onClick={()=>setPersonActions(person)}><Send/></Button><Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:text-destructive" aria-label={t("deleteRecord",{name:personName(person)})} disabled={!enabled} onClick={()=>setDeleting(person)}><Trash2/></Button></div>;
  const columns:ColumnDef<Person>[]=[{id:"name",header:t("fullName"),cell:({row})=>identityCell(row.original)},...([{id:"units",label:"unit",render:(p:Person)=>[...new Set(p.units?.map(u=>u.number))].join(", ")||"—"},{id:"blocks",label:"buildingBlock",render:(p:Person)=>[...new Set(p.units?.map(u=>u.buildingName).filter(Boolean))].join(", ")||"—"},{id:"property",label:"personPropertyColumn",render:(p:Person)=>p.propertyName??property?.name??"—"},{id:"kind",label:"personKind",render:(p:Person)=>p.personKind?t(p.personKind==="LEGAL"?"personLegal":"personNatural"):t("notSpecified")},{id:"role",label:"personRoleColumn",render:(p:Person)=>[...new Set(p.units?.map(u=>t(occupancyTypeNames[u.role])))].join(", ")||"—"},{id:"citizenship",label:"personCitizenship",render:(p:Person)=>p.citizenship?`${t(p.citizenship==="FOREIGN"?"personForeign":"personDomestic")} · ${p.countryCode}`:t("notSpecified")},{id:"phone",label:"phone",render:(p:Person)=>p.phoneNumber??"—"},{id:"email",label:"email",render:(p:Person)=>p.email??"—"}]).map(c=>({id:c.id,header:t(c.label),cell:({row}:{row:{original:Person}})=>c.render(row.original)})),{id:"status",header:t("status"),cell:({row})=><StatusBadge enabled={row.original.isEnabled}/>},{id:"actions",header:t("actions"),cell:({row})=>actions(row.original)}];
  if(!canManage)return <div className="py-12"><h1 className="text-2xl font-bold">{t("people")}</h1><MessageBanner className="my-4">{t("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner><Button asChild variant="outline" className="mt-5"><Link href={`/properties/${propertyId}`}>{t("backToProperty2")}</Link></Button></div>;
  return <div className="py-8">
    <WorkspaceHeading title={t("people")} description={t("addAPersonThenManageTheirUnitRelationshipsAnd")} scope={property?.name??t("loading")} icon={Users} action={<Button disabled={!enabled} onClick={()=>setEditor({target:null})}><Plus/>{t("newPerson")}</Button>}/>
    <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      <SummaryCard title={search?m("searchResults"):m("peopleRecords")} value={rows.loading||rows.error?"—":formatNumber(rows.data?.totalCount??0,locale)} note={search?m("matchingPeople"):m("selectedPropertyPeople")} icon={Users}/>
      <SummaryCard title={t("numberOfUnits")} value={property?formatNumber(property.unitCount,locale):"—"} note={m("selectedPropertyUnits")} icon={DoorOpen}/>
      <div className="rounded-xl border bg-card p-5 shadow-sm sm:col-span-2 xl:col-span-1"><p className="mb-3 flex items-center gap-2 text-xs font-medium text-muted-foreground"><UserRoundCheck aria-hidden="true" className="size-4 text-primary"/>{m("relationshipRoles")}</p><div className="flex flex-wrap gap-2">{["owner","tenant","resident"].map(key=><Badge key={key} variant="secondary">{t(key)}</Badge>)}</div><p className="mt-3 text-xs leading-5 text-muted-foreground">{m("multipleRolesExplanation")}</p></div>
    </div>
    {propertyError&&<MessageBanner className="my-4" action={<Button variant="outline" onClick={refresh}>{t("tryAgain")}</Button>}>{propertyError}</MessageBanner>}
    {property&&!property.isEnabled&&<MessageBanner kind="warning" className="my-4">{t("thePropertyIsInactiveRecordsCanBeViewedBut")}</MessageBanner>}
    {notice&&<MessageBanner kind="success" className="my-4" announce={false}>{notice}</MessageBanner>}
    <section aria-label={t("peopleList")} className="overflow-hidden rounded-2xl border bg-card shadow-sm">
      <div className="flex items-center gap-3 border-b p-5"><Users aria-hidden="true" className="size-5 text-primary"/><h2 className="m-0 text-base font-semibold">{t("peopleList")}</h2><Badge variant="secondary">{rows.loading||rows.error?"…":formatNumber(rows.data?.totalCount??0,locale)}</Badge></div>
      <ListToolbar term={term} onTerm={setTerm} searchLabel={t("searchPeople")} placeholder={t("personSearch")} loading={rows.loading} onRefresh={refresh} onReset={()=>{setTerm("");setSort("name:asc");setPage(1);}} resetEnabled={!!term||sort!=="name:asc"}>
        <select aria-label={t("peopleSorting")} className={toolbarSelectClass} value={sort} onChange={event=>setSort(event.target.value)}><option value="name:asc">{t("lastNameAZ")}</option><option value="name:desc">{t("lastNameZA")}</option><option value="createdAt:desc">{t("newestFirst")}</option></select>
      </ListToolbar>
      <RecordList compact cardBreakpoint="xl" {...rows} columns={columns} page={page} onPage={setPage} onRetry={refresh}
        footerControls={<select aria-label={t("peoplePerPage")} className="h-8 rounded-md border bg-card px-2 text-xs" value={pageSize} onChange={event=>setPageSize(Number(event.target.value))}>{[10,20,50].map(n=><option key={n} value={n}>{n} {t("page")}</option>)}</select>}
        empty={<><IdentityMark icon={Users} className="mx-auto mb-4 size-14"/><strong>{search?t("noPeopleMatchYourSearch"):t("noPeopleHaveBeenAddedYet")}</strong>{!search&&<p className="mt-2">{t("addYourFirstOwnerTenantOrResidentWithNew")}</p>}</>}
        card={person=><><div className="mb-4 flex flex-wrap items-start justify-between gap-3">{identityCell(person)}<StatusBadge enabled={person.isEnabled}/></div>{contacts(person)}<p className="mt-3 text-sm">{person.propertyName} · {person.units?.map(u=>`${u.buildingName??""} ${u.number}`).join(", ")}</p><p className="mt-2 text-sm">{person.personKind?t(person.personKind==="LEGAL"?"personLegal":"personNatural"):t("notSpecified")} · {person.countryCode??"—"}</p><div className="mt-4 border-t pt-3">{actions(person)}</div></>}/>
    </section>
    {personActions&&<PersonActionsPanel person={personActions} propertyId={propertyId} onClose={()=>setPersonActions(null)}/>}
    {details&&<RecordDetails kind="person" url={`${root}/people`} target={details} propertyId={propertyId} canEdit={enabled} onClose={()=>setDetails(null)} onEdit={record=>{setDetails(null);setEditor({target:record,focusReturnTarget:detailTrigger.current});}}/>}
    {editor&&<EntityEditor kind="person" url={`${root}/people`} target={editor.target} focusReturnTarget={editor.focusReturnTarget} onClose={()=>setEditor(null)} onSaved={()=>changed(t("personSaved"))}/>}
    {deleting&&<DeleteEntity kind="person" url={`${root}/people`} target={deleting} onClose={()=>setDeleting(null)} onDeleted={()=>changed(t("personRemovedFromTheListHistoryPreserved"))}/>}
  </div>;
}
