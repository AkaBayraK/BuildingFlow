"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Archive,Eye,Globe,History,Pencil,Plus,Rocket,Trash2} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Badge} from "@/components/ui/badge";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {DetailField,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {parseOffsets,readCustomType,readRevision,readSource,readTypePreview,render,sampleValues,triggers,unknownVariables,type Condition,type CustomType,type Revision,type SourceInfo,type Trigger,type TypePreview} from "./model";
import {customIcons,typeStatusIcon} from "./automation-style";

const weekdays=[1,2,3,4,5,6,7];

function useSaving(){const lock=useRef(false);const [busy,setBusy]=useState(false);
 async function run(action:()=>Promise<void>){if(lock.current)return;lock.current=true;setBusy(true);try{await action();}finally{lock.current=false;setBusy(false);}}
 return {busy,run};}

/** User-defined automation kinds: assembled only from code-owned sources, fields, triggers and recipient scopes served by the API. */
export function AutomationTypes({propertyId,writable,onChanged}:{propertyId:string;writable:boolean;onChanged:()=>void}){
 const t=useTranslations("automations"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const editor=["SUPER_ADMIN","SITE_ADMIN"].some(r=>identity.hasRealmRole(r)),platformAdmin=identity.hasRealmRole("SUPER_ADMIN");
 const root=`/api/v1/properties/${propertyId}/automation-types`;
 const [types,setTypes]=useState<CustomType[]|null>(null),[sources,setSources]=useState<SourceInfo[]|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 const [edit,setEdit]=useState<CustomType|"new"|null>(null),[status,setStatus]=useState<{type:CustomType;action:"publish"|"archive"}|null>(null),[preview,setPreview]=useState<CustomType|null>(null),[history,setHistory]=useState<CustomType|null>(null);
 useEffect(()=>{const c=new AbortController();setError("");
  void Promise.all([api<unknown>(root,{signal:c.signal}),api<unknown>(root+"/sources",{signal:c.signal})]).then(([list,catalog])=>{if(c.signal.aborted)return;
   if(!Array.isArray(list)||!Array.isArray(catalog))throw new Error("Invalid automation types");setTypes(list.map(x=>readCustomType(x,propertyId)));setSources(catalog.map(readSource));})
   .catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,propertyId,revision,locale]);
 const changed=()=>{setRevision(n=>n+1);onChanged();};
 if(error)return <MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{t("refresh")}</Button>}>{error}</MessageBanner>;
 if(!types||!sources)return <p role="status">{t("loadingTypes")}</p>;
 return <div className="space-y-4">
  <section className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border bg-card p-5 shadow-sm"><div><h2 className="font-semibold">{t("typesTitle")}</h2><p className="text-sm text-muted-foreground">{t("typesHelp")}</p></div>
   {editor&&<Button disabled={!writable} onClick={()=>setEdit("new")}><Plus/>{t("newType")}</Button>}</section>
  {!types.length&&<p className="rounded-2xl border bg-card p-6 text-sm text-muted-foreground">{t("noTypes")}</p>}
  <div className="grid gap-4 lg:grid-cols-2">{types.map(type=>{const I=customIcons[type.icon as keyof typeof customIcons]??customIcons.BellRing;const S=typeStatusIcon[type.status];
   return <article key={type.id} className="rounded-2xl border bg-card p-5 shadow-sm">
    <div className="flex flex-wrap items-start justify-between gap-3"><div className="flex items-start gap-3"><span className="grid size-11 shrink-0 place-items-center rounded-xl bg-secondary text-primary"><I aria-hidden="true" className="size-5"/></span>
     <div><h3 className="font-semibold">{type.name}</h3><p className="text-xs text-muted-foreground">{t(`source_${type.sourceCode}`)} · {t("typeVersion",{version:type.typeVersion})}</p></div></div>
     <span className="flex flex-wrap gap-1"><Badge variant={type.status==="PUBLISHED"?"default":"secondary"}><S aria-hidden="true"/>{t(`typeStatus_${type.status}`)}</Badge>{type.isPlatform&&<Badge variant="outline"><Globe aria-hidden="true"/>{t("platform")}</Badge>}</span></div>
    <dl className="mt-3 grid gap-x-4 sm:grid-cols-2">
     <DetailField label={t("trigger")}>{triggerSummary(t,type)}</DetailField>
     <DetailField label={t("recipientScopes")}>{type.recipientScopes.map(s=>t(`scope_${s}`)).join(", ")}</DetailField>
     <DetailField label={t("conditions")}>{type.conditions.length?type.conditions.map(c=>`${t(`field_${c.field}`)} ${t(`op_${c.operator}`)} ${c.values.map(v=>t(`option_${v}`)).join(", ")}`).join(" · "):t("noConditions")}</DetailField>
     <DetailField label={t("ruleCount")}>{type.ruleCount}</DetailField>
    </dl>
    <div className="mt-4 flex flex-wrap gap-2">
     {type.canEdit&&editor&&<Button variant="edit" disabled={!writable} onClick={()=>setEdit(type)}><Pencil/>{t("edit")}</Button>}
     {type.canEdit&&editor&&type.status==="DRAFT"&&<Button disabled={!writable} onClick={()=>setStatus({type,action:"publish"})}><Rocket/>{t("publish")}</Button>}
     {type.canEdit&&editor&&<Button variant="warn" disabled={!writable} onClick={()=>setStatus({type,action:"archive"})}><Archive/>{t("archive")}</Button>}
     <Button variant="view" onClick={()=>setPreview(type)}><Eye/>{t("preview")}</Button>
     <Button variant="neutral" onClick={()=>setHistory(type)}><History/>{t("revisions")}</Button>
    </div></article>;})}</div>
  {edit&&<TypeDialog root={root} propertyId={propertyId} type={edit==="new"?null:edit} sources={sources} platformAdmin={platformAdmin} onClose={()=>setEdit(null)} onSaved={changed}/>}
  {status&&<TypeStatusDialog root={root} propertyId={propertyId} value={status} onClose={()=>setStatus(null)} onSaved={changed}/>}
  {preview&&<TypePreviewDialog root={root} type={preview} onClose={()=>setPreview(null)}/>}
  {history&&<TypeRevisionsDialog root={root} type={history} onClose={()=>setHistory(null)}/>}
 </div>;
}

function triggerSummary(t:ReturnType<typeof useTranslations>,x:{trigger:Trigger;dateField:string|null;defaultOffsets:number[];dayOfMonth:number|null;dayOfWeek:number|null}){
 if(x.trigger==="EVENT")return t("trigger_EVENT_TYPE");
 if(x.trigger==="CALENDAR")return x.dayOfMonth?t("everyMonthDay",{day:x.dayOfMonth}):t("everyWeekday",{day:t(`weekday_${x.dayOfWeek}`)});
 return t("dateOffsetSummary",{field:t(`dateField_${x.dateField}`),days:x.defaultOffsets.join(", ")});
}

function TypeDialog({root,propertyId,type,sources,platformAdmin,onClose,onSaved}:{root:string;propertyId:string;type:CustomType|null;sources:SourceInfo[];platformAdmin:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const published=type?.status==="PUBLISHED";
 const [sourceCode,setSourceCode]=useState(type?.sourceCode??sources[0]?.code??""),source=sources.find(s=>s.code===sourceCode)??sources[0];
 const [name,setName]=useState(type?.name??""),[icon,setIcon]=useState(type?.icon??"BellRing"),[trigger,setTrigger]=useState<Trigger>(type?.trigger??source?.triggers[0]??"EVENT");
 const [dateField,setDateField]=useState(type?.dateField??source?.dateFields[0]??""),[offsets,setOffsets]=useState((type?.defaultOffsets??[-1]).join(", "));
 const [calendar,setCalendar]=useState<"month"|"week">(type?.dayOfWeek?"week":"month"),[dayOfMonth,setDayOfMonth]=useState(type?.dayOfMonth??1),[dayOfWeek,setDayOfWeek]=useState(type?.dayOfWeek??1);
 const [conditions,setConditions]=useState<Condition[]>(type?.conditions??[]),[scopes,setScopes]=useState<string[]>(type?.recipientScopes??[]);
 const [title,setTitle]=useState(type?.defaultTitle??""),[body,setBody]=useState(type?.defaultBody??""),[platform,setPlatform]=useState(type?.isPlatform??false),[reason,setReason]=useState(""),[error,setError]=useState("");
 if(!source)return null;
 function changeSource(code:string){const next=sources.find(s=>s.code===code);if(!next)return;setSourceCode(code);setTrigger(next.triggers[0]);setDateField(next.dateFields[0]??"");setConditions([]);setScopes([]);}
 const unknown=[...unknownVariables(title,source.variables),...unknownVariables(body,source.variables)];
 function setCondition(i:number,patch:Partial<Condition>){setConditions(list=>list.map((c,j)=>j===i?{...c,...patch}:c));}
 function save(e:React.FormEvent){e.preventDefault();setError("");
  const days=trigger==="DATE_OFFSET"?parseOffsets(offsets):[];
  if(days===null||trigger==="DATE_OFFSET"&&(days.length<1||days.length>10||days.some(d=>d<-60||d>60))){setError(t("offsetsInvalid",{min:-60,max:60}));return;}
  if(!scopes.length){setError(t("scopeRequired"));return;}
  if(conditions.some(c=>!c.values.length||c.operator==="EQ"&&c.values.length!==1)){setError(t("conditionInvalid"));return;}
  if(unknown.length){setError(t("variablesInvalid",{names:unknown.join(", ")}));return;}
  if(!name.trim()||!title.trim()||!body.trim()||!reason.trim()){setError(t("required"));return;}
  const payload={name:name.trim(),icon,sourceCode,conditions,trigger,dateField:trigger==="DATE_OFFSET"?dateField:null,defaultOffsets:days,dayOfMonth:trigger==="CALENDAR"&&calendar==="month"?dayOfMonth:null,dayOfWeek:trigger==="CALENDAR"&&calendar==="week"?dayOfWeek:null,recipientScopes:scopes,defaultTitle:title.trim(),defaultBody:body.trim(),platform,reason:reason.trim(),version:type?.version??0};
  void run(async()=>{try{readCustomType(await api(type?`${root}/${type.id}`:root,{method:type?"PUT":"POST",body:JSON.stringify(payload)}),propertyId);notice.success(t(type?"typeSaved":"typeCreated"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 const id=(x:string)=>`automation-type-${x}`;
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(type?"editTypeTitle":"newType")}</DialogTitle><DialogDescription>{t("typeDialogHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-5"><fieldset disabled={busy} className="space-y-5">{error&&<MessageBanner>{error}</MessageBanner>}
   <section className="space-y-2"><h3 className="text-sm font-semibold">{t("step",{n:1})} · {t("stepSource")}</h3>
    <Label htmlFor={id("name")}>{t("name")} *</Label><Input id={id("name")} maxLength={100} value={name} onChange={e=>setName(e.target.value)}/>
    <Label htmlFor={id("source")}>{t("source")}</Label><select id={id("source")} className={toolbarSelectClass+" w-full"} value={sourceCode} disabled={published} onChange={e=>changeSource(e.target.value)}>{sources.map(s=><option key={s.code} value={s.code}>{t(`source_${s.code}`)}</option>)}</select>
    <div className="flex flex-wrap gap-2" role="radiogroup" aria-label={t("icon")}>{Object.entries(customIcons).map(([key,I])=><Button key={key} type="button" size="icon" variant={icon===key?"default":"outline"} role="radio" aria-checked={icon===key} aria-label={t(`icon_${key}`)} onClick={()=>setIcon(key)}><I aria-hidden="true"/></Button>)}</div>
   </section>
   <section className="space-y-2"><h3 className="text-sm font-semibold">{t("step",{n:2})} · {t("stepConditions")}</h3>
    {conditions.map((c,i)=>{const field=source.fields.find(f=>f.name===c.field)??source.fields[0];return <div key={i} className="space-y-2 rounded-xl border p-3">
     <div className="grid grid-cols-[1fr_auto_auto] gap-2"><select aria-label={t("field")} className={toolbarSelectClass} value={c.field} onChange={e=>setCondition(i,{field:e.target.value,values:[]})}>{source.fields.map(f=><option key={f.name} value={f.name}>{t(`field_${f.name}`)}</option>)}</select>
      <select aria-label={t("operator")} className={toolbarSelectClass} value={c.operator} onChange={e=>setCondition(i,{operator:e.target.value as Condition["operator"],values:[]})}>{(["EQ","IN","NOT_IN"] as const).map(o=><option key={o} value={o}>{t(`op_${o}`)}</option>)}</select>
      <Button type="button" size="icon" variant="danger" aria-label={t("removeCondition")} onClick={()=>setConditions(list=>list.filter((_,j)=>j!==i))}><Trash2/></Button></div>
     <div className="flex flex-wrap gap-x-4 gap-y-1">{field?.options.map(o=><label key={o} className="flex items-center gap-2 text-sm"><input type={c.operator==="EQ"?"radio":"checkbox"} name={`${id("cond")}-${i}`} checked={c.values.includes(o)} onChange={e=>setCondition(i,{values:c.operator==="EQ"?[o]:e.target.checked?[...c.values,o]:c.values.filter(v=>v!==o)})}/>{t(`option_${o}`)}</label>)}</div>
    </div>;})}
    {source.fields.length>0&&conditions.length<source.fields.length&&<Button type="button" variant="neutral" onClick={()=>setConditions(list=>[...list,{field:source.fields.find(f=>!list.some(c=>c.field===f.name))!.name,operator:"IN",values:[]}])}><Plus/>{t("addCondition")}</Button>}
    {!source.fields.length&&<p className="text-xs text-muted-foreground">{t("noFields")}</p>}
   </section>
   <section className="space-y-2"><h3 className="text-sm font-semibold">{t("step",{n:3})} · {t("stepTrigger")}</h3>
    <select aria-label={t("trigger")} className={toolbarSelectClass+" w-full"} value={trigger} disabled={published} onChange={e=>setTrigger(e.target.value as Trigger)}>{triggers.filter(x=>source.triggers.includes(x)).map(x=><option key={x} value={x}>{t(`triggerKind_${x}`)}</option>)}</select>
    {trigger==="DATE_OFFSET"&&<div className="grid grid-cols-2 gap-3"><div className="space-y-2"><Label htmlFor={id("date")}>{t("dateFieldLabel")}</Label><select id={id("date")} className={toolbarSelectClass+" w-full"} value={dateField} onChange={e=>setDateField(e.target.value)}>{source.dateFields.map(f=><option key={f} value={f}>{t(`dateField_${f}`)}</option>)}</select></div>
     <div className="space-y-2"><Label htmlFor={id("offsets")}>{t("offsets")}</Label><Input id={id("offsets")} value={offsets} onChange={e=>setOffsets(e.target.value)} aria-describedby={id("offsets-help")}/></div><p id={id("offsets-help")} className="col-span-2 text-xs text-muted-foreground">{t("offsetsCustomHelp")}</p></div>}
    {trigger==="EVENT"&&<p className="text-xs text-muted-foreground">{t("customEventHelp")}</p>}
    {trigger==="CALENDAR"&&<div className="grid grid-cols-2 gap-3"><select aria-label={t("calendarMode")} className={toolbarSelectClass} value={calendar} onChange={e=>setCalendar(e.target.value as "month"|"week")}><option value="month">{t("calendarMonth")}</option><option value="week">{t("calendarWeek")}</option></select>
     {calendar==="month"?<select aria-label={t("calendarMonth")} className={toolbarSelectClass} value={dayOfMonth} onChange={e=>setDayOfMonth(Number(e.target.value))}>{Array.from({length:28},(_,i)=>i+1).map(d=><option key={d} value={d}>{d}</option>)}</select>
      :<select aria-label={t("calendarWeek")} className={toolbarSelectClass} value={dayOfWeek} onChange={e=>setDayOfWeek(Number(e.target.value))}>{weekdays.map(d=><option key={d} value={d}>{t(`weekday_${d}`)}</option>)}</select>}</div>}
   </section>
   <fieldset className="space-y-2"><legend className="text-sm font-semibold">{t("step",{n:4})} · {t("stepRecipients")}</legend>
    {source.recipientScopes.map(s=><label key={s} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={scopes.includes(s)} onChange={e=>setScopes(list=>e.target.checked?[...list,s]:list.filter(x=>x!==s))}/>{t(`scope_${s}`)}</label>)}
    <p className="text-xs text-muted-foreground">{t("scopeHelp")}</p>
   </fieldset>
   <section className="space-y-2"><h3 className="text-sm font-semibold">{t("step",{n:5})} · {t("stepMessage")}</h3>
    <Label htmlFor={id("title")}>{t("titleTemplate")} *</Label><Input id={id("title")} maxLength={200} value={title} onChange={e=>setTitle(e.target.value)}/>
    <Label htmlFor={id("body")}>{t("bodyTemplate")} *</Label><Textarea id={id("body")} maxLength={2000} rows={4} value={body} onChange={e=>setBody(e.target.value)}/>
    <div className="flex flex-wrap gap-1" aria-label={t("variables")}>{source.variables.map(v=><Button key={v} type="button" size="sm" variant="neutral" onClick={()=>setBody(b=>b+` {{${v}}}`)}><Plus aria-hidden="true"/>{v}</Button>)}</div>
    <div className="rounded-xl border bg-muted/40 p-3 text-sm" aria-live="polite"><p className="font-semibold">{render(title,sampleValues)}</p><p className="whitespace-pre-wrap">{render(body,sampleValues)}</p></div>
   </section>
   {platformAdmin&&!type&&<label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={platform} onChange={e=>setPlatform(e.target.checked)}/><Globe aria-hidden="true" className="size-4"/>{t("platformType")}</label>}
   <Label htmlFor={id("reason")}>{t("reason")} *</Label><Input id={id("reason")} maxLength={500} value={reason} onChange={e=>setReason(e.target.value)}/>
   {!type&&<p className="text-xs text-muted-foreground">{t("typeCreatedDraft")}</p>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function TypeStatusDialog({root,propertyId,value,onClose,onSaved}:{root:string;propertyId:string;value:{type:CustomType;action:"publish"|"archive"};onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [reason,setReason]=useState(""),[error,setError]=useState("");
 function save(e:React.FormEvent){e.preventDefault();if(!reason.trim()){setError(t("required"));return;}
  void run(async()=>{try{readCustomType(await api(`${root}/${value.type.id}/${value.action}`,{method:"POST",body:JSON.stringify({reason:reason.trim(),version:value.type.version})}),propertyId);notice.success(t(value.action==="publish"?"typePublished":"typeArchived"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(value.action==="publish"?"publishTitle":"archiveTitle",{name:value.type.name})}</DialogTitle><DialogDescription>{t(value.action==="publish"?"publishHelp":"archiveHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="automation-type-status-reason">{t("reason")} *</Label><Input id="automation-type-status-reason" maxLength={500} value={reason} onChange={e=>setReason(e.target.value)}/>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" variant={value.action==="publish"?"default":"warn"} disabled={busy}>{t(value.action)}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function TypePreviewDialog({root,type,onClose}:{root:string;type:CustomType;onClose:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [data,setData]=useState<TypePreview|null>(null),[error,setError]=useState("");
 useEffect(()=>{const c=new AbortController();void api<unknown>(`${root}/${type.id}/preview`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readTypePreview(v));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,type.id,locale]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><DialogTitle>{t("previewTitle",{name:type.name})}</DialogTitle><DialogDescription>{t("typePreviewHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}{!data&&!error&&<p role="status">{ui("loading")}</p>}
  {data&&<div className="space-y-3"><p className="text-sm">{t("typePreviewCount",{records:data.records,recipients:data.recipients})}</p>
   <ul className="space-y-3">{data.items.map((item,i)=><li key={i} className="rounded-xl border p-3 text-sm">{item.unitNumber&&<p className="text-xs text-muted-foreground">{t("unit",{number:item.unitNumber})} · {t("recipients",{count:item.recipients})}</p>}<p className="font-medium">{item.title}</p><p className="whitespace-pre-wrap">{item.body}</p></li>)}</ul></div>}
  <DialogFooter><Button type="button" variant="outline" onClick={onClose}>{ui("close")}</Button></DialogFooter></ManagementPanel></Dialog>;
}

function TypeRevisionsDialog({root,type,onClose}:{root:string;type:CustomType;onClose:()=>void}){
 const t=useTranslations("automations"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [rows,setRows]=useState<Revision[]|null>(null),[error,setError]=useState("");
 useEffect(()=>{const c=new AbortController();void api<unknown>(`${root}/${type.id}/revisions`,{signal:c.signal}).then(v=>{if(!c.signal.aborted){if(!Array.isArray(v))throw new Error("Invalid revisions");setRows(v.map(readRevision));}}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,type.id,locale]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><DialogTitle>{t("revisionsTitle",{name:type.name})}</DialogTitle><DialogDescription>{t("revisionsHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}{!rows&&!error&&<p role="status">{ui("loading")}</p>}
  {rows&&<ol className="space-y-3">{rows.map(r=><li key={r.id} className="rounded-xl border p-3 text-sm"><div className="flex flex-wrap justify-between gap-2"><strong>{t("typeVersion",{version:r.ruleVersion})} · {t(`action_${r.action}`)}</strong><span className="text-xs text-muted-foreground">{formatDateTime(r.createdAt,locale)}</span></div><p className="mt-1">{r.reason}</p></li>)}</ol>}
  <DialogFooter><Button type="button" variant="outline" onClick={onClose}>{ui("close")}</Button></DialogFooter></ManagementPanel></Dialog>;
}
