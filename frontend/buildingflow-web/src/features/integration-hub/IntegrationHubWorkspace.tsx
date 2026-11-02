"use client";
import {useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {ExternalLink,History,Mail,Plug} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {WorkspaceHeading} from "@/components/management/WorkspacePresentation";
import {useTheme} from "@/features/theme/ThemeProvider";
import {integrationHubBase,integrationHubHref,integrationHubRoles,type IntegrationHubScreen} from "./hub-link";

const base=integrationHubBase(process.env.NEXT_PUBLIC_AKA_INTEGRATION_HUB_URL);
const screens:readonly {screen:IntegrationHubScreen;icon:typeof Mail}[]=[{screen:"smtp",icon:Mail},{screen:"deliveries",icon:History}];

// Yönetim > Entegrasyonlar: e-posta (SMTP) tanımları AkaIntegrationHub ekranında yapılır; aynı Keycloak oturumu kullanılır.
export function IntegrationHubWorkspace(){
 const t=useTranslations("integrationHub"),locale=useLocale(),identity=useIdentity(),{resolved}=useTheme();
 const allowed=integrationHubRoles.some(role=>identity.hasRealmRole(role));
 const [returnUrl,setReturnUrl]=useState("");
 useEffect(()=>{setReturnUrl(window.location.href);},[]);
 return <div className="space-y-6 py-8">
  <WorkspaceHeading title={t("title")} description={t("description")} scope={t("scope")} icon={Plug}/>
  {!allowed?<MessageBanner>{t("forbidden")}</MessageBanner>:!base?<MessageBanner>{t("notConfigured")}</MessageBanner>:
  <div className="grid gap-4 md:grid-cols-2">{screens.map(({screen,icon:Icon})=>{const href=returnUrl?integrationHubHref({base,screen,locale,theme:resolved,returnUrl}):null;return <section key={screen} className="flex flex-col gap-3 rounded-2xl border bg-card p-5 shadow-sm">
   <div className="flex items-center gap-3"><Icon aria-hidden="true" className="size-6 text-primary"/><h2 className="font-semibold">{t(`${screen}Title`)}</h2></div>
   <p className="text-sm text-muted-foreground">{t(`${screen}Description`)}</p>
   <div className="mt-auto">{href?<Button asChild><a href={href}><ExternalLink/>{t("open")}</a></Button>:<Button disabled><ExternalLink/>{t("open")}</Button>}</div>
  </section>;})}</div>}
  <p className="text-sm text-muted-foreground">{t("ssoHint")}</p>
 </div>;
}
