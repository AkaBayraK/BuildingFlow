"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {BadgeCheck,Ban,CircleX,KeyRound,Landmark,LoaderCircle,LockKeyhole,PlugZap,Plus,PowerOff,RefreshCw,Save,Settings,Star} from "lucide-react";
import {ApiError,errorMessage,useAuthorizedApi,type Building} from "@/services/api";
import {useRows} from "@/features/people/useRows";
import {submissionKey,type SubmissionKey} from "@/features/dues/model";
import {businessToday,formatDateOnly,formatDateTime,type Locale} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {ToneBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {FilterChips} from "@/components/management/FilterChips";
import {FieldError,MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {bankAccountPurposes,bankAccountStatuses,canDecide,formatIban,isIban,normalizeIban,readBankAccount,readBankAccountPage,readMerchant,readMerchantPage,readProviders,readSettings,
 type BankAccount,type BankAccountPurpose,type MerchantAccount,type PaymentProvider,type PaymentSettings} from "./model";
import {allIcon,bankStatusStyle,purposeStyle} from "./payment-style";

const rootOf=(propertyId:string)=>`/api/v1/properties/${encodeURIComponent(propertyId)}`;
type Decision={account:BankAccount;mode:"APPROVE"|"REJECT"|"CLOSE"};

export function PaymentSettingsPanel({propertyId,enabled}:{propertyId:string;enabled:boolean}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();
 const [settings,setSettings]=useState<PaymentSettings|null>(null);const [draft,setDraft]=useState<PaymentSettings|null>(null);const [error,setError]=useState("");const [busy,setBusy]=useState(false);const [revision,setRevision]=useState(0);
 const [providers,setProviders]=useState<PaymentProvider[]>([]);const lock=useRef(false);
 useEffect(()=>{const controller=new AbortController();setError("");void api<unknown>(rootOf(propertyId)+"/payment-settings",{signal:controller.signal}).then(value=>{if(controller.signal.aborted)return;const read=readSettings(value,propertyId);setSettings(read);setDraft(read);}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));});return()=>controller.abort();},[api,propertyId,locale,revision]);
 useEffect(()=>{const controller=new AbortController();void api<unknown>("/api/v1/payment-providers",{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setProviders(readProviders(value));}).catch(()=>{if(!controller.signal.aborted)setProviders([]);});return()=>controller.abort();},[api]);
 const buildings=useRows<Building>(rootOf(propertyId)+"/buildings?"+new URLSearchParams({page:"1",pageSize:"100",sortBy:"name",sortDirection:"asc"}),0);
 const dirty=!!settings&&!!draft&&(["onlineEnabled","partialAllowed","showIbanToResidents","requireDualApproval","receiptPrefix"] as const).some(key=>settings[key]!==draft[key]);
 const prefixValid=!!draft&&/^[A-Z0-9]{2,10}$/.test(draft.receiptPrefix);
 async function save(event:React.FormEvent){
  event.preventDefault();if(lock.current||!draft||!prefixValid)return;lock.current=true;setBusy(true);setError("");
  try{const read=readSettings(await api<unknown>(rootOf(propertyId)+"/payment-settings",{method:"PUT",body:JSON.stringify({onlineEnabled:draft.onlineEnabled,partialAllowed:draft.partialAllowed,showIbanToResidents:draft.showIbanToResidents,requireDualApproval:draft.requireDualApproval,receiptPrefix:draft.receiptPrefix,commissionPolicy:"ABSORBED",version:draft.version})}),propertyId);setSettings(read);setDraft(read);notice.success(t("settingsSaved"));}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)setRevision(n=>n+1);}
  finally{lock.current=false;setBusy(false);}
 }
 const toggle=(key:"onlineEnabled"|"partialAllowed"|"showIbanToResidents"|"requireDualApproval")=><label key={key} className="flex cursor-pointer items-start gap-3 rounded-xl border bg-muted p-4 text-sm"><input type="checkbox" className="mt-1 size-4 accent-primary" checked={draft?.[key]??false} disabled={!draft?.canEdit||!enabled} onChange={event=>setDraft(value=>value&&{...value,[key]:event.target.checked})}/><span><strong>{t("setting_"+key)}</strong><span className="mt-1 block text-xs text-muted-foreground">{t("setting_"+key+"Help")}</span></span></label>;
 return <div className="space-y-6">
  <section className="rounded-2xl border bg-card p-5 shadow-sm" aria-labelledby="payment-settings-title">
   <h2 id="payment-settings-title" className="mb-4 flex items-center gap-2 font-semibold"><Settings className="size-5 text-primary"/>{t("settingsTitle")}</h2>
   {error&&<MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
   {!draft&&!error&&<p role="status">{ui("loading")}</p>}
   {draft&&<form noValidate onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="grid gap-3 border-0 p-0 md:grid-cols-2">
    {(["onlineEnabled","partialAllowed","showIbanToResidents","requireDualApproval"] as const).map(toggle)}
    <div className="space-y-2"><Label htmlFor="payment-receipt-prefix">{t("receiptPrefix")}</Label><Input id="payment-receipt-prefix" value={draft.receiptPrefix} maxLength={10} disabled={!draft.canEdit||!enabled} aria-invalid={!prefixValid} aria-describedby="payment-receipt-prefix-help" onChange={event=>setDraft({...draft,receiptPrefix:event.target.value.toUpperCase().replace(/[^A-Z0-9]/g,"")})}/><p id="payment-receipt-prefix-help" className="text-xs text-muted-foreground">{t("receiptPrefixHelp",{example:`${draft.receiptPrefix||"BF"}-${businessToday().slice(0,4)}-000001`})}</p>{!prefixValid&&<FieldError>{t("receiptPrefixInvalid")}</FieldError>}</div>
    <div className="space-y-2"><Label>{t("commissionPolicy")}</Label><p className="rounded-xl border bg-muted p-3 text-sm">{t("commission_ABSORBED")}</p><p className="text-xs text-muted-foreground">{t("commissionHelp")}</p></div>
   </fieldset>
   {!draft.canEdit&&<MessageBanner kind="info" presentation="inline">{t("settingsReadOnly")}</MessageBanner>}
   {draft.canEdit&&<div className="flex justify-end gap-2"><Button type="button" variant="outline" disabled={busy||!dirty} onClick={()=>setDraft(settings)}>{ui("cancel")}</Button><Button type="submit" disabled={busy||!dirty||!prefixValid||!enabled}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{ui("save")}</Button></div>}
   </form>}
  </section>
  <BankAccountsSection propertyId={propertyId} enabled={enabled} settings={settings} buildings={buildings.data?.items??[]}/>
  <MerchantsSection propertyId={propertyId} enabled={enabled} settings={settings} providers={providers} buildings={buildings.data?.items??[]}/>
 </div>;
}

function BuildingSelect({id,value,onChange,buildings}:{id:string;value:string;onChange:(value:string)=>void;buildings:Building[]}){
 const t=useTranslations("onlinePayments");
 if(!buildings.length)return null;
 return <div className="space-y-2"><Label htmlFor={id}>{t("building")}</Label><select id={id} className={toolbarSelectClass+" w-full"} value={value} onChange={event=>onChange(event.target.value)}><option value="">{t("allBuildings")}</option>{buildings.map(b=><option key={b.id} value={b.id}>{b.name}</option>)}</select><p className="text-xs text-muted-foreground">{t("buildingHelp")}</p></div>;
}

function BankAccountsSection({propertyId,enabled,settings,buildings}:{propertyId:string;enabled:boolean;settings:PaymentSettings|null;buildings:Building[]}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [status,setStatus]=useState("");const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);const [creating,setCreating]=useState(false);const [decision,setDecision]=useState<Decision|null>(null);const [busy,setBusy]=useState<string|null>(null);
 useEffect(()=>setPage(1),[status]);
 const query=new URLSearchParams({page:String(page),pageSize:"20"});if(status)query.set("status",status);
 const rows=useRows<BankAccount>(rootOf(propertyId)+"/bank-accounts?"+query,revision,true,readBankAccountPage);
 const canPropose=!!settings?.canPropose;const canApprove=!!settings?.canEdit;
 async function makeDefault(account:BankAccount){
  if(busy)return;setBusy(account.id);
  try{readBankAccount(await api<unknown>(rootOf(propertyId)+`/bank-accounts/${account.id}/default`,{method:"PUT",body:JSON.stringify({version:account.version})}));notice.success(t("defaultSaved"));setRevision(n=>n+1);}
  catch(err){notice.error(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)setRevision(n=>n+1);}
  finally{setBusy(null);}
 }
 const actions=(account:BankAccount)=><div className="flex flex-wrap gap-1">
  {account.status==="PENDING_APPROVAL"&&canDecide(account.createdByMe,canApprove,settings?.requireDualApproval)&&<><Button variant="view" size="sm" disabled={!enabled} onClick={()=>setDecision({account,mode:"APPROVE"})}><BadgeCheck/>{t("approve")}</Button><Button variant="danger" size="sm" disabled={!enabled} onClick={()=>setDecision({account,mode:"REJECT"})}><CircleX/>{t("reject")}</Button></>}
  {account.status==="PENDING_APPROVAL"&&account.createdByMe&&settings?.requireDualApproval&&<span className="text-xs text-muted-foreground">{t("awaitingSecondApprover")}</span>}
  {account.status==="ACTIVE"&&canApprove&&!account.isDefault&&<Button variant="edit" size="sm" disabled={!enabled||busy===account.id} onClick={()=>void makeDefault(account)}>{busy===account.id?<LoaderCircle className="animate-spin"/>:<Star/>}{t("makeDefault")}</Button>}
  {account.status==="ACTIVE"&&canApprove&&<Button variant="warn" size="sm" disabled={!enabled} onClick={()=>setDecision({account,mode:"CLOSE"})}><LockKeyhole/>{t("closeAccount")}</Button>}
 </div>;
 const columns:ColumnDef<BankAccount>[]=[
  {id:"bank",header:t("bankName"),cell:({row})=><div><strong>{row.original.bankName}</strong>{row.original.isDefault&&<ToneBadge tone="success" icon={Star}>{t("default")}</ToneBadge>}<span className="block text-xs text-muted-foreground">{row.original.accountHolder}</span></div>},
  {id:"iban",header:"IBAN",cell:({row})=><span className="font-mono text-xs whitespace-nowrap">{formatIban(row.original.iban)}</span>},
  {id:"scope",header:t("scope"),cell:({row})=>{const style=purposeStyle[row.original.purpose];return <div className="space-y-1"><ToneBadge tone={style.tone} icon={style.icon}>{t("purpose_"+row.original.purpose)}</ToneBadge><span className="block text-xs text-muted-foreground">{row.original.buildingName??t("allBuildings")}</span></div>;}},
  {id:"validity",header:t("validity"),cell:({row})=><span className="text-sm">{formatDateOnly(row.original.validFrom,locale)} – {row.original.validTo?formatDateOnly(row.original.validTo,locale):t("openEnded")}</span>},
  {id:"status",header:ui("status"),cell:({row})=>{const style=bankStatusStyle[row.original.status];return <div><ToneBadge tone={style.tone} icon={style.icon}>{t("bank_"+row.original.status)}</ToneBadge>{row.original.statusReason&&<span className="block max-w-48 wrap-anywhere text-xs text-muted-foreground">{row.original.statusReason}</span>}</div>;}},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}
 ];
 const chips=[{value:"",label:t("allStatuses"),icon:allIcon},...bankAccountStatuses.map(value=>({value,label:t("bank_"+value),icon:bankStatusStyle[value].icon}))];
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-labelledby="bank-accounts-title">
  <div className="flex flex-wrap items-center justify-between gap-3 border-b p-5"><h2 id="bank-accounts-title" className="flex items-center gap-2 font-semibold"><Landmark className="size-5 text-primary"/>{t("bankAccounts")}</h2>{canPropose&&<Button variant="view" disabled={!enabled} onClick={()=>setCreating(true)}><Plus/>{t("newBankAccount")}</Button>}</div>
  <div className="space-y-2 p-5 pb-0"><FilterChips label={ui("status")} items={chips} value={status} onChange={setStatus}/><p className="text-xs text-muted-foreground">{t(settings?.requireDualApproval?"dualApprovalHelp":"singleApprovalHelp")}</p></div>
  <RecordList compact data={rows.data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} empty={<><Landmark className="mx-auto mb-3 size-8"/><strong>{t("noBankAccounts")}</strong></>}
   card={account=>{const style=bankStatusStyle[account.status];return <><div className="mb-2 flex flex-wrap justify-between gap-2"><strong>{account.bankName}</strong><ToneBadge tone={style.tone} icon={style.icon}>{t("bank_"+account.status)}</ToneBadge></div><p className="mb-1 font-mono text-xs">{formatIban(account.iban)}</p><p className="mb-3 text-sm">{account.accountHolder} · {t("purpose_"+account.purpose)} · {account.buildingName??t("allBuildings")}</p>{actions(account)}</>;}}/>
  {creating&&<BankAccountEditor propertyId={propertyId} buildings={buildings} onClose={()=>setCreating(false)} onSaved={()=>setRevision(n=>n+1)}/>}
  {decision&&<BankAccountDecision propertyId={propertyId} decision={decision} onClose={()=>setDecision(null)} onSaved={()=>setRevision(n=>n+1)}/>}
 </section>;
}

function BankAccountEditor({propertyId,buildings,onClose,onSaved}:{propertyId:string;buildings:Building[];onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();
 const [bankName,setBankName]=useState("");const [holder,setHolder]=useState("");const [iban,setIban]=useState("TR");const [purpose,setPurpose]=useState<BankAccountPurpose>("ALL");const [building,setBuilding]=useState("");const [validFrom,setValidFrom]=useState(businessToday);const [validTo,setValidTo]=useState("");
 const [errors,setErrors]=useState<Record<string,string[]>>({});const [error,setError]=useState("");const [busy,setBusy]=useState(false);const [ambiguous,setAmbiguous]=useState(false);const lock=useRef(false);const last=useRef<SubmissionKey|null>(null);
 const normalized=normalizeIban(iban);
 const field=(key:string)=>errors[key]?.length?<FieldError id={"bank-error-"+key}>{errors[key].join(" ")}</FieldError>:null;
 async function save(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;const validation:Record<string,string[]>={};
  if(!bankName.trim())validation.bankName=[t("bankNameRequired")];if(!holder.trim())validation.accountHolder=[t("holderRequired")];if(!isIban(normalized))validation.iban=[t("ibanInvalid")];
  if(!validFrom||validFrom<"2000-01-01")validation.validFrom=[t("validFromRequired")];if(validTo&&validTo<validFrom)validation.validTo=[t("validToInvalid")];
  setErrors(validation);if(Object.keys(validation).length)return;
  const payload={buildingId:building||null,bankName:bankName.trim(),accountHolder:holder.trim(),iban:normalized,purpose,validFrom,validTo:validTo||null};
  last.current=submissionKey(last.current,JSON.stringify({propertyId,...payload}),()=>crypto.randomUUID());lock.current=true;setBusy(true);setError("");setAmbiguous(false);
  try{const saved=readBankAccount(await api<unknown>(rootOf(propertyId)+"/bank-accounts",{method:"POST",body:JSON.stringify({requestId:last.current.id,...payload})}));notice.success(t(saved.status==="ACTIVE"?"bankAccountActive":"bankAccountProposed"));onSaved();onClose();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError)setErrors(err.errors);setAmbiguous(!(err instanceof ApiError)||err.status>=500);}
  finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><Landmark className="size-8 text-primary"/><DialogTitle>{t("newBankAccount")}</DialogTitle><DialogDescription>{t("newBankAccountHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}{ambiguous&&<MessageBanner kind="warning">{t("ambiguous")}</MessageBanner>}
  <form noValidate onSubmit={save}><fieldset disabled={busy} className="space-y-4 border-0 p-0">
   <div className="space-y-2"><Label htmlFor="bank-name">{t("bankName")}</Label><Input id="bank-name" value={bankName} maxLength={100} aria-invalid={!!errors.bankName?.length} aria-describedby={errors.bankName?.length?"bank-error-bankName":undefined} onChange={event=>setBankName(event.target.value)}/>{field("bankName")}</div>
   <div className="space-y-2"><Label htmlFor="bank-holder">{t("accountHolder")}</Label><Input id="bank-holder" value={holder} maxLength={200} aria-invalid={!!errors.accountHolder?.length} aria-describedby={errors.accountHolder?.length?"bank-error-accountHolder":undefined} onChange={event=>setHolder(event.target.value)}/>{field("accountHolder")}</div>
   <div className="space-y-2"><Label htmlFor="bank-iban">IBAN</Label><Input id="bank-iban" className="font-mono" value={iban} maxLength={34} inputMode="text" autoComplete="off" aria-invalid={!!errors.iban?.length||(normalized.length>=26&&!isIban(normalized))} aria-describedby="bank-iban-help" onChange={event=>setIban(event.target.value)}/><p id="bank-iban-help" className="text-xs text-muted-foreground">{normalized.length>=26&&isIban(normalized)?t("ibanValid",{iban:formatIban(normalized)}):t("ibanHelp")}</p>{field("iban")}</div>
   <div className="space-y-2"><Label>{t("purpose")}</Label><FilterChips label={t("purpose")} value={purpose} onChange={value=>setPurpose(value as BankAccountPurpose)} items={bankAccountPurposes.map(value=>({value,label:t("purpose_"+value),icon:purposeStyle[value].icon}))}/></div>
   <BuildingSelect id="bank-building" value={building} onChange={setBuilding} buildings={buildings}/>
   <div className="grid gap-3 sm:grid-cols-2"><div className="space-y-2"><Label htmlFor="bank-from">{t("validFrom")}</Label><Input id="bank-from" type="date" min="2000-01-01" value={validFrom} aria-invalid={!!errors.validFrom?.length} onInput={event=>setValidFrom(event.currentTarget.value)}/>{field("validFrom")}</div><div className="space-y-2"><Label htmlFor="bank-to">{t("validToOptional")}</Label><Input id="bank-to" type="date" min={validFrom} value={validTo} aria-invalid={!!errors.validTo?.length} onInput={event=>setValidTo(event.currentTarget.value)}/>{field("validTo")}</div></div>
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("proposeAccount")}</Button></DialogFooter>
  </fieldset></form>
 </ManagementPanel></Dialog>;
}

function BankAccountDecision({propertyId,decision,onClose,onSaved}:{propertyId:string;decision:Decision;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();
 const {account,mode}=decision;const [reason,setReason]=useState("");const [validTo,setValidTo]=useState(businessToday);const [error,setError]=useState("");const [fieldError,setFieldError]=useState("");const [busy,setBusy]=useState(false);const lock=useRef(false);
 const needsReason=mode!=="APPROVE";
 async function submit(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;if(needsReason&&!reason.trim()){setFieldError(t("reasonRequired"));return;}if(mode==="CLOSE"&&(!validTo||validTo<account.validFrom)){setFieldError(t("validToInvalid"));return;}
  lock.current=true;setBusy(true);setError("");
  const url=rootOf(propertyId)+`/bank-accounts/${account.id}/`+(mode==="CLOSE"?"close":"decision");
  const body=mode==="CLOSE"?{validTo,reason:reason.trim(),version:account.version}:{decision:mode,reason:needsReason?reason.trim():null,version:account.version};
  try{readBankAccount(await api<unknown>(url,{method:"PUT",body:JSON.stringify(body)}));notice.success(t("decision_"+mode));onSaved();onClose();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)onSaved();}
  finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><KeyRound className="size-8 text-primary"/><DialogTitle>{t("decisionTitle_"+mode)}</DialogTitle><DialogDescription>{account.bankName} · {formatIban(account.iban)}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}
  <dl className="grid grid-cols-2 gap-2 text-sm"><dt className="text-muted-foreground">{t("accountHolder")}</dt><dd>{account.accountHolder}</dd><dt className="text-muted-foreground">{t("purpose")}</dt><dd>{t("purpose_"+account.purpose)}</dd><dt className="text-muted-foreground">{t("building")}</dt><dd>{account.buildingName??t("allBuildings")}</dd><dt className="text-muted-foreground">{t("proposedAt")}</dt><dd>{formatDateTime(account.createdAt,locale as Locale)}</dd></dl>
  <MessageBanner kind="info" presentation="inline">{t("decisionHelp_"+mode)}</MessageBanner>
  <form noValidate onSubmit={submit}><fieldset disabled={busy} className="space-y-4 border-0 p-0">
   {mode==="CLOSE"&&<div className="space-y-2"><Label htmlFor="bank-close-date">{t("validTo")}</Label><Input id="bank-close-date" type="date" min={account.validFrom} value={validTo} onInput={event=>{setValidTo(event.currentTarget.value);setFieldError("");}}/></div>}
   {needsReason&&<div className="space-y-2"><Label htmlFor="bank-decision-reason">{t("reason")}</Label><Textarea id="bank-decision-reason" maxLength={500} value={reason} aria-invalid={!!fieldError} aria-describedby={fieldError?"bank-decision-error":undefined} onChange={event=>{setReason(event.target.value);setFieldError("");}}/></div>}
   {fieldError&&<FieldError id="bank-decision-error">{fieldError}</FieldError>}
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" variant={mode==="APPROVE"?"default":"destructive"} disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:mode==="APPROVE"?<BadgeCheck/>:mode==="REJECT"?<CircleX/>:<LockKeyhole/>}{t("confirm_"+mode)}</Button></DialogFooter>
  </fieldset></form>
 </ManagementPanel></Dialog>;
}

function MerchantsSection({propertyId,enabled,settings,providers,buildings}:{propertyId:string;enabled:boolean;settings:PaymentSettings|null;providers:PaymentProvider[];buildings:Building[]}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale() as Locale;const api=useAuthorizedApi();const notice=useNotification();
 const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);const [creating,setCreating]=useState(false);const [busy,setBusy]=useState<string|null>(null);
 const rows=useRows<MerchantAccount>(rootOf(propertyId)+"/merchant-accounts?"+new URLSearchParams({page:String(page),pageSize:"20"}),revision,true,readMerchantPage);
 const canPropose=!!settings?.canPropose;const canApprove=!!settings?.canEdit;
 async function act(merchant:MerchantAccount,action:"default"|"deactivate"|"test"){
  if(busy)return;setBusy(merchant.id+action);
  try{
   const result=readMerchant(await api<unknown>(rootOf(propertyId)+`/merchant-accounts/${merchant.id}/${action}`,action==="test"?{method:"POST"}:{method:"PUT",body:JSON.stringify({version:merchant.version})}));
   if(action==="test")notice[result.lastCheckSucceeded?"success":"warning"](t(result.lastCheckSucceeded?"testSucceeded":"testFailed"));else notice.success(t("merchant_"+action));
   setRevision(n=>n+1);
  }catch(err){notice.error(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)setRevision(n=>n+1);}
  finally{setBusy(null);}
 }
 const actions=(merchant:MerchantAccount)=><div className="flex flex-wrap gap-1">
  {merchant.isEnabled&&canPropose&&<Button variant="view" size="sm" disabled={!enabled||!!busy} onClick={()=>void act(merchant,"test")}>{busy===merchant.id+"test"?<LoaderCircle className="animate-spin"/>:<PlugZap/>}{t("testConnection")}</Button>}
  {merchant.isEnabled&&canApprove&&!merchant.isDefault&&<Button variant="edit" size="sm" disabled={!enabled||!!busy} onClick={()=>void act(merchant,"default")}><Star/>{t("makeDefault")}</Button>}
  {merchant.isEnabled&&canApprove&&<Button variant="warn" size="sm" disabled={!enabled||!!busy} onClick={()=>void act(merchant,"deactivate")}><PowerOff/>{t("deactivate")}</Button>}
 </div>;
 const check=(merchant:MerchantAccount)=>merchant.lastCheckedAt?<ToneBadge tone={merchant.lastCheckSucceeded?"success":"error"} icon={merchant.lastCheckSucceeded?BadgeCheck:CircleX}>{formatDateTime(merchant.lastCheckedAt,locale)}</ToneBadge>:<span className="text-xs text-muted-foreground">{t("notTested")}</span>;
 const columns:ColumnDef<MerchantAccount>[]=[
  {id:"label",header:t("merchantLabel"),cell:({row})=><div><strong>{row.original.label}</strong>{row.original.isDefault&&<ToneBadge tone="success" icon={Star}>{t("default")}</ToneBadge>}<span className="block text-xs text-muted-foreground">{providers.find(p=>p.code===row.original.providerCode)?.name??row.original.providerCode} · {row.original.buildingName??t("allBuildings")}</span></div>},
  {id:"merchant",header:t("merchantId"),cell:({row})=><div className="text-xs"><span className="font-mono">{row.original.merchantId}</span><span className="block text-muted-foreground">{t("secretReference")}: <span className="font-mono">{row.original.secretReference}</span></span><ToneBadge tone={row.original.secretConfigured?"success":"warning"} icon={KeyRound}>{t(row.original.secretConfigured?"secretConfigured":"secretMissing")}</ToneBadge></div>},
  {id:"environment",header:t("environment"),cell:()=> <ToneBadge tone="info" icon={PlugZap}>{t("environment_SANDBOX")}</ToneBadge>},
  {id:"check",header:t("lastCheck"),cell:({row})=>check(row.original)},
  {id:"status",header:ui("status"),cell:({row})=><ToneBadge tone={row.original.isEnabled?"success":"neutral"} icon={row.original.isEnabled?BadgeCheck:Ban}>{ui(row.original.isEnabled?"active":"inactive")}</ToneBadge>},
  {id:"actions",header:ui("actions"),cell:({row})=>actions(row.original)}
 ];
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm" aria-labelledby="merchants-title">
  <div className="flex flex-wrap items-center justify-between gap-3 border-b p-5"><h2 id="merchants-title" className="flex items-center gap-2 font-semibold"><PlugZap className="size-5 text-primary"/>{t("merchants")}</h2>{canPropose&&<Button variant="view" disabled={!enabled} onClick={()=>setCreating(true)}><Plus/>{t("newMerchant")}</Button>}</div>
  <p className="px-5 pt-4 text-xs text-muted-foreground">{t("merchantsHelp")}</p>
  <RecordList compact data={rows.data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} empty={<><PlugZap className="mx-auto mb-3 size-8"/><strong>{t("noMerchants")}</strong></>}
   card={merchant=><><div className="mb-2 flex flex-wrap justify-between gap-2"><strong>{merchant.label}</strong><ToneBadge tone={merchant.isEnabled?"success":"neutral"} icon={merchant.isEnabled?BadgeCheck:Ban}>{ui(merchant.isEnabled?"active":"inactive")}</ToneBadge></div><p className="mb-1 font-mono text-xs">{merchant.merchantId}</p><div className="mb-3">{check(merchant)}</div>{actions(merchant)}</>}/>
  {creating&&<MerchantEditor propertyId={propertyId} providers={providers} buildings={buildings} onClose={()=>setCreating(false)} onSaved={()=>setRevision(n=>n+1)}/>}
 </section>;
}

function MerchantEditor({propertyId,providers,buildings,onClose,onSaved}:{propertyId:string;providers:PaymentProvider[];buildings:Building[];onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();
 const available=providers.filter(p=>p.enabled&&p.integrated);
 const [provider,setProvider]=useState(available[0]?.code??"");const [label,setLabel]=useState("");const [merchantId,setMerchantId]=useState("");const [secret,setSecret]=useState("");const [building,setBuilding]=useState("");
 const [errors,setErrors]=useState<Record<string,string[]>>({});const [error,setError]=useState("");const [busy,setBusy]=useState(false);const [ambiguous,setAmbiguous]=useState(false);const lock=useRef(false);const last=useRef<SubmissionKey|null>(null);
 const field=(key:string)=>errors[key]?.length?<FieldError id={"merchant-error-"+key}>{errors[key].join(" ")}</FieldError>:null;
 async function save(event:React.FormEvent){
  event.preventDefault();if(lock.current)return;const validation:Record<string,string[]>={};
  if(!provider)validation.providerCode=[t("providerRequired")];if(!label.trim())validation.label=[t("labelRequired")];if(!merchantId.trim())validation.merchantId=[t("merchantIdRequired")];if(!/^[A-Za-z0-9_:-]{1,100}$/.test(secret))validation.secretReference=[t("secretReferenceInvalid")];
  setErrors(validation);if(Object.keys(validation).length)return;
  const payload={buildingId:building||null,providerCode:provider,label:label.trim(),merchantId:merchantId.trim(),secretReference:secret,environment:"SANDBOX"};
  last.current=submissionKey(last.current,JSON.stringify({propertyId,...payload}),()=>crypto.randomUUID());lock.current=true;setBusy(true);setError("");setAmbiguous(false);
  try{readMerchant(await api<unknown>(rootOf(propertyId)+"/merchant-accounts",{method:"POST",body:JSON.stringify({requestId:last.current.id,...payload})}));notice.success(t("merchantSaved"));onSaved();onClose();}
  catch(err){setError(errorMessage(err,locale));if(err instanceof ApiError)setErrors(err.errors);setAmbiguous(!(err instanceof ApiError)||err.status>=500);}
  finally{lock.current=false;setBusy(false);}
 }
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><PlugZap className="size-8 text-primary"/><DialogTitle>{t("newMerchant")}</DialogTitle><DialogDescription>{t("newMerchantHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}{ambiguous&&<MessageBanner kind="warning">{t("ambiguous")}</MessageBanner>}
  {!available.length&&<MessageBanner kind="warning">{t("noProviderEnabled")}</MessageBanner>}
  <form noValidate onSubmit={save}><fieldset disabled={busy||!available.length} className="space-y-4 border-0 p-0">
   <div className="space-y-2"><Label htmlFor="merchant-provider">{t("provider")}</Label><select id="merchant-provider" className={toolbarSelectClass+" w-full"} value={provider} onChange={event=>setProvider(event.target.value)}>{available.map(p=><option key={p.code} value={p.code}>{p.name}</option>)}</select>{field("providerCode")}</div>
   <div className="space-y-2"><Label htmlFor="merchant-label">{t("merchantLabel")}</Label><Input id="merchant-label" value={label} maxLength={100} aria-invalid={!!errors.label?.length} onChange={event=>setLabel(event.target.value)}/>{field("label")}</div>
   <div className="space-y-2"><Label htmlFor="merchant-id">{t("merchantId")}</Label><Input id="merchant-id" className="font-mono" value={merchantId} maxLength={100} autoComplete="off" aria-invalid={!!errors.merchantId?.length} onChange={event=>setMerchantId(event.target.value)}/>{field("merchantId")}</div>
   <div className="space-y-2"><Label htmlFor="merchant-secret">{t("secretReference")}</Label><Input id="merchant-secret" className="font-mono" value={secret} maxLength={100} autoComplete="off" spellCheck={false} aria-invalid={!!errors.secretReference?.length} aria-describedby="merchant-secret-help" onChange={event=>setSecret(event.target.value.trim())}/><p id="merchant-secret-help" className="text-xs text-muted-foreground">{t("secretReferenceHelp")}</p>{field("secretReference")}</div>
   <BuildingSelect id="merchant-building" value={building} onChange={setBuilding} buildings={buildings}/>
   <div className="space-y-2"><Label>{t("environment")}</Label><p className="rounded-xl border bg-muted p-3 text-sm">{t("environment_SANDBOX")}</p><p className="text-xs text-muted-foreground">{t("liveDisabledHelp")}</p></div>
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy||!available.length}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{ui("save")}</Button></DialogFooter>
  </fieldset></form>
 </ManagementPanel></Dialog>;
}
