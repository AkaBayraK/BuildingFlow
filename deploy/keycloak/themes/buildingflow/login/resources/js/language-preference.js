// Only a validated display preference; never credentials or authentication URLs.
(() => {
  const locale = document.documentElement.lang.split("-")[0];
  if (locale !== "tr" && locale !== "en") return;
  document.cookie = `buildingflow-login-locale=${locale}; Path=/; Max-Age=31536000; SameSite=Lax${location.protocol === "https:" ? "; Secure" : ""}`;
})();
