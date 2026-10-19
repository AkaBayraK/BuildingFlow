"use client";

import {useEffect,useMemo,useRef,useState} from "react";
import Link from "next/link";
import {useRouter} from "next/navigation";
import {useUnitPeople,UnitPersonLinks} from "@/features/people/UnitPeople";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef,PaginationState,SortingState,OnChangeFn,VisibilityState} from "@tanstack/react-table";
import {ArrowUpDown,ArrowUpRight,Building2,DoorOpen,Eye,Pencil,Plus,Trash2} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Badge} from "@/components/ui/badge";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {RecordList} from "@/components/management/RecordList";
import {RecordDetails} from "@/features/hierarchy/RecordDetails";
import {IdentityMark,ListToolbar,StatusBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {useIdentity} from "@/auth/AuthGate";
import {errorMessage,useAuthorizedApi,unitTypeNames,type Unit,type Building,type PagedRows} from "@/services/api";
import {formatNumber} from "@/lib/format";
import {DeleteEntity,EntityEditor} from "./EntityEditor";

import {buildingDetailFields,unitDetailFields} from "@/features/property-details/model";
import {DetailValue,DetailValues} from "@/features/property-details/DetailFields";
import {parseBuilding,parseUnit,parseManagementPage} from "@/services/management-contracts";
import {DropdownMenu,DropdownMenuTrigger,DropdownMenuContent,DropdownMenuLabel,DropdownMenuCheckboxItem} from "@/components/ui/dropdown-menu";
export {StatusBadge} from "@/components/management/WorkspacePresentation";
type Row=Building|Unit;
export function HierarchyRows({kind,propertyId,buildingId,unitId,parentEnabled,onChanged}:{kind:"building"|"unit";propertyId:string;buildingId?:string;unitId?:string;parentEnabled:boolean;onChanged:()=>void}){
  const router=useRouter();const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const api=useAuthorizedApi();const identity=useIdentity();
  const canWrite=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
  const url=`/api/v1/properties/${encodeURIComponent(propertyId)}`+(kind==="building"?"/buildings":buildingId?`/buildings/${encodeURIComponent(buildingId)}/units`:"/units");
  const [data,setData]=useState<PagedRows<Row>|null>(null);const [error,setError]=useState("");const [loading,setLoading]=useState(true);
  const [pagination,setPagination]=useState<PaginationState>({pageIndex:0,pageSize:10});const [sorting,setSorting]=useState<SortingState>([{id:kind==="building"?"name":"number",desc:false}]);
  const [term,setTerm]=useState("");const [search,setSearch]=useState("");const [revision,setRevision]=useState(0);const [notice,setNotice]=useState("");
  const [editor,setEditor]=useState<{target:Row|null;focusReturnTarget?:HTMLElement|null}|null>(null);const [deleting,setDeleting]=useState<Row|null>(null);const [details,setDetails]=useState<Unit|null>(null);
  const extraFields=kind==="unit"?unitDetailFields:buildingDetailFields;const [visibility,setVisibility]=useState<VisibilityState>({floorPosition:false,isMezzanine:false,bathroomCount:false,kitchenType:false,hasBalcony:false,heatingType:false});
  const detailTrigger=useRef<HTMLElement|null>(null);
  function openDetails(value:Unit){detailTrigger.current=document.activeElement instanceof HTMLElement?document.activeElement:null;setDetails(value);}
  const label=kind==="building"?t("buildingBlock"):buildingId?t("unit"):t("apartment");const Icon=kind==="building"?Building2:DoorOpen;
  useEffect(()=>{const timer=setTimeout(()=>{setSearch(term.trim());setPagination(value=>({...value,pageIndex:0}));},300);return()=>clearTimeout(timer);},[term]);
  useEffect(()=>{
    const controller=new AbortController();setLoading(true);setError("");setData(null);
    const sort=sorting[0];const query=new URLSearchParams({page:String(pagination.pageIndex+1),pageSize:String(pagination.pageSize),search,sortBy:sort.id,sortDirection:sort.desc?"desc":"asc"});if(kind==="unit"&&unitId)query.set("unitId",unitId);
    void api<unknown>(`${url}?${query}`,{signal:controller.signal}).then(value=>parseManagementPage<Row>(value,row=>kind==="unit"?parseUnit(row,propertyId,buildingId??null):parseBuilding(row,propertyId))).then(result=>{
      if(controller.signal.aborted)return;const last=Math.max(0,Math.ceil(result.totalCount/pagination.pageSize)-1);
      if(pagination.pageIndex>last){setPagination(value=>({...value,pageIndex:last}));return;}setData(result);
    }).catch(reason=>{if(!controller.signal.aborted)setError(errorMessage(reason,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[api,url,pagination,sorting,search,revision,locale,unitId,kind,propertyId,buildingId]);
  function changed(message:string){setNotice(message);setRevision(value=>value+1);onChanged();}
  const changeSorting:OnChangeFn<SortingState>=updater=>{setSorting(updater);setPagination(value=>({...value,pageIndex:0}));};
  function clearUnitFilter(){router.replace(`/properties/${propertyId}${buildingId?`/buildings/${buildingId}`:""}/units`);}
  function reset(){if(unitId)clearUnitFilter();setTerm("");setSearch("");setSorting([{id:kind==="building"?"name":"number",desc:false}]);setPagination(value=>({...value,pageIndex:0}));}
  const people=useUnitPeople(propertyId,kind==="unit"&&!loading&&!error?(data?.items as Unit[]??[]):[],kind==="unit"&&canWrite&&!loading&&!error,revision);
  const parties=useMemo(()=>new Map(people.data?.items.map(item=>[item.unitId,item])??[]),[people.data]);
  const personLinks=(record:Row,role:"owners"|"tenants")=><UnitPersonLinks propertyId={propertyId} links={parties.get(record.id)?.[role]} loading={people.loading} error={!!people.error}/>;
  const actions=(record:Row)=><div className="flex justify-end gap-1">
    {"name" in record?<Button asChild variant="view" size="icon-sm" aria-label={t("viewBuildingUnits",{name:record.name})}><Link href={`/properties/${propertyId}/buildings/${record.id}/units`}><ArrowUpRight className="size-4"/></Link></Button>:<Button variant="view" size="icon-sm" aria-label={m("viewRecord",{name:record.number})} onClick={()=>openDetails(record)}><Eye className="size-4"/></Button>}
    {canWrite&&<><Button variant="edit" size="icon-sm" disabled={!parentEnabled} aria-label={t("editRecord",{name:"name" in record?record.name:record.number})} onClick={()=>setEditor({target:record})}><Pencil className="size-4"/></Button><Button variant="danger" size="icon-sm" disabled={!parentEnabled} className="text-muted-foreground hover:text-destructive" aria-label={t("deleteRecord",{name:"name" in record?record.name:record.number})} onClick={()=>setDeleting(record)}><Trash2 className="size-4"/></Button></>}
  </div>;
  const columns=useMemo<ColumnDef<Row>[]>(()=>{
    const sortHeader=(title:string)=>({column}:{column:{toggleSorting:(desc:boolean)=>void;getIsSorted:()=>false|"asc"|"desc"}})=><Button variant="ghost" size="sm" className="-ml-3 text-xs" aria-label={t("sortColumn",{column:title})} onClick={()=>column.toggleSorting(column.getIsSorted()==="asc")}>{title}<ArrowUpDown className="size-3"/></Button>;
    const result:ColumnDef<Row>[]=[{id:kind==="building"?"name":"number",accessorFn:row=>"name" in row?row.name:row.number,header:sortHeader(kind==="building"?t("buildingBlockName"):buildingId?t("unitNumber"):t("apartmentNumber")),cell:({row})=><div className="flex items-center gap-3"><IdentityMark icon={Icon}/><div className="min-w-0">{"name" in row.original?<Link className="block max-w-56 truncate font-semibold hover:text-primary" href={`/properties/${propertyId}/buildings/${row.original.id}`} title={row.original.name}>{row.original.name}</Link>:<Button variant="link" className="h-auto max-w-44 justify-start truncate p-0 font-semibold text-foreground" onClick={()=>openDetails(row.original as Unit)}>{row.original.number}</Button>}<span className="mt-1 block text-xs text-muted-foreground">{"number" in row.original?t(unitTypeNames[row.original.type]):t("buildingBlock")}</span></div></div>}];
    if(kind==="building")result.push({id:"unitCount",header:t("structureSummary"),enableSorting:false,cell:({row})=><span className="text-sm text-muted-foreground">{t("unitSummary",{count:"unitCount" in row.original?row.original.unitCount:0})}{"floorCount" in row.original&&row.original.floorCount?t("floorSummary",{count:row.original.floorCount}):""}</span>});
    else result.push({id:"floor",accessorFn:row=>"floor" in row?row.floor:0,header:sortHeader(t("floor")),cell:({row})=>"floor" in row.original?formatNumber(row.original.floor,locale):"—"},{id:"grossSquareMeters",accessorFn:row=>"grossSquareMeters" in row?row.grossSquareMeters:0,header:sortHeader(t("grossArea")),cell:({row})=><span className="whitespace-nowrap tabular-nums">{"grossSquareMeters" in row.original?formatNumber(row.original.grossSquareMeters,locale):"—"} m²</span>});
    if(kind==="unit"&&canWrite)result.push(...(["tenants","owners"] as const).map(role=>({id:role,header:t(role==="tenants"?"currentTenants":"currentOwners"),enableSorting:false,cell:({row}:{row:{original:Row}})=>personLinks(row.original,role)})));
    result.push(...(kind==="unit"?unitDetailFields:buildingDetailFields).map(f=>({id:f.key,header:t(f.label),enableSorting:false,cell:({row}:{row:{original:Row}})=><DetailValue field={f} value={(row.original as unknown as Record<string,unknown>)[f.key]}/>})));
    result.push({id:"isEnabled",header:t("status"),enableSorting:false,cell:({row})=><StatusBadge enabled={row.original.isEnabled}/>},{id:"actions",header:()=> <span className="sr-only">{t("actions")}</span>,enableSorting:false,cell:({row})=>actions(row.original)});
    return result;
  },[kind,propertyId,buildingId,canWrite,parentEnabled,Icon,t,m,locale,parties,people.loading,people.error]);
  const sortValue=`${sorting[0].id}:${sorting[0].desc?"desc":"asc"}`;
  return <section className="mt-6 overflow-hidden rounded-2xl border bg-card shadow-sm" aria-label={t("manageEntity",{entity:label})}>
    <div className="flex flex-wrap items-center justify-between gap-3 border-b p-5"><div className="flex items-center gap-3"><Icon aria-hidden="true" className="size-5 text-primary"/><h2 className="m-0 text-base font-semibold">{kind==="building"?t("buildingsBlocks"):buildingId?t("units2"):t("apartments")}</h2><Badge variant="secondary">{loading||error?"…":formatNumber(data?.totalCount??0,locale)}</Badge></div>{canWrite&&<Button disabled={!parentEnabled} onClick={()=>setEditor({target:null})}><Plus className="size-4"/>{t("newRecord",{entity:label})}</Button>}</div>
    {!parentEnabled&&<MessageBanner kind="warning" className="m-4">{t("theParentIsInactiveActivateThePropertyAndBuilding")}</MessageBanner>}
    {unitId&&<div className="flex flex-wrap items-center justify-between gap-3 border-b bg-secondary/40 p-4"><span className="text-sm font-medium">{t("selectedUnitFilter")}</span><Button variant="outline" onClick={clearUnitFilter}>{t("clearUnitFilter")}</Button></div>}
    {kind==="unit"&&canWrite&&<p className="px-5 pt-4 text-xs text-muted-foreground">{t("currentUnitPeopleDescription")}</p>}
    {people.error&&<MessageBanner className="m-4" action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{t("tryAgain")}</Button>}>{people.error}</MessageBanner>}
    {notice&&<MessageBanner kind="success" className="m-4" announce={false}>{notice}</MessageBanner>}
    <ListToolbar term={term} onTerm={setTerm} searchLabel={t("searchEntityLabel",{entity:label})} placeholder={t("searchEntity",{entity:label})} loading={loading} onRefresh={()=>{setRevision(n=>n+1);onChanged();}} onReset={reset} resetEnabled={!!unitId||!!term||sortValue!==`${kind==="building"?"name":"number"}:asc`}>
      <DropdownMenu><DropdownMenuTrigger asChild><Button variant="outline">{t("columns")}</Button></DropdownMenuTrigger><DropdownMenuContent align="end"><DropdownMenuLabel>{t("detailColumns")}</DropdownMenuLabel>{kind==="unit"&&canWrite&&(["tenants","owners"] as const).map(role=><DropdownMenuCheckboxItem key={role} checked={visibility[role]!==false} onCheckedChange={checked=>setVisibility(v=>({...v,[role]:!!checked}))}>{t(role==="tenants"?"currentTenants":"currentOwners")}</DropdownMenuCheckboxItem>)}{extraFields.map(f=><DropdownMenuCheckboxItem key={f.key} checked={visibility[f.key]!==false} onCheckedChange={checked=>setVisibility(v=>({...v,[f.key]:!!checked}))}>{t(f.label)}</DropdownMenuCheckboxItem>)}</DropdownMenuContent></DropdownMenu>
      <select aria-label={m("sorting")} value={sortValue} className={toolbarSelectClass} onChange={event=>{const [id,direction]=event.target.value.split(":");changeSorting([{id,desc:direction==="desc"}]);}}>
        <option value={`${kind==="building"?"name":"number"}:asc`}>{m("numberNameAsc")}</option><option value={`${kind==="building"?"name":"number"}:desc`}>{m("numberNameDesc")}</option>
        {kind==="unit"&&<><option value="floor:asc">{m("floorAsc")}</option><option value="floor:desc">{m("floorDesc")}</option><option value="grossSquareMeters:asc">{m("areaAsc")}</option><option value="grossSquareMeters:desc">{m("areaDesc")}</option></>}
        <option value="createdAt:desc">{t("newestFirst")}</option>
      </select>
    </ListToolbar>
    <RecordList compact cardBreakpoint={kind==="unit"?"xl":"md"} data={data} columns={columns} loading={loading} error={error} page={pagination.pageIndex+1} onPage={page=>setPagination(value=>({...value,pageIndex:page-1}))} onRetry={()=>setRevision(n=>n+1)} sorting={sorting} onSortingChange={changeSorting} columnVisibility={visibility} onColumnVisibilityChange={setVisibility}
      footerControls={<select aria-label={t("pageSizeEntity",{entity:label})} value={pagination.pageSize} disabled={loading} className="h-8 rounded-md border bg-card px-2 text-xs" onChange={event=>setPagination({pageIndex:0,pageSize:Number(event.target.value)})}>{[10,20,50].map(size=><option key={size} value={size}>{size} {t("page")}</option>)}</select>}
      empty={<><IdentityMark icon={Icon} className="mx-auto mb-4 size-14"/><strong>{search?t("noRecordsMatchYourSearch"):t("emptyEntity",{entity:label})}</strong><p className="mt-2">{search?t("tryADifferentSearch"):t("createdRecords",{entity:kind==="building"?t("buildingsBlocks"):t("units3")})}</p></>}
      card={record=><><div className="mb-4 flex items-center gap-3"><IdentityMark icon={Icon}/><strong className="min-w-0 flex-1 wrap-anywhere">{"name" in record?record.name:record.number}</strong><StatusBadge enabled={record.isEnabled}/></div>{"number" in record?<dl className="mb-3 grid grid-cols-2 gap-3 text-sm"><div><dt className="text-xs text-muted-foreground">{t("floor")}</dt><dd className="mt-1 font-medium">{formatNumber(record.floor,locale)}</dd></div><div><dt className="text-xs text-muted-foreground">{t("grossArea")}</dt><dd className="mt-1 font-medium">{formatNumber(record.grossSquareMeters,locale)} m²</dd></div><div className="col-span-2 text-xs text-muted-foreground">{t(unitTypeNames[record.type])}</div>{canWrite&&(["tenants","owners"] as const).map(role=><div key={role} className="col-span-2 border-t pt-2"><dt className="text-xs text-muted-foreground">{t(role==="tenants"?"currentTenants":"currentOwners")}</dt><dd>{personLinks(record,role)}</dd></div>)}<DetailValues fields={unitDetailFields} record={record}/></dl>:<p className="mb-3 text-sm text-muted-foreground">{t("unitSummary",{count:record.unitCount})}{record.floorCount?t("floorSummary",{count:record.floorCount}):""}</p>}{"name" in record&&<dl className="grid grid-cols-2 gap-x-3"><DetailValues fields={buildingDetailFields} record={record}/></dl>}<div className="border-t pt-3">{actions(record)}</div></>}/>
    {details&&<RecordDetails kind="unit" target={details} url={url} propertyId={propertyId} canEdit={canWrite&&parentEnabled} onClose={()=>setDetails(null)} onEdit={record=>{setDetails(null);setEditor({target:record,focusReturnTarget:detailTrigger.current});}}/>}
    {editor&&<EntityEditor kind={kind} url={url} target={editor.target} focusReturnTarget={editor.focusReturnTarget} apartmentOnly={kind==="unit"&&!buildingId} onClose={()=>setEditor(null)} onSaved={()=>changed(t("entitySaved",{entity:label}))}/>}
    {deleting&&<DeleteEntity kind={kind} url={url} target={deleting} onClose={()=>setDeleting(null)} onDeleted={()=>changed(t("entityDeleted",{entity:label}))}/>}
  </section>;
}
