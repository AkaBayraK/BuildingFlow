"use client";
import {useEffect,useState} from "react";
import {useLocale} from "next-intl";
import {useAuthorizedApi} from "@/services/api";
import {resolvedStyle} from "./definition-style";
import {definitionOptions,overrideName,propertyDefinitionsPath,readPropertyDefinitions,type DefinitionKey,type PropertyDefinitions} from "./model";

type Style={icon:import("lucide-react").LucideIcon;tone:string};
// Kısa ömürlü paylaşım: aynı ekrandaki formlar tek istek yapar; Tanımlar değişikliği bir dakika içinde görünür.
const cache=new Map<string,{at:number;value:Promise<PropertyDefinitions>}>();
const ttl=60_000;
export function clearDefinitionCache(property?:string){if(property)cache.delete(property);else cache.clear();}

export function useDefinitionOptions<T extends string>(propertyId:string|null|undefined,key:DefinitionKey,codes:readonly T[],current?:string|null){
 const api=useAuthorizedApi(),locale=useLocale();
 const [data,setData]=useState<PropertyDefinitions|null>(null);
 useEffect(()=>{
  if(!propertyId)return;let live=true;
  const hit=cache.get(propertyId);
  const value=hit&&Date.now()-hit.at<ttl?hit.value:api<unknown>(propertyDefinitionsPath(propertyId)).then(v=>readPropertyDefinitions(v,propertyId));
  if(value!==hit?.value)cache.set(propertyId,{at:Date.now(),value});
  value.then(v=>{if(live)setData(v);},()=>{cache.delete(propertyId);if(live)setData(null);});
  return()=>{live=false;};
 },[api,propertyId]);
 const list=data?.lists.find(l=>l.key===key);
 const value=(code:string)=>list?.values.find(v=>v.code===code);
 return {
  options:definitionOptions(codes,list,current),
  label:(code:string,fallback:string)=>{const v=value(code);return (v&&overrideName(v,locale))??fallback;},
  style:<S extends Style>(code:string,base:S):S=>{const v=value(code);if(!v||!v.iconKey&&!v.tone)return base;const r=resolvedStyle(key,v);return {...base,icon:v.iconKey?r.icon:base.icon,tone:v.tone??base.tone};},
 };
}
