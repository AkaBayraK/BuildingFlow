"use client";
import {useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Sparkles} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {FilterChips} from "@/components/management/FilterChips";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {Choices} from "@/features/person-center/Choices";
import {workspace,type Workspace} from "@/features/person-center/model";
import {adviceStyle,adviceLevelStyle,relationStyle} from "./supplier-style";
import {adviceKinds,readAdvice,type Advice,type AdviceKind} from "./advice-model";
const AreaIcon=relationStyle.area.icon;
export function SupplierAdvice({tenant}:{tenant:Workspace}){
 const t=useTranslations("suppliers"),locale=useLocale(),api=useAuthorizedApi();
 const [kind,setKind]=useState<AdviceKind>("MATCH"),[property,setProperty]=useState<Workspace|null>(null),[category,setCategory]=useState<Workspace|null>(null),[consent,setConsent]=useState(false),[busy,setBusy]=useState(false),[error,setError]=useState(""),[advice,setAdvice]=useState<Advice|null>(null);
 async function run(){if(!property||!consent)return;setBusy(true);setError("");setAdvice(null);try{setAdvice(readAdvice(await api<unknown>(`/api/v2/tenants/${tenant.id}/supplier-advice`,{method:"POST",body:JSON.stringify({kind,propertyId:property.id,categoryId:kind==="MATCH"?category?.id??null:null,consent:true,note:null})}),kind));}catch(e){setError(errorMessage(e,locale));}finally{setBusy(false);}}
 return <section className="space-y-4">
  <MessageBanner kind="info">{t("adviceHelp")}</MessageBanner>
  <FilterChips label={t("adviceKind")} value={kind} disabled={busy} onChange={v=>{setKind(v as AdviceKind);setAdvice(null);}} items={adviceKinds.map(k=>({value:k,label:t(`advice_${k}`),icon:adviceStyle[k].icon}))}/>
  <div className="grid gap-3 sm:grid-cols-2"><Choices path={`/api/v2/tenants/${tenant.id}/supplier-property-choices`} value={property?.id??""} onChange={v=>{setProperty(v);setAdvice(null);}} parse={workspace} id="supplier-advice-property" label={t("invoiceProperty")} disabled={busy}/>{kind==="MATCH"&&<div className="space-y-1"><Choices path={`/api/v2/tenants/${tenant.id}/supplier-categories`} value={category?.id??""} onChange={v=>{setCategory(v);setAdvice(null);}} parse={workspace} id="supplier-advice-category" label={t("adviceCategory")} disabled={busy}/>{category&&<Button type="button" size="sm" variant="outline" onClick={()=>setCategory(null)}>{t("adviceAllCategories")}</Button>}</div>}</div>
  <label className="flex items-start gap-2 text-sm"><input type="checkbox" className="mt-1" checked={consent} disabled={busy} onChange={e=>setConsent(e.target.checked)}/><span>{t("adviceConsent")}</span></label>
  <Button type="button" disabled={busy||!property||!consent} onClick={()=>void run()}><Sparkles aria-hidden="true"/>{busy?t("adviceRunning"):t("adviceRun")}</Button>
  {error&&<MessageBanner>{error}</MessageBanner>}
  {advice&&<div className="space-y-3" aria-live="polite"><p className="text-xs text-muted-foreground">{t("adviceSource",{source:advice.source,remaining:advice.remainingToday})}</p>
   {!advice.items.length&&<p className="text-sm text-muted-foreground">{t("noAdvice")}</p>}
   <ul className="space-y-2">{advice.items.map(i=>{const level=adviceLevelStyle[i.level];return <li key={i.id} className="space-y-1 rounded-xl border p-3 text-sm"><div className="flex flex-wrap items-center justify-between gap-2"><strong className="break-words">{i.supplierName}</strong><ToneBadge icon={level.icon} tone={level.tone}>{t(`adviceLevel_${i.level}`)}</ToneBadge></div><p>{t(`adviceReason_${i.reasonCode}`)}</p><p className="flex flex-wrap gap-x-3 text-xs text-muted-foreground"><AreaIcon aria-hidden="true" className="size-4 shrink-0"/>{Object.entries(i.evidence).map(([k,v])=><span key={k}>{t(`adviceEvidence_${k}`)}: {v}</span>)}<span>n={i.sampleCount}</span></p></li>;})}</ul>
   <p className="text-xs text-muted-foreground">{t("adviceDecision")}</p></div>}
 </section>;
}
