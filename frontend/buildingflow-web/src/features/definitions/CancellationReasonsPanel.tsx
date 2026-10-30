"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {CheckCircle2,CircleX,Lock,PauseCircle,Pencil,Plus,RefreshCw,Save,X} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Dialog,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {cancellationReasonsPath,readCancellationReason,readCancellationReasons,reasonBody,reasonFormErrors,type CancellationReason,type ReasonForm} from "./model";

const blank:ReasonForm={code:"",name:"",requiresExplanation:false,isEnabled:true};

// TNM 04: aidat iptal nedenleri yönetimi; yalnız SUPER_ADMIN yazar, diğer yöneticiler listeyi görür.
export function CancellationReasonsPanel({canEdit,trigger,onClose}:{canEdit:boolean;trigger:HTMLElement|null;onClose:()=>void}){
 const t=useTranslations("definitions"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();
 const [rows,setRows]=useState<CancellationReason[]|null>(null),[loadError,setLoadError]=useState("");
 const [editing,setEditing]=useState<CancellationReason|"new"|null>(null),[form,setForm]=useState<ReasonForm>(blank),[invalid,setInvalid]=useState<string[]>([]);
 const [busy,setBusy]=useState(false),[error,setError]=useState(""),[conflict,setConflict]=useState(false),[fields,setFields]=useState<Record<string,string[]>>({});const lock=useRef(false);
 const load=useCallback(async()=>{try{setRows(readCancellationReasons(await api<unknown>(cancellationReasonsPath)));setLoadError("");}catch(e){setLoadError(errorMessage(e,locale));}},[api,locale]);
 useEffect(()=>{void load();},[load]);
 function start(target:CancellationReason|"new"){setEditing(target);setForm(target==="new"?blank:{code:target.code,name:target.name,requiresExplanation:target.requiresExplanation,isEnabled:target.isEnabled});setInvalid([]);setError("");setConflict(false);setFields({});}
 async function save(e:React.FormEvent){
  e.preventDefault();if(lock.current||!editing)return;const create=editing==="new";
  const problems=reasonFormErrors(form,create);setInvalid(problems);if(problems.length)return;
  lock.current=true;setBusy(true);setError("");setConflict(false);setFields({});
  try{const saved=readCancellationReason(await api<unknown>(create?cancellationReasonsPath:`${cancellationReasonsPath}/${editing.id}`,{method:create?"POST":"PUT",body:JSON.stringify(reasonBody(form,create?0:editing.version,create))}));
   setRows(current=>current&&(current.some(r=>r.id===saved.id)?current.map(r=>r.id===saved.id?saved:r):[...current,saved].sort((a,b)=>a.code.localeCompare(b.code))));setEditing(null);notice.success(t("saved"));}
  catch(err){setError(errorMessage(err,locale));setFields(err instanceof ApiError?err.errors:{});setConflict(err instanceof ApiError&&err.status===409&&!create);}
  finally{lock.current=false;setBusy(false);}
 }
 const issue=(field:string)=>fields[field]?.join(" ")||(invalid.includes(field)?t(`reasonInvalid_${field}`):"");
 const locked=editing&&editing!=="new"&&editing.locked;
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel className="grid-cols-1 [&>*]:min-w-0" showCloseButton={!busy} onCloseAutoFocus={e=>{e.preventDefault();requestAnimationFrame(()=>trigger?.isConnected&&trigger.focus());}} onEscapeKeyDown={e=>{if(busy)e.preventDefault();}} onPointerDownOutside={e=>{if(busy)e.preventDefault();}}>
  <DialogHeader><DialogTitle>{t("catalog_DUES_CANCELLATION_REASONS")}</DialogTitle><DialogDescription>{t("reasonsHelp")}</DialogDescription></DialogHeader>
  {loadError&&<MessageBanner action={<Button type="button" variant="view" onClick={()=>void load()}><RefreshCw/>{t("refresh")}</Button>}>{loadError}</MessageBanner>}
  {!rows&&!loadError&&<p role="status" className="text-sm text-muted-foreground">{ui("loading")}</p>}
  {rows&&<ul className="space-y-2" aria-label={t("catalog_DUES_CANCELLATION_REASONS")}>{rows.map(r=><li key={r.id} className={"flex flex-wrap items-center gap-2 rounded-xl border p-3"+(r.isEnabled?"":" opacity-70")}>
   <span className="request-type-icon tone-red"><CircleX aria-hidden="true"/></span>
   <div className="min-w-0 flex-1"><p className="font-medium wrap-anywhere">{r.name}</p><p className="text-xs text-muted-foreground"><code>{r.code}</code> · {t("usageCount",{count:r.usageCount})}</p></div>
   <div className="flex flex-wrap gap-1.5">{r.locked&&<ToneBadge tone="neutral" icon={Lock}>{t("locked")}</ToneBadge>}{r.requiresExplanation&&<ToneBadge tone="info">{t("explanationRequired")}</ToneBadge>}<ToneBadge tone={r.isEnabled?"success":"neutral"} icon={r.isEnabled?CheckCircle2:PauseCircle}>{r.isEnabled?ui("active"):ui("inactive")}</ToneBadge></div>
   {canEdit&&<Button type="button" variant="edit" size="sm" disabled={busy} onClick={()=>start(r)} aria-label={t("editValue",{value:r.name})}><Pencil aria-hidden="true"/>{t("edit")}</Button>}
  </li>)}</ul>}
  {canEdit&&!editing&&rows&&<Button type="button" variant="outline" onClick={()=>start("new")}><Plus aria-hidden="true"/>{t("newReason")}</Button>}
  {canEdit&&editing&&<form noValidate onSubmit={save} className="space-y-4 rounded-xl border p-4"><fieldset disabled={busy} className="space-y-4">
   <h3 className="font-semibold">{editing==="new"?t("newReason"):t("editTitle",{value:editing.name})}</h3>
   <div><Label htmlFor="reason-code">{t("code")}</Label><Input id="reason-code" maxLength={50} value={form.code} readOnly={editing!=="new"} onChange={e=>setForm({...form,code:e.target.value.toUpperCase()})} aria-invalid={!!issue("code")} aria-describedby="reason-code-help"/><p id="reason-code-help" className="mt-1 text-xs text-muted-foreground">{t(editing==="new"?"reasonCodeHelp":"reasonCodeFixed")}</p>{issue("code")&&<FieldError>{issue("code")}</FieldError>}</div>
   <div><Label htmlFor="reason-name">{t("reasonName")}</Label><Input id="reason-name" maxLength={100} value={form.name} onChange={e=>setForm({...form,name:e.target.value})} aria-invalid={!!issue("name")}/>{issue("name")&&<FieldError>{issue("name")}</FieldError>}</div>
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.requiresExplanation} disabled={!!locked} onChange={e=>setForm({...form,requiresExplanation:e.target.checked})}/>{t("explanationRequired")}</label>
   <div role="radiogroup" aria-labelledby="reason-status-label"><p id="reason-status-label" className="mb-2 text-sm font-medium">{ui("status")}</p><div className="flex flex-wrap gap-2">
    <Button type="button" role="radio" aria-checked={form.isEnabled} variant={form.isEnabled?"default":"outline"} onClick={()=>setForm({...form,isEnabled:true})}><CheckCircle2 aria-hidden="true"/>{ui("active")}</Button>
    <Button type="button" role="radio" aria-checked={!form.isEnabled} variant={!form.isEnabled?"default":"outline"} disabled={!!locked} onClick={()=>setForm({...form,isEnabled:false})}><PauseCircle aria-hidden="true"/>{ui("inactive")}</Button>
   </div><p className="mt-1 text-xs text-muted-foreground">{t(locked?"reasonLockedHelp":"reasonPassiveHelp")}</p>{issue("isEnabled")&&<FieldError>{issue("isEnabled")}</FieldError>}</div>
  </fieldset>
  {error&&<MessageBanner kind={conflict?"warning":"error"} action={conflict?<Button type="button" variant="view" disabled={busy} onClick={()=>{void load();setEditing(null);}}><RefreshCw/>{ui("loadCurrentRecord")}</Button>:undefined}>{error}</MessageBanner>}
  <div className="flex flex-wrap justify-end gap-2"><Button type="button" variant="outline" disabled={busy} onClick={()=>setEditing(null)}><X aria-hidden="true"/>{t("cancel")}</Button><Button type="submit" disabled={busy}><Save aria-hidden="true"/>{t("save")}</Button></div>
  </form>}
  <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}
