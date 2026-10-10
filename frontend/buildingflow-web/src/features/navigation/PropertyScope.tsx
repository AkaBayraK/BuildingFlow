"use client";
import {createContext,useContext,useEffect,useState,useCallback,type ReactNode} from "react";
import {usePathname} from "next/navigation";
import {useLocale} from "next-intl";
import {useAuthorizedApi,errorMessage,type Property} from "@/services/api";
import {propertyIdFromPath} from "./routes";
type Scope={propertyId:string|null;property:Property|null;loading:boolean;error:string;refresh:()=>void};
const Context=createContext<Scope|null>(null);
export function PropertyScopeProvider({children}:{children:ReactNode}){
 const propertyId=propertyIdFromPath(usePathname());const api=useAuthorizedApi();const locale=useLocale();const [property,setProperty]=useState<Property|null>(null);const [loading,setLoading]=useState(false);const [error,setError]=useState("");const [revision,setRevision]=useState(0);
 const refresh=useCallback(()=>setRevision(value=>value+1),[]);
 useEffect(()=>{const controller=new AbortController();setProperty(null);setError("");setLoading(!!propertyId);
  if(propertyId)void api<Property>(`/api/v1/properties/${propertyId}`,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setProperty(value);}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
  return()=>controller.abort();
 },[api,propertyId,revision,locale]);
 return <Context.Provider value={{propertyId,property:property?.id===propertyId?property:null,loading,error,refresh}}>{children}</Context.Provider>;
}
export function usePropertyScope(){const value=useContext(Context);if(!value)throw new Error("Property scope required");return value;}
