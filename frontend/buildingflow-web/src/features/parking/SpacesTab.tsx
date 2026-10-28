"use client";
import {useEffect,useState} from "react";
import {useTranslations} from "next-intl";
import {Building2,Layers,LoaderCircle,Plus,Save,SquareParking,Trash2,Warehouse} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {Skeleton} from "@/components/ui/skeleton";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {cn} from "@/lib/utils";
import {ParkingKind} from "./ParkingBadges";
import {mapState,spaceStyle} from "./parking-style";
import {limits,readFacilities,readFacility,readLevelResponse,readPage,readSpace,spaceState,spaceTypes,type Facility,type Space,type SpaceType} from "./model";
import {parkingRoot,useParkingAction,useParkingRead} from "./useParking";

type Panel={kind:"facility"}|{kind:"level";facility:Facility}|{kind:"space";facility:Facility;levelId:string}|{kind:"detail";space:Space};

export function ParkingMap({spaces,onSelect}:{spaces:readonly Space[];onSelect?:(space:Space)=>void}){
 const t=useTranslations("parking");
 return <ul className="grid grid-cols-[repeat(auto-fill,minmax(5.5rem,1fr))] gap-2" aria-label={t("map")}>
  {spaces.map(s=>{const state=spaceState(s),style=mapState[state],Type=spaceStyle[s.type].icon;
   const label=`${s.code} · ${t(`state_${state}`)}${s.allocatedUnitNumber?` · ${t("unit")} ${s.allocatedUnitNumber}`:""}`;
   return <li key={s.id}><button type="button" disabled={!onSelect} onClick={()=>onSelect?.(s)} aria-label={label} title={label}
    className={cn("dashboard-status flex h-20 w-full flex-col items-center justify-center gap-1 rounded-xl border-2 text-sm font-bold focus-visible:outline-2 focus-visible:outline-offset-2",`tone-${style.tone}`,onSelect&&"cursor-pointer hover:brightness-95")}>
    <span className="flex items-center gap-1"><Type aria-hidden="true" className="size-4"/>{s.code}</span>
    <span className="flex items-center gap-1 text-[11px] font-medium"><style.icon aria-hidden="true" className="size-3.5"/>{s.allocatedUnitNumber??t(`state_${state}`)}</span>
   </button></li>;})}
 </ul>;
}

export function MapLegend(){
 const t=useTranslations("parking");
 return <ul className="flex flex-wrap gap-3 text-xs" aria-label={t("legend")}>{(Object.keys(mapState) as (keyof typeof mapState)[]).map(k=>{const s=mapState[k];return <li key={k} className={cn("dashboard-status inline-flex items-center gap-1",`tone-${s.tone}`)}><s.icon aria-hidden="true" className="size-3.5"/>{t(`state_${k}`)}</li>;})}</ul>;
}

export function SpacesTab({propertyId,canWrite,revision,onChanged}:{propertyId:string;canWrite:boolean;revision:number;onChanged:()=>void}){
 const t=useTranslations("parking");
 const [local,setLocal]=useState(0),[facilityId,setFacilityId]=useState(""),[levelId,setLevelId]=useState(""),[panel,setPanel]=useState<Panel|null>(null);
 const facilities=useParkingRead(`${parkingRoot(propertyId)}/facilities`,readFacilities,revision+local);
 const facility=facilities.data?.find(f=>f.id===facilityId)??null;
 useEffect(()=>{if(facilities.data&&!facility)setFacilityId(facilities.data[0]?.id??"");},[facilities.data,facility]);
 useEffect(()=>setLevelId(""),[facilityId]);
 const query=new URLSearchParams({facilityId,pageSize:"100"});if(levelId)query.set("levelId",levelId);
 const spaces=useParkingRead(facilityId?`${parkingRoot(propertyId)}/spaces?${query}`:null,v=>readPage(v,readSpace),revision+local);
 const changed=()=>{setLocal(n=>n+1);onChanged();};
 return <section className="space-y-4" aria-label={t("tab_spaces")}>
  <div className="flex flex-wrap items-end gap-3 rounded-2xl border bg-card p-4 shadow-sm">
   <div><Label htmlFor="parking-facility">{t("facility")}</Label><select id="parking-facility" className={toolbarSelectClass+" min-w-48"} value={facilityId} disabled={facilities.loading||!facilities.data?.length} onChange={e=>setFacilityId(e.target.value)}>{facilities.data?.map(f=><option key={f.id} value={f.id}>{f.name}{f.buildingName?` · ${f.buildingName}`:""}</option>)}</select></div>
   <div><Label htmlFor="parking-level">{t("level")}</Label><select id="parking-level" className={toolbarSelectClass+" min-w-36"} value={levelId} disabled={!facility?.levels.length} onChange={e=>setLevelId(e.target.value)}><option value="">{t("allLevels")}</option>{facility?.levels.map(l=><option key={l.id} value={l.id}>{l.name}</option>)}</select></div>
   {canWrite&&<div className="ml-auto flex flex-wrap gap-2"><Button variant="outline" onClick={()=>setPanel({kind:"facility"})}><Warehouse/>{t("newFacility")}</Button>
    {facility&&<Button variant="outline" onClick={()=>setPanel({kind:"level",facility})}><Layers/>{t("newLevel")}</Button>}
    {facility&&<Button onClick={()=>setPanel({kind:"space",facility,levelId})}><Plus/>{t("newSpace")}</Button>}</div>}
  </div>
  {facilities.error&&<MessageBanner>{facilities.error}</MessageBanner>}
  {facilities.data&&!facilities.data.length&&<div className="rounded-2xl border bg-card p-10 text-center text-muted-foreground"><Warehouse className="mx-auto mb-3 size-8"/><strong>{t("facilitiesEmpty")}</strong><p className="mt-2">{t("facilitiesEmptyHelp")}</p></div>}
  {facility&&<div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-5">
   <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><h2 className="flex items-center gap-2 text-lg font-bold"><SquareParking aria-hidden="true" className="size-5 text-primary"/>{facility.name}{facility.buildingName&&<span className="flex items-center gap-1 text-sm font-normal text-muted-foreground"><Building2 aria-hidden="true" className="size-4"/>{facility.buildingName}</span>}</h2><MapLegend/></div>
   {spaces.loading&&!spaces.data?<Skeleton className="h-40 w-full"/>:spaces.error?<MessageBanner>{spaces.error}</MessageBanner>:spaces.data?.items.length?<>
    <ParkingMap spaces={spaces.data.items} onSelect={s=>setPanel({kind:"detail",space:s})}/>
    {spaces.data.totalCount>spaces.data.items.length&&<p className="mt-3 text-xs text-muted-foreground">{t("mapLimited",{shown:spaces.data.items.length,total:spaces.data.totalCount})}</p>}
   </>:<p className="py-8 text-center text-muted-foreground">{t("spacesEmpty")}</p>}
  </div>}
  {panel&&<SpacePanel propertyId={propertyId} panel={panel} canWrite={canWrite} onClose={()=>setPanel(null)} onSaved={changed}/>}
 </section>;
}

function SpacePanel({propertyId,panel,canWrite,onClose,onSaved}:{propertyId:string;panel:Panel;canWrite:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),scope=usePropertyScope();
 const action=useParkingAction(),root=parkingRoot(propertyId);
 const [name,setName]=useState(""),[order,setOrder]=useState("0"),[buildingId,setBuildingId]=useState(""),[code,setCode]=useState(""),[type,setType]=useState<SpaceType>("STANDARD"),[levelId,setLevelId]=useState(panel.kind==="space"?panel.levelId:""),[invalid,setInvalid]=useState(false),[confirm,setConfirm]=useState(false);
 const buildings=useParkingRead(panel.kind==="facility"&&scope.property?.type!=="APARTMENT"?`/api/v1/properties/${propertyId}/buildings?page=1&pageSize=50`:null,readBuildings,0);
 async function save(e:React.FormEvent){e.preventDefault();
  if(panel.kind==="facility"){if(!name.trim()||name.trim().length>limits.facility)return setInvalid(true);if(await action.run(`${root}/facilities`,"POST",{name:name.trim(),buildingId:buildingId||null},readFacility,t("facilityCreated"))){onSaved();onClose();}}
  if(panel.kind==="level"){const n=Number(order);if(!name.trim()||name.trim().length>limits.level||!Number.isInteger(n)||n<-10||n>50)return setInvalid(true);if(await action.run(`${root}/levels`,"POST",{facilityId:panel.facility.id,name:name.trim(),sortOrder:n},readLevelResponse,t("levelCreated"))){onSaved();onClose();}}
  if(panel.kind==="space"){if(!code.trim()||code.trim().length>limits.code)return setInvalid(true);if(await action.run(`${root}/spaces`,"POST",{facilityId:panel.facility.id,levelId:levelId||null,code:code.trim(),type},readSpace,t("spaceCreated"))){setCode("");onSaved();}}
 }
 async function remove(space:Space){if(await action.run(`${root}/spaces/${space.id}?version=${space.version}`,"DELETE",undefined,()=>true,t("spaceDeleted"))){onSaved();onClose();}}
 const title=panel.kind==="facility"?t("newFacility"):panel.kind==="level"?t("newLevel"):panel.kind==="space"?t("newSpace"):t("spaceDetails");
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><SquareParking className="size-9 text-primary"/><DialogTitle>{title}</DialogTitle><DialogDescription>{panel.kind==="space"?t("newSpaceHelp"):t("setupHelp")}</DialogDescription></DialogHeader>
  {panel.kind==="detail"?<>
   <h2 className="my-4 text-2xl font-bold">{panel.space.code}</h2>
   <div className="space-y-3 text-sm"><ParkingKind icon={spaceStyle[panel.space.type].icon} tone={spaceStyle[panel.space.type].tone} label={t(`space_${panel.space.type}`)}/><p>{panel.space.allocatedUnitNumber?t("allocatedTo",{unit:panel.space.allocatedUnitNumber}):t("notAllocated")}</p></div>
   {canWrite&&!confirm&&<div className="my-5"><Button variant="danger" disabled={action.busy} onClick={()=>setConfirm(true)}><Trash2/>{t("deleteSpace")}</Button></div>}
   {confirm&&<div className="my-5 space-y-3 rounded-xl border p-4"><p className="text-sm">{t("deleteSpaceHelp")}</p><div className="flex flex-wrap gap-2"><Button variant="destructive" disabled={action.busy} onClick={()=>void remove(panel.space)}>{action.busy?<LoaderCircle className="animate-spin"/>:<Trash2/>}{t("deleteConfirm")}</Button><Button variant="outline" disabled={action.busy} onClick={()=>setConfirm(false)}>{ui("cancel")}</Button></div></div>}
   <DialogFooter><Button variant="outline" disabled={action.busy} onClick={onClose}>{ui("close")}</Button></DialogFooter></>
  :<form onSubmit={save} noValidate className="space-y-5"><fieldset disabled={action.busy||!canWrite} className="space-y-5">
   {(panel.kind==="facility"||panel.kind==="level")&&<div><Label htmlFor="parking-name">{t(panel.kind==="facility"?"facilityName":"levelName")} *</Label><Input id="parking-name" maxLength={panel.kind==="facility"?limits.facility:limits.level} value={name} aria-invalid={invalid} placeholder={t(panel.kind==="facility"?"facilityHint":"levelHint")} onChange={e=>{setName(e.target.value);setInvalid(false);}}/></div>}
   {panel.kind==="facility"&&scope.property?.type!=="APARTMENT"&&<div><Label htmlFor="parking-building">{t("building")}</Label><select id="parking-building" className={toolbarSelectClass+" w-full"} value={buildingId} onChange={e=>setBuildingId(e.target.value)}><option value="">{t("sharedFacility")}</option>{buildings.data?.map(b=><option key={b.id} value={b.id}>{b.name}</option>)}</select></div>}
   {panel.kind==="level"&&<div><Label htmlFor="parking-order">{t("levelOrder")} *</Label><Input id="parking-order" type="number" min={-10} max={50} value={order} aria-invalid={invalid} onChange={e=>{setOrder(e.target.value);setInvalid(false);}}/></div>}
   {panel.kind==="space"&&<>
    <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="parking-code">{t("spaceCode")} *</Label><Input id="parking-code" className="uppercase" maxLength={limits.code} value={code} aria-invalid={invalid} placeholder="A01" onChange={e=>{setCode(e.target.value);setInvalid(false);}}/></div>
     <div><Label htmlFor="parking-space-level">{t("level")}</Label><select id="parking-space-level" className={toolbarSelectClass+" w-full"} value={levelId} onChange={e=>setLevelId(e.target.value)}><option value="">{t("noLevel")}</option>{panel.facility.levels.map(l=><option key={l.id} value={l.id}>{l.name}</option>)}</select></div></div>
    <div role="radiogroup" aria-labelledby="space-type-label"><p id="space-type-label" className="mb-2 text-sm font-medium">{t("spaceType")} *</p><div className="request-type-grid">{spaceTypes.map(k=>{const s=spaceStyle[k];return <button key={k} type="button" role="radio" aria-checked={type===k} onClick={()=>setType(k)} className={`request-type-option${type===k?` tone-${s.tone}`:""}`}><span className={`dashboard-icon tone-${s.tone}`}><s.icon aria-hidden="true"/></span><span>{t(`space_${k}`)}</span></button>;})}</div></div>
   </>}
   {invalid&&<FieldError>{t("setupInvalid")}</FieldError>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={action.busy} onClick={onClose}>{ui(panel.kind==="space"?"close":"cancel")}</Button><Button type="submit" disabled={action.busy||!canWrite}>{action.busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("save")}</Button></DialogFooter></form>}
 </ManagementPanel></Dialog>;
}

function readBuildings(v:unknown):{id:string;name:string}[]{
 const items=v&&typeof v==="object"&&!Array.isArray(v)?(v as {items?:unknown}).items:v;
 if(!Array.isArray(items)||!items.every(b=>b&&typeof b==="object"&&typeof (b as {id?:unknown}).id==="string"&&typeof (b as {name?:unknown}).name==="string"))throw new Error("Invalid buildings");
 return items.map(b=>({id:(b as {id:string}).id,name:(b as {name:string}).name}));
}
