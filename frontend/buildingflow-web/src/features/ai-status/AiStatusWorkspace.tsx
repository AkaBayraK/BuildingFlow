"use client";
import {useCallback,useEffect,useMemo,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {CircleAlert,Clock3,Cpu,DatabaseZap,ExternalLink,Gauge,ListChecks,RefreshCw,ShieldCheck} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Badge} from "@/components/ui/badge";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {BackButton} from "@/components/navigation/BackButton";
import {WorkspaceHeading} from "@/components/management/WorkspacePresentation";
import {cn} from "@/lib/utils";
import {aiComponentTypes,aiStatusHref,aiStatusPath,aiStatusRoles,filterAiComponents,parseAiStatus,type AiComponentType,type AiStatus,type AiStatusComponent} from "./model";
import {aiOverallIcon as OverallIcon,aiStatusDot,aiStatusIcon,aiStatusText,aiTypeIcon} from "./ai-status-style";

const panel="min-w-0 rounded-2xl border bg-card shadow-sm";
const tones={emerald:"bg-emerald-100 text-emerald-600 dark:bg-emerald-950/40",blue:"bg-blue-100 text-blue-600 dark:bg-blue-950/40",violet:"bg-violet-100 text-violet-600 dark:bg-violet-950/40",amber:"bg-amber-100 text-amber-600 dark:bg-amber-950/40"} as const;

// Yönetim > Sistem Ayarları > AI Durumu: TraceFlow "AI Durumu" ekranının BuildingFlow karşılığı.
export function AiStatusWorkspace(){
 const t=useTranslations("aiStatus"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const allowed=aiStatusRoles.some(role=>identity.hasRealmRole(role));
 const [status,setStatus]=useState<AiStatus|null>(null),[loading,setLoading]=useState(true),[refreshing,setRefreshing]=useState(false),[error,setError]=useState("");
 const [filter,setFilter]=useState<"ALL"|AiComponentType>("ALL"),[refreshSeconds,setRefreshSeconds]=useState("30");

 const load=useCallback(async(signal?:AbortSignal,manual=false)=>{
  if(manual)setRefreshing(true);
  try{const value=parseAiStatus(await api<unknown>(aiStatusPath,{signal}));if(!signal?.aborted){setStatus(value);setError("");}}
  catch(e){if(!signal?.aborted)setError(errorMessage(e,locale));}
  finally{if(!signal?.aborted){setLoading(false);setRefreshing(false);}}
 },[api,locale]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();void load(c.signal);return()=>c.abort();},[allowed,load]);
 useEffect(()=>{const seconds=Number(refreshSeconds);if(!allowed||!(seconds>0))return;const timer=window.setInterval(()=>{void load();},seconds*1000);return()=>window.clearInterval(timer);},[allowed,load,refreshSeconds]);

 const components=useMemo(()=>filterAiComponents(status?.components??[],filter),[status,filter]);
 const date=(value?:string)=>value?new Intl.DateTimeFormat(locale,{dateStyle:"short",timeStyle:"medium"}).format(new Date(value)):"—";
 const describe=(c:AiStatusComponent)=>t(`detail_${c.detail}`,{count:c.count??0});

 if(!allowed)return <div className="py-10"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;
 const overall=status?.overallStatus??"UNKNOWN";
 const model=status?.mode==="AKAAI"?status.model??t("defaultModel"):t("rulesModel");

 return <div className="flex min-w-0 flex-col gap-4 py-8">
  <BackButton href="/system-settings" label={t("back")}/>
  <WorkspaceHeading title={t("title")} description={t("subtitle")} scope={t("scope")} icon={Gauge} action={<div className="flex flex-wrap items-center gap-2">
   <Button variant="view" onClick={()=>void load(undefined,true)} disabled={refreshing}><RefreshCw className={refreshing?"animate-spin":undefined}/>{t("refresh")}</Button>
   {status?.consoleUrl&&<Button asChild variant="view"><a href={status.consoleUrl} target="_blank" rel="noopener noreferrer"><ExternalLink aria-hidden="true"/>{t("manageInAkaAi")}</a></Button>}
   <label className="flex items-center gap-2 text-sm text-primary-foreground/75">{t("autoRefresh")}
    <select className="h-9 rounded-lg border border-input bg-background px-3 text-sm text-foreground" value={refreshSeconds} onChange={e=>setRefreshSeconds(e.target.value)}>
     {["15","30","60"].map(v=><option key={v} value={v}>{t("seconds",{count:Number(v)})}</option>)}<option value="0">{t("off")}</option>
    </select></label>
  </div>}/>
  <p className="flex items-center gap-1 text-xs text-muted-foreground"><Clock3 className="size-3.5" aria-hidden="true"/>{t("lastCheck")}: {date(status?.lastCheckedAt)}</p>
  {error&&<MessageBanner>{error}</MessageBanner>}
  {status?.locality==="EXTERNAL"&&<div role="alert" className="flex items-start gap-2 rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-200"><CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true"/>{t("externalWarning")}</div>}
  {status?.mode==="RULES"&&<div className="flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50/60 p-3 text-sm text-blue-900 dark:border-blue-900 dark:bg-blue-950/20 dark:text-blue-200"><CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true"/>{t("rulesNotice")}</div>}

  <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
   <div className={cn(panel,"flex flex-col gap-3 border-emerald-200 bg-emerald-50/60 p-4 dark:border-emerald-900 dark:bg-emerald-950/20")}>
    <div className="flex items-center justify-between"><span className="text-sm font-medium">{t("summaryOverall")}</span><span className={cn("rounded-full p-2",tones.emerald)}><OverallIcon className="size-5" aria-hidden="true"/></span></div>
    <div className={cn("flex items-center gap-2 text-2xl font-semibold",aiStatusText[overall])}>{loading?<Skeleton className="h-8 w-24"/>:<><span className={cn("size-3 rounded-full",aiStatusDot[overall])} aria-hidden="true"/>{t(`status_${overall}`)}</>}</div>
    <p className="text-xs text-muted-foreground">{t("componentCount",{up:status?.upComponents??0,total:status?.totalComponents??0})}</p>
   </div>
   <div className={cn(panel,"flex flex-col gap-3 p-4")}>
    <div className="flex items-center justify-between"><span className="text-sm font-medium">{t("summaryModel")}</span><span className={cn("rounded-full p-2",tones.blue)}><Cpu className="size-5" aria-hidden="true"/></span></div>
    <span className="truncate text-2xl font-semibold">{loading?<Skeleton className="h-8 w-28"/>:model}</span>
    <p className="truncate text-xs text-muted-foreground">{t(`mode_${status?.mode??"RULES"}`)}{status?.modelProvider?` › ${status.modelProvider}`:""} · {status?.endpoint??"-"}</p>
   </div>
   <div className={cn(panel,"flex flex-col gap-3 p-4")}>
    <div className="flex items-center justify-between"><span className="text-sm font-medium">{t("summaryPrompts")}</span><span className={cn("rounded-full p-2",tones.violet)}><ListChecks className="size-5" aria-hidden="true"/></span></div>
    <span className="text-2xl font-semibold tabular-nums">{loading?<Skeleton className="h-8 w-20"/>:status?.activePromptCount??0}</span>
    <p className="text-xs text-muted-foreground">{t("activePrompts")}</p>
   </div>
   <div className={cn(panel,"flex flex-col gap-3 p-4")}>
    <div className="flex items-center justify-between"><span className="text-sm font-medium">{t("summarySafety")}</span><span className={cn("rounded-full p-2",tones.amber)}><ShieldCheck className="size-5" aria-hidden="true"/></span></div>
    <span className="text-2xl font-semibold">{t("readOnly")}</span>
    <p className="text-xs text-muted-foreground">{t("noSecrets")}</p>
   </div>
  </div>

  <div className="grid min-w-0 gap-3 xl:grid-cols-4">
   <section className={cn(panel,"space-y-3 p-4 xl:col-span-3")} aria-label={t("components")}>
    <h2 className="font-semibold">{t("components")}</h2>
    <div className="flex flex-wrap gap-2">
     {(["ALL",...aiComponentTypes] as const).map(value=>{const Icon=aiTypeIcon[value];return <Button key={value} size="sm" variant={filter===value?"default":"outline"} aria-pressed={filter===value} onClick={()=>setFilter(value)}><Icon aria-hidden="true"/>{t(`filter_${value}`)}</Button>;})}
    </div>
    <div className="overflow-x-auto rounded-lg border"><table className="w-full min-w-[720px] text-sm">
     <thead className="bg-muted/50 text-left text-xs text-muted-foreground"><tr>{["name","type","status","response","checkedAt"].map(k=><th key={k} scope="col" className="px-3 py-2 font-medium">{t(`table_${k}`)}</th>)}</tr></thead>
     <tbody>
      {loading&&<tr><td colSpan={5} className="p-3"><Skeleton className="h-10 w-full"/></td></tr>}
      {!loading&&components.length===0&&<tr><td colSpan={5} className="py-10 text-center text-muted-foreground">{t("empty")}</td></tr>}
      {!loading&&components.map(c=>{const Icon=aiTypeIcon[c.type],StatusIcon=aiStatusIcon[c.status];return <tr key={c.id} className="border-t">
       <td className="px-3 py-2"><div className="flex items-center gap-3"><span className="grid size-9 shrink-0 place-items-center rounded-lg bg-muted"><Icon className="size-5 text-muted-foreground" aria-hidden="true"/></span><div className="min-w-0"><p className="font-medium">{t(`component_${c.id}`)}</p><p className="text-xs text-muted-foreground">{describe(c)}</p></div></div></td>
       <td className="px-3 py-2"><Badge variant="secondary">{t(`filter_${c.type}`)}</Badge></td>
       <td className="px-3 py-2"><span className={cn("flex items-center gap-1.5 font-medium",aiStatusText[c.status])}><StatusIcon className="size-4" aria-hidden="true"/>{t(`status_${c.status}`)}</span></td>
       <td className="px-3 py-2 tabular-nums text-muted-foreground">{c.responseTimeMs===null?"—":`${c.responseTimeMs} ms`}</td>
       <td className="px-3 py-2 text-xs whitespace-nowrap text-muted-foreground">{date(c.checkedAt)}</td>
      </tr>;})}
     </tbody>
    </table></div>
   </section>

   <div className="grid content-start gap-3 xl:col-span-1">
    <section className={cn(panel,"p-4")}><h2 className="mb-3 font-semibold">{t("recentChecks")}</h2>
     <ul className="space-y-3">{(status?.components??[]).map(c=><li key={c.id} className="flex items-start gap-2 text-sm"><span className={cn("mt-1.5 size-2 shrink-0 rounded-full",aiStatusDot[c.status])} aria-hidden="true"/><div className="min-w-0 flex-1"><p className="truncate font-medium">{t(`component_${c.id}`)}</p><p className="truncate text-xs text-muted-foreground">{describe(c)}</p></div><span className="text-[11px] whitespace-nowrap text-muted-foreground">{date(c.checkedAt)}</span></li>)}</ul>
    </section>
    <section className={cn(panel,"space-y-3 border-blue-200 bg-blue-50/50 p-4 dark:border-blue-900 dark:bg-blue-950/20")}><h2 className="flex items-center gap-2 font-medium text-blue-700 dark:text-blue-300"><DatabaseZap className="size-4" aria-hidden="true"/>{t("infoTitle")}</h2>
     <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
      <dt className="text-muted-foreground">{t("infoMode")}</dt><dd className="text-right">{t(`mode_${status?.mode??"RULES"}`)}</dd>
      {status?.modelProvider&&<><dt className="text-muted-foreground">{t("infoModelProvider")}</dt><dd className="truncate text-right font-medium">{status.modelProvider}</dd></>}
      <dt className="text-muted-foreground">{t("infoModel")}</dt><dd className="truncate text-right">{model}</dd>
      <dt className="text-muted-foreground">{t("infoEndpoint")}</dt><dd className="text-right">{status?.endpoint??"-"}</dd>
      <dt className="text-muted-foreground">{t("infoLocality")}</dt><dd className="text-right">{status?.locality?t(`locality_${status.locality}`):"—"}</dd>
      <dt className="text-muted-foreground">{t("infoManagementLimit")}</dt><dd className="text-right tabular-nums">{status?.managementDailyLimit??"—"}</dd>
      <dt className="text-muted-foreground">{t("infoResidentLimit")}</dt><dd className="text-right tabular-nums">{status?(status.residentEnabled?status.residentDailyLimit:t("off")):"—"}</dd>
      <dt className="font-medium">{t("infoHealthy")}</dt><dd className="text-right font-semibold tabular-nums">{status?`${status.upComponents} / ${status.totalComponents}`:"—"}</dd>
     </dl>
    </section>
   </div>
  </div>
 </div>;
}

export function AiStatusEntry(){
 const t=useTranslations("aiStatus"),identity=useIdentity();
 if(!aiStatusRoles.some(role=>identity.hasRealmRole(role)))return null;
 return <div className="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-2xl border bg-card p-4 shadow-sm"><div className="flex items-center gap-3"><Gauge aria-hidden="true" className="size-6 text-primary"/><div><p className="font-semibold">{t("title")}</p><p className="text-sm text-muted-foreground">{t("entryHint")}</p></div></div>
  <Button asChild variant="view"><Link href={aiStatusHref}>{t("open")}</Link></Button></div>;
}
