"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Save,LoaderCircle,ReceiptText,RefreshCw,CircleX} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {submissionKey,type SubmissionKey} from "@/features/dues/model";
import {businessToday,formatDateOnly,formatDateTime,formatDecimalCurrency} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {DetailField} from "@/components/management/WorkspacePresentation";
import {categoryStyle} from "./category-style";
import {ExpenseCategoryLabel} from "./ExpenseCategoryLabel";
import {expenseCategories,normalizeFinanceAmount,readExpense,readBudget,readFinanceSummary,type CategoryFinance,type BudgetRecord,type ExpenseRecord,type ExpenseCategory} from "./model";

export function FinanceEditor({propertyId,period,budget,onClose,onSaved}:{propertyId:string;period:string;budget?:CategoryFinance;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("expenses");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();
 const [category,setCategory]=useState(budget?.category??"");const [amount,setAmount]=useState(budget?.hasBudget?budget.budgetAmount:"");const [date,setDate]=useState(businessToday);const [description,setDescription]=useState("");const [reference,setReference]=useState("");
 const [history,setHistory]=useState<BudgetRecord|null>(null);const [version,setVersion]=useState(budget?.budgetVersion??0);const [loading,setLoading]=useState(!!budget?.budgetId);const [error,setError]=useState("");const [errors,setErrors]=useState<Record<string,string[]>>({});const [busy,setBusy]=useState(false);const [conflict,setConflict]=useState(false);const [ambiguous,setAmbiguous]=useState(false);
 const initialBudgetLoaded=useRef(false);const lock=useRef(false);const last=useRef<SubmissionKey|null>(null);const form=useRef<HTMLFormElement>(null);const root=`/api/v1/properties/${encodeURIComponent(propertyId)}`;
 useEffect(()=>{if(!budget?.budgetId||initialBudgetLoaded.current)return;const controller=new AbortController();setLoading(true);void api<unknown>(root+"/budgets/"+budget.budgetId,{signal:controller.signal}).then(value=>{const current=readBudget(value,propertyId,period,budget.category);if(!controller.signal.aborted){initialBudgetLoaded.current=true;setHistory(current);setVersion(current.version);setAmount(current.amount);}}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});return()=>controller.abort();},[api,root,propertyId,period,budget?.budgetId,budget?.category,locale]);
 const clear=(key:string)=>setErrors(previous=>({...previous,[key]:[]}));
 const invalid=(key:string)=>!!errors[key]?.length;
 const field=(key:string)=>invalid(key)?<FieldError id={"finance-error-"+key}>{errors[key].join("\n")}</FieldError>:null;
 const fieldProps=(key:string)=>({"aria-invalid":invalid(key),"aria-describedby":invalid(key)?"finance-error-"+key:undefined});
 async function loadCurrent(){if(lock.current)return;lock.current=true;setBusy(true);try{const summary=readFinanceSummary(await api<unknown>(root+"/finance-summary?period="+period),propertyId,period);const current=summary.categories.find(row=>row.category===category)!;
  if(current.budgetId){const record=readBudget(await api<unknown>(root+"/budgets/"+current.budgetId),propertyId,period,current.category);setHistory(record);setVersion(record.version);setAmount(record.amount);}else{setHistory(null);setVersion(0);setAmount("");}
  setError("");setConflict(false);setErrors({});setAmbiguous(false);last.current=null;
 }catch(err){setError(errorMessage(err,locale));}finally{lock.current=false;setBusy(false);}}
 async function save(event:React.FormEvent){event.preventDefault();if(lock.current||loading||conflict||(budget?.budgetId&&!history))return;
  const validation:Record<string,string[]>={};const normalized=normalizeFinanceAmount(amount,!!budget);
  if(!(expenseCategories as readonly string[]).includes(category))validation.category=[t("categoryRequired")];if(normalized===null)validation.amount=[t(budget?"budgetAmountInvalid":"amountInvalid")];
  if(!budget&&(!date||date<"2000-01-01"||date>businessToday()||!Number.isFinite(Date.parse(date))))validation.occurredDate=[t("dateInvalid")];
  if(!description.trim()||description.trim().length>500)validation[budget?"explanation":"description"]=[t("descriptionRequired")];if(!budget&&reference.trim().length>100)validation.documentReference=[t("referenceInvalid")];
  setErrors(validation);setError("");if(Object.keys(validation).length){requestAnimationFrame(()=>form.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus());return;}
  const payload=budget?{period,category,amount:normalized,currency:"TRY",explanation:description.trim(),version}:{category,amount:normalized,currency:"TRY",occurredDate:date,description:description.trim(),documentReference:reference.trim()||null};
  last.current=submissionKey(last.current,JSON.stringify({propertyId,...payload}),()=>crypto.randomUUID());lock.current=true;setBusy(true);setAmbiguous(false);
  try{const value=await api<unknown>(root+(budget?"/budgets":"/expenses"),{method:"POST",body:JSON.stringify({requestId:last.current.id,...payload})});
   if(budget){const result=readBudget(value,propertyId,period,category as ExpenseCategory);notice.success(t(result.replayed?"budgetReplayed":"budgetSaved"));}
   else{const result=readExpense(value,propertyId);if(result.status==="CANCELLED")notice.warning(t("replayedCancelled"));else notice.success(t("saved"));}
   onSaved();onClose();
  }catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError){setErrors(err.errors);setConflict(err.status===409&&!!budget);}setAmbiguous(!(err instanceof ApiError)||err.status>=500);}
  finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><ReceiptText className="size-9 text-primary"/><DialogTitle>{t(budget?"editBudget":"newExpense")}</DialogTitle><DialogDescription>{t(budget?"budgetHelp":"expenseHelp")}</DialogDescription></DialogHeader>
  <form ref={form} onSubmit={save} noValidate className="space-y-5"><fieldset disabled={busy||loading} className="space-y-5">
   {budget&&<div className="rounded-xl bg-muted p-3 text-sm"><strong className="flex flex-wrap items-center gap-2">{period} · <ExpenseCategoryLabel category={budget.category} label={t(budget.category)}/></strong></div>}
   {!budget&&<div role="radiogroup" aria-labelledby="finance-category-label" tabIndex={-1} {...fieldProps("category")}><p id="finance-category-label" className="mb-2 text-sm font-medium">{t("category")} *</p><div className="request-type-grid">{expenseCategories.map(value=>{const c=categoryStyle[value];return <button key={value} type="button" role="radio" aria-checked={category===value} onClick={()=>{setCategory(value);clear("category");}} className={`request-type-option${category===value?` tone-${c.tone}`:""}`}><span className={`dashboard-icon tone-${c.tone}`}><c.icon aria-hidden="true"/></span><span>{t(value)}</span></button>;})}</div>{field("category")}</div>}
   <div><Label htmlFor="finance-amount">{t(budget?"budgetAmount":"amount")} (TRY) *</Label><Input id="finance-amount" inputMode="decimal" autoComplete="off" value={amount} placeholder={locale==="tr"?"1250,00":"1250.00"} {...fieldProps("amount")} onChange={event=>{setAmount(event.target.value);clear("amount");}}/>{field("amount")}<p className="mt-1 text-xs text-muted-foreground">{t(budget?"zeroBudgetHelp":"moneyHelp")}</p></div>
   {!budget&&<div><Label htmlFor="finance-date">{t("date")} *</Label><Input id="finance-date" type="date" min="2000-01-01" max={businessToday()} value={date} {...fieldProps("occurredDate")} onInput={event=>{setDate(event.currentTarget.value);clear("occurredDate");}}/>{field("occurredDate")}</div>}
   <div><Label htmlFor="finance-description">{t(budget?"explanation":"description")} *</Label><Textarea id="finance-description" maxLength={500} value={description} {...fieldProps(budget?"explanation":"description")} onChange={event=>{setDescription(event.target.value);clear(budget?"explanation":"description");}}/>{field(budget?"explanation":"description")}</div>
   {!budget&&<div><Label htmlFor="finance-reference">{t("reference")}</Label><Input id="finance-reference" maxLength={100} value={reference} {...fieldProps("documentReference")} onChange={event=>{setReference(event.target.value);clear("documentReference");}}/>{field("documentReference")}<p className="mt-1 text-xs text-muted-foreground">{t("referenceHelp")}</p></div>}
  </fieldset>
  {loading&&<p role="status">{ui("loading")}</p>}{error&&<MessageBanner action={budget?<Button type="button" variant="view" disabled={busy||loading} onClick={loadCurrent}><RefreshCw/>{t("loadCurrentBudget")}</Button>:undefined}>{error}</MessageBanner>}{ambiguous&&<MessageBanner kind="warning">{t("ambiguous")}</MessageBanner>}
  <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy||loading||conflict||!!budget?.budgetId&&!history}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t(budget?"saveBudget":"saveExpense")}</Button></DialogFooter>
  </form>
  {history&&<section className="mt-6 border-t pt-5"><h3 className="mb-3 font-semibold">{t("revisionHistory")}</h3><ol className="space-y-3">{history.revisions.map(row=><li key={row.id} className="rounded-xl border p-3 text-sm"><strong>{t("revision",{number:row.sequence})} · {formatDecimalCurrency(row.amount,locale)}</strong><p className="mt-1 whitespace-pre-wrap break-words">{row.explanation}</p><p className="mt-1 text-xs text-muted-foreground">{formatDateTime(row.createdAt,locale)}</p></li>)}</ol></section>}
 </ManagementPanel></Dialog>;
}

export function ExpenseDetail({propertyId,target,enabled,onClose,onSaved}:{propertyId:string;target:ExpenseRecord;enabled:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("expenses");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();const [record,setRecord]=useState<ExpenseRecord|null>(null);const [error,setError]=useState("");const [loading,setLoading]=useState(true);const [revision,setRevision]=useState(0);const [cancelling,setCancelling]=useState(false);const [reason,setReason]=useState("");const [invalid,setInvalid]=useState("");const [busy,setBusy]=useState(false);const [conflict,setConflict]=useState(false);const lock=useRef(false);const root=`/api/v1/properties/${encodeURIComponent(propertyId)}/expenses/${target.id}`;
 useEffect(()=>{const controller=new AbortController();setLoading(true);setRecord(null);setError("");setConflict(false);setCancelling(false);void api<unknown>(root,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setRecord(readExpense(value,propertyId));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});return()=>controller.abort();},[api,root,propertyId,locale,revision]);
 async function cancel(event:React.FormEvent){event.preventDefault();if(lock.current||!record||conflict)return;if(!reason.trim()){setInvalid(t("reasonRequired"));return;}lock.current=true;setBusy(true);setError("");try{const result=readExpense(await api<unknown>(root+"/cancel",{method:"PUT",body:JSON.stringify({reason:reason.trim(),version:record.version})}),propertyId);setRecord(result);setCancelling(false);notice.success(t("cancelled"));onSaved();}catch(err){setError(errorMessage(err,locale));setConflict(!(err instanceof ApiError)||err.status===409||err.status>=500);}finally{lock.current=false;setBusy(false);}}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><ReceiptText className="size-9 text-primary"/><DialogTitle>{t("expenseDetail")}</DialogTitle><DialogDescription>{t("historyHelp")}</DialogDescription></DialogHeader>
  {loading&&<p role="status">{ui("loading")}</p>}{error&&<MessageBanner action={<Button variant="view" disabled={busy} onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{t("loadCurrent")}</Button>}>{error}</MessageBanner>}
  {record&&<><div className="grid gap-4 sm:grid-cols-2"><DetailField label={t("category")}><ExpenseCategoryLabel category={record.category} label={t(record.category)}/></DetailField><DetailField label={t("amount")}>{formatDecimalCurrency(record.amount,locale)}</DetailField><DetailField label={t("date")}>{formatDateOnly(record.occurredDate,locale)}</DetailField><DetailField label={ui("status")}>{t(record.status)}</DetailField><DetailField label={t("reference")}>{record.documentReference??"—"}</DetailField><DetailField label={t("createdAt")}>{formatDateTime(record.createdAt,locale)}</DetailField></div><p className="my-5 whitespace-pre-wrap break-words rounded-xl bg-muted p-4 text-sm">{record.description}</p>
   {record.status==="CANCELLED"&&<section className="rounded-xl border p-4 text-sm"><strong>{t("cancellationReason")}</strong><p className="my-2 whitespace-pre-wrap break-words">{record.cancellationReason}</p><p className="text-muted-foreground">{formatDateTime(record.cancelledAt!,locale)}</p></section>}
   {record.status==="RECORDED"&&enabled&&!cancelling&&<Button variant="danger" disabled={busy||conflict} onClick={()=>setCancelling(true)}><CircleX/>{t("cancelExpense")}</Button>}
   {cancelling&&<form onSubmit={cancel} className="space-y-4 rounded-xl border p-4"><p className="text-sm">{t("cancelHelp")}</p><Label htmlFor="expense-cancel-reason">{t("cancellationReason")} *</Label><Textarea autoFocus id="expense-cancel-reason" maxLength={500} value={reason} disabled={busy} aria-invalid={!!invalid} aria-describedby={invalid?"expense-cancel-error":undefined} onChange={event=>{setReason(event.target.value);setInvalid("");}}/>{invalid&&<FieldError id="expense-cancel-error">{invalid}</FieldError>}<div className="flex flex-wrap gap-2"><Button type="button" variant="outline" disabled={busy} onClick={()=>setCancelling(false)}>{ui("cancel")}</Button><Button type="submit" disabled={busy||conflict}>{busy?<LoaderCircle className="animate-spin"/>:<CircleX/>}{t("confirmCancel")}</Button></div></form>}
  </>}
  <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}
