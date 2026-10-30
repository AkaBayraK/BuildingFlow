"use client";
import {useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {CalendarCheck2,LoaderCircle,RefreshCw,Save,Trash2} from "lucide-react";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {DetailField,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {businessToday,formatDateOnly,formatDateTime} from "@/lib/format";
import {MaintenanceStatusBadge,MaintenanceType} from "./MaintenanceType";
import {typeStyle} from "./type-style";
import {useDefinitionOptions} from "@/features/definitions/useDefinitionOptions";
import {limits,maintenanceStatuses,maintenanceTypes,planBody,readPlan,validPlanForm,type MaintenancePlan,type MaintenanceStatus,type MaintenanceType as Type} from "./model";

type Locale="tr"|"en";
export function MaintenancePanel({propertyId,target,editing,canWrite,initialType,onClose,onSaved}:{propertyId:string;target?:MaintenancePlan;editing:boolean;canWrite:boolean;initialType?:Type;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("maintenance"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),notice=useNotification();
 const [record,setRecord]=useState(target),[edit,setEdit]=useState(!target||editing);
 const blank=(p?:MaintenancePlan)=>({title:p?.title??"",description:p?.description??"",type:p?.type??initialType??"MAINTENANCE" as Type,plannedDate:p?.plannedDate??businessToday(),location:p?.location??"",responsible:p?.responsible??"",status:p?.status??"PLANNED" as MaintenanceStatus});
 const [form,setForm]=useState(()=>blank(target));
 const typeChoices=useDefinitionOptions(propertyId,"MAINTENANCE_TYPE",maintenanceTypes,record?.type);
 const [busy,setBusy]=useState(false),[invalid,setInvalid]=useState(false),[stale,setStale]=useState(false),[confirm,setConfirm]=useState(false),[loadingCurrent,setLoadingCurrent]=useState(false);
 const lock=useRef(false),root=`/api/v1/properties/${propertyId}/maintenance-plans`;
 const set=<K extends keyof typeof form>(key:K,value:(typeof form)[K])=>{setForm(f=>({...f,[key]:value}));setInvalid(false);};
 async function reload(){if(!record)return;setLoadingCurrent(true);try{const current=readPlan(await api<unknown>(`${root}/${record.id}`),propertyId);setRecord(current);setForm(blank(current));setStale(false);}catch(e){if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));}finally{setLoadingCurrent(false);}}
 async function save(e:React.FormEvent){e.preventDefault();if(lock.current||!canWrite)return;if(!validPlanForm(form)){setInvalid(true);return;}
  lock.current=true;setBusy(true);setStale(false);
  try{const saved=readPlan(await api<unknown>(record?`${root}/${record.id}`:root,{method:record?"PUT":"POST",body:JSON.stringify(planBody(form,record?.version??0))}),propertyId);notice.success(t(record?"updated":"created"));onSaved();setRecord(saved);setForm(blank(saved));if(!record)onClose();else setEdit(false);}
  catch(e){if(e instanceof ApiError&&e.status===409)setStale(true);if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));}
  finally{lock.current=false;setBusy(false);}
 }
 async function remove(){if(!record||lock.current)return;lock.current=true;setBusy(true);
  try{await api(`${root}/${record.id}?version=${record.version}`,{method:"DELETE"});notice.success(t("deleted"));onSaved();onClose();}
  catch(e){if(e instanceof ApiError&&e.status===409){setStale(true);setConfirm(false);}if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));}
  finally{lock.current=false;setBusy(false);}
 }
 const staleBanner=stale&&<MessageBanner kind="warning" action={<Button type="button" variant="view" size="sm" disabled={loadingCurrent} onClick={()=>void reload()}><RefreshCw/>{t("loadCurrent")}</Button>}>{t("staleHelp")}</MessageBanner>;
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}>
  <DialogHeader><CalendarCheck2 className="size-9 text-primary"/><DialogTitle>{t(!record?"new":edit?"edit":"details")}</DialogTitle><DialogDescription>{t("panelHelp")}</DialogDescription></DialogHeader>
  {edit?<form onSubmit={save} noValidate className="space-y-5">{staleBanner}<fieldset disabled={busy||!canWrite} className="space-y-5">
   <div role="radiogroup" aria-labelledby="plan-type-label"><p id="plan-type-label" className="mb-2 text-sm font-medium">{t("type")} *</p><div className="request-type-grid">{typeChoices.options.map(k=>{const c=typeChoices.style(k,typeStyle[k]);return <button key={k} type="button" role="radio" aria-checked={form.type===k} onClick={()=>set("type",k)} className={`request-type-option${form.type===k?` tone-${c.tone}`:""}`}><span className={`dashboard-icon tone-${c.tone}`}><c.icon aria-hidden="true"/></span><span>{typeChoices.label(k,t(`type_${k}`))}</span></button>;})}</div></div>
   <div><Label htmlFor="plan-title">{t("title")} *</Label><Input id="plan-title" maxLength={limits.title} value={form.title} aria-invalid={invalid&&!form.title.trim()} onChange={e=>set("title",e.target.value)}/></div>
   <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="plan-date">{t("date")} *</Label><Input id="plan-date" type="date" min="2000-01-01" max="2100-12-31" value={form.plannedDate} aria-invalid={invalid&&!form.plannedDate} onChange={e=>set("plannedDate",e.target.value)}/></div>
    <div><Label htmlFor="plan-status">{ui("status")} *</Label><select id="plan-status" className={toolbarSelectClass+" w-full"} value={form.status} onChange={e=>set("status",e.target.value as MaintenanceStatus)}>{maintenanceStatuses.map(s=><option key={s} value={s}>{t(`status_${s}`)}</option>)}</select></div></div>
   <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="plan-location">{t("location")}</Label><Input id="plan-location" maxLength={limits.text} placeholder={t("locationHint")} value={form.location} onChange={e=>set("location",e.target.value)}/></div>
    <div><Label htmlFor="plan-responsible">{t("responsible")}</Label><Input id="plan-responsible" maxLength={limits.text} placeholder={t("responsibleHint")} value={form.responsible} onChange={e=>set("responsible",e.target.value)}/></div></div>
   <div><Label htmlFor="plan-description">{t("description")}</Label><Textarea id="plan-description" className="min-h-28" maxLength={limits.description} value={form.description} onChange={e=>set("description",e.target.value)}/></div>
   {invalid&&<FieldError>{t("invalid")}</FieldError>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={()=>{if(record){setEdit(false);setForm(blank(record));setStale(false);}else onClose();}}>{ui("cancel")}</Button><Button type="submit" disabled={busy||!canWrite||stale}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("save")}</Button></DialogFooter></form>
  :record&&<>{staleBanner}<h2 className="my-4 break-words text-xl font-bold">{record.title}</h2>
   <div className="grid gap-4 sm:grid-cols-2"><DetailField label={t("type")}><MaintenanceType type={record.type} label={t(`type_${record.type}`)}/></DetailField><DetailField label={ui("status")}><MaintenanceStatusBadge status={record.status} label={t(`status_${record.status}`)}/></DetailField>
    <DetailField label={t("date")}>{formatDateOnly(record.plannedDate,locale)}</DetailField><DetailField label={t("location")}>{record.location??"—"}</DetailField><DetailField label={t("responsible")}>{record.responsible??"—"}</DetailField><DetailField label={t("modifiedAt")}>{formatDateTime(record.modifiedAt,locale)}</DetailField></div>
   {record.description&&<p className="my-5 whitespace-pre-wrap break-words rounded-xl bg-muted p-4 text-sm">{record.description}</p>}
   {canWrite&&<div className="my-5 flex flex-wrap gap-2"><Button variant="edit" disabled={busy} onClick={()=>setEdit(true)}>{t("edit")}</Button><Button variant="danger" disabled={busy} onClick={()=>setConfirm(true)}><Trash2/>{t("delete")}</Button></div>}
   {confirm&&<div className="space-y-3 rounded-xl border p-4"><p className="text-sm">{t("deleteHelp")}</p><div className="flex flex-wrap gap-2"><Button variant="destructive" disabled={busy} onClick={()=>void remove()}>{busy?<LoaderCircle className="animate-spin"/>:<Trash2/>}{t("deleteConfirm")}</Button><Button variant="outline" disabled={busy} onClick={()=>setConfirm(false)}>{ui("cancel")}</Button></div></div>}
   <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button></DialogFooter></>}
 </ManagementPanel></Dialog>;
}
