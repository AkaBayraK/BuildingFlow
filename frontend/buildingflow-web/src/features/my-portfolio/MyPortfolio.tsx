"use client";
import Link from "next/link";
import {useCallback,useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Tabs} from "radix-ui";
import type {ColumnDef} from "@tanstack/react-table";
import {Briefcase,DoorOpen,LayoutGrid,type LucideIcon} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Label} from "@/components/ui/label";
import {categories as requestCategories,priorities as requestPriorities} from "@/features/requests/model";
import {categoryStyle} from "@/features/requests/category-style";
import {useCenterMutation} from "@/features/person-center/hooks";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {FilterChips} from "@/components/management/FilterChips";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,DetailField} from "@/components/management/WorkspacePresentation";
import {formatDateOnly,formatDateTime,formatDecimalCurrency,formatNumber} from "@/lib/format";
import {useCenterRecord} from "@/features/person-center/hooks";
import {CapacityBadge,LeaseStatusBadge,PortfolioBadge,VerificationBadge} from "@/features/person-center/Badges";
import {capacityStyle} from "@/features/person-center/relation-style";
import type {PortfolioUnit} from "@/features/person-center/model";
import {CommonAreasWorkspace} from "@/features/common-areas/CommonAreasWorkspace";
import {workspacePath} from "@/features/session-workspaces/model";
import {sectionStyle} from "./section-style";
import {requestRows,requestCreated,requestsPath,downloadPath,financeSummary,myPortfolio,activeContext,dues,payments,announcements,documents,leases,unitPath,activePath,sectionsFor,type MyPortfolio,type Selection,type UnitAccess,type Rows,type Charge,type Payment,type Lease} from "./model";
export function MyPortfolioView(){const identity=useIdentity();return <View key={identity.subject??""}/>;}
type Found={unit:PortfolioUnit;propertyName:string;buildingName:string|null;personName:string};
function find(value:MyPortfolio,unitId:string):Found|null{for(const p of value.people)for(const x of p.properties)for(const b of x.buildings)for(const u of b.units)if(u.unitId===unitId)return{unit:u,propertyName:x.propertyName,buildingName:b.buildingName,personName:p.displayName};return null;}
function View(){
 const t=useTranslations("myPortfolio"),ui=useTranslations("ui");const [revision,setRevision]=useState(0),[mode,setMode]=useState("all"),[selection,setSelection]=useState<Selection|null>(null),[lost,setLost]=useState(false);
 const data=useCenterRecord("/api/v2/me/portfolio",myPortfolio,revision);const value=data.value;
 // Returning to the tab re-reads authorization; a selection that disappeared from the new fingerprint is dropped, never shown stale.
 useEffect(()=>{const refresh=()=>setRevision(n=>n+1);window.addEventListener("focus",refresh);return()=>window.removeEventListener("focus",refresh);},[]);
 useEffect(()=>{if(selection&&value&&!value.units.some(u=>u.contexts.some(c=>c.contextKey===selection.contextKey))){setSelection(null);setMode("all");setLost(true);}},[value,selection]);
 const select=(u:UnitAccess,c:UnitAccess["contexts"][number])=>{setLost(false);setSelection({tenantId:u.tenantId,propertyId:u.propertyId,unitId:u.unitId,capacity:c.capacity,relationId:c.relationId,contextKey:c.contextKey});setMode("unit");};
 const chips=[{value:"all",label:t("allUnits"),icon:LayoutGrid},{value:"unit",label:t("selectedUnit"),icon:DoorOpen}];
 return <section className="mx-auto max-w-6xl py-8"><WorkspaceHeading scope={t("scope")} title={t("title")} description={t("description")} icon={DoorOpen}/>
  {lost&&<MessageBanner kind="warning">{t("selectionLost")}</MessageBanner>}
  {data.error?<MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button>}>{data.error}</MessageBanner>:!value?<p role="status">{ui("loading")}</p>:
  <><div className="mb-5 flex flex-wrap items-center gap-3"><FilterChips label={t("mode")} items={chips} value={selection?mode:"all"} onChange={v=>{if(v==="all"||selection)setMode(v);}}/><Button variant="view" onClick={()=>setRevision(n=>n+1)}>{t("refresh")}</Button></div>
   {mode==="unit"&&selection?<UnitDetail key={selection.contextKey} selection={selection} access={value.units.find(u=>u.unitId===selection.unitId)!} found={find(value,selection.unitId)} onSwitch={select} onBack={()=>setMode("all")}/>:<AllUnits value={value} selected={selection} onSelect={select}/>}</>}
 </section>;
}
function AllUnits({value,selected,onSelect}:{value:MyPortfolio;selected:Selection|null;onSelect:(u:UnitAccess,c:UnitAccess["contexts"][number])=>void}){
 const t=useTranslations("myPortfolio"),w=useTranslations("sessionWorkspaces");const access=new Map(value.units.map(u=>[u.unitId,u]));
 if(!value.people.length&&!value.workspaces.length)return <MessageBanner kind="info">{t("empty")}</MessageBanner>;
 return <div className="space-y-8">{value.units.some(u=>u.capabilities.includes("VIEW_DUES"))&&<FinanceOverview fingerprint={value.fingerprint}/>}{value.people.map(p=><div key={p.tenantId+p.personId} className="space-y-5"><p className="text-sm text-muted-foreground">{p.tenantName} · {p.displayName}</p>{p.properties.map(x=><section key={x.propertyId} aria-label={x.propertyName}><h2 className="mb-3 text-lg font-semibold">{x.propertyName}</h2><ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{x.buildings.flatMap(b=>b.units.map(u=>{const a=access.get(u.unitId)!;const active=selected?.unitId===u.unitId;return <li key={u.unitId} className={"rounded-xl border bg-card p-4"+(active?" ring-2 ring-ring":"")}>
   <div className="flex flex-wrap items-center justify-between gap-2"><h3 className="font-semibold">{[b.buildingName,u.unitNumber].filter(Boolean).join(" · ")}</h3><PortfolioBadge value={u.status}/></div>
   <div className="mt-3 flex flex-wrap gap-2">{u.capacities.map(c=><CapacityBadge key={c} value={c}/>)}</div>
   <div className="mt-4 flex flex-wrap gap-2">{a.contexts.map(c=>{const Icon=capacityStyle[c.capacity].icon;return <Button key={c.contextKey} className="min-h-11" variant={selected?.contextKey===c.contextKey?"default":"outline"} onClick={()=>onSelect(a,c)}><Icon aria-hidden="true"/>{t("openAs",{role:t("role_"+c.capacity)})}</Button>;})}</div></li>;}))}</ul></section>)}</div>)}
  {value.workspaces.length>0&&<section aria-label={t("workspaces")}><h2 className="mb-1 text-lg font-semibold">{t("workspaces")}</h2><p className="mb-3 text-sm text-muted-foreground">{t("workspacesHelp")}</p><ul className="grid gap-4 sm:grid-cols-2">{value.workspaces.map(c=><li key={c.kind+c.id} className="rounded-xl border bg-card p-4"><p className="flex items-center gap-2 font-semibold"><Briefcase aria-hidden="true" className="size-4"/>{c.propertyName}</p><p className="mt-1 text-sm text-muted-foreground">{c.tenantName} · {w(c.role)}</p><Button asChild variant="outline" className="mt-3 min-h-11"><Link href={workspacePath(c)}>{w("open")}</Link></Button></li>)}</ul></section>}</div>;
}
function UnitDetail({selection,access,found,onSwitch,onBack}:{selection:Selection;access:UnitAccess;found:Found|null;onSwitch:(u:UnitAccess,c:UnitAccess["contexts"][number])=>void;onBack:()=>void}){
 const t=useTranslations("myPortfolio"),ui=useTranslations("ui");const decode=useCallback((v:unknown)=>activeContext(v,selection),[selection]);const active=useCenterRecord(activePath(selection),decode);const [tab,setTab]=useState("");
 if(active.error)return <MessageBanner action={<Button variant="outline" onClick={onBack}>{t("backToAll")}</Button>}>{t("contextUnavailable")}</MessageBanner>;
 if(!active.value||!found)return <p role="status">{ui("loading")}</p>;
 const sections=[...sectionsFor(active.value.capabilities),"common-areas"] as const;const current=sections.includes(tab as typeof sections[number])?tab:sections[0];
 return <div className="space-y-5"><div className="rounded-2xl border bg-card p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><p className="text-sm text-muted-foreground">{active.value.context.tenantName} · {found.propertyName}</p><h2 className="text-xl font-semibold">{[found.buildingName,found.unit.unitNumber].filter(Boolean).join(" · ")}</h2></div><div className="flex flex-wrap gap-2"><PortfolioBadge value={found.unit.status}/><CapacityBadge value={selection.capacity}/></div></div>
  {access.contexts.length>1&&<div className="mt-4"><FilterChips label={t("activeRole")} items={access.contexts.map(c=>({value:c.contextKey,label:t("role_"+c.capacity),icon:capacityStyle[c.capacity].icon}))} value={selection.contextKey} onChange={key=>{const c=access.contexts.find(x=>x.contextKey===key);if(c)onSwitch(access,c);}}/></div>}</div>
  <Tabs.Root value={current} onValueChange={setTab}><Tabs.List aria-label={t("sections")} className="mb-4 flex flex-wrap gap-2 rounded-xl bg-muted p-2">{sections.map(s=>{const Icon:LucideIcon=sectionStyle[s];return <Tabs.Trigger key={s} value={s} className="inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium outline-offset-2 data-[state=active]:bg-card data-[state=active]:shadow-sm"><Icon aria-hidden="true" className="size-4"/>{t("section_"+s)}</Tabs.Trigger>;})}</Tabs.List>
   {sections.map(s=><Tabs.Content key={s} value={s}>{s===current&&<Section name={s} selection={selection} unit={found.unit}/>}</Tabs.Content>)}</Tabs.Root></div>;
}
function useScoped<T>(selection:Selection,section:string,page:number,parse:(v:unknown,key:string)=>T,revision:number){const decode=useCallback((v:unknown)=>parse(v,selection.contextKey),[parse,selection.contextKey]);const r=useCenterRecord(unitPath(selection,section,page),decode,revision);return{data:r.value,loading:r.loading,error:r.error};}
function Section({name,selection,unit}:{name:string;selection:Selection;unit:PortfolioUnit}){
 const t=useTranslations("myPortfolio"),locale=useLocale();
 if(name==="common-areas")return <CommonAreasWorkspace propertyId={selection.propertyId} context={selection}/>;
 if(name==="requests")return <RequestsSection selection={selection}/>;
 if(name==="relations")return <div className="rounded-2xl border bg-card p-5"><p className="mb-3 text-sm text-muted-foreground">{t("relationsReadonly")}</p><ul className="space-y-2">{unit.relations.map(r=><li key={r.relationId} className="flex flex-wrap items-center gap-2"><CapacityBadge value={r.kind}/>{formatDateOnly(r.startDate,locale)} – {r.endDate?formatDateOnly(r.endDate,locale):t("openEnded")}<VerificationBadge value={r.verificationStatus}/></li>)}</ul></div>;
 if(name==="dues")return <DuesSection selection={selection}/>;
 if(name==="payments")return <PaymentsSection selection={selection}/>;
 if(name==="lease")return <LeaseSection selection={selection}/>;
 if(name==="announcements")return <AnnouncementsSection selection={selection}/>;
 return <DocumentsSection selection={selection}/>;
}
function DuesSection({selection}:{selection:Selection}){
 const t=useTranslations("myPortfolio"),locale=useLocale();const [page,setPage]=useState(1),[revision,setRevision]=useState(0);const rows=useScoped(selection,"dues",page,dues,revision);const money=(v:string)=>formatDecimalCurrency(v,locale);
 const columns:ColumnDef<Charge>[]=[{id:"period",header:t("period"),cell:({row})=>row.original.period},{id:"amount",header:t("amount"),cell:({row})=>money(row.original.amount)},{id:"paid",header:t("paid"),cell:({row})=>money(row.original.paid)},{id:"balance",header:t("balance"),cell:({row})=>money(row.original.balance)},{id:"due",header:t("dueDate"),cell:({row})=>formatDateOnly(row.original.dueDate,locale)},{id:"status",header:t("status"),cell:({row})=>t("charge_"+row.original.status)}];
 return <div className="space-y-4">{rows.data&&<dl className="grid gap-3 rounded-2xl border bg-card p-5 sm:grid-cols-4">{(["amount","paid","balance","overdue"] as const).map(k=><DetailField key={k} label={t("summary_"+k)}>{money(rows.data!.summary[k])}</DetailField>)}</dl>}<p className="text-sm text-muted-foreground">{t("duesScope")}</p>
  <RecordList data={rows.data?.rows??null} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} empty={t("noDues")} card={c=><><p className="font-semibold">{c.period} · {money(c.balance)}</p><p className="mt-2 text-sm">{t("dueDate")}: {formatDateOnly(c.dueDate,locale)} · {t("charge_"+c.status)}</p></>}/></div>;
}
function PaymentsSection({selection}:{selection:Selection}){
 const t=useTranslations("myPortfolio"),locale=useLocale();const [page,setPage]=useState(1),[revision,setRevision]=useState(0);const rows=useScoped(selection,"payments",page,payments,revision);
 const columns:ColumnDef<Payment>[]=[{id:"date",header:t("paidDate"),cell:({row})=>formatDateOnly(row.original.paidDate,locale)},{id:"period",header:t("period"),cell:({row})=>row.original.period},{id:"amount",header:t("amount"),cell:({row})=>formatDecimalCurrency(row.original.amount,locale)},{id:"state",header:t("status"),cell:({row})=>t(row.original.cancelled?"paymentCancelled":"paymentRecorded")}];
 return <div className="space-y-3"><p className="text-sm text-muted-foreground">{t("paymentsNote")}</p><RecordList data={rows.data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} empty={t("noPayments")} card={p=><><p className="font-semibold">{formatDecimalCurrency(p.amount,locale)}</p><p className="mt-2 text-sm">{formatDateOnly(p.paidDate,locale)} · {p.period} · {t(p.cancelled?"paymentCancelled":"paymentRecorded")}</p></>}/></div>;
}
function LeaseSection({selection}:{selection:Selection}){
 const t=useTranslations("myPortfolio"),locale=useLocale();const [page,setPage]=useState(1),[revision,setRevision]=useState(0);const rows=useScoped(selection,"leases",page,leases,revision);const period=(l:Lease)=>`${formatDateOnly(l.startDate,locale)} – ${l.endDate?formatDateOnly(l.endDate,locale):t("openEnded")}`;
 const columns:ColumnDef<Lease>[]=[{id:"period",header:t("period"),cell:({row})=>period(row.original)},{id:"reference",header:t("contractReference"),cell:({row})=>row.original.contractReference??"—"},{id:"status",header:t("status"),cell:({row})=><LeaseStatusBadge value={row.original.status}/>},{id:"parties",header:t("partyCount"),cell:({row})=>formatNumber(row.original.partyCount,locale)},{id:"party",header:t("yourContract"),cell:({row})=>t(row.original.isParty?"yes":"no")}];
 return <RecordList data={rows.data} loading={rows.loading} error={rows.error} columns={columns} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} empty={t("noLeases")} card={l=><><p className="font-semibold">{period(l)}</p><p className="my-2"><LeaseStatusBadge value={l.status}/></p><p className="text-sm">{t("partyCount")}: {formatNumber(l.partyCount,locale)} · {t("yourContract")}: {t(l.isParty?"yes":"no")}</p></>}/>;
}
function AnnouncementsSection({selection}:{selection:Selection}){
 const t=useTranslations("myPortfolio"),ui=useTranslations("ui"),locale=useLocale();const [page,setPage]=useState(1),[revision,setRevision]=useState(0);const rows=useScoped(selection,"announcements",page,announcements,revision);
 if(rows.error)return <MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button>}>{rows.error}</MessageBanner>;if(!rows.data)return <p role="status">{ui("loading")}</p>;if(!rows.data.items.length)return <MessageBanner kind="info">{t("noAnnouncements")}</MessageBanner>;
 return <div className="space-y-3"><ul className="space-y-3">{rows.data.items.map(a=><li key={a.id} className="rounded-xl border bg-card p-4"><h3 className="font-semibold">{a.title}</h3><p className="text-xs text-muted-foreground">{formatDateTime(a.publishedAt,locale)}</p><p className="mt-2 whitespace-pre-line text-sm">{a.content}</p></li>)}</ul><div className="flex gap-2"><Button variant="outline" disabled={page===1} onClick={()=>setPage(n=>n-1)}>{ui("previousPage")}</Button><Button variant="outline" disabled={page*20>=rows.data.totalCount} onClick={()=>setPage(n=>n+1)}>{ui("nextPage")}</Button></div></div>;
}
function DocumentsSection({selection}:{selection:Selection}){
 const t=useTranslations("myPortfolio"),locale=useLocale();const [page,setPage]=useState(1),[revision,setRevision]=useState(0);const rows=useScoped(selection,"documents",page,documents,revision);
 return <div className="space-y-3"><p className="text-sm text-muted-foreground">{t("documentsNote")}</p><RecordList data={rows.data} loading={rows.loading} error={rows.error} columns={[{id:"name",header:t("fileName"),cell:({row})=>row.original.fileName},{id:"size",header:t("fileSize"),cell:({row})=>formatNumber(Math.ceil(row.original.size/1024),locale)+" KB"},{id:"date",header:t("addedAt"),cell:({row})=>formatDateTime(row.original.createdAt,locale)},{id:"download",header:t("download"),cell:({row})=><DownloadButton selection={selection} id={row.original.id} name={row.original.fileName}/>}]} page={page} onPage={setPage} onRetry={()=>setRevision(n=>n+1)} empty={t("noDocuments")} card={d=><><p className="font-semibold">{d.fileName}</p><p className="mt-2 text-sm">{formatDateTime(d.createdAt,locale)}</p><DownloadButton selection={selection} id={d.id} name={d.fileName}/></>}/></div>;
}

// The authorized file arrives as a blob and is handed to the browser once; nothing is cached or stored.
function DownloadButton({selection,id,name}:{selection:Selection;id:string;name:string}){
 const t=useTranslations("myPortfolio"),api=useAuthorizedApi();const [busy,setBusy]=useState(false);
 return <Button size="sm" variant="view" className="min-h-11" disabled={busy} onClick={()=>{setBusy(true);void api<Blob>(downloadPath(selection,id),{},"blob").then(blob=>{const url=URL.createObjectURL(blob);const a=document.createElement("a");a.href=url;a.download=name;a.click();URL.revokeObjectURL(url);}).catch(()=>{}).finally(()=>setBusy(false));}}>{t("download")}</Button>;
}
function RequestsSection({selection}:{selection:Selection}){
 const t=useTranslations("myPortfolio"),ui=useTranslations("ui"),locale=useLocale(),notice=useNotification();const [revision,setRevision]=useState(0),[category,setCategory]=useState("FAULT"),[priority,setPriority]=useState("NORMAL"),[description,setDescription]=useState(""),[recordId,setRecordId]=useState(()=>crypto.randomUUID());
 const decode=useCallback((v:unknown)=>requestRows(v,selection),[selection]);const rows=useCenterRecord(requestsPath(selection),decode,revision);
 const state=useCenterMutation(()=>{notice.success(t("requestSaved"));setDescription("");setRecordId(crypto.randomUUID());setRevision(n=>n+1);});
 const chips=requestCategories.map(c=>({value:c,label:t("category_"+c),icon:categoryStyle[c].icon}));
 return <div className="space-y-4"><form className="space-y-3 rounded-2xl border bg-card p-5" onSubmit={e=>{e.preventDefault();if(!description.trim())return;void state.submit("/api/v1/mobile/sync/commands","POST",{recordId,propertyId:selection.propertyId,unitId:selection.unitId,role:selection.capacity,action:"CREATE",expectedVersion:0,description:description.trim(),category,priority,photoAction:"KEEP",contextKind:selection.capacity,contextId:selection.relationId},v=>requestCreated(v,selection,recordId));}}>
  <h3 className="font-semibold">{t("newRequest")}</h3><fieldset disabled={state.busy||state.ambiguous} className="space-y-3 border-0 p-0"><FilterChips label={t("requestCategory")} items={chips} value={category} onChange={setCategory}/>
  <div><Label htmlFor="portal-priority">{t("requestPriority")}</Label><select id="portal-priority" className={toolbarSelectClass+" w-full"} value={priority} onChange={e=>setPriority(e.target.value)}>{requestPriorities.map(p=><option key={p} value={p}>{t("priority_"+p)}</option>)}</select></div>
  <div><Label htmlFor="portal-description">{t("requestDescription")} *</Label><textarea id="portal-description" required maxLength={2000} rows={4} className="w-full rounded-lg border bg-card p-3 text-sm" value={description} onChange={e=>setDescription(e.target.value)}/></div></fieldset>
  {state.error&&<MessageBanner>{state.error}</MessageBanner>}{state.ambiguous&&<MessageBanner kind="warning">{t("requestAmbiguous")}</MessageBanner>}<Button type="submit" disabled={state.busy||!description.trim()}>{t("sendRequest")}</Button></form>
  {rows.error?<MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{t("refresh")}</Button>}>{rows.error}</MessageBanner>:!rows.value?<p role="status">{ui("loading")}</p>:rows.value.length===0?<MessageBanner kind="info">{t("noRequests")}</MessageBanner>:
  <ul className="space-y-3">{rows.value.map(r=>{const Icon=categoryStyle[r.category as keyof typeof categoryStyle].icon;return <li key={r.id} className="rounded-xl border bg-card p-4"><p className="flex items-center gap-2 font-semibold"><Icon aria-hidden="true" className="size-4"/>{t("category_"+r.category)}</p><p className="mt-1 text-xs text-muted-foreground">{formatDateTime(r.modifiedAt,locale)} · {t("priority_"+r.priority)}</p><p className="mt-2 whitespace-pre-line text-sm">{r.description}</p></li>;})}</ul>}</div>;
}
function FinanceOverview({fingerprint}:{fingerprint:string}){
 const t=useTranslations("myPortfolio"),locale=useLocale();const data=useCenterRecord("/api/v2/me/finance-summary?v="+fingerprint.slice(0,12),financeSummary);const v=data.value;
 if(data.error||!v)return null;const money=(x:string)=>formatDecimalCurrency(x,locale);
 return <section aria-label={t("financeOverview")} className="rounded-2xl border bg-card p-5"><h2 className="font-semibold">{t("financeOverview")}</h2><p className="mt-1 text-sm text-muted-foreground">{t("financeScope",{units:v.scope.map(x=>`${x.propertyName} ${x.unitNumber} (${t("role_"+x.capacity)})`).join(", ")})}</p>
  <dl className="mt-3 grid gap-3 sm:grid-cols-4">{(["amount","paid","balance","overdue"] as const).map(k=><DetailField key={k} label={t("summary_"+k)}>{money(v.summary[k])}</DetailField>)}</dl>{v.sharedCharges>0&&<p className="mt-2 text-sm text-muted-foreground">{t("sharedCharges",{count:v.sharedCharges})}</p>}</section>;
}
