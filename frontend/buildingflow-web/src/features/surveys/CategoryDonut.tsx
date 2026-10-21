"use client";
import {useState} from "react";
import {categoryStyle} from "./category-style";
import type {SurveyCategory,SurveyCount} from "./model";

// Category distribution: thin ring, 2px surface gaps between segments, legend with icon, label and count so identity never relies on colour.
const R=74,STROKE=22,C=2*Math.PI*R,GAP=2;
export function CategoryDonut({counts,total,label,centerLabel,description,empty,locale}:{counts:readonly SurveyCount<SurveyCategory>[];total:number;label:(c:SurveyCategory)=>string;centerLabel:string;description:string;empty:string;locale:"tr"|"en"}){
 const [hover,setHover]=useState<SurveyCategory|null>(null);
 const used=counts.filter(c=>c.count>0);let offset=0;
 const segments=used.map(c=>{const length=total>0?c.count/total*C:0;const s={...c,length,offset};offset+=length;return s;});
 const active=hover?counts.find(c=>c.key===hover):null;
 return <div className="survey-donut">
  <svg viewBox="0 0 200 200" role="img" aria-label={description}>
   <circle cx={100} cy={100} r={R} fill="none" stroke="var(--muted)" strokeWidth={STROKE}/>
   {segments.map(s=><circle key={s.key} cx={100} cy={100} r={R} fill="none" stroke={categoryStyle[s.key].color} strokeWidth={hover===s.key?STROKE+4:STROKE}
    strokeDasharray={`${Math.max(0,s.length-(used.length>1?GAP:0))} ${C}`} strokeDashoffset={-s.offset} transform="rotate(-90 100 100)" opacity={hover&&hover!==s.key?.45:1}
    onMouseEnter={()=>setHover(s.key)} onMouseLeave={()=>setHover(null)}><title>{`${label(s.key)}: ${s.count.toLocaleString(locale)}`}</title></circle>)}
   <text x={100} y={100} textAnchor="middle" className="survey-donut-total">{(active?active.count:total).toLocaleString(locale)}</text>
   <text x={100} y={124} textAnchor="middle" className="survey-donut-caption">{active?label(active.key):total>0?centerLabel:empty}</text>
  </svg>
  <ul className="survey-legend">{counts.map(c=>{const Icon=categoryStyle[c.key].icon;return <li key={c.key} className={c.count===0?"survey-legend-zero":undefined} onMouseEnter={()=>c.count&&setHover(c.key)} onMouseLeave={()=>setHover(null)}>
   <i style={{background:categoryStyle[c.key].color}} aria-hidden="true"/><Icon aria-hidden="true" className="survey-legend-icon" style={{color:categoryStyle[c.key].color}}/><span>{label(c.key)}</span><b>{c.count.toLocaleString(locale)}</b></li>;})}</ul>
 </div>;
}
