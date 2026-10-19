"use client";
import Link from "next/link";
import {useEffect,useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {ArrowLeft,Printer} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {DetailField} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {parseMarkup} from "./markup";
import {ConsentMarkupView} from "./ConsentMarkupView";
import {ConsentStateBadge} from "./ConsentsWorkspace";
import {readDocument,recordTypeName,type ConsentDocument} from "./model";

// Printable evidence of one approval: the exact approved text plus the stored hashes and their integrity check.
export function ConsentDocumentPage({recordId}:{recordId:string}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r));
 const [data,setData]=useState<ConsentDocument|null>(null),[error,setError]=useState(""),[retry,setRetry]=useState(0);
 useEffect(()=>{const c=new AbortController();setError("");if(manager)void api<unknown>(`/api/v1/consents/records/${recordId}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readDocument(v,recordId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));});return()=>c.abort();},[api,recordId,retry,locale,manager]);
 const parsed=useMemo(()=>data?parseMarkup(data.body):null,[data]);
 if(!manager)return <div className="py-10"><MessageBanner>{ui("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 const back=<Button asChild variant="outline"><Link href="/consents"><ArrowLeft/>{t("backToConsents")}</Link></Button>;
 if(error)return <div className="space-y-4 py-8">{back}<MessageBanner action={<Button variant="view" onClick={()=>setRetry(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner></div>;
 if(!data||!parsed)return <p role="status" className="py-8">{ui("loading")}</p>;
 const r=data.record,intact=data.bodyIntact&&data.evidenceIntact;
 return <div className="consent-document space-y-6 py-8">
  <div className="print-hidden flex flex-wrap items-center justify-between gap-2">{back}<Button onClick={()=>window.print()}><Printer/>{t("print")}</Button></div>
  <div className="consent-sheet space-y-6 rounded-2xl border bg-card p-5 shadow-sm sm:p-8">
   <div className="space-y-2 border-b pb-4"><p className="text-xs uppercase tracking-wide text-muted-foreground">{t("documentTitle")}</p><h1 className="break-words text-2xl font-bold">{data.title}</h1><div className="flex flex-wrap items-center gap-2 text-sm"><span>{recordTypeName(r,locale)} · {t("versionValue",{version:r.textVersion})}</span><ConsentStateBadge state={r.state}/></div></div>
   {!intact&&<MessageBanner presentation="inline" title={t("integrityFailedTitle")}>{t(!data.bodyIntact?"integrityBodyFailed":"integrityEvidenceFailed")}</MessageBanner>}
   <dl className="grid gap-4 sm:grid-cols-2">
    <DetailField label={t("approver")}>{r.approverName}</DetailField>
    <DetailField label={t("userId")}><span className="break-all font-mono text-xs">{r.ownerUserId}</span></DetailField>
    <DetailField label={t("approvedAt")}>{formatDateTime(r.approvedAt,locale)}</DetailField>
    <DetailField label={t("channel")}>{t(`channel_${r.channel}`)}{r.appVersion?` · ${r.appVersion}`:""}</DetailField>
    {r.revokedAt&&<DetailField label={t(r.revokeReason==="WITHDRAWN"?"withdrawnAt":"supersededAt")}>{formatDateTime(r.revokedAt,locale)}</DetailField>}
    <DetailField label={t("recordId")}><span className="break-all font-mono text-xs">{r.id}</span></DetailField>
   </dl>
   <div className="border-t pt-4">{parsed.error?<MessageBanner presentation="inline">{t("renderFailed")}</MessageBanner>:<ConsentMarkupView blocks={parsed.blocks}/>}</div>
   <div className="space-y-3 border-t pt-4"><h2 className="text-sm font-semibold">{t("integrity")}</h2>
    <dl className="grid gap-4"><DetailField label={t("bodyHash")}><span className="break-all font-mono text-xs">{data.bodySha256}</span> · {t(data.bodyIntact?"intact":"notIntact")}</DetailField>
     <DetailField label={t("evidenceHash")}><span className="break-all font-mono text-xs">{data.evidenceSha256}</span> · {t(data.evidenceIntact?"intact":"notIntact")}</DetailField></dl>
    <p className="text-xs text-muted-foreground">{t("integrityHelp")}</p></div>
  </div>
 </div>;
}
