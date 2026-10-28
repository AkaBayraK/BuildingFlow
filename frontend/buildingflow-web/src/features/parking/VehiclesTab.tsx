"use client";
import {useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {BadgeCheck,Ban,Car,CircleSlash,Eye,LayoutGrid,LoaderCircle,Plus,RefreshCw,Save} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {FilterChips} from "@/components/management/FilterChips";
import {ListToolbar,DetailField,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useSearch} from "@/features/people/useRows";
import {formatDateTime} from "@/lib/format";
import {ParkingBadge,ParkingKind} from "./ParkingBadges";
import {vehicleStatusStyle,vehicleTypeIcon} from "./parking-style";
import {UnitPicker} from "./UnitPicker";
import {limits,readPage,readResidents,readVehicle,vehicleBody,vehicleStatuses,vehicleTypes,type Vehicle,type VehicleStatus,type VehicleType} from "./model";
import {parkingRoot,useParkingAction,useParkingRead,useRequestId} from "./useParking";

type Locale="tr"|"en";
export function VehicleStatusBadge({status}:{status:VehicleStatus}){const t=useTranslations("parking");const s=vehicleStatusStyle[status];return <ParkingBadge icon={s.icon} tone={s.tone} label={t(`vehicle_${status}`)}/>;}

export function VehiclesTab({propertyId,canWrite,revision,onChanged,initialStatus=""}:{propertyId:string;canWrite:boolean;revision:number;onChanged:()=>void;initialStatus?:string}){
 const t=useTranslations("parking"),ui=useTranslations("ui");
 const [term,setTerm]=useState(""),search=useSearch(term),[status,setStatus]=useState(initialStatus),[page,setPage]=useState(1),[panel,setPanel]=useState<Vehicle|"new"|null>(null),[local,setLocal]=useState(0);
 const query=new URLSearchParams({page:String(page),pageSize:"20"});if(search)query.set("search",search);if(status)query.set("status",status);
 const list=useParkingRead(`${parkingRoot(propertyId)}/vehicles?${query}`,v=>readPage(v,readVehicle),revision+local);
 const refresh=()=>setLocal(n=>n+1);
 const chips=[{value:"",label:t("all"),icon:LayoutGrid},...vehicleStatuses.map(s=>({value:s,label:t(`vehicle_${s}`),icon:vehicleStatusStyle[s].icon,tone:vehicleStatusStyle[s].tone}))];
 const columns=useMemo<ColumnDef<Vehicle>[]>(()=>[
  {id:"plate",header:t("plate"),cell:({row})=><strong className="font-mono tracking-wide">{row.original.plate}</strong>},
  {id:"vehicle",header:t("vehicle"),cell:({row})=><ParkingKind icon={vehicleTypeIcon[row.original.type]} tone="blue" label={[row.original.brand,row.original.model,row.original.color].filter(Boolean).join(" · ")||t(`type_${row.original.type}`)}/>},
  {id:"owner",header:t("owner"),cell:({row})=>row.original.personName},
  {id:"unit",header:t("unit"),cell:({row})=>row.original.unitNumber},
  {id:"status",header:ui("status"),cell:({row})=><VehicleStatusBadge status={row.original.status}/>},
  {id:"actions",header:ui("actions"),cell:({row})=><Button variant="view" size="icon" aria-label={`${t("details")} · ${row.original.plate}`} onClick={()=>setPanel(row.original)}><Eye/></Button>},
 ],[t,ui]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-label={t("tab_vehicles")}>
  <div className="flex flex-wrap items-center justify-between gap-3 border-b p-4 sm:px-5"><FilterChips label={t("statusFilter")} items={chips} value={status} onChange={v=>{setStatus(v);setPage(1);}} disabled={list.loading&&!list.data}/>{canWrite&&<Button onClick={()=>setPanel("new")}><Plus/>{t("newVehicle")}</Button>}</div>
  <ListToolbar term={term} onTerm={v=>{setTerm(v);setPage(1);}} searchLabel={t("vehicleSearch")} placeholder={t("vehicleSearch")} loading={list.loading} onRefresh={refresh} onReset={()=>{setTerm("");setStatus("");setPage(1);}} resetEnabled={!!term||!!status}/>
  <RecordList compact data={list.data} loading={list.loading} error={list.error} columns={columns} page={page} onPage={setPage} onRetry={refresh}
   empty={<><Car className="mx-auto mb-3 size-8"/><strong>{t("vehiclesEmpty")}</strong><p className="mt-2">{t("vehiclesEmptyHelp")}</p></>}
   card={v=><><div className="flex items-start justify-between gap-3"><strong className="font-mono tracking-wide">{v.plate}</strong><VehicleStatusBadge status={v.status}/></div>
    <p className="my-2 text-xs text-muted-foreground">{[v.personName,v.unitNumber,v.brand,v.model].filter(Boolean).join(" · ")}</p>
    <Button variant="view" size="sm" onClick={()=>setPanel(v)}><Eye/>{t("details")}</Button></>}/>
  {panel==="new"&&<VehicleCreatePanel propertyId={propertyId} onClose={()=>setPanel(null)} onSaved={()=>{refresh();onChanged();}}/>}
  {panel&&panel!=="new"&&<VehicleReviewPanel propertyId={propertyId} vehicle={panel} canWrite={canWrite} onClose={()=>setPanel(null)} onSaved={()=>{refresh();onChanged();}}/>}
 </section>;
}

function VehicleReviewPanel({propertyId,vehicle,canWrite,onClose,onSaved}:{propertyId:string;vehicle:Vehicle;canWrite:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),locale=useLocale() as Locale;
 const [record,setRecord]=useState(vehicle),[reason,setReason]=useState(""),[rejecting,setRejecting]=useState(false),[confirmPassive,setConfirmPassive]=useState(false),[invalid,setInvalid]=useState(false);
 const action=useParkingAction(),root=`${parkingRoot(propertyId)}/vehicles/${record.id}`;
 async function act(kind:"approve"|"reject"|"deactivate"){
  if(kind==="reject"&&(!reason.trim()||reason.trim().length>limits.reason)){setInvalid(true);return;}
  const saved=await action.run(`${root}/${kind}`,"POST",{version:record.version,reason:kind==="reject"?reason.trim():null},readVehicle,t(kind==="approve"?"vehicleApproved":kind==="reject"?"vehicleRejected":"vehicleDeactivated"));
  if(saved){setRecord(saved);setRejecting(false);setConfirmPassive(false);onSaved();}
 }
 async function reload(){const page=await action.run(`${parkingRoot(propertyId)}/vehicles?search=${record.plate}&pageSize=20`,"GET",undefined,v=>readPage(v,readVehicle));const current=page?.items.find(v=>v.id===record.id);if(current){setRecord(current);action.setStale(false);}}
 const pending=record.status==="PENDING",active=pending||record.status==="APPROVED";
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><Car className="size-9 text-primary"/><DialogTitle>{t("vehicleDetails")}</DialogTitle><DialogDescription>{t("vehicleReviewHelp")}</DialogDescription></DialogHeader>
  {action.stale&&<MessageBanner kind="warning" action={<Button variant="view" size="sm" disabled={action.busy} onClick={()=>void reload()}><RefreshCw/>{t("loadCurrent")}</Button>}>{t("staleHelp")}</MessageBanner>}
  <h2 className="my-4 font-mono text-2xl font-bold tracking-wider">{record.plate}</h2>
  <div className="grid gap-4 sm:grid-cols-2">
   <DetailField label={ui("status")}><VehicleStatusBadge status={record.status}/></DetailField>
   <DetailField label={t("vehicleType")}><ParkingKind icon={vehicleTypeIcon[record.type]} tone="blue" label={t(`type_${record.type}`)}/></DetailField>
   <DetailField label={t("owner")}>{record.personName}</DetailField><DetailField label={t("unit")}>{record.unitNumber}</DetailField>
   <DetailField label={t("brandModel")}>{[record.brand,record.model].filter(Boolean).join(" ")||"—"}</DetailField><DetailField label={t("color")}>{record.color??"—"}</DetailField>
   <DetailField label={t("createdAt")}>{formatDateTime(record.createdAt,locale)}</DetailField><DetailField label={t("reviewedAt")}>{record.reviewedAt?formatDateTime(record.reviewedAt,locale):"—"}</DetailField>
  </div>
  {record.reviewReason&&<p className="my-4 whitespace-pre-wrap break-words rounded-xl bg-muted p-4 text-sm"><strong>{t("rejectReason")}: </strong>{record.reviewReason}</p>}
  {canWrite&&active&&<div className="my-5 flex flex-wrap gap-2">
   {pending&&<Button disabled={action.busy||action.stale} onClick={()=>void act("approve")}>{action.busy?<LoaderCircle className="animate-spin"/>:<BadgeCheck/>}{t("approve")}</Button>}
   {pending&&<Button variant="warn" disabled={action.busy||action.stale} onClick={()=>setRejecting(true)}><Ban/>{t("reject")}</Button>}
   <Button variant="danger" disabled={action.busy||action.stale} onClick={()=>setConfirmPassive(true)}><CircleSlash/>{t("deactivate")}</Button>
  </div>}
  {rejecting&&<div className="space-y-3 rounded-xl border p-4"><Label htmlFor="vehicle-reason">{t("rejectReason")} *</Label><Textarea id="vehicle-reason" maxLength={limits.reason} value={reason} aria-invalid={invalid} aria-describedby={invalid?"vehicle-reason-error":undefined} onChange={e=>{setReason(e.target.value);setInvalid(false);}}/>
   {invalid&&<FieldError id="vehicle-reason-error">{t("rejectReasonRequired")}</FieldError>}
   <div className="flex flex-wrap gap-2"><Button variant="destructive" disabled={action.busy} onClick={()=>void act("reject")}>{t("rejectConfirm")}</Button><Button variant="outline" disabled={action.busy} onClick={()=>setRejecting(false)}>{ui("cancel")}</Button></div></div>}
  {confirmPassive&&<div className="space-y-3 rounded-xl border p-4"><p className="text-sm">{t("deactivateHelp")}</p><div className="flex flex-wrap gap-2"><Button variant="destructive" disabled={action.busy} onClick={()=>void act("deactivate")}>{t("deactivateConfirm")}</Button><Button variant="outline" disabled={action.busy} onClick={()=>setConfirmPassive(false)}>{ui("cancel")}</Button></div></div>}
  <DialogFooter><Button variant="outline" disabled={action.busy} onClick={onClose}>{ui("close")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}

function VehicleCreatePanel({propertyId,onClose,onSaved}:{propertyId:string;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui");
 const [form,setForm]=useState({unitId:"",personId:"",plate:"",type:"CAR" as VehicleType,brand:"",model:"",color:""}),[invalid,setInvalid]=useState(false);
 const residents=useParkingRead(form.unitId?`${parkingRoot(propertyId)}/units/${form.unitId}/residents`:null,readResidents,0);
 const requestId=useRequestId(JSON.stringify(form));const action=useParkingAction();
 const set=<K extends keyof typeof form>(k:K,v:(typeof form)[K])=>{setForm(f=>({...f,[k]:v}));setInvalid(false);};
 async function save(e:React.FormEvent){e.preventDefault();const body=vehicleBody(form,requestId);if(!body){setInvalid(true);return;}
  if(await action.run(`${parkingRoot(propertyId)}/vehicles`,"POST",body,readVehicle,t("vehicleCreated"))){onSaved();onClose();}}
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><Car className="size-9 text-primary"/><DialogTitle>{t("newVehicle")}</DialogTitle><DialogDescription>{t("newVehicleHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} noValidate className="space-y-5"><fieldset disabled={action.busy} className="space-y-5">
   <UnitPicker propertyId={propertyId} value={form.unitId} required onChange={u=>setForm(f=>({...f,unitId:u?.id??"",personId:""}))}/>
   <div><Label htmlFor="vehicle-person">{t("owner")} *</Label><select id="vehicle-person" className={toolbarSelectClass+" w-full"} disabled={!form.unitId||residents.loading} value={form.personId} aria-invalid={invalid&&!form.personId} onChange={e=>set("personId",e.target.value)}>
    <option value="">{!form.unitId?t("chooseUnitFirst"):residents.loading?t("loading"):residents.data?.length?t("choosePerson"):t("noResidents")}</option>
    {residents.data?.map(r=><option key={r.personId+r.relation} value={r.personId}>{r.name} · {t(`relation_${r.relation}`)}</option>)}</select></div>
   <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="vehicle-plate">{t("plate")} *</Label><Input id="vehicle-plate" className="font-mono uppercase" maxLength={16} value={form.plate} placeholder="34 ABC 123" aria-invalid={invalid} onChange={e=>set("plate",e.target.value)}/></div>
    <div><Label htmlFor="vehicle-type">{t("vehicleType")} *</Label><select id="vehicle-type" className={toolbarSelectClass+" w-full"} value={form.type} onChange={e=>set("type",e.target.value as VehicleType)}>{vehicleTypes.map(v=><option key={v} value={v}>{t(`type_${v}`)}</option>)}</select></div></div>
   <div className="grid gap-4 sm:grid-cols-3">{(["brand","model","color"] as const).map(k=><div key={k}><Label htmlFor={`vehicle-${k}`}>{t(k)}</Label><Input id={`vehicle-${k}`} maxLength={limits.text} value={form[k]} onChange={e=>set(k,e.target.value)}/></div>)}</div>
   {invalid&&<FieldError>{t("vehicleInvalid")}</FieldError>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={action.busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={action.busy}>{action.busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("save")}</Button></DialogFooter></form>
 </ManagementPanel></Dialog>;
}
