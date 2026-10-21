"use client";
import {useEffect,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {ArrowRight,ArrowUpRight,Building2,CalendarClock,CircleAlert,DoorClosed,DoorOpen,RefreshCw,Wallet,Wrench,Hammer,type LucideIcon} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useDashboardArea} from "./DashboardArea";
import {DashboardHero} from "./DashboardHero";
import {TrendChart} from "./TrendChart";
import {CollectionCard} from "./CollectionCard";
import {OccupancyCard} from "./OccupancyCard";
import {UpcomingDates} from "./UpcomingDates";
import {DashboardWidgets} from "./DashboardWidgets";
import {useIdentity} from "@/auth/AuthGate";
import {useUpcomingPlans} from "../maintenance/useUpcomingPlans";
import {formatDecimalCurrency} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {cn} from "@/lib/utils";
import {readDashboard,share,plot,type DashboardSummary,type RecentRequest} from "./model";
import {categoryStyle,statusTone} from "../requests/category-style";

type Locale="tr"|"en";

export function DashboardPage(){const area=useDashboardArea();return <DashboardContent key={area.propertyId}/>;}
function DashboardContent(){
 const t=useTranslations("dashboard"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi();
 const {allowed,properties,propertyId,listError,reload}=useDashboardArea(),identity=useIdentity();
 const [data,setData]=useState<DashboardSummary|null>(null),[loading,setLoading]=useState(false),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 useEffect(()=>{if(!propertyId)return;const c=new AbortController();setLoading(true);setError("");setData(null);
  void api<unknown>(`/api/v1/properties/${propertyId}/dashboard`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readDashboard(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();},[api,propertyId,revision,locale]);
 const plans=useUpcomingPlans(propertyId||null,revision);
 const property=properties?.find(p=>p.id===propertyId);const base=`/properties/${propertyId}`;

 const money=(v:string)=>formatDecimalCurrency(v,locale);
 return <div className="dashboard py-8">
  <DashboardHero property={property??null} data={data} loading={loading} onRefresh={()=>setRevision(n=>n+1)}/>
  {!allowed?<MessageBanner>{t("roleRequired")}</MessageBanner>
  :listError?<MessageBanner action={<Button variant="view" size="sm" onClick={reload}><RefreshCw/>{ui("tryAgain")}</Button>}>{listError}</MessageBanner>
  :properties&&properties.length===0?<div className="rounded-2xl border bg-card p-6 text-sm"><p className="font-semibold">{t("noAreaTitle")}</p><p className="mt-1 text-muted-foreground">{t("noAreaHint")}</p><Button asChild className="mt-4"><Link href="/properties"><Building2/>{t("goAreas")}</Link></Button></div>
  :error?<MessageBanner action={<Button variant="view" size="sm" onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{error}</MessageBanner>
  :!data?<div className="dashboard-kpis" aria-busy="true" aria-label={t("loading")}>{Array.from({length:8},(_,i)=><Skeleton key={i} className="h-28 rounded-2xl"/>)}</div>
  :<DashboardWidgets subject={identity.subject??"current"} widgets={[
   {id:"kpis",title:t("widget_kpis"),node:<div className="dashboard-kpis">
    <Kpi icon={Building2} tone="blue" value={data.unitCount.toLocaleString(locale)} label={t("units")} href={`${base}/units`} linkLabel={t("openUnits")}/>
    <Kpi icon={DoorOpen} tone="green" value={data.occupiedUnitCount.toLocaleString(locale)} label={t("occupied")} badge={`%${share(data.occupiedUnitCount,data.unitCount)}`}/>
    <Kpi icon={DoorClosed} tone="amber" value={data.vacantUnitCount.toLocaleString(locale)} label={t("vacant")} badge={`%${share(data.vacantUnitCount,data.unitCount)}`}/>
    <Kpi icon={Wallet} tone="teal" value={money(data.receiptsThisMonth)} label={t("receipts")} note={t("receiptsNote")} href={`${base}/payments`} linkLabel={t("openPayments")} extra={<Spark values={data.trend.map(m=>plot(m.receipts))} label={t("receiptsTrend")}/>}/>
    <Kpi icon={CircleAlert} tone="red" value={money(data.overdueAmount)} label={t("overdue")} note={t("chargeCount",{count:data.overdueChargeCount})} href={`${base}/dues`} linkLabel={t("openDues")}/>
    {data.requests&&<Kpi icon={Wrench} tone="violet" value={(data.requests.recorded+data.requests.inProgress).toLocaleString(locale)} label={t("openRequests")} note={t("requestSplit",{recorded:data.requests.recorded,inProgress:data.requests.inProgress})} href={`${base}/requests`} linkLabel={t("openRequestsLink")}/>}
    <Kpi icon={CalendarClock} tone="blue" value={money(data.upcomingAmount)} label={t("upcoming")} note={t("upcomingNote",{count:data.upcomingChargeCount})} href={`${base}/dues`} linkLabel={t("openDues")}/>
    <Kpi icon={Hammer} tone="amber" value={plans.data?plans.data.nextDays.toLocaleString(locale):"—"} label={t("maintenance")} note={plans.data?t("maintenanceNote",{count:plans.data.nextDays}):plans.error?t("upcomingPlansUnavailable"):t("loading")} href={`${base}/maintenance`} linkLabel={t("openMaintenance")} pending={!plans.data}/>
   </div>},
   {id:"trend",title:t("trendTitle"),node:<TrendChart data={data} locale={locale}/>},
   {id:"occupancy",title:t("widget_occupancy"),node:<OccupancyCard data={data} locale={locale}/>},
   ...(data.recentRequests?[{id:"recent" as const,title:t("recentTitle"),node:<RecentRequests items={data.recentRequests} href={`${base}/requests`} locale={locale}/>}]:[]),
   {id:"collection",title:t("collectionTitle"),node:<CollectionCard data={data} locale={locale}/>},
   {id:"upcoming",title:t("upcomingDatesTitle"),node:<UpcomingDates propertyId={propertyId} today={data.today} dues={data.upcomingDues} plans={plans.data} plansError={plans.error} locale={locale}/>},
  ]}/>}
 </div>;
}
function Kpi({icon:Icon,tone,value,label,note,badge,href,linkLabel,extra,pending}:{icon:LucideIcon;tone:string;value:string;label:string;note?:string;badge?:string;href?:string;linkLabel?:string;extra?:React.ReactNode;pending?:boolean}){
 return <div className={cn("dashboard-kpi",pending&&"dashboard-kpi-pending")}>
  <span className={`dashboard-icon tone-${tone}`}><Icon aria-hidden="true"/></span>
  <div className="min-w-0"><p className="dashboard-kpi-value">{value}</p><p className="dashboard-kpi-label">{label}{badge&&<span className="dashboard-badge">{badge}</span>}</p>{note&&<p className="dashboard-kpi-note">{note}</p>}{extra}</div>
  {href&&<Link href={href} className="dashboard-kpi-link" aria-label={linkLabel} title={linkLabel}><ArrowUpRight aria-hidden="true"/></Link>}
 </div>;
}
function RecentRequests({items,href,locale}:{items:RecentRequest[];href:string;locale:Locale}){
 const t=useTranslations("dashboard"),r=useTranslations("requests");
 return <section className="dashboard-panel" aria-labelledby="dash-recent"><div className="dashboard-recent-head"><h2 id="dash-recent">{t("recentTitle")}</h2><Link href={href} className="dashboard-recent-all">{t("recentAll")}<ArrowRight aria-hidden="true"/></Link></div>
  {items.length===0?<p className="mt-3 text-sm text-muted-foreground">{t("recentEmpty")}</p>:<ul className="dashboard-recent">{items.map(i=>{const c=categoryStyle[i.category];return <li key={i.id}>
   <span className={cn("dashboard-icon",`tone-${c.tone}`)} title={r(`category_${i.category}`)}><c.icon aria-hidden="true"/><span className="sr-only">{r(`category_${i.category}`)}</span></span>
   <span className="min-w-0 flex-1"><strong className="block truncate">{i.title}</strong><small>{i.unitNumber?[i.buildingName,t("recentUnit",{number:i.unitNumber})].filter(Boolean).join(" - "):t("recentCommonArea")}</small></span>
   <time dateTime={i.createdAt} title={new Date(i.createdAt).toLocaleString(locale)}>{ago(i.createdAt,locale)}</time>
   <span className={cn("dashboard-status",`tone-${statusTone[i.status]}`)}>{t(`recentStatus_${i.status}`)}</span></li>;})}</ul>}
 </section>;
}
/** "2 saat önce" style relative time; the exact time stays in the title. */
function ago(at:string,locale:Locale){
 const s=(Date.parse(at)-Date.now())/1000,f=new Intl.RelativeTimeFormat(locale,{numeric:"auto"});
 for(const [unit,size] of [["year",31536000],["month",2592000],["day",86400],["hour",3600],["minute",60]] as const)if(Math.abs(s)>=size)return f.format(Math.round(s/size),unit);
 return f.format(0,"minute");
}
function Spark({values,label}:{values:number[];label:string}){
 const max=Math.max(...values,0);if(max<=0)return null;const w=96,h=24;
 return <svg className="dashboard-spark" viewBox={`0 0 ${w} ${h}`} role="img" aria-label={label}>{values.map((v,i)=>{const bh=Math.max(2,v/max*h);return <rect key={i} x={i*(w/values.length)+2} y={h-bh} width={w/values.length-4} height={bh} rx={2} fill="var(--chart-2)" opacity={i===values.length-1?1:.45}/>;})}</svg>;
}
