"use client";
import {useCallback,useState} from "react";
import {useTranslations} from "next-intl";
import {useRows} from "@/features/people/useRows";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {Choices} from "./Choices";
import {pageOf,property,workspace,unitChoice,type PropertyChoice,type UnitChoice,type Workspace} from "./model";
export type UnitScope={property:PropertyChoice;building:Workspace|null;unit:UnitChoice};
// Area → optional building → unit; an apartment has no buildings, so its units are listed directly (no fake block).
export function UnitPicker({tenant,value,onChange,disabled=false}:{tenant:string;value:UnitScope|null;onChange:(v:UnitScope|null)=>void;disabled?:boolean}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui");const [scope,setScope]=useState<PropertyChoice|null>(value?.property??null),[building,setBuilding]=useState<Workspace|null>(value?.building??null);
 const root=scope?`/api/v2/tenants/${tenant}/properties/${scope.id}`:"";const decode=useCallback((v:unknown)=>pageOf(v,workspace),[]);
 const blocks=useRows<Workspace>(root+"/person-building-choices?page=1&pageSize=1",0,!!scope,decode);const hasBlocks=(blocks.data?.totalCount??0)>0;
 const unitsReady=!!scope&&!!blocks.data&&(!hasBlocks||!!building);
 return <div className="space-y-4"><Choices path={`/api/v2/tenants/${tenant}/person-properties`} id="center-property" label={t("property")} parse={property} filter={p=>p.canWriteRelations} disabled={disabled} value={scope?.id??""} onChange={v=>{setScope(v);setBuilding(null);onChange(null);}}/>
  {scope&&blocks.loading&&<p role="status" className="text-sm text-muted-foreground">{ui("loading")}</p>}{blocks.error&&<MessageBanner>{blocks.error}</MessageBanner>}
  {scope&&hasBlocks&&<Choices key={scope.id+":b"} path={root+"/person-building-choices"} id="center-building" label={t("building")} parse={workspace} disabled={disabled} value={building?.id??""} onChange={v=>{setBuilding(v);onChange(null);}}/>}
  {unitsReady&&<Choices key={(scope?.id??"")+":"+(building?.id??"")} path={root+"/person-unit-choices"} query={building?{buildingId:building.id}:undefined} id="center-unit" label={ui("unit")} parse={unitChoice} disabled={disabled} value={value?.unit.id??""} onChange={v=>onChange(v&&scope?{property:scope,building,unit:v}:null)}/>}</div>;
}
