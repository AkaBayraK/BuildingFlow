"use client";
import {useTranslations} from "next-intl";
import {EyeOff,Star} from "lucide-react";
import type {QuestionType} from "./model";

/** Read-only picture of what a resident will see; inputs are inert so the preview can never submit an answer. */
export function SurveyPreview({questions,isAnonymous}:{questions:{text:string;type:QuestionType;isRequired:boolean;options:string[]}[];isAnonymous:boolean}){
 const t=useTranslations("surveys");
 if(questions.length===0)return <p className="survey-empty">{t("previewEmpty")}</p>;
 return <div className="survey-preview" aria-label={t("previewLabel")} inert>
  {isAnonymous&&<p className="survey-flag"><EyeOff aria-hidden="true"/>{t("previewAnonymous")}</p>}
  <ol>{questions.map((q,i)=><li key={i}><p className="font-semibold">{q.text.trim()||t("questionPlaceholder")}{q.isRequired&&<span className="text-(--action-danger-text)"> *</span>}</p>
   {q.type==="RATING"?<div className="survey-preview-stars">{[1,2,3,4,5].map(n=><Star key={n} aria-hidden="true"/>)}</div>
   :q.type==="TEXT"?<div className="survey-preview-text">{t("previewText")}</div>
   :<ul className="survey-preview-options">{q.options.map((o,j)=><li key={j}><span className={q.type==="SINGLE_CHOICE"?"survey-preview-radio":"survey-preview-check"}/>{o.trim()||t("optionPlaceholder",{index:j+1})}</li>)}</ul>}
  </li>)}</ol>
 </div>;
}
