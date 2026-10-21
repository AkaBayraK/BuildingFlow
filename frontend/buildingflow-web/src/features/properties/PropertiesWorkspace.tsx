"use client";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {formatDate} from "@/lib/format";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {useLocale,useTranslations} from "next-intl";
import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { flexRender, getCoreRowModel, useReactTable, type ColumnDef, type PaginationState, type SortingState, type VisibilityState } from "@tanstack/react-table";
import { ArrowDown, ArrowUp, ArrowUpDown, ArrowUpRight, Building2, Check, ChevronLeft, ChevronRight, ListFilter, LoaderCircle, MapPin, Plus, RefreshCw, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Dialog, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { useIdentity } from "@/auth/AuthGate";
import { ApiError, errorMessage, type Property, type PropertyPage, type PropertyType, propertyTypeNames, useAuthorizedApi } from "@/services/api";

import {detailFields,detailValues,detailPayload,validateDetailValues,buildingDetailFields,neighborhoodField} from "@/features/property-details/model";
import {DetailInputs,DetailValue} from "@/features/property-details/DetailFields";
import {parseProperty,parsePropertyPage} from "@/services/management-contracts";
const fieldDefinitions=[{key:"name",label:"propertyName",max:200,placeholder:"eGMapleCommunity"},{key:"city",label:"city",max:100,placeholder:"eGIstanbul"},{key:"district",label:"district",max:100,placeholder:"eGKadikoy"},{key:"address",label:"address",max:500,placeholder:"neighborhoodStreetAndBuildingNumber"}] as const;
const empty={name:"",city:"",district:"",address:""};
const columnNames:Record<string,string>={address:"address",createdAt:"registrationDate",neighborhood:"neighborhood",...Object.fromEntries(buildingDetailFields.map(f=>[f.key,f.label]))};

export function PropertiesWorkspace({destination}:{destination?:"units"|"people"|"dues"|"payments"|"expenses"|"announcements"|"surveys"|"documents"|"meters"|"maintenance"|"reports"|"sms"|"requests"|"website"}={}) {
  const t=useTranslations("ui");const codes=useTranslations("apiErrors"); const locale=useLocale();const notification=useNotification();

  const fields=fieldDefinitions.map(field=>({...field,label:t(field.label),placeholder:t(field.placeholder)}));
  const identity=useIdentity();
  const api=useAuthorizedApi();
  const canCreate=!destination && identity.hasRealmRole("SUPER_ADMIN");
  const [open,setOpen]=useState(false);
  const [values,setValues]=useState(empty);
  const [propertyType,setPropertyType]=useState<PropertyType>("APARTMENT");
  const [floorCount,setFloorCount]=useState("");const [features,setFeatures]=useState(()=>detailValues(null,"property"));
  const [errors,setErrors]=useState<Record<string,string[]>>({});
  const [saving,setSaving]=useState(false);
  const lock=useRef(false);
  const [created,setCreated]=useState<Property|null>(null);
  const [formError,setFormError]=useState("");
  const [data,setData]=useState<PropertyPage|null>(null);
  const [listError,setListError]=useState("");
  const [loading,setLoading]=useState(true);
  const [pagination,setPagination]=useState<PaginationState>({pageIndex:0,pageSize:10});
  const [sorting,setSorting]=useState<SortingState>([{id:"name",desc:false}]);
  const [visibility,setVisibility]=useState<VisibilityState>({address:false,constructionYear:false,heatingType:false,hasElevator:false,parkingType:false});
  const [term,setTerm]=useState("");
  const [search,setSearch]=useState("");
  const [revision,setRevision]=useState(0);
  useEffect(()=>{
    const timer=window.setTimeout(()=>{setSearch(term.trim());setPagination(value=>({...value,pageIndex:0}));},300);
    return ()=>window.clearTimeout(timer);
  },[term]);
  useEffect(()=>{
    const controller=new AbortController();setLoading(true);setListError("");
    const sort=sorting[0] ?? {id:"name",desc:false};
    const query=new URLSearchParams({page:String(pagination.pageIndex+1),pageSize:String(pagination.pageSize),search,sortBy:sort.id,sortDirection:sort.desc ? "desc" : "asc"});
    void api<unknown>(`/api/v1/properties?${query}`,{signal:controller.signal}).then(parsePropertyPage)
      .then(result=>{if(!controller.signal.aborted)setData(result);})
      .catch(error=>{if(!controller.signal.aborted)setListError(errorMessage(error,locale));})
      .finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return ()=>controller.abort();
  },[api,pagination,sorting,search,revision]);
  function updateField(key:keyof typeof empty,value:string) {
    setValues(previous=>({...previous,[key]:value}));
    setErrors(previous=>{const next={...previous};delete next[key];return next;});
    setFormError("");
  }
  async function create(event:React.FormEvent<HTMLFormElement>) {
    event.preventDefault();if(lock.current)return;
    const validation:Record<string,string[]>={};
    for(const field of fields) {
      const value=values[field.key].trim();
      if(!value)validation[field.key]=[t("requiredField",{field:field.label})];
      else if(value.length>field.max)validation[field.key]=[t("fieldMaxCharacters",{field:field.label,max:field.max})];
    }
    if(propertyType==="APARTMENT" && floorCount && (!Number.isInteger(Number(floorCount)) || Number(floorCount)<=0 || Number(floorCount)>2147483647))validation.floorCount=[t("theNumberOfFloorsMustBeAPositiveInteger")];
    for(const [key,code] of Object.entries(validateDetailValues(features,"property",propertyType)))validation[key]=[code.startsWith("BF_")?codes(code):t(code)];
    setErrors(validation);setFormError("");
    if(Object.keys(validation).length){document.getElementById(`site-${Object.keys(validation)[0]}`)?.focus();return;}
    lock.current=true;setSaving(true);
    try {
      const result=parseProperty(await api<unknown>("/api/v1/properties",{method:"POST",body:JSON.stringify({...values,...detailPayload(features,"property",propertyType),type:propertyType,floorCount:propertyType==="APARTMENT" && floorCount?Number(floorCount):null})}));
      notification.success(t("propertyCreated",{name:result.name}));setCreated(result);setValues(empty);setFloorCount("");setFeatures(detailValues(null,"property"));setOpen(false);setTerm("");setSearch("");
      setPagination(value=>({...value,pageIndex:0}));setRevision(value=>value+1);
    } catch(error) {
      if(error instanceof ApiError)setErrors(error.errors);
      setFormError(errorMessage(error,locale));
    } finally {lock.current=false;setSaving(false);}
  }
  const columns=useMemo<ColumnDef<Property>[]>(()=>[
    {accessorKey:"name",enableHiding:false,header:({column})=><SortHeading title={t("propertyName")} direction={column.getIsSorted()} onClick={()=>column.toggleSorting(column.getIsSorted()==="asc")}/>,cell:({row})=><Link className="group flex items-center gap-3" href={`/properties/${row.original.id}${destination?`/${destination}`:""}`}><span className="grid size-10 shrink-0 place-items-center rounded-xl border border-blue-100 bg-blue-50 text-blue-600"><Building2 className="size-5"/></span><span className="max-w-60 truncate font-semibold text-slate-800 group-hover:text-primary" title={row.original.name}>{row.original.name}</span></Link>},
    {id:"type",enableHiding:false,enableSorting:false,header:t("typeStructure"),cell:({row})=><div className="min-w-40"><Badge variant="secondary" className="bg-blue-50 text-primary">{t(propertyTypeNames[row.original.type])}</Badge><span className="mt-2 block text-xs text-muted-foreground">{row.original.type==="APARTMENT"?t("apartmentSummary",{floors:row.original.floorCount ?? "—",units:row.original.unitCount}):t("buildingSummary",{buildings:row.original.buildingCount,units:row.original.unitCount})}</span></div>},
    {accessorKey:"city",enableHiding:false,header:({column})=><SortHeading title={t("location")} direction={column.getIsSorted()} onClick={()=>column.toggleSorting(column.getIsSorted()==="asc")}/>,cell:({row})=><div className="min-w-32"><span className="font-medium text-slate-700">{row.original.city}</span><span className="mt-1 block text-xs text-muted-foreground">{row.original.district}</span></div>},
    {accessorKey:"address",enableSorting:false,header:t("address"),cell:({row})=><span className="block max-w-52 truncate text-muted-foreground" title={row.original.address}>{row.original.address}</span>},
    ...[neighborhoodField,...buildingDetailFields].map(f=>({id:f.key,header:t(f.label),enableSorting:false,cell:({row}:{row:{original:Property}})=><DetailValue field={f} value={(row.original as unknown as Record<string,unknown>)[f.key]}/>})),
    {accessorKey:"createdAt",header:({column})=><SortHeading title={t("registrationDate")} direction={column.getIsSorted()} onClick={()=>column.toggleSorting(column.getIsSorted()==="asc")}/>,cell:({row})=><span className="text-xs text-muted-foreground">{formatDate(row.original.createdAt,locale)}</span>},
    {accessorKey:"isEnabled",enableSorting:false,enableHiding:false,header:t("status"),cell:({row})=><Badge variant="secondary" className={row.original.isEnabled ? "gap-1.5 border border-emerald-100 bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-600"}><span className={`size-1.5 rounded-full ${row.original.isEnabled ? "bg-emerald-500" : "bg-slate-400"}`}/>{row.original.isEnabled ? t("active") : t("inactive")}</Badge>},
    {id:"actions",enableSorting:false,enableHiding:false,header:()=><span className="sr-only">{t("details")}</span>,cell:({row})=><Button variant="view" size="icon" asChild><Link href={`/properties/${row.original.id}${destination?`/${destination}`:""}`} aria-label={destination?t("openPropertyModule",{name:row.original.name,module:t(destination==="website"?"navWebsite":destination==="surveys"?"navSurveys":destination==="requests"?"shellRequests":destination==="sms"?"navSms":destination==="meters"?"navMeters":destination==="maintenance"?"navMaintenance":destination==="reports"?"navReports":destination==="units"?"navUnits":destination==="people"?"people":destination==="announcements"?"navAnnouncements":destination==="documents"?"navDocuments":destination==="expenses"?"navExpenses":destination==="payments"?"navPayments":"navDues")}):t("viewRecord",{name:row.original.name})}><ArrowUpRight className="size-4"/></Link></Button>}
  ],[t,locale,destination]);
  const table=useReactTable({data:data?.items ?? [],columns,getCoreRowModel:getCoreRowModel(),manualPagination:true,manualSorting:true,enableSortingRemoval:false,enableMultiSort:false,rowCount:data?.totalCount ?? 0,
    state:{pagination,sorting,columnVisibility:visibility},onPaginationChange:setPagination,onSortingChange:updater=>{setSorting(updater);setPagination(value=>({...value,pageIndex:0}));},onColumnVisibilityChange:setVisibility});
  return <div className="py-10">
    <div className="mb-8 flex flex-wrap items-center justify-between gap-6">
      <div><span className="text-[11px] font-bold tracking-[.18em] text-primary">{t("yourPROPERTIES")}</span><h1 className="mt-3 mb-2 text-3xl font-bold tracking-tight lg:text-4xl">{destination?t(destination==="website"?"navWebsite":destination==="surveys"?"navSurveys":destination==="requests"?"shellRequests":destination==="sms"?"navSms":destination==="meters"?"navMeters":destination==="maintenance"?"navMaintenance":destination==="reports"?"navReports":destination==="units"?"navUnits":destination==="people"?"people":destination==="announcements"?"navAnnouncements":destination==="documents"?"navDocuments":destination==="expenses"?"navExpenses":destination==="payments"?"navPayments":"navDues"):t("properties2")}</h1><p className="text-sm leading-6 text-muted-foreground">{destination?t("choosePropertyForModule",{module:t(destination==="website"?"navWebsite":destination==="surveys"?"navSurveys":destination==="requests"?"shellRequests":destination==="sms"?"navSms":destination==="meters"?"navMeters":destination==="maintenance"?"navMaintenance":destination==="reports"?"navReports":destination==="units"?"navUnits":destination==="people"?"people":destination==="announcements"?"navAnnouncements":destination==="documents"?"navDocuments":destination==="expenses"?"navExpenses":destination==="payments"?"navPayments":"navDues")}):t("manageAllYourPropertiesInOnePlace")}</p></div>
      {canCreate && <Button size="lg" className="h-11 rounded-xl shadow-sm shadow-blue-200" onClick={()=>{setFormError("");setErrors({});setOpen(true);}}><Plus className="size-4"/>{t("newProperty")}</Button>}
    </div>
    {created && <MessageBanner className="my-4 text-left" kind="success" announce={false}><span className="flex items-center gap-2"><Check className="size-4"/><strong>{t("propertyCreated",{name:created.name})}</strong></span><Link className="font-semibold hover:underline" href={`/properties/${created.id}`}>{t("viewDetails")}</Link></MessageBanner>}
    <div className="mb-6 flex items-start gap-4 rounded-2xl border border-blue-100 bg-gradient-to-r from-blue-50 to-white p-5"><span className="grid size-11 shrink-0 place-items-center rounded-xl bg-white text-primary shadow-sm"><MapPin className="size-5"/></span><div><h2 className="m-0 text-sm font-semibold text-slate-800">{destination?t("selectProperty"):t("organizedManagementStartsWithAccurateInformation")}</h2><p className="mt-1 text-xs leading-6 text-muted-foreground">{destination?t("propertyModuleSelectionHelp"):canCreate ? t("recordPropertyNamesAndLocationsThenReviewTheirDetails") : t("viewThePropertiesYouCanAccessAndReviewTheir")}</p></div></div>
    <section className="overflow-hidden rounded-2xl border bg-white shadow-sm shadow-slate-200/30" aria-labelledby="sites-list-title">
      <div className="flex flex-wrap items-center justify-between gap-4 border-b px-6 py-5"><div className="flex items-center gap-3"><h2 id="sites-list-title" className="m-0 text-base font-semibold">{t("propertiesList")}</h2><Badge variant="secondary" className="border bg-slate-50 text-slate-600">{data ? t("recordCount",{count:data.totalCount}) : "…"}</Badge></div><span className="text-xs text-muted-foreground">{canCreate ? t("allProperties") : t("accessibleProperties")}</span></div>
      <div className="flex flex-wrap items-center justify-between gap-3 px-6 py-4"><div className="relative w-full sm:max-w-xs"><Search className="absolute top-3 left-3 size-4 text-slate-400"/><Input className="h-10 rounded-lg border-slate-200 bg-white pl-9 text-sm" value={term} maxLength={200} onChange={event=>setTerm(event.target.value)} placeholder={t("searchPropertyNameOrLocation")} aria-label={t("searchProperties")}/></div><div className="flex gap-2"><Button variant="view" className="h-10 bg-white" disabled={loading} aria-label={t("refreshPropertiesList")} onClick={()=>setRevision(value=>value+1)}><RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`}/><span className="hidden sm:inline">{t("refresh")}</span></Button><DropdownMenu><DropdownMenuTrigger asChild><Button variant="view" className="h-10 bg-white"><ListFilter className="size-4"/>{t("view")}</Button></DropdownMenuTrigger><DropdownMenuContent align="end"><DropdownMenuLabel>{t("showColumns")}</DropdownMenuLabel><DropdownMenuSeparator/>{table.getAllColumns().filter(column=>column.getCanHide()).map(column=><DropdownMenuCheckboxItem key={column.id} checked={column.getIsVisible()} onCheckedChange={value=>column.toggleVisibility(!!value)}>{columnNames[column.id] ? t(columnNames[column.id]) : column.id}</DropdownMenuCheckboxItem>)}</DropdownMenuContent></DropdownMenu></div></div>
      <Table className="min-w-[900px]"><TableHeader className="bg-slate-50/70">{table.getHeaderGroups().map(group=><TableRow key={group.id}>{group.headers.map(header=><TableHead className="px-6 text-xs text-slate-500" key={header.id} aria-sort={header.column.getIsSorted()==="asc" ? "ascending" : header.column.getIsSorted()==="desc" ? "descending" : undefined}>{header.isPlaceholder ? null : flexRender(header.column.columnDef.header,header.getContext())}</TableHead>)}</TableRow>)}</TableHeader>
      <TableBody>{loading ? Array.from({length:3},(_,index)=><TableRow key={index}>{table.getVisibleLeafColumns().map(column=><TableCell className="h-20 px-6" key={column.id}><Skeleton className="h-4 w-24"/></TableCell>)}</TableRow>) : listError ? <TableRow><TableCell colSpan={table.getVisibleLeafColumns().length} className="h-56 text-center"><MessageBanner className="my-4 text-left">{listError}</MessageBanner><Button variant="view" onClick={()=>setRevision(value=>value+1)}>{t("tryAgain")}</Button></TableCell></TableRow> : table.getRowModel().rows.length ? table.getRowModel().rows.map(row=><TableRow className="h-20 hover:bg-blue-50/30" key={row.id}>{row.getVisibleCells().map(cell=><TableCell className="px-6 py-4" key={cell.id}>{flexRender(cell.column.columnDef.cell,cell.getContext())}</TableCell>)}</TableRow>) : <TableRow><TableCell colSpan={table.getVisibleLeafColumns().length} className="h-64 text-center"><Building2 className="mx-auto mb-4 size-9 text-slate-300"/><h3 className="text-sm font-semibold text-slate-700">{search ? t("noPropertiesMatchYourSearch") : t("noPropertiesYet")}</h3><p className="mt-2 text-xs text-muted-foreground">{search ? t("tryAnotherPropertyNameOrLocation") : canCreate ? t("yourFirstPropertyWillAppearHereAfterYouCreate") : t("propertiesYouCanAccessWillAppearHere")}</p>{canCreate && !search && <Button variant="link" className="mt-3" onClick={()=>setOpen(true)}>{t("createYourFirstProperty")}</Button>}</TableCell></TableRow>}</TableBody></Table>
      <div className="flex flex-wrap items-center justify-between gap-4 border-t px-6 py-4"><span className="text-xs text-muted-foreground">{data?.totalCount ? t("recordRange",{from:pagination.pageIndex*pagination.pageSize+1,to:Math.min((pagination.pageIndex+1)*pagination.pageSize,data.totalCount),count:data.totalCount}) : t("0Records")}{loading && <span className="ml-2" role="status">{t("loading")}</span>}</span><div className="flex items-center gap-3"><Label htmlFor="site-page-size" className="hidden text-xs text-muted-foreground sm:inline">{t("perPage")}</Label><select id="site-page-size" aria-label={t("recordsPerPage")} value={pagination.pageSize} disabled={loading} onChange={event=>setPagination({pageIndex:0,pageSize:Number(event.target.value)})} className="h-8 rounded-md border bg-white px-2 text-xs outline-none focus:ring-2 focus:ring-primary">{[10,20,50].map(size=><option key={size} value={size}>{size}</option>)}</select><span className="text-xs text-slate-600">{pagination.pageIndex+1} / {Math.max(1,table.getPageCount())}</span><Button variant="outline" size="icon-sm" className="bg-white" aria-label={t("previousPage")} disabled={loading || !table.getCanPreviousPage()} onClick={()=>table.previousPage()}><ChevronLeft/></Button><Button variant="outline" size="icon-sm" className="bg-white" aria-label={t("nextPage")} disabled={loading || !table.getCanNextPage()} onClick={()=>table.nextPage()}><ChevronRight/></Button></div></div>
    </section>
    <Dialog open={open} onOpenChange={value=>{if(!saving)setOpen(value);}}><ManagementPanel showCloseButton={!saving}><DialogHeader className="text-left"><div className="mb-2 grid size-12 place-items-center rounded-xl border border-primary/10 bg-secondary text-primary"><Building2 className="size-6"/></div><DialogTitle className="text-2xl font-bold tracking-tight">{t("newProperty")}</DialogTitle><DialogDescription className="leading-6">{t("recordYourPropertySNameAndLocation")}</DialogDescription></DialogHeader>
      {formError && <MessageBanner className="my-4 text-left">{formError}</MessageBanner>}
      <form onSubmit={create} noValidate><fieldset disabled={saving} className="space-y-5 border-0 p-0">
        <div><Label className="mb-3">{t("whatWouldYouLikeToManage")}</Label><div className="grid grid-cols-2 gap-3">{(Object.keys(propertyTypeNames) as PropertyType[]).map(type=><Button type="button" key={type} variant="field" aria-pressed={propertyType===type} className={`h-auto flex-col items-start whitespace-normal rounded-xl px-4 py-3 text-left ${propertyType===type?"border-primary bg-blue-50 text-primary":"bg-white"}`} onClick={()=>{setPropertyType(type);setErrors(previous=>({...previous,type:[]}));}}><Building2 className="size-5"/><strong>{t(propertyTypeNames[type])}</strong><span className="text-[11px] font-normal text-muted-foreground">{type==="APARTMENT"?t("oneBuildingDirectUnits"):type==="SITE"?t("buildingBlockUnits"):type==="RESIDENCE"?t("towerBlockUnits"):t("apartmentsOfficesCommercialUnits")}</span></Button>)}</div></div>
        <div className="grid gap-5 sm:grid-cols-2">{fields.map(field=><div className={`space-y-2 ${field.key==="name" || field.key==="address" ? "sm:col-span-2" : ""}`} key={field.key}><Label htmlFor={`site-${field.key}`} className="text-sm font-semibold text-slate-700">{field.label}<span className="text-muted-foreground">*</span></Label>{field.key==="address" ? <Textarea id={`site-${field.key}`} value={values[field.key]} onChange={event=>updateField(field.key,event.target.value)} placeholder={field.placeholder} maxLength={field.max} required aria-invalid={!!errors[field.key]} aria-describedby={errors[field.key] ? `error-${field.key}` : undefined} className="min-h-24 resize-y rounded-lg bg-white"/> : <Input id={`site-${field.key}`} value={values[field.key]} onChange={event=>updateField(field.key,event.target.value)} placeholder={field.placeholder} maxLength={field.max} required aria-invalid={!!errors[field.key]} aria-describedby={errors[field.key] ? `error-${field.key}` : undefined} className="h-11 rounded-lg bg-white"/>}{errors[field.key] && <FieldError id={`error-${field.key}`}>{errors[field.key].join(" ")}</FieldError>}</div>)}</div>
        {propertyType==="APARTMENT" && <div className="space-y-2"><Label htmlFor="site-floorCount">{t("numberOfFloorsOptional")}</Label><Input id="site-floorCount" type="number" step="1" min="1" value={floorCount} onChange={event=>{setFloorCount(event.target.value);setErrors(previous=>({...previous,floorCount:[]}));}} aria-invalid={!!errors.floorCount?.length} aria-describedby={errors.floorCount?.length?"site-floorCount-error":undefined}/>{errors.floorCount?.length?<FieldError id="site-floorCount-error">{errors.floorCount.join(" ")}</FieldError>:null}<p className="text-xs leading-6 text-muted-foreground">{t("addApartmentsIndividuallyAfterSavingTheApartmentCountComes")}</p></div>}
        <section className="space-y-4 border-t pt-5"><h3 className="text-sm font-semibold">{t("buildingDetailFeatures")}</h3><DetailInputs prefix="site" fields={detailFields("property",propertyType)} values={features} errors={errors} onChange={(key,value)=>{setFeatures(previous=>({...previous,[key]:value}));setErrors(previous=>({...previous,[key]:[]}));}}/>{propertyType!=="APARTMENT"&&<p className="text-xs leading-5 text-muted-foreground">{t("buildingFeaturesOnBlocks")}</p>}</section>
        <p className="text-xs text-muted-foreground">{t("fieldsMarkedWithAnAsteriskAreRequired")}</p><DialogFooter className="border-t pt-5"><Button type="button" variant="outline" disabled={saving} onClick={()=>setOpen(false)}>{t("cancel")}</Button><Button type="submit" disabled={saving} className="min-w-32">{saving ? <><LoaderCircle className="size-4 animate-spin"/>{t("saving")}</> : <><Plus className="size-4"/>{t("create")}</>}</Button></DialogFooter>
      </fieldset></form>
    </ManagementPanel></Dialog>
  </div>;
}
function SortHeading({title,direction,onClick}:{title:string;direction:false|"asc"|"desc";onClick:()=>void}) {
  const t=useTranslations("ui");
  const Icon=direction==="asc" ? ArrowUp : direction==="desc" ? ArrowDown : ArrowUpDown;
  return <Button variant="ghost" size="sm" className="-ml-3 h-9 text-xs text-slate-500 hover:text-slate-800" onClick={onClick} aria-label={t("sortColumn",{column:title})}>{title}<Icon className="size-3.5"/></Button>;
}
