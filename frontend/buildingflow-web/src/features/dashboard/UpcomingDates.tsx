"use client";
import Link from "next/link";
import {useTranslations} from "next-intl";
import {ArrowRight,CalendarDays} from "lucide-react";
import {cn} from "@/lib/utils";
import {formatDateOnly,formatDecimalCurrency} from "@/lib/format";
import {typeStyle,statusTone} from "../maintenance/type-style";
import type {MaintenanceUpcoming} from "../maintenance/model";
import {upcomingDates,type UpcomingDue} from "./upcoming-dates";

type Locale="tr"|"en";
/** Dashboard card fed by dues deadlines and Planlama & Bakımlar; nothing is invented when plans fail to load. */
export function UpcomingDates({propertyId,today,dues,plans,plansError,locale}:{propertyId:string;today:string;dues:readonly UpcomingDue[];plans:MaintenanceUpcoming|null;plansError:string;locale:Locale}){
 const t=useTranslations("dashboard"),m=useTranslations("maintenance");const base=`/properties/${propertyId}`;
 const rows=upcomingDates(dues,plans?.items??[],today);
 const left=(days:number)=>days===0?t("upcomingToday"):t("upcomingDaysLeft",{count:days});
 return <section className="dashboard-panel dashboard-upcoming" aria-labelledby="dash-next">
  <div className="dashboard-recent-head"><h2 id="dash-next">{t("upcomingDatesTitle")}</h2><Link href={`${base}/maintenance`} className="dashboard-recent-all">{t("upcomingAll")}<ArrowRight aria-hidden="true"/></Link></div>
  {rows.length>0&&<ul className="dashboard-upcoming-list">{rows.map(r=>{
   if(r.kind==="dues")return <li key={r.key}><Link href={`${base}/dues`} className="dashboard-upcoming-item">
    <span className="dashboard-icon tone-red"><CalendarDays aria-hidden="true"/></span>
    <span className="min-w-0 flex-1"><strong>{t("dueItem")}</strong><small><time dateTime={r.date}>{formatDateOnly(r.date,locale)}</time> · {t("chargeCount",{count:r.due.chargeCount})} · {formatDecimalCurrency(r.due.amount,locale)}</small></span>
    <span className={cn("dashboard-status",r.days<=7?"tone-red":"tone-amber")}>{left(r.days)}</span></Link></li>;
   const c=typeStyle[r.plan.type];
   return <li key={r.key}><Link href={`${base}/maintenance`} className="dashboard-upcoming-item">
    <span className={cn("dashboard-icon",`tone-${c.tone}`)} title={m(`type_${r.plan.type}`)}><c.icon aria-hidden="true"/><span className="sr-only">{m(`type_${r.plan.type}`)}</span></span>
    <span className="min-w-0 flex-1"><strong className="block truncate">{r.plan.title}</strong><small><time dateTime={r.date}>{formatDateOnly(r.date,locale)}</time>{r.days<=7&&<> · {left(r.days)}</>}</small></span>
    <span className={cn("dashboard-status",`tone-${statusTone[r.plan.status]}`)}>{m(`status_${r.plan.status}`)}</span></Link></li>;
  })}</ul>}
  {plansError&&<p className="mt-3 text-xs text-muted-foreground" role="status">{t("upcomingPlansUnavailable")}</p>}
  {rows.length===0&&!plansError&&<p className="mt-3 text-sm text-muted-foreground">{plans?t("upcomingEmpty"):t("loading")}</p>}
 </section>;
}
