"use client";
import {PersonProfileDetails} from "@/features/people/PersonProfileDetails";
import {ManagementPanel} from "@/components/management/ManagementPanel";

import {useCallback,useEffect,useState} from "react";
import Link from "next/link";
import {unitListHref} from "@/features/people/unit-links";
import {useLocale,useTranslations} from "next-intl";
import {DoorOpen,Users,Pencil,ArrowUpRight,ChevronLeft,ChevronRight} from "lucide-react";
import {Dialog,DialogHeader,DialogTitle,DialogDescription} from "@/components/ui/dialog";
import {Button} from "@/components/ui/button";
import {Badge} from "@/components/ui/badge";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {DetailField,IdentityMark,StatusBadge,RoleBadge} from "@/components/management/WorkspacePresentation";
import {errorMessage,useAuthorizedApi,unitTypeNames,type Unit} from "@/services/api";
import {parseUnit,parsePerson,parseOccupancyPage} from "@/services/management-contracts";
import {personName,occupancyTypeNames,occupancyStatusNames,type Person} from "@/features/people/types";
import {useRows} from "@/features/people/useRows";
import {formatDate,formatDateOnly,formatNumber} from "@/lib/format";

import {unitDetailFields} from "@/features/property-details/model";
import {DetailValues} from "@/features/property-details/DetailFields";
type DetailsProps={propertyId:string;onClose:()=>void;canEdit:boolean};
type Props=DetailsProps&({kind:"unit";url:string;target:Unit;onEdit:(value:Unit)=>void}|{kind:"person";url:string;target:Person;onEdit:(value:Person)=>void});
export function RecordDetails(props:Props){
  const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const api=useAuthorizedApi();
  const [record,setRecord]=useState<Unit|Person|null>(null);const [error,setError]=useState("");const [revision,setRevision]=useState(0);
  const {kind,url,target,propertyId,onClose,canEdit}=props;
  useEffect(()=>{
    const controller=new AbortController();setRecord(null);setError("");
    void api<unknown>(`${url}/${encodeURIComponent(target.id)}`,{signal:controller.signal}).then(value=>{
      const latest=kind==="unit"?parseUnit(value,propertyId,(target as Unit).buildingId):parsePerson(value,propertyId);
      if(latest.id!==target.id)throw new Error("Invalid record identity");
      if(!controller.signal.aborted)setRecord(latest);
    }).catch(reason=>{if(!controller.signal.aborted)setError(errorMessage(reason,locale));});
    return()=>controller.abort();
  },[api,url,kind,target.id,kind==="unit"?(target as Unit).buildingId:null,propertyId,revision,locale]);
  const name=record?("number" in record?record.number:personName(record)):("number" in target?target.number:personName(target));
  return <Dialog open onOpenChange={open=>{if(!open)onClose();}}><ManagementPanel>
    <DialogHeader className="text-left"><p className="mb-2 text-xs font-semibold text-primary">{kind==="unit"?t("unit"):t("person")}</p><DialogTitle className="text-2xl font-bold wrap-anywhere">{m("recordDetails",{name})}</DialogTitle><DialogDescription>{m("detailsDescription")}</DialogDescription></DialogHeader>
    <div>
      {error?<MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{t("tryAgain")}</Button>}>{error}</MessageBanner>:!record?<div role="status" className="space-y-4"><span className="sr-only">{t("loading")}</span><Skeleton className="h-24"/><Skeleton className="h-40"/></div>:<>
        <div className="mb-3 flex items-center gap-3"><IdentityMark icon={kind==="unit"?DoorOpen:Users} className="size-12"/><div className="min-w-0 flex-1"><strong className="block wrap-anywhere">{name}</strong><div className="mt-2"><StatusBadge enabled={record.isEnabled}/></div></div>{canEdit&&<Button variant="edit" size="sm" onClick={()=>{if(props.kind==="unit"&&"number" in record)props.onEdit(record);else if(props.kind==="person"&&"firstName" in record)props.onEdit(record);}}><Pencil className="size-4"/>{m("edit")}</Button>}</div>
        <dl className="grid grid-cols-2 gap-x-5">
          {"number" in record?<><DetailField label={t("unitNumber")}>{record.number}</DetailField><DetailField label={t("type")}>{t(unitTypeNames[record.type])}</DetailField><DetailField label={t("floor")}>{formatNumber(record.floor,locale)}</DetailField><DetailField label={t("grossArea")}>{formatNumber(record.grossSquareMeters,locale)} m²</DetailField><DetailValues fields={unitDetailFields} record={record}/></>:<><DetailField label={t("phone")}>{record.phoneNumber??t("noPhone")}</DetailField><DetailField label={t("email")}>{record.email??t("noEmail")}</DetailField><PersonProfileDetails person={record}/><DetailField label={m("ongoingRelationships")}>{formatNumber(record.activeOccupancyCount,locale)}</DetailField></>}
          <DetailField label={m("createdAt")}>{formatDate(record.createdAt,locale)}</DetailField>
        </dl>
        {"number" in record?<p className="mt-5 rounded-xl bg-muted p-4 text-xs leading-6 text-muted-foreground">{m("unitStatusExplanation")}</p>:<PersonRelationships propertyId={propertyId} personId={record.id}/>}
      </>}
    </div>
  </ManagementPanel></Dialog>;
}

function PersonRelationships({propertyId,personId}:{propertyId:string;personId:string}){
  const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);
  const decode=useCallback((value:unknown)=>parseOccupancyPage(value,propertyId,personId),[propertyId,personId]);
  const rows=useRows(`/api/v1/properties/${encodeURIComponent(propertyId)}/occupancies?${new URLSearchParams({personId,page:String(page),pageSize:"5"})}`,revision,true,decode);
  const pages=Math.max(1,Math.ceil((rows.data?.totalCount??0)/5));
  return <section className="mt-6" aria-label={t("unitRelationships")}><div className="mb-4 flex flex-wrap items-center justify-between gap-3"><h2 className="m-0 text-base font-semibold">{t("unitRelationships")}</h2><Button asChild size="sm" variant="view"><Link href={`/properties/${propertyId}/people/${personId}`}><ArrowUpRight className="size-4"/>{m("manageRelationships")}</Link></Button></div>
    {rows.loading?<p role="status" className="text-sm text-muted-foreground">{t("loading")}</p>:rows.error?<MessageBanner action={<Button variant="view" size="sm" onClick={()=>setRevision(n=>n+1)}>{t("tryAgain")}</Button>}>{rows.error}</MessageBanner>:!rows.data?.items.length?<p className="rounded-xl border border-dashed p-5 text-sm text-muted-foreground">{t("thisPersonHasNoUnitRelationshipsYet")}</p>:<div className="space-y-3">{rows.data.items.map(item=><div key={item.id} className="rounded-xl border p-4"><div className="flex flex-wrap items-center justify-between gap-2"><Link className="inline-flex min-h-11 items-center rounded-md text-sm font-semibold text-primary hover:underline wrap-anywhere" href={unitListHref(item)} aria-label={t("openUnitList",{unit:item.buildingName?`${item.buildingName} · ${item.unitNumber}`:item.unitNumber})}>{item.buildingName?`${item.buildingName} · `:""}{item.unitNumber}</Link><RoleBadge role={item.type} label={t(occupancyTypeNames[item.type])}/></div><p className="mt-2 text-xs text-muted-foreground">{formatDateOnly(item.startDate,locale)} — {item.endDate?formatDateOnly(item.endDate,locale):t("ongoing")}</p><p className="mt-2 flex flex-wrap gap-2 text-xs"><span>{t(occupancyStatusNames[item.status])}</span>{item.isPrimaryContact&&<span className="text-primary">{t("primaryContact")}</span>}</p></div>)}</div>}
    {pages>1&&<div className="mt-4 flex items-center justify-end gap-3"><span className="text-xs text-muted-foreground">{page} / {pages}</span><Button size="icon-sm" variant="outline" aria-label={t("previousPage")} disabled={rows.loading||page===1} onClick={()=>setPage(n=>n-1)}><ChevronLeft/></Button><Button size="icon-sm" variant="outline" aria-label={t("nextPage")} disabled={rows.loading||!!rows.error||page>=pages} onClick={()=>setPage(n=>n+1)}><ChevronRight/></Button></div>}
    <p className="mt-4 text-xs leading-6 text-muted-foreground">{m("multipleRolesExplanation")}</p>
  </section>;
}
