"use client";
import {useEffect,useMemo,useRef,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {ArrowUpRight,Building2,CalendarClock,CircleAlert,DoorClosed,DoorOpen,RefreshCw,Wallet,Wrench,Hammer,ReceiptText,type LucideIcon} from "lucide-react";
import {useAuthorizedApi,errorMessage,unitTypeNames,type Property} from "@/services/api";
import {parsePropertyPage} from "@/services/management-contracts";
import {useIdentity} from "@/auth/AuthGate";
import {useAccount} from "@/features/account/useAccount";
import {formatDateOnly,formatDecimalCurrency} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {cn} from "@/lib/utils";
import {readDashboard,share,plot,UNIT_TYPES,type DashboardSummary} from "./model";
import {pickScene,SceneArt,type Scene} from "./scenes";
type Locale="tr"|"en";
const FINANCE=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"];
// Categorical order validated for colour-vision deficiency (dataviz validator, light surface); colour follows the unit type, never its rank.
const TYPE_COLORS=["var(--chart-1)","var(--chart-2)","var(--chart-3)","var(--chart-4)","var(--chart-5)","var(--chart-6)"];
export function DashboardPage(){
 const t=useTranslations("dashboard"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),identity=useIdentity(),{account}=useAccount();
 const allowed=FINANCE.some(r=>identity.hasRealmRole(r));
 const [scene,setScene]=useState<Scene|null>(null);
 const [properties,setProperties]=useState<Property[]|null>(null),[propertyId,setPropertyId]=useState(""),[listError,setListError]=useState("");
 const [data,setData]=useState<DashboardSummary|null>(null),[loading,setLoading]=useState(false),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 // A new background on every page load; chosen after hydration so server and client markup match.
 useEffect(()=>{setScene(pickScene(Math.random()));},[]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();
  void api<unknown>("/api/v1/properties?page=1&pageSize=100&sortBy=name&sortDirection=asc",{signal:c.signal}).then(v=>{if(c.signal.aborted)return;const items=parsePropertyPage(v).items.filter(p=>p.isEnabled);setProperties(items);
   const wanted=new URLSearchParams(window.location.search).get("property");setPropertyId(items.some(p=>p.id===wanted)?wanted!:items[0]?.id??"");}).catch(e=>{if(!c.signal.aborted)setListError(errorMessage(e,locale));});
  return()=>c.abort();},[api,allowed,locale]);
 useEffect(()=>{if(!propertyId)return;const c=new AbortController();setLoading(true);setError("");setData(null);
  void api<unknown>(`/api/v1/properties/${propertyId}/dashboard`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readDashboard(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();},[api,propertyId,revision,locale]);
 function choose(id:string){setPropertyId(id);const url=new URL(window.location.href);url.searchParams.set("property",id);window.history.replaceState(null,"",url);}
 const property=properties?.find(p=>p.id===propertyId);const base=`/properties/${propertyId}`;
 const name=account?.profile.firstName;const dark=scene?.tone==="dark";
 const money=(v:string)=>formatDecimalCurrency(v,locale);
 return <div className="dashboard py-8">
  <section className={cn("dashboard-hero",dark&&"dashboard-hero-dark")} data-scene={scene?.id}>
   {scene&&<SceneArt scene={scene}/>}
   <div className="dashboard-hero-veil" aria-hidden="true"/>
   <div className="dashboard-hero-content">
    <div className="min-w-0">
     <h1>{name?t("greetingName",{name}):t("greeting")}</h1>
     <p>{property?t("subtitleArea",{name:property.name}):t("subtitle")}</p>
    </div>
    {allowed&&properties&&properties.length>0&&<div className="dashboard-hero-actions">
     <label className="sr-only" htmlFor="dashboard-area">{t("area")}</label>
     <select id="dashboard-area" className="dashboard-select" value={propertyId} onChange={e=>choose(e.target.value)}>{properties.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select>
     <Button variant="view" size="icon" aria-label={ui("refresh")} title={ui("refresh")} disabled={loading} onClick={()=>setRevision(n=>n+1)}><RefreshCw className={cn(loading&&"animate-spin")}/></Button>
    </div>}
   </div>
  </section>
  {!allowed?<MessageBanner>{t("roleRequired")}</MessageBanner>
  :listError?<MessageBanner action={<Button variant="view" size="sm" onClick={()=>window.location.reload()}><RefreshCw/>{ui("tryAgain")}</Button>}>{listError}</MessageBanner>
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
    <section className="dashboard-panel" aria-labelledby="dash-trend"><h2 id="dash-trend">{t("trendTitle")}</h2><p className="dashboard-panel-note">{t("trendNote")}</p><Trend data={data} locale={locale}/></section>
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
const monthLabel=(period:string,locale:Locale)=>new Intl.DateTimeFormat(locale==="en"?"en-GB":"tr-TR",{month:"short",timeZone:"UTC"}).format(new Date(period+"-01T00:00:00Z"));
function Trend({data,locale}:{data:DashboardSummary;locale:Locale}){
 const t=useTranslations("dashboard");const [hover,setHover]=useState<number|null>(null);
 // Drawn at the real container width so axis text keeps its 11px size on phones too.
 const box=useRef<HTMLDivElement>(null),[W,setW]=useState(640);
 useEffect(()=>{const el=box.current;if(!el)return;const o=new ResizeObserver(([e])=>setW(Math.max(280,Math.round(e.contentRect.width))));o.observe(el);return()=>o.disconnect();},[]);
 const H=W<480?220:260,L=52,R=24,T=16,B=32;const series=[{key:"receipts" as const,label:t("income"),color:"var(--chart-1)"},{key:"expenses" as const,label:t("expense"),color:"var(--chart-3)"}];
 const max=Math.max(...data.trend.flatMap(m=>[plot(m.receipts),plot(m.expenses)]),0);
 const nice=useMemo(()=>{if(max<=0)return 100;const p=10**Math.floor(Math.log10(max));return Math.ceil(max/p)*p;},[max]);
 const x=(i:number)=>L+i*(W-L-R)/(data.trend.length-1),y=(v:number)=>T+(H-T-B)*(1-v/nice);
 const axis=(v:number)=>new Intl.NumberFormat(locale==="en"?"en-GB":"tr-TR",{notation:"compact",maximumFractionDigits:1}).format(v);
 const empty=max<=0;
 return <div className="dashboard-chart" ref={box}>
  <ul className="dashboard-legend">{series.map(s=><li key={s.key}><span style={{background:s.color}}/>{s.label}</li>)}</ul>
  <svg viewBox={`0 0 ${W} ${H}`} role="img" aria-label={t("trendTitle")} onMouseLeave={()=>setHover(null)}>
   {[0,.25,.5,.75,1].map(f=><g key={f}><line x1={L} x2={W-R} y1={y(nice*f)} y2={y(nice*f)} className="dashboard-grid-line"/><text x={L-8} y={y(nice*f)+4} textAnchor="end" className="dashboard-axis">{axis(nice*f)}</text></g>)}
   {data.trend.map((m,i)=><text key={m.period} x={x(i)} y={H-8} textAnchor="middle" className="dashboard-axis">{monthLabel(m.period,locale)}</text>)}
   {hover!==null&&<line x1={x(hover)} x2={x(hover)} y1={T} y2={H-B} className="dashboard-crosshair"/>}
   {!empty&&series.map(s=><g key={s.key}><polyline fill="none" stroke={s.color} strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" points={data.trend.map((m,i)=>`${x(i)},${y(plot(m[s.key]))}`).join(" ")}/>
    {data.trend.map((m,i)=><circle key={i} cx={x(i)} cy={y(plot(m[s.key]))} r={hover===i?5:4} fill={s.color} stroke="var(--card)" strokeWidth={2}/>)}</g>)}
   {data.trend.map((m,i)=><rect key={m.period} x={x(i)-(W-L-R)/(data.trend.length-1)/2} y={T} width={(W-L-R)/(data.trend.length-1)} height={H-T-B} fill="transparent" onMouseEnter={()=>setHover(i)} onFocus={()=>setHover(i)} tabIndex={-1}/>)}
  </svg>
  {empty&&<p className="dashboard-chart-empty">{t("trendEmpty")}</p>}
  {hover!==null&&<div className="dashboard-tooltip" style={{left:`${x(hover)/W*100}%`}} role="status"><strong>{monthLabel(data.trend[hover].period,locale)} {data.trend[hover].period.slice(0,4)}</strong>{series.map(s=><span key={s.key}><i style={{background:s.color}}/>{s.label}: {formatDecimalCurrency(data.trend[hover][s.key],locale)}</span>)}</div>}
  <table className="sr-only"><caption>{t("trendTitle")}</caption><thead><tr><th>{t("month")}</th><th>{t("income")}</th><th>{t("expense")}</th></tr></thead><tbody>{data.trend.map(m=><tr key={m.period}><td>{m.period}</td><td>{formatDecimalCurrency(m.receipts,locale)}</td><td>{formatDecimalCurrency(m.expenses,locale)}</td></tr>)}</tbody></table>
 </div>;
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
