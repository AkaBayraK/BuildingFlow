"use client";
import {useCallback,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Download,Printer,Save,RefreshCw} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {FilterChips} from "@/components/management/FilterChips";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {useRows} from "@/features/people/useRows";
import {Choices} from "@/features/person-center/Choices";
import {workspace,type Workspace} from "@/features/person-center/model";
import {formatDateOnly,formatDecimalCurrency} from "@/lib/format";
import {alertStyle,relationStyle} from "./supplier-style";
import {alertKinds,readAlertPage,readPreference,readPerformance,alertsCsv,performanceCsv,type AlertKind,type SupplierAlert,type AlertPreference,type PerformanceReport} from "./insights-model";
const AreaIcon=relationStyle.area.icon;
const istanbulDay=(offset=0)=>{const d=new Date(Date.now()+offset*86400000);return new Intl.DateTimeFormat("en-CA",{timeZone:"Europe/Istanbul",year:"numeric",month:"2-digit",day:"2-digit"}).format(d);};
function save(name:string,csv:string){const url=URL.createObjectURL(new Blob([csv],{type:"text/csv;charset=utf-8"}));const a=document.createElement("a");a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
export function SupplierAlerts({tenant}:{tenant:Workspace}){
 const t=useTranslations("suppliers"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();
 const [kind,setKind]=useState<AlertKind|"ALL">("ALL"),[page,setPage]=useState(1),[revision,setRevision]=useState(0),[draft,setDraft]=useState<AlertPreference|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState("");
 const decode=useCallback((v:unknown)=>readAlertPage(v),[]);
 const rows=useRows<SupplierAlert&{id:string}>(`/api/v2/tenants/${tenant.id}/supplier-alerts?page=${page}&pageSize=20${kind==="ALL"?"":`&kind=${kind}`}`,revision,true,decode);
 const data=rows.data as ReturnType<typeof readAlertPage>|null,preference=draft??data?.preference??null;
 const kinds=Object.fromEntries(alertKinds.map(k=>[k,t(`alert_${k}`)])) as Record<AlertKind,string>;
 async function savePreference(){if(!preference)return;setBusy(true);setError("");try{readPreference(await api<unknown>(`/api/v2/tenants/${tenant.id}/supplier-alert-preferences`,{method:"PUT",body:JSON.stringify(preference)}));setDraft(null);setRevision(n=>n+1);notice.success(t("alertPreferenceSaved"));}catch(e){setError(errorMessage(e,locale));}finally{setBusy(false);}}
 return <section className="space-y-4">
  <p className="text-sm text-muted-foreground">{t("alertsHelp")}</p>
  {preference&&<fieldset disabled={busy} className="print-hidden space-y-2 rounded-xl border p-4"><legend className="px-1 text-sm font-medium">{t("alertPreferences")}</legend><div className="flex flex-wrap gap-4">{(["contractExpiry","workOrderOverdue","maintenanceDue"] as const).map(k=><label key={k} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={preference[k]} onChange={e=>setDraft({...preference,[k]:e.target.checked})}/>{t(`alertPreference_${k}`)}</label>)}</div>{error&&<MessageBanner>{error}</MessageBanner>}<div className="flex gap-2"><Button type="button" size="sm" disabled={busy||!draft} onClick={()=>void savePreference()}><Save aria-hidden="true"/>{ui("save")}</Button>{draft&&<Button type="button" size="sm" variant="outline" onClick={()=>{setDraft(null);setError("");}}><RefreshCw aria-hidden="true"/>{ui("cancel")}</Button>}</div></fieldset>}
  <div className="print-hidden"><FilterChips label={t("alertKind")} value={kind} disabled={rows.loading} onChange={v=>{setKind(v as AlertKind|"ALL");setPage(1);}} items={[{value:"ALL",label:t("invoiceAll"),icon:alertStyle.ALL.icon},...alertKinds.map(k=>({value:k,label:kinds[k],icon:alertStyle[k].icon}))]}/></div>
  <div className="print-hidden flex flex-wrap gap-2"><Button type="button" variant="outline" disabled={!data?.items.length} onClick={()=>data&&save(`supplier-alerts-${istanbulDay()}.csv`,alertsCsv(data.items,{kind:t("alertKind"),supplier:t("title"),property:t("invoiceProperty"),title:t("contractTitle"),date:t("alertDate"),days:t("alertDays"),kinds}))}><Download aria-hidden="true"/>{t("exportCsv")}</Button><Button type="button" variant="outline" disabled={!data?.items.length} onClick={()=>window.print()}><Printer aria-hidden="true"/>{t("exportPdf")}</Button></div>
  {data?.truncated&&<MessageBanner kind="warning">{t("alertsTruncated")}</MessageBanner>}
  {rows.error?<MessageBanner>{rows.error}</MessageBanner>:rows.loading?<p role="status">{ui("loading")}</p>:<ul className="space-y-2">{data?.items.map(a=>{const style=alertStyle[a.kind];return <li key={a.id} className="rounded-xl border p-3 text-sm"><div className="flex flex-wrap items-center justify-between gap-2"><strong className="break-words">{a.title}</strong><ToneBadge icon={style.icon} tone={style.tone}>{kinds[a.kind]}</ToneBadge></div><p className="flex items-start gap-2 text-xs text-muted-foreground"><AreaIcon aria-hidden="true" className="size-4 shrink-0"/>{a.supplierName} · {a.propertyName}</p><p className="text-xs">{formatDateOnly(a.date,locale)} · {a.days<0?t("alertOverdueDays",{days:-a.days}):t("alertInDays",{days:a.days})}</p></li>;})}</ul>}
  {!rows.loading&&!rows.error&&!data?.items.length&&<p className="text-sm text-muted-foreground">{t("noAlerts")}</p>}
  <div className="print-hidden flex gap-2"><Button variant="outline" size="sm" disabled={rows.loading||page===1} onClick={()=>setPage(n=>n-1)}>{ui("previousPage")}</Button><Button variant="outline" size="sm" disabled={rows.loading||!data||page*20>=data.totalCount} onClick={()=>setPage(n=>n+1)}>{ui("nextPage")}</Button></div>
 </section>;
}
export function SupplierPerformance({tenant}:{tenant:Workspace}){
 const t=useTranslations("suppliers"),ui=useTranslations("ui"),locale=useLocale();
 const [from,setFrom]=useState(()=>istanbulDay(-90)),[to,setTo]=useState(()=>istanbulDay()),[property,setProperty]=useState<Workspace|null>(null),[page,setPage]=useState(1);
 const valid=!!from&&!!to&&from<=to&&(Date.parse(to)-Date.parse(from))/86400000<=366;
 const decode=useCallback((v:unknown)=>readPerformance(v) as unknown as {items:(PerformanceReport["items"][number]&{id:string})[];page:number;pageSize:number;totalCount:number},[]);
 const rows=useRows(`/api/v2/tenants/${tenant.id}/supplier-performance?from=${from}&to=${to}&page=${page}&pageSize=20${property?`&propertyId=${property.id}`:""}`,0,valid,decode);
 const report=rows.data as unknown as PerformanceReport|null;
 const csvLabels={supplier:t("title"),completed:t("perfCompleted"),hours:t("perfHours"),sla:t("perfSla"),maintenance:t("perfMaintenance"),rate:t("perfRate"),invoices:t("perfInvoices"),total:t("perfTotal"),score:t("perfScore"),sample:t("perfSample"),insufficient:t("perfInsufficient")};
 return <section className="space-y-4">
  <p className="text-sm text-muted-foreground">{t("performanceHelp",{minimum:report?.minimumSample??3})}</p>
  <div className="print-hidden grid gap-3 sm:grid-cols-3"><div><Label htmlFor="supplier-perf-from">{t("starts")}</Label><Input id="supplier-perf-from" type="date" min="2000-01-01" value={from} onChange={e=>{setFrom(e.target.value);setPage(1);}}/></div><div><Label htmlFor="supplier-perf-to">{t("ends")}</Label><Input id="supplier-perf-to" type="date" min={from} value={to} onChange={e=>{setTo(e.target.value);setPage(1);}}/></div><div className="space-y-1"><Choices path={`/api/v2/tenants/${tenant.id}/supplier-property-choices`} value={property?.id??""} onChange={v=>{setProperty(v);setPage(1);}} parse={workspace} id="supplier-perf-property" label={t("invoiceProperty")}/>{property&&<Button type="button" size="sm" variant="outline" onClick={()=>{setProperty(null);setPage(1);}}>{t("allProperties")}</Button>}</div></div>
  {!valid&&<MessageBanner kind="warning">{t("performanceRange")}</MessageBanner>}
  <div className="print-hidden flex flex-wrap gap-2"><Button type="button" variant="outline" disabled={!report?.items.length} onClick={()=>report&&save(`supplier-performance-${from}-${to}.csv`,performanceCsv(report,csvLabels))}><Download aria-hidden="true"/>{t("exportCsv")}</Button><Button type="button" variant="outline" disabled={!report?.items.length} onClick={()=>window.print()}><Printer aria-hidden="true"/>{t("exportPdf")}</Button></div>
  <p className="hidden text-sm print:block">{tenant.name} · {formatDateOnly(from,locale)} – {formatDateOnly(to,locale)}{property?` · ${property.name}`:""}</p>
  {rows.error?<MessageBanner>{rows.error}</MessageBanner>:rows.loading&&valid?<p role="status">{ui("loading")}</p>:<div className="overflow-x-auto"><table className="w-full min-w-[720px] text-sm"><thead><tr className="border-b text-left">{[t("title"),t("perfCompleted"),t("perfHours"),t("perfSla"),t("perfMaintenance"),t("perfRate"),t("perfTotal"),t("perfScore")].map(h=><th key={h} scope="col" className="p-2 font-medium">{h}</th>)}</tr></thead><tbody>{report?.items.map(r=><tr key={r.supplierId} className="border-b"><th scope="row" className="p-2 text-left font-medium">{r.supplierName}</th><td className="p-2">{r.completedWorkOrders}</td><td className="p-2">{r.averageCompletionHours??"—"}</td><td className="p-2">{r.slaOnTimeCount}/{r.slaSampleCount}</td><td className="p-2">{r.maintenanceOnTimeCount}/{r.maintenanceSampleCount}</td><td className="p-2">{r.onTimeRate?`%${r.onTimeRate}`:"—"}</td><td className="p-2">{formatDecimalCurrency(r.invoicedTotal,locale)} ({r.invoiceCount})</td><td className="p-2">{r.score?<strong>{r.score}</strong>:<span className="text-muted-foreground">{t("perfInsufficient")}</span>} <span className="text-xs text-muted-foreground">n={r.scoreSampleCount}</span></td></tr>)}</tbody></table></div>}
  {!rows.loading&&!rows.error&&valid&&!report?.items.length&&<p className="text-sm text-muted-foreground">{t("noPerformance")}</p>}
  <div className="print-hidden flex gap-2"><Button variant="outline" size="sm" disabled={rows.loading||page===1} onClick={()=>setPage(n=>n-1)}>{ui("previousPage")}</Button><Button variant="outline" size="sm" disabled={rows.loading||!report||page*20>=report.totalCount} onClick={()=>setPage(n=>n+1)}>{ui("nextPage")}</Button></div>
 </section>;
}
