"use client";
import {formatDate} from "@/lib/format";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useLocale,useTranslations} from "next-intl";
import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Building2, ChevronLeft, MapPin, Pencil, Trash2, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useIdentity } from "@/auth/AuthGate";
import { errorMessage, type Property, propertyTypeNames, useAuthorizedApi } from "@/services/api";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import { EntityEditor, DeleteEntity } from "@/features/hierarchy/EntityEditor";
import { HierarchyRows, StatusBadge } from "@/features/hierarchy/HierarchyRows";

export function PropertyDetail({id}:{id:string}) {
  const t=useTranslations("ui"); const locale=useLocale();

  const scope=usePropertyScope();const api=useAuthorizedApi();const identity=useIdentity();const router=useRouter();
  const [site,setSite]=useState<Property|null>(null);const [error,setError]=useState("");const [loading,setLoading]=useState(true);const [revision,setRevision]=useState(0);
  const [edit,setEdit]=useState(false);const [deleting,setDeleting]=useState(false);const [notice,setNotice]=useState("");
  const canManage=identity.hasRealmRole("SUPER_ADMIN");
  useEffect(()=>{
    const controller=new AbortController();setLoading(true);setError("");
    void api<Property>(`/api/v1/properties/${encodeURIComponent(id)}`,{signal:controller.signal}).then(result=>{if(!controller.signal.aborted)setSite(result);}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});
    return()=>controller.abort();
  },[api,id,revision]);
  const current=site?.id===id?site:null;
  return <div className="py-10"><Button asChild variant="ghost" size="sm" className="mb-6 -ml-3 text-muted-foreground"><Link href="/properties"><ChevronLeft className="size-4"/>{t("allProperties")}</Link></Button>
    <div className="mb-8 flex flex-wrap items-center justify-between gap-5"><div className="flex min-w-0 items-center gap-4"><span className="grid size-14 shrink-0 place-items-center rounded-2xl border border-blue-100 bg-blue-50 text-primary"><Building2 className="size-7"/></span><div className="min-w-0"><span className="text-[11px] font-bold tracking-[.18em] text-primary">{t("managementAREA")}</span><h1 className="mt-2 mb-0 text-3xl font-bold tracking-tight wrap-anywhere">{current?.name ?? t("propertyDetails")}</h1></div></div>{current && canManage && <div className="flex gap-2"><Button variant="outline" disabled={loading || !!error} onClick={()=>setEdit(true)}><Pencil className="size-4"/>{t("editProperty")}</Button><Button variant="outline" disabled={loading || !!error} aria-label={t("deleteProperty")} className="text-slate-500 hover:text-destructive" onClick={()=>setDeleting(true)}><Trash2 className="size-4"/></Button></div>}</div>
    {notice && <MessageBanner className="my-4 text-left" kind="success" announce={false}>{notice}</MessageBanner>}
    <section className="overflow-hidden rounded-2xl border bg-white shadow-sm"><div className="flex items-center justify-between border-b px-7 py-5"><h2 className="m-0 text-base font-semibold">{t("generalInformation")}</h2>{current && <div className="flex gap-2"><span className="rounded-lg bg-blue-50 px-3 py-1 text-xs font-semibold text-primary">{t(propertyTypeNames[current.type])}</span><StatusBadge enabled={current.isEnabled}/></div>}</div>
      {error ? <div className="p-10 text-center"><MessageBanner className="my-4 text-left">{error}</MessageBanner><Button variant="outline" onClick={()=>setRevision(value=>value+1)}>{t("tryAgain")}</Button></div> : !current ? <div role="status" className="grid grid-cols-2 gap-6 p-7"><span className="sr-only">{t("loadingPropertyDetails")}</span>{Array.from({length:6},(_,i)=><Skeleton key={i} className="h-12"/>)}</div> : <dl className="grid gap-6 p-7 sm:grid-cols-3"><div><dt className="mb-2 text-xs text-muted-foreground">{t("cityDistrict")}</dt><dd className="m-0 text-sm font-semibold text-slate-700 wrap-anywhere">{current.city} / {current.district}</dd></div><div><dt className="mb-2 text-xs text-muted-foreground">{t("structureSummary")}</dt><dd className="m-0 text-sm font-semibold text-slate-700">{current.type==="APARTMENT"?t("apartmentSummary",{floors:current.floorCount ?? "—",units:current.unitCount}):t("buildingSummary",{buildings:current.buildingCount,units:current.unitCount})}</dd></div><div><dt className="mb-2 text-xs text-muted-foreground">{t("createdOn")}</dt><dd className="m-0 text-sm font-semibold text-slate-700">{formatDate(current.createdAt,locale)}</dd></div><div className="sm:col-span-1"><dt className="mb-2 flex items-center gap-1 text-xs text-muted-foreground"><MapPin className="size-3"/>{t("address")}</dt><dd className="m-0 text-sm leading-6 text-slate-700 wrap-anywhere">{current.address}</dd></div></dl>}
    </section>
    {edit && current && <EntityEditor kind="property" url="/api/v1/properties" target={current} onClose={()=>setEdit(false)} onSaved={()=>{setNotice(t("propertyDetailsUpdated"));setRevision(value=>value+1);scope.refresh();}}/>}
    {deleting && current && <DeleteEntity kind="property" url="/api/v1/properties" target={current} onClose={()=>setDeleting(false)} onDeleted={()=>router.push("/properties")}/>}
  </div>;
}
