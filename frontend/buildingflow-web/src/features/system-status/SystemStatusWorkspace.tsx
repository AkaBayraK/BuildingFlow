"use client";
import {useCallback,useEffect,useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Activity,ChevronDown,Clock3,Database,ExternalLink,Info,Layers3,RefreshCw,Search,Server,Settings,Wifi} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Badge} from "@/components/ui/badge";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {WorkspaceHeading} from "@/components/management/WorkspacePresentation";
import {cn} from "@/lib/utils";
import {componentStatuses,componentTypes,filterComponents,parseSystemStatus,statusColor,statusDot,statusText,systemStatusPath,systemStatusRoles,upCount,type ComponentStatus,type ComponentType,type SystemStatus} from "./model";

const typeIcons={SERVICE:Server,DATABASE:Database,INFRASTRUCTURE:Layers3,EXTERNAL_SERVICE:ExternalLink} as const;
const tones={emerald:"bg-emerald-100 text-emerald-600 dark:bg-emerald-950/40",blue:"bg-blue-100 text-blue-600 dark:bg-blue-950/40",amber:"bg-amber-100 text-amber-600 dark:bg-amber-950/40"} as const;
const panel="min-w-0 rounded-2xl border bg-card shadow-sm";
const selectClass="h-9 appearance-none rounded-lg border border-input bg-background px-3 pr-8 text-sm text-foreground";

// Yönetim > Sistem Ayarları: TraceFlow "Sistem Durumu" ekranının BuildingFlow karşılığı.
export function SystemStatusWorkspace(){
 const t=useTranslations("systemStatus"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const allowed=systemStatusRoles.some(role=>identity.hasRealmRole(role));
 const [status,setStatus]=useState<SystemStatus|null>(null),[loading,setLoading]=useState(true),[refreshing,setRefreshing]=useState(false),[error,setError]=useState("");
 const [typeFilter,setTypeFilter]=useState<"ALL"|ComponentType>("ALL"),[statusFilter,setStatusFilter]=useState<"ALL"|ComponentStatus>("ALL"),[query,setQuery]=useState("");
 const [refreshSeconds,setRefreshSeconds]=useState("30"),[showAll,setShowAll]=useState(false);

 const load=useCallback(async(signal?:AbortSignal,manual=false)=>{
  if(manual)setRefreshing(true);
  try{const value=parseSystemStatus(await api<unknown>(systemStatusPath,{signal}));if(!signal?.aborted){setStatus(value);setError("");}}
  catch(e){if(!signal?.aborted)setError(errorMessage(e,locale));}
  finally{if(!signal?.aborted){setLoading(false);setRefreshing(false);}}
 },[api,locale]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();void load(c.signal);return()=>c.abort();},[allowed,load]);
 useEffect(()=>{const seconds=Number(refreshSeconds);if(!allowed||!(seconds>0))return;const timer=window.setInterval(()=>{void load();},seconds*1000);return()=>window.clearInterval(timer);},[allowed,load,refreshSeconds]);

 const components=useMemo(()=>status?.components??[],[status]);
 const filtered=useMemo(()=>filterComponents(components,typeFilter,statusFilter,query,locale),[components,typeFilter,statusFilter,query,locale]);
 const recent=useMemo(()=>components.slice().sort((a,b)=>new Date(b.checkedAt).getTime()-new Date(a.checkedAt).getTime()),[components]);
 const date=(value?:string)=>value?new Intl.DateTimeFormat(locale,{dateStyle:"short",timeStyle:"medium"}).format(new Date(value)):"—";
 const uptime=(seconds?:number)=>{if(seconds===undefined)return "—";const days=Math.floor(seconds/86400),hours=Math.floor(seconds%86400/3600),minutes=Math.floor(seconds%3600/60);return days>0?t("uptimeDays",{days,hours}):t("uptimeHours",{hours,minutes});};

 if(!allowed)return <div className="py-10"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;

 const summary=[
  {key:"ALL" as const,icon:Activity,tone:"emerald" as const,current:status?.upComponents??0,total:status?.totalComponents??0},
  {key:"SERVICE" as const,icon:Server,tone:"blue" as const,...upCount(components,"SERVICE")},
  {key:"DATABASE" as const,icon:Database,tone:"emerald" as const,...upCount(components,"DATABASE")},
  {key:"INFRASTRUCTURE" as const,icon:Layers3,tone:"blue" as const,...upCount(components,"INFRASTRUCTURE")},
  {key:"EXTERNAL_SERVICE" as const,icon:Wifi,tone:"amber" as const,...upCount(components,"EXTERNAL_SERVICE")}
 ];
 const distribution=componentStatuses.map(value=>({status:value,count:components.filter(c=>c.status===value).length}));
 const total=distribution.reduce((sum,item)=>sum+item.count,0);
 let angle=0;const gradient=distribution.map(item=>{const start=angle;angle+=total?item.count/total*360:0;return `${statusColor[item.status]} ${start}deg ${angle}deg`;}).join(", ");

 return <div className="flex min-w-0 flex-col gap-4 py-8">
  <WorkspaceHeading title={t("title")} description={t("subtitle")} scope={t("scope")} icon={Settings} action={<div className="flex flex-wrap items-center gap-2">
   <Button variant="view" onClick={()=>void load(undefined,true)} disabled={refreshing}><RefreshCw className={refreshing?"animate-spin":undefined}/>{t("refresh")}</Button>
   <label className="flex items-center gap-2 text-sm text-primary-foreground/75">{t("autoRefresh")}
    <select className="h-9 rounded-lg border border-input bg-background px-3 text-sm text-foreground" value={refreshSeconds} onChange={e=>setRefreshSeconds(e.target.value)}>
     {["15","30","60"].map(v=><option key={v} value={v}>{t("seconds",{count:Number(v)})}</option>)}<option value="0">{t("off")}</option>
    </select></label>
  </div>}/>
  <p className="flex items-center gap-1 text-xs text-muted-foreground"><Clock3 className="size-3.5" aria-hidden="true"/>{t("lastCheck")}: {date(status?.lastCheckedAt)}</p>
  {error&&<MessageBanner>{error}</MessageBanner>}

  <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-5">
   {summary.map(card=>{const Icon=card.icon,percent=card.total?Math.round(card.current/card.total*100):0;
    return <div key={card.key} className={cn(panel,"flex flex-col gap-3 p-4",card.key==="ALL"&&"border-emerald-200 bg-emerald-50/60 dark:border-emerald-900 dark:bg-emerald-950/20")}>
     <div className="flex items-center justify-between"><span className="text-sm font-medium">{t(`summary_${card.key}`)}</span><span className={cn("rounded-full p-2",tones[card.tone])}><Icon className="size-5" aria-hidden="true"/></span></div>
     <div className="flex items-end justify-between gap-2"><span className="text-2xl font-semibold tabular-nums">{loading?<Skeleton className="h-8 w-20"/>:`${card.current} / ${card.total}`}</span><span className="text-xs text-muted-foreground">{card.key==="ALL"?t(`overall_${status?.overallStatus??"UNKNOWN"}`):t("running")}</span></div>
     <div className="flex items-center gap-2"><div className="h-1.5 flex-1 overflow-hidden rounded-full bg-muted"><div className={cn("h-full rounded-full",percent===100?"bg-emerald-500":percent>0?"bg-amber-500":"bg-red-500")} style={{width:`${percent}%`}}/></div><span className="text-xs tabular-nums">%{percent}</span></div>
    </div>;})}
  </div>

  <div className="grid min-w-0 gap-3 xl:grid-cols-5">
   <section className={cn(panel,"space-y-3 p-4 xl:col-span-4")} aria-label={t("components")}>
    <div className="flex flex-wrap items-center gap-2">
     {(["ALL",...componentTypes] as const).map(value=>{const Icon=value==="ALL"?Activity:typeIcons[value];return <Button key={value} size="sm" variant={typeFilter===value?"default":"outline"} aria-pressed={typeFilter===value} onClick={()=>setTypeFilter(value)}><Icon aria-hidden="true"/>{t(`filter_${value}`)}</Button>;})}
     <div className="ml-auto flex flex-wrap items-center gap-2">
      <div className="relative"><Search className="pointer-events-none absolute top-2.5 left-2.5 size-4 text-muted-foreground" aria-hidden="true"/><Input className="h-9 w-48 pl-8" type="search" aria-label={t("search")} value={query} onChange={e=>setQuery(e.target.value)} placeholder={t("search")}/></div>
      <div className="relative"><select aria-label={t("statusFilter")} className={selectClass} value={statusFilter} onChange={e=>setStatusFilter(e.target.value as "ALL"|ComponentStatus)}><option value="ALL">{t("allStatuses")}</option>{componentStatuses.map(v=><option key={v} value={v}>{t(`status_${v}`)}</option>)}</select><ChevronDown className="pointer-events-none absolute top-2.5 right-2 size-4 text-muted-foreground" aria-hidden="true"/></div>
     </div>
    </div>
    <div className="overflow-x-auto rounded-lg border"><table className="w-full min-w-[760px] text-sm">
     <thead className="bg-muted/50 text-left text-xs text-muted-foreground"><tr>{["name","type","status","response","checkedAt","description"].map(k=><th key={k} scope="col" className="px-3 py-2 font-medium">{t(`table_${k}`)}</th>)}</tr></thead>
     <tbody>
      {loading&&<tr><td colSpan={6} className="p-3"><Skeleton className="h-10 w-full"/></td></tr>}
      {!loading&&filtered.length===0&&<tr><td colSpan={6} className="py-10 text-center text-muted-foreground">{t("empty")}</td></tr>}
      {!loading&&filtered.map(c=>{const Icon=typeIcons[c.type];return <tr key={c.id} className="border-t">
       <td className="px-3 py-2"><div className="flex items-center gap-2 font-medium"><Icon className="size-4 text-muted-foreground" aria-hidden="true"/>{c.name}</div></td>
       <td className="px-3 py-2"><Badge variant="secondary">{t(`filter_${c.type}`)}</Badge></td>
       <td className="px-3 py-2"><span className={cn("flex items-center gap-1.5 font-medium",statusText[c.status])}><span className={cn("size-2 rounded-full",statusDot[c.status])} aria-hidden="true"/>{t(`status_${c.status}`)}</span></td>
       <td className="px-3 py-2 tabular-nums">{c.responseTimeMs===null?"—":`${c.responseTimeMs} ms`}</td>
       <td className="px-3 py-2 text-xs whitespace-nowrap text-muted-foreground">{date(c.checkedAt)}</td>
       <td className="max-w-xs px-3 py-2 text-muted-foreground">{c.description}</td>
      </tr>;})}
     </tbody>
    </table></div>
   </section>

   <div className="grid content-start gap-3 xl:col-span-1">
    <section className={cn(panel,"p-4")}><h2 className="mb-3 font-semibold">{t("distribution")}</h2><div className="flex flex-wrap items-center gap-4">
     <div className="relative grid size-32 shrink-0 place-items-center rounded-full" style={{background:total?`conic-gradient(${gradient})`:"#e2e8f0"}} aria-hidden="true"><div className="grid size-20 place-items-center rounded-full bg-card text-center"><span className="text-2xl font-semibold">{total}</span><span className="text-[10px] text-muted-foreground">{t("componentUnit")}</span></div></div>
     <dl className="grid gap-2 text-sm">{distribution.map(item=><div key={item.status} className="flex items-center justify-between gap-4"><dt className="flex items-center gap-2"><span className="size-2.5 rounded-full" style={{backgroundColor:statusColor[item.status]}} aria-hidden="true"/>{t(`status_${item.status}`)}</dt><dd className="font-medium tabular-nums">{item.count}</dd></div>)}</dl>
    </div></section>
    <section className={cn(panel,"p-4")}><div className="mb-3 flex items-center justify-between gap-2"><h2 className="font-semibold">{t("recentChecks")}</h2><Button variant="link" size="sm" onClick={()=>setShowAll(v=>!v)}>{t(showAll?"showRecent":"showAll")}</Button></div>
     <ul className="space-y-3">{(showAll?recent:recent.slice(0,5)).map(c=><li key={c.id} className="flex items-start gap-2 text-sm"><span className={cn("mt-1.5 size-2 shrink-0 rounded-full",statusDot[c.status])} aria-hidden="true"/><div className="min-w-0 flex-1"><p className="truncate font-medium">{c.name}</p><p className="truncate text-xs text-muted-foreground">{c.description}</p></div><span className="text-[11px] whitespace-nowrap text-muted-foreground">{date(c.checkedAt)}</span></li>)}</ul>
    </section>
    <section className={cn(panel,"space-y-3 border-blue-200 bg-blue-50/50 p-4 dark:border-blue-900 dark:bg-blue-950/20")}><h2 className="flex items-center gap-2 font-medium text-blue-700 dark:text-blue-300"><Info className="size-4" aria-hidden="true"/>{t("infoTitle")}</h2>
     <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
      <dt className="text-muted-foreground">{t("infoEnvironment")}</dt><dd className="text-right">{status?.environment||"—"}</dd>
      <dt className="text-muted-foreground">{t("infoVersion")}</dt><dd className="text-right">{status?.version||"—"}</dd>
      <dt className="text-muted-foreground">{t("infoCluster")}</dt><dd className="text-right">{status?.cluster||"—"}</dd>
      <dt className="text-muted-foreground">{t("infoUptime")}</dt><dd className="text-right">{uptime(status?.uptimeSeconds)}</dd>
      <dt className="font-medium">{t("infoTotal")}</dt><dd className="text-right font-semibold">{status?.totalComponents??"—"}</dd>
     </dl>
    </section>
   </div>
  </div>
 </div>;
}
