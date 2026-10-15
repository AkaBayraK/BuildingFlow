export const locales = ["tr", "en"] as const;
export type Locale = (typeof locales)[number];
export const localeCookie = "buildingflow-locale";
export function resolveLocale(value: unknown): Locale { return value === "en" ? "en" : "tr"; }
export function isLocale(value: unknown): value is Locale { return value === "tr" || value === "en"; }
export function localeCookieOptions(secure: boolean) { return {path:"/",maxAge:31536000,sameSite:"lax" as const,httpOnly:true,secure}; }

// Non-sensitive host preference shared with the local Keycloak login theme.
export const loginLocaleCookie = "buildingflow-login-locale";
export function resolvePreferredLocale(loginValue: unknown, appValue: unknown): Locale { return isLocale(loginValue) ? loginValue : resolveLocale(appValue); }
