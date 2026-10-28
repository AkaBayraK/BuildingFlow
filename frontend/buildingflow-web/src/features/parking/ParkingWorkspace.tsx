"use client";
import {useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {BadgeCheck,Car,CircleParking,Clock3,DoorOpen,Users} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {WorkspaceHeading,SummaryCard} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatNumber} from "@/lib/format";
import {AllocationsTab} from "./AllocationsTab";
import {ClosuresTab} from "./ClosuresTab";
import {GuestsTab} from "./GuestsTab";
import {MapLegend,ParkingMap,SpacesTab} from "./SpacesTab";
import {ReportsTab} from "./ReportsTab";
import {RulesTab} from "./RulesTab";
import {VehiclesTab,VehicleStatusBadge} from "./VehiclesTab";
import {parkingTabs,tabIcon,type ParkingTab} from "./parking-style";
import {istanbulDayWindow,readGuestPolicy,readAllocation,readFacilities,readGuest,readPage,readPolicy,readSpace,readVehicle} from "./model";
import {parkingRoot,useParkingRead} from "./useParking";

type Locale="tr"|"en";
const managerRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER"];

export function ParkingWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),identity=useIdentity(),scope=usePropertyScope();
 const manager=managerRoles.some(r=>identity.hasRealmRole(r)),guard=manager||identity.hasRealmRole("SECURITY");
 const writable=scope.property?.id===propertyId&&!!scope.property.isEnabled&&!scope.error;
 const tabs:readonly ParkingTab[]=manager?parkingTabs:["guests","closures"];
 const [tab,setTab]=useState<ParkingTab>(manager?"overview":"guests"),[revision,setRevision]=useState(0);
 const changed=()=>setRevision(n=>n+1);
 const policy=useParkingRead(manager?`${parkingRoot(propertyId)}/policy`:null,v=>readPolicy(v,propertyId),revision);
 const guestPolicy=useParkingRead(guard?`${parkingRoot(propertyId)}/guest-policy`:null,readGuestPolicy,revision);
 const current=policy.data;
 if(!guard)return <div className="py-8"><MessageBanner>{ui("parkingRoleRequired")}</MessageBanner></div>;
 return <div className="py-8">
  <WorkspaceHeading title={t("title")} description={manager?t("help"):t("guardHelp")} scope={scope.property?.name??ui("loading")} icon={Car}/>
  {scope.error&&<MessageBanner action={<Button variant="view" onClick={scope.refresh}>{ui("tryAgain")}</Button>}>{scope.error}</MessageBanner>}
  {scope.property&&!scope.property.isEnabled&&<MessageBanner kind="warning">{t("inactive")}</MessageBanner>}
  {tabs.length>1&&<div role="tablist" aria-label={t("title")} className="mb-6 flex flex-wrap gap-2">{tabs.map(value=>{const I=tabIcon[value];return <Button key={value} role="tab" id={`parking-tab-${value}`} aria-controls="parking-panel" aria-selected={tab===value} tabIndex={tab===value?0:-1} variant={tab===value?"default":"outline"} onClick={()=>setTab(value)} onKeyDown={e=>{
    const index=tabs.indexOf(value),next=e.key==="Home"?tabs[0]:e.key==="End"?tabs[tabs.length-1]:e.key==="ArrowRight"?tabs[(index+1)%tabs.length]:e.key==="ArrowLeft"?tabs[(index+tabs.length-1)%tabs.length]:null;
    if(next){e.preventDefault();setTab(next);e.currentTarget.parentElement?.querySelector<HTMLButtonElement>(`#parking-tab-${next}`)?.focus();}
   }}><I aria-hidden="true"/>{t(`tab_${value}`)}</Button>;})}</div>}
  <div role="tabpanel" id="parking-panel" aria-labelledby={`parking-tab-${tab}`}>
   {tab==="overview"&&manager&&<Overview propertyId={propertyId} revision={revision} onOpen={setTab}/>}
   {tab==="vehicles"&&manager&&<VehiclesTab propertyId={propertyId} canWrite={writable} revision={revision} onChanged={changed}/>}
   {tab==="spaces"&&manager&&<SpacesTab propertyId={propertyId} canWrite={writable} revision={revision} onChanged={changed}/>}
   {tab==="allocations"&&manager&&<AllocationsTab propertyId={propertyId} canWrite={writable} revision={revision} onChanged={changed}/>}
   {tab==="guests"&&<GuestsTab propertyId={propertyId} canWrite={writable} maxHours={guestPolicy.data?.configured?guestPolicy.data.guestMaxHours:null} policyLoading={guestPolicy.loading} policyError={guestPolicy.error} revision={revision} onChanged={changed}/>}
   {tab==="closures"&&<ClosuresTab propertyId={propertyId} canWrite={writable&&manager} revision={revision} onChanged={changed}/>}
   {tab==="reports"&&manager&&<ReportsTab propertyId={propertyId} revision={revision}/>}
   {tab==="rules"&&manager&&<RulesTab propertyId={propertyId} policy={current??null} canWrite={writable} onSaved={changed} onReload={changed}/>}
  </div>
 </div>;
}

function Overview({propertyId,revision,onOpen}:{propertyId:string;revision:number;onOpen:(tab:ParkingTab)=>void}){
 const t=useTranslations("parking"),locale=useLocale() as Locale,root=parkingRoot(propertyId);
 const facilities=useParkingRead(`${root}/facilities`,readFacilities,revision);
 const spaces=useParkingRead(`${root}/spaces?pageSize=100`,v=>readPage(v,readSpace),revision);
 const approved=useParkingRead(`${root}/vehicles?status=APPROVED&pageSize=1`,v=>readPage(v,readVehicle),revision);
 const pending=useParkingRead(`${root}/vehicles?status=PENDING&pageSize=5`,v=>readPage(v,readVehicle),revision);
 const allocations=useParkingRead(`${root}/allocations?activeOnly=true&pageSize=1`,v=>readPage(v,readAllocation),revision);
 const {start,end}=istanbulDayWindow();
 const guests=useParkingRead(`${root}/guests?pageSize=1&from=${encodeURIComponent(start)}&to=${encodeURIComponent(end)}`,v=>readPage(v,readGuest),revision);
 const total=facilities.data?.reduce((a,f)=>a+f.spaceCount,0);
 const value=(n:number|undefined,loading:boolean)=>loading?"…":n===undefined?"—":formatNumber(n,locale);
 return <div className="space-y-6">
  <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
   <SummaryCard title={t("kpiCapacity")} value={value(total,facilities.loading)} note={t("kpiCapacityNote",{count:facilities.data?.length??0})} icon={CircleParking}/>
   <SummaryCard title={t("kpiAllocations")} value={value(allocations.data?.totalCount,allocations.loading)} note={t("kpiAllocationsNote")} icon={DoorOpen}/>
   <SummaryCard title={t("kpiApproved")} value={value(approved.data?.totalCount,approved.loading)} note={t("kpiApprovedNote")} icon={BadgeCheck}/>
   <SummaryCard title={t("kpiPending")} value={value(pending.data?.totalCount,pending.loading)} note={t("kpiPendingNote")} icon={Clock3}/>
   <SummaryCard title={t("kpiGuests")} value={value(guests.data?.totalCount,guests.loading)} note={t("kpiGuestsNote")} icon={Users}/>
  </div>
  <div className="grid gap-6 xl:grid-cols-[2fr_1fr]">
   <section className="rounded-2xl border bg-card p-5 shadow-sm" aria-labelledby="parking-map-title">
    <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><h2 id="parking-map-title" className="text-lg font-bold">{t("map")}</h2><MapLegend/></div>
    {spaces.loading&&!spaces.data?<Skeleton className="h-40 w-full"/>:spaces.error?<MessageBanner>{spaces.error}</MessageBanner>:spaces.data?.items.length?<ParkingMap spaces={spaces.data.items}/>
     :<div className="py-8 text-center text-muted-foreground"><p>{t("mapEmpty")}</p><Button className="mt-3" variant="view" onClick={()=>onOpen("spaces")}>{t("tab_spaces")}</Button></div>}
    <p className="mt-3 text-xs text-muted-foreground">{t("mapNote")}</p>
   </section>
   <section className="rounded-2xl border bg-card p-5 shadow-sm" aria-labelledby="parking-pending-title">
    <div className="mb-4 flex items-center justify-between gap-3"><h2 id="parking-pending-title" className="text-lg font-bold">{t("pendingApprovals")}</h2><Button variant="view" size="sm" onClick={()=>onOpen("vehicles")}>{t("all")}</Button></div>
    {pending.loading&&!pending.data?<Skeleton className="h-24 w-full"/>:pending.data?.items.length?<ul className="space-y-3">{pending.data.items.map(v=><li key={v.id} className="flex items-center justify-between gap-3 rounded-xl border p-3"><div><strong className="font-mono">{v.plate}</strong><p className="text-xs text-muted-foreground">{v.personName} · {v.unitNumber}</p></div><VehicleStatusBadge status={v.status}/></li>)}</ul>
     :<p className="text-sm text-muted-foreground">{t("noPending")}</p>}
   </section>
  </div>
 </div>;
}
