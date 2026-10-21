"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Building2,DoorOpen,UsersRound,ShieldCheck,ImagePlus,RefreshCw,Upload} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage,type Property} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {readDocument,verifyDownload} from "@/features/communication/model";
import {bannerName,latestBanner,prepareBanner} from "./banner";
import type {DashboardSummary} from "./model";
import {WeatherLocation} from "./WeatherLocation";
const fallback="/images/dashboard-residence.webp";
export function DashboardHero({property,data,loading,onRefresh}:{property:Property|null;data:DashboardSummary|null;loading:boolean;onRefresh:()=>void}){
 const t=useTranslations("dashboard"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity(),notice=useNotification();
 const [now,setNow]=useState<Date|null>(null),[background,setBackground]=useState<{id:string;url:string}|null>(null),[revision,setRevision]=useState(0),[open,setOpen]=useState(false),[file,setFile]=useState<File|null>(null),[busy,setBusy]=useState(false),[invalid,setInvalid]=useState("");
 const request=useRef("");const lock=useRef(false);const alive=useRef(true);const id=property?.id;const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r));
 useEffect(()=>{alive.current=true;return()=>{alive.current=false;};},[]);
 useEffect(()=>{const tick=()=>setNow(new Date());tick();const timer=window.setInterval(tick,1000);return()=>window.clearInterval(timer);},[]);
 useEffect(()=>{setBackground(null);if(!id)return;const c=new AbortController();let url:string|null=null;
  void api<unknown>(`/api/v1/properties/${id}/documents?page=1&pageSize=100&search=${bannerName}`,{signal:c.signal}).then(async value=>{
   const record=latestBanner(value,id);if(!record)return;
   const blob=await api<Blob>(`/api/v1/properties/${id}/documents/${record.id}/download`,{signal:c.signal},"blob");
   if(!await verifyDownload(blob,record))throw new Error("Invalid banner download");if(c.signal.aborted)return;
   url=URL.createObjectURL(blob);setBackground({id,url});
  }).catch(()=>{if(!c.signal.aborted)setBackground(null);});
  return()=>{c.abort();if(url)URL.revokeObjectURL(url);};
 },[api,id,revision]);
 async function picked(value:File|null){setInvalid("");setFile(null);request.current="";if(!value)return;lock.current=true;setBusy(true);try{const result=await prepareBanner(value);if(alive.current){setFile(result);request.current=crypto.randomUUID();}}catch{if(alive.current)setInvalid(t("bannerInvalid"));}finally{lock.current=false;if(alive.current)setBusy(false);}}
 async function save(){if(!id||!file||lock.current||!manager)return;lock.current=true;setBusy(true);setInvalid("");const form=new FormData();form.set("file",file);form.set("requestId",request.current);form.set("audience","ALL_MEMBERS");
  try{readDocument(await api<unknown>(`/api/v1/properties/${id}/documents`,{method:"POST",body:form}),id);if(alive.current){setRevision(n=>n+1);setOpen(false);setFile(null);request.current="";notice.success(t("bannerSaved"));}}
  catch(e){if(alive.current)setInvalid(errorMessage(e,locale));}finally{lock.current=false;if(alive.current)setBusy(false);}
 }
 const zone={timeZone:"Europe/Istanbul"};const date=now?new Intl.DateTimeFormat(locale==="tr"?"tr-TR":"en-GB",{...zone,day:"numeric",month:"long",year:"numeric",weekday:"long"}).format(now):"—";const time=now?new Intl.DateTimeFormat(locale==="tr"?"tr-TR":"en-GB",{...zone,hour:"2-digit",minute:"2-digit",hour12:false}).format(now):"—";
 return <><section className="dashboard-photo-hero" aria-label={t("heroTitle")}>
  <img className="dashboard-hero-photo" src={background&&background.id===id?background.url:fallback} alt="" onError={()=>setBackground(null)}/><div className="dashboard-photo-shade"/>
  <div className="dashboard-photo-content"><div className="dashboard-photo-main"><p className="dashboard-area-name"><Building2 aria-hidden="true"/>{property?.name??ui("selectProperty")}</p><h1>Building<span>Flow</span></h1><p className="dashboard-photo-tagline">{t("heroTagline")}</p>
   <div className="dashboard-hero-chips"><span><Building2 aria-hidden="true"/>{t("heroBuildings",{count:property?.buildingCount??0})}</span><span><DoorOpen aria-hidden="true"/>{t("heroUnits",{count:data?.unitCount??property?.unitCount??0})}</span><span><UsersRound aria-hidden="true"/>{data?t("heroOccupied",{count:data.occupiedUnitCount}):t("loading")}</span><span><ShieldCheck aria-hidden="true"/>{ui(manager?"shellManager":"shellAccountant")}</span></div>
  </div><aside className="dashboard-clock-panel" aria-label={t("clockLabel")}><time dateTime={now?.toISOString()}><span>{date}</span><strong>{time}</strong></time>{(manager||identity.hasRealmRole("ACCOUNTANT"))&&<WeatherLocation key={identity.subject??"current"} subject={identity.subject??"current"}/>}</aside></div>
  <div className="dashboard-hero-tools"><button type="button" disabled={loading||!id} onClick={()=>{setRevision(n=>n+1);onRefresh();}} aria-label={ui("refresh")} title={ui("refresh")}><RefreshCw size={16}/></button>{manager&&id&&<button type="button" onClick={()=>{setInvalid("");setOpen(true);}} aria-label={t("changeBanner")} title={t("changeBanner")}><ImagePlus size={16}/></button>}</div>
 </section><Dialog open={open} onOpenChange={value=>{if(!busy)setOpen(value);}}><DialogContent><DialogHeader><DialogTitle>{t("changeBanner")}</DialogTitle><DialogDescription>{t("bannerShared")}</DialogDescription></DialogHeader><label className="space-y-2"><span className="block text-sm font-medium">{t("bannerFile")}</span><input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={e=>void picked(e.target.files?.[0]??null)} aria-invalid={!!invalid}/></label><p className="text-xs text-muted-foreground">{t("bannerHelp")}</p>{file&&<p className="text-sm" role="status">{t("bannerReady")}</p>}{invalid&&<p className="text-sm text-destructive" role="alert">{invalid}</p>}<DialogFooter><Button variant="outline" disabled={busy} onClick={()=>setOpen(false)}>{ui("cancel")}</Button><Button disabled={busy||!file} onClick={()=>void save()}><Upload/>{busy?ui("saving"):ui("save")}</Button></DialogFooter></DialogContent></Dialog></>;
}
