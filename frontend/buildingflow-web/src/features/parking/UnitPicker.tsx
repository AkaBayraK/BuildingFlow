"use client";
import {useId,useState} from "react";
import {useTranslations} from "next-intl";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {useSearch} from "@/features/people/useRows";
import {readUnits,type UnitOption} from "./model";
import {parkingRoot,useParkingRead} from "./useParking";

export const unitLabel=(u:{number:string;buildingName:string|null})=>u.buildingName?`${u.buildingName} · ${u.number}`:u.number;

/** Server-side unit search (max 50) for forms; no personal data is returned by this endpoint. */
export function UnitPicker({propertyId,value,onChange,required,disabled}:{propertyId:string;value:string;onChange:(unit:UnitOption|null)=>void;required?:boolean;disabled?:boolean}){
 const t=useTranslations("parking"),id=useId();
 const [term,setTerm]=useState(""),search=useSearch(term);
 const units=useParkingRead(`${parkingRoot(propertyId)}/units${search?`?search=${encodeURIComponent(search)}`:""}`,readUnits,0);
 return <div className="grid gap-2 sm:grid-cols-[1fr_1.4fr]">
  <div><Label htmlFor={`${id}-search`}>{t("unitSearch")}</Label><Input id={`${id}-search`} value={term} maxLength={50} disabled={disabled} placeholder={t("unitSearchHint")} onChange={e=>setTerm(e.target.value)}/></div>
  <div><Label htmlFor={`${id}-unit`}>{t("unit")}{required&&" *"}</Label>
   <select id={`${id}-unit`} className={toolbarSelectClass+" w-full"} disabled={disabled||units.loading} value={value} aria-busy={units.loading} onChange={e=>onChange(units.data?.find(u=>u.id===e.target.value)??null)}>
    <option value="">{units.loading?t("loading"):units.error?t("loadFailed"):t("chooseUnit")}</option>
    {units.data?.map(u=><option key={u.id} value={u.id}>{unitLabel(u)}</option>)}
   </select></div>
 </div>;
}
