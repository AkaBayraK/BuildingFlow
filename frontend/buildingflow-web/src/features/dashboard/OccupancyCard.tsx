"use client";
import Link from "next/link";
import {useTranslations} from "next-intl";
import {ArrowRight} from "lucide-react";
import {occupancyRing} from "./occupancy";
import type {DashboardSummary} from "./model";
export function OccupancyCard({data,locale}:{data:DashboardSummary;locale:"tr"|"en"}){
 const t=useTranslations("dashboard"),ring=occupancyRing(data),radius=82,circumference=2*Math.PI*radius;
 const title=t(ring.apartmentOnly?"apartmentOccupancyTitle":"occupancyTitle"),label=t(ring.apartmentOnly?"occupiedApartmentLabel":"occupiedUnitLabel"),percentage=ring.percent===null?"—":new Intl.NumberFormat(locale==="en"?"en-GB":"tr-TR",{style:"percent",maximumFractionDigits:0}).format(ring.ratio);
 return <section className="dashboard-panel dashboard-occupancy-card" aria-labelledby="dash-occupancy"><h2 id="dash-occupancy">{title}</h2><div className="dashboard-occupancy-body">
  <svg className="dashboard-occupancy-ring" viewBox="0 0 220 220" role="img" aria-label={t("occupancyDescription",{occupied:ring.occupied,total:data.unitCount,percentage})}>
   <circle cx={110} cy={110} r={radius} fill="none" stroke="var(--occupancy-vacant)" strokeWidth={22}/>
   {ring.ratio>0&&<circle className="occupancy-filled-arc" cx={110} cy={110} r={radius} fill="none" stroke="var(--occupancy-filled)" strokeWidth={22} strokeLinecap="round" strokeDasharray={`${ring.ratio*circumference} ${circumference}`} transform="rotate(-90 110 110)"/>}
   <text x={110} y={111} textAnchor="middle" className="dashboard-occupancy-percent">{percentage}</text><text x={110} y={137} textAnchor="middle" className="dashboard-occupancy-caption">{data.unitCount>0?label:t("typesEmpty")}</text>
  </svg>
  <dl className="dashboard-occupancy-legend"><div><dt><i className="occupancy-dot-filled" aria-hidden="true"/>{t("occupied")}</dt><dd>{ring.occupied.toLocaleString(locale)}</dd></div><div><dt><i className="occupancy-dot-vacant" aria-hidden="true"/>{t("vacant")}</dt><dd>{ring.vacant.toLocaleString(locale)}</dd></div><div title={t("pendingUnavailable")}><dt><i className="occupancy-dot-pending" aria-hidden="true"/>{t("occupancyPending")}</dt><dd>—<span className="sr-only">{t("pendingUnavailable")}</span></dd></div></dl>
 </div><Link className="dashboard-occupancy-link" href={`/properties/${data.propertyId}/units`} aria-label={t("openUnits")}>{t(ring.apartmentOnly?"apartmentList":"unitList")}<ArrowRight size={18} aria-hidden="true"/></Link></section>;
}
