"use client";
import {useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {KeyRound,Unlink} from "lucide-react";
import {useNotification} from "@/components/feedback/Notifications";
import {Dialog,DialogContent,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {DetailField} from "@/components/management/WorkspacePresentation";
import {formatDateTime} from "@/lib/format";
import {useCenterMutation,useCenterRecord} from "./hooks";
import {account,linkCode,personRoot,type LinkCode} from "./model";
// The plain code is shown once from the POST response and kept only in this component's memory.
export function AccountLink({tenant,person,revision,canManage}:{tenant:string;person:string;revision:number;canManage:boolean}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui"),locale=useLocale(),notice=useNotification();const [local,setLocal]=useState(0),[issued,setIssued]=useState<LinkCode|null>(null),[confirm,setConfirm]=useState(false);
 const value=useCenterRecord(personRoot(tenant,person)+"/account-status",account,revision+local);const refresh=()=>setLocal(n=>n+1);
 const issue=useCenterMutation(refresh),end=useCenterMutation(()=>{notice.success(t("linkEnded"));setConfirm(false);setIssued(null);refresh();});
 const status=value.value;
 return <section className="rounded-2xl border bg-card p-5"><h2>{t("account")}</h2><p className="my-4 text-sm text-muted-foreground">{t("accountHelp")}</p>
  {value.error?<MessageBanner>{value.error}</MessageBanner>:!status?<p role="status">{ui("loading")}</p>:<dl className="grid gap-4 sm:grid-cols-2"><DetailField label={t("accountLink")}>{t(status.isLinked?"linked":"unlinked")}</DetailField>{status.linkedAt&&<DetailField label={t("linkedAt")}>{formatDateTime(status.linkedAt,locale)}</DetailField>}{!issued&&status.codeHint&&status.codeExpiresAt&&<DetailField label={t("openCode")}>{t("codeHint",{hint:status.codeHint,expires:formatDateTime(status.codeExpiresAt,locale)})}</DetailField>}</dl>}
  {issued&&<div role="status" className="my-4 rounded-xl border border-(--message-info-border) bg-(--message-info-bg) p-4 text-(--message-info-text)"><p className="text-sm">{t("codeOnce",{expires:formatDateTime(issued.expiresAt,locale)})}</p><p className="mt-2 font-mono text-3xl tracking-[0.3em]" aria-label={t("linkCode")}>{issued.code}</p></div>}
  {(issue.error||end.error)&&<MessageBanner>{issue.error||end.error}</MessageBanner>}
  {canManage&&status&&<div className="mt-4 flex flex-wrap gap-2">{!status.isLinked&&<Button disabled={issue.busy} onClick={()=>void issue.submit(personRoot(tenant,person)+"/account-link-codes","POST",{},v=>{const code=linkCode(v);setIssued(code);return code;})}><KeyRound/>{t(status.codeHint?"regenerateCode":"issueCode")}</Button>}{status.isLinked&&<Button variant="warn" onClick={()=>{end.reset();setConfirm(true);}}><Unlink/>{t("endLink")}</Button>}</div>}
  {confirm&&<Dialog open onOpenChange={v=>{if(!v&&!end.busy)setConfirm(false);}}><DialogContent><DialogHeader><DialogTitle>{t("endLink")}</DialogTitle><DialogDescription>{t("endLinkHelp")}</DialogDescription></DialogHeader>{end.error&&<MessageBanner>{end.error}</MessageBanner>}<DialogFooter><Button variant="outline" disabled={end.busy} onClick={()=>setConfirm(false)}>{ui("cancel")}</Button><Button variant="destructive" disabled={end.busy} onClick={()=>void end.submit(personRoot(tenant,person)+"/account-link/end","POST",{},account)}>{t("endLink")}</Button></DialogFooter></DialogContent></Dialog>}</section>;
}
