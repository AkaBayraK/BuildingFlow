import {cookies} from "next/headers";
import {getRequestConfig} from "next-intl/server";
import {localeCookie, loginLocaleCookie, resolvePreferredLocale} from "./locale";
export default getRequestConfig(async () => {
  const store=await cookies();
  const locale = resolvePreferredLocale(store.get(loginLocaleCookie)?.value,store.get(localeCookie)?.value);
  return {locale, timeZone:"Europe/Istanbul", messages:(await import("../../messages/"+locale+".json")).default};
});
