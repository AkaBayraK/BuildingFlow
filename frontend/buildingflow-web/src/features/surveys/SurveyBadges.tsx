import type {CSSProperties} from "react";
import {cn} from "@/lib/utils";
import {categoryStyle,stateIcon,stateTone} from "./category-style";
import type {SurveyCategory,SurveyState} from "./model";
const swatch=(category:SurveyCategory)=>({"--survey-cat":categoryStyle[category].color}) as CSSProperties;
/** Category pill: the coloured icon carries identity, the label stays in text ink. */
export function SurveyCategoryBadge({category,label}:{category:SurveyCategory;label:string}){const C=categoryStyle[category].icon;return <span className="survey-category" style={swatch(category)}><C aria-hidden="true"/>{label}</span>;}
/** Square category tile used instead of a photo in lists, so no stock imagery or licence is needed. */
export function SurveyCategoryTile({category,size="md"}:{category:SurveyCategory;size?:"md"|"lg"}){const C=categoryStyle[category].icon;return <span aria-hidden="true" className={cn("survey-tile",size==="lg"&&"survey-tile-lg")} style={swatch(category)}><C/></span>;}
export function SurveyStateBadge({state,label}:{state:SurveyState;label:string}){const I=stateIcon[state];return <span className={cn("dashboard-status survey-state",`tone-${stateTone[state]}`)}><I aria-hidden="true"/>{label}</span>;}
/** Participation meter; the number is always printed next to the bar. */
export function ParticipationBar({count,percent,percentText,label}:{count:string;percent:number;percentText:string;label:string}){
 return <div className="survey-meter" role="img" aria-label={label}><span className="survey-meter-count">{count}</span><span className="survey-meter-row"><span className="survey-meter-track"><span className={cn("survey-meter-fill",percent<34?"survey-meter-low":percent<67?"survey-meter-mid":"survey-meter-high")} style={{width:`${Math.min(100,percent)}%`}}/></span><b>{percentText}</b></span></div>;
}
