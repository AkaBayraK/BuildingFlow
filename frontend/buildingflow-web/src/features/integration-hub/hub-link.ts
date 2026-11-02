// AkaIntegrationHub yönetim ekranına giden bağlantı. Yalnız herkese açık adres ve UI tercihleri taşınır;
// token/oturum bilgisi URL'ye konmaz. Hub kendi Keycloak oturumunu aynı realm'den (SSO) açar.
export const integrationHubIdp = "buildingflow";
export const integrationHubScreens = {smtp: "/admin/email/providers", deliveries: "/admin/email/deliveries"} as const;
export type IntegrationHubScreen = keyof typeof integrationHubScreens;
// SUPER_ADMIN/SITE_ADMIN Hub'da TenantAdmin, MANAGER Viewer olarak eşlenir (tenant kaydındaki rol listesi).
export const integrationHubRoles = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER"] as const;

export function integrationHubBase(raw: string | undefined): string | null {
  if (!raw) return null;
  try {
    const url = new URL(raw.trim());
    if (url.protocol !== "https:" && url.protocol !== "http:") return null;
    if (url.username || url.password || url.search || url.hash) return null;
    return url.origin + url.pathname.replace(/\/+$/, "");
  } catch {
    return null;
  }
}

export function integrationHubHref(options: {base: string | null; screen: IntegrationHubScreen; locale: string; theme: "light" | "dark"; returnUrl: string}): string | null {
  if (!options.base) return null;
  const url = new URL(options.base + integrationHubScreens[options.screen]);
  url.searchParams.set("idp", integrationHubIdp);
  url.searchParams.set("host", "BuildingFlow");
  url.searchParams.set("locale", options.locale === "en" ? "en" : "tr");
  url.searchParams.set("theme", options.theme);
  try {
    const back = new URL(options.returnUrl);
    if (back.protocol === "https:" || back.protocol === "http:") url.searchParams.set("return", back.origin + back.pathname);
  } catch {
    // Dönüş adresi yoksa Hub "geri dön" bağlantısını göstermez.
  }
  return url.toString();
}
