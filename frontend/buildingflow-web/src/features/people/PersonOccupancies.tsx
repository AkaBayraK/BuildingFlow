"use client";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {useLocale,useTranslations} from "next-intl";
import {useEffect,useRef,useState} from "react";
import type {ColumnDef} from "@tanstack/react-table";
import {Plus,X,DoorOpen,CalendarCheck,RefreshCw,LoaderCircle,Save} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Badge} from "@/components/ui/badge";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {RecordList} from "@/components/management/RecordList";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {businessToday,formatDateOnly,formatCurrency} from "@/lib/format";
import {occupancyTypeNames,occupancyStatusNames,personName,type Person,type Occupancy,type OccupancyType,type UnitChoice,type CreateOccupancyRequest} from "./types";
import {readMoveOutPreview,readTenantDebtResult,moveOutApprovals,type MoveOutPreview} from "../dues/model";
import {parseOccupancyPage} from "@/services/management-contracts";
import {useRows,useSearch} from "./useRows";

function unitLabel(unit:{buildingName:string|null;number:string}){return `${unit.buildingName?unit.buildingName+" · ":""}${unit.number}`;}
export function PersonOccupancies({propertyId,person,parentEnabled,revision,onChanged,onClose}:{propertyId:string;person:Person;parentEnabled:boolean;revision:number;onChanged:()=>void;onClose:()=>void}){
  const t=useTranslations("ui"); const locale=useLocale();const notification=useNotification();

  const api=useAuthorizedApi();const root=`/api/v1/properties/${encodeURIComponent(propertyId)}`;
  const [current,setCurrent]=useState<Person|null>(null);const [personError,setPersonError]=useState("");const [page,setPage]=useState(1);const [creating,setCreating]=useState(false);const [ending,setEnding]=useState<Occupancy|null>(null);const [notice,setNotice]=useState("");
  const rows=useRows<Occupancy>(`${root}/occupancies?personId=${encodeURIComponent(person.id)}&page=${page}&pageSize=10`,revision);
  useEffect(()=>{const controller=new AbortController();setCurrent(null);setPersonError("");void api<Person>(`${root}/people/${encodeURIComponent(person.id)}`,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setCurrent(value);}).catch(error=>{if(!controller.signal.aborted)setPersonError(errorMessage(error,locale));});return()=>controller.abort();},[api,root,person.id,revision]);
  const enabled=parentEnabled && !!current?.isEnabled && !personError;
  const actions=(value:Occupancy)=>value.endDate===null?<Button size="sm" variant="outline" disabled={!parentEnabled} onClick={()=>setEnding(value)}><CalendarCheck className="size-4"/>{t("endRelationship")}</Button>:<span className="text-xs text-muted-foreground">{t("endDateRecorded")}</span>;
  const columns:ColumnDef<Occupancy>[]=[
    {id:"unit",header:t("unit"),cell:({row})=><strong>{unitLabel({number:row.original.unitNumber,buildingName:row.original.buildingName})}</strong>},
    {id:"type",header:t("relationship"),cell:({row})=><><Badge variant="secondary">{t(occupancyTypeNames[row.original.type])}</Badge>{row.original.isPrimaryContact && <span className="mt-1 block text-xs text-muted-foreground">{t("primaryContact")}</span>}</>},
    {id:"dates",header:t("dateRange"),cell:({row})=><span className="text-xs">{formatDateOnly(row.original.startDate,locale)} — {row.original.endDate?formatDateOnly(row.original.endDate,locale):t("ongoing")}</span>},
    {id:"status",header:t("status"),cell:({row})=><Badge variant={row.original.status==="ACTIVE"?"default":"outline"}>{t(occupancyStatusNames[row.original.status])}</Badge>},
    {id:"actions",header:t("actions"),cell:({row})=>actions(row.original)}
  ];
  return <section id="person-occupancies" aria-label={t("personSUnitRelationships")} className="mt-8 overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="flex flex-wrap items-center justify-between gap-4 border-b p-5"><div><span className="text-xs text-primary">{t("unitRELATIONSHIPSANDHISTORY")}</span><h2 className="mt-1 text-xl font-semibold wrap-anywhere">{current?personName(current):personName(person)}</h2></div><div className="flex gap-2"><Button disabled={!enabled} onClick={()=>setCreating(true)}><Plus className="size-4"/>{t("linkToUnit")}</Button><Button variant="outline" onClick={onClose}>{t("backToPeople")}</Button></div></div>
    <p className="border-b p-5 text-xs leading-6 text-muted-foreground">{t("ownerTenantAndResidentAreSeparateRelationshipsBothDate")}</p>
    {personError && <MessageBanner className="my-4 text-left">{personError}</MessageBanner>}{notice && <MessageBanner className="my-4 text-left" kind="success" announce={false}>{notice}</MessageBanner>}
    <RecordList compact {...rows} columns={columns} page={page} onPage={setPage} onRetry={onChanged} empty={<><DoorOpen className="mx-auto mb-3 size-8"/><strong>{t("thisPersonHasNoUnitRelationshipsYet")}</strong><p className="mt-2">{t("useLinkToUnitToAddAnOwnerTenant")}</p></>} card={value=><><div className="mb-2 flex items-start justify-between gap-2"><strong>{unitLabel({number:value.unitNumber,buildingName:value.buildingName})}</strong><Badge variant="secondary">{t(occupancyTypeNames[value.type])}</Badge></div><p className="mb-2 text-xs text-muted-foreground">{formatDateOnly(value.startDate,locale)} — {value.endDate?formatDateOnly(value.endDate,locale):t("ongoing")}</p><p className="mb-3 text-xs text-muted-foreground">{t(occupancyStatusNames[value.status])}{value.isPrimaryContact?t("primaryContact2"):""}</p>{actions(value)}</>}/>
    {creating && current && <OccupancyEditor root={root} person={current} onClose={()=>setCreating(false)} onSaved={()=>{setNotice(t("unitRelationshipSaved"));setPage(1);onChanged();}}/>}
    {ending && <EndOccupancy root={root} target={ending} onClose={()=>setEnding(null)} onSaved={()=>{setNotice(t("endDateRecordedRelationshipHistoryPreserved"));onChanged();}}/>}
  </section>;
}
function OccupancyEditor({root,person,onClose,onSaved}:{root:string;person:Person;onClose:()=>void;onSaved:()=>void}){
  const t=useTranslations("ui"); const locale=useLocale();const notification=useNotification();

  const api=useAuthorizedApi();const [term,setTerm]=useState("");const search=useSearch(term);const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);
  const rows=useRows<UnitChoice>(`${root}/unit-choices?${new URLSearchParams({page:String(page),pageSize:"10",search})}`,revision);
  const [unit,setUnit]=useState<UnitChoice|null>(null);const [type,setType]=useState<OccupancyType>("OWNER");const [start,setStart]=useState(businessToday);const [end,setEnd]=useState("");const [primary,setPrimary]=useState(false);
  const [conflict,setConflict]=useState(false);const [errors,setErrors]=useState<Record<string,string[]>>({});const [error,setError]=useState("");const [busy,setBusy]=useState(false);const lock=useRef(false);
  useEffect(()=>setPage(1),[search]);
  async function save(event:React.FormEvent){
    event.preventDefault();if(lock.current)return;const validation:Record<string,string[]>={};
    if(!unit)validation.unitId=[t("selectAUnit")];if(!start)validation.startDate=[t("aStartDateIsRequired")];if(end && start && end<start)validation.endDate=[t("theEndDateCannotPrecedeTheStartDate")];
    setErrors(validation);setError("");if(Object.keys(validation).length){document.getElementById(`occupancy-${Object.keys(validation)[0]}`)?.focus();return;}
    if(!unit)return;const request:CreateOccupancyRequest={personId:person.id,unitId:unit.id,type,startDate:start,endDate:end||null,isPrimaryContact:primary};
    lock.current=true;setBusy(true);
    try{await api<Occupancy>(`${root}/occupancies`,{method:"POST",body:JSON.stringify(request)});notification.success(t("unitRelationshipSaved"));onSaved();onClose();}
    catch(error){setError(errorMessage(error,locale));setConflict(error instanceof ApiError && error.status===409);if(error instanceof ApiError)setErrors(error.errors);}finally{lock.current=false;setBusy(false);}
  }
  const field=(name:string)=>errors[name]?.length?<FieldError id={`occupancy-error-${name}`}>{errors[name].join(" ")}</FieldError>:null;
  const clear=(name:string)=>setErrors(previous=>({...previous,[name]:[]}));
  return <Dialog open onOpenChange={open=>{if(!open && !busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("linkAPersonToAUnit")}</DialogTitle><DialogDescription>{t("linkPersonDescription",{name:personName(person)})}</DialogDescription></DialogHeader>
    {error && <MessageBanner className="my-4 text-left" kind={conflict?"warning":"error"}>{error}</MessageBanner>}
    <form noValidate onSubmit={save}><fieldset disabled={busy} className="space-y-4 border-0 p-0">
      <div className="space-y-2"><Label htmlFor="occupancy-unitId">{t("unit2")}</Label><Input id="occupancy-unitId" aria-invalid={!!errors.unitId?.length} aria-describedby={errors.unitId?.length?"occupancy-error-unitId":undefined} aria-label={t("searchUnits")} placeholder={t("searchBuildingOrUnitNumber")} value={term} maxLength={200} onChange={event=>setTerm(event.target.value)}/>{unit && <p className="rounded-lg border p-3 text-sm">{t("selected")}<strong>{unitLabel(unit)}</strong></p>}{field("unitId")}
      {rows.loading?<p role="status" className="text-xs text-muted-foreground">{t("loadingUnits")}</p>:rows.error?<div><MessageBanner className="my-4 text-left">{rows.error}</MessageBanner><Button type="button" variant="outline" onClick={()=>setRevision(n=>n+1)}>{t("tryAgain")}</Button></div>:<div role="group" aria-label={t("unitChoices")} className="grid max-h-40 gap-2 overflow-y-auto">{rows.data?.items.length?rows.data.items.map(value=><Button key={value.id} type="button" variant={unit?.id===value.id?"secondary":"outline"} className="justify-between" disabled={!value.isEnabled} aria-pressed={unit?.id===value.id} onClick={()=>{setUnit(value);clear("unitId");}}>{unitLabel(value)}{!value.isEnabled && <span className="text-xs">{t("inactive")}</span>}</Button>):<p className="text-xs text-muted-foreground">{t("noUnitsFoundAddAnApartmentOrUnitTo")}</p>}</div>}
      <div className="flex items-center justify-between text-xs text-muted-foreground"><span>{page} / {Math.max(1,Math.ceil((rows.data?.totalCount ?? 0)/10))}</span><div className="flex gap-2"><Button type="button" variant="ghost" size="sm" disabled={rows.loading || page===1} onClick={()=>setPage(n=>n-1)}>{t("previous")}</Button><Button type="button" variant="ghost" size="sm" disabled={rows.loading || !!rows.error || page*10>=(rows.data?.totalCount ?? 0)} onClick={()=>setPage(n=>n+1)}>{t("next")}</Button></div></div></div>
      <div className="space-y-2"><Label htmlFor="occupancy-type">{t("relationshipType")}</Label><select id="occupancy-type" className="h-10 w-full rounded-md border bg-background px-3 text-sm" value={type} onChange={event=>{setType(event.target.value as OccupancyType);clear("type");}}>{(Object.keys(occupancyTypeNames) as OccupancyType[]).map(value=><option key={value} value={value}>{t(occupancyTypeNames[value])}</option>)}</select>{field("type")}</div>
      <div className="grid gap-4 sm:grid-cols-2"><div className="space-y-2"><Label htmlFor="occupancy-startDate">{t("startDate")}</Label><Input id="occupancy-startDate" type="date" required value={start} aria-invalid={!!errors.startDate?.length} aria-describedby={errors.startDate?.length?"occupancy-error-startDate":undefined} onInput={event=>{setStart(event.currentTarget.value);clear("startDate");clear("endDate");}}/>{field("startDate")}</div><div className="space-y-2"><Label htmlFor="occupancy-endDate">{t("endDateOptional")}</Label><Input id="occupancy-endDate" type="date" value={end} min={start || undefined} aria-invalid={!!errors.endDate?.length} aria-describedby={errors.endDate?.length?"occupancy-error-endDate":undefined} onInput={event=>{setEnd(event.currentTarget.value);clear("endDate");}}/>{field("endDate")}</div></div>
      <p className="text-xs text-muted-foreground">{t("theEndDateIsTheLastValidDayOf")}</p>
      <label className="flex items-center gap-3 rounded-lg border p-3 text-sm"><input type="checkbox" checked={primary} onChange={event=>setPrimary(event.target.checked)}/>{t("primaryContactPerson")}</label>
      <DialogFooter className="border-t pt-4"><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{t("cancel")}</Button><Button type="submit" disabled={busy}>{busy?<LoaderCircle className="size-4 animate-spin"/>:<Save className="size-4"/>}{t("save")}</Button></DialogFooter>
    </fieldset></form>
  </ManagementPanel></Dialog>;
}
function EndOccupancy({root,target,onClose,onSaved}:{root:string;target:Occupancy;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("ui");const d=useTranslations("dues");const locale=useLocale();const notification=useNotification();const api=useAuthorizedApi();
 const [record,setRecord]=useState(target);const [end,setEnd]=useState(()=>target.startDate>businessToday()?target.startDate:businessToday());
 const [error,setError]=useState("");const [fieldError,setFieldError]=useState("");const [conflict,setConflict]=useState(false);const [busy,setBusy]=useState(false);const lock=useRef(false);
 const [preview,setPreview]=useState<MoveOutPreview|null>(null);const [previewError,setPreviewError]=useState("");const [previewLoading,setPreviewLoading]=useState(false);const [retry,setRetry]=useState(0);const [approved,setApproved]=useState(false);
 const tenant=record.type==="TENANT";
 useEffect(()=>{
  setPreview(null);setApproved(false);setPreviewError("");setPreviewLoading(false);
  if(!tenant||record.endDate!==null||!end||end<record.startDate)return;
  const controller=new AbortController();setPreviewLoading(true);
  void api<unknown>(`${root}/occupancies/${record.id}/end-preview?${new URLSearchParams({endDate:end,version:String(record.version)})}`,{signal:controller.signal})
   .then(value=>{if(!controller.signal.aborted)setPreview(readMoveOutPreview(value,record.id,record.version,end));})
   .catch(value=>{if(!controller.signal.aborted){setPreviewError(errorMessage(value,locale));if(value instanceof ApiError&&value.status===409)setConflict(true);}})
   .finally(()=>{if(!controller.signal.aborted)setPreviewLoading(false);});
  return()=>controller.abort();
 },[api,root,record.id,record.version,record.startDate,record.endDate,tenant,end,retry,locale]);
 async function reload(){
  if(lock.current)return;lock.current=true;setBusy(true);setPreview(null);setApproved(false);
  try{const value=await api<unknown>(`${root}/occupancies/${target.id}`);const current=parseOccupancyPage({items:[value],page:1,pageSize:1,totalCount:1},target.propertyId,target.personId).items[0];if(current.id!==target.id)throw new Error('Invalid occupancy identity');setRecord(current);setError("");setConflict(false);setFieldError("");setRetry(n=>n+1);}
  catch(value){setError(errorMessage(value,locale));}finally{lock.current=false;setBusy(false);}
 }
 async function save(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;setError("");setFieldError("");setConflict(false);
  if(!end||end<record.startDate){setFieldError(t("theEndDateIsRequiredAndCannotPrecedeThe"));document.getElementById("end-occupancy-date")?.focus();return;}
  if(tenant&&(!preview||previewLoading||previewError))return;
  lock.current=true;setBusy(true);
  try{
   const cancellationApprovals=moveOutApprovals(preview,record.id,record.version,end,tenant&&approved);
   const value=await api<unknown>(`${root}/occupancies/${record.id}/end`,{method:"PUT",body:JSON.stringify({endDate:end,version:record.version,cancellationApprovals})});
   if(tenant){const result=readTenantDebtResult(value);notification.success(d("moveOutResult",{cancelled:result.cancelledCount,pending:result.pendingCount,skipped:result.skippedPaymentCount}));}
   else notification.success(t("endDateRecordedRelationshipHistoryPreserved"));
   onSaved();onClose();
  }catch(value){setError(errorMessage(value,locale));if(value instanceof ApiError){setFieldError(value.errors.endDate?.join(" ")??"");setConflict(value.status===409);if(value.status===409){setApproved(false);setPreview(null);setRetry(n=>n+1);}}}
  finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}>
  <DialogHeader><DialogTitle>{t("endRelationship")}</DialogTitle><DialogDescription>{t("endDescription",{name:record.personName,unit:record.unitNumber,type:t(occupancyTypeNames[record.type])})}</DialogDescription></DialogHeader>
  {error&&<MessageBanner className="my-4 text-left" kind={conflict?"warning":"error"}>{error}</MessageBanner>}
  {conflict&&<Button type="button" variant="outline" className="my-3" disabled={busy} onClick={()=>void reload()}><RefreshCw className="size-4"/>{t("loadCurrentRecord")}</Button>}
  {record.endDate!==null?<p role="status">{t("endRecorded",{date:formatDateOnly(record.endDate,locale)})}</p>:<form noValidate onSubmit={save}>
   <div className="mb-5 space-y-2"><Label htmlFor="end-occupancy-date">{t("lastValidDay")}</Label><Input id="end-occupancy-date" type="date" disabled={busy} value={end} min={record.startDate} aria-invalid={!!fieldError} aria-describedby={fieldError?"end-occupancy-error":undefined} onInput={event=>{setEnd(event.currentTarget.value);setFieldError("");setApproved(false);setPreview(null);}}/>{fieldError&&<FieldError id="end-occupancy-error">{fieldError}</FieldError>}</div>
   {tenant&&<section className="mb-5 space-y-3 rounded-xl border p-4" aria-label={d("moveOutTitle")}>
    <h3 className="font-semibold">{d("moveOutTitle")}</h3><p className="text-xs text-muted-foreground">{d("moveOutHelp")}</p>
    {previewLoading&&<p role="status">{t("loading")}</p>}
    {previewError&&<><MessageBanner>{previewError}</MessageBanner><Button type="button" variant="outline" disabled={busy} onClick={()=>{setPreview(null);setApproved(false);setRetry(n=>n+1);}}>{t("tryAgain")}</Button></>}
    {preview&&<><p className="text-sm font-semibold">{d("moveOutTotal",{count:preview.eligibleCount,total:formatCurrency(Number(preview.totalAmount),locale,"TRY")})}</p>
     {!!preview.skippedPaymentCount&&<MessageBanner kind="warning">{d("moveOutPayments",{count:preview.skippedPaymentCount})}</MessageBanner>}
     {preview.items.length>0&&<ul className="max-h-48 space-y-2 overflow-y-auto text-xs">{preview.items.map(item=><li key={item.id} className="flex justify-between gap-3 border-b pb-2"><span>{formatDateOnly(item.period+"-01",locale)}</span><strong>{formatCurrency(Number(item.amount),locale,"TRY")}</strong></li>)}</ul>}
     {!preview.approvalAvailable?<MessageBanner kind="warning">{d("moveOutLimit")}</MessageBanner>:preview.eligibleCount>0?<label className="flex items-start gap-3 rounded-lg border p-3 text-sm"><input type="checkbox" className="mt-1" disabled={busy} checked={approved} onChange={event=>setApproved(event.target.checked)}/>{d("moveOutApprove",{count:preview.eligibleCount})}</label>:<p className="text-xs">{d("moveOutEmpty")}</p>}
     <p className="text-xs text-muted-foreground">{d("moveOutNoConsent")}</p>
    </>}
   </section>}
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{t("cancel")}</Button><Button type="submit" disabled={busy||(tenant&&(!preview||previewLoading||!!previewError))}>{busy?<LoaderCircle className="size-4 animate-spin"/>:<CalendarCheck className="size-4"/>}{approved?d("moveOutSaveAndCancel"):t("saveEndDate")}</Button></DialogFooter>
  </form>}
 </ManagementPanel></Dialog>;
}
