"use client";
import {useCallback,useEffect,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {BarChart3,CalendarRange,ChevronRight,CircleCheckBig,ClipboardList,Copy,Ellipsis,Eye,FileDown,Hourglass,LayoutGrid,LayoutTemplate,Lightbulb,Pencil,Plus,Trophy,Users,type LucideIcon} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useSearch} from "@/features/people/useRows";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Skeleton} from "@/components/ui/skeleton";
import {DropdownMenu,DropdownMenuContent,DropdownMenuItem,DropdownMenuSeparator,DropdownMenuTrigger} from "@/components/ui/dropdown-menu";
import {RecordList} from "@/components/management/RecordList";
import {FilterChips} from "@/components/management/FilterChips";
import {ListToolbar,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDate,formatDateOnly} from "@/lib/format";
import {cn} from "@/lib/utils";
import {ParticipationBar,SurveyCategoryBadge,SurveyCategoryTile,SurveyStateBadge} from "./SurveyBadges";
import {CategoryDonut} from "./CategoryDonut";
import {stateIcon,stateTone} from "./category-style";
import {SurveyEditor} from "./SurveyEditor";
import {SurveyDetailPanel,useSurveyExport} from "./SurveyDetailPanel";
import {SurveyPicker} from "./SurveyPicker";
import {participation,readDetail,readOverview,readPage,share,surveyCategories,type SurveyDetail,type SurveyOverview,type SurveyPage,type SurveySummary,type SurveyTemplate} from "./model";

type Locale="tr"|"en";
type Panel={kind:"editor";detail?:SurveyDetail;template?:boolean;preset?:SurveyTemplate["id"]}|{kind:"detail";survey:SurveySummary}|{kind:"pick";purpose:"copy"|"export"};
// Tabs follow the design order; "scheduled" only appears when such surveys exist.
const tabs=["","DRAFT","ACTIVE","SCHEDULED","COMPLETED","EXPIRED"] as const;

export function SurveysWorkspace({propertyId}:{propertyId:string}){
 const t=useTranslations("surveys"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),identity=useIdentity(),scope=usePropertyScope(),notice=useNotification();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r));
 const canWrite=manager&&scope.property?.id===propertyId&&!!scope.property.isEnabled&&!scope.error;
 const [data,setData]=useState<SurveyPage|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState("");
 const [overview,setOverview]=useState<SurveyOverview|null>(null),[overviewError,setOverviewError]=useState(""),[revision,setRevision]=useState(0);
 const [term,setTerm]=useState(""),search=useSearch(term),[state,setState]=useState(""),[category,setCategory]=useState(""),[from,setFrom]=useState(""),[to,setTo]=useState(""),[page,setPage]=useState(1),[pageSize,setPageSize]=useState(10);
 const [panel,setPanel]=useState<Panel|null>(null);
 const rangeInvalid=!!from&&!!to&&from>to;
 const root=`/api/v1/properties/${propertyId}/surveys`;
 const query=new URLSearchParams({page:String(page),pageSize:String(pageSize)});if(search)query.set("search",search);if(state)query.set("state",state);if(category)query.set("category",category);if(from)query.set("from",from);if(to)query.set("to",to);
 const path=`${root}?${query}`;
 useEffect(()=>setPage(1),[search,state,category,from,to,pageSize]);
 useEffect(()=>{if(!manager||rangeInvalid)return;const c=new AbortController();setLoading(true);setError("");setData(null);
  void api<unknown>(path,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readPage(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();},[api,path,propertyId,locale,revision,manager,rangeInvalid]);
 useEffect(()=>{if(!manager)return;const c=new AbortController();setOverviewError("");
  void api<unknown>(`${root}/overview`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setOverview(readOverview(v,propertyId));}).catch(e=>{if(!c.signal.aborted){setOverview(null);setOverviewError(errorMessage(e,locale));}});
  return()=>c.abort();},[api,root,propertyId,locale,revision,manager]);
 useEffect(()=>{if(data&&page>Math.max(1,Math.ceil(data.totalCount/pageSize)))setPage(Math.max(1,Math.ceil(data.totalCount/pageSize)));},[data,page,pageSize]);
 const refresh=useCallback(()=>setRevision(n=>n+1),[]);
 const exporter=useSurveyExport(propertyId);
 const percent=useCallback((n:number)=>new Intl.NumberFormat(locale==="en"?"en-GB":"tr-TR",{style:"percent",maximumFractionDigits:0}).format(n/100),[locale]);
 async function copy(id:string){
  try{const copied=readDetail(await api<unknown>(`${root}/${id}/copy`,{method:"POST"}),propertyId);notice.success(t("copied"));refresh();setPanel({kind:"editor",detail:copied});}
  catch(e){notice.error(errorMessage(e,locale));}
 }
 async function edit(s:SurveySummary){
  try{setPanel({kind:"editor",detail:readDetail(await api<unknown>(`${root}/${s.id}`),propertyId)});}catch(e){notice.error(errorMessage(e,locale));}
 }
 const counts=data?.stateCounts,total=counts?.reduce((a,c)=>a+c.count,0);
 const chips=tabs.filter(v=>v!=="SCHEDULED"||!!counts?.find(c=>c.key==="SCHEDULED")?.count||state==="SCHEDULED").map(v=>({value:v,label:v?t(`tab_${v}`):t("tab_all"),count:v?counts?.find(c=>c.key===v)?.count:total,icon:v?stateIcon[v]:LayoutGrid,tone:v?stateTone[v]:"blue" as const}));
 const columns:ColumnDef<SurveySummary>[]=([
  {id:"title",header:t("col_title"),cell:({row})=>{const s=row.original;return <button type="button" className="survey-title-cell" onClick={()=>setPanel({kind:"detail",survey:s})}><SurveyCategoryTile category={s.category}/><span className="min-w-0"><strong className="line-clamp-1 break-words">{s.title}</strong>{s.description&&<span className="line-clamp-1 break-words text-xs text-muted-foreground">{s.description}</span>}</span></button>;}},
  {id:"category",header:t("col_category"),cell:({row})=><SurveyCategoryBadge category={row.original.category} label={t(`category_${row.original.category}`)}/>},
  {id:"start",header:t("col_start"),cell:({row})=><time className="whitespace-nowrap" dateTime={row.original.startDate}>{formatDateOnly(row.original.startDate,locale)}</time>},
  {id:"end",header:t("col_end"),cell:({row})=><time className="whitespace-nowrap" dateTime={row.original.endDate}>{formatDateOnly(row.original.endDate,locale)}</time>},
  {id:"participation",header:t("col_participation"),cell:({row})=>{const s=row.original,p=participation(s);return s.status==="DRAFT"?<span className="text-xs text-muted-foreground">{t("notStarted")}</span>:<ParticipationBar count={`${s.responseCount.toLocaleString(locale)} / ${s.eligibleCount.toLocaleString(locale)}`} percent={p} percentText={percent(p)} label={t("participationLabel",{responses:s.responseCount,eligible:s.eligibleCount,percent:p})}/>;}},
  {id:"state",header:ui("status"),cell:({row})=><SurveyStateBadge state={row.original.state} label={t(`state_${row.original.state}`)}/>},
  {id:"actions",header:ui("actions"),cell:({row})=>rowActions(row.original)},
 ]);
 function rowActions(s:SurveySummary){
  return <DropdownMenu><DropdownMenuTrigger asChild><Button variant="neutral" size="icon-sm" aria-label={t("actionsFor",{title:s.title})}><Ellipsis/></Button></DropdownMenuTrigger>
   <DropdownMenuContent align="end">
    <DropdownMenuItem onSelect={()=>setPanel({kind:"detail",survey:s})}>{s.status==="DRAFT"?<Eye/>:<BarChart3/>}{t(s.status==="DRAFT"?"view":"results")}</DropdownMenuItem>
    {canWrite&&s.status==="DRAFT"&&<DropdownMenuItem onSelect={()=>void edit(s)}><Pencil/>{t("edit")}</DropdownMenuItem>}
    {canWrite&&<DropdownMenuItem onSelect={()=>void copy(s.id)}><Copy/>{t("copy")}</DropdownMenuItem>}
    {s.status!=="DRAFT"&&<><DropdownMenuSeparator/><DropdownMenuItem disabled={exporter.busy} onSelect={()=>void exporter.run(s)}><FileDown/>{t("export")}</DropdownMenuItem></>}
   </DropdownMenuContent></DropdownMenu>;
 }
 if(!manager)return <div className="py-8"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;
 const o=overview;
 return <div className="survey-page py-8">
  <header className="survey-header">
   <span className="survey-header-icon"><ClipboardList aria-hidden="true"/></span>
   <div className="min-w-0 flex-1"><h1>{t("title_page")}</h1><p>{t("help")}</p>
    <nav aria-label={t("breadcrumb")} className="survey-breadcrumb"><ol><li><Link href="/dashboard">{t("crumbHome")}</Link></li><li aria-hidden="true"><ChevronRight/></li><li>{t("crumbCommunication")}</li><li aria-hidden="true"><ChevronRight/></li><li aria-current="page">{t("title_page")}</li></ol></nav>
   </div>
   <div className="flex flex-col items-end gap-2"><Button size="lg" disabled={!canWrite} onClick={()=>setPanel({kind:"editor"})}><Plus/>{t("new")}</Button><span className="text-xs text-muted-foreground">{scope.property?.name??ui("loading")}</span></div>
  </header>
  {scope.error&&<MessageBanner action={<Button variant="view" onClick={scope.refresh}>{ui("tryAgain")}</Button>}>{scope.error}</MessageBanner>}
  {scope.property&&!scope.property.isEnabled&&<MessageBanner kind="warning">{t("inactive")}</MessageBanner>}
  {overviewError&&<MessageBanner action={<Button variant="view" size="sm" onClick={refresh}>{ui("tryAgain")}</Button>}>{overviewError}</MessageBanner>}

  <div className="survey-kpis" aria-busy={!o}>
   {!o?Array.from({length:4},(_,i)=><Skeleton key={i} className="h-28 rounded-2xl"/>):<>
    <Kpi tone="green" icon={BarChart3} label={t("kpi_total")} value={o.total.toLocaleString(locale)} note={t("kpi_totalNote",{count:o.createdThisMonth})}/>
    <Kpi tone="blue" icon={Users} label={t("kpi_responses")} value={o.responses.toLocaleString(locale)} note={t("kpi_responsesNote",{percent:percent(share(o.responses,o.eligible))})}/>
    <Kpi tone="amber" icon={Hourglass} label={t("kpi_active")} value={o.active.toLocaleString(locale)} note={t("kpi_activeNote")}/>
    <Kpi tone="red" icon={CircleCheckBig} label={t("kpi_completed")} value={o.completed.toLocaleString(locale)} note={t("kpi_completedNote",{percent:percent(share(o.completedResponses,o.completedEligible))})}/>
   </>}
  </div>

  <div className="survey-main">
   <section className="survey-card min-w-0" aria-labelledby="survey-list-title">
    <h2 id="survey-list-title" className="sr-only">{t("listTitle")}</h2>
    <div className="survey-filters">
     <FilterChips label={t("stateFilter")} items={chips} value={state} onChange={setState} disabled={loading&&!data}/>
    </div>
    <ListToolbar term={term} onTerm={setTerm} searchLabel={t("search")} placeholder={t("search")} loading={loading} onRefresh={refresh} onReset={()=>{setTerm("");setState("");setCategory("");setFrom("");setTo("");}} resetEnabled={!!term||!!state||!!category||!!from||!!to}>
     <div className="flex flex-wrap items-center gap-2"><CalendarRange aria-hidden="true" className="size-4 text-muted-foreground"/><Label htmlFor="survey-from" className="sr-only">{t("from")}</Label><Input id="survey-from" type="date" className="h-10 w-auto bg-card" min="2000-01-01" max="2100-12-31" value={from} aria-invalid={rangeInvalid} title={t("from")} onChange={e=>setFrom(e.target.value)}/><span aria-hidden="true">–</span><Label htmlFor="survey-to" className="sr-only">{t("to")}</Label><Input id="survey-to" type="date" className="h-10 w-auto bg-card" min="2000-01-01" max="2100-12-31" value={to} aria-invalid={rangeInvalid} title={t("to")} onChange={e=>setTo(e.target.value)}/></div>
     <select aria-label={t("col_category")} className={toolbarSelectClass} value={category} onChange={e=>setCategory(e.target.value)}><option value="">{t("allCategories")}</option>{surveyCategories.map(c=><option key={c} value={c}>{t(`category_${c}`)} ({data?.categoryCounts.find(x=>x.key===c)?.count??"…"})</option>)}</select>
    </ListToolbar>
    {rangeInvalid?<div className="p-4"><MessageBanner kind="warning">{t("rangeInvalid")}</MessageBanner></div>:
    <RecordList compact cardBreakpoint="xl" data={data} loading={loading} error={error} columns={columns} page={page} onPage={setPage} onRetry={refresh}
     footerControls={<select className={toolbarSelectClass} aria-label={ui("recordsPerPage")} value={pageSize} onChange={e=>setPageSize(Number(e.target.value))}>{[10,20,50].map(n=><option value={n} key={n}>{n}</option>)}</select>}
     empty={<><ClipboardList className="mx-auto mb-3 size-8"/><strong>{t("empty")}</strong><p className="mt-2">{t("emptyHelp")}</p>{canWrite&&<Button className="mt-4" onClick={()=>setPanel({kind:"editor",template:true})}><LayoutTemplate/>{t("fromTemplate")}</Button>}</>}
     card={s=>{const p=participation(s);return <><div className="flex items-start gap-3"><SurveyCategoryTile category={s.category}/><div className="min-w-0 flex-1"><strong className="break-words">{s.title}</strong><div className="mt-1 flex flex-wrap items-center gap-2"><SurveyCategoryBadge category={s.category} label={t(`category_${s.category}`)}/><SurveyStateBadge state={s.state} label={t(`state_${s.state}`)}/></div></div>{rowActions(s)}</div>
      <p className="my-3 text-xs text-muted-foreground">{formatDateOnly(s.startDate,locale)} – {formatDateOnly(s.endDate,locale)}</p>
      {s.status!=="DRAFT"&&<ParticipationBar count={`${s.responseCount.toLocaleString(locale)} / ${s.eligibleCount.toLocaleString(locale)}`} percent={p} percentText={percent(p)} label={t("participationLabel",{responses:s.responseCount,eligible:s.eligibleCount,percent:p})}/>}
      <Button className="mt-3" variant="view" size="sm" onClick={()=>setPanel({kind:"detail",survey:s})}>{s.status==="DRAFT"?<Eye/>:<BarChart3/>}{t(s.status==="DRAFT"?"view":"results")}</Button></>;}}/>}
   </section>

   <div className="survey-side">
    <section className="survey-card survey-side-card" aria-labelledby="survey-quick"><h2 id="survey-quick"><span className="survey-card-icon tone-blue"><ClipboardList aria-hidden="true"/></span>{t("quickTitle")}</h2>
     <ul className="survey-quick">
      <Quick icon={Plus} tone="blue" label={t("quickNew")} disabled={!canWrite} onClick={()=>setPanel({kind:"editor"})}/>
      <Quick icon={LayoutTemplate} tone="violet" label={t("quickTemplate")} disabled={!canWrite} onClick={()=>setPanel({kind:"editor",template:true})}/>
      <Quick icon={Copy} tone="amber" label={t("quickCopy")} disabled={!canWrite||!o?.total} onClick={()=>setPanel({kind:"pick",purpose:"copy"})}/>
      <Quick icon={FileDown} tone="red" label={t("quickExport")} disabled={!o||o.total===o.drafts} onClick={()=>setPanel({kind:"pick",purpose:"export"})}/>
     </ul>
    </section>
    <section className="survey-card survey-side-card" aria-labelledby="survey-categories"><h2 id="survey-categories"><span className="survey-card-icon tone-violet"><BarChart3 aria-hidden="true"/></span>{t("categoryTitle")}</h2>
     {o?<CategoryDonut counts={o.categories} total={o.total} label={(c)=>t(`category_${c}`)} centerLabel={t("donutCenter")} description={t("donutDescription",{count:o.total})} empty={t("donutEmpty")} locale={locale}/>:<Skeleton className="mt-4 h-44 rounded-xl"/>}
    </section>
   </div>
  </div>

  <div className="survey-bottom">
   <section className="survey-card" aria-labelledby="survey-top"><h2 id="survey-top"><Trophy aria-hidden="true" className="survey-h2-icon text-(--action-warn-text)"/>{t("topTitle")}</h2>
    {!o?<Skeleton className="mt-4 h-32 rounded-xl"/>:o.topParticipation.length===0?<p className="survey-empty">{t("topEmpty")}</p>:
    <ol className="survey-top">{o.topParticipation.map((s,i)=>{const p=participation(s);return <li key={s.id}><span className={`survey-rank survey-rank-${i+1}`}>{i+1}</span><div className="min-w-0 flex-1"><div className="flex items-baseline justify-between gap-3"><button type="button" className="survey-link line-clamp-1" onClick={()=>setPanel({kind:"detail",survey:s})}>{s.title}</button><b>{percent(p)}</b></div><span className="survey-meter-track"><span className="survey-meter-fill survey-meter-high" style={{width:`${Math.min(100,p)}%`}}/></span></div></li>;})}</ol>}
   </section>
   <section className="survey-card" aria-labelledby="survey-last"><h2 id="survey-last"><CircleCheckBig aria-hidden="true" className="survey-h2-icon text-(--message-success-text)"/>{t("lastTitle")}</h2>
    {!o?<Skeleton className="mt-4 h-32 rounded-xl"/>:!o.lastCompleted?<p className="survey-empty">{t("lastEmpty")}</p>:lastCompleted(o.lastCompleted)}
   </section>
   <section className="survey-card" aria-labelledby="survey-tips"><h2 id="survey-tips"><Lightbulb aria-hidden="true" className="survey-h2-icon text-(--action-warn-text)"/>{t("tipsTitle")}</h2>
    <ul className="survey-tips">
     <Tip label={t("tip_quarterly")} disabled={!canWrite} onClick={()=>setPanel({kind:"editor",preset:"satisfaction"})}/>
     <Tip label={t("tip_decisions")} disabled={!canWrite} onClick={()=>setPanel({kind:"editor",preset:"dues"})}/>
     <li><Link className="survey-tip" href={`/properties/${propertyId}/announcements`}><ChevronRight aria-hidden="true"/><span>{t("tip_share")}</span><ChevronRight aria-hidden="true" className="ml-auto"/></Link></li>
     <Tip label={t("tip_templates")} disabled={!canWrite} onClick={()=>setPanel({kind:"editor",template:true})}/>
    </ul>
   </section>
  </div>

  {panel?.kind==="editor"&&<SurveyEditor propertyId={propertyId} detail={panel.detail} startWithTemplates={!!panel.template} preset={panel.preset} canWrite={canWrite} onClose={()=>setPanel(null)} onSaved={refresh}/>}
  {panel?.kind==="detail"&&<SurveyDetailPanel propertyId={propertyId} survey={panel.survey} canWrite={canWrite} onClose={()=>setPanel(null)} onChanged={refresh} onEdit={d=>setPanel({kind:"editor",detail:d})} onCopy={id=>void copy(id)}/>}
  {panel?.kind==="pick"&&<SurveyPicker propertyId={propertyId} purpose={panel.purpose} onClose={()=>setPanel(null)} onPick={s=>{setPanel(null);if(panel.purpose==="copy")void copy(s.id);else void exporter.run(s);}}/>}
 </div>;

function lastCompleted(s:SurveySummary){const fmt=percent,onOpen=(v:SurveySummary)=>setPanel({kind:"detail",survey:v});
  return <div className="survey-last"><div className="flex items-start gap-3"><SurveyCategoryTile category={s.category} size="lg"/><div className="min-w-0"><button type="button" className="survey-link text-left font-bold" onClick={()=>onOpen(s)}>{s.title}</button>{s.description&&<p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{s.description}</p>}</div></div>
   <dl className="survey-last-stats"><div><dt><Users aria-hidden="true"/>{t("lastResponses")}</dt><dd>{s.responseCount.toLocaleString(locale)} / {s.eligibleCount.toLocaleString(locale)}</dd></div><div><dt><BarChart3 aria-hidden="true"/>{t("lastRate")}</dt><dd>{fmt(participation(s))}</dd></div><div><dt><CircleCheckBig aria-hidden="true"/>{t("lastClosed")}</dt><dd>{s.closedAt?formatDate(s.closedAt,locale):"—"}</dd></div></dl></div>;
 }
}

function Kpi({tone,icon:Icon,label,value,note}:{tone:"green"|"blue"|"amber"|"red";icon:LucideIcon;label:string;value:string;note:string}){
 return <div className={cn("survey-kpi",`survey-kpi-${tone}`)}><span className={`survey-kpi-icon tone-${tone}`}><Icon aria-hidden="true"/></span><div className="min-w-0"><p className="survey-kpi-label">{label}</p><p className="survey-kpi-value">{value}</p><p className="survey-kpi-note">{note}</p></div><span aria-hidden="true" className="survey-kpi-art"><i/><i/><i/></span></div>;
}
function Quick({icon:Icon,tone,label,onClick,disabled}:{icon:LucideIcon;tone:string;label:string;onClick:()=>void;disabled?:boolean}){
 return <li><button type="button" className="survey-quick-item" disabled={disabled} onClick={onClick}><span className={`survey-card-icon tone-${tone}`}><Icon aria-hidden="true"/></span><span>{label}</span><ChevronRight aria-hidden="true" className="ml-auto"/></button></li>;
}
function Tip({label,onClick,disabled}:{label:string;onClick:()=>void;disabled?:boolean}){
 return <li><button type="button" className="survey-tip" disabled={disabled} onClick={onClick}><ChevronRight aria-hidden="true"/><span>{label}</span><ChevronRight aria-hidden="true" className="ml-auto"/></button></li>;
}
