"use client";
import {useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Copy,FileDown,Search} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useSearch} from "@/features/people/useRows";
import {Input} from "@/components/ui/input";
import {Skeleton} from "@/components/ui/skeleton";
import {Dialog,DialogContent,DialogDescription,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateOnly} from "@/lib/format";
import {SurveyCategoryTile,SurveyStateBadge} from "./SurveyBadges";
import {readPage,type SurveySummary} from "./model";

/** Chooses one survey for a quick action: copy (any) or export results (published or closed only). */
export function SurveyPicker({propertyId,purpose,onClose,onPick}:{propertyId:string;purpose:"copy"|"export";onClose:()=>void;onPick:(s:SurveySummary)=>void}){
 const t=useTranslations("surveys"),locale=useLocale() as "tr"|"en",api=useAuthorizedApi();
 const [term,setTerm]=useState(""),search=useSearch(term),[items,setItems]=useState<SurveySummary[]|null>(null),[error,setError]=useState("");
 useEffect(()=>{const c=new AbortController();setItems(null);setError("");const q=new URLSearchParams({page:"1",pageSize:"50"});if(search)q.set("search",search);
  void api<unknown>(`/api/v1/properties/${propertyId}/surveys?${q}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setItems(readPage(v,propertyId).items.filter(s=>purpose==="copy"||s.status!=="DRAFT"));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});
  return()=>c.abort();},[api,propertyId,search,purpose,locale]);
 return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><DialogContent className="sm:max-w-lg">
  <DialogHeader><DialogTitle>{t(purpose==="copy"?"pickCopy":"pickExport")}</DialogTitle><DialogDescription>{t(purpose==="copy"?"pickCopyHelp":"pickExportHelp")}</DialogDescription></DialogHeader>
  <div className="relative"><Search aria-hidden="true" className="pointer-events-none absolute left-3 top-3 size-4 text-muted-foreground"/><Input className="pl-9" maxLength={200} value={term} aria-label={t("search")} placeholder={t("search")} onChange={e=>setTerm(e.target.value)}/></div>
  {error?<MessageBanner presentation="inline">{error}</MessageBanner>:!items?<div className="space-y-2">{[0,1,2].map(i=><Skeleton key={i} className="h-14 rounded-xl"/>)}</div>:items.length===0?<p className="survey-empty">{t("pickEmpty")}</p>:
  <ul className="survey-pick">{items.map(s=><li key={s.id}><button type="button" onClick={()=>onPick(s)}><SurveyCategoryTile category={s.category}/><span className="min-w-0 flex-1"><strong className="line-clamp-1">{s.title}</strong><small>{formatDateOnly(s.startDate,locale)} – {formatDateOnly(s.endDate,locale)}</small></span><SurveyStateBadge state={s.state} label={t(`state_${s.state}`)}/>{purpose==="copy"?<Copy aria-hidden="true" className="size-4"/>:<FileDown aria-hidden="true" className="size-4"/>}</button></li>)}</ul>}
 </DialogContent></Dialog>;
}
