"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {BarChart3,CalendarPlus,CircleStop,Copy,EyeOff,FileDown,LoaderCircle,Lock,Pencil,RefreshCw,Send,Star,Trash2} from "lucide-react";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Skeleton} from "@/components/ui/skeleton";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {DetailField} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {businessToday,formatDateOnly,formatDateTime} from "@/lib/format";
import {ParticipationBar,SurveyCategoryBadge,SurveyCategoryTile,SurveyStateBadge} from "./SurveyBadges";
import {SurveyPreview} from "./SurveyPreview";
import {isChoice,participation,readDetail,readResults,resultsCsv,share,type QuestionResult,type SurveyDetail,type SurveyResults,type SurveySummary} from "./model";

type Locale="tr"|"en";
/** Fetches current results and saves them as a CSV file in the browser; no file is stored on the server. */
export function useSurveyExport(propertyId:string){
 const t=useTranslations("surveys"),locale=useLocale() as Locale,api=useAuthorizedApi(),notice=useNotification();const [busy,setBusy]=useState(false);
 const run=useCallback(async(s:SurveySummary)=>{
  setBusy(true);
  try{const r=readResults(await api<unknown>(`/api/v1/properties/${propertyId}/surveys/${s.id}/results`),propertyId);
   if(r.hidden){notice.warning(t("exportHidden",{count:r.minAnonymousResults}));return;}
   const csv=resultsCsv(r,{question:t("csvQuestion"),type:t("csvType"),answer:t("csvAnswer"),count:t("csvCount"),unit:t("csvUnit"),date:t("csvDate"),types:{SINGLE_CHOICE:t("type_SINGLE_CHOICE"),MULTIPLE_CHOICE:t("type_MULTIPLE_CHOICE"),RATING:t("type_RATING"),TEXT:t("type_TEXT")},rating:n=>t("ratingValue",{value:n})});
   const url=URL.createObjectURL(new Blob([csv],{type:"text/csv;charset=utf-8"}));const a=document.createElement("a");a.href=url;a.download=`anket-${s.startDate}-${s.id.slice(0,8)}.csv`;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),10000);
   notice.success(t("exported"));
  }catch(e){notice.error(errorMessage(e,locale));}finally{setBusy(false);}
 },[api,propertyId,notice,t,locale]);
 return {busy,run};
}

export function SurveyDetailPanel({propertyId,survey,canWrite,onClose,onChanged,onEdit,onCopy}:{propertyId:string;survey:SurveySummary;canWrite:boolean;onClose:()=>void;onChanged:()=>void;onEdit:(d:SurveyDetail)=>void;onCopy:(id:string)=>void}){
 const t=useTranslations("surveys"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),notice=useNotification(),exporter=useSurveyExport(propertyId);
 const root=`/api/v1/properties/${propertyId}/surveys/${survey.id}`;
 const [detail,setDetail]=useState<SurveyDetail|null>(null),[results,setResults]=useState<SurveyResults|null>(null),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 const [busy,setBusy]=useState(false),[stale,setStale]=useState(false),[confirm,setConfirm]=useState<"publish"|"close"|"delete"|null>(null),[endDate,setEndDate]=useState(survey.endDate),[extending,setExtending]=useState(false);
 const lock=useRef(false);
 useEffect(()=>{const c=new AbortController();setError("");
  void (async()=>{try{const d=readDetail(await api<unknown>(root,{signal:c.signal}),propertyId);if(c.signal.aborted)return;setDetail(d);setEndDate(d.survey.endDate);
   if(d.survey.status!=="DRAFT"){const r=readResults(await api<unknown>(`${root}/results`,{signal:c.signal}),propertyId);if(!c.signal.aborted)setResults(r);}else setResults(null);}
   catch(e){if(!c.signal.aborted)setError(errorMessage(e,locale));}})();
  return()=>c.abort();},[api,root,propertyId,locale,revision]);
 const s=detail?.survey??survey;
 async function act(kind:"publish"|"close"|"delete"|"schedule"){
  if(lock.current||!canWrite)return;lock.current=true;setBusy(true);setStale(false);
  try{
   if(kind==="delete"){await api(`${root}?version=${s.version}`,{method:"DELETE"});notice.success(t("deleted"));onChanged();onClose();return;}
   const body=kind==="schedule"?{endDate,version:s.version}:{version:s.version};
   readDetail(await api<unknown>(`${root}/${kind}`,{method:"PUT",body:JSON.stringify(body)}),propertyId);
   notice.success(t(kind==="publish"?"published":kind==="close"?"closed":"extended"));setConfirm(null);setExtending(false);onChanged();setRevision(n=>n+1);
  }catch(e){if(e instanceof ApiError&&e.status===409)setStale(true);if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));}
  finally{lock.current=false;setBusy(false);}
 }
 const p=participation(s),pct=(n:number)=>new Intl.NumberFormat(locale==="en"?"en-GB":"tr-TR",{style:"percent",maximumFractionDigits:0}).format(n/100);
 const today=businessToday();
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy} className="survey-panel sm:max-w-[min(960px,calc(100vw-2rem))]">
  <DialogHeader><div className="flex items-start gap-3"><SurveyCategoryTile category={s.category} size="lg"/><div className="min-w-0"><DialogTitle>{s.title}</DialogTitle><DialogDescription className="mt-1 flex flex-wrap items-center gap-2"><SurveyStateBadge state={s.state} label={t(`state_${s.state}`)}/><SurveyCategoryBadge category={s.category} label={t(`category_${s.category}`)}/>{s.isAnonymous&&<span className="survey-flag"><EyeOff aria-hidden="true"/>{t("anonymous")}</span>}</DialogDescription></div></div></DialogHeader>
  {error&&<MessageBanner presentation="inline" action={<Button variant="view" size="sm" onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}
  {stale&&<MessageBanner kind="warning" presentation="inline" action={<Button variant="view" size="sm" onClick={()=>{setStale(false);setRevision(n=>n+1);onChanged();}}><RefreshCw/>{t("loadCurrent")}</Button>}>{t("staleHelp")}</MessageBanner>}
  <dl className="grid gap-x-6 sm:grid-cols-3">
   <DetailField label={t("startDate")}>{formatDateOnly(s.startDate,locale)}</DetailField><DetailField label={t("endDate")}>{formatDateOnly(s.endDate,locale)}</DetailField><DetailField label={t("questionCount")}>{s.questionCount}</DetailField>
   <DetailField label={t("audience")}>{t(`audience_${s.audience}`)}</DetailField><DetailField label={t("responseMode")}>{t(`mode_${s.responseMode}`)}</DetailField><DetailField label={t("modifiedAt")}>{formatDateTime(s.modifiedAt,locale)}</DetailField>
  </dl>
  {s.description&&<p className="my-4 whitespace-pre-wrap break-words rounded-xl bg-muted p-4 text-sm">{s.description}</p>}
  {s.status!=="DRAFT"&&<div className="survey-detail-meter"><p className="text-sm font-semibold">{t("col_participation")}</p><ParticipationBar count={t("participationCount",{responses:s.responseCount.toLocaleString(locale),eligible:s.eligibleCount.toLocaleString(locale)})} percent={p} percentText={pct(p)} label={t("participationLabel",{responses:s.responseCount,eligible:s.eligibleCount,percent:p})}/><p className="text-xs text-muted-foreground">{t(s.status==="CLOSED"?"eligibleFrozen":"eligibleLive")}</p></div>}

  {canWrite&&<div className="my-5 flex flex-wrap gap-2">
   {s.status==="DRAFT"&&detail&&<><Button variant="edit" disabled={busy} onClick={()=>onEdit(detail)}><Pencil/>{t("edit")}</Button><Button disabled={busy||s.questionCount===0||s.endDate<today} onClick={()=>setConfirm("publish")}><Send/>{t("publish")}</Button></>}
   {s.status==="PUBLISHED"&&<><Button variant="view" disabled={busy} onClick={()=>setExtending(v=>!v)}><CalendarPlus/>{t("extend")}</Button><Button variant="warn" disabled={busy} onClick={()=>setConfirm("close")}><CircleStop/>{t("close")}</Button></>}
   <Button variant="neutral" disabled={busy} onClick={()=>onCopy(s.id)}><Copy/>{t("copy")}</Button>
   {s.status!=="DRAFT"&&<Button variant="neutral" disabled={exporter.busy} onClick={()=>void exporter.run(s)}>{exporter.busy?<LoaderCircle className="animate-spin"/>:<FileDown/>}{t("export")}</Button>}
   {s.status==="DRAFT"&&<Button variant="danger" disabled={busy} onClick={()=>setConfirm("delete")}><Trash2/>{t("delete")}</Button>}
  </div>}
  {s.status==="DRAFT"&&s.endDate<today&&<MessageBanner kind="warning" presentation="inline">{t("publishEnded")}</MessageBanner>}
  {extending&&<div className="mb-5 flex flex-wrap items-end gap-3 rounded-xl border p-4"><div><Label htmlFor="survey-extend">{t("newEndDate")}</Label><Input id="survey-extend" type="date" min={s.startDate>today?s.startDate:today} max="2100-12-31" value={endDate} onChange={e=>setEndDate(e.target.value)}/></div><Button disabled={busy||!endDate||endDate<today||endDate<s.startDate} onClick={()=>void act("schedule")}>{busy?<LoaderCircle className="animate-spin"/>:<CalendarPlus/>}{t("saveEndDate")}</Button></div>}
  {confirm&&<div className="mb-5 space-y-3 rounded-xl border p-4"><p className="text-sm">{t(`confirm_${confirm}`)}</p><div className="flex flex-wrap gap-2">
   <Button variant={confirm==="delete"?"destructive":"default"} disabled={busy} onClick={()=>void act(confirm)}>{busy?<LoaderCircle className="animate-spin"/>:confirm==="delete"?<Trash2/>:confirm==="close"?<Lock/>:<Send/>}{t(`confirmAction_${confirm}`)}</Button><Button variant="outline" disabled={busy} onClick={()=>setConfirm(null)}>{ui("cancel")}</Button></div></div>}

  <section aria-labelledby="survey-results-title" className="mt-2"><h3 id="survey-results-title" className="mb-3 flex items-center gap-2 text-lg font-bold"><BarChart3 aria-hidden="true" className="size-5 text-primary"/>{t(s.status==="DRAFT"?"previewTitle":"resultsTitle")}</h3>
   {!detail&&!error?<Skeleton className="h-40 rounded-xl"/>:s.status==="DRAFT"?(detail&&<SurveyPreview questions={detail.questions.map(q=>({text:q.text,type:q.type,isRequired:q.isRequired,options:q.options.map(o=>o.text)}))} isAnonymous={s.isAnonymous}/>)
   :!results?<Skeleton className="h-40 rounded-xl"/>:results.hidden?<MessageBanner kind="info" presentation="inline" title={t("hiddenTitle")}>{t("hiddenHelp",{count:results.minAnonymousResults})}</MessageBanner>
   :<ol className="survey-results">{results.questions.map(q=><li key={q.id}><QuestionResultView q={q} total={s.responseCount} pct={pct} anonymous={s.isAnonymous}/></li>)}</ol>}
  </section>
  <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button></DialogFooter>
 </ManagementPanel></Dialog>;
}

function QuestionResultView({q,total,pct,anonymous}:{q:QuestionResult;total:number;pct:(n:number)=>string;anonymous:boolean}){
 const t=useTranslations("surveys"),locale=useLocale() as Locale;
 const max=Math.max(1,...q.options.map(o=>o.count),...q.ratingCounts);
 return <div className="survey-result">
  <div className="flex flex-wrap items-baseline justify-between gap-2"><p className="font-semibold"><span className="text-muted-foreground">{q.position}. </span>{q.text}</p><span className="text-xs text-muted-foreground">{t(`type_${q.type}`)} · {t("answeredBy",{count:q.answered,total})}</span></div>
  {isChoice(q.type)&&<ul className="survey-bars">{q.options.map(o=><li key={o.id} title={`${o.text}: ${o.count}`}><span className="survey-bar-label">{o.text}</span><span className="survey-bar-track"><span className="survey-bar-fill" style={{width:`${100*o.count/max}%`}}/></span><b>{o.count.toLocaleString(locale)} · {pct(share(o.count,q.answered))}</b></li>)}</ul>}
  {q.type==="RATING"&&<div className="survey-rating"><p className="survey-rating-average"><Star aria-hidden="true"/>{q.ratingAverage===null?"—":q.ratingAverage.toLocaleString(locale,{minimumFractionDigits:1,maximumFractionDigits:2})}<small>/ 5</small></p>
   <ul className="survey-bars">{q.ratingCounts.map((c,i)=><li key={i} title={`${t("ratingValue",{value:i+1})}: ${c}`}><span className="survey-bar-label">{t("ratingValue",{value:i+1})}</span><span className="survey-bar-track"><span className="survey-bar-fill" style={{width:`${100*c/max}%`}}/></span><b>{c.toLocaleString(locale)}</b></li>)}</ul></div>}
  {q.type==="TEXT"&&(q.texts.length===0?<p className="survey-empty">{t("noText")}</p>:<ul className="survey-texts">{q.texts.map((x,i)=><li key={i}><p className="whitespace-pre-wrap break-words">{x.text}</p><small>{anonymous?t("anonymousAnswer"):x.unitNumber?t("unitAnswer",{unit:x.unitNumber}):""} · {formatDateOnly(x.submittedOn,locale)}</small></li>)}</ul>)}
 </div>;
}
