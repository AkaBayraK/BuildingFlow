"use client";
import {useEffect,useMemo,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {Activity,Eye,History,ListChecks,Pause,Pencil,Play,Plus,RefreshCw,Send,ServerCog,Zap} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Badge} from "@/components/ui/badge";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,SummaryCard,DetailField,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateOnly,formatDateTime,formatCurrency} from "@/lib/format";
import {segments} from "@/features/sms/model";
import {channels,dispatchStatuses,parseOffsets,quiet,readAutomation,readExecution,readOverview,readPage,readDispatch,readPreview,readRevision,render,sampleValues,unknownVariables,
 type Automation,type AutomationExecution,type AutomationOverview,type AutomationPreview,type AutomationTypeInfo,type Channel,type Dispatch,type Page,type Revision} from "./model";
import {automationIcon,channelIcon,statusIcon,statusVariant,typeIcon} from "./automation-style";

type Tab="rules"|"dispatches"|"executions"|"health";
const tabs:Tab[]=["rules","dispatches","executions","health"];
const tabIcon={rules:ListChecks,dispatches:Send,executions:History,health:ServerCog} as const satisfies Record<Tab,unknown>;
const hours=Array.from({length:24},(_,h)=>h);
const pad=(h:number)=>String(h).padStart(2,"0")+":00";

function useSaving(){const lock=useRef(false);const [busy,setBusy]=useState(false);
 async function run(action:()=>Promise<void>){if(lock.current)return;lock.current=true;setBusy(true);try{await action();}finally{lock.current=false;setBusy(false);}}
 return {busy,run};}

export function AutomationWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),scope=usePropertyScope(),identity=useIdentity();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r));
 const [tab,setTab]=useState<Tab>("rules"),[overview,setOverview]=useState<AutomationOverview|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0),[creating,setCreating]=useState(false);
 const root=`/api/v1/properties/${propertyId}/automations`;const refresh=()=>setRevision(n=>n+1);
 useEffect(()=>{const c=new AbortController();setError("");if(manager)void api<unknown>(root,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setOverview(readOverview(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,propertyId,revision,locale,manager]);
 if(!manager)return <div className="py-10"><MessageBanner>{ui("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 const writable=!!scope.property?.isEnabled&&!scope.error;
 const missing=overview?.types.filter(type=>!overview.definitions.some(d=>d.type===type.type))??[];
 const sent=overview?.channels.reduce((sum,c)=>sum+c.delivered+c.queued,0)??0,skipped=overview?.channels.reduce((sum,c)=>sum+c.skipped+c.failed+c.cancelled,0)??0;
 return <div className="py-8"><WorkspaceHeading title={t("title")} description={t("help")} scope={scope.property?.name??ui("loading")} icon={automationIcon} action={<Button disabled={!writable||!missing.length} onClick={()=>setCreating(true)}><Plus/>{t("create")}</Button>}/>
  {error&&<MessageBanner className="mb-4" action={<Button variant="view" onClick={refresh}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
  {overview&&<div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
   <SummaryCard title={t("activeRules")} value={overview.activeCount} note={t("pausedRules",{count:overview.pausedCount})} icon={Zap}/>
   <SummaryCard title={t("runsToday")} value={overview.executionsToday} note={t("lastRun",{value:overview.health.lastExecutionAt?formatDateTime(overview.health.lastExecutionAt,locale):"—"})} icon={Activity}/>
   <SummaryCard title={t("sentToday")} value={sent} note={channels.map(c=>`${t(`channel_${c}`)} ${overview.channels.find(x=>x.channel===c)?.delivered??0}/${overview.channels.find(x=>x.channel===c)?.queued??0}`).join(" · ")} icon={Send}/>
   <SummaryCard title={t("skippedToday")} value={skipped} note={t("skippedHelp")} icon={statusIcon.SKIPPED}/>
  </div>}
  <MessageBanner className="mb-4">{t("pullNotice")}</MessageBanner>
  <div role="tablist" aria-label={t("title")} className="mb-4 flex flex-wrap gap-2">{tabs.map(value=>{const I=tabIcon[value];return <Button key={value} role="tab" aria-selected={tab===value} variant={tab===value?"default":"outline"} onClick={()=>setTab(value)}><I aria-hidden="true"/>{t(`tab_${value}`)}</Button>;})}</div>
  {!overview&&!error?<p role="status">{ui("loading")}</p>:overview&&<div role="tabpanel">
   {tab==="rules"&&<RulesTab root={root} propertyId={propertyId} overview={overview} writable={writable} onChanged={refresh}/>}
   {tab==="dispatches"&&<DispatchesTab root={root} overview={overview} revision={revision}/>}
   {tab==="executions"&&<ExecutionsTab root={root} revision={revision}/>}
   {tab==="health"&&<HealthTab overview={overview}/>}
  </div>}
  {creating&&overview&&<RuleDialog root={root} propertyId={propertyId} types={missing} rule={null} emailAvailable={overview.health.emailChannelAvailable} onClose={()=>setCreating(false)} onSaved={refresh}/>}
 </div>;
}

function Channels({rule}:{rule:Pick<Automation,"inApp"|"sms"|"email">}){const t=useTranslations("automations");
 const active=channels.filter(c=>c==="IN_APP"?rule.inApp:c==="SMS"?rule.sms:rule.email);
 return <span className="flex flex-wrap gap-1">{active.map(c=>{const I=channelIcon[c];return <Badge key={c} variant="outline"><I aria-hidden="true"/>{t(`channel_${c}`)}</Badge>;})}</span>;}

function offsetLabel(t:ReturnType<typeof useTranslations>,offset:number){return offset===0?t("offsetDue"):offset<0?t("offsetBefore",{days:-offset}):t("offsetAfter",{days:offset});}

function RulesTab({root,propertyId,overview,writable,onChanged}:{root:string;propertyId:string;overview:AutomationOverview;writable:boolean;onChanged:()=>void}){
 const t=useTranslations("automations"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [edit,setEdit]=useState<Automation|null>(null),[status,setStatus]=useState<Automation|null>(null),[preview,setPreview]=useState<Automation|null>(null),[history,setHistory]=useState<Automation|null>(null);
 function start(rule:Automation){void run(async()=>{try{const result=readExecution(await api(`${root}/${rule.id}/run`,{method:"POST"}));notice.success(t("runDone",{dispatched:result.dispatched,skipped:result.skipped}));onChanged();}catch(e){notice.error(errorMessage(e,locale));}});}
 if(!overview.definitions.length)return <section className="rounded-2xl border bg-card p-6 shadow-sm"><h2 className="font-semibold">{t("empty")}</h2><p className="mt-2 text-sm text-muted-foreground">{t("emptyHelp")}</p></section>;
 return <div className="grid gap-4 lg:grid-cols-2">{overview.definitions.map(rule=>{const I=typeIcon[rule.type];const last=rule.lastExecution;
  return <article key={rule.id} className="rounded-2xl border bg-card p-5 shadow-sm">
   <div className="flex flex-wrap items-start justify-between gap-3"><div className="flex items-start gap-3"><span className="grid size-11 shrink-0 place-items-center rounded-xl bg-secondary text-primary"><I aria-hidden="true" className="size-5"/></span><div><h2 className="font-semibold">{rule.name}</h2><p className="text-xs text-muted-foreground">{t(`type_${rule.type}`)} · {t("ruleVersion",{version:rule.ruleVersion})}</p></div></div>
    <Badge variant={rule.status==="ACTIVE"?"default":"secondary"}>{rule.status==="ACTIVE"?<Play aria-hidden="true"/>:<Pause aria-hidden="true"/>}{t(`status_${rule.status}`)}</Badge></div>
   <dl className="mt-3 grid gap-x-4 sm:grid-cols-2">
    <DetailField label={t("trigger")}>{rule.type==="PAYMENT_RECEIVED"?t("triggerEvent"):rule.offsetDays.map(o=>offsetLabel(t,o)).join(", ")}</DetailField>
    <DetailField label={t("channels")}><Channels rule={rule}/></DetailField>
    <DetailField label={t("schedule")}>{rule.type==="PAYMENT_RECEIVED"?t("scheduleEvent"):t("scheduleAt",{hour:pad(rule.sendHour)})} · {rule.quietStartHour===rule.quietEndHour?t("noQuiet"):t("quietRange",{start:pad(rule.quietStartHour),end:pad(rule.quietEndHour)})}</DetailField>
    <DetailField label={t("nextRun")}>{rule.nextRunAt?formatDateTime(rule.nextRunAt,locale):"—"}</DetailField>
    <DetailField label={t("lastResult")}>{last?<span>{t(`exec_${last.status}`)} · {formatDateTime(last.startedAt,locale)} · {t("counts",{dispatched:last.dispatched,skipped:last.skipped})}</span>:t("neverRun")}</DetailField>
    <DetailField label={t("validity")}>{rule.startsOn?formatDateOnly(rule.startsOn,locale):t("noStart")} – {rule.endsOn?formatDateOnly(rule.endsOn,locale):t("noEnd")}</DetailField>
   </dl>
   <div className="mt-4 flex flex-wrap gap-2">
    <Button variant="edit" disabled={!writable||busy} onClick={()=>setEdit(rule)}><Pencil/>{t("edit")}</Button>
    <Button variant={rule.status==="ACTIVE"?"warn":"default"} disabled={!writable||busy} onClick={()=>setStatus(rule)}>{rule.status==="ACTIVE"?<Pause/>:<Play/>}{t(rule.status==="ACTIVE"?"pause":"activate")}</Button>
    <Button variant="view" disabled={busy||rule.status!=="ACTIVE"||!writable} onClick={()=>start(rule)}><RefreshCw/>{t("runNow")}</Button>
    <Button variant="view" onClick={()=>setPreview(rule)}><Eye/>{t("preview")}</Button>
    <Button variant="neutral" onClick={()=>setHistory(rule)}><History/>{t("revisions")}</Button>
   </div></article>;})}
  {edit&&<RuleDialog root={root} propertyId={propertyId} types={overview.types.filter(x=>x.type===edit.type)} rule={edit} emailAvailable={overview.health.emailChannelAvailable} onClose={()=>setEdit(null)} onSaved={onChanged}/>}
  {status&&<StatusDialog root={root} rule={status} onClose={()=>setStatus(null)} onSaved={onChanged}/>}
  {preview&&<PreviewDialog root={root} rule={preview} onClose={()=>setPreview(null)}/>}
  {history&&<RevisionsDialog root={root} rule={history} onClose={()=>setHistory(null)}/>}
 </div>;
}

function RuleDialog({root,propertyId,types,rule,emailAvailable,onClose,onSaved}:{root:string;propertyId:string;types:AutomationTypeInfo[];rule:Automation|null;emailAvailable:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [type,setType]=useState(rule?.type??types[0]?.type),info=types.find(x=>x.type===type)??types[0];
 const [name,setName]=useState(rule?.name??info?.defaultName??""),[offsets,setOffsets]=useState((rule?.offsetDays??info?.defaultOffsets??[]).join(", ")),[sendHour,setSendHour]=useState(rule?.sendHour??10),[quietStart,setQuietStart]=useState(rule?.quietStartHour??21),[quietEnd,setQuietEnd]=useState(rule?.quietEndHour??9);
 const [startsOn,setStartsOn]=useState(rule?.startsOn??""),[endsOn,setEndsOn]=useState(rule?.endsOn??""),[inApp,setInApp]=useState(rule?.inApp??true),[sms,setSms]=useState(rule?.sms??false),[email,setEmail]=useState(rule?.email??false);
 const [title,setTitle]=useState(rule?.titleTemplate??info?.defaultTitle??""),[body,setBody]=useState(rule?.bodyTemplate??info?.defaultBody??""),[reason,setReason]=useState(""),[error,setError]=useState("");
 function pick(next:string){const value=types.find(x=>x.type===next);if(!value)return;setType(value.type);setName(value.defaultName);setOffsets(value.defaultOffsets.join(", "));setTitle(value.defaultTitle);setBody(value.defaultBody);}
 if(!info)return null;
 const event=info.type==="PAYMENT_RECEIVED";
 const unknown=[...unknownVariables(title,info.variables),...unknownVariables(body,info.variables)];
 const previewBody=render(body,sampleValues),smsSize=segments(previewBody);
 function save(e:React.FormEvent){e.preventDefault();setError("");
  const days=event?[]:parseOffsets(offsets);
  if(days===null||!event&&(days.length<1||days.length>10||days.some(d=>d<info.minOffset||d>info.maxOffset))){setError(t("offsetsInvalid",{min:info.minOffset,max:info.maxOffset}));return;}
  if(!inApp&&!sms&&!email){setError(t("channelRequired"));return;}
  if(!event&&quiet(quietStart,quietEnd,sendHour)){setError(t("sendHourQuiet"));return;}
  if(startsOn&&endsOn&&endsOn<startsOn){setError(t("datesInvalid"));return;}
  if(unknown.length){setError(t("variablesInvalid",{names:unknown.join(", ")}));return;}
  if(!name.trim()||!title.trim()||!body.trim()||!reason.trim()){setError(t("required"));return;}
  void run(async()=>{try{readAutomation(await api(rule?`${root}/${rule.id}`:root,{method:rule?"PUT":"POST",body:JSON.stringify({type:info.type,name:name.trim(),startsOn:startsOn||null,endsOn:endsOn||null,sendHour,quietStartHour:quietStart,quietEndHour:quietEnd,offsetDays:days,inApp,sms,email,titleTemplate:title.trim(),bodyTemplate:body.trim(),reason:reason.trim(),version:rule?.version??0})}),propertyId);notice.success(t(rule?"saved":"created"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 const id=(x:string)=>`automation-${x}`;
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(rule?"editTitle":"createTitle")}</DialogTitle><DialogDescription>{t("dialogHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   {!rule&&<><Label htmlFor={id("type")}>{t("type")}</Label><select id={id("type")} className={toolbarSelectClass+" w-full"} value={info.type} onChange={e=>pick(e.target.value)}>{types.map(x=><option key={x.type} value={x.type}>{t(`type_${x.type}`)}</option>)}</select></>}
   <Label htmlFor={id("name")}>{t("name")} *</Label><Input id={id("name")} maxLength={100} value={name} onChange={e=>setName(e.target.value)}/>
   {!event&&<><Label htmlFor={id("offsets")}>{t("offsets")} *</Label><Input id={id("offsets")} value={offsets} onChange={e=>setOffsets(e.target.value)} aria-describedby={id("offsets-help")}/><p id={id("offsets-help")} className="text-xs text-muted-foreground">{t(info.type==="DUES_DUE_REMINDER"?"offsetsDueHelp":"offsetsOverdueHelp")}</p></>}
   {event&&<p className="text-sm text-muted-foreground">{t("eventHelp")}</p>}
   <div className="grid grid-cols-3 gap-3">{([["send",sendHour,setSendHour],["quietStart",quietStart,setQuietStart],["quietEnd",quietEnd,setQuietEnd]] as const).map(([key,value,set])=><div key={key} className="space-y-2"><Label htmlFor={id(key)}>{t(key)}</Label><select id={id(key)} className={toolbarSelectClass+" w-full"} value={value} disabled={key==="send"&&event} onChange={e=>set(Number(e.target.value))}>{hours.map(h=><option key={h} value={h}>{pad(h)}</option>)}</select></div>)}</div>
   <p className="text-xs text-muted-foreground">{t("quietHelp")}</p>
   <div className="grid grid-cols-2 gap-3"><div className="space-y-2"><Label htmlFor={id("starts")}>{t("startsOn")}</Label><Input id={id("starts")} type="date" value={startsOn} onChange={e=>setStartsOn(e.target.value)}/></div><div className="space-y-2"><Label htmlFor={id("ends")}>{t("endsOn")}</Label><Input id={id("ends")} type="date" value={endsOn} onChange={e=>setEndsOn(e.target.value)}/></div></div>
   <fieldset className="space-y-2"><legend className="text-sm font-medium">{t("channels")} *</legend>
    {([["IN_APP",inApp,setInApp,true],["SMS",sms,setSms,true],["EMAIL",email,setEmail,emailAvailable||email]] as const).map(([c,value,set,enabled])=>{const I=channelIcon[c];return <label key={c} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={value} disabled={!enabled} onChange={e=>set(e.target.checked)}/><I aria-hidden="true" className="size-4"/>{t(`channel_${c}`)}<span className="text-xs text-muted-foreground">{t(`channelHelp_${c}`)}</span></label>;})}
    <p className="flex items-center gap-2 text-xs text-muted-foreground"><Zap aria-hidden="true" className="size-4"/>{t("pushLater")}</p>
    {!emailAvailable&&<p className="text-xs text-muted-foreground">{t("emailUnavailable")}</p>}
   </fieldset>
   <Label htmlFor={id("title")}>{t("titleTemplate")} *</Label><Input id={id("title")} maxLength={200} value={title} onChange={e=>setTitle(e.target.value)}/>
   <Label htmlFor={id("body")}>{t("bodyTemplate")} *</Label><Textarea id={id("body")} maxLength={2000} rows={5} value={body} onChange={e=>setBody(e.target.value)}/>
   <div className="flex flex-wrap gap-1" aria-label={t("variables")}>{info.variables.map(v=><Button key={v} type="button" size="sm" variant="neutral" onClick={()=>setBody(b=>b+` {{${v}}}`)}><Plus aria-hidden="true"/>{v}</Button>)}</div>
   <section className="rounded-xl border bg-muted/40 p-4 text-sm" aria-live="polite"><h3 className="mb-1 flex items-center gap-2 font-medium"><Eye aria-hidden="true" className="size-4"/>{t("livePreview")}</h3><p className="font-semibold">{render(title,sampleValues)}</p><p className="mt-1 whitespace-pre-wrap">{previewBody}</p>
    {sms&&<p className="mt-2 text-xs text-muted-foreground">{t("smsTemplateNote",{segments:smsSize.segments,encoding:smsSize.encoding})}</p>}</section>
   <Label htmlFor={id("reason")}>{t("reason")} *</Label><Input id={id("reason")} maxLength={500} value={reason} onChange={e=>setReason(e.target.value)} aria-describedby={id("reason-help")}/><p id={id("reason-help")} className="text-xs text-muted-foreground">{t("reasonHelp")}</p>
   {!rule&&<p className="text-xs text-muted-foreground">{t("createdPaused")}</p>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function StatusDialog({root,rule,onClose,onSaved}:{root:string;rule:Automation;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [reason,setReason]=useState(""),[error,setError]=useState("");const activate=rule.status!=="ACTIVE";
 function save(e:React.FormEvent){e.preventDefault();if(!reason.trim()){setError(t("required"));return;}
  void run(async()=>{try{readAutomation(await api(`${root}/${rule.id}/${activate?"activate":"pause"}`,{method:"POST",body:JSON.stringify({reason:reason.trim(),version:rule.version})}),rule.propertyId);notice.success(t(activate?"activated":"paused"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(activate?"activateTitle":"pauseTitle",{name:rule.name})}</DialogTitle><DialogDescription>{t(activate?"activateHelp":"pauseHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="automation-status-reason">{t("reason")} *</Label><Input id="automation-status-reason" maxLength={500} value={reason} onChange={e=>setReason(e.target.value)}/>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" variant={activate?"default":"warn"} disabled={busy}>{t(activate?"activate":"pause")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function PreviewDialog({root,rule,onClose}:{root:string;rule:Automation;onClose:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [date,setDate]=useState(""),[data,setData]=useState<AutomationPreview|null>(null),[error,setError]=useState("");
 useEffect(()=>{const c=new AbortController();setError("");setData(null);void api<unknown>(`${root}/${rule.id}/preview`+(date?`?date=${date}`:""),{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPreview(v));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,rule.id,date,locale]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><DialogTitle>{t("previewTitle",{name:rule.name})}</DialogTitle><DialogDescription>{t("previewHelp")}</DialogDescription></DialogHeader>
  <div className="space-y-4"><Label htmlFor="automation-preview-date">{t("previewDate")}</Label><Input id="automation-preview-date" type="date" value={date} onChange={e=>setDate(e.target.value)}/>
   {error&&<MessageBanner>{error}</MessageBanner>}{!data&&!error&&<p role="status">{ui("loading")}</p>}
   {data&&<><p className="text-sm">{t("previewCount",{count:data.total,date:formatDateOnly(data.date,locale)})}</p>{data.truncated&&<p className="text-xs text-muted-foreground">{t("previewTruncated",{count:data.items.length})}</p>}
    <ul className="space-y-3">{data.items.map((item,i)=><li key={i} className="rounded-xl border p-3 text-sm"><div className="flex flex-wrap justify-between gap-2"><strong>{t("unit",{number:item.unitNumber})}</strong><span>{formatCurrency(item.balance,locale)}</span></div>
     <p className="text-xs text-muted-foreground">{item.offsetDays!==null?offsetLabel(t,item.offsetDays):t("triggerEvent")} · {t("recipients",{count:item.recipients})}</p><p className="mt-1 font-medium">{item.title}</p><p className="whitespace-pre-wrap">{item.body}</p></li>)}</ul></>}
  </div><DialogFooter><Button type="button" variant="outline" onClick={onClose}>{ui("close")}</Button></DialogFooter></ManagementPanel></Dialog>;
}

function RevisionsDialog({root,rule,onClose}:{root:string;rule:Automation;onClose:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [rows,setRows]=useState<Revision[]|null>(null),[error,setError]=useState("");
 useEffect(()=>{const c=new AbortController();void api<unknown>(`${root}/${rule.id}/revisions`,{signal:c.signal}).then(v=>{if(!c.signal.aborted){if(!Array.isArray(v))throw new Error("Invalid revisions");setRows(v.map(readRevision));}}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,rule.id,locale]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><DialogTitle>{t("revisionsTitle",{name:rule.name})}</DialogTitle><DialogDescription>{t("revisionsHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}{!rows&&!error&&<p role="status">{ui("loading")}</p>}
  {rows&&<ol className="space-y-3">{rows.map(r=><li key={r.id} className="rounded-xl border p-3 text-sm"><div className="flex flex-wrap justify-between gap-2"><strong>{t("ruleVersion",{version:r.ruleVersion})} · {t(`action_${r.action}`)}</strong><span className="text-xs text-muted-foreground">{formatDateTime(r.createdAt,locale)}</span></div><p className="mt-1">{r.reason}</p></li>)}</ol>}
  <DialogFooter><Button type="button" variant="outline" onClick={onClose}>{ui("close")}</Button></DialogFooter></ManagementPanel></Dialog>;
}

function DispatchesTab({root,overview,revision}:{root:string;overview:AutomationOverview;revision:number}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [data,setData]=useState<Page<Dispatch>|null>(null),[page,setPage]=useState(1),[channel,setChannel]=useState(""),[status,setStatus]=useState(""),[rule,setRule]=useState(""),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0);
 useEffect(()=>{const c=new AbortController();setLoading(true);setError("");void api<unknown>(`${root}/dispatches?`+new URLSearchParams({page:String(page),pageSize:"20",...(channel?{channel}:{}),...(status?{status}:{}),...(rule?{automationId:rule}:{})}),{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,readDispatch));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,root,page,channel,status,rule,revision,retry,locale]);
 const columns=useMemo<ColumnDef<Dispatch>[]>(()=>[
  {id:"date",header:t("date"),cell:({row})=>formatDateTime(row.original.createdAt,locale)},
  {id:"type",header:t("type"),cell:({row})=>{const I=typeIcon[row.original.type];return <span className="flex items-center gap-2"><I aria-hidden="true" className="size-4"/>{t(`type_${row.original.type}`)}</span>;}},
  {id:"unit",header:t("unitColumn"),cell:({row})=>row.original.unitNumber??"—"},
  {id:"period",header:t("period"),cell:({row})=>row.original.period?formatDateOnly(row.original.period,locale).slice(3):"—"},
  {id:"channel",header:t("channel"),cell:({row})=><ChannelBadge channel={row.original.channel}/>},
  {id:"status",header:ui("status"),cell:({row})=><DispatchStatusCell row={row.original}/>},
  {id:"recipient",header:t("recipient"),cell:({row})=><span className="font-mono text-xs">{row.original.recipientMasked??"—"}</span>},
  {id:"title",header:t("message"),cell:({row})=><span className="line-clamp-2 max-w-xs">{row.original.title??"—"}</span>}],[t,ui,locale]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="flex flex-wrap items-center gap-3 p-4">
   <label className="flex items-center gap-2 text-sm">{t("rule")}<select className={toolbarSelectClass} value={rule} onChange={e=>{setRule(e.target.value);setPage(1);}}><option value="">{t("allRules")}</option>{overview.definitions.map(d=><option key={d.id} value={d.id}>{d.name}</option>)}</select></label>
   <label className="flex items-center gap-2 text-sm">{t("channel")}<select className={toolbarSelectClass} value={channel} onChange={e=>{setChannel(e.target.value);setPage(1);}}><option value="">{t("allChannels")}</option>{channels.map(c=><option key={c} value={c}>{t(`channel_${c}`)}</option>)}</select></label>
   <label className="flex items-center gap-2 text-sm">{ui("status")}<select className={toolbarSelectClass} value={status} onChange={e=>{setStatus(e.target.value);setPage(1);}}><option value="">{t("allStatuses")}</option>{dispatchStatuses.map(s=><option key={s} value={s}>{t(`dispatch_${s}`)}</option>)}</select></label>
   <Button variant="view" onClick={()=>setRetry(n=>n+1)}><RefreshCw/>{t("refresh")}</Button></div>
  <RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t("noDispatches")} card={d=><><div className="flex items-center justify-between gap-2"><strong>{t(`type_${d.type}`)}</strong><DispatchStatusCell row={d}/></div><p className="text-xs text-muted-foreground">{formatDateTime(d.createdAt,locale)} · {t("unit",{number:d.unitNumber??"—"})}</p><ChannelBadge channel={d.channel}/><p className="text-sm">{d.title??"—"}</p></>}/></section>;
}

function ChannelBadge({channel}:{channel:Channel}){const t=useTranslations("automations");const I=channelIcon[channel];return <Badge variant="outline"><I aria-hidden="true"/>{t(`channel_${channel}`)}</Badge>;}
function DispatchStatusCell({row}:{row:Dispatch}){const t=useTranslations("automations");const I=statusIcon[row.status];const reason=row.reasonCode??row.smsErrorCode;
 return <span className="flex flex-col items-start gap-1"><Badge variant={statusVariant[row.status]}><I aria-hidden="true"/>{t(`dispatch_${row.status}`)}</Badge>{row.smsStatus&&<small className="text-muted-foreground">{t("smsStatus",{status:t.has(`sms_${row.smsStatus}`)?t(`sms_${row.smsStatus}`):row.smsStatus})}</small>}{reason&&<small className="text-muted-foreground">{t.has(`reason_${reason}`)?t(`reason_${reason}`):reason}</small>}</span>;}

function ExecutionsTab({root,revision}:{root:string;revision:number}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [data,setData]=useState<Page<AutomationExecution>|null>(null),[page,setPage]=useState(1),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0);
 useEffect(()=>{const c=new AbortController();setLoading(true);setError("");void api<unknown>(`${root}/executions?page=${page}&pageSize=20`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,readExecution));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,root,page,revision,retry,locale]);
 const columns=useMemo<ColumnDef<AutomationExecution>[]>(()=>[
  {id:"date",header:t("date"),cell:({row})=>formatDateTime(row.original.startedAt,locale)},
  {id:"type",header:t("type"),cell:({row})=>t(`type_${row.original.type}`)},
  {id:"trigger",header:t("trigger"),cell:({row})=>t(`trigger_${row.original.trigger}`)},
  {id:"status",header:ui("status"),cell:({row})=><Badge variant={row.original.status==="SUCCEEDED"?"default":"destructive"}>{t(`exec_${row.original.status}`)}</Badge>},
  {id:"counts",header:t("result"),cell:({row})=>t("countsFull",{evaluated:row.original.evaluated,dispatched:row.original.dispatched,skipped:row.original.skipped})},
  {id:"attempts",header:t("attempts"),cell:({row})=>row.original.attempts},
  {id:"error",header:t("error"),cell:({row})=>row.original.errorCode??"—"}],[t,ui,locale]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="flex justify-end p-4"><Button variant="view" onClick={()=>setRetry(n=>n+1)}><RefreshCw/>{t("refresh")}</Button></div>
  <RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t("noExecutions")} card={e=><><div className="flex items-center justify-between gap-2"><strong>{t(`type_${e.type}`)}</strong><Badge variant={e.status==="SUCCEEDED"?"default":"destructive"}>{t(`exec_${e.status}`)}</Badge></div><p className="text-xs text-muted-foreground">{formatDateTime(e.startedAt,locale)} · {t(`trigger_${e.trigger}`)}</p><p className="text-sm">{t("countsFull",{evaluated:e.evaluated,dispatched:e.dispatched,skipped:e.skipped})}</p></>}/></section>;
}

function HealthTab({overview}:{overview:AutomationOverview}){
 const t=useTranslations("automations"),locale=useLocale();const h=overview.health;
 return <div className="space-y-4">
  {h.executionsDeadLetter>0&&<MessageBanner>{t("deadLetterWarning",{count:h.executionsDeadLetter})}</MessageBanner>}
  <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
   <SummaryCard title={t("outbox")} value={h.outboxPending} note={t("outboxRetrying",{count:h.outboxRetrying})} icon={ServerCog}/>
   <SummaryCard title={t("smsQueue")} value={h.smsQueued} note={t("smsFailedToday",{count:h.smsFailedToday})} icon={channelIcon.SMS}/>
   <SummaryCard title={t("executionFailures")} value={h.executionsFailed} note={t("deadLetter",{count:h.executionsDeadLetter})} icon={statusIcon.FAILED}/>
  </div>
  <section className="rounded-2xl border bg-card p-5 shadow-sm"><h2 className="font-semibold">{t("channelState")}</h2><dl className="mt-3 grid gap-x-4 sm:grid-cols-2">
   <DetailField label={t("channel_IN_APP")}>{t("inAppState")}</DetailField>
   <DetailField label={t("channel_SMS")}>{t(h.smsAccountEnabled?"smsReady":"smsMissing")}</DetailField>
   <DetailField label={t("channel_EMAIL")}>{t(h.emailChannelAvailable?"emailReady":"emailMissing")}</DetailField>
   <DetailField label={t("push")}>{t("pushState")}</DetailField>
   <DetailField label={t("lastRunLabel")}>{h.lastExecutionAt?formatDateTime(h.lastExecutionAt,locale):"—"}</DetailField>
  </dl></section></div>;
}
