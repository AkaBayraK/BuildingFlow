"use client";
import {useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {Ban,LayoutGrid,LoaderCircle,LogIn,LogOut,Plus,RefreshCw,Save,Search,Users} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {FilterChips} from "@/components/management/FilterChips";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useSearch} from "@/features/people/useRows";
import {formatDateTime} from "@/lib/format";
import {ParkingBadge} from "./ParkingBadges";
import {guestStatusStyle,vehicleTypeIcon} from "./parking-style";
import {UnitPicker,unitLabel} from "./UnitPicker";
import {istanbulDayWindow,guestBody,guestStatuses,limits,localInput,normalizePlate,readGuest,readPage,readPlates,readSpace,type Guest,type GuestStatus} from "./model";
import {parkingRoot,useParkingAction,useParkingRead,useRequestId} from "./useParking";

type Locale="tr"|"en";
type Window="today"|"upcoming"|"all";
export function GuestStatusBadge({status}:{status:GuestStatus}){const t=useTranslations("parking");const s=guestStatusStyle[status];return <ParkingBadge icon={s.icon} tone={s.tone} label={t(`guest_${status}`)}/>;}

function windowQuery(w:Window){
 if(w==="all")return "";const {start,end}=istanbulDayWindow();
 if(w==="today"){return `&from=${encodeURIComponent(start)}&to=${encodeURIComponent(end)}`;}
 return `&from=${encodeURIComponent(new Date().toISOString())}`;
}

export function GuestsTab({propertyId,canWrite,maxHours,policyLoading,policyError,revision,onChanged}:{propertyId:string;canWrite:boolean;maxHours:number|null;policyLoading?:boolean;policyError?:string;revision:number;onChanged:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui"),locale=useLocale() as Locale;
 const [range,setRange]=useState<Window>("today"),[status,setStatus]=useState(""),[page,setPage]=useState(1),[local,setLocal]=useState(0),[creating,setCreating]=useState(false);
 const list=useParkingRead(`${parkingRoot(propertyId)}/guests?page=${page}&pageSize=20${status?`&status=${status}`:""}${windowQuery(range)}`,v=>readPage(v,readGuest),revision+local);
 const action=useParkingAction();
 const refresh=()=>setLocal(n=>n+1),changed=()=>{refresh();onChanged();};
 async function change(g:Guest,kind:"arrive"|"depart"|"cancel"){if(await action.run(`${parkingRoot(propertyId)}/guests/${g.id}/${kind}`,"POST",{version:g.version},readGuest,t(kind==="arrive"?"guestArrivedDone":kind==="depart"?"guestDepartedDone":"guestCancelledDone")))changed();else if(action.stale)refresh();}
 const actions=(g:Guest)=>canWrite&&<div className="flex flex-wrap gap-2">
  {g.status==="PLANNED"&&<Button size="sm" disabled={action.busy||Date.now()<Date.parse(g.startsAt)||Date.now()>=Date.parse(g.endsAt)} onClick={()=>void change(g,"arrive")}><LogIn/>{t("markArrived")}</Button>}
  {g.status==="ARRIVED"&&<Button size="sm" variant="view" disabled={action.busy} onClick={()=>void change(g,"depart")}><LogOut/>{t("markDeparted")}</Button>}
  {g.status==="PLANNED"&&<Button size="sm" variant="danger" disabled={action.busy} onClick={()=>void change(g,"cancel")}><Ban/>{t("cancelGuest")}</Button>}</div>;
 const columns:ColumnDef<Guest>[]=[
  {id:"plate",header:t("plate"),cell:({row})=><strong className="font-mono tracking-wide">{row.original.plate}</strong>},
  {id:"guest",header:t("guestName"),cell:({row})=>row.original.guestName??"—"},
  {id:"unit",header:t("hostUnit"),cell:({row})=>row.original.unitNumber},
  {id:"space",header:t("space"),cell:({row})=>row.original.spaceCode??"—"},
  {id:"time",header:t("window"),cell:({row})=><span className="whitespace-nowrap text-xs">{formatDateTime(row.original.startsAt,locale)} – {formatDateTime(row.original.endsAt,locale)}</span>},
  {id:"status",header:ui("status"),cell:({row})=><GuestStatusBadge status={row.original.status}/>},
  {id:"access",header:t("accessTimes"),cell:({row})=><span className="text-xs">{row.original.arrivedAt?formatDateTime(row.original.arrivedAt,locale):"—"} / {row.original.departedAt?formatDateTime(row.original.departedAt,locale):"—"}</span>},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)},
 ];
 const chips=[{value:"",label:t("all"),icon:LayoutGrid},...guestStatuses.map(s=>({value:s,label:t(`guest_${s}`),icon:guestStatusStyle[s].icon,tone:guestStatusStyle[s].tone}))];
 return <section className="space-y-4" aria-label={t("tab_guests")}>
  <PlateLookup propertyId={propertyId}/>
  <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
   <div className="flex flex-wrap items-center justify-between gap-3 border-b p-4 sm:px-5">
    <FilterChips label={t("statusFilter")} items={chips} value={status} onChange={v=>{setStatus(v);setPage(1);}}/>
    <div className="flex flex-wrap gap-2"><select aria-label={t("window")} className={toolbarSelectClass} value={range} onChange={e=>{setRange(e.target.value as Window);setPage(1);}}>{(["today","upcoming","all"] as Window[]).map(w=><option key={w} value={w}>{t(`window_${w}`)}</option>)}</select>
     <Button variant="view" disabled={list.loading} onClick={refresh}><RefreshCw/>{t("refresh")}</Button>
     {canWrite&&<Button disabled={!maxHours} onClick={()=>setCreating(true)}><Plus/>{t("newGuest")}</Button>}</div>
   </div>
   {policyError&&<div className="p-4"><MessageBanner>{policyError}</MessageBanner></div>}
   {canWrite&&!policyLoading&&!policyError&&!maxHours&&<div className="p-4"><MessageBanner kind="warning">{t("policyRequired")}</MessageBanner></div>}
   <RecordList compact data={list.data} loading={list.loading} error={list.error} columns={columns} page={page} onPage={setPage} onRetry={refresh}
    empty={<><Users className="mx-auto mb-3 size-8"/><strong>{t("guestsEmpty")}</strong><p className="mt-2">{t("guestsEmptyHelp")}</p></>}
    card={g=><><div className="flex items-start justify-between gap-3"><strong className="font-mono tracking-wide">{g.plate}</strong><GuestStatusBadge status={g.status}/></div>
     <p className="my-2 text-xs text-muted-foreground">{[g.guestName,g.unitNumber,g.spaceCode].filter(Boolean).join(" · ")}</p>
     <p className="mb-3 text-xs">{formatDateTime(g.startsAt,locale)} – {formatDateTime(g.endsAt,locale)}</p>{actions(g)}</>}/>
  </div>
  <p className="text-xs text-muted-foreground">{t("operationNote")}</p>
  {creating&&maxHours&&<GuestPanel propertyId={propertyId} maxHours={maxHours} onClose={()=>setCreating(false)} onSaved={changed}/>}
 </section>;
}

/** Guard lookup: approved plates with unit only, never the owner's name or contact. */
function PlateLookup({propertyId}:{propertyId:string}){
 const t=useTranslations("parking");const [term,setTerm]=useState(""),search=useSearch(term);
 const normalized=search.replace(/[^0-9a-z]/gi,"").toUpperCase(),ready=normalized.length>=2&&normalized.length<=limits.plate;
 const plates=useParkingRead(ready?`${parkingRoot(propertyId)}/plates?search=${normalized}`:null,readPlates,0);
 return <div className="rounded-2xl border bg-card p-4 shadow-sm sm:p-5">
  <Label htmlFor="plate-lookup" className="flex items-center gap-2"><Search aria-hidden="true" className="size-4"/>{t("plateLookup")}</Label>
  <Input id="plate-lookup" className="mt-2 max-w-sm font-mono uppercase" maxLength={16} value={term} placeholder="34 ABC" onChange={e=>setTerm(e.target.value)}/>
  <div aria-live="polite" className="mt-3 text-sm">{!ready?<p className="text-muted-foreground">{t("plateLookupHelp")}</p>:plates.loading?<p>{t("loading")}</p>:plates.error?<MessageBanner>{plates.error}</MessageBanner>:plates.data?.length?
   <ul className="flex flex-wrap gap-2">{plates.data.map(p=>{const Icon=vehicleTypeIcon[p.type];return <li key={p.plate} className="dashboard-status tone-green inline-flex items-center gap-2"><Icon aria-hidden="true" className="size-4"/><strong className="font-mono">{p.plate}</strong><span>{unitLabel({number:p.unitNumber,buildingName:p.buildingName})}</span></li>;})}</ul>
   :<p className="text-muted-foreground">{t("plateNotFound")}</p>}</div>
 </div>;
}

function GuestPanel({propertyId,maxHours,onClose,onSaved}:{propertyId:string;maxHours:number;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("parking"),ui=useTranslations("ui");
 const now=new Date();now.setSeconds(0,0);const later=new Date(now.getTime()+Math.min(4,maxHours)*3600000);
 const [form,setForm]=useState({hostUnitId:"",plate:"",guestName:"",startsAt:localInput(now),endsAt:localInput(later),spaceId:"",note:""}),[invalid,setInvalid]=useState(false);
 const root=parkingRoot(propertyId),action=useParkingAction(),requestId=useRequestId(JSON.stringify(form));
 const spaces=useParkingRead(`${root}/guest-spaces`,v=>readPage(v,readSpace),0);
 const set=<K extends keyof typeof form>(k:K,v:(typeof form)[K])=>{setForm(f=>({...f,[k]:v}));setInvalid(false);};
 async function save(e:React.FormEvent){e.preventDefault();const body=guestBody(form,requestId,maxHours);if(!body){setInvalid(true);return;}
  if(await action.run(`${root}/guests`,"POST",body,readGuest,t("guestCreated"))){onSaved();onClose();}}
 return <Dialog open onOpenChange={o=>{if(!o&&!action.busy)onClose();}}><ManagementPanel showCloseButton={!action.busy}>
  <DialogHeader><Users className="size-9 text-primary"/><DialogTitle>{t("newGuest")}</DialogTitle><DialogDescription>{t("newGuestHelp",{hours:maxHours})}</DialogDescription></DialogHeader>
  <form onSubmit={save} noValidate className="space-y-5"><fieldset disabled={action.busy} className="space-y-5">
   <UnitPicker propertyId={propertyId} value={form.hostUnitId} required onChange={u=>set("hostUnitId",u?.id??"")}/>
   <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="guest-plate">{t("plate")} *</Label><Input id="guest-plate" className="font-mono uppercase" maxLength={16} value={form.plate} placeholder="34 TST 100" aria-invalid={invalid&&!normalizePlate(form.plate)} onChange={e=>set("plate",e.target.value)}/></div>
    <div><Label htmlFor="guest-name">{t("guestNameOptional")}</Label><Input id="guest-name" maxLength={limits.guestName} value={form.guestName} onChange={e=>set("guestName",e.target.value)}/></div></div>
   <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="guest-start">{t("startsAt")} *</Label><Input id="guest-start" type="datetime-local" value={form.startsAt} onChange={e=>set("startsAt",e.target.value)}/></div>
    <div><Label htmlFor="guest-end">{t("endsAt")} *</Label><Input id="guest-end" type="datetime-local" min={form.startsAt} value={form.endsAt} onChange={e=>set("endsAt",e.target.value)}/></div></div>
   <div><Label htmlFor="guest-space">{t("visitorSpaceOptional")}</Label><select id="guest-space" className={toolbarSelectClass+" w-full"} value={form.spaceId} onChange={e=>set("spaceId",e.target.value)}><option value="">{t("noSpace")}</option>{spaces.data?.items.map(s=><option key={s.id} value={s.id}>{s.code}</option>)}</select></div>
   <div><Label htmlFor="guest-note">{t("note")}</Label><Textarea id="guest-note" maxLength={limits.note} value={form.note} onChange={e=>set("note",e.target.value)}/></div>
   {invalid&&<FieldError>{t("guestInvalid",{hours:maxHours})}</FieldError>}
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={action.busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={action.busy}>{action.busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("save")}</Button></DialogFooter></form>
 </ManagementPanel></Dialog>;
}
