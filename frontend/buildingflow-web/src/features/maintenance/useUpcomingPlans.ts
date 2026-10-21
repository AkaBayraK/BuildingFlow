"use client";
import {useEffect,useState} from "react";
import {useLocale} from "next-intl";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {readUpcoming,type MaintenanceUpcoming} from "./model";
export const UPCOMING_LIMIT=4;
/** Upcoming open plans for the dashboard; a failure stays local so the rest of the dashboard keeps working. */
export function useUpcomingPlans(propertyId:string|null,revision:number){
 const api=useAuthorizedApi(),locale=useLocale() as "tr"|"en";
 const [data,setData]=useState<MaintenanceUpcoming|null>(null),[error,setError]=useState("");
 useEffect(()=>{if(!propertyId)return;const c=new AbortController();setData(null);setError("");
  void api<unknown>(`/api/v1/properties/${propertyId}/maintenance-plans/upcoming?limit=${UPCOMING_LIMIT}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readUpcoming(v,propertyId,UPCOMING_LIMIT));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});
  return()=>c.abort();},[api,propertyId,revision,locale]);
 return {data,error,loading:!!propertyId&&!data&&!error};
}
