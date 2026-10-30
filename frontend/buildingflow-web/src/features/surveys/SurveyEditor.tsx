"use client";
import {useRef,useState,type CSSProperties} from "react";
import {useLocale,useTranslations} from "next-intl";
import {ArrowDown,ArrowUp,CircleDot,ClipboardList,Eye,EyeOff,LayoutTemplate,ListChecks,LoaderCircle,Plus,RefreshCw,Save,Send,ShieldCheck,SquareCheck,Star,Trash2,Type,X,type LucideIcon} from "lucide-react";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Textarea} from "@/components/ui/textarea";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {businessToday} from "@/lib/format";
import {cn} from "@/lib/utils";
import {categoryStyle} from "./category-style";
import {SurveyPreview} from "./SurveyPreview";
import {useDefinitionOptions} from "@/features/definitions/useDefinitionOptions";
import {addDays,formFrom,isChoice,limits,move,questionKey,questionTypes,readDetail,responseModes,retype,surveyAudiences,surveyBody,surveyCategories,surveyIssues,surveyTemplates,templateForm,
 type FormIssue,type QuestionForm,type QuestionType,type SurveyDetail,type SurveyForm,type SurveyTemplate} from "./model";

type Locale="tr"|"en";
const typeIcon:Record<QuestionType,LucideIcon>={SINGLE_CHOICE:CircleDot,MULTIPLE_CHOICE:SquareCheck,RATING:Star,TEXT:Type};
const blankQuestion=(type:QuestionType="SINGLE_CHOICE"):QuestionForm=>({key:questionKey(),text:"",type,isRequired:true,options:isChoice(type)?["",""]:[]});

export function SurveyEditor({propertyId,detail,startWithTemplates,preset,canWrite,onClose,onSaved}:{propertyId:string;detail?:SurveyDetail;startWithTemplates:boolean;preset?:SurveyTemplate["id"];canWrite:boolean;onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("surveys"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),notice=useNotification();
 const today=businessToday();
 const label=(key:string)=>t(key as Parameters<typeof t>[0]);
 const blank=():SurveyForm=>({title:"",description:"",category:"GENERAL",startDate:today,endDate:addDays(today,14),audience:"ALL_RESIDENTS",responseMode:"PER_UNIT",isAnonymous:false,questions:[blankQuestion()]});
 const initial=():SurveyForm=>{const tpl=surveyTemplates.find(x=>x.id===preset);return detail?formFrom(detail):tpl?templateForm(tpl,today,label):blank();};
 const [form,setForm]=useState<SurveyForm>(initial),[record,setRecord]=useState(detail),[picking,setPicking]=useState(startWithTemplates&&!detail);
 const categoryChoices=useDefinitionOptions(propertyId,"SURVEY_CATEGORY",surveyCategories,record?.survey.category);
 const [tab,setTab]=useState<"questions"|"preview">("questions"),[busy,setBusy]=useState(false),[shown,setShown]=useState<FormIssue[]>([]),[stale,setStale]=useState(false);
 const lock=useRef(false),root=`/api/v1/properties/${propertyId}/surveys`;
 const set=<K extends keyof SurveyForm>(key:K,value:SurveyForm[K])=>{setForm(f=>({...f,[key]:value}));setShown([]);};
 const setQuestion=(i:number,patch:Partial<QuestionForm>)=>{setForm(f=>({...f,questions:f.questions.map((q,j)=>j===i?{...q,...patch}:q)}));setShown([]);};
 const has=(issue:FormIssue)=>shown.includes(issue);
 async function submit(publish:boolean){
  if(lock.current||!canWrite)return;const issues=surveyIssues(form,publish);if(issues.length){setShown(issues);if(issues.some(i=>i.startsWith("question")))setTab("questions");return;}
  lock.current=true;setBusy(true);setStale(false);
  try{
   let saved=readDetail(await api<unknown>(record?`${root}/${record.survey.id}`:root,{method:record?"PUT":"POST",body:JSON.stringify(surveyBody(form,record?.survey.version??0))}),propertyId);
   setRecord(saved);onSaved();
   if(publish){saved=readDetail(await api<unknown>(`${root}/${saved.survey.id}/publish`,{method:"PUT",body:JSON.stringify({version:saved.survey.version})}),propertyId);setRecord(saved);onSaved();notice.success(t("published"));onClose();return;}
   notice.success(t(record?"updated":"created"));onClose();
  }catch(e){if(e instanceof ApiError&&e.status===409)setStale(true);if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));}
  finally{lock.current=false;setBusy(false);}
 }
 async function reload(){if(!record)return;try{const current=readDetail(await api<unknown>(`${root}/${record.survey.id}`),propertyId);setRecord(current);setForm(formFrom(current));setStale(false);}catch(e){if(!(e instanceof ApiError))notice.error(errorMessage(e,locale));}}
 const choose=(tpl:SurveyTemplate|null)=>{setForm(tpl?templateForm(tpl,today,label):blank());setPicking(false);setShown([]);};
 const draftLocked=!!record&&record.survey.status!=="DRAFT";

 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy} className="survey-editor sm:max-w-[min(1180px,calc(100vw-2rem))]">
  <DialogHeader><ClipboardList className="size-9 text-primary" aria-hidden="true"/><DialogTitle>{t(record?"editTitle":"newTitle")}</DialogTitle><DialogDescription>{t("editorHelp")}</DialogDescription></DialogHeader>
  {picking?<TemplatePicker onChoose={choose}/>:<form noValidate onSubmit={e=>{e.preventDefault();void submit(false);}} className="space-y-5">
   {stale&&<MessageBanner kind="warning" presentation="inline" action={<Button type="button" variant="view" size="sm" onClick={()=>void reload()}><RefreshCw/>{t("loadCurrent")}</Button>}>{t("staleHelp")}</MessageBanner>}
   {draftLocked&&<MessageBanner kind="warning" presentation="inline">{t("lockedHelp")}</MessageBanner>}
   {!record&&<Button type="button" variant="view" size="sm" onClick={()=>setPicking(true)}><LayoutTemplate/>{t("chooseTemplate")}</Button>}
   <fieldset disabled={busy||!canWrite||draftLocked} className="survey-editor-grid">
    <div className="survey-editor-col space-y-5">
     <h3 className="survey-editor-heading">{t("settingsTitle")}</h3>
     <div><Label htmlFor="survey-title">{t("field_title")} *</Label><Input id="survey-title" maxLength={limits.title} value={form.title} placeholder={t("titleHint")} aria-invalid={has("title")} aria-describedby={has("title")?"survey-title-error":undefined} onChange={e=>set("title",e.target.value)}/>{has("title")&&<FieldError id="survey-title-error">{t("invalidTitle")}</FieldError>}</div>
     <div><Label htmlFor="survey-description">{t("field_description")}</Label><Textarea id="survey-description" className="min-h-24" maxLength={limits.description} value={form.description} placeholder={t("descriptionHint")} aria-invalid={has("description")} onChange={e=>set("description",e.target.value)}/><p className="mt-1 text-right text-xs text-muted-foreground">{form.description.length}/{limits.description}</p></div>
     <div role="radiogroup" aria-labelledby="survey-category-label"><p id="survey-category-label" className="mb-2 text-sm font-medium">{t("col_category")} *</p><div className="survey-choice-grid">{categoryChoices.options.map(c=>{const S=categoryChoices.style(c,{...categoryStyle[c],tone:""});return <button key={c} type="button" role="radio" aria-checked={form.category===c} onClick={()=>set("category",c)} className={cn("survey-choice",form.category===c&&"survey-choice-active")} style={{"--survey-cat":S.color} as CSSProperties}><span className="survey-tile survey-tile-sm"><S.icon aria-hidden="true"/></span>{categoryChoices.label(c,t(`category_${c}`))}</button>;})}</div></div>
     <div className="grid gap-4 sm:grid-cols-2"><div><Label htmlFor="survey-start">{t("startDate")} *</Label><Input id="survey-start" type="date" min="2000-01-01" max="2100-12-31" value={form.startDate} aria-invalid={has("dates")} onChange={e=>set("startDate",e.target.value)}/></div>
      <div><Label htmlFor="survey-end">{t("endDate")} *</Label><Input id="survey-end" type="date" min={form.startDate||"2000-01-01"} max="2100-12-31" value={form.endDate} aria-invalid={has("dates")} onChange={e=>set("endDate",e.target.value)}/></div></div>
     {has("dates")&&<FieldError>{t("invalidDates")}</FieldError>}
     <div role="radiogroup" aria-labelledby="survey-audience-label"><p id="survey-audience-label" className="mb-2 text-sm font-medium">{t("audience")} *</p><div className="survey-segment">{surveyAudiences.map(a=><button key={a} type="button" role="radio" aria-checked={form.audience===a} className={cn(form.audience===a&&"survey-segment-active")} onClick={()=>set("audience",a)}>{t(`audience_${a}`)}</button>)}</div></div>
     <div role="radiogroup" aria-labelledby="survey-mode-label"><p id="survey-mode-label" className="mb-2 text-sm font-medium">{t("responseMode")} *</p><div className="grid gap-2 sm:grid-cols-2">{responseModes.map(m=><button key={m} type="button" role="radio" aria-checked={form.responseMode===m} className={cn("survey-option-card",form.responseMode===m&&"survey-option-card-active")} onClick={()=>set("responseMode",m)}><strong>{t(`mode_${m}`)}</strong><small>{t(`modeHelp_${m}`)}</small></button>)}</div></div>
     <div className={cn("survey-privacy",form.isAnonymous&&"survey-privacy-on")}>
      <label className="flex cursor-pointer items-start gap-3"><input type="checkbox" className="survey-switch" checked={form.isAnonymous} onChange={e=>set("isAnonymous",e.target.checked)}/><span><strong className="flex items-center gap-2">{form.isAnonymous?<EyeOff aria-hidden="true" className="size-4"/>:<Eye aria-hidden="true" className="size-4"/>}{t("anonymousToggle")}</strong><small>{t(form.isAnonymous?"anonymousOn":"anonymousOff")}</small></span></label>
      <p className="survey-kvkk"><ShieldCheck aria-hidden="true"/>{t("kvkkNote")}</p>
     </div>
    </div>
    <div className="survey-editor-col">
     <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><h3 className="survey-editor-heading">{t("questionsTitle",{count:form.questions.length,max:limits.questions})}</h3>
      <div role="tablist" className="survey-segment survey-segment-sm"><button type="button" role="tab" aria-selected={tab==="questions"} className={cn(tab==="questions"&&"survey-segment-active")} onClick={()=>setTab("questions")}><ListChecks aria-hidden="true"/>{t("tabQuestions")}</button><button type="button" role="tab" aria-selected={tab==="preview"} className={cn(tab==="preview"&&"survey-segment-active")} onClick={()=>setTab("preview")}><Eye aria-hidden="true"/>{t("tabPreview")}</button></div></div>
     {has("questions")&&<FieldError>{t("invalidQuestions",{max:limits.questions})}</FieldError>}
     {tab==="preview"?<SurveyPreview questions={form.questions} isAnonymous={form.isAnonymous}/>:<>
      <ol className="survey-questions">{form.questions.map((q,i)=><li key={q.key} className={cn("survey-question",has(`question:${i}`)&&"survey-question-invalid")}>
       <div className="flex items-center gap-2"><span className="survey-question-number">{i+1}</span>
        <select aria-label={t("questionType",{index:i+1})} className="survey-type-select" value={q.type} onChange={e=>setQuestion(i,retype(q,e.target.value as QuestionType))}>{questionTypes.map(type=><option key={type} value={type}>{t(`type_${type}`)}</option>)}</select>
        <div className="ml-auto flex gap-1"><Button type="button" variant="neutral" size="icon-sm" aria-label={t("moveUp",{index:i+1})} disabled={i===0} onClick={()=>set("questions",move(form.questions,i,i-1))}><ArrowUp/></Button><Button type="button" variant="neutral" size="icon-sm" aria-label={t("moveDown",{index:i+1})} disabled={i===form.questions.length-1} onClick={()=>set("questions",move(form.questions,i,i+1))}><ArrowDown/></Button><Button type="button" variant="danger" size="icon-sm" aria-label={t("removeQuestion",{index:i+1})} onClick={()=>set("questions",form.questions.filter((_,j)=>j!==i))}><Trash2/></Button></div></div>
       <Label htmlFor={`q-${q.key}`} className="sr-only">{t("questionText",{index:i+1})}</Label><Input id={`q-${q.key}`} className="mt-3" maxLength={limits.question} placeholder={t("questionPlaceholder")} value={q.text} onChange={e=>setQuestion(i,{text:e.target.value})}/>
       {isChoice(q.type)&&<ul className="survey-option-list">{q.options.map((o,j)=><li key={j}><span className={q.type==="SINGLE_CHOICE"?"survey-preview-radio":"survey-preview-check"} aria-hidden="true"/><Input aria-label={t("optionLabel",{question:i+1,index:j+1})} maxLength={limits.option} placeholder={t("optionPlaceholder",{index:j+1})} value={o} onChange={e=>setQuestion(i,{options:q.options.map((x,k)=>k===j?e.target.value:x)})}/>
         <Button type="button" variant="neutral" size="icon-sm" aria-label={t("removeOption",{index:j+1})} disabled={q.options.length<=limits.minOptions} onClick={()=>setQuestion(i,{options:q.options.filter((_,k)=>k!==j)})}><X/></Button></li>)}
        {q.options.length<limits.maxOptions&&<li><Button type="button" variant="view" size="sm" onClick={()=>setQuestion(i,{options:[...q.options,""]})}><Plus/>{t("addOption")}</Button></li>}</ul>}
       {q.type==="RATING"&&<p className="survey-question-hint"><Star aria-hidden="true"/>{t("ratingHint")}</p>}
       {q.type==="TEXT"&&<p className="survey-question-hint"><Type aria-hidden="true"/>{t("textHint")}</p>}
       <label className="mt-3 flex items-center gap-2 text-sm"><input type="checkbox" checked={q.isRequired} onChange={e=>setQuestion(i,{isRequired:e.target.checked})}/>{t("required")}</label>
       {has(`question:${i}`)&&<FieldError>{t("invalidQuestion")}</FieldError>}
      </li>)}</ol>
      {form.questions.length<limits.questions&&<div className="survey-add"><p className="text-sm font-medium">{t("addQuestion")}</p><div className="flex flex-wrap gap-2">{questionTypes.map(type=>{const I=typeIcon[type];return <Button key={type} type="button" variant="view" size="sm" onClick={()=>set("questions",[...form.questions,blankQuestion(type)])}><I/>{t(`type_${type}`)}</Button>;})}</div></div>}
     </>}
    </div>
   </fieldset>
   <DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button>
    <Button type="submit" variant="view" disabled={busy||!canWrite||draftLocked||stale}>{busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("saveDraft")}</Button>
    <Button type="button" disabled={busy||!canWrite||draftLocked||stale||form.endDate<today} onClick={()=>void submit(true)}>{busy?<LoaderCircle className="animate-spin"/>:<Send/>}{t("saveAndPublish")}</Button></DialogFooter>
  </form>}
 </ManagementPanel></Dialog>;
}

function TemplatePicker({onChoose}:{onChoose:(tpl:SurveyTemplate|null)=>void}){
 const t=useTranslations("surveys");
 return <div className="space-y-4"><p className="text-sm text-muted-foreground">{t("templateHelp")}</p><div className="survey-templates">
  {surveyTemplates.map(tpl=>{const S=categoryStyle[tpl.category];return <button key={tpl.id} type="button" className="survey-template" style={{"--survey-cat":S.color} as CSSProperties} onClick={()=>onChoose(tpl)}><span className="survey-tile"><S.icon aria-hidden="true"/></span><strong>{t(`template_${tpl.id}_title`)}</strong><small>{t(`template_${tpl.id}_description`)}</small><span className="survey-template-meta">{t("templateMeta",{count:tpl.questions.length,days:tpl.days})}{tpl.anonymous?` · ${t("anonymous")}`:""}</span></button>;})}
  <button type="button" className="survey-template survey-template-blank" onClick={()=>onChoose(null)}><span className="survey-tile"><Plus aria-hidden="true"/></span><strong>{t("blankTitle")}</strong><small>{t("blankHelp")}</small></button>
 </div></div>;
}
