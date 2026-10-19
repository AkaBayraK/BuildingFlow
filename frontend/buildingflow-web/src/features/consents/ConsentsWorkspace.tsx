"use client";
import Link from "next/link";
import {useEffect,useMemo,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {ShieldCheck,Plus,Pencil,Trash2,Send,Eye,History,FileText,RefreshCw} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage,type Property} from "@/services/api";
import {parsePropertyPage} from "@/services/management-contracts";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Badge} from "@/components/ui/badge";
import {Dialog,DialogContent,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,DetailField,StatusBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {useSearch} from "@/features/people/useRows";
import {formatDateTime} from "@/lib/format";
import {parseMarkup} from "./markup";
import {ConsentMarkupView} from "./ConsentMarkupView";
import {readTypes,readType,readText,readRecord,readHistory,readPage,typeName,recordTypeName,consentKinds,consentRowStates,consentCodePattern,type ConsentType,type ConsentText,type ConsentRecord,type ConsentHistory,type ConsentKind,type ConsentRowState,type ConsentSummaryState,type Page} from "./model";

type Tab="types"|"texts"|"records";
const root="/api/v1/consents";
const stateVariant:Record<ConsentSummaryState,"default"|"secondary"|"destructive"|"outline">={ACTIVE:"default",OUTDATED:"outline",WITHDRAWN:"destructive",SUPERSEDED:"secondary",NONE:"secondary"};

function useSaving(){const lock=useRef(false);const [busy,setBusy]=useState(false);
 async function run(action:()=>Promise<void>){if(lock.current)return;lock.current=true;setBusy(true);try{await action();}finally{lock.current=false;setBusy(false);}}
 return {busy,run};}

export function ConsentStateBadge({state}:{state:ConsentSummaryState}){const t=useTranslations("consents");return <Badge variant={stateVariant[state]}>{t(`state_${state}`)}</Badge>;}

export function ConsentsWorkspace(){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r)),admin=identity.hasRealmRole("SUPER_ADMIN");
 const [tab,setTab]=useState<Tab>("types"),[types,setTypes]=useState<ConsentType[]|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 const refresh=()=>setRevision(n=>n+1);
 useEffect(()=>{const c=new AbortController();setError("");if(manager)void api<unknown>(root+"/types",{signal:c.signal}).then(v=>{if(!c.signal.aborted)setTypes(readTypes(v));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,revision,locale,manager]);
 if(!manager)return <div className="py-10"><MessageBanner>{ui("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 return <div className="py-8"><WorkspaceHeading title={t("title")} description={t("help")} scope={t("scopeAll")} icon={ShieldCheck}/>
  {error&&<MessageBanner className="mb-4" action={<Button variant="view" onClick={refresh}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
  <div role="tablist" aria-label={t("title")} className="mb-4 flex flex-wrap gap-2">{(["types","texts","records"] as Tab[]).map(value=><Button key={value} role="tab" aria-selected={tab===value} variant={tab===value?"default":"outline"} onClick={()=>setTab(value)}>{t(`tab_${value}`)}</Button>)}</div>
  {!types&&!error?<p role="status">{ui("loading")}</p>:types&&<div role="tabpanel">
   {tab==="types"&&<TypesTab types={types} admin={admin} onChanged={refresh}/>}
   {tab==="texts"&&<TextsTab types={types} admin={admin} onChanged={refresh}/>}
   {tab==="records"&&<RecordsTab types={types}/>}
  </div>}
 </div>;
}

function TypesTab({types,admin,onChanged}:{types:ConsentType[];admin:boolean;onChanged:()=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale();const [edit,setEdit]=useState<ConsentType|"new"|null>(null),[remove,setRemove]=useState<ConsentType|null>(null);
 const data:Page<ConsentType>={items:types,page:1,pageSize:Math.max(1,types.length),totalCount:types.length};
 const columns=useMemo<ColumnDef<ConsentType>[]>(()=>[
  {id:"name",header:t("typeName"),cell:({row})=><span className="flex flex-col"><strong>{typeName(row.original,locale)}</strong><small className="font-mono text-muted-foreground">{row.original.code}</small></span>},
  {id:"kind",header:t("kind"),cell:({row})=><span className="flex flex-wrap gap-1"><Badge variant="outline">{t(`kind_${row.original.kind}`)}</Badge>{row.original.isSystem&&<Badge variant="secondary">{t("system")}</Badge>}</span>},
  {id:"current",header:t("currentText"),cell:({row})=>row.original.currentText?<span className="flex flex-col"><span>{t("versionValue",{version:row.original.currentText.textVersion})}</span><small className="text-muted-foreground">{formatDateTime(row.original.currentText.publishedAt,locale)}</small></span>:<span className="text-muted-foreground">{t("noText")}</span>},
  {id:"status",header:ui("status"),cell:({row})=><StatusBadge enabled={row.original.isEnabled}/>},
  ...(admin?[{id:"actions",header:"",cell:({row}:{row:{original:ConsentType}})=><TypeActions type={row.original} onEdit={setEdit} onRemove={setRemove}/>}]:[]),
 ],[t,ui,locale,admin]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm">
  <div className="flex flex-wrap items-center justify-between gap-2 p-4"><p className="text-sm text-muted-foreground">{t("typesHelp")}</p>{admin&&<Button onClick={()=>setEdit("new")}><Plus/>{t("newType")}</Button>}</div>
  <RecordList compact data={data} columns={columns} page={1} onPage={()=>{}} loading={false} error="" onRetry={onChanged} empty={t("noTypes")} card={x=><><div className="flex items-start justify-between gap-2"><span className="flex flex-col"><strong>{typeName(x,locale)}</strong><small className="font-mono text-muted-foreground">{x.code}</small></span><StatusBadge enabled={x.isEnabled}/></div><p className="text-sm">{t(`kind_${x.kind}`)}{x.isSystem?` · ${t("system")}`:""} · {x.currentText?t("versionValue",{version:x.currentText.textVersion}):t("noText")}</p>{admin&&<TypeActions type={x} onEdit={setEdit} onRemove={setRemove}/>}</>}/>
  {edit&&<TypeDialog type={edit==="new"?null:edit} onClose={()=>setEdit(null)} onSaved={onChanged}/>}
  {remove&&<DeleteTypeDialog type={remove} onClose={()=>setRemove(null)} onDeleted={onChanged}/>}
 </section>;
}

function TypeActions({type,onEdit,onRemove}:{type:ConsentType;onEdit:(value:ConsentType)=>void;onRemove:(value:ConsentType)=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui");
 return <span className="flex flex-wrap gap-1"><Button variant="edit" size="sm" onClick={()=>onEdit(type)}><Pencil/>{t("edit")}</Button>{!type.isSystem&&type.textCount===0&&<Button variant="danger" size="sm" onClick={()=>onRemove(type)}><Trash2/>{ui("delete")}</Button>}</span>;
}

function TypeDialog({type,onClose,onSaved}:{type:ConsentType|null;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [code,setCode]=useState(type?.code??""),[nameTr,setNameTr]=useState(type?.nameTr??""),[nameEn,setNameEn]=useState(type?.nameEn??""),[kind,setKind]=useState<ConsentKind>(type?.kind??"EXPLICIT_CONSENT"),[sortOrder,setSortOrder]=useState(String(type?.sortOrder??100)),[enabled,setEnabled]=useState(type?.isEnabled??true),[error,setError]=useState("");
 function save(e:React.FormEvent){e.preventDefault();setError("");const order=Number(sortOrder);
  if((!type&&!consentCodePattern.test(code.trim()))||!nameTr.trim()||!nameEn.trim()||nameTr.trim().length>150||nameEn.trim().length>150||!Number.isSafeInteger(order)||order<0||order>10000){setError(t("typeInvalid"));return;}
  void run(async()=>{try{
   const body=type?{nameTr:nameTr.trim(),nameEn:nameEn.trim(),sortOrder:order,isEnabled:enabled,version:type.version}:{code:code.trim(),nameTr:nameTr.trim(),nameEn:nameEn.trim(),kind,sortOrder:order};
   readType(await api(type?`${root}/types/${type.id}`:`${root}/types`,{method:type?"PUT":"POST",body:JSON.stringify(body)}));notice.success(t("typeSaved"));onSaved();onClose();
  }catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(type?"editType":"newType")}</DialogTitle><DialogDescription>{t("typeDialogHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="consent-type-code">{t("code")} *</Label><Input id="consent-type-code" className="font-mono" maxLength={40} value={code} disabled={!!type} onChange={e=>setCode(e.target.value.toUpperCase())} aria-describedby="consent-type-code-help"/><p id="consent-type-code-help" className="text-xs text-muted-foreground">{t("codeHelp")}</p>
   <Label htmlFor="consent-type-name-tr">{t("nameTr")} *</Label><Input id="consent-type-name-tr" maxLength={150} value={nameTr} onChange={e=>setNameTr(e.target.value)}/>
   <Label htmlFor="consent-type-name-en">{t("nameEn")} *</Label><Input id="consent-type-name-en" maxLength={150} value={nameEn} onChange={e=>setNameEn(e.target.value)}/>
   <Label htmlFor="consent-type-kind">{t("kind")}</Label><select id="consent-type-kind" className={toolbarSelectClass+" w-full"} value={kind} disabled={!!type} onChange={e=>setKind(e.target.value as ConsentKind)}>{consentKinds.map(k=><option key={k} value={k}>{t(`kind_${k}`)}</option>)}</select><p className="text-xs text-muted-foreground">{t(`kindHelp_${kind}`)}</p>
   <Label htmlFor="consent-type-order">{t("sortOrder")} *</Label><Input id="consent-type-order" inputMode="numeric" value={sortOrder} onChange={e=>setSortOrder(e.target.value)}/>
   {type&&<label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={enabled} disabled={type.isSystem} onChange={e=>setEnabled(e.target.checked)}/>{ui("active")}{type.isSystem&&<span className="text-xs text-muted-foreground">({t("systemAlwaysActive")})</span>}</label>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function DeleteTypeDialog({type,onClose,onDeleted}:{type:ConsentType;onClose:()=>void;onDeleted:()=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();const [error,setError]=useState("");
 function remove(){void run(async()=>{try{await api(`${root}/types/${type.id}?version=${type.version}`,{method:"DELETE"});notice.success(t("typeDeleted"));onDeleted();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><DialogContent showCloseButton={!busy} className="rounded-2xl bg-card"><DialogHeader><DialogTitle>{t("deleteType")}</DialogTitle><DialogDescription>{t("deleteTypeHelp",{name:typeName(type,locale)})}</DialogDescription></DialogHeader>{error&&<MessageBanner>{error}</MessageBanner>}<DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button variant="destructive" disabled={busy} onClick={remove}><Trash2/>{ui("delete")}</Button></DialogFooter></DialogContent></Dialog>;
}

function TextsTab({types,admin,onChanged}:{types:ConsentType[];admin:boolean;onChanged:()=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [typeId,setTypeId]=useState(types[0]?.id??""),[page,setPage]=useState(1),[data,setData]=useState<Page<ConsentText>|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0),[view,setView]=useState<ConsentText|null>(null),[publish,setPublish]=useState(false);
 const type=types.find(x=>x.id===typeId);
 useEffect(()=>{if(!typeId){setLoading(false);return;}const c=new AbortController();setLoading(true);setError("");void api<unknown>(`${root}/types/${typeId}/texts?page=${page}&pageSize=10`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,row=>readText(row,typeId)));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,typeId,page,retry,locale]);
 const columns=useMemo<ColumnDef<ConsentText>[]>(()=>[
  {id:"version",header:t("version"),cell:({row})=>t("versionValue",{version:row.original.textVersion})},
  {id:"title",header:t("textTitle"),cell:({row})=><span className="line-clamp-2 max-w-md break-words">{row.original.title}</span>},
  {id:"reconsent",header:t("reconsent"),cell:({row})=><Badge variant={row.original.requiresReconsent?"default":"secondary"}>{t(row.original.requiresReconsent?"reconsentRequired":"reconsentNotRequired")}</Badge>},
  {id:"published",header:t("publishedAt"),cell:({row})=><span className="flex flex-col"><span>{formatDateTime(row.original.publishedAt,locale)}</span><small className="text-muted-foreground">{row.original.publishedBy}</small></span>},
  {id:"actions",header:"",cell:({row})=><Button variant="view" size="sm" onClick={()=>setView(row.original)}><Eye/>{t("view")}</Button>},
 ],[t,locale]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm">
  <div className="flex flex-wrap items-center justify-between gap-2 p-4"><label className="flex min-w-0 items-center gap-2 text-sm">{t("type")}<select className={toolbarSelectClass} value={typeId} onChange={e=>{setTypeId(e.target.value);setPage(1);}}>{types.map(x=><option key={x.id} value={x.id}>{typeName(x,locale)}{x.isEnabled?"":` (${ui("inactive")})`}</option>)}</select></label>
   {admin&&type&&<Button disabled={!type.isEnabled} title={type.isEnabled?undefined:t("typeDisabledPublish")} onClick={()=>setPublish(true)}><Send/>{t("publish")}</Button>}</div>
  {type&&<p className="px-4 pb-2 text-xs text-muted-foreground">{t("textsHelp")}</p>}
  <RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t("noTexts")} card={x=><><div className="flex items-center justify-between gap-2"><strong>{t("versionValue",{version:x.textVersion})}</strong><Badge variant={x.requiresReconsent?"default":"secondary"}>{t(x.requiresReconsent?"reconsentRequired":"reconsentNotRequired")}</Badge></div><p className="break-words text-sm">{x.title}</p><p className="text-xs text-muted-foreground">{formatDateTime(x.publishedAt,locale)} · {x.publishedBy}</p><Button variant="view" size="sm" onClick={()=>setView(x)}><Eye/>{t("view")}</Button></>}/>
  {view&&<TextDialog text={view} onClose={()=>setView(null)}/>}
  {publish&&type&&<PublishDialog type={type} onClose={()=>setPublish(false)} onPublished={()=>{setPage(1);setRetry(n=>n+1);onChanged();}}/>}
 </section>;
}

function TextDialog({text,onClose}:{text:ConsentText;onClose:()=>void}){
 const t=useTranslations("consents"),locale=useLocale();const parsed=useMemo(()=>parseMarkup(text.body),[text.body]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><DialogTitle className="break-words">{text.title}</DialogTitle><DialogDescription>{t("versionValue",{version:text.textVersion})} · {formatDateTime(text.publishedAt,locale)} · {text.publishedBy}</DialogDescription></DialogHeader>
  <div className="space-y-4">{parsed.error?<MessageBanner presentation="inline">{t("renderFailed")}</MessageBanner>:<ConsentMarkupView blocks={parsed.blocks} className="rounded-lg border p-4"/>}
   <DetailField label={t("bodyHash")}><span className="break-all font-mono text-xs">{text.bodySha256}</span></DetailField></div></ManagementPanel></Dialog>;
}

function PublishDialog({type,onClose,onPublished}:{type:ConsentType;onClose:()=>void;onPublished:()=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [title,setTitle]=useState(""),[body,setBody]=useState(""),[reconsent,setReconsent]=useState(true),[error,setError]=useState("");const operation=useRef<{key:string;id:string}|null>(null);
 const parsed=useMemo(()=>parseMarkup(body),[body]);const first=type.textCount===0;
 function save(e:React.FormEvent){e.preventDefault();setError("");
  if(!title.trim()||title.trim().length>200){setError(t("titleInvalid"));return;}
  if(parsed.error){setError(t(`markupError_${parsed.error}`));return;}
  // A lost response is retried with the same operation id so the API replays instead of publishing twice.
  const key=[title.trim(),body,String(reconsent)].join("\n");if(operation.current?.key!==key)operation.current={key,id:crypto.randomUUID()};const operationId=operation.current.id;
  void run(async()=>{try{const text=readText(await api(`${root}/types/${type.id}/texts`,{method:"POST",body:JSON.stringify({operationId,title:title.trim(),body,requiresReconsent:reconsent})}),type.id);notice.success(t("published",{version:text.textVersion}));onPublished();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy} className="sm:max-w-3xl"><DialogHeader><DialogTitle>{t("publishTitle",{name:typeName(type,locale)})}</DialogTitle><DialogDescription>{t("publishHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="consent-text-title">{t("textTitle")} *</Label><Input id="consent-text-title" maxLength={200} value={title} onChange={e=>setTitle(e.target.value)}/>
   <Label htmlFor="consent-text-body">{t("body")} *</Label>
   <Textarea id="consent-text-body" rows={12} className="font-mono text-sm" value={body} onChange={e=>setBody(e.target.value)} aria-describedby="consent-text-format" aria-invalid={!!body&&!!parsed.error}/>
   <p id="consent-text-format" className="whitespace-pre-line text-xs text-muted-foreground">{t("formatHelp")}</p>
   {body&&parsed.error&&<p className="text-xs text-destructive">{t(`markupError_${parsed.error}`)}</p>}
   <div><p className="text-xs font-medium">{t("preview")}</p>{parsed.error?<p className="mt-1 rounded-lg border p-3 text-sm text-muted-foreground">{t("previewEmpty")}</p>:<ConsentMarkupView blocks={parsed.blocks} className="mt-1 max-h-80 overflow-auto rounded-lg border p-3"/>}</div>
   <label className="flex items-start gap-2 text-sm"><input type="checkbox" className="mt-1" checked={reconsent} disabled={first} onChange={e=>setReconsent(e.target.checked)}/><span><span className="font-medium">{t("reconsentRequired")}</span><span className="block text-xs text-muted-foreground">{t(first?"reconsentFirst":"reconsentHelp")}</span></span></label>
   <MessageBanner kind="warning" presentation="inline" announce={false}>{t("publishImmutable")}</MessageBanner>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}><Send/>{busy?ui("saving"):t("publish")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function useProperties(){
 const api=useAuthorizedApi();const [items,setItems]=useState<Property[]>([]);
 useEffect(()=>{const c=new AbortController();void api<unknown>("/api/v1/properties?page=1&pageSize=100&sortBy=name&sortDirection=asc",{signal:c.signal}).then(v=>{if(!c.signal.aborted)setItems(parsePropertyPage(v).items);}).catch(()=>{});return()=>c.abort();},[api]);
 return items;
}

function RecordsTab({types}:{types:ConsentType[]}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),properties=useProperties();
 const [typeId,setTypeId]=useState(""),[state,setState]=useState<ConsentRowState|"">(""),[propertyId,setPropertyId]=useState(""),[term,setTerm]=useState(""),search=useSearch(term),[page,setPage]=useState(1);
 const [data,setData]=useState<Page<ConsentRecord>|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0),[history,setHistory]=useState<string|null>(null);
 useEffect(()=>{setPage(1);},[typeId,state,propertyId,search]);
 useEffect(()=>{const c=new AbortController();setLoading(true);setError("");
  const query=new URLSearchParams({page:String(page),pageSize:"20",...(typeId?{typeId}:{}),...(state?{state}:{}),...(propertyId?{propertyId}:{}),...(search?{search:search.slice(0,100)}:{})});
  void api<unknown>(`${root}/records?${query}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,readRecord));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,page,typeId,state,propertyId,search,retry,locale]);
 const columns=useMemo<ColumnDef<ConsentRecord>[]>(()=>[
  {id:"person",header:t("approver"),cell:({row})=><span className="flex flex-col"><strong>{row.original.approverName}</strong><small className="break-all font-mono text-muted-foreground">{row.original.ownerUserId}</small></span>},
  {id:"type",header:t("type"),cell:({row})=><span className="flex flex-col"><span>{recordTypeName(row.original,locale)}</span><small className="text-muted-foreground">{t("versionValue",{version:row.original.textVersion})}</small></span>},
  {id:"state",header:ui("status"),cell:({row})=><ConsentStateBadge state={row.original.state}/>},
  {id:"approved",header:t("approvedAt"),cell:({row})=><span className="flex flex-col"><span>{formatDateTime(row.original.approvedAt,locale)}</span>{row.original.revokedAt&&<small className="text-muted-foreground">{t("revokedAtValue",{date:formatDateTime(row.original.revokedAt,locale)})}</small>}</span>},
  {id:"channel",header:t("channel"),cell:({row})=>t(`channel_${row.original.channel}`)+(row.original.appVersion?` · ${row.original.appVersion}`:"")},
  {id:"actions",header:"",cell:({row})=><RecordActions record={row.original} onHistory={setHistory}/>},
 ],[t,ui,locale]);
 const filtered=!!(typeId||state||propertyId||term);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm">
  <div className="flex flex-wrap items-end gap-3 p-4">
   <label className="flex min-w-0 flex-1 basis-56 flex-col gap-1 text-sm">{t("search")}<Input type="search" maxLength={100} placeholder={t("searchPlaceholder")} value={term} onChange={e=>setTerm(e.target.value)}/></label>
   <label className="flex min-w-0 flex-col gap-1 text-sm">{t("type")}<select className={toolbarSelectClass} value={typeId} onChange={e=>setTypeId(e.target.value)}><option value="">{t("allTypes")}</option>{types.map(x=><option key={x.id} value={x.id}>{typeName(x,locale)}</option>)}</select></label>
   <label className="flex min-w-0 flex-col gap-1 text-sm">{ui("status")}<select className={toolbarSelectClass} value={state} onChange={e=>setState(e.target.value as ConsentRowState|"")}><option value="">{t("allStates")}</option>{consentRowStates.map(s=><option key={s} value={s}>{t(`state_${s}`)}</option>)}</select></label>
   <label className="flex min-w-0 flex-col gap-1 text-sm">{t("property")}<select className={toolbarSelectClass} value={propertyId} onChange={e=>setPropertyId(e.target.value)}><option value="">{t("allProperties")}</option>{properties.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
   <div className="flex gap-2"><Button variant="outline" disabled={!filtered} onClick={()=>{setTypeId("");setState("");setPropertyId("");setTerm("");}}>{t("clearFilters")}</Button><Button variant="view" size="icon" aria-label={t("refresh")} onClick={()=>setRetry(n=>n+1)}><RefreshCw/></Button></div>
  </div>
  <p className="px-4 pb-2 text-xs text-muted-foreground">{t("recordsHelp")}</p>
  <RecordList compact cardBreakpoint="xl" data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t(filtered?"noRecordsFiltered":"noRecords")} card={r=><><div className="flex items-start justify-between gap-2"><strong className="break-words">{r.approverName}</strong><ConsentStateBadge state={r.state}/></div><p className="text-sm">{recordTypeName(r,locale)} · {t("versionValue",{version:r.textVersion})}</p><p className="text-xs text-muted-foreground">{formatDateTime(r.approvedAt,locale)} · {t(`channel_${r.channel}`)}</p><RecordActions record={r} onHistory={setHistory}/></>}/>
  {history&&<HistoryDialog userId={history} onClose={()=>setHistory(null)}/>}
 </section>;
}

function RecordActions({record,onHistory}:{record:ConsentRecord;onHistory:(userId:string)=>void}){
 const t=useTranslations("consents");
 return <span className="flex flex-wrap gap-1"><Button asChild variant="view" size="sm"><Link href={`/consents/records/${record.id}`}><FileText/>{t("document")}</Link></Button><Button variant="view" size="sm" onClick={()=>onHistory(record.ownerUserId)}><History/>{t("history")}</Button></span>;
}

export function HistoryDialog({userId,onClose}:{userId:string;onClose:()=>void}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();const [data,setData]=useState<ConsentHistory|null>(null),[error,setError]=useState(""),[retry,setRetry]=useState(0);
 useEffect(()=>{const c=new AbortController();setError("");void api<unknown>(`${root}/users/${encodeURIComponent(userId)}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readHistory(v,userId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,userId,retry,locale]);
 const name=data?.records[0]?.approverName;
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel><DialogHeader><DialogTitle>{t("historyTitle")}</DialogTitle><DialogDescription className="break-all">{name?`${name} · `:""}{userId}</DialogDescription></DialogHeader>
  {error?<MessageBanner action={<Button variant="view" onClick={()=>setRetry(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>:!data?<p role="status">{ui("loading")}</p>:<div className="space-y-6">
   <section><h3 className="mb-2 text-sm font-semibold">{t("currentStatus")}</h3><ul className="space-y-2">{data.summary.map(s=><li key={s.typeId} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border p-3"><span className="flex flex-col"><span className="font-medium">{typeName(s,locale)}</span><small className="text-muted-foreground">{s.approvedVersion?t("approvedVersionOf",{approved:s.approvedVersion,current:s.currentVersion??s.approvedVersion}):s.currentVersion?t("currentVersionOnly",{current:s.currentVersion}):t("noText")}</small></span><ConsentStateBadge state={s.state}/></li>)}</ul></section>
   <section><h3 className="mb-2 text-sm font-semibold">{t("timeline")}</h3>{data.records.length===0?<p className="text-sm text-muted-foreground">{t("noRecords")}</p>:<ol className="space-y-2">{data.records.map(r=><li key={r.id} className="rounded-lg border p-3 text-sm"><div className="flex flex-wrap items-center justify-between gap-2"><span className="font-medium">{recordTypeName(r,locale)} · {t("versionValue",{version:r.textVersion})}</span><ConsentStateBadge state={r.state}/></div>
    <p className="text-xs text-muted-foreground">{t("approvedAtValue",{date:formatDateTime(r.approvedAt,locale)})} · {t(`channel_${r.channel}`)}{r.revokedAt?` · ${t(r.revokeReason==="WITHDRAWN"?"withdrawnAtValue":"supersededAtValue",{date:formatDateTime(r.revokedAt,locale)})}`:""}</p>
    <Link className="text-xs text-primary underline underline-offset-2" href={`/consents/records/${r.id}`}>{t("document")}</Link></li>)}</ol>}</section>
   <p className="text-xs text-muted-foreground">{t("historyPrivate")}</p>
  </div>}</ManagementPanel></Dialog>;
}
