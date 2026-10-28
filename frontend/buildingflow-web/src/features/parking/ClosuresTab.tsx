"use client";
import {useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {Ban,Plus,RefreshCw,Save,Wrench} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {FieldError} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {ParkingBadge} from "./ParkingBadges";
import {closureBody,localInput,readClosure,readPage,readSpace,type Closure} from "./model";
import {parkingRoot,useParkingAction,useParkingRead,useRequestId} from "./useParking";

export function ClosuresTab({propertyId,canWrite,revision,onChanged}:{propertyId:string;canWrite:boolean;revision:number;onChanged:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),locale=useLocale() as "tr"|"en";
 const [page,setPage]=useState(1),[local,setLocal]=useState(0),[editing,setEditing]=useState<Closure|"new"|null>(null);
 const root=parkingRoot(propertyId),list=useParkingRead(`${root}/closures?page=${page}`,v=>readPage(v,readClosure),revision+local);
 const refresh=()=>setLocal(n=>n+1),changed=()=>{refresh();onChanged();setEditing(null);};
 const status=(c:Closure)=>c.cancelledAt?"closureCancelled":Date.now()>=Date.parse(c.endsAt)?"closureEnded":Date.now()<Date.parse(c.startsAt)?"closurePlanned":"closureActive";
 const badge=(c:Closure)=><ParkingBadge icon={c.cancelledAt?Ban:Wrench} tone={c.cancelledAt?"neutral":"amber"} label={t(status(c))}/>;
 const action=(c:Closure)=>canWrite&&!c.cancelledAt&&Date.now()<Date.parse(c.endsAt)&&<Button size="sm" variant="danger" onClick={()=>setEditing(c)}><Ban aria-hidden="true"/>{t("cancelClosure")}</Button>;
 const columns:ColumnDef<Closure>[]=[
  {id:"space",header:t("space"),cell:({row})=>row.original.spaceCode},
  {id:"window",header:t("window"),cell:({row})=><span>{formatDateTime(row.original.startsAt,locale)} – {formatDateTime(row.original.endsAt,locale)}</span>},
  {id:"reason",header:t("reason"),cell:({row})=><div className="max-w-sm break-words">{row.original.reason}{row.original.cancellationReason&&<p>{row.original.cancellationReason}</p>}</div>},
  {id:"status",header:ui("status"),cell:({row})=>badge(row.original)},
  {id:"actions",header:ui("actions"),cell:({row})=>action(row.original)},
 ];
 return <section className="space-y-4"><p className="text-sm text-muted-foreground">{t("closureHelp")}</p>
  <div className="flex flex-wrap justify-end gap-2"><Button variant="view" onClick={refresh}><RefreshCw aria-hidden="true"/>{t("refresh")}</Button>{canWrite&&<Button onClick={()=>setEditing("new")}><Plus aria-hidden="true"/>{t("newClosure")}</Button>}</div>
  <RecordList compact data={list.data} loading={list.loading} error={list.error} columns={columns} page={page} onPage={setPage} onRetry={refresh}
   empty={<><Wrench className="mx-auto size-8" aria-hidden="true"/><p>{t("closuresEmpty")}</p></>}
   card={c=><div className="space-y-3"><div className="flex justify-between gap-2"><strong>{c.spaceCode}</strong>{badge(c)}</div><p>{formatDateTime(c.startsAt,locale)} – {formatDateTime(c.endsAt,locale)}</p><p className="break-words">{c.reason}</p>{c.cancellationReason&&<p className="break-words">{c.cancellationReason}</p>}{action(c)}</div>}/>
  {editing&&<ClosurePanel key={editing==="new"?"new":editing.id} propertyId={propertyId} value={editing} onClose={()=>setEditing(null)} onSaved={changed}/>}
 </section>;
}
function ClosurePanel({propertyId,value,onClose,onSaved}:{propertyId:string;value:Closure|"new";onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),root=parkingRoot(propertyId),action=useParkingAction();
 const [form,setForm]=useState({spaceId:"",startsAt:localInput(new Date()),endsAt:localInput(new Date(Date.now()+3600000)),reason:""}),[invalid,setInvalid]=useState(false),[spacePage,setSpacePage]=useState(1);
 const spaces=useParkingRead(value==="new"?`${root}/spaces?pageSize=100&page=${spacePage}`:null,v=>readPage(v,readSpace),0),requestId=useRequestId(JSON.stringify(form));
 async function save(e:React.FormEvent){e.preventDefault();const body=value==="new"?closureBody(form,requestId):form.reason.trim()?{version:value.version,reason:form.reason.trim()}:null;
  if(!body){setInvalid(true);return;}if(await action.run(value==="new"?`${root}/closures`:`${root}/closures/${value.id}/cancel`,"POST",body,readClosure,t("closureSaved")))onSaved();}
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><Wrench className="size-9 text-primary" aria-hidden="true"/><DialogTitle>{t(value==="new"?"newClosure":"cancelClosure")}</DialogTitle><DialogDescription>{t("closureHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} noValidate className="space-y-5"><fieldset disabled={action.busy} className="space-y-4">
   {value==="new"&&<><div><Label htmlFor="closure-space">{t("space")}</Label><select id="closure-space" className={toolbarSelectClass+" w-full"} value={form.spaceId} onChange={e=>setForm(f=>({...f,spaceId:e.target.value}))}><option value="">{t("selectSpace")}</option>{spaces.data?.items.map(s=><option key={s.id} value={s.id}>{s.code}</option>)}</select>
    {spaces.data&&spaces.data.totalCount>100&&<div className="mt-2 flex gap-2"><Button type="button" variant="view" aria-label={ui("previousPage")} disabled={spacePage===1} onClick={()=>{setSpacePage(n=>n-1);setForm(f=>({...f,spaceId:""}));}}>←</Button><span>{spacePage}</span><Button type="button" variant="view" aria-label={ui("nextPage")} disabled={spacePage*100>=spaces.data.totalCount} onClick={()=>{setSpacePage(n=>n+1);setForm(f=>({...f,spaceId:""}));}}>→</Button></div>}</div>
   <div><Label htmlFor="closure-start">{t("startsAt")}</Label><Input id="closure-start" type="datetime-local" value={form.startsAt} onChange={e=>setForm(f=>({...f,startsAt:e.target.value}))}/></div>
   <div><Label htmlFor="closure-end">{t("endsAt")}</Label><Input id="closure-end" type="datetime-local" value={form.endsAt} onChange={e=>setForm(f=>({...f,endsAt:e.target.value}))}/></div></>}
   <div><Label htmlFor="closure-reason">{t("reason")}</Label><Textarea id="closure-reason" maxLength={500} value={form.reason} onChange={e=>setForm(f=>({...f,reason:e.target.value}))}/></div>
   {invalid&&<FieldError>{t("closureInvalid")}</FieldError>}{spaces.error&&<FieldError>{spaces.error}</FieldError>}
   {action.stale&&<FieldError>{t("closureStale")}</FieldError>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={action.busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={action.busy||action.stale}><Save aria-hidden="true"/>{t("save")}</Button></DialogFooter></form>
 </ManagementPanel></Dialog>;
}
