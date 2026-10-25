"use client";
import {ChargePaymentHistory} from "@/features/payments/PaymentDialogs";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {useRef,useState,useEffect} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Check,Save,LoaderCircle,RefreshCw,CircleX,ListChecks,Square,SquareCheck,Minus,Trash2} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {useRows,useSearch} from "@/features/people/useRows";
import type {UnitChoice} from "@/features/people/types";
import {businessToday,formatCurrency,formatDateOnly,formatDateTime} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {ChargeStatusBadge} from "./ChargeStatusBadge";
import {normalizeAmount,previewTotal,submissionKey,readUnitChoices,readAssessment,readCharge,readCancellationReasons,type CancellationReason,type SubmissionKey,type DuesCharge} from "./model";
import {ChargePeople} from "./ChargePeople";
import {FilterChips} from "@/components/management/FilterChips";
import {ChargeKindBadge} from "./ChargeKindBadge";
import {chargeCategories,chargeKinds,categoryStyle,kindStyle,type ChargeCategory,type ChargeKind} from "./charge-kind-style";

import {collectUnitChoices,mergeUnitSelection,UnitSelectionError} from "./unit-selection";

export function AssessmentEditor({root,propertyId,allowParties=false,onClose,onSaved}:{root:string;propertyId:string;allowParties?:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("dues");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notification=useNotification();
 const [term,setTerm]=useState("");const search=useSearch(term);const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);
 const rows=useRows<UnitChoice>(root+"/unit-choices?"+new URLSearchParams({page:String(page),pageSize:"10",search}),revision,true,readUnitChoices);
 const [selected,setSelected]=useState<Map<string,UnitChoice>>(new Map());
 const [debtorType,setDebtorType]=useState('UNIT');
 const [kind,setKind]=useState<ChargeKind>("DUES");const [category,setCategory]=useState<ChargeCategory|"">("");
 const [selecting,setSelecting]=useState(false);const [selectionError,setSelectionError]=useState("");const selectionRequest=useRef<AbortController|null>(null);
 useEffect(()=>()=>selectionRequest.current?.abort(),[root]);
 const [period,setPeriod]=useState(()=>businessToday().slice(0,7));const [amount,setAmount]=useState("");const [dueDate,setDueDate]=useState(businessToday);const [description,setDescription]=useState("");
 const [errors,setErrors]=useState<Record<string,string[]>>({});const [error,setError]=useState("");const [conflict,setConflict]=useState(false);const [ambiguous,setAmbiguous]=useState(false);const [busy,setBusy]=useState(false);
 const lock=useRef(false);const lastSubmission=useRef<SubmissionKey|null>(null);const form=useRef<HTMLFormElement>(null);
 useEffect(()=>setPage(1),[search]);
 const clear=(name:string)=>setErrors(value=>({...value,[name]:[]}));
 const field=(name:string)=>errors[name]?.length?<FieldError id={"assessment-error-"+name}>{errors[name].join(" ")}</FieldError>:null;
 function toggle(unit:UnitChoice){setSelected(previous=>{const next=new Map(previous);if(next.has(unit.id))next.delete(unit.id);else if(next.size<500)next.set(unit.id,unit);return next;});clear("unitIds");setSelectionError("");}
 const choicesReady=!rows.loading&&!rows.error&&rows.data?.page===page&&search===term.trim();
 const visible=choicesReady?rows.data!.items:[];
 function selectPage(){try{setSelected(mergeUnitSelection(selected,visible));clear("unitIds");setSelectionError("");}catch(err){setSelectionError(err instanceof UnitSelectionError?t(err.code==="limit"?"selectionLimit":"selectionChanged"):errorMessage(err,locale));}}
 function removePage(){setSelected(previous=>{const next=new Map(previous);for(const unit of visible)next.delete(unit.id);return next;});clear("unitIds");setSelectionError("");}
 async function selectAll(){
  if(!choicesReady||selectionRequest.current||lock.current)return;
  const controller=new AbortController();selectionRequest.current=controller;setSelecting(true);setSelectionError("");
  try{
   const items=await collectUnitChoices(async (number,signal)=>readUnitChoices(await api<unknown>(root+"/unit-choices?"+new URLSearchParams({page:String(number),pageSize:"100",search}),{signal})),rows.data!.totalCount,controller.signal);
   if(!controller.signal.aborted){setSelected(mergeUnitSelection(selected,items));clear("unitIds");}
  }catch(err){if(!controller.signal.aborted)setSelectionError(err instanceof UnitSelectionError?t(err.code==="limit"?"selectionLimit":"selectionChanged"):errorMessage(err,locale));}
  finally{if(selectionRequest.current===controller){selectionRequest.current=null;setSelecting(false);}}
 }
 function close(){selectionRequest.current?.abort();onClose();}
 async function save(event:React.FormEvent){
  event.preventDefault();if(lock.current||selectionRequest.current)return;const validation:Record<string,string[]>={};const normalized=normalizeAmount(amount);
  if(!selected.size)validation.unitIds=[t("selectUnits")];if(!/^(?:20[0-9]{2}|[3-9][0-9]{3})-(?:0[1-9]|1[0-2])$/.test(period))validation.period=[t("invalidPeriod")];
  if(!normalized)validation.amount=[t("invalidAmount")];if(!dueDate||dueDate<period+"-01")validation.dueDate=[t("invalidDue")];
  if(!description.trim())validation.description=[t("descriptionRequired")];if(kind==="EXTRA"&&!category)validation.category=[t("categoryRequired")];setErrors(validation);setError("");setConflict(false);
  if(Object.keys(validation).length){requestAnimationFrame(()=>form.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus());return;}
  const payload={unitIds:[...selected.keys()].sort(),period,amount:normalized!,currency:"TRY",dueDate,description:description.trim(),debtorType,...(kind==="EXTRA"?{kind,category}:{})};
  lastSubmission.current=submissionKey(lastSubmission.current,JSON.stringify({propertyId,...payload}),()=>crypto.randomUUID());
  lock.current=true;setBusy(true);setAmbiguous(false);
  try{
   const result=readAssessment(await api<unknown>(root+"/assessments",{method:"POST",body:JSON.stringify({requestId:lastSubmission.current.id,...payload})}),propertyId);
   notification.success(t("assessmentSaved",{count:result.unitCount,total:formatCurrency(Number(result.totalAmount),locale)}));onSaved();onClose();
  }catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError){setErrors(err.errors);setConflict(err.status===409);}setAmbiguous(!(err instanceof ApiError)||err.status>=500);}
  finally{lock.current=false;setBusy(false);}
 }
 const normalized=normalizeAmount(amount);const total=normalized&&selected.size?previewTotal(normalized,selected.size):null;
 const selectedRows=[...selected.values()].sort((a,b)=>(a.buildingName??"").localeCompare(b.buildingName??"",locale,{numeric:true})||a.number.localeCompare(b.number,locale,{numeric:true})||a.id.localeCompare(b.id));
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)close();}}><ManagementPanel showCloseButton={!busy} className="assessment-editor sm:max-w-[min(1120px,calc(100vw-2rem))]"><DialogHeader><DialogTitle>{t("newAssessment")}</DialogTitle><DialogDescription>{t("assessmentHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner kind={conflict?"warning":"error"}>{error}</MessageBanner>}{ambiguous&&<MessageBanner kind="warning">{t("ambiguousResult")}</MessageBanner>}
  {selectionError&&<MessageBanner kind="warning">{selectionError}</MessageBanner>}
  {selecting&&<MessageBanner kind="info">{t("selectingAll")}<Button type="button" variant="outline" onClick={()=>selectionRequest.current?.abort()}>{ui("cancel")}</Button></MessageBanner>}
  <form ref={form} noValidate onSubmit={save}><fieldset disabled={busy||selecting} className="min-w-0 space-y-5 border-0 p-0">
   <div className="grid min-w-0 gap-6 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,1fr)]">
   <div className="assessment-selection min-w-0 space-y-4">
   <div className="space-y-2"><FilterChips label={t("chargeKind")} value={kind} onChange={value=>{setKind(value as ChargeKind);if(value==="DUES")setCategory("");clear("category");}} items={chargeKinds.map(value=>({value,label:t("kind_"+value),icon:kindStyle[value].icon,tone:undefined}))}/><p className="text-xs text-muted-foreground">{t(kind==="EXTRA"?"extraHelp":"duesHelp")}</p>
    {kind==="EXTRA"&&<div className="space-y-2"><FilterChips label={t("chargeCategory")} value={category} onChange={value=>{setCategory(value as ChargeCategory);clear("category");}} items={chargeCategories.map(value=>({value,label:t("category_"+value),icon:categoryStyle[value].icon}))}/>{field("category")}</div>}</div>
   <div className="space-y-2"><Label htmlFor="assessment-debtorType">{t('debtorRequired')}</Label><select id="assessment-debtorType" className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={debtorType} aria-invalid={!!errors.debtorType?.length} aria-describedby={errors.debtorType?.length?'assessment-error-debtorType':'assessment-debtor-help'} onChange={e=>{setDebtorType(e.target.value);clear('debtorType');}}>{(allowParties?['UNIT','TENANT','OWNER']:['UNIT']).map(type=><option key={type} value={type}>{t('debtor_'+type)}</option>)}</select>{field('debtorType')}<p id="assessment-debtor-help" className="text-xs text-muted-foreground">{t('debtorHelp')}</p></div>
   <div className="space-y-2"><Label htmlFor="assessment-unitIds">{t("unitsRequired")}</Label><Input id="assessment-unitIds" maxLength={200} value={term} onChange={event=>setTerm(event.target.value)} placeholder={ui("searchBuildingOrUnitNumber")} aria-invalid={!!errors.unitIds?.length} aria-describedby={errors.unitIds?.length?"assessment-error-unitIds":undefined}/>{field("unitIds")}
    <div className="flex flex-wrap gap-2" role="group" aria-label={t("selectionActions")}>
     <Button type="button" variant="outline" size="sm" disabled={!choicesReady||!rows.data?.totalCount||rows.data.totalCount>500} onClick={()=>void selectAll()}><ListChecks className="size-4"/>{t("selectAllResults",{count:rows.data?.totalCount??0})}</Button>
     <Button type="button" variant="outline" size="sm" disabled={!visible.length||visible.every(unit=>selected.has(unit.id))} onClick={selectPage}><SquareCheck className="size-4"/>{t("selectPage")}</Button>
     <Button type="button" variant="outline" size="sm" disabled={!visible.some(unit=>selected.has(unit.id))} onClick={removePage}><Minus className="size-4"/>{t("removePage")}</Button>
    </div>
    <p className="text-xs text-muted-foreground">{t("selectAllHelp")}</p>
    {(rows.data?.totalCount??0)>500&&<MessageBanner kind="warning">{t("selectionLimit")}</MessageBanner>}
    {rows.loading?<p role="status">{ui("loadingUnits")}</p>:rows.error?<MessageBanner>{rows.error}<Button type="button" variant="view" onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button></MessageBanner>:<div className="grid max-h-44 lg:max-h-80 gap-2 overflow-y-auto rounded-lg border p-2" role="group" aria-label={ui("unitChoices")}>{rows.data?.items.length?rows.data.items.map(unit=><Button key={unit.id} type="button" variant={selected.has(unit.id)?"view":"field"} aria-pressed={selected.has(unit.id)} className="h-auto min-h-9 justify-between whitespace-normal text-left" disabled={!unit.isEnabled||(!selected.has(unit.id)&&selected.size>=500)} onClick={()=>toggle(unit)}><span className="flex items-center gap-2">{selected.has(unit.id)?<SquareCheck aria-hidden="true" className="size-4 shrink-0 text-primary"/>:<Square aria-hidden="true" className="size-4 shrink-0 text-muted-foreground"/>}<span className="wrap-anywhere">{unit.buildingName?unit.buildingName+" · ":""}{unit.number}</span></span>{selected.has(unit.id)&&<Check aria-hidden="true" className="size-4 shrink-0"/>}</Button>):<p className="p-3 text-sm text-muted-foreground">{ui("noUnitsFoundAddAnApartmentOrUnitTo")}</p>}</div>}
    <div className="flex flex-wrap items-center justify-between gap-2 text-xs"><strong role="status">{t("selectedCount",{count:selected.size})}</strong><div className="flex gap-1"><Button type="button" variant="outline" size="sm" disabled={rows.loading||page===1} onClick={()=>setPage(n=>n-1)}>{ui("previous")}</Button><span className="self-center">{page}</span><Button type="button" variant="outline" size="sm" disabled={rows.loading||!!rows.error||page*10>=(rows.data?.totalCount??0)} onClick={()=>setPage(n=>n+1)}>{ui("next")}</Button></div></div>
    {selected.size>0&&<div className="rounded-xl border bg-muted/30 p-3"><div className="flex items-center justify-between gap-2"><strong className="text-sm">{t("selectedUnits")}</strong><Button type="button" variant="danger" size="sm" onClick={()=>{setSelected(new Map());clear("unitIds");setSelectionError("");}}><Trash2 className="size-4"/>{t("clearSelection")}</Button></div><p className="mb-3 text-xs text-muted-foreground">{t("removeSelectionHelp")}</p><div className="flex max-h-36 flex-wrap gap-1 overflow-y-auto" aria-label={t("selectedUnits")}>{selectedRows.map(unit=><Button type="button" key={unit.id} variant="view" size="sm" className="h-auto whitespace-normal wrap-anywhere" onClick={()=>toggle(unit)} aria-label={t("removeSelected",{name:(unit.buildingName?unit.buildingName+" · ":"")+unit.number})}>{unit.buildingName?unit.buildingName+" · ":""}{unit.number}<CircleX className="size-3 shrink-0"/></Button>)}</div></div>}
   </div>
   </div><div className="assessment-details min-w-0 space-y-4 lg:border-l lg:pl-6">
   <div className="grid gap-4 sm:grid-cols-2"><div className="space-y-2"><Label htmlFor="assessment-period">{t("periodRequired")}</Label><Input id="assessment-period" type="month" min="2000-01" value={period} aria-invalid={!!errors.period?.length} aria-describedby={errors.period?.length?"assessment-error-period":undefined} onInput={event=>{setPeriod(event.currentTarget.value);clear("period");}}/>{field("period")}</div><div className="space-y-2"><Label htmlFor="assessment-amount">{t("amountRequired")}</Label><Input id="assessment-amount" inputMode="decimal" maxLength={11} value={amount} aria-invalid={!!errors.amount?.length} aria-describedby={errors.amount?.length?"assessment-error-amount":"assessment-amount-help"} onChange={event=>{setAmount(event.target.value);clear("amount");}} placeholder={t("amountPlaceholder")}/>{field("amount")}<p id="assessment-amount-help" className="text-xs text-muted-foreground">{t("amountHelp")}</p></div></div>
   <div className="space-y-2"><Label htmlFor="assessment-dueDate">{t("dueRequired")}</Label><Input id="assessment-dueDate" type="date" min={period?period+"-01":undefined} value={dueDate} aria-invalid={!!errors.dueDate?.length} aria-describedby={errors.dueDate?.length?"assessment-error-dueDate":undefined} onInput={event=>{setDueDate(event.currentTarget.value);clear("dueDate");}}/>{field("dueDate")}</div>
   <div className="space-y-2"><Label htmlFor="assessment-description">{t("descriptionRequiredLabel")}</Label><Textarea id="assessment-description" maxLength={500} value={description} aria-invalid={!!errors.description?.length} aria-describedby={errors.description?.length?"assessment-error-description":undefined} onChange={event=>{setDescription(event.target.value);clear("description");}}/>{field("description")}</div>
   <div className="rounded-xl border bg-secondary p-4"><p className="font-semibold">{t("preview")}: {total?formatCurrency(Number(total),locale):"—"}</p><p className="mt-2 text-xs text-muted-foreground">{t("confirmAssessment")}</p></div>
   </div></div>
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={close}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{selected.size>1?t("createBulkCharges",{count:selected.size}):t("createCharges")}</Button></DialogFooter>
  </fieldset></form>
 </ManagementPanel></Dialog>;
}

export function ChargeDetail({root,propertyId,target,parentEnabled,onPayment,onClose,onSaved}:{root:string;propertyId:string;target:DuesCharge;parentEnabled:boolean;onPayment:(charge:DuesCharge)=>void;onClose:()=>void;onSaved:()=>void}){
 const payments=useTranslations("payments");const t=useTranslations("dues");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notification=useNotification();
 const [record,setRecord]=useState(target);const [reason,setReason]=useState("");const [error,setError]=useState("");const [reasonError,setReasonError]=useState("");const [conflict,setConflict]=useState(false);const [busy,setBusy]=useState(false);const lock=useRef(false);
 const [reasonId,setReasonId]=useState('');const [reasonIdError,setReasonIdError]=useState('');const [catalog,setCatalog]=useState<CancellationReason[]>([]);const [catalogError,setCatalogError]=useState('');const [catalogLoading,setCatalogLoading]=useState(true);const [catalogRevision,setCatalogRevision]=useState(0);
 useEffect(()=>{const controller=new AbortController();setCatalogLoading(true);setCatalogError('');void api<unknown>(root+'/cancellation-reasons',{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setCatalog(readCancellationReasons(value));}).catch(err=>{if(!controller.signal.aborted)setCatalogError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setCatalogLoading(false);});return()=>controller.abort();},[api,root,locale,catalogRevision]);
 useEffect(()=>{const controller=new AbortController();setBusy(true);lock.current=true;void api<unknown>(root+"/"+target.id,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setRecord(readCharge(value,propertyId));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted){setBusy(false);lock.current=false;}});return()=>controller.abort();},[api,root,target.id,propertyId,locale]);
 async function reload(){if(lock.current)return;lock.current=true;setBusy(true);try{setRecord(readCharge(await api<unknown>(root+"/"+target.id),propertyId));setError("");setConflict(false);}catch(err){setError(errorMessage(err,locale));}finally{lock.current=false;setBusy(false);}}
 async function cancel(event:React.FormEvent){event.preventDefault();if(lock.current||catalogLoading||catalogError)return;setReasonError("");setReasonIdError('');setError("");const selectedReason=catalog.find(value=>value.id===reasonId);if(!selectedReason){setReasonIdError(t('reasonChoiceRequired'));document.getElementById('cancel-dues-reasonId')?.focus();return;}if(selectedReason.requiresExplanation&&!reason.trim()){setReasonError(t('otherReasonRequired'));document.getElementById("cancel-dues-reason")?.focus();return;}lock.current=true;setBusy(true);
  try{setRecord(readCharge(await api<unknown>(root+"/"+record.id+"/cancel",{method:"PUT",body:JSON.stringify({reason:reason.trim(),reasonId,version:record.version})}),propertyId));setConflict(false);notification.success(t("chargeCancelled"));onSaved();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError){setReasonError(err.errors.reason?.join(" ")??"");setReasonIdError(err.errors.reasonId?.join(' ')??'');setConflict(err.status===409);}}
  finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("chargeDetails")}</DialogTitle><DialogDescription>{record.buildingName?record.buildingName+" · ":""}{record.unitNumber} · {record.period} <ChargeKindBadge kind={record.kind} category={record.category}/></DialogDescription></DialogHeader>
  {error&&<MessageBanner kind={conflict?"warning":"error"}>{error}<Button variant="view" disabled={busy} onClick={()=>void reload()}><RefreshCw/>{ui(conflict?"loadCurrentRecord":"tryAgain")}</Button></MessageBanner>}
  <ChargeStatusBadge charge={record}/><dl className="grid grid-cols-2 gap-3 text-sm"><dt>{t("amountRequired")}</dt><dd>{formatCurrency(Number(record.amount),locale)}</dd><dt>{t("balance")}</dt><dd>{formatCurrency(Number(record.balance),locale)}</dd><dt>{t("dueDate")}</dt><dd>{formatDateOnly(record.dueDate,locale)}</dd><dt>{t("createdAt")}</dt><dd>{formatDateTime(record.createdAt,locale)}</dd></dl><p className="wrap-anywhere rounded-lg border p-3 text-sm">{record.description}</p>
  <section className="rounded-xl border p-4" aria-label={t('chargePeople')}><h3 className="mb-3 text-sm font-semibold">{t('chargePeople')}</h3><ChargePeople charge={record} detail/></section>
  {parentEnabled&&record.status!=="CANCELLED"&&Number(record.balance)>0&&<Button disabled={busy||!!error} onClick={()=>onPayment(record)}><Save/>{payments("payCharge")}</Button>}
  {record.status==="CANCELLED"?<MessageBanner kind="info"><strong>{t("chargeCancelled")}</strong>{record.cancellationReasonName&&<p>{record.cancellationReasonName}</p>}<p className="mt-2 wrap-anywhere">{record.cancellationReason}</p>{record.cancelledAt&&<p>{formatDateTime(record.cancelledAt,locale)}</p>}</MessageBanner>:record.balance!==record.amount?<MessageBanner kind="warning">{payments("debtHasPayments")}</MessageBanner>:<form noValidate onSubmit={cancel}><p className="mb-3 text-xs text-muted-foreground">{t("cancellationHelp")}</p>
   {catalogError&&<MessageBanner>{catalogError}<Button type="button" variant="view" onClick={()=>setCatalogRevision(n=>n+1)}>{ui('tryAgain')}</Button></MessageBanner>}
   <Label htmlFor="cancel-dues-reasonId">{t('reasonChoiceRequired')}</Label><select id="cancel-dues-reasonId" className="mb-3 h-9 w-full rounded-md border bg-background px-3 text-sm" value={reasonId} disabled={busy||catalogLoading||!!catalogError||!parentEnabled||!!error} aria-invalid={!!reasonIdError} aria-describedby={reasonIdError?'cancel-dues-reasonId-error':undefined} onChange={e=>{setReasonId(e.target.value);setReasonIdError('');setReasonError('');}}><option value="">{catalogLoading?ui('loading'):t('chooseReason')}</option>{catalog.map(value=><option key={value.id} value={value.id}>{['DUPLICATE_ASSESSMENT','WRONG_AMOUNT','WRONG_PERIOD','TENANT_MOVED_OUT','OTHER'].includes(value.code)?t('reason_'+value.code):value.name}</option>)}</select>{reasonIdError&&<FieldError id="cancel-dues-reasonId-error">{reasonIdError}</FieldError>}
   <Label htmlFor="cancel-dues-reason">{t(catalog.find(value=>value.id===reasonId)?.requiresExplanation?'reasonDetailsRequired':'reasonDetails')}</Label><Textarea id="cancel-dues-reason" maxLength={500} value={reason} disabled={busy||!parentEnabled||!!error} aria-invalid={!!reasonError} aria-describedby={reasonError?"cancel-dues-error":undefined} onChange={event=>{setReason(event.target.value);setReasonError("");}}/>{reasonError&&<FieldError id="cancel-dues-error">{reasonError}</FieldError>}<DialogFooter className="mt-4"><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button><Button type="submit" variant="destructive" disabled={busy||catalogLoading||!!catalogError||!parentEnabled||!!error}>{busy?<LoaderCircle className="animate-spin"/>:<CircleX/>}{t("cancelCharge")}</Button></DialogFooter></form>}
 <ChargePaymentHistory propertyId={propertyId} chargeId={record.id}/>
 </ManagementPanel></Dialog>;
}
