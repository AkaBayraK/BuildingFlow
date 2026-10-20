"use client";
import {createContext,useContext,useEffect,useState,type ReactNode} from "react";
import {usePathname} from "next/navigation";
import {useLocale,useTranslations} from "next-intl";
import {Building2} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage,type Property} from "@/services/api";
import {dashboardAreas,dashboardRoles,selectedDashboardArea} from "./area-selection";
type Area={allowed:boolean;properties:Property[]|null;propertyId:string;listError:string;choose:(id:string)=>void;reload:()=>void};
const Context=createContext<Area|null>(null);
export function DashboardAreaProvider({children}:{children:ReactNode}){const identity=useIdentity();return <AreaState key={identity.subject??""}>{children}</AreaState>;}
function AreaState({children}:{children:ReactNode}){
 const identity=useIdentity(),api=useAuthorizedApi(),locale=useLocale(),path=usePathname();const allowed=dashboardRoles.some(r=>identity.hasRealmRole(r));
 const active=path==="/dashboard";const [properties,setProperties]=useState<Property[]|null>(null),[propertyId,setPropertyId]=useState(""),[listError,setError]=useState(""),[revision,setRevision]=useState(0);
 useEffect(()=>{if(!active||!allowed){setProperties(null);setPropertyId("");return;}const c=new AbortController();setProperties(null);setPropertyId("");setError("");
  void dashboardAreas(api,c.signal).then(items=>{if(!c.signal.aborted){setProperties(items);setPropertyId(selectedDashboardArea(items,new URLSearchParams(window.location.search).get("property")));}}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();
 },[api,active,allowed,locale,revision]);
 useEffect(()=>{if(!properties||!active)return;const sync=()=>setPropertyId(selectedDashboardArea(properties,new URLSearchParams(window.location.search).get("property")));window.addEventListener("popstate",sync);return()=>window.removeEventListener("popstate",sync);},[properties,active]);
 function choose(id:string){if(!properties?.some(p=>p.id===id&&p.isEnabled)||id===propertyId)return;setPropertyId(id);const url=new URL(window.location.href);url.searchParams.set("property",id);window.history.pushState(null,"",url);}
 return <Context.Provider value={{allowed,properties,propertyId,listError,choose,reload:()=>setRevision(n=>n+1)}}>{children}</Context.Provider>;
}
export function useDashboardArea(){const area=useContext(Context);if(!area)throw new Error("Dashboard area provider required");return area;}
export function DashboardAreaPicker(){const t=useTranslations("dashboard"),ui=useTranslations("ui"),path=usePathname(),area=useDashboardArea();if(path!=="/dashboard"||!area.allowed)return null;
 return <label className="dashboard-area-picker"><Building2 size={17} aria-hidden="true"/><span className="sr-only">{t("area")}</span><select aria-label={t("area")} value={area.propertyId} disabled={!area.properties?.length} onChange={e=>area.choose(e.target.value)}>{!area.properties?.length&&<option value="">{area.listError?ui("selectProperty"):area.properties?t("noAreaTitle"):ui("loading")}</option>}{area.properties?.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select></label>;
}
