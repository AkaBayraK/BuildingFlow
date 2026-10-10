export const locales = ["tr", "en"] as const;
export type Locale = (typeof locales)[number];
export const localeCookie = "buildingflow-locale";
export function resolveLocale(value: unknown): Locale { return value === "en" ? "en" : "tr"; }
export function isLocale(value: unknown): value is Locale { return value === "tr" || value === "en"; }
