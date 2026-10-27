"use client";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useLocale,useTranslations} from "next-intl";
import { useEffect, useState } from "react";
import { Building2, DoorOpen, Pencil, Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import {BackButton} from "@/components/navigation/BackButton";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useIdentity } from "@/auth/AuthGate";
import { errorMessage, useAuthorizedApi, type Building, type Property } from "@/services/api";
import { HierarchyRows, StatusBadge } from "./HierarchyRows";
import { DeleteEntity, EntityEditor } from "./EntityEditor";
import {buildingDetailFields} from "@/features/property-details/model";
import {DetailValues} from "@/features/property-details/DetailFields";
import {parseProperty,parseBuilding} from "@/services/management-contracts";
export function BuildingDetail({propertyId,buildingId}:{propertyId:string;buildingId:string}) {
  const t=useTranslations("ui"); const locale=useLocale();

  const api=useAuthorizedApi();const router=useRouter();const identity=useIdentity();const canWrite=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
  const [context,setContext]=useState<{site:Property;block:Building}|null>(null);const [error,setError]=useState("");const [loading,setLoading]=useState(true);const [revision,setRevision]=useState(0);const [edit,setEdit]=useState(false);const [deleting,setDeleting]=useState(false);
  const url=`/api/v1/properties/${encodeURIComponent(propertyId)}/buildings`;
  useEffect(()=>{const controller=new AbortController();setLoading(true);setError("");
    void Promise.all([api<Property>(`/api/v1/properties/${encodeURIComponent(propertyId)}`,{signal:controller.signal}),api<Building>(`${url}/${encodeURIComponent(buildingId)}`,{signal:controller.signal})]).then(([rawSite,rawBlock])=>{const site=parseProperty(rawSite,propertyId);const block=parseBuilding(rawBlock,propertyId);if(block.id!==buildingId)throw new Error("Invalid building identity");if(!controller.signal.aborted)setContext({site,block});}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[api,propertyId,buildingId,url,revision]);
  const current=context?.site.id===propertyId && context?.block.id===buildingId?context:null;
  return <div className="py-10"><BackButton className="mb-6" href={`/properties/${propertyId}/buildings`} label={current?.site.name ?? t("backToProperty")}/>
    <div className="mb-8 flex flex-wrap items-center justify-between gap-5"><div className="flex min-w-0 items-center gap-4"><span className="grid size-14 shrink-0 place-items-center rounded-2xl border border-blue-100 bg-blue-50 text-primary"><Building2 className="size-7"/></span><div className="min-w-0"><span className="text-[11px] font-bold tracking-[.18em] text-primary">{t("buildingBLOCKMANAGEMENT")}</span><h1 className="mt-2 mb-0 text-3xl font-bold tracking-tight wrap-anywhere">{current?.block.name ?? t("buildingDetails")}</h1></div></div>{current && canWrite && <div className="flex gap-2"><Button variant="edit" disabled={loading || !!error || !current.site.isEnabled} onClick={()=>setEdit(true)}><Pencil className="size-4"/>{t("editBuildingBlock")}</Button><Button variant="danger" disabled={loading || !!error || !current.site.isEnabled} aria-label={t("deleteBuildingBlock")} className="text-slate-500 hover:text-destructive" onClick={()=>setDeleting(true)}><Trash2 className="size-4"/></Button></div>}</div>
    {error?<div className="rounded-2xl border bg-white p-10 text-center"><MessageBanner className="my-4 text-left">{error}</MessageBanner><Button variant="view" onClick={()=>setRevision(value=>value+1)}>{t("tryAgain")}</Button></div>:!current?<div role="status" className="rounded-2xl border p-7"><span className="sr-only">{t("loadingBuilding")}</span><Skeleton className="h-20"/></div>:<><div className="grid gap-4 sm:grid-cols-2"><div className="flex items-center justify-between rounded-2xl border bg-white p-6"><div><p className="mb-2 text-xs text-muted-foreground">{t("buildingStatus")}</p><StatusBadge enabled={current.block.isEnabled}/></div><Building2 className="size-8 text-blue-200"/></div><div className="flex items-center justify-between rounded-2xl border bg-white p-6"><div><p className="mb-2 text-xs text-muted-foreground">{t("numberOfUnits")}</p><strong className="text-2xl text-slate-800">{current.block.unitCount}</strong></div><DoorOpen className="size-8 text-blue-200"/></div></div><section className="mt-6 rounded-2xl border bg-card p-6"><h2 className="text-base font-semibold">{t("buildingDetailFeatures")}</h2><dl className="grid gap-x-6 sm:grid-cols-2 lg:grid-cols-4"><DetailValues fields={buildingDetailFields} record={current.block}/></dl></section><HierarchyRows key={`${propertyId}/${buildingId}`} kind="unit" propertyId={propertyId} buildingId={buildingId} parentEnabled={current.site.isEnabled && current.block.isEnabled} onChanged={()=>setRevision(value=>value+1)}/></>}
    {edit && current && <EntityEditor kind="building" url={url} target={current.block} onClose={()=>setEdit(false)} onSaved={()=>setRevision(value=>value+1)}/>}
    {deleting && current && <DeleteEntity kind="building" url={url} target={current.block} onClose={()=>setDeleting(false)} onDeleted={()=>router.push(`/properties/${propertyId}`)}/>}
  </div>;
}
