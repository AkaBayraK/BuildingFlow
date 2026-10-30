"use client";
import {useCallback,useEffect,useMemo,useRef,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {CheckCircle2,ExternalLink,FileText,Lock,PauseCircle,Pencil,RefreshCw,Save,SlidersHorizontal} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {ApiError,useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Dialog,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {FilterChips} from "@/components/management/FilterChips";
import {ToneBadge,WorkspaceHeading,type Tone} from "@/components/management/WorkspacePresentation";
import {allIcon,catalogIcon,groupStyle,iconByKey,levelStyle,listIcon,resolvedStyle} from "./definition-style";
import {definitionBody,definitionEditorRole,definitionForm,definitionFormErrors,definitionGroups,definitionIconKeys,definitionLabelKey,definitionLevels,definitionRoles,definitionTones,definitionValuePath,definitionsPath,filterLists,maxDefinitionName,maxDefinitionOrder,overrideName,readDefinitionValue,readDefinitions,sortedValues,type DefinitionForm,type DefinitionGroup,type DefinitionLevel,type DefinitionList,type DefinitionValue,type Definitions} from "./model";

const panel="min-w-0 rounded-2xl border bg-card shadow-sm";
const levelTone:Record<DefinitionLevel,Tone>={FIXED:"neutral",SYSTEM:"info",PROPERTY:"success"};

// Yönetim > Tanımlar: TNM 01 liste görünümü, TNM 02 sistem geneli ad/ikon/ton/sıra/aktiflik düzenlemesi (yalnız SUPER_ADMIN).
export function DefinitionsWorkspace(){
 const t=useTranslations("definitions"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const allowed=definitionRoles.some(role=>identity.hasRealmRole(role)),canEdit=identity.hasRealmRole(definitionEditorRole);
 const [data,setData]=useState<Definitions|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState("");
 const [editing,setEditing]=useState<{list:DefinitionList;value:DefinitionValue;trigger:HTMLElement|null}|null>(null);
 const [group,setGroup]=useState<DefinitionGroup|"ALL">("ALL"),[level,setLevel]=useState<DefinitionLevel|"ALL">("ALL");

 const load=useCallback(async(signal?:AbortSignal)=>{
  setLoading(true);
  try{const value=readDefinitions(await api<unknown>(definitionsPath,{signal}));if(!signal?.aborted){setData(value);setError("");}}
  catch(e){if(!signal?.aborted)setError(errorMessage(e,locale));}
  finally{if(!signal?.aborted)setLoading(false);}
 },[api,locale]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();void load(c.signal);return()=>c.abort();},[allowed,load]);

 const lists=useMemo(()=>filterLists(data?.lists??[],group,level),[data,group,level]);
 const catalogs=useMemo(()=>(data?.catalogs??[]).filter(c=>group==="ALL"||c.group===group),[data,group]);
 if(!allowed)return <div className="py-10"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;

 const groupChips=[{value:"ALL",label:t("allGroups"),icon:allIcon,count:data?.lists.length},...definitionGroups.map(g=>({value:g,label:t(`group_${g}`),icon:groupStyle[g].icon,tone:groupStyle[g].tone,count:data?.lists.filter(l=>l.group===g).length}))];
 const levelChips=[{value:"ALL",label:t("allLevels"),icon:allIcon},...definitionLevels.map(l=>({value:l,label:t(`level_${l}`),icon:levelStyle[l].icon,tone:levelStyle[l].tone}))];

 return <div className="flex min-w-0 flex-col gap-4 py-8">
  <WorkspaceHeading title={t("title")} description={t("subtitle")} scope={t("scope")} icon={SlidersHorizontal} action={<Button variant="view" onClick={()=>void load()} disabled={loading}><RefreshCw className={loading?"animate-spin":undefined}/>{t("refresh")}</Button>}/>
  <MessageBanner kind="info">{t(canEdit?"editNotice":"readOnlyNotice")}</MessageBanner>
  {error&&<MessageBanner>{error}</MessageBanner>}
  <section className={panel+" flex flex-col gap-3 p-4"} aria-label={t("filters")}>
   <FilterChips label={t("groupFilter")} items={groupChips} value={group} onChange={value=>setGroup(value as DefinitionGroup|"ALL")}/>
   <FilterChips label={t("levelFilter")} items={levelChips} value={level} onChange={value=>setLevel(value as DefinitionLevel|"ALL")}/>
  </section>
  {loading&&!data?<div className="grid gap-4 lg:grid-cols-2">{Array.from({length:4},(_,i)=><Skeleton key={i} className="h-40 rounded-2xl"/>)}</div>:<>
   {group==="DOCUMENT"&&<div className={panel+" flex items-start gap-3 p-4"}><span className="request-type-icon tone-neutral"><FileText aria-hidden="true"/></span><div><h2 className="font-semibold">{t("group_DOCUMENT")}</h2><p className="text-sm text-muted-foreground">{t("documentTypesMissing")}</p></div></div>}
   {lists.length===0&&group!=="DOCUMENT"&&<p className="py-6 text-center text-sm text-muted-foreground">{t("noLists")}</p>}
   <div className="grid gap-4 lg:grid-cols-2">{lists.map(list=><DefinitionCard key={list.key} list={list} canEdit={canEdit} onEdit={(value,trigger)=>setEditing({list,value,trigger})}/>)}</div>
   {catalogs.length>0&&<section className={panel+" p-4"} aria-labelledby="definition-catalogs">
    <h2 id="definition-catalogs" className="font-semibold">{t("catalogsTitle")}</h2>
    <p className="mb-3 text-sm text-muted-foreground">{t("catalogsDescription")}</p>
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">{catalogs.map(c=>{const Icon=catalogIcon[c.key];return <div key={c.key} className="flex min-w-0 flex-col gap-2 rounded-xl border p-3">
     <div className="flex items-center gap-2 font-medium"><span className="request-type-icon tone-blue"><Icon aria-hidden="true"/></span>{t(`catalog_${c.key}`)}</div>
     <p className="text-sm text-muted-foreground">{t(`catalogNote_${c.key}`)}</p>
     <div className="mt-auto flex flex-wrap items-center gap-2"><ToneBadge tone="neutral" icon={groupStyle[c.group].icon}>{t(`scope_${c.scope}`)}</ToneBadge>
      {c.route?<Button asChild variant="view" size="sm"><Link href={c.route}><ExternalLink aria-hidden="true"/>{t("openCatalog")}</Link></Button>:<ToneBadge tone="warning" icon={Lock}>{t("catalogPlanned")}</ToneBadge>}</div>
    </div>;})}</div>
   </section>}
  </>}
  {editing&&<DefinitionEditor list={editing.list} value={editing.value} trigger={editing.trigger} onClose={()=>setEditing(null)} onSaved={saved=>setData(current=>current&&{...current,lists:current.lists.map(l=>l.key!==editing.list.key?l:{...l,values:l.values.map(v=>v.code===saved.code?saved:v)})})}/>}
 </div>;
}

function useValueLabel(list:DefinitionList){
 const root=useTranslations(),locale=useLocale();
 return (value:DefinitionValue)=>{const custom=overrideName(value,locale);if(custom)return custom;const {namespace,key}=definitionLabelKey(list.key,value.code);const path=`${namespace}.${key}`;return root.has(path)?root(path):value.code;};
}

function DefinitionCard({list,canEdit,onEdit}:{list:DefinitionList;canEdit:boolean;onEdit:(value:DefinitionValue,trigger:HTMLElement)=>void}){
 const t=useTranslations("definitions"),label=useValueLabel(list);
 const Icon=listIcon[list.key],style=groupStyle[list.group],editable=canEdit&&list.editable;
 return <section className={panel+" flex flex-col gap-3 p-4"} aria-labelledby={`definition-${list.key}`}>
  <div className="flex flex-wrap items-start justify-between gap-2">
   <div className="flex min-w-0 items-center gap-2"><span className={`request-type-icon tone-${style.tone}`}><Icon aria-hidden="true"/></span><div className="min-w-0"><h2 id={`definition-${list.key}`} className="font-semibold">{t(`list_${list.key}`)}</h2><p className="text-xs text-muted-foreground">{t(`group_${list.group}`)} · {t("valueCount",{count:list.values.length})}</p></div></div>
   <div className="flex flex-wrap gap-1.5"><ToneBadge tone={levelTone[list.level]} icon={levelStyle[list.level].icon}>{t(`level_${list.level}`)}</ToneBadge>{list.plannedPhase&&!list.editable&&<ToneBadge tone="violet">{t("plannedPhase",{phase:list.plannedPhase.replace("_"," ")})}</ToneBadge>}{list.editable&&<ToneBadge tone="success" icon={Pencil}>{t("editable")}</ToneBadge>}</div>
  </div>
  <p className="text-sm text-muted-foreground">{t(`levelHint_${list.level}`)}</p>
  <ul className="flex flex-wrap gap-2" aria-label={t("valuesOf",{list:t(`list_${list.key}`)})}>{sortedValues(list.values).map(value=>{const s=resolvedStyle(list.key,value);const V=s.icon;const content=<><span className={`request-type-icon tone-${s.tone}`}><V aria-hidden="true"/></span>{label(value)}{!value.isEnabled&&<span className="text-xs text-muted-foreground">({t("passive")})</span>}{editable&&<Pencil aria-hidden="true" className="size-3.5 text-muted-foreground"/>}</>;
   return <li key={value.code} title={value.code}>{editable?<button type="button" className={"request-type rounded-lg border px-2 py-1 text-sm hover:bg-accent"+(value.isEnabled?"":" opacity-60")} aria-label={t("editValue",{value:label(value)})} onClick={e=>onEdit(value,e.currentTarget)}>{content}</button>:<span className={"request-type rounded-lg border px-2 py-1 text-sm"+(value.isEnabled?"":" opacity-60")}>{content}</span>}</li>;})}</ul>
 </section>;
}

function DefinitionEditor({list,value,trigger,onClose,onSaved}:{list:DefinitionList;value:DefinitionValue;trigger:HTMLElement|null;onClose:()=>void;onSaved:(value:DefinitionValue)=>void}){
 const t=useTranslations("definitions"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification(),label=useValueLabel(list);
 const [current,setCurrent]=useState(value),[form,setForm]=useState<DefinitionForm>(()=>definitionForm(value));
 const [busy,setBusy]=useState(false),[error,setError]=useState(""),[conflict,setConflict]=useState(false),[fields,setFields]=useState<Record<string,string[]>>({}),[invalid,setInvalid]=useState<string[]>([]);
 const lock=useRef(false),defaultStyle=resolvedStyle(list.key,{...current,iconKey:null,tone:null});
 const preview=resolvedStyle(list.key,{...current,iconKey:form.iconKey||null,tone:form.tone||null}),Preview=preview.icon;
 async function reload(){
  setBusy(true);
  try{const fresh=readDefinitions(await api<unknown>(definitionsPath)).lists.find(l=>l.key===list.key)?.values.find(v=>v.code===current.code);if(fresh){setCurrent(fresh);setForm(definitionForm(fresh));onSaved(fresh);}setError("");setConflict(false);setFields({});}
  catch(e){setError(errorMessage(e,locale));}finally{setBusy(false);}
 }
 async function save(e:React.FormEvent){
  e.preventDefault();if(lock.current)return;
  const problems=definitionFormErrors(form,current.locked);setInvalid(problems);if(problems.length)return;
  lock.current=true;setBusy(true);setError("");setConflict(false);setFields({});
  try{const saved=readDefinitionValue(await api<unknown>(definitionValuePath(list.key,current.code),{method:"PUT",body:JSON.stringify(definitionBody(form,current.version))}));onSaved(saved);notice.success(t("saved"));onClose();}
  catch(err){setError(errorMessage(err,locale));setFields(err instanceof ApiError?err.errors:{});setConflict(err instanceof ApiError&&err.status===409);}
  finally{lock.current=false;setBusy(false);}
 }
 const issue=(field:string)=>fields[field]?.join(" ")||(invalid.includes(field)?t(`invalid_${field}`):"");
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel className="grid-cols-1 [&>*]:min-w-0" showCloseButton={!busy} onCloseAutoFocus={e=>{e.preventDefault();requestAnimationFrame(()=>trigger?.isConnected&&trigger.focus());}} onEscapeKeyDown={e=>{if(busy)e.preventDefault();}} onPointerDownOutside={e=>{if(busy)e.preventDefault();}}>
  <DialogHeader><DialogTitle>{t("editTitle",{value:label(current)})}</DialogTitle><DialogDescription>{t("editHelp",{list:t(`list_${list.key}`)})}</DialogDescription></DialogHeader>
  <form noValidate onSubmit={save} className="space-y-5"><fieldset disabled={busy} className="space-y-5">
   <div className="flex items-center gap-3 rounded-xl border p-3"><span className={`request-type-icon tone-${preview.tone}`}><Preview aria-hidden="true"/></span><div className="min-w-0"><p className="font-medium">{form.nameTr.trim()&&!locale.startsWith("en")||form.nameEn.trim()&&locale.startsWith("en")?(locale.startsWith("en")?form.nameEn:form.nameTr).trim():label({...current,nameTr:null,nameEn:null})}</p><p className="text-xs text-muted-foreground">{t("code")}: <code>{current.code}</code></p></div></div>
   {(["nameTr","nameEn"] as const).map(field=><div key={field}><Label htmlFor={`definition-${field}`}>{t(field)}</Label><Input id={`definition-${field}`} maxLength={maxDefinitionName} value={form[field]} placeholder={label({...current,nameTr:null,nameEn:null})} onChange={e=>setForm({...form,[field]:e.target.value})} aria-invalid={!!issue(field)} aria-describedby={`definition-${field}-help`}/><p id={`definition-${field}-help`} className="mt-1 text-xs text-muted-foreground">{t("nameHelp")}</p>{issue(field)&&<FieldError>{issue(field)}</FieldError>}</div>)}
   <div role="radiogroup" aria-labelledby="definition-icon-label"><p id="definition-icon-label" className="mb-2 text-sm font-medium">{t("icon")}</p><div className="grid grid-cols-6 gap-2 sm:grid-cols-8">
    <button type="button" role="radio" aria-checked={form.iconKey===""} aria-label={t("defaultIcon")} title={t("defaultIcon")} onClick={()=>setForm({...form,iconKey:""})} className={"grid aspect-square place-items-center rounded-lg border "+(form.iconKey===""?"border-primary bg-accent text-primary":"")}><defaultStyle.icon aria-hidden="true" className="size-4"/></button>
    {definitionIconKeys.map(key=>{const I=iconByKey[key];return <button key={key} type="button" role="radio" aria-checked={form.iconKey===key} aria-label={key} title={key} onClick={()=>setForm({...form,iconKey:key})} className={"grid aspect-square place-items-center rounded-lg border "+(form.iconKey===key?"border-primary bg-accent text-primary":"")}><I aria-hidden="true" className="size-4"/></button>;})}
   </div>{issue("iconKey")&&<FieldError>{issue("iconKey")}</FieldError>}</div>
   <div role="radiogroup" aria-labelledby="definition-tone-label"><p id="definition-tone-label" className="mb-2 text-sm font-medium">{t("tone")}</p><div className="flex flex-wrap gap-2">
    {(["",...definitionTones] as const).map(tone=><button key={tone||"default"} type="button" role="radio" aria-checked={form.tone===tone} onClick={()=>setForm({...form,tone})} className={"request-type rounded-lg border px-2 py-1 text-sm "+(form.tone===tone?"border-primary":"")}><span className={`request-type-icon tone-${tone||defaultStyle.tone}`}><Preview aria-hidden="true"/></span>{t(tone?`tone_${tone}`:"defaultTone")}</button>)}
   </div>{issue("tone")&&<FieldError>{issue("tone")}</FieldError>}</div>
   <div><Label htmlFor="definition-sortOrder">{t("sortOrder")}</Label><Input id="definition-sortOrder" inputMode="numeric" maxLength={3} value={form.sortOrder} onChange={e=>setForm({...form,sortOrder:e.target.value})} aria-invalid={!!issue("sortOrder")} aria-describedby="definition-sortOrder-help"/><p id="definition-sortOrder-help" className="mt-1 text-xs text-muted-foreground">{t("sortOrderHelp",{max:maxDefinitionOrder})}</p>{issue("sortOrder")&&<FieldError>{issue("sortOrder")}</FieldError>}</div>
   <div role="radiogroup" aria-labelledby="definition-status-label"><p id="definition-status-label" className="mb-2 text-sm font-medium">{ui("status")}</p><div className="flex flex-wrap gap-2">
    <Button type="button" role="radio" aria-checked={form.isEnabled} variant={form.isEnabled?"default":"outline"} onClick={()=>setForm({...form,isEnabled:true})}><CheckCircle2 aria-hidden="true"/>{ui("active")}</Button>
    <Button type="button" role="radio" aria-checked={!form.isEnabled} variant={!form.isEnabled?"default":"outline"} disabled={current.locked} onClick={()=>setForm({...form,isEnabled:false})}><PauseCircle aria-hidden="true"/>{ui("inactive")}</Button>
   </div><p className="mt-1 text-xs text-muted-foreground">{t(current.locked?"lockedHelp":"passiveHelp")}</p>{issue("isEnabled")&&<FieldError>{issue("isEnabled")}</FieldError>}</div>
  </fieldset>
  {error&&<MessageBanner kind={conflict?"warning":"error"} action={conflict?<Button type="button" variant="view" disabled={busy} onClick={()=>void reload()}><RefreshCw/>{ui("loadCurrentRecord")}</Button>:undefined}>{error}</MessageBanner>}
  <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button><Button type="submit" disabled={busy}><Save aria-hidden="true"/>{t("save")}</Button></DialogFooter>
  </form>
 </ManagementPanel></Dialog>;
}
