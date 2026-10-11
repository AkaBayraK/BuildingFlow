"use client";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useLocale,useTranslations} from "next-intl";

import { createContext, useContext, useEffect, useRef, useState } from "react";
import Keycloak from "keycloak-js";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { LogOut, ShieldCheck, UserRound } from "lucide-react";
import {LanguageSwitcher} from "@/components/LanguageSwitcher";
import {useNotification} from "@/components/feedback/Notifications";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import { authConfig } from "./config";
import {isWorkspaceReturnPath} from "./return-path";

let client: Keycloak | undefined;
let initialization: Promise<boolean> | undefined;
function identity() {
  if (!client) {
    const authority = authConfig.authority;
    const index = authority.lastIndexOf("/realms/");
    if (index < 0) throw new Error("OIDC authority must contain /realms/.");
    client = new Keycloak({ url: authority.slice(0, index), realm: authority.slice(index + 8), clientId: authConfig.clientId });
  }
  return client;
}
const AuthContext = createContext<Keycloak | null>(null);
const returnPathKey = "buildingflow-return-path";
function rememberWorkspacePath() {
  const path=window.location.pathname+window.location.search;
  if (isWorkspaceReturnPath(path)) {
    // Store only a validated local route. Credentials and tokens remain in memory.
    window.sessionStorage.setItem(returnPathKey, path);
  }
}
export function useIdentity() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("Authenticated context required.");
  return value;
}
export function AuthGate({ children }: { children: React.ReactNode }) {
  const t=useTranslations("ui"); const locale=useLocale();

  const localeRef=useRef(locale);localeRef.current=locale;
  const router = useRouter();
  const [authenticated, setAuthenticated] = useState(false);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    let active = true;
    const kc = identity();
    rememberWorkspacePath();
    const clear = () => { if (active) setAuthenticated(false); };
    const renew = async () => {
      try { await kc.updateToken(30); }
      catch { kc.clearToken(); clear(); rememberWorkspacePath(); await kc.login({ redirectUri: window.location.origin + "/", locale: localeRef.current }); }
    };
    kc.onAuthLogout = clear;
    kc.onTokenExpired = () => { void renew(); };
    initialization ??= kc.init({ onLoad: "login-required", pkceMethod: "S256", flow: "standard", checkLoginIframe: false, locale: localeRef.current, redirectUri: window.location.origin + "/" });
    void initialization.then(value => {
      if (!active) return;
      setAuthenticated(value);
      if (value) {
        const path = window.sessionStorage.getItem(returnPathKey);
        window.sessionStorage.removeItem(returnPathKey);
        if (path && isWorkspaceReturnPath(path) && path !== window.location.pathname+window.location.search) router.replace(path);
      }
    }).catch(() => { if (active) setFailed(true); });
    const interval = window.setInterval(() => { if (kc.authenticated) void renew(); }, 30000);
    return () => { active = false; window.clearInterval(interval); };
  }, [router]);
  if (failed) return <MessageBanner className="my-4 text-left"><h1>{t("signInServiceUnavailable")}</h1><p>{t("checkTheKeycloakConnectionAndTryAgain")}</p><button onClick={() => window.location.reload()}>{t("tryAgain")}</button></MessageBanner>;
  if (!authenticated) return <div className="auth-wait" role="status">{t("preparingSecureSignIn")}</div>;
  return <AuthContext.Provider value={identity()}>{children}</AuthContext.Provider>;
}
export function SessionControls() {
 const t=useTranslations("ui");const locale=useLocale();const kc=useIdentity();const api=useAuthorizedApi();const notification=useNotification();const [checking,setChecking]=useState(false);
 async function checkApi(){if(checking)return;setChecking(true);try{await api("/api/v1/status");notification.success(t("authenticatedAPIConnectionSuccessful"));}catch(error){notification.error(errorMessage(error,locale));}finally{setChecking(false);}}
 return <div className="session-controls"><LanguageSwitcher/><span className="inline-flex items-center gap-2 font-semibold text-slate-600"><UserRound aria-hidden="true" size={14}/>{kc.tokenParsed?.preferred_username ?? t("user")}</span><Button variant="ghost" size="sm" disabled={checking} aria-label={t("checkAPIConnection")} onClick={()=>void checkApi()}><ShieldCheck aria-hidden="true" size={14}/><span className="hidden lg:inline">{t("checkAPIConnection")}</span></Button><Button variant="outline" size="sm" onClick={()=>void kc.logout({redirectUri:window.location.origin+"/login"})}><LogOut aria-hidden="true" size={14}/>{t("signOut")}</Button></div>;
}
