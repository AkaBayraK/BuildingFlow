"use client";
import Link from "next/link";
import {useCallback,useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Activity,Clock3,ShieldCheck} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi} from "@/services/api";
import {Dialog,DialogContent,DialogDescription,DialogHeader,DialogTitle,DialogTrigger} from "@/components/ui/dialog";
import {cn} from "@/lib/utils";
import {parseSystemStatus,statusDot,systemSettingsHref,systemStatusPath,systemStatusRoles,type SystemStatus} from "./model";

// Durum sorgusu her dakika tekrarlanır; beklenen hata kodları sağ üst bildirim üretmez, göstergede "doğrulanamadı" görünür.
const quiet=[401,403,404,429,500,502,503,504] as const;

// TraceFlow ApplicationFooter karşılığı: telif/sürüm, genel sistem durumu, bileşen çipleri, son kontrol ve gizlilik.
export function StatusFooter(){
 const t=useTranslations("systemStatus"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const canRead=systemStatusRoles.some(role=>identity.hasRealmRole(role));
 const [status,setStatus]=useState<SystemStatus|null>(null),[unavailable,setUnavailable]=useState(false);
 const load=useCallback(async(signal?:AbortSignal)=>{
  try{const value=parseSystemStatus(await api<unknown>(systemStatusPath,{signal},"json",quiet));if(!signal?.aborted){setStatus(value);setUnavailable(false);}}
  catch{if(!signal?.aborted){setStatus(null);setUnavailable(true);}}
 },[api]);
 useEffect(()=>{if(!canRead)return;const c=new AbortController();void load(c.signal);const timer=window.setInterval(()=>{void load();},60_000);return()=>{c.abort();window.clearInterval(timer);};},[canRead,load]);
 const overall=status?.overallStatus??"UNKNOWN";
 const overallLabel=unavailable?t("footerUnavailable"):t(`overall_${overall}`);
 const lastChecked=status?.lastCheckedAt?new Intl.DateTimeFormat(locale,{dateStyle:"short",timeStyle:"medium"}).format(new Date(status.lastCheckedAt)):"";
 const separator=<span className="h-4 w-px shrink-0 bg-border" aria-hidden="true"/>;
 const action="flex shrink-0 items-center gap-1.5 rounded-md px-1.5 py-1 font-medium text-foreground transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none";
 return <div className="flex h-full min-w-0 flex-1 items-center gap-3 overflow-hidden text-xs text-muted-foreground">
  <div className="flex shrink-0 items-center gap-2 whitespace-nowrap text-foreground">
   <span>{t("footerCopyright",{year:new Date().getFullYear()})}</span>
   {status?.version&&status.version!=="—"?<>{separator}<span className="hidden sm:inline">v{status.version}</span></>:null}
   {separator}<span className="font-semibold tracking-wide text-primary">AkaBayraK</span>
  </div>
  {canRead?<>
   {separator}
   <Link href={systemSettingsHref} className={action} aria-label={t("footerOpen")} title={t("footerOpen")}>
    <Activity className="size-3.5 text-primary" aria-hidden="true"/><span className="hidden sm:inline">{t("footerLabel")}</span>
    <span className={cn("size-2 rounded-full",statusDot[overall])} aria-hidden="true"/><span>{overallLabel}</span>
   </Link>
   {status?<div className="hidden min-w-0 items-center gap-1.5 overflow-hidden lg:flex" aria-label={t("footerComponents")}>
    {status.components.slice(0,6).map(c=><span key={c.id} className="flex max-w-36 shrink-0 items-center gap-1 rounded-md border border-border/80 bg-muted/50 px-2 py-1" title={`${c.name}: ${t(`status_${c.status}`)}`}><span className={cn("size-1.5 shrink-0 rounded-full",statusDot[c.status])} aria-hidden="true"/><span className="truncate">{c.name}</span></span>)}
   </div>:null}
  </>:null}
  <div className="ml-auto flex shrink-0 items-center gap-2">
   {lastChecked?<span className="hidden items-center gap-1 whitespace-nowrap xl:flex"><Clock3 className="size-3.5" aria-hidden="true"/>{t("footerLastChecked",{time:lastChecked})}</span>:null}
   <Dialog><DialogTrigger asChild><button type="button" className={action} aria-label={t("footerPrivacyOpen")} title={t("footerPrivacyOpen")}><ShieldCheck className="size-3.5" aria-hidden="true"/><span className="hidden sm:inline">{t("footerPrivacy")}</span></button></DialogTrigger>
    <DialogContent className="sm:max-w-md"><DialogHeader><DialogTitle>{t("footerPrivacy")}</DialogTitle><DialogDescription>{t("footerPrivacyDescription")}</DialogDescription></DialogHeader></DialogContent>
   </Dialog>
  </div>
 </div>;
}
