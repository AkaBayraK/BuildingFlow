"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {BadgeCheck,Ban,CreditCard,LoaderCircle,PlugZap,Power,PowerOff,RefreshCw,Undo2} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {ApiError,errorMessage,useAuthorizedApi} from "@/services/api";
import {Button} from "@/components/ui/button";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/FeedbackContext";
import {readProviders,type PaymentProvider} from "./model";

// Yönetim > Sistem ayarları: ödeme sağlayıcı kataloğu yalnız SUPER_ADMIN içindir.
export function ProvidersPanel(){
 const t=useTranslations("onlinePayments");const ui=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const notice=useNotification();const identity=useIdentity();
 const allowed=identity.hasRealmRole("SUPER_ADMIN");
 const [providers,setProviders]=useState<PaymentProvider[]|null>(null);const [error,setError]=useState("");const [revision,setRevision]=useState(0);const [busy,setBusy]=useState<string|null>(null);const lock=useRef(false);
 useEffect(()=>{if(!allowed)return;const controller=new AbortController();setError("");void api<unknown>("/api/v1/payment-providers",{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setProviders(readProviders(value));}).catch(err=>{if(!controller.signal.aborted)setError(errorMessage(err,locale));});return()=>controller.abort();},[api,allowed,locale,revision]);
 async function toggle(provider:PaymentProvider){
  if(lock.current)return;lock.current=true;setBusy(provider.code);
  try{await api<unknown>("/api/v1/payment-providers/"+encodeURIComponent(provider.code),{method:"PUT",body:JSON.stringify({enabled:!provider.enabled,version:provider.version})});notice.success(t(provider.enabled?"providerDisabled":"providerEnabled",{name:provider.name}));setRevision(n=>n+1);}
  catch(err){notice.error(errorMessage(err,locale));if(err instanceof ApiError&&err.status===409)setRevision(n=>n+1);}
  finally{lock.current=false;setBusy(null);}
 }
 if(!allowed)return null;
 return <section className="mt-6 min-w-0 rounded-2xl border bg-card p-5 shadow-sm" aria-labelledby="payment-providers-title">
  <div className="mb-3 flex flex-wrap items-center justify-between gap-2"><h2 id="payment-providers-title" className="flex items-center gap-2 font-semibold"><CreditCard className="size-5 text-primary"/>{t("providers")}</h2><Button variant="outline" size="sm" onClick={()=>setRevision(n=>n+1)}><RefreshCw/>{ui("refresh")}</Button></div>
  <p className="mb-4 text-xs text-muted-foreground">{t("providersHelp")}</p>
  {error&&<MessageBanner>{error}</MessageBanner>}
  {!providers&&!error&&<p role="status">{ui("loading")}</p>}
  {providers&&(providers.length?<ul className="grid gap-3 md:grid-cols-2">{providers.map(provider=><li key={provider.code} className="space-y-3 rounded-xl border p-4">
   <div className="flex flex-wrap items-center justify-between gap-2"><strong>{provider.name}</strong><span className="font-mono text-xs text-muted-foreground">{provider.code}</span></div>
   <div className="flex flex-wrap gap-2"><ToneBadge tone={provider.enabled?"success":"neutral"} icon={provider.enabled?BadgeCheck:Ban}>{ui(provider.enabled?"active":"inactive")}</ToneBadge><ToneBadge tone={provider.integrated?"info":"warning"} icon={PlugZap}>{t(provider.integrated?"integrated":"notIntegrated")}</ToneBadge><ToneBadge tone="info" icon={PlugZap}>{t("environment_"+(provider.environment==="SANDBOX"?"SANDBOX":"OTHER"))}</ToneBadge>{provider.supportsRefund&&<ToneBadge tone="violet" icon={Undo2}>{t("supportsRefund")}</ToneBadge>}</div>
   <Button size="sm" variant={provider.enabled?"warn":"view"} disabled={!provider.integrated||!!busy} onClick={()=>void toggle(provider)}>{busy===provider.code?<LoaderCircle className="animate-spin"/>:provider.enabled?<PowerOff/>:<Power/>}{t(provider.enabled?"disableProvider":"enableProvider")}</Button>
  </li>)}</ul>:<p className="text-sm text-muted-foreground">{t("noProviders")}</p>)}
 </section>;
}
