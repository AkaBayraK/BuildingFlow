"use client";
import {useState,useEffect} from "react";
import Link from "next/link";
import {useTranslations,useLocale} from "next-intl";
import {usePropertyScope} from "./PropertyScope";
import {useAuthorizedApi,errorMessage,propertyTypeNames,type Building} from "@/services/api";
import {HierarchyRows} from "@/features/hierarchy/HierarchyRows";
import {useRows,useSearch} from "@/features/people/useRows";
import {RecordList} from "@/components/management/RecordList";
import {Input} from "@/components/ui/input";
import {BackButton} from "@/components/navigation/BackButton";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {Skeleton} from "@/components/ui/skeleton";
import {Building2,DoorOpen,Layers,MapPin,ArrowUpRight} from "lucide-react";
import {WorkspaceHeading,SummaryCard} from "@/components/management/WorkspacePresentation";
import type {UnitFilter} from "@/features/people/unit-links";
import {formatNumber} from "@/lib/format";
export function ManagementPage({section,buildingId,unitFilter}:{section:"buildings"|"units";buildingId?:string;unitFilter?:UnitFilter}){
 const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const scope=usePropertyScope();const api=useAuthorizedApi();const [building,setBuilding]=useState<Building|null>(null);const [error,setError]=useState("");
 useEffect(()=>{const controller=new AbortController();setBuilding(null);setError("");if(buildingId&&scope.propertyId)void api<Building>(`/api/v1/properties/${scope.propertyId}/buildings/${buildingId}`,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setBuilding(value);}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));});return()=>controller.abort();},[api,buildingId,scope.propertyId,locale]);
 const title=section==="buildings"?t("navBuildings"):scope.property?.type==="APARTMENT"?t("apartments"):t("navUnits");
 if(scope.error)return <div className="py-10"><MessageBanner action={<Button variant="view" onClick={scope.refresh}>{t("tryAgain")}</Button>}>{scope.error}</MessageBanner></div>;
 if(!scope.property)return <div className="py-10" role="status"><span className="sr-only">{t("loading")}</span><Skeleton className="h-24"/></div>;
 if(section==="buildings"&&scope.property.type==="APARTMENT")return <div className="py-10"><MessageBanner kind="info" action={<Button asChild><Link href={`/properties/${scope.propertyId}/units`}>{t("openApartments")}</Link></Button>}>{t("apartmentHasDirectUnits")}</MessageBanner></div>;
 return <div className="py-8">{section==="units"?<>
 <WorkspaceHeading title={title} description={t("unitsPageDescription")} scope={`${scope.property.name}${building?` / ${building.name}`:""}`} icon={DoorOpen} action={buildingId?<BackButton href={`/properties/${scope.propertyId}/units`} label={t("changeBuilding")}/>:undefined}/>
 <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
 <SummaryCard title={building?m("selectedUnits"):t("numberOfUnits")} value={buildingId&&!building?"—":formatNumber(building?.unitCount??scope.property.unitCount,locale)} note={building?m("selectedBuildingUnits"):m("selectedPropertyUnits")} icon={DoorOpen}/>
 <SummaryCard title={t("numberOfFloors")} value={(building?.floorCount??(scope.property.type==="APARTMENT"?scope.property.floorCount:null))==null?"—":formatNumber((building?.floorCount??scope.property.floorCount)!,locale)} note={building||scope.property.type==="APARTMENT"?m("floorMetadata"):m("selectBuildingForFloors")} icon={Layers}/>
 <SummaryCard title={m("propertyType")} value={<span className="text-xl">{t(propertyTypeNames[scope.property.type])}</span>} note={`${scope.property.city} / ${scope.property.district}`} icon={MapPin}/>
 </div></>:<div className="mb-5"><p className="text-xs font-semibold text-primary wrap-anywhere">{scope.property.name}</p><h1 className="mt-3 text-3xl font-bold">{title}</h1><p className="text-sm text-muted-foreground">{t("buildingsPageDescription")}</p></div>}
 {(unitFilter?.invalid||(unitFilter?.unitId&&scope.property.type!=="APARTMENT"&&!buildingId))?<MessageBanner action={<Button asChild variant="outline"><Link href={`/properties/${scope.propertyId}${buildingId?`/buildings/${buildingId}`:""}/units`}>{t("clearUnitFilter")}</Link></Button>}>{t("invalidUnitFilter")}</MessageBanner>:error?<MessageBanner className="mt-5">{error}</MessageBanner>:section==="buildings"?<HierarchyRows kind="building" propertyId={scope.property.id} parentEnabled={scope.property.isEnabled} onChanged={scope.refresh}/>:scope.property.type==="APARTMENT"?<HierarchyRows kind="unit" unitId={unitFilter?.unitId} propertyId={scope.property.id} parentEnabled={scope.property.isEnabled} onChanged={scope.refresh}/>:buildingId?building?<HierarchyRows key={building.id} kind="unit" unitId={unitFilter?.unitId} propertyId={scope.property.id} buildingId={building.id} parentEnabled={scope.property.isEnabled&&building.isEnabled} onChanged={scope.refresh}/>:<Skeleton className="mt-6 h-24"/>:<BuildingSelection propertyId={scope.property.id}/>}
 </div>;
}
function BuildingSelection({propertyId}:{propertyId:string}){
 const t=useTranslations("ui");const [term,setTerm]=useState("");const search=useSearch(term);const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);const rows=useRows<Building>(`/api/v1/properties/${propertyId}/buildings?${new URLSearchParams({page:String(page),pageSize:"10",search,sortBy:"name",sortDirection:"asc"})}`,revision);
 useEffect(()=>setPage(1),[search]);
 const link=(value:Building)=><Button asChild variant="view" size="sm"><Link href={`/properties/${propertyId}/buildings/${value.id}/units`}><ArrowUpRight/>{t("openUnits")}</Link></Button>;
 return <section className="mt-6 overflow-hidden rounded-2xl border bg-card"><div className="space-y-4 p-5"><h2 className="text-base font-semibold">{t("selectBuilding")}</h2><Input aria-label={t("searchBuilding")} placeholder={t("searchBuilding")} value={term} maxLength={200} onChange={event=>setTerm(event.target.value)}/></div><RecordList compact {...rows} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} columns={[{accessorKey:"name",header:t("buildingBlockName")},{id:"count",header:t("numberOfUnits"),cell:({row})=>row.original.unitCount},{id:"actions",header:t("actions"),cell:({row})=>link(row.original)}]} card={value=><div className="flex flex-wrap items-center justify-between gap-3"><strong className="wrap-anywhere">{value.name}</strong>{link(value)}</div>} empty={<><Building2 className="mx-auto mb-3 size-8"/><p>{t("noBuildingsYet")}</p><Button asChild variant="link"><Link href={`/properties/${propertyId}/buildings`}>{t("openBuildings")}</Link></Button></>}/></section>;
}
