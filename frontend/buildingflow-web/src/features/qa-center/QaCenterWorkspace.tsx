"use client";
import {useCallback,useEffect,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {FlaskConical,RefreshCw,RotateCcw,Square,Play,Sparkles} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {ApiError,useAuthorizedApi} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Badge} from "@/components/ui/badge";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {BackButton} from "@/components/navigation/BackButton";
import {WorkspaceHeading} from "@/components/management/WorkspacePresentation";
import {cn} from "@/lib/utils";
import {canConfirmCleanup,qaDataKinds,readBatch,readBatches,readPreview,type QaCleanupPreview,type QaDataBatch,type QaDataKind,canRetry,isActive,qaCenterPath,qaCenterRoles,qaPlannedTabs,qaPromptLimit,qaRunStatuses,qaTabs,readAiExplanation,readAiPlan,readAiStatus,readDetail,readOverview,readPage,readRun,readScenarios,runsQuery,scenarioMessageKey,type QaAiExplanation,type QaAiPlan,type QaAiStatus,type QaOverview,type QaRun,type QaRunDetail,type QaRunPage,type QaRunStatus,type QaScenario,type QaTab} from "./model";
import {qaDataKindIcon,qaDefaultScenarioIcon,qaPlannedIcon,qaRunStatusIcon,qaRunStatusTone,qaScenarioIcon,qaStepStatusIcon,qaTabIcon} from "./qa-style";

const panel="min-w-0 rounded-2xl border bg-card p-4 shadow-sm sm:p-5";
const json=(body:unknown)=>({method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify(body)});

// Sistem Ayarları > Test ve Kalite Merkezi: yalnız SUPER_ADMIN; senaryolar kod sahiplidir, ekran yalnız başlat/durdur/yeniden dene ister.
export function QaCenterWorkspace(){
 const t=useTranslations("qaCenter"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const allowed=qaCenterRoles.some(role=>identity.hasRealmRole(role));
 const [tab,setTab]=useState<QaTab>("overview"),[overview,setOverview]=useState<QaOverview|null>(null),[scenarios,setScenarios]=useState<QaScenario[]>([]);
 const [page,setPage]=useState<QaRunPage|null>(null),[pageNumber,setPageNumber]=useState(1),[filter,setFilter]=useState<QaRunStatus|"ALL">("ALL");
 const [selected,setSelected]=useState<QaRunDetail|null>(null),[disabled,setDisabled]=useState(false),[error,setError]=useState(false),[busy,setBusy]=useState("");
 const [batches,setBatches]=useState<QaDataBatch[]>([]),[dataKind,setDataKind]=useState<QaDataKind>("TEST"),[preview,setPreview]=useState<QaCleanupPreview|null>(null),[typed,setTyped]=useState("");
 const [aiStatus,setAiStatus]=useState<QaAiStatus|null>(null),[prompt,setPrompt]=useState(""),[plan,setPlan]=useState<QaAiPlan|null>(null),[explanation,setExplanation]=useState<QaAiExplanation|null>(null);

 const load=useCallback(async(signal?:AbortSignal)=>{
  try{
   const [o,s,p]=await Promise.all([api<unknown>(`${qaCenterPath}/overview`,{signal},"json",[404]),api<unknown>(`${qaCenterPath}/scenarios`,{signal},"json",[404]),api<unknown>(runsQuery(pageNumber,filter),{signal},"json",[404])]);
   if(!signal?.aborted){setOverview(readOverview(o));setScenarios(readScenarios(s));setPage(readPage(p));setError(false);setDisabled(false);}
   const a=await api<unknown>(`${qaCenterPath}/ai/status`,{signal},"json",[404]).catch(()=>null);if(!signal?.aborted&&a)setAiStatus(readAiStatus(a));
   const b=await api<unknown>(`${qaCenterPath}/data/batches`,{signal},"json",[404]).catch(()=>null);if(!signal?.aborted&&b)setBatches(readBatches(b));
  }catch(e){if(!signal?.aborted){if(e instanceof ApiError&&e.status===404)setDisabled(true);else setError(true);}}
 },[api,pageNumber,filter]);
 const loadRun=useCallback(async(id:string,signal?:AbortSignal)=>{try{const d=readDetail(await api<unknown>(`${qaCenterPath}/runs/${id}`,{signal}));if(!signal?.aborted)setSelected(d);}catch{if(!signal?.aborted)setError(true);}},[api]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();void load(c.signal);return()=>c.abort();},[allowed,load]);
 const watching=selected&&isActive(selected.run)?selected.run.id:null;
 useEffect(()=>{if(!watching)return;const timer=window.setInterval(()=>{void loadRun(watching);void load();},2000);return()=>window.clearInterval(timer);},[watching,loadRun,load]);

 const act=async(key:string,request:()=>Promise<unknown>)=>{setBusy(key);try{const run=readRun(await request());await loadRun(run.id);setTab("live");await load();}catch{}finally{setBusy("");}};
 const createBatch=async()=>{setBusy("data");try{readBatch(await api(`${qaCenterPath}/data/batches`,json({kind:dataKind,requestId:crypto.randomUUID()})));await load();}catch{}finally{setBusy("");}};
 const loadPreview=async(batch:QaDataBatch)=>{setBusy("preview");setTyped("");try{setPreview(readPreview(await api(`${qaCenterPath}/data/batches/${batch.id}/cleanup-preview`)));}catch{}finally{setBusy("");}};
 const clean=async(value:QaCleanupPreview)=>{setBusy("clean");try{readBatch(await api(`${qaCenterPath}/data/batches/${value.batchId}/cleanup`,json({version:value.version,confirmation:typed})));setPreview(null);setTyped("");await load();}catch{}finally{setBusy("");}};
 const askPlan=async()=>{setBusy("ai-plan");setPlan(null);try{setPlan(readAiPlan(await api(`${qaCenterPath}/ai/plans`,json({prompt}))));}catch{}finally{setBusy("");void load();}};
 const runPlan=async(value:QaAiPlan)=>{for(const key of value.scenarios)await start(key);setPlan(null);};
 const explain=async(run:QaRun)=>{setBusy("ai-explain");setExplanation(null);try{setExplanation(readAiExplanation(await api(`${qaCenterPath}/ai/runs/${run.id}/explanation`,{method:"POST"})));}catch{}finally{setBusy("");}};
 const start=(scenario:string)=>act(scenario,()=>api(`${qaCenterPath}/runs`,json({scenarioKey:scenario,requestId:crypto.randomUUID()})));
 const retry=(run:QaRun)=>act(run.id,()=>api(`${qaCenterPath}/runs/${run.id}/retry`,json({requestId:crypto.randomUUID(),version:run.version})));
 const cancel=(run:QaRun)=>act(run.id,()=>api(`${qaCenterPath}/runs/${run.id}/cancel`,{...json({version:run.version}),method:"PUT"}));
 const date=(value:string|null)=>value?new Intl.DateTimeFormat(locale,{dateStyle:"short",timeStyle:"medium"}).format(new Date(value)):"—";
 const scenarioName=(key:string)=>t.has(scenarioMessageKey(key))?t(scenarioMessageKey(key)):key;

 if(!allowed)return <div className="py-10"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;
 const status=(value:QaRunStatus)=>{const I=qaRunStatusIcon[value];return <Badge className={cn("gap-1",qaRunStatusTone[value])}><I aria-hidden="true" className={cn("size-3.5",value==="RUNNING"&&"animate-spin")}/>{t(`status_${value}`)}</Badge>;};
 const actions=(run:QaRun)=><div className="flex flex-wrap gap-2">
  <Button size="sm" variant="view" onClick={()=>{void loadRun(run.id);setTab("live");}}>{t("viewRun")}</Button>
  {isActive(run)&&<Button size="sm" variant="warn" disabled={busy===run.id||run.cancelRequested} onClick={()=>void cancel(run)}><Square aria-hidden="true"/>{t("cancel")}</Button>}
  {canRetry(run)&&<Button size="sm" variant="edit" disabled={busy===run.id} onClick={()=>void retry(run)}><RotateCcw aria-hidden="true"/>{t("retry")}</Button>}
 </div>;

 return <div className="flex min-w-0 flex-col gap-4 py-8">
  <BackButton href="/system-settings" label={t("back")}/>
  <WorkspaceHeading title={t("title")} description={t("subtitle")} scope={t("scope")} icon={FlaskConical} action={<div className="flex flex-wrap items-center gap-2">
   {overview&&<Badge variant="secondary">{t("environment",{name:overview.environment})}</Badge>}
   <Button variant="view" onClick={()=>void load()}><RefreshCw aria-hidden="true"/>{t("refresh")}</Button></div>}/>
  {disabled&&<MessageBanner kind="info">{t("disabled")}</MessageBanner>}
  {error&&<MessageBanner>{t("loadFailed")}</MessageBanner>}
  <div role="tablist" aria-label={t("title")} className="flex flex-wrap gap-2">
   {qaTabs.map(value=>{const I=qaTabIcon[value];return <Button key={value} role="tab" aria-selected={tab===value} variant={tab===value?"default":"outline"} onClick={()=>setTab(value)}><I aria-hidden="true"/>{t(`tab_${value}`)}</Button>;})}
   {qaPlannedTabs.map(([value,phase])=>{const I=qaPlannedIcon[value];return <Button key={value} role="tab" aria-selected={false} aria-disabled="true" disabled variant="outline" title={t("plannedPhase",{phase})}><I aria-hidden="true"/>{t(`tab_${value}`)}<span className="text-xs">{t("plannedPhase",{phase})}</span></Button>;})}
  </div>
  {!overview&&!disabled&&!error?<p role="status" className="text-sm text-muted-foreground">{t("loading")}</p>:overview&&<div role="tabpanel" className="flex min-w-0 flex-col gap-4">
   {tab==="overview"&&<>
    <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
     {[["scenarios",overview.scenarioCount],["endpoints",overview.endpointCount],["active",overview.activeRuns],["total",Object.values(overview.runCounts).reduce((a,b)=>a+b,0)]].map(([key,value])=>
      <div key={key} className={panel}><p className="text-xs text-muted-foreground">{t(`card_${key}`)}</p><p className="mt-1 text-2xl font-semibold">{value}</p></div>)}
    </div>
    <div className={panel}><h2 className="mb-3 font-semibold">{t("statusDistribution")}</h2><div className="flex flex-wrap gap-2">{qaRunStatuses.map(value=><span key={value} className="flex items-center gap-1">{status(value)}<span className="text-sm font-medium">{overview.runCounts[value]}</span></span>)}</div></div>
    <div className={panel}><h2 className="mb-2 font-semibold">{t("lastRun")}</h2>{overview.lastRun?<div className="flex flex-wrap items-center gap-3 text-sm"><code>{overview.lastRun.code}</code>{status(overview.lastRun.status)}<span>{scenarioName(overview.lastRun.scenarioKey)}</span><span className="text-muted-foreground">{date(overview.lastRun.queuedAt)}</span></div>:<p className="text-sm text-muted-foreground">{t("noRuns")}</p>}</div>
   </>}
   {tab==="inventory"&&<div className={panel}><h2 className="mb-2 font-semibold">{t("tab_inventory")}</h2><p className="text-sm">{t("inventoryLive",{count:overview.endpointCount})}</p><p className="mt-2 text-sm text-muted-foreground">{t("inventoryStatic")}</p></div>}
   {tab==="scenarios"&&<div className="grid gap-3 md:grid-cols-3">{scenarios.map(s=>{const I=qaScenarioIcon[s.key]??qaDefaultScenarioIcon;return <div key={s.key} className={cn(panel,"flex flex-col gap-3")}>
    <div className="flex items-center gap-2"><I aria-hidden="true" className="size-5 text-primary"/><h2 className="font-semibold">{scenarioName(s.key)}</h2></div>
    <p className="text-sm text-muted-foreground">{t.has(scenarioMessageKey(s.key)+"_hint")?t(scenarioMessageKey(s.key)+"_hint"):s.key}</p>
    <div className="flex flex-wrap gap-2 text-xs"><Badge variant="outline">{t("readOnly")}</Badge><Badge variant="outline">{t("timeout",{seconds:s.timeoutSeconds})}</Badge></div>
    <Button className="mt-auto" disabled={busy===s.key} onClick={()=>void start(s.key)}><Play aria-hidden="true"/>{t("start")}</Button></div>;})}</div>}
   {tab==="assistant"&&<div className={cn(panel,"flex flex-col gap-3")}>
    <div className="flex flex-wrap items-center gap-2"><Sparkles aria-hidden="true" className="size-5 text-primary"/><h2 className="font-semibold">{t("tab_assistant")}</h2>{aiStatus&&<Badge variant="outline">{aiStatus.enabled?t("aiUsage",{used:aiStatus.usedToday,limit:aiStatus.dailyLimit}):t("aiDisabled")}</Badge>}</div>
    <p className="text-sm text-muted-foreground">{t("aiIntro")}</p>
    <label htmlFor="qa-ai-prompt" className="text-sm font-medium">{t("aiPrompt")}</label>
    <textarea id="qa-ai-prompt" className="min-h-28 rounded-lg border bg-background p-3 text-sm" maxLength={qaPromptLimit} value={prompt} onChange={event=>setPrompt(event.target.value)} placeholder={t("aiPlaceholder")}/>
    <div className="flex flex-wrap items-center justify-between gap-2"><span className="text-xs text-muted-foreground">{prompt.length}/{qaPromptLimit}</span>
     <Button disabled={!aiStatus?.enabled||!prompt.trim()||busy==="ai-plan"} onClick={()=>void askPlan()}><Sparkles aria-hidden="true"/>{t("aiAsk")}</Button></div>
    {plan&&<div className="rounded-xl border border-dashed border-amber-400 bg-amber-50 p-3 text-sm dark:bg-amber-950/30" aria-live="polite">
     <p className="font-semibold">{t("aiProposal")}</p><p className="mt-1">{plan.summary}</p>
     <ul className="mt-2 list-disc ps-5">{plan.scenarios.map(key=><li key={key}>{scenarioName(key)} <code className="text-xs">{key}</code></li>)}</ul>
     {plan.risks.length>0&&<><p className="mt-2 font-medium">{t("aiRisks")}</p><ul className="list-disc ps-5">{plan.risks.map(risk=><li key={risk}>{risk}</li>)}</ul></>}
     <div className="mt-3 flex flex-wrap gap-2"><Button onClick={()=>void runPlan(plan)}><Play aria-hidden="true"/>{t("aiApprove")}</Button><Button variant="outline" onClick={()=>setPlan(null)}>{t("aiReject")}</Button></div></div>}
   </div>}
   {tab==="data"&&<div className={cn(panel,"flex flex-col gap-3")}>
    <p className="text-sm text-muted-foreground">{t("dataIntro")}</p>
    <div role="group" aria-label={t("dataKind")} className="flex flex-wrap items-center gap-2">{qaDataKinds.map(kind=>{const I=qaDataKindIcon[kind];return <Button key={kind} size="sm" aria-pressed={dataKind===kind} variant={dataKind===kind?"default":"outline"} onClick={()=>setDataKind(kind)}><I aria-hidden="true"/>{t(`kind_${kind}`)}</Button>;})}
     <Button disabled={busy==="data"} onClick={()=>void createBatch()}><Play aria-hidden="true"/>{t("dataCreate")}</Button></div>
    {batches.length===0?<p className="text-sm text-muted-foreground">{t("dataEmpty")}</p>:<ul className="divide-y">{batches.map(batch=>{const I=qaDataKindIcon[batch.kind];return <li key={batch.id} className="flex flex-col gap-2 py-3 text-sm md:flex-row md:items-center md:justify-between">
     <div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><code className="break-all">{batch.code}</code><Badge variant="outline" className="gap-1"><I aria-hidden="true" className="size-3.5"/>{t(`kind_${batch.kind}`)}</Badge><Badge variant="secondary">{t(`dataStatus_${batch.status}`)}</Badge></div>
      <p className="text-muted-foreground">{t("dataRecords",{count:batch.recordCount})} · {batch.environment} · {date(batch.generatedAt)}</p></div>
     <div className="flex flex-wrap gap-2">{batch.propertyId&&batch.status==="ACTIVE"&&<Button asChild size="sm" variant="view"><Link href={`/properties/${batch.propertyId}`}>{t("dataOpen")}</Link></Button>}
      {batch.kind==="TEST"&&batch.status==="ACTIVE"&&<Button size="sm" variant="warn" onClick={()=>{setTab("cleanup");void loadPreview(batch);}}>{t("dataPreview")}</Button>}</div></li>;})}</ul>}
   </div>}
   {tab==="cleanup"&&<div className={cn(panel,"flex flex-col gap-3")}>
    <p className="text-sm text-muted-foreground">{t("cleanupIntro")}</p>
    {!preview?<p className="text-sm text-muted-foreground">{t("cleanupSelect")}</p>:<>
     <div className="flex flex-wrap items-center gap-2"><code className="break-all">{preview.code}</code><Badge variant="outline">{t(`kind_${preview.kind}`)}</Badge></div>
     <ol className="list-decimal ps-5 text-sm">{preview.items.map(item=><li key={item.entityType}>{t("cleanupItem",{type:item.entityType,count:item.count})}</li>)}</ol>
     {preview.blockers.length>0&&<MessageBanner kind="warning">{preview.blockers.map(code=>t(`blocker_${code}`)).join(" ")}</MessageBanner>}
     <label htmlFor="qa-cleanup-confirm" className="text-sm font-medium">{t("cleanupConfirmLabel",{text:preview.confirmation})}</label>
     <input id="qa-cleanup-confirm" className="h-9 rounded-lg border bg-background px-3 text-sm" value={typed} disabled={!preview.canClean} onChange={event=>setTyped(event.target.value)} autoComplete="off"/>
     <div className="flex flex-wrap gap-2"><Button variant="danger" disabled={!canConfirmCleanup(preview,typed)||busy==="clean"} onClick={()=>void clean(preview)}>{t("cleanupRun")}</Button><Button variant="outline" onClick={()=>setPreview(null)}>{t("cancel")}</Button></div></>}
   </div>}
   {tab==="runs"&&page&&<div className={panel}>
    <div role="group" aria-label={t("statusFilter")} className="mb-3 flex flex-wrap gap-2">{(["ALL",...qaRunStatuses] as const).map(value=>{const I=value==="ALL"?qaTabIcon.runs:qaRunStatusIcon[value];return <Button key={value} size="sm" aria-pressed={filter===value} variant={filter===value?"default":"outline"} onClick={()=>{setFilter(value);setPageNumber(1);}}><I aria-hidden="true"/>{value==="ALL"?t("all"):t(`status_${value}`)}</Button>;})}</div>
    {page.items.length===0?<p className="text-sm text-muted-foreground">{t("noRuns")}</p>:<ul className="divide-y">{page.items.map(run=><li key={run.id} className="flex flex-col gap-2 py-3 md:flex-row md:items-center md:justify-between">
     <div className="min-w-0 text-sm"><div className="flex flex-wrap items-center gap-2"><code className="break-all">{run.code}</code>{status(run.status)}</div>
      <p className="text-muted-foreground">{scenarioName(run.scenarioKey)} · {t("attempt",{attempt:run.attempt})} · {date(run.queuedAt)} · {t("stepsSummary",{passed:run.passedSteps,failed:run.failedSteps})}</p></div>{actions(run)}</li>)}</ul>}
    <div className="mt-3 flex items-center justify-between text-sm"><span>{t("pageOf",{page:page.page,total:Math.max(1,Math.ceil(page.total/page.pageSize))})}</span><div className="flex gap-2">
     <Button size="sm" variant="outline" disabled={page.page<=1} onClick={()=>setPageNumber(page.page-1)}>{t("previous")}</Button>
     <Button size="sm" variant="outline" disabled={page.page*page.pageSize>=page.total} onClick={()=>setPageNumber(page.page+1)}>{t("next")}</Button></div></div>
   </div>}
   {tab==="live"&&<div className={panel}>{!selected?<p className="text-sm text-muted-foreground">{t("selectRun")}</p>:<>
    <div className="mb-3 flex flex-wrap items-center justify-between gap-2"><div className="flex flex-wrap items-center gap-2"><code className="break-all">{selected.run.code}</code>{status(selected.run.status)}{selected.run.resultCode&&<Badge variant="outline">{selected.run.resultCode}</Badge>}</div>{actions(selected.run)}</div>
    <p className="mb-3 text-xs text-muted-foreground" aria-live="polite">{isActive(selected.run)?t("polling"):t("finishedAt",{time:date(selected.run.finishedAt)})}</p>
    {selected.steps.length===0?<p className="text-sm text-muted-foreground">{t("noSteps")}</p>:<ol className="flex flex-col gap-2">{selected.steps.map(step=>{const I=qaStepStatusIcon[step.status];return <li key={step.sequence} className="rounded-xl border p-3 text-sm">
     <div className="flex flex-wrap items-center gap-2 font-medium"><I aria-hidden="true" className={cn("size-4",step.status==="PASSED"?"text-emerald-600":step.status==="FAILED"?"text-red-600":"text-muted-foreground")}/>{step.sequence}. <code className="break-all">{step.stepKey}</code><span className="sr-only">{t(`step_${step.status}`)}</span><span className="ms-auto text-xs text-muted-foreground">{step.durationMs} ms</span></div>
     <p className="mt-1 break-words">{t("stepResult",{expected:step.expected,actual:step.actual})}</p></li>;})}</ol>}
    <p className="mt-3 text-xs text-muted-foreground">{t("evidenceNote")}</p>
    {!isActive(selected.run)&&aiStatus?.enabled&&<Button className="mt-3" size="sm" variant="view" disabled={busy==="ai-explain"} onClick={()=>void explain(selected.run)}><Sparkles aria-hidden="true"/>{t("aiExplain")}</Button>}
    {explanation&&explanation.runId===selected.run.id&&<div className="mt-3 rounded-xl border border-dashed border-amber-400 bg-amber-50 p-3 text-sm dark:bg-amber-950/30" aria-live="polite"><p className="font-semibold">{t("aiExplanationTitle")}</p><p className="mt-1 whitespace-pre-line">{explanation.explanation}</p></div>}</>}</div>}
  </div>}
 </div>;
}

export function QaCenterEntry(){
 const t=useTranslations("qaCenter"),identity=useIdentity();
 if(!qaCenterRoles.some(role=>identity.hasRealmRole(role)))return null;
 return <div className="mb-8 flex flex-wrap items-center justify-between gap-3 rounded-2xl border bg-card p-4 shadow-sm"><div className="flex items-center gap-3"><FlaskConical aria-hidden="true" className="size-6 text-primary"/><div><p className="font-semibold">{t("title")}</p><p className="text-sm text-muted-foreground">{t("entryHint")}</p></div></div>
  <Button asChild variant="view"><Link href="/system-settings/qa-center">{t("open")}</Link></Button></div>;
}
