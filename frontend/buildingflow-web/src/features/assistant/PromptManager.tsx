"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {ArrowLeft,CheckCircle2,PauseCircle,Pencil,Plus,RefreshCw,Save,Trash2} from "lucide-react";
import {ApiError,useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Skeleton} from "@/components/ui/skeleton";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {StatusBadge,ToneBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {assistantBadgeTone,assistantToolStyle} from "./assistant-style";
import {assistantCategories,assistantPath,assistantTools,emptyPromptForm,promptBody,promptForm,promptFormValid,promptTitle,readPrompt,readPrompts,type AssistantPrompt,type AssistantPromptForm} from "./model";

const adminPath=`${assistantPath}/admin/prompts`;

// Global ready questions; SUPER_ADMIN and SITE_ADMIN only. Deleting keeps the soft-deleted row and its audit fields.
export function PromptManager({trigger,onClose,onChanged}:{trigger:HTMLElement|null;onClose:()=>void;onChanged:()=>void}){
 const t=useTranslations("assistant"),ui=useTranslations("ui"),locale=useLocale() as "tr"|"en",api=useAuthorizedApi(),notice=useNotification();
 const [items,setItems]=useState<AssistantPrompt[]|null>(null),[error,setError]=useState(""),[editing,setEditing]=useState<AssistantPrompt|"new"|null>(null),[removing,setRemoving]=useState<AssistantPrompt|null>(null);
 const [busy,setBusy]=useState(false),[form,setForm]=useState<AssistantPromptForm>(emptyPromptForm()),[fields,setFields]=useState<Record<string,string[]>>({}),[invalid,setInvalid]=useState(false),[conflict,setConflict]=useState(false),[formError,setFormError]=useState("");
 const lock=useRef(false);
 const load=useCallback(async()=>{setError("");try{setItems(readPrompts(await api<unknown>(adminPath)));}catch(e){setError(errorMessage(e,locale));}},[api,locale]);
 useEffect(()=>{void load();},[load]);
 function open(value:AssistantPrompt|"new"){setEditing(value);setForm(value==="new"?emptyPromptForm(((items?.at(-1)?.sortOrder??490)+10)%1000):promptForm(value));setFields({});setInvalid(false);setConflict(false);setFormError("");}
 async function save(e:React.FormEvent){
  e.preventDefault();if(lock.current||!editing)return;if(!promptFormValid(form)){setInvalid(true);return;}
  lock.current=true;setBusy(true);setFields({});setFormError("");setConflict(false);
  try{
   const body=JSON.stringify(promptBody(form,editing==="new"?0:editing.version));
   readPrompt(await api<unknown>(editing==="new"?adminPath:`${adminPath}/${editing.id}`,{method:editing==="new"?"POST":"PUT",body}));
   notice.success(t("promptSaved"));setEditing(null);onChanged();await load();
  }catch(err){setFormError(errorMessage(err,locale));setFields(err instanceof ApiError?err.errors:{});setConflict(err instanceof ApiError&&err.status===409);}
  finally{lock.current=false;setBusy(false);}
 }
 async function remove(value:AssistantPrompt){
  if(lock.current)return;lock.current=true;setBusy(true);
  try{await api<unknown>(`${adminPath}/${value.id}?version=${value.version}`,{method:"DELETE"});notice.success(t("promptDeleted"));setRemoving(null);onChanged();await load();}
  catch{setRemoving(null);await load();}finally{lock.current=false;setBusy(false);}
 }
 const issue=(field:keyof AssistantPromptForm)=>fields[field]?.join(" ")||(invalid&&!fieldValid(form,field)?t(`invalid_${field}`):"");
 return <Dialog open onOpenChange={value=>{if(!value&&!busy)onClose();}}><ManagementPanel className="grid-cols-1 sm:max-w-xl [&>*]:min-w-0" showCloseButton={!busy}
  onCloseAutoFocus={e=>{e.preventDefault();requestAnimationFrame(()=>trigger?.isConnected&&trigger.focus());}} onEscapeKeyDown={e=>{if(busy)e.preventDefault();}} onPointerDownOutside={e=>{if(busy)e.preventDefault();}}>
  <DialogHeader><DialogTitle>{editing?t(editing==="new"?"newPrompt":"editPrompt"):t("managePrompts")}</DialogTitle><DialogDescription>{t("manageHelp")}</DialogDescription></DialogHeader>
  {!editing?<div className="space-y-3">
   <div className="flex flex-wrap gap-2"><Button onClick={()=>open("new")} disabled={!items}><Plus aria-hidden="true"/>{t("newPrompt")}</Button><Button variant="view" onClick={()=>void load()}><RefreshCw aria-hidden="true"/>{ui("refresh")}</Button></div>
   {error&&<MessageBanner presentation="inline">{error}</MessageBanner>}
   {!items?<div className="space-y-2" aria-busy="true">{Array.from({length:4},(_,i)=><Skeleton key={i} className="h-16 rounded-lg"/>)}</div>
   :<ul className="space-y-2">{items.map(p=>{const style=assistantToolStyle[p.toolKey];return <li key={p.id} className="flex items-start gap-3 rounded-xl border p-3">
    <span className={`request-type-icon tone-${style.tone}`}><style.icon aria-hidden="true"/></span>
    <div className="min-w-0 flex-1"><p className="font-medium wrap-anywhere">{promptTitle(p,locale)}</p><p className="text-xs text-muted-foreground"><code>{p.code}</code> · {t(`category_${p.category}`)} · {t("sortOrderShort",{value:p.sortOrder})}</p>
     <div className="mt-1 flex flex-wrap gap-1"><StatusBadge enabled={p.isEnabled}/><ToneBadge tone={assistantBadgeTone[style.tone]} icon={style.icon}>{t(`tool_${p.toolKey}`)}</ToneBadge></div></div>
    <div className="flex shrink-0 gap-1"><Button size="icon" variant="edit" className="size-11" aria-label={t("editPromptNamed",{name:promptTitle(p,locale)})} title={t("editPrompt")} onClick={()=>open(p)}><Pencil aria-hidden="true"/></Button>
     <Button size="icon" variant="danger" className="size-11" aria-label={t("deletePromptNamed",{name:promptTitle(p,locale)})} title={t("deletePrompt")} onClick={()=>setRemoving(p)}><Trash2 aria-hidden="true"/></Button></div>
   </li>;})}</ul>}
  </div>
  :<form noValidate onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">
   <div><Label htmlFor="prompt-code">{t("code")}</Label><Input id="prompt-code" maxLength={40} value={form.code} disabled={editing!=="new"} onChange={e=>setForm({...form,code:e.target.value.toUpperCase()})} aria-invalid={!!issue("code")} aria-describedby="prompt-code-help"/><p id="prompt-code-help" className="mt-1 text-xs text-muted-foreground">{t("codeHelp")}</p>{issue("code")&&<FieldError>{issue("code")}</FieldError>}</div>
   <div className="grid gap-4 sm:grid-cols-2">
    <div><Label htmlFor="prompt-category">{t("categoryLabel")}</Label><select id="prompt-category" className={toolbarSelectClass+" w-full"} value={form.category} onChange={e=>setForm({...form,category:e.target.value as AssistantPromptForm["category"]})}>{assistantCategories.map(c=><option key={c} value={c}>{t(`category_${c}`)}</option>)}</select></div>
    <div><Label htmlFor="prompt-tool">{t("toolLabel")}</Label><select id="prompt-tool" className={toolbarSelectClass+" w-full"} value={form.toolKey} onChange={e=>setForm({...form,toolKey:e.target.value as AssistantPromptForm["toolKey"]})} aria-describedby="prompt-tool-help">{assistantTools.map(tool=><option key={tool} value={tool}>{t(`tool_${tool}`)}</option>)}</select></div>
   </div>
   <p id="prompt-tool-help" className="text-xs text-muted-foreground">{t("toolHelp")}</p>
   {(["titleTr","titleEn"] as const).map(field=><div key={field}><Label htmlFor={`prompt-${field}`}>{t(field)}</Label><Input id={`prompt-${field}`} maxLength={120} value={form[field]} onChange={e=>setForm({...form,[field]:e.target.value})} aria-invalid={!!issue(field)}/>{issue(field)&&<FieldError>{issue(field)}</FieldError>}</div>)}
   {(["textTr","textEn"] as const).map(field=><div key={field}><Label htmlFor={`prompt-${field}`}>{t(field)}</Label><textarea id={`prompt-${field}`} rows={2} maxLength={500} value={form[field]} onChange={e=>setForm({...form,[field]:e.target.value})} aria-invalid={!!issue(field)} className="w-full rounded-lg border bg-background p-3 text-sm focus-visible:outline-2 focus-visible:outline-ring"/>{issue(field)&&<FieldError>{issue(field)}</FieldError>}</div>)}
   <div><Label htmlFor="prompt-sortOrder">{t("sortOrder")}</Label><Input id="prompt-sortOrder" inputMode="numeric" maxLength={3} value={form.sortOrder} onChange={e=>setForm({...form,sortOrder:e.target.value})} aria-invalid={!!issue("sortOrder")}/>{issue("sortOrder")&&<FieldError>{issue("sortOrder")}</FieldError>}</div>
   <div role="radiogroup" aria-labelledby="prompt-status-label"><p id="prompt-status-label" className="mb-2 text-sm font-medium">{ui("status")}</p><div className="flex flex-wrap gap-2">
    <Button type="button" role="radio" aria-checked={form.isEnabled} variant={form.isEnabled?"default":"outline"} onClick={()=>setForm({...form,isEnabled:true})}><CheckCircle2 aria-hidden="true"/>{ui("active")}</Button>
    <Button type="button" role="radio" aria-checked={!form.isEnabled} variant={!form.isEnabled?"default":"outline"} onClick={()=>setForm({...form,isEnabled:false})}><PauseCircle aria-hidden="true"/>{ui("inactive")}</Button></div></div>
  </fieldset>
  {formError&&<MessageBanner presentation="inline" kind={conflict?"warning":"error"} action={conflict?<Button type="button" variant="view" disabled={busy} onClick={()=>{setEditing(null);void load();}}><RefreshCw aria-hidden="true"/>{ui("loadCurrentRecord")}</Button>:undefined}>{formError}</MessageBanner>}
  <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={()=>setEditing(null)}><ArrowLeft aria-hidden="true"/>{t("backToList")}</Button><Button type="submit" disabled={busy}><Save aria-hidden="true"/>{busy?ui("saving"):ui("save")}</Button></DialogFooter>
  </form>}
 <Dialog open={!!removing} onOpenChange={value=>{if(!value&&!busy)setRemoving(null);}}><DialogContent><DialogHeader><DialogTitle>{t("deletePrompt")}</DialogTitle><DialogDescription>{t("deleteHelp",{name:removing?promptTitle(removing,locale):""})}</DialogDescription></DialogHeader>
  <DialogFooter><Button variant="outline" disabled={busy} onClick={()=>setRemoving(null)}>{ui("cancel")}</Button><Button variant="destructive" disabled={busy} onClick={()=>removing&&void remove(removing)}><Trash2 aria-hidden="true"/>{t("deletePrompt")}</Button></DialogFooter></DialogContent></Dialog>
 </ManagementPanel>
 </Dialog>;
}

function fieldValid(form:AssistantPromptForm,field:keyof AssistantPromptForm){
 const body=promptBody(form,0);
 if(field==="code")return /^[A-Z][A-Z0-9_]{1,39}$/.test(body.code);
 if(field==="sortOrder")return body.sortOrder>=0&&body.sortOrder<=999;
 if(field==="titleTr"||field==="titleEn")return body[field].length>0;
 if(field==="textTr"||field==="textEn")return body[field].length>0;
 return true;
}
