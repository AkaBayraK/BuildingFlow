"use client";
import {useEffect,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {ArrowUpRight,Building2,CalendarClock,CircleAlert,DoorClosed,DoorOpen,RefreshCw,Wallet,Wrench,Hammer,ReceiptText,type LucideIcon} from "lucide-react";
import {useAuthorizedApi,errorMessage,unitTypeNames} from "@/services/api";
import {useDashboardArea} from "./DashboardArea";
import {DashboardHero} from "./DashboardHero";
import {TrendChart} from "./TrendChart";
import {formatDateOnly,formatDecimalCurrency} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {cn} from "@/lib/utils";
import {readDashboard,share,plot,UNIT_TYPES,type DashboardSummary} from "./model";

type Locale="tr"|"en";

// Categorical order validated for colour-vision deficiency (dataviz validator, light surface); colour follows the unit type, never its rank.
const TYPE_COLORS=["var(--chart-1)","var(--chart-2)","var(--chart-3)","var(--chart-4)","var(--chart-5)","var(--chart-6)"];
export function DashboardPage(){const area=useDashboardArea();return <DashboardContent key={area.propertyId}/>;}
function DashboardContent(){
 const t=useTranslations("dashboard"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi();
 const {allowed,properties,propertyId,listError,reload}=useDashboardArea();
 const [data,setData]=useState<DashboardSummary|null>(null),[loading,setLoading]=useState(false),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 useEffect(()=>{if(!propertyId)return;const c=new AbortController();setLoading(true);setError("");setData(null);
  void api<unknown>(`/api/v1/properties/${propertyId}/dashboard`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readDashboard(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();},[api,propertyId,revision,locale]);
 const property=properties?.find(p=>p.id===propertyId);const base=`/properties/${propertyId}`;

 const money=(v:string)=>formatDecimalCurrency(v,locale);
 return <div className="dashboard py-8">
  <DashboardHero property={property??null} data={data} loading={loading} onRefresh={()=>setRevision(n=>n+1)}/>
  {!allowed?<MessageBanner>{t("roleRequired")}</MessageBanner>
  :listError?<MessageBanner action={<Button variant="view" size="sm" onClick={reload}><RefreshCw/>{ui("tryAgain")}</Button>}>{listError}</MessageBanner>
  :properties&&properties.length===0?<div className="rounded-2xl border bg-card p-6 text-sm"><p className="font-semibold">{t("noAreaTitle")}</p><p className="mt-1 text-muted-foreground">{t("noAreaHint")}</p><Button asChild className="mt-4"><Link href="/properties"><Building2/>{t("goAreas")}</Link></Button></div>
  :error?<MessageBanner action={<Button variant="view" size="sm" onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("tryAgain")}</Button>}>{error}</MessageBanner>
  :!data?<div className="dashboard-kpis" aria-busy="true" aria-label={t("loading")}>{Array.from({length:8},(_,i)=><Skeleton key={i} className="h-28 rounded-2xl"/>)}</div>
  :<>
   <div className="dashboard-kpis">
    <Kpi icon={Building2} tone="blue" value={data.unitCount.toLocaleString(locale)} label={t("units")} href={`${base}/units`} linkLabel={t("openUnits")}/>
    <Kpi icon={DoorOpen} tone="green" value={data.occupiedUnitCount.toLocaleString(locale)} label={t("occupied")} badge={`%${share(data.occupiedUnitCount,data.unitCount)}`}/>
    <Kpi icon={DoorClosed} tone="amber" value={data.vacantUnitCount.toLocaleString(locale)} label={t("vacant")} badge={`%${share(data.vacantUnitCount,data.unitCount)}`}/>
    <Kpi icon={Wallet} tone="teal" value={money(data.receiptsThisMonth)} label={t("receipts")} note={t("receiptsNote")} href={`${base}/payments`} linkLabel={t("openPayments")} extra={<Spark values={data.trend.map(m=>plot(m.receipts))} label={t("receiptsTrend")}/>}/>
    <Kpi icon={CircleAlert} tone="red" value={money(data.overdueAmount)} label={t("overdue")} note={t("chargeCount",{count:data.overdueChargeCount})} href={`${base}/dues`} linkLabel={t("openDues")}/>
    {data.requests&&<Kpi icon={Wrench} tone="violet" value={(data.requests.recorded+data.requests.inProgress).toLocaleString(locale)} label={t("openRequests")} note={t("requestSplit",{recorded:data.requests.recorded,inProgress:data.requests.inProgress})} href={`${base}/requests`} linkLabel={t("openRequestsLink")}/>}
    <Kpi icon={CalendarClock} tone="blue" value={money(data.upcomingAmount)} label={t("upcoming")} note={t("upcomingNote",{count:data.upcomingChargeCount})} href={`${base}/dues`} linkLabel={t("openDues")}/>
    <Kpi icon={Hammer} tone="neutral" value="—" label={t("maintenance")} note={t("inDevelopment")} pending/>
   </div>
   <div className="dashboard-grid">
    <TrendChart data={data} locale={locale}/>
    <section className="dashboard-panel" aria-labelledby="dash-types"><h2 id="dash-types">{t("typesTitle")}</h2><Donut data={data} locale={locale}/></section>
   </div>
   <section className="dashboard-panel" aria-labelledby="dash-next"><h2 id="dash-next">{t("nextTitle")}</h2>
    <div className="dashboard-next">
     {data.upcomingDues.map(d=><Link key={d.dueDate} href={`${base}/dues`} className="dashboard-next-item"><span className="dashboard-icon tone-red"><ReceiptText aria-hidden="true"/></span><span className="min-w-0 flex-1"><strong>{t("dueItem")}</strong><small>{t("chargeCount",{count:d.chargeCount})} · {money(d.amount)}</small></span><time dateTime={d.dueDate}>{formatDateOnly(d.dueDate,locale)}</time></Link>)}
     <div className="dashboard-next-item dashboard-next-pending"><span className="dashboard-icon tone-neutral"><Hammer aria-hidden="true"/></span><span className="min-w-0 flex-1"><strong>{t("maintenance")}</strong><small>{t("maintenanceEmpty")}</small></span></div>
     {data.upcomingDues.length===0&&<p className="text-sm text-muted-foreground">{t("nextEmpty")}</p>}
    </div>
   </section>
  </>}
 </div>;
}
function Kpi({icon:Icon,tone,value,label,note,badge,href,linkLabel,extra,pending}:{icon:LucideIcon;tone:string;value:string;label:string;note?:string;badge?:string;href?:string;linkLabel?:string;extra?:React.ReactNode;pending?:boolean}){
 return <div className={cn("dashboard-kpi",pending&&"dashboard-kpi-pending")}>
  <span className={`dashboard-icon tone-${tone}`}><Icon aria-hidden="true"/></span>
  <div className="min-w-0"><p className="dashboard-kpi-value">{value}</p><p className="dashboard-kpi-label">{label}{badge&&<span className="dashboard-badge">{badge}</span>}</p>{note&&<p className="dashboard-kpi-note">{note}</p>}{extra}</div>
  {href&&<Link href={href} className="dashboard-kpi-link" aria-label={linkLabel} title={linkLabel}><ArrowUpRight aria-hidden="true"/></Link>}
 </div>;
}
function Spark({values,label}:{values:number[];label:string}){
 const max=Math.max(...values,0);if(max<=0)return null;const w=96,h=24;
 return <svg className="dashboard-spark" viewBox={`0 0 ${w} ${h}`} role="img" aria-label={label}>{values.map((v,i)=>{const bh=Math.max(2,v/max*h);return <rect key={i} x={i*(w/values.length)+2} y={h-bh} width={w/values.length-4} height={bh} rx={2} fill="var(--chart-2)" opacity={i===values.length-1?1:.45}/>;})}</svg>;
}
function Donut({data,locale}:{data:DashboardSummary;locale:Locale}){
 const t=useTranslations("dashboard"),ui=useTranslations("ui");
 const rows=data.unitTypes.map((u,i)=>({...u,color:TYPE_COLORS[UNIT_TYPES.indexOf(u.type)],index:i})).filter(u=>u.count>0);
 const R=70,C=2*Math.PI*R;let offset=0;
 return <div className="dashboard-donut">
  <svg viewBox="0 0 200 200" role="img" aria-label={t("typesTitle")}>
   <circle cx={100} cy={100} r={R} fill="none" stroke="var(--muted)" strokeWidth={22}/>
   {rows.map(u=>{const len=u.count/data.unitCount*C,gap=rows.length>1?2:0,el=<circle key={u.type} cx={100} cy={100} r={R} fill="none" stroke={u.color} strokeWidth={22} strokeDasharray={`${Math.max(len-gap,0)} ${C}`} strokeDashoffset={-offset} transform="rotate(-90 100 100)"><title>{`${ui(unitTypeNames[u.type])}: ${u.count}`}</title></circle>;offset+=len;return el;})}
   <text x={100} y={98} textAnchor="middle" className="dashboard-donut-total">{data.unitCount.toLocaleString(locale)}</text>
   <text x={100} y={120} textAnchor="middle" className="dashboard-axis">{t("total")}</text>
  </svg>
  {rows.length?<ul className="dashboard-legend dashboard-legend-list">{rows.map(u=><li key={u.type}><span style={{background:u.color}}/>{ui(unitTypeNames[u.type])}<b>{u.count.toLocaleString(locale)}</b><small>%{share(u.count,data.unitCount)}</small></li>)}</ul>:<p className="text-sm text-muted-foreground">{t("typesEmpty")}</p>}
 </div>;
}
