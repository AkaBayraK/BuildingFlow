"use client";
import {useState} from "react";
import {useTranslations} from "next-intl";
import {formatDecimalCurrency} from "@/lib/format";
import {collectionRatio} from "./collection";
import type {DashboardSummary} from "./model";
export function CollectionCard({data,locale}:{data:DashboardSummary;locale:"tr"|"en"}){
 const t=useTranslations("dashboard"),[index,setIndex]=useState(1),row=data.collections?.[index],radius=82,circumference=2*Math.PI*radius;
 const result=row?collectionRatio(row):null,percentage=result?.percent==null?"—":new Intl.NumberFormat(locale==="en"?"en-GB":"tr-TR",{style:"percent",maximumFractionDigits:0}).format(result.percent/100);
 const amounts=[{key:"collectedAmount" as const,label:"collectionCollected" as const,color:"var(--collection-paid)"},{key:"pendingAmount" as const,label:"collectionPending" as const,color:"var(--collection-pending)"},{key:"overdueAmount" as const,label:"collectionOverdue" as const,color:"var(--collection-overdue)"}];
 return <section className="dashboard-panel dashboard-collection-card" aria-labelledby="dash-collection"><div className="dashboard-collection-heading"><h2 id="dash-collection">{t("collectionTitle")}</h2><label><span className="sr-only">{t("collectionRange")}</span><select aria-label={t("collectionRange")} value={index} disabled={!data.collections} onChange={e=>setIndex(Number(e.target.value))}><option value={1}>{t("collectionThisMonth")}</option><option value={0}>{t("collectionPreviousMonth")}</option></select></label></div>
 <svg className="dashboard-collection-ring" viewBox="0 0 220 220" role="img" aria-label={t("collectionRingLabel",{percentage})}>
  <circle cx={110} cy={110} r={radius} fill="none" stroke="var(--collection-pending)" strokeWidth={22}/>
  {!!result&&result.ratio>0&&<circle cx={110} cy={110} r={radius} fill="none" stroke="var(--collection-paid)" strokeWidth={22} strokeLinecap="round" strokeDasharray={`${result.ratio*circumference} ${circumference}`} transform="rotate(-90 110 110)"/>}
  <text x={110} y={111} textAnchor="middle" className="dashboard-occupancy-percent">{percentage}</text><text x={110} y={137} textAnchor="middle" className="dashboard-occupancy-caption">{t("collectionRate")}</text>
 </svg><dl className="dashboard-collection-legend">{amounts.map(item=><div key={item.key}><dt><i style={{background:item.color}} aria-hidden="true"/>{t(item.label)}</dt><dd className={item.key==="overdueAmount"?"collection-overdue":""}>{row?formatDecimalCurrency(row[item.key],locale):"—"}</dd></div>)}</dl>
 <p className="dashboard-collection-note">{row?t("collectionBasis"):t("collectionUnavailable")}</p></section>;
}
