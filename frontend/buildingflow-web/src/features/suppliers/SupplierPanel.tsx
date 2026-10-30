"use client";
import {useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Save,Pencil,RefreshCw} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {DetailField} from "@/components/management/WorkspacePresentation";
import {formatDateTime} from "@/lib/format";
import {useCenterMutation} from "@/features/person-center/hooks";
import {kindStyle,statusStyle,supplierTabs} from "./supplier-style";
import {kinds,statuses,readSupplier,readSupplierMutation,supplierForm,supplierBody,validSupplier,type Supplier} from "./model";
import {SupplierRelations} from "./SupplierRelations";
import {SupplierDocuments} from "./SupplierDocuments";
import {SupplierWorkOrders} from "./SupplierWorkOrders";
import {SupplierQuotes} from "./SupplierQuotes";
import {SupplierContracts} from "./SupplierContracts";
import {SupplierBadge} from "./SupplierBadge";
export function SupplierPanel({tenant,target,editing,focusTarget,fallbackTarget,onClose,onSaved}:{tenant:string;target?:Supplier;editing:boolean;focusTarget:HTMLElement|null;fallbackTarget:HTMLElement|null;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("suppliers"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();
 const [workOrderId,setWorkOrderId]=useState<string>();
 const [record,setRecord]=useState(target),[detailTab,setDetailTab]=useState<keyof typeof supplierTabs>("about"),[edit,setEdit]=useState(editing||!target),[form,setForm]=useState(()=>supplierForm(target)),[invalid,setInvalid]=useState(false),[relationsBusy,setRelationsBusy]=useState(false),[reloadError,setReloadError]=useState(""),[reloading,setReloading]=useState(false);const formRef=useRef<HTMLFormElement>(null),reloadLock=useRef(false);
 const state=useCenterMutation(()=>{notice.success(t("saved"));onSaved();onClose();});const busy=state.busy||reloading||relationsBusy;
 async function reload(){if(!record||reloadLock.current)return;reloadLock.current=true;setReloading(true);try{const fresh=readSupplier(await api<unknown>(`/api/v2/tenants/${tenant}/suppliers/${record.id}`),tenant);setRecord(fresh);setForm(supplierForm(fresh));state.reset();setReloadError("");formRef.current?.querySelector<HTMLInputElement>("#supplier-name")?.focus();onSaved();}catch(e){setReloadError(errorMessage(e,locale));}finally{reloadLock.current=false;setReloading(false);}}
 async function save(e:React.FormEvent){e.preventDefault();if(!validSupplier(form)){setInvalid(true);formRef.current?.querySelector<HTMLElement>(!form.name.trim()?"#supplier-name":"#supplier-reason")?.focus();return;}await state.submit(`/api/v2/tenants/${tenant}/suppliers${record?`/${record.id}`:""}`,record?"PUT":"POST",supplierBody(form,record?.version??0),v=>readSupplierMutation(v,tenant));}
 return <Dialog open onOpenChange={v=>{if(!v&&!busy)onClose();}}><ManagementPanel className="grid-cols-1 [&>*]:min-w-0" showCloseButton={!busy} onCloseAutoFocus={e=>{e.preventDefault();requestAnimationFrame(()=>(focusTarget?.isConnected?focusTarget:fallbackTarget)?.focus());}} onEscapeKeyDown={e=>{if(busy)e.preventDefault();}} onPointerDownOutside={e=>{if(busy)e.preventDefault();}}><DialogHeader><DialogTitle>{edit?record?t("edit"):t("create"):t("detail")}</DialogTitle><DialogDescription>{t("panelHelp")}</DialogDescription></DialogHeader>
 {edit?<form ref={formRef} noValidate onSubmit={save} className="space-y-5"><fieldset disabled={busy} className="space-y-5">
  <div><Label htmlFor="supplier-name">{t("name")} *</Label><Input id="supplier-name" required maxLength={200} autoFocus value={form.name} onChange={e=>setForm({...form,name:e.target.value})} aria-invalid={invalid&&!form.name.trim()||!!state.fields.name} aria-describedby={state.fields.name?"supplier-name-error":undefined}/>{state.fields.name&&<FieldError id="supplier-name-error">{state.fields.name.join(" ")}</FieldError>}</div>
  <div role="radiogroup" aria-labelledby="supplier-kind"><p id="supplier-kind" className="mb-2 text-sm font-medium">{t("kind")}</p><div className="flex flex-wrap gap-2">{kinds.map(k=>{const Icon=kindStyle[k].icon;return <Button type="button" role="radio" aria-checked={form.kind===k} variant={form.kind===k?"default":"outline"} key={k} onClick={()=>setForm({...form,kind:k})}><Icon aria-hidden="true"/>{t(k)}</Button>;})}</div></div>
  <div role="radiogroup" aria-labelledby="supplier-status"><p id="supplier-status" className="mb-2 text-sm font-medium">{ui("status")}</p><div className="flex flex-wrap gap-2">{statuses.map(s=>{const Icon=statusStyle[s].icon;return <Button type="button" role="radio" aria-checked={form.status===s} variant={form.status===s?"default":"outline"} key={s} onClick={()=>setForm({...form,status:s})}><Icon aria-hidden="true"/>{t(s)}</Button>;})}</div></div>
  <div><Label htmlFor="supplier-reason">{t("reason")}{["INACTIVE","BLOCKED"].includes(form.status)?" *":""}</Label><Textarea id="supplier-reason" maxLength={1000} value={form.statusReason} onChange={e=>setForm({...form,statusReason:e.target.value})} aria-invalid={invalid&&["INACTIVE","BLOCKED"].includes(form.status)&&!form.statusReason.trim()} aria-describedby="supplier-reason-help"/><p id="supplier-reason-help" className="mt-1 text-xs text-muted-foreground">{t("historyHelp")}</p></div>
  <div><Label htmlFor="supplier-description">{t("description")}</Label><Textarea id="supplier-description" maxLength={2000} value={form.description} onChange={e=>setForm({...form,description:e.target.value})}/></div>
 </fieldset>{invalid&&<FieldError>{t("invalid")}</FieldError>}{(state.error||reloadError)&&<MessageBanner kind={state.conflict?"warning":"error"} action={state.conflict&&record?<Button type="button" variant="view" disabled={busy} onClick={()=>void reload()}><RefreshCw/>{ui("loadCurrentRecord")}</Button>:undefined}>{reloadError||state.error}</MessageBanner>}{state.ambiguous&&<MessageBanner kind="warning">{t("ambiguous")}</MessageBanner>}<DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy||state.conflict&&!!record}><Save/>{ui(busy?"saving":"save")}</Button></DialogFooter></form>
 :record&&<div className="space-y-5"><h2 className="break-words text-xl font-bold">{record.name}</h2><div role="tablist" aria-label={t("detail")} className="flex flex-wrap gap-2">{(Object.keys(supplierTabs) as (keyof typeof supplierTabs)[]).map(tab=>{const Icon=supplierTabs[tab].icon;return <Button key={tab} type="button" role="tab" aria-selected={detailTab===tab} aria-controls={`supplier-tab-${tab}`} id={`supplier-tab-button-${tab}`} disabled={busy} variant={detailTab===tab?"default":"outline"} onKeyDown={e=>{if(busy||!["ArrowRight","ArrowLeft","Home","End"].includes(e.key))return;e.preventDefault();const tabs=Array.from(e.currentTarget.parentElement?.querySelectorAll<HTMLButtonElement>('[role="tab"]')??[]);const index=tabs.indexOf(e.currentTarget);const next=e.key==="Home"?0:e.key==="End"?tabs.length-1:(index+(e.key==="ArrowRight"?1:-1)+tabs.length)%tabs.length;tabs[next]?.focus();tabs[next]?.click();}} onClick={()=>setDetailTab(tab)}><Icon aria-hidden="true"/>{t(`tab_${tab}`)}</Button>;})}</div><div role="tabpanel" id={`supplier-tab-${detailTab}`} aria-labelledby={`supplier-tab-button-${detailTab}`} className="space-y-5">
 {detailTab==="about"&&<><div className="grid gap-4 sm:grid-cols-2"><DetailField label={t("kind")}><SupplierBadge kind={record.kind}/></DetailField><DetailField label={ui("status")}><SupplierBadge status={record.status}/></DetailField><DetailField label={t("modified")}>{formatDateTime(record.modifiedAt,locale)}</DetailField></div>{record.description&&<p className="whitespace-pre-wrap break-words">{record.description}</p>}{record.statusReason&&<p className="whitespace-pre-wrap break-words text-sm">{record.statusReason}</p>}<p className="text-xs text-muted-foreground">{t("historyHelp")}</p></>}
 {detailTab==="services"&&<SupplierRelations supplier={record} onBusyChange={setRelationsBusy} onChanged={value=>{setRecord(value);setForm(supplierForm(value));onSaved();}}/>}
 {detailTab==="documents"&&<SupplierDocuments supplier={record} onBusyChange={setRelationsBusy} onChanged={value=>{setRecord(value);setForm(supplierForm(value));onSaved();}}/>}
 {detailTab==="workOrders"&&<SupplierWorkOrders supplier={record} initialId={workOrderId} onBusyChange={setRelationsBusy} onChanged={value=>{setRecord(value);setForm(supplierForm(value));onSaved();}}/>}
 {detailTab==="quotes"&&<SupplierQuotes onOpenWorkOrder={id=>{setWorkOrderId(id);setDetailTab("workOrders");}} supplier={record} onBusyChange={setRelationsBusy} onChanged={value=>{setRecord(value);setForm(supplierForm(value));onSaved();}}/>}
 {detailTab==="contracts"&&<SupplierContracts supplier={record} onBusyChange={setRelationsBusy} onChanged={value=>{setRecord(value);setForm(supplierForm(value));onSaved();}}/>}
 </div><DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button><Button variant="edit" disabled={busy} onClick={()=>setEdit(true)}><Pencil/>{t("edit")}</Button></DialogFooter></div>}
 </ManagementPanel></Dialog>;
}
