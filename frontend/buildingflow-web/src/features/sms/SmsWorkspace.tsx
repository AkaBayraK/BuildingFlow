"use client";
import {useEffect,useMemo,useRef,useState} from "react";
import {PhoneInput} from "@/components/forms/ContactInputs";
import {formatPhone,validPhone} from "@/lib/contact";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {MessageSquareText,Send,Pencil,Plus,RefreshCw,RotateCcw,Coins,Settings,FileText,ShieldCheck,Eye,ListChecks} from "lucide-react";
import {ConsentsTab,PreviewTab,SenderTab} from "./SmsComplianceTabs";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Badge} from "@/components/ui/badge";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,SummaryCard,DetailField,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime,formatNumber} from "@/lib/format";
import {readOverview,readAccount,readCredit,readTemplate,readMessage,readPage,segments,placeholders,preview,type SmsOverview,type SmsAccount,type SmsTemplate,type SmsType,type SmsMessage,type SmsCredit,type Page} from "./model";

type Tab="account"|"templates"|"messages"|"sender"|"preview"|"consents";
const tabs:Tab[]=["account","templates","messages","sender","preview","consents"];
const tabIcon={account:Settings,templates:FileText,messages:MessageSquareText,sender:ShieldCheck,preview:Eye,consents:ListChecks} as const satisfies Record<Tab,unknown>;
const statusVariant={QUEUED:"secondary",SENT:"default",FAILED:"destructive"} as const;

export function SmsWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),scope=usePropertyScope(),identity=useIdentity();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r)),admin=identity.hasRealmRole("SUPER_ADMIN"),siteAdmin=["SUPER_ADMIN","SITE_ADMIN"].some(r=>identity.hasRealmRole(r));
 const [tab,setTab]=useState<Tab>("account"),[overview,setOverview]=useState<SmsOverview|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0),[send,setSend]=useState(false);
 const root=`/api/v1/properties/${propertyId}/sms`;const refresh=()=>setRevision(n=>n+1);
 useEffect(()=>{const c=new AbortController();setError("");if(manager)void api<unknown>(root,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setOverview(readOverview(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,propertyId,revision,locale,manager]);
 if(!manager)return <div className="py-10"><MessageBanner>{ui("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 const writable=!!scope.property?.isEnabled&&!scope.error;
 return <div className="py-8"><WorkspaceHeading title={t("title")} description={t("help")} scope={scope.property?.name??ui("loading")} icon={MessageSquareText} action={<Button disabled={!writable||!overview?.account?.isEnabled} onClick={()=>setSend(true)}><Send/>{t("send")}</Button>}/>
  {error&&<MessageBanner className="mb-4" action={<Button variant="view" onClick={refresh}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
  <div role="tablist" aria-label={t("title")} className="mb-4 flex flex-wrap gap-2">{tabs.map(value=>{const I=tabIcon[value];return <Button key={value} role="tab" aria-selected={tab===value} variant={tab===value?"default":"outline"} onClick={()=>setTab(value)}><I aria-hidden="true"/>{t(`tab_${value}`)}</Button>;})}</div>
  {!overview&&!error?<p role="status">{ui("loading")}</p>:overview&&<div role="tabpanel">
   {tab==="account"&&<AccountTab root={root} propertyId={propertyId} account={overview.account} admin={admin&&writable} onChanged={refresh}/>}
   {tab==="templates"&&<TemplatesTab root={root} types={overview.types} writable={writable}/>}
   {tab==="messages"&&<MessagesTab root={root} revision={revision}/>}
   {tab==="sender"&&<SenderTab root={root} propertyId={propertyId} admin={siteAdmin&&writable}/>}
   {tab==="preview"&&<PreviewTab root={root}/>}
   {tab==="consents"&&<ConsentsTab root={root}/>}
  </div>}
  {send&&overview?.account&&<SendDialog root={root} balance={overview.account.creditBalance} onClose={()=>setSend(false)} onSent={()=>{setTab("messages");refresh();}}/>}
 </div>;
}

function AccountTab({root,propertyId,account,admin,onChanged}:{root:string;propertyId:string;account:SmsAccount|null;admin:boolean;onChanged:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale();const [edit,setEdit]=useState(false),[credit,setCredit]=useState(false),[creditRevision,setCreditRevision]=useState(0);
 if(!account)return <section className="rounded-2xl border bg-card p-6 shadow-sm"><h2 className="font-semibold">{t("noAccount")}</h2><p className="mt-2 text-sm text-muted-foreground">{t(admin?"noAccountAdmin":"noAccountHelp")}</p>{admin&&<Button className="mt-4" onClick={()=>setEdit(true)}><Plus/>{t("defineAccount")}</Button>}{edit&&<AccountDialog root={root} propertyId={propertyId} account={null} onClose={()=>setEdit(false)} onSaved={onChanged}/>}</section>;
 return <div className="space-y-4">
  {account.provider==="MOCK"&&<MessageBanner>{t("mockNotice")}</MessageBanner>}
  {account.isLowCredit&&<MessageBanner>{t("lowCredit",{balance:formatNumber(account.creditBalance,locale)})}</MessageBanner>}
  <div className="grid gap-4 sm:grid-cols-3"><SummaryCard title={t("balance")} value={formatNumber(account.creditBalance,locale)} note={t("lowThreshold",{value:formatNumber(account.lowCreditThreshold,locale)})} icon={Coins}/><SummaryCard title={t("provider")} value={t(`provider_${account.provider}`)} note={t("senderHeaderValue",{value:account.senderHeader})} icon={Settings}/><SummaryCard title={ui("status")} value={ui(account.isEnabled?"active":"inactive")} note={account.name} icon={MessageSquareText}/></div>
  <section className="rounded-2xl border bg-card p-5 shadow-sm"><div className="flex flex-wrap items-center justify-between gap-2"><h2 className="font-semibold">{t("accountDetails")}</h2>{admin&&<div className="flex gap-2"><Button variant="edit" onClick={()=>setEdit(true)}><Pencil/>{t("edit")}</Button><Button onClick={()=>setCredit(true)}><Coins/>{t("loadCredit")}</Button></div>}</div>
   <dl className="mt-4 grid gap-4 sm:grid-cols-2"><DetailField label={t("accountName")}>{account.name}</DetailField><DetailField label={t("senderHeader")}>{account.senderHeader}</DetailField><DetailField label={t("username")}>{account.username??"—"}</DetailField><DetailField label={t("secret")}>{t(account.hasSecret?"secretSet":"secretMissing")}</DetailField><DetailField label={t("apiUrl")}>{account.apiUrl??"—"}</DetailField></dl></section>
  <CreditHistory root={root} revision={creditRevision}/>
  {edit&&<AccountDialog root={root} propertyId={propertyId} account={account} onClose={()=>setEdit(false)} onSaved={onChanged}/>}
  {credit&&<CreditDialog root={root} onClose={()=>setCredit(false)} onSaved={()=>{setCreditRevision(n=>n+1);onChanged();}}/>}
 </div>;
}

function useSaving(){const lock=useRef(false);const [busy,setBusy]=useState(false);
 async function run(action:()=>Promise<void>){if(lock.current)return;lock.current=true;setBusy(true);try{await action();}finally{lock.current=false;setBusy(false);}}
 return {busy,run};}

function AccountDialog({root,propertyId,account,onClose,onSaved}:{root:string;propertyId:string;account:SmsAccount|null;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [name,setName]=useState(account?.name??""),[header,setHeader]=useState(account?.senderHeader??""),[username,setUsername]=useState(account?.username??""),[secret,setSecret]=useState(""),[apiUrl,setApiUrl]=useState(account?.apiUrl??""),[threshold,setThreshold]=useState(String(account?.lowCreditThreshold??100)),[enabled,setEnabled]=useState(account?.isEnabled??true),[error,setError]=useState("");
 function save(e:React.FormEvent){e.preventDefault();setError("");const low=Number(threshold);
  if(!name.trim()||!/^[A-Za-z0-9 .-]{3,11}$/.test(header.trim())||!Number.isSafeInteger(low)||low<0){setError(t("accountInvalid"));return;}
  void run(async()=>{try{readAccount(await api(root+"/account",{method:"PUT",body:JSON.stringify({name:name.trim(),provider:"MOCK",senderHeader:header.trim(),username:username.trim()||null,secret:secret||null,apiUrl:apiUrl.trim()||null,lowCreditThreshold:low,isEnabled:enabled,version:account?.version??0})}),propertyId);notice.success(t("accountSaved"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(account?"editAccount":"defineAccount")}</DialogTitle><DialogDescription>{t("mockNotice")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-account-name">{t("accountName")} *</Label><Input id="sms-account-name" maxLength={100} value={name} onChange={e=>setName(e.target.value)}/>
   <Label htmlFor="sms-provider">{t("provider")}</Label><select id="sms-provider" className={toolbarSelectClass+" w-full"} value="MOCK" disabled><option value="MOCK">{t("provider_MOCK")}</option></select>
   <Label htmlFor="sms-header">{t("senderHeader")} *</Label><Input id="sms-header" maxLength={11} value={header} onChange={e=>setHeader(e.target.value)} aria-describedby="sms-header-help"/><p id="sms-header-help" className="text-xs text-muted-foreground">{t("senderHeaderHelp")}</p>
   <Label htmlFor="sms-username">{t("username")}</Label><Input id="sms-username" maxLength={100} autoComplete="off" value={username} onChange={e=>setUsername(e.target.value)}/>
   <Label htmlFor="sms-secret">{t("secret")}</Label><Input id="sms-secret" type="password" maxLength={500} autoComplete="new-password" placeholder={account?.hasSecret?t("secretKeep"):""} value={secret} onChange={e=>setSecret(e.target.value)}/>
   <Label htmlFor="sms-api-url">{t("apiUrl")}</Label><Input id="sms-api-url" type="url" maxLength={300} placeholder="https://" value={apiUrl} onChange={e=>setApiUrl(e.target.value)}/>
   <Label htmlFor="sms-threshold">{t("lowThresholdLabel")} *</Label><Input id="sms-threshold" inputMode="numeric" value={threshold} onChange={e=>setThreshold(e.target.value)}/>
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={enabled} onChange={e=>setEnabled(e.target.checked)}/>{ui("active")}</label>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function CreditDialog({root,onClose,onSaved}:{root:string;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [type,setType]=useState<"LOAD"|"ADJUST">("LOAD"),[quantity,setQuantity]=useState(""),[price,setPrice]=useState(""),[currency,setCurrency]=useState("TRY"),[reference,setReference]=useState(""),[description,setDescription]=useState(""),[error,setError]=useState("");
 function save(e:React.FormEvent){e.preventDefault();setError("");const amount=Number(quantity),unit=price.trim()?Number(price.replace(",",".")):null;
  if(!Number.isSafeInteger(amount)||amount===0||type==="LOAD"&&amount<0||unit!==null&&(!Number.isFinite(unit)||unit<0)){setError(t("creditInvalid"));return;}
  void run(async()=>{try{readCredit(await api(root+"/account/credits",{method:"POST",body:JSON.stringify({type,quantity:amount,unitPrice:unit,currency:unit===null?null:currency,referenceNo:reference.trim()||null,description:description.trim()||null})}));notice.success(t("creditSaved"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("loadCredit")}</DialogTitle><DialogDescription>{t("creditHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-credit-type">{t("creditType")}</Label><select id="sms-credit-type" className={toolbarSelectClass+" w-full"} value={type} onChange={e=>setType(e.target.value as "LOAD"|"ADJUST")}><option value="LOAD">{t("credit_LOAD")}</option><option value="ADJUST">{t("credit_ADJUST")}</option></select>
   <Label htmlFor="sms-credit-quantity">{t("quantity")} *</Label><Input id="sms-credit-quantity" inputMode="numeric" value={quantity} onChange={e=>setQuantity(e.target.value)} aria-describedby="sms-credit-quantity-help"/><p id="sms-credit-quantity-help" className="text-xs text-muted-foreground">{t(type==="LOAD"?"quantityLoadHelp":"quantityAdjustHelp")}</p>
   <div className="grid grid-cols-2 gap-3"><div className="space-y-2"><Label htmlFor="sms-credit-price">{t("unitPrice")}</Label><Input id="sms-credit-price" inputMode="decimal" value={price} onChange={e=>setPrice(e.target.value)}/></div><div className="space-y-2"><Label htmlFor="sms-credit-currency">{t("currency")}</Label><Input id="sms-credit-currency" maxLength={3} value={currency} onChange={e=>setCurrency(e.target.value.toUpperCase())}/></div></div>
   <Label htmlFor="sms-credit-reference">{t("referenceNo")}</Label><Input id="sms-credit-reference" maxLength={100} value={reference} onChange={e=>setReference(e.target.value)}/>
   <Label htmlFor="sms-credit-description">{t("description")}</Label><Textarea id="sms-credit-description" maxLength={500} value={description} onChange={e=>setDescription(e.target.value)}/>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function CreditHistory({root,revision}:{root:string;revision:number}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();const [data,setData]=useState<Page<SmsCredit>|null>(null),[page,setPage]=useState(1),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0);
 useEffect(()=>{const c=new AbortController();setLoading(true);setError("");void api<unknown>(`${root}/account/credits?page=${page}&pageSize=10`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,readCredit));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,root,page,revision,retry,locale]);
 const columns=useMemo<ColumnDef<SmsCredit>[]>(()=>[{id:"date",header:t("date"),cell:({row})=>formatDateTime(row.original.createdAt,locale)},{id:"type",header:t("creditType"),cell:({row})=>t(`credit_${row.original.type}`)},{id:"quantity",header:t("quantity"),cell:({row})=><span className={row.original.quantity<0?"text-destructive":""}>{row.original.quantity>0?"+":""}{formatNumber(row.original.quantity,locale)}</span>},{id:"balance",header:t("balanceAfter"),cell:({row})=>formatNumber(row.original.balanceAfter,locale)},{id:"note",header:t("description"),cell:({row})=>[row.original.referenceNo,row.original.description].filter(Boolean).join(" · ")||"—"}],[t,locale]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><h2 className="p-5 pb-0 font-semibold">{t("creditHistory")}</h2><RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t("noCredits")} card={c=><><strong>{t(`credit_${c.type}`)} · {c.quantity>0?"+":""}{formatNumber(c.quantity,locale)}</strong><p>{formatDateTime(c.createdAt,locale)} · {t("balanceAfter")}: {formatNumber(c.balanceAfter,locale)}</p></>}/></section>;
}

function TemplatesTab({root,types,writable}:{root:string;types:SmsType[];writable:boolean}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [items,setItems]=useState<SmsTemplate[]|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0),[edit,setEdit]=useState<SmsTemplate|null>(null);
 useEffect(()=>{const c=new AbortController();setError("");void api<unknown>(root+"/templates",{signal:c.signal}).then(v=>{if(!c.signal.aborted){if(!Array.isArray(v))throw new Error("Invalid templates");setItems(v.map(readTemplate));}}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,root,revision,locale]);
 function reset(template:SmsTemplate){void run(async()=>{try{await api(`${root}/templates/${template.id}?version=${template.version}`,{method:"DELETE"});notice.success(t("templateReset"));setRevision(n=>n+1);}catch(reason){notice.error(errorMessage(reason,locale));}});}
 if(error)return <MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>;
 if(!items)return <p role="status">{ui("loading")}</p>;
 return <div className="grid gap-4 lg:grid-cols-2">{items.map(template=>{const type=types.find(x=>x.code===template.typeCode);return <article key={template.typeCode} className="rounded-2xl border bg-card p-5 shadow-sm"><div className="flex flex-wrap items-start justify-between gap-2"><div><h3 className="font-semibold">{t(`type_${template.typeCode}`)}</h3><p className="text-xs text-muted-foreground">{t(`purpose_${template.purpose}`)} · {template.isSystemDefault?t("systemDefault"):template.name}</p></div><div className="flex gap-1">{!template.isEnabled&&<Badge variant="secondary">{ui("inactive")}</Badge>}<Badge variant="outline">{t("segmentsValue",{count:template.estimatedSegments})}</Badge></div></div>
  <p className="mt-3 whitespace-pre-wrap break-words rounded-lg bg-muted/40 p-3 text-sm">{template.content}</p>
  {type&&<p className="mt-2 text-xs text-muted-foreground">{t("parameters")}: {type.parameters.map(p=>`{{${p}}}`).join(", ")}</p>}
  {writable&&<div className="mt-4 flex gap-2"><Button variant="edit" size="sm" disabled={busy} onClick={()=>setEdit(template)}><Pencil/>{t("edit")}</Button>{!template.isSystemDefault&&<Button variant="outline" size="sm" disabled={busy} onClick={()=>reset(template)}><RotateCcw/>{t("resetTemplate")}</Button>}</div>}
 </article>;})}
 {edit&&<TemplateDialog root={root} template={edit} type={types.find(x=>x.code===edit.typeCode)!} onClose={()=>setEdit(null)} onSaved={()=>setRevision(n=>n+1)}/>}</div>;
}

function TemplateDialog({root,template,type,onClose,onSaved}:{root:string;template:SmsTemplate;type:SmsType;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [name,setName]=useState(template.isSystemDefault?t(`type_${template.typeCode}`):template.name),[content,setContent]=useState(template.content),[enabled,setEnabled]=useState(template.isEnabled),[error,setError]=useState("");const area=useRef<HTMLTextAreaElement>(null);
 const unknown=placeholders(content).filter(p=>!type.parameters.includes(p));const sample=Object.fromEntries(type.parameters.map(p=>[p,t(`sample_${p}`)]));const info=segments(preview(content,sample));
 function insert(parameter:string){const el=area.current;const token=`{{${parameter}}}`;if(!el){setContent(c=>c+token);return;}const start=el.selectionStart,end=el.selectionEnd;setContent(c=>c.slice(0,start)+token+c.slice(end));requestAnimationFrame(()=>{el.focus();el.setSelectionRange(start+token.length,start+token.length);});}
 function save(e:React.FormEvent){e.preventDefault();setError("");if(!name.trim()||!content.trim()||unknown.length>0||content.length>1000){setError(t("templateInvalid"));return;}
  void run(async()=>{try{readTemplate(await api(template.id?`${root}/templates/${template.id}`:`${root}/templates`,{method:template.id?"PUT":"POST",body:JSON.stringify({typeCode:template.typeCode,name:name.trim(),content:content.trim(),isEnabled:enabled,version:template.version})}));notice.success(t("templateSaved"));onSaved();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t(`type_${template.typeCode}`)}</DialogTitle><DialogDescription>{t("templateHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-template-name">{t("templateName")} *</Label><Input id="sms-template-name" maxLength={100} value={name} onChange={e=>setName(e.target.value)}/>
   <Label htmlFor="sms-template-content">{t("content")} *</Label><div className="flex flex-wrap gap-2">{type.parameters.map(p=><Button key={p} type="button" variant="outline" size="sm" onClick={()=>insert(p)}>{`{{${p}}}`}</Button>)}</div>
   <Textarea id="sms-template-content" ref={area} rows={5} maxLength={1000} value={content} onChange={e=>setContent(e.target.value)} aria-describedby="sms-template-meta" aria-invalid={unknown.length>0}/>
   <p id="sms-template-meta" className="text-xs text-muted-foreground">{t("segmentInfo",{encoding:info.encoding,length:info.length,count:info.segments})}{unknown.length>0&&<span className="block text-destructive">{t("unknownParameters",{names:unknown.join(", ")})}</span>}</p>
   <div><p className="text-xs font-medium">{t("preview")}</p><p className="mt-1 whitespace-pre-wrap break-words rounded-lg border p-3 text-sm">{preview(content,sample)}</p></div>
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={enabled} onChange={e=>setEnabled(e.target.checked)}/>{t("templateEnabled")}</label>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function MessagesTab({root,revision}:{root:string;revision:number}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();const [data,setData]=useState<Page<SmsMessage>|null>(null),[page,setPage]=useState(1),[status,setStatus]=useState(""),[loading,setLoading]=useState(true),[error,setError]=useState(""),[retry,setRetry]=useState(0);
 useEffect(()=>{const c=new AbortController();setLoading(true);setError("");void api<unknown>(`${root}/messages?`+new URLSearchParams({page:String(page),pageSize:"10",...(status?{status}:{})}),{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,readMessage));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,root,page,status,revision,retry,locale]);
 const columns=useMemo<ColumnDef<SmsMessage>[]>(()=>[{id:"date",header:t("date"),cell:({row})=>formatDateTime(row.original.createdAt,locale)},{id:"type",header:t("type"),cell:({row})=>t(`type_${row.original.typeCode}`)},{id:"phone",header:t("phone"),cell:({row})=><span className="font-mono">{row.original.phoneMasked}</span>},{id:"message",header:t("message"),cell:({row})=><span className="line-clamp-2 max-w-md whitespace-pre-wrap break-words">{row.original.message}</span>},{id:"segments",header:t("segments"),cell:({row})=>row.original.segments},{id:"status",header:ui("status"),cell:({row})=><StatusCell message={row.original}/>}],[t,ui,locale]);
 return <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="flex flex-wrap items-center justify-between gap-2 p-4"><label className="flex items-center gap-2 text-sm">{ui("status")}<select className={toolbarSelectClass} value={status} onChange={e=>{setStatus(e.target.value);setPage(1);}}><option value="">{t("allStatuses")}</option>{["QUEUED","SENT","FAILED"].map(s=><option key={s} value={s}>{t(`status_${s}`)}</option>)}</select></label><Button variant="view" onClick={()=>setRetry(n=>n+1)}><RefreshCw/>{t("refresh")}</Button></div>
  <RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={()=>setRetry(n=>n+1)} empty={t("noMessages")} card={m=><><div className="flex items-center justify-between gap-2"><strong>{t(`type_${m.typeCode}`)}</strong><StatusCell message={m}/></div><p className="font-mono text-xs">{m.phoneMasked} · {formatDateTime(m.createdAt,locale)}</p><p className="whitespace-pre-wrap break-words text-sm">{m.message}</p></>}/></section>;
}

function StatusCell({message}:{message:SmsMessage}){const t=useTranslations("sms");
 return <span className="flex flex-col items-start gap-1"><Badge variant={statusVariant[message.status]}>{t(`status_${message.status}`)}</Badge>{message.errorCode&&<small className="text-muted-foreground">{t.has(`error_${message.errorCode}`)?t(`error_${message.errorCode}`):message.errorCode}</small>}</span>;}

function SendDialog({root,balance,onClose,onSent}:{root:string;balance:number;onClose:()=>void;onSent:()=>void}){
 const t=useTranslations("sms"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [phone,setPhone]=useState(""),[body,setBody]=useState(""),[error,setError]=useState("");const request=useRef<{key:string;id:string}|null>(null);const info=segments(body.trim());
 function save(e:React.FormEvent){e.preventDefault();setError("");const number=formatPhone(phone);if(!validPhone(number)||!body.trim()||info.segments>10){setError(t("sendInvalid"));return;}
  const key=number+"\n"+body.trim();if(request.current?.key!==key)request.current={key,id:crypto.randomUUID()};const id=request.current.id;
  void run(async()=>{try{readMessage(await api(root+"/messages",{method:"POST",body:JSON.stringify({requestId:id,personId:null,phoneNumber:number,body:body.trim()})}));notice.success(t("sent"));onSent();onClose();}catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("send")}</DialogTitle><DialogDescription>{t("sendHelp",{balance:formatNumber(balance,locale)})}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="sms-send-phone">{t("phone")} *</Label><PhoneInput id="sms-send-phone" autoComplete="off" required value={phone} onValueChange={setPhone}/>
   <Label htmlFor="sms-send-body">{t("message")} *</Label><Textarea id="sms-send-body" rows={5} maxLength={1000} value={body} onChange={e=>setBody(e.target.value)} aria-describedby="sms-send-meta"/>
   <p id="sms-send-meta" className="text-xs text-muted-foreground">{t("segmentInfo",{encoding:info.encoding,length:info.length,count:info.segments})}</p>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}><Send/>{busy?ui("saving"):t("send")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}
