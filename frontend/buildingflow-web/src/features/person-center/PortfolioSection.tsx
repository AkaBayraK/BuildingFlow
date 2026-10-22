"use client";
import {useCallback} from "react";
import {useLocale,useTranslations} from "next-intl";
import {FileSignature} from "lucide-react";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateOnly} from "@/lib/format";
import {useCenterRecord} from "./hooks";
import {CapacityBadge,PortfolioBadge,VerificationBadge} from "./Badges";
import {personRoot,portfolio,type PortfolioUnit} from "./model";
import type {LeaseScope} from "./LeasePanel";
// Grouped per area → building → unit; a person with several capacities in one unit appears once with all badges.
export function PortfolioSection({tenant,person,revision,canWrite,onLease}:{tenant:string;person:string;revision:number;canWrite:boolean;onLease:(scope:LeaseScope)=>void}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui"),locale=useLocale();const decode=useCallback((v:unknown)=>portfolio(v,tenant,person),[tenant,person]);const data=useCenterRecord(personRoot(tenant,person)+"/portfolio",decode,revision);
 const period=(s:string,e:string|null)=>`${formatDateOnly(s,locale)} – ${e?formatDateOnly(e,locale):t("openEnded")}`;
 const unit=(propertyName:string,propertyId:string,building:string|null,u:PortfolioUnit)=><li key={u.unitId} className="rounded-xl border p-3"><div className="flex flex-wrap items-center justify-between gap-2"><p className="font-medium">{[building,u.unitNumber].filter(Boolean).join(" · ")}</p><PortfolioBadge value={u.status}/></div>
  <div className="mt-2 flex flex-wrap gap-2">{u.capacities.map(c=><CapacityBadge key={c} value={c}/>)}</div>
  <ul className="mt-2 space-y-1 text-sm">{u.relations.map(r=><li key={r.relationId} className="flex flex-wrap items-center gap-2">{t(r.kind)} · {period(r.startDate,r.endDate)}<VerificationBadge value={r.verificationStatus}/></li>)}</ul>
  {u.activeLease&&<p className="mt-2 text-sm text-muted-foreground">{t("activeLease")}: {period(u.activeLease.startDate,u.activeLease.endDate)}{u.activeLease.contractReference?` · ${u.activeLease.contractReference}`:""}</p>}
  {canWrite&&<Button className="mt-3 min-h-11" size="sm" variant="outline" onClick={()=>onLease({propertyId,unitId:u.unitId,label:[propertyName,building,u.unitNumber].filter(Boolean).join(" · ")})}><FileSignature/>{t("leases")}</Button>}</li>;
 return <section aria-label={t("portfolio")} className="rounded-2xl border bg-card p-4"><h2 className="mb-1">{t("portfolio")}</h2><p className="mb-4 text-sm text-muted-foreground">{t("portfolioHelp")}</p>
  {data.error?<MessageBanner>{data.error}</MessageBanner>:!data.value?<p role="status">{ui("loading")}</p>:data.value.properties.length===0?<p className="text-sm text-muted-foreground">{t("noPortfolio")}</p>:
  <div className="space-y-5">{data.value.properties.map(p=><div key={p.propertyId}><h3 className="mb-2 font-semibold">{p.propertyName}</h3><ul className="grid gap-3 md:grid-cols-2">{p.buildings.flatMap(b=>b.units.map(u=>unit(p.propertyName,p.propertyId,b.buildingName,u)))}</ul></div>)}</div>}</section>;
}
