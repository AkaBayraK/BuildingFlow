"use client";
import {useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {CalendarX2,DoorOpen,LoaderCircle,Plus,RefreshCw,Save} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {FieldError} from "@/components/feedback/MessageBanner";
import {businessToday,formatDateOnly} from "@/lib/format";
import {UnitPicker} from "./UnitPicker";
import {allocationBody,calendarDay,readAllocation,readPage,readSpace,readVehicle,type Allocation} from "./model";
import {parkingRoot,useParkingAction,useParkingRead} from "./useParking";

type Locale="tr"|"en";
export function AllocationsTab({propertyId,canWrite,revision,onChanged}:{propertyId:string;canWrite:boolean;revision:number;onChanged:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),locale=useLocale() as Locale;
 const [activeOnly,setActiveOnly]=useState(true),[page,setPage]=useState(1),[local,setLocal]=useState(0),[creating,setCreating]=useState(false),[ending,setEnding]=useState<Allocation|null>(null);
 const list=useParkingRead(`${parkingRoot(propertyId)}/allocations?activeOnly=${activeOnly}&page=${page}&pageSize=20`,v=>readPage(v,readAllocation),revision+local);
 const refresh=()=>setLocal(n=>n+1),changed=()=>{refresh();onChanged();};
 const period=(a:Allocation)=>`${formatDateOnly(a.startDate,locale)} – ${a.endDate?formatDateOnly(a.endDate,locale):t("openEnded")}`;
 const columns=useMemo<ColumnDef<Allocation>[]>(()=>[
  {id:"space",header:t("space"),cell:({row})=><strong>{row.original.spaceCode}</strong>},
  {id:"unit",header:t("unit"),cell:({row})=>row.original.unitNumber},
  {id:"plate",header:t("plate"),cell:({row})=><span className="font-mono">{row.original.plate??"—"}</span>},
  {id:"period",header:t("period"),cell:({row})=>period(row.original)},
  {id:"actions",header:ui("actions"),cell:({row})=>canWrite&&!row.original.endDate?<Button variant="warn" size="sm" onClick={()=>setEnding(row.original)}><CalendarX2/>{t("endAllocation")}</Button>:null},
 ],[t,ui,locale,canWrite]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-label={t("tab_allocations")}>
  <div className="flex flex-wrap items-center justify-between gap-3 border-b p-4 sm:px-5"><label className="flex items-center gap-2 text-sm"><input type="checkbox" className="size-4" checked={activeOnly} onChange={e=>{setActiveOnly(e.target.checked);setPage(1);}}/>{t("activeOnly")}</label><div className="flex flex-wrap gap-2"><Button variant="view" disabled={list.loading} onClick={refresh}><RefreshCw/>{t("refresh")}</Button>{canWrite&&<Button onClick={()=>setCreating(true)}><Plus/>{t("newAllocation")}</Button>}</div></div>
  <RecordList compact data={list.data} loading={list.loading} error={list.error} columns={columns} page={page} onPage={setPage} onRetry={refresh}
   empty={<><DoorOpen className="mx-auto mb-3 size-8"/><strong>{t("allocationsEmpty")}</strong><p className="mt-2">{t("allocationsEmptyHelp")}</p></>}
   card={a=><><div className="flex items-start justify-between gap-3"><strong>{a.spaceCode} · {a.unitNumber}</strong><span className="font-mono text-xs">{a.plate??""}</span></div><p className="my-2 text-xs text-muted-foreground">{period(a)}</p>
    {canWrite&&!a.endDate&&<Button variant="warn" size="sm" onClick={()=>setEnding(a)}><CalendarX2/>{t("endAllocation")}</Button>}</>}/>
  {creating&&<AllocationPanel propertyId={propertyId} onClose={()=>setCreating(false)} onSaved={changed}/>}
  {ending&&<EndAllocationPanel propertyId={propertyId} allocation={ending} onClose={()=>setEnding(null)} onSaved={changed}/>}
 </section>;
}

function AllocationPanel({propertyId,onClose,onSaved}:{propertyId:string;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui");
 const [form,setForm]=useState({spaceId:"",unitId:"",vehicleId:"",startDate:businessToday(),endDate:""}),[invalid,setInvalid]=useState(false);
 const root=parkingRoot(propertyId),action=useParkingAction();
 const spaces=useParkingRead(`${root}/spaces?pageSize=100`,v=>readPage(v,readSpace),0);
 const vehicles=useParkingRead(form.unitId?`${root}/vehicles?status=APPROVED&unitId=${form.unitId}&pageSize=50`:null,v=>readPage(v,readVehicle),0);
 const set=<K extends keyof typeof form>(k:K,v:(typeof form)[K])=>{setForm(f=>({...f,[k]:v}));setInvalid(false);};
 async function save(e:React.FormEvent){e.preventDefault();const body=allocationBody(form);if(!body){setInvalid(true);return;}
  if(await action.run(`${root}/allocations`,"POST",body,readAllocation,t("allocationCreated"))){onSaved();onClose();}}
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><DoorOpen className="size-9 text-primary"/><DialogTitle>{t("newAllocation")}</DialogTitle><DialogDescription>{t("allocationHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} noValidate className="space-y-5"><fieldset disabled={action.busy} className="space-y-5">
   <div><Label htmlFor="allocation-space">{t("space")} *</Label><select id="allocation-space" className={toolbarSelectClass+" w-full"} value={form.spaceId} aria-invalid={invalid&&!form.spaceId} onChange={e=>set("spaceId",e.target.value)}>
    <option value="">{spaces.loading?t("loading"):t("chooseSpace")}</option>{spaces.data?.items.filter(s=>s.type!=="VISITOR").map(s=><option key={s.id} value={s.id}>{s.code} · {t(`space_${s.type}`)}{s.allocatedUnitNumber?` · ${t("allocatedTo",{unit:s.allocatedUnitNumber})}`:""}</option>)}</select></div>
   <UnitPicker propertyId={propertyId} value={form.unitId} required onChange={u=>setForm(f=>({...f,unitId:u?.id??"",vehicleId:""}))}/>
   <div><Label htmlFor="allocation-vehicle">{t("vehicleOptional")}</Label><select id="allocation-vehicle" className={toolbarSelectClass+" w-full"} disabled={!form.unitId||vehicles.loading} value={form.vehicleId} onChange={e=>set("vehicleId",e.target.value)}>
    <option value="">{t("noVehicle")}</option>{vehicles.data?.items.map(v=><option key={v.id} value={v.id}>{v.plate} · {v.personName}</option>)}</select></div>
   <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="allocation-start">{t("startDate")} *</Label><Input id="allocation-start" type="date" min="2000-01-01" max="2100-12-31" value={form.startDate} aria-invalid={invalid&&!calendarDay(form.startDate)} onChange={e=>set("startDate",e.target.value)}/></div>
    <div><Label htmlFor="allocation-end">{t("endDateOptional")}</Label><Input id="allocation-end" type="date" min={form.startDate} max="2100-12-31" value={form.endDate} onChange={e=>set("endDate",e.target.value)}/></div></div>
   {invalid&&<FieldError>{t("allocationInvalid")}</FieldError>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={action.busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={action.busy}>{action.busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("save")}</Button></DialogFooter></form>
 </ManagementPanel></Dialog>;
}

function EndAllocationPanel({propertyId,allocation,onClose,onSaved}:{propertyId:string;allocation:Allocation;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui");
 const today=businessToday(),[endDate,setEndDate]=useState(allocation.startDate>today?allocation.startDate:today),action=useParkingAction();
 const valid=calendarDay(endDate)&&endDate>=allocation.startDate;
 async function save(e:React.FormEvent){e.preventDefault();if(!valid)return;
  if(await action.run(`${parkingRoot(propertyId)}/allocations/${allocation.id}/end`,"POST",{endDate,version:allocation.version},readAllocation,t("allocationEnded"))){onSaved();onClose();}}
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><CalendarX2 className="size-9 text-primary"/><DialogTitle>{t("endAllocation")}</DialogTitle><DialogDescription>{t("endAllocationHelp",{space:allocation.spaceCode,unit:allocation.unitNumber})}</DialogDescription></DialogHeader>
  <form onSubmit={save} noValidate className="space-y-5">
   {action.stale&&<FieldError>{t("staleHelp")}</FieldError>}
   <div><Label htmlFor="allocation-end-date">{t("endDate")} *</Label><Input id="allocation-end-date" type="date" min={allocation.startDate} max="2100-12-31" value={endDate} aria-invalid={!valid} onChange={e=>setEndDate(e.target.value)}/></div>
   <DialogFooter><Button type="button" variant="outline" disabled={action.busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" variant="warn" disabled={action.busy||!valid||action.stale}>{action.busy?<LoaderCircle className="animate-spin"/>:<CalendarX2/>}{t("endAllocation")}</Button></DialogFooter>
  </form>
 </ManagementPanel></Dialog>;
}
