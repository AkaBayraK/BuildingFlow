"use client";
import {useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {History,ShieldCheck} from "lucide-react";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {formatDateTime} from "@/lib/format";
import {ConsentStateBadge,HistoryDialog} from "./ConsentsWorkspace";
import {readHistory,typeName,type ConsentHistory} from "./model";

// T17.5: consent of the login account linked to this person. The centre sees state and history; the person only sees state on mobile.
export function PersonConsents({userId}:{userId:string|null|undefined}){
 const t=useTranslations("consents"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const [data,setData]=useState<ConsentHistory|null>(null),[error,setError]=useState(""),[hidden,setHidden]=useState(false),[retry,setRetry]=useState(0),[open,setOpen]=useState(false);
 useEffect(()=>{if(!userId)return;const c=new AbortController();setData(null);setError("");setHidden(false);
  void api<unknown>(`/api/v1/consents/users/${encodeURIComponent(userId)}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readHistory(v,userId));})
   .catch(e=>{if(c.signal.aborted)return;if(e instanceof ApiError&&e.status===404)setHidden(true);else setError(errorMessage(e,locale));});
  return()=>c.abort();},[api,userId,retry,locale]);
 return <section aria-label={t("personSection")} className="mt-6 rounded-2xl border bg-card p-5 shadow-sm">
  <div className="flex flex-wrap items-center justify-between gap-3"><h2 className="flex items-center gap-2 text-base font-semibold"><ShieldCheck className="size-4"/>{t("personSection")}</h2>
   {userId&&data&&<Button variant="view" size="sm" onClick={()=>setOpen(true)}><History/>{t("history")}</Button>}</div>
  {!userId?<p className="mt-3 text-sm text-muted-foreground">{t("personNoAccount")}</p>
   :hidden?<p className="mt-3 text-sm text-muted-foreground">{t("personOutOfScope")}</p>
   :error?<div className="mt-3"><MessageBanner presentation="inline" action={<Button variant="view" onClick={()=>setRetry(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner></div>
   :!data?<p className="mt-3 text-sm" role="status">{ui("loading")}</p>
   :<ul className="mt-3 grid gap-2 sm:grid-cols-2">{data.summary.map(s=><li key={s.typeId} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border p-3">
     <span className="flex min-w-0 flex-col"><span className="font-medium">{typeName(s,locale)}</span><small className="text-muted-foreground">{s.approvedVersion&&s.approvedAt?`${t("versionValue",{version:s.approvedVersion})} · ${formatDateTime(s.approvedAt,locale)}`:s.currentVersion?t("currentVersionOnly",{current:s.currentVersion}):t("noText")}</small></span>
     <ConsentStateBadge state={s.state}/></li>)}</ul>}
  {open&&userId&&<HistoryDialog userId={userId} onClose={()=>setOpen(false)}/>}
 </section>;
}
