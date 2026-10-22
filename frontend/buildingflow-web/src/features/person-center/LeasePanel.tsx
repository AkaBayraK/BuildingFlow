"use client";
import {useCallback,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {FilePlus2,Link2,UserPlus,CircleStop} from "lucide-react";
import {useNotification} from "@/components/feedback/Notifications";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {businessToday,formatDateOnly} from "@/lib/format";
import {useRows} from "@/features/people/useRows";
import {useCenterMutation} from "./hooks";
import {UnitPicker,type UnitScope} from "./UnitPicker";
import {PersonPicker} from "./PersonPicker";
import {LeaseStatusBadge,VerificationBadge} from "./Badges";
import {leaseContract,leaseRoot,mutation,pageOf,type DirectoryRow,type Lease,type Relation} from "./model";
export type LeaseScope={propertyId:string;unitId:string;label:string};
type Mode={kind:"create"}|{kind:"party";lease:Lease}|{kind:"end";lease:Lease}|null;
// A unit has one lease per period: joint tenants join the same contract, a renewal is a new one (BF_LEASE_OVERLAP otherwise).
export function LeasePanel({tenant,person,initial,attach,onClose,onSaved}:{tenant:string;person:string;initial:LeaseScope|null;attach?:Relation;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui"),locale=useLocale(),notice=useNotification();
 const [picked,setPicked]=useState<UnitScope|null>(null),[mode,setMode]=useState<Mode>(attach?null:initial?null:{kind:"create"}),[revision,setRevision]=useState(0),[page,setPage]=useState(1);
 const scope:LeaseScope|null=initial??(picked?{propertyId:picked.property.id,unitId:picked.unit.id,label:[picked.property.name,picked.building?.name,picked.unit.name].filter(Boolean).join(" · ")}:null);
 const decode=useCallback((v:unknown)=>pageOf(v,r=>leaseContract(r,tenant,scope?.propertyId??"",scope?.unitId??"")),[tenant,scope?.propertyId,scope?.unitId]);
 const rows=useRows<Lease>(scope?leaseRoot(tenant,scope.propertyId,scope.unitId)+`?page=${page}&pageSize=10`:"",revision,!!scope,decode);
 const state=useCenterMutation(()=>{notice.success(t("saved"));setMode(null);setRevision(n=>n+1);onSaved();});
 const reload=()=>{state.reset();setRevision(n=>n+1);};
 const parse=(v:unknown)=>mutation(v,r=>leaseContract(r,tenant,scope!.propertyId,scope!.unitId));
 const open=(l:Lease)=>l.status==="ACTIVE"||l.status==="UPCOMING";
 const period=(s:string,e:string|null)=>`${formatDateOnly(s,locale)} – ${e?formatDateOnly(e,locale):t("openEnded")}`;
 const covers=(l:Lease)=>!!attach&&attach.startDate>=l.startDate&&(l.endDate===null||attach.endDate!==null&&attach.endDate<=l.endDate);
 const close=()=>{if(!state.busy){onSaved();onClose();}};
 return <Dialog open onOpenChange={v=>{if(!v)close();}}><ManagementPanel showCloseButton={!state.busy}><DialogHeader><DialogTitle>{t("leases")}</DialogTitle><DialogDescription>{t("leaseHelp")}</DialogDescription></DialogHeader>
  {!initial&&<UnitPicker tenant={tenant} value={picked} disabled={state.busy} onChange={v=>{setPicked(v);setPage(1);state.reset();}}/>}
  {scope&&<p className="text-sm font-medium">{scope.label}</p>}
  {attach&&<MessageBanner kind="info">{t("attachHelp",{period:period(attach.startDate,attach.endDate)})}</MessageBanner>}
  {state.error&&<MessageBanner action={state.conflict?<Button type="button" variant="outline" disabled={state.busy} onClick={reload}>{ui("loadCurrentRecord")}</Button>:undefined}>{state.error}</MessageBanner>}{state.ambiguous&&<MessageBanner kind="warning">{t("ambiguous")}</MessageBanner>}
  {scope&&<section aria-label={t("leases")} className="space-y-3">
   {rows.loading&&<p role="status">{ui("loading")}</p>}{rows.error&&<MessageBanner action={<Button type="button" variant="view" onClick={reload}>{ui("tryAgain")}</Button>}>{rows.error}</MessageBanner>}
   {rows.data&&rows.data.items.length===0&&<p className="text-sm text-muted-foreground">{t("noLeases")}</p>}
   <ul className="space-y-3">{rows.data?.items.map(l=><li key={l.id} className="rounded-xl border p-3"><div className="flex flex-wrap items-center gap-2"><span className="font-medium">{period(l.startDate,l.endDate)}</span><LeaseStatusBadge value={l.status}/><VerificationBadge value={l.verificationStatus}/></div>{l.contractReference&&<p className="mt-1 text-sm text-muted-foreground">{t("contractReference")}: {l.contractReference}</p>}
    <ul className="mt-2 space-y-1 text-sm">{l.parties.map(p=><li key={p.tenancyId}>{p.displayName} · {period(p.startDate,p.endDate)}{p.isPrimaryContact?` · ${t("primary")}`:""}</li>)}</ul>
    {open(l)&&<div className="mt-3 flex flex-wrap gap-2"><Button type="button" size="sm" variant="outline" disabled={state.busy||!!state.conflict} onClick={()=>{state.reset();setMode({kind:"party",lease:l});}}><UserPlus/>{t("addParty")}</Button>{attach&&covers(l)&&<Button type="button" size="sm" disabled={state.busy||!!state.conflict} onClick={()=>void state.submit(leaseRoot(tenant,scope.propertyId,scope.unitId,l.id)+"/attach","POST",{expectedVersion:l.version,tenancyId:attach.id,tenancyVersion:attach.version},parse)}><Link2/>{t("attachTenancy")}</Button>}{l.endDate===null&&<Button type="button" size="sm" variant="warn" disabled={state.busy||!!state.conflict} onClick={()=>{state.reset();setMode({kind:"end",lease:l});}}><CircleStop/>{t("endLease")}</Button>}</div>}</li>)}</ul>
   {rows.data&&rows.data.totalCount>10&&<div className="flex gap-2"><Button type="button" size="sm" variant="outline" disabled={page===1||rows.loading} onClick={()=>setPage(n=>n-1)}>{ui("previousPage")}</Button><Button type="button" size="sm" variant="outline" disabled={rows.loading||page*10>=rows.data.totalCount} onClick={()=>setPage(n=>n+1)}>{ui("nextPage")}</Button></div>}
   {!mode&&<Button type="button" variant="outline" disabled={state.busy} onClick={()=>{state.reset();setMode({kind:"create"});}}><FilePlus2/>{t("newLease")}</Button>}
  </section>}
  {scope&&mode?.kind==="create"&&<CreateLease key={scope.unitId} tenant={tenant} person={person} busy={state.busy||state.ambiguous} onCancel={()=>setMode(null)} onSubmit={payload=>void state.submit(leaseRoot(tenant,scope.propertyId,scope.unitId),"POST",payload,parse)}/>}
  {scope&&mode?.kind==="party"&&<AddParty key={mode.lease.id} tenant={tenant} person={person} lease={mode.lease} busy={state.busy||state.ambiguous} onCancel={()=>setMode(null)} onSubmit={payload=>void state.submit(leaseRoot(tenant,scope.propertyId,scope.unitId,mode.lease.id)+"/parties","POST",payload,parse)}/>}
  {scope&&mode?.kind==="end"&&<EndLease key={mode.lease.id} lease={mode.lease} busy={state.busy||state.ambiguous} onCancel={()=>setMode(null)} onSubmit={payload=>void state.submit(leaseRoot(tenant,scope.propertyId,scope.unitId,mode.lease.id)+"/end","POST",payload,parse)}/>}
  <DialogFooter><Button type="button" variant="outline" disabled={state.busy} onClick={close}>{ui("close")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}
function Dates({start,end,onStart,onEnd,min,max,prefix}:{start:string;end:string;onStart:(v:string)=>void;onEnd:(v:string)=>void;min?:string;max?:string|null;prefix:string}){
 const t=useTranslations("personCenter");
 return <div className="grid gap-3 sm:grid-cols-2"><div><Label htmlFor={prefix+"-start"}>{t("startDate")} *</Label><Input id={prefix+"-start"} type="date" required min={min} max={max??undefined} value={start} onChange={e=>onStart(e.target.value)}/></div><div><Label htmlFor={prefix+"-end"}>{t("endDate")}{max?" *":""}</Label><Input id={prefix+"-end"} type="date" required={!!max} min={start} max={max??undefined} value={end} onChange={e=>onEnd(e.target.value)}/></div></div>;
}
function CreateLease({tenant,person,busy,onCancel,onSubmit}:{tenant:string;person:string;busy:boolean;onCancel:()=>void;onSubmit:(p:Record<string,unknown>)=>void}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui");const form=useRef<HTMLFormElement>(null);const [start,setStart]=useState(businessToday),[end,setEnd]=useState(""),[reference,setReference]=useState(""),[primary,setPrimary]=useState(true),[joint,setJoint]=useState<DirectoryRow|null>(null);
 return <form ref={form} className="space-y-4 rounded-xl border p-4" onSubmit={e=>{e.preventDefault();if(!form.current?.reportValidity())return;const party=(id:string,isPrimary:boolean)=>({personId:id,startDate:start,endDate:end||null,isPrimaryContact:isPrimary});onSubmit({startDate:start,endDate:end||null,contractReference:reference.trim()||null,parties:[party(person,primary),...(joint&&joint.id!==person?[party(joint.id,false)]:[])]});}}>
  <h3 className="font-medium">{t("newLease")}</h3><fieldset disabled={busy} className="space-y-4 border-0 p-0"><Dates prefix="lease" start={start} end={end} onStart={setStart} onEnd={setEnd}/><div><Label htmlFor="lease-reference">{t("contractReference")}</Label><Input id="lease-reference" maxLength={100} value={reference} onChange={e=>setReference(e.target.value)}/></div><label className="flex items-center gap-3"><input type="checkbox" checked={primary} onChange={e=>setPrimary(e.target.checked)}/>{t("primary")}</label><PersonPicker tenant={tenant} id="lease-joint" value={joint} onChange={setJoint} disabled={busy}/></fieldset>
  <div className="flex gap-2"><Button type="button" variant="outline" disabled={busy} onClick={onCancel}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{ui(busy?"saving":"save")}</Button></div></form>;
}
function AddParty({tenant,person,lease,busy,onCancel,onSubmit}:{tenant:string;person:string;lease:Lease;busy:boolean;onCancel:()=>void;onSubmit:(p:Record<string,unknown>)=>void}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui");const form=useRef<HTMLFormElement>(null);const today=businessToday();const [self,setSelf]=useState(!lease.parties.some(p=>p.personId===person)),[other,setOther]=useState<DirectoryRow|null>(null),[start,setStart]=useState(today>lease.startDate?today:lease.startDate),[end,setEnd]=useState(lease.endDate??""),[primary,setPrimary]=useState(false);
 const target=self?person:other?.id;
 return <form ref={form} className="space-y-4 rounded-xl border p-4" onSubmit={e=>{e.preventDefault();if(!form.current?.reportValidity()||!target)return;onSubmit({expectedVersion:lease.version,party:{personId:target,startDate:start,endDate:end||null,isPrimaryContact:primary}});}}>
  <h3 className="font-medium">{t("addParty")}</h3><fieldset disabled={busy} className="space-y-4 border-0 p-0"><label className="flex items-center gap-3"><input type="checkbox" checked={self} onChange={e=>setSelf(e.target.checked)}/>{t("thisPerson")}</label>{!self&&<PersonPicker tenant={tenant} id="party-person" value={other} onChange={setOther} disabled={busy}/>}<Dates prefix="party" start={start} end={end} onStart={setStart} onEnd={setEnd} min={lease.startDate} max={lease.endDate}/><label className="flex items-center gap-3"><input type="checkbox" checked={primary} onChange={e=>setPrimary(e.target.checked)}/>{t("primary")}</label></fieldset>
  <div className="flex gap-2"><Button type="button" variant="outline" disabled={busy} onClick={onCancel}>{ui("cancel")}</Button><Button type="submit" disabled={busy||!target}>{ui(busy?"saving":"save")}</Button></div></form>;
}
function EndLease({lease,busy,onCancel,onSubmit}:{lease:Lease;busy:boolean;onCancel:()=>void;onSubmit:(p:Record<string,unknown>)=>void}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui");const form=useRef<HTMLFormElement>(null);const [date,setDate]=useState(()=>{const today=businessToday();return today<lease.startDate?lease.startDate:today;});
 return <form ref={form} className="space-y-4 rounded-xl border p-4" onSubmit={e=>{e.preventDefault();if(!form.current?.reportValidity())return;onSubmit({expectedVersion:lease.version,endDate:date});}}>
  <h3 className="font-medium">{t("endLease")}</h3><p className="text-sm text-muted-foreground">{t("endLeaseHelp")}</p><fieldset disabled={busy} className="border-0 p-0"><Label htmlFor="lease-end-date">{t("endDate")} *</Label><Input id="lease-end-date" type="date" required min={lease.startDate} value={date} onChange={e=>setDate(e.target.value)}/></fieldset>
  <div className="flex gap-2"><Button type="button" variant="outline" disabled={busy} onClick={onCancel}>{ui("cancel")}</Button><Button type="submit" variant="warn" disabled={busy}>{ui(busy?"saving":"save")}</Button></div></form>;
}
