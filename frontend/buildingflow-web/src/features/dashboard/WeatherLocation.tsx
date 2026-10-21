"use client";
import {useEffect,useState} from "react";
import {CloudSun,CloudRain,CloudSnow,CloudLightning,CloudFog,Cloud,Sun,Moon,MapPin,RefreshCw} from "lucide-react";
import {useTranslations} from "next-intl";
import {Button} from "@/components/ui/button";
import {useLocale} from "next-intl";
import {useAuthorizedApi} from "@/services/api";
import {readWeather,weatherCitySlug,weatherCities,weatherCondition,type WeatherSnapshot} from "./weather";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {DEFAULT_WEATHER_CITY,normalizeWeatherCity,readWeatherCity,writeWeatherCity} from "./weather-location";
export function WeatherLocation({subject}:{subject:string}){
 const t=useTranslations("dashboard"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi();
 const[city,setCity]=useState(DEFAULT_WEATHER_CITY),[draft,setDraft]=useState(DEFAULT_WEATHER_CITY),[open,setOpen]=useState(false),[invalid,setInvalid]=useState(false),[temporary,setTemporary]=useState(false);
 useEffect(()=>{try{setCity(readWeatherCity(window.localStorage,subject));}catch{}},[subject]);
 const [weather,setWeather]=useState<{slug:string;value:WeatherSnapshot}|null>(null),[busy,setBusy]=useState(false),[failed,setFailed]=useState(false),[revision,setRevision]=useState(0);
 const slug=weatherCitySlug(city);
 useEffect(()=>{
  if(!slug)return;const controller=new AbortController();let timer:ReturnType<typeof setTimeout>|undefined,running=false;
  const run=async()=>{
   if(controller.signal.aborted||document.hidden||running)return;
   if(timer)clearTimeout(timer);running=true;setBusy(true);let delay=300000;
   try{const value=readWeather(await api<unknown>(`/api/v1/weather?city=${slug}`,{signal:controller.signal}),slug);if(!controller.signal.aborted){setWeather({slug,value});setFailed(false);delay=Math.max(60000,Math.min(1800000,Date.parse(value.nextRefreshAt)-Date.now()));}}
   catch{if(!controller.signal.aborted)setFailed(true);}
   finally{running=false;if(!controller.signal.aborted){setBusy(false);timer=setTimeout(()=>void run(),delay+Math.random()*30000);}}
  };
  const visible=()=>{if(!document.hidden)void run();};document.addEventListener("visibilitychange",visible);void run();
  return()=>{controller.abort();if(timer)clearTimeout(timer);document.removeEventListener("visibilitychange",visible);};
 },[api,slug,revision]);
 const value=weather?.slug===slug?weather.value:null,condition=weatherCondition(value?.symbol??null),stale=failed||value?.status==="STALE"||!!value?.forecastAt&&Date.parse(value.forecastAt)<Date.now()-5400000;
 const Icon=condition==="clear"?(value?.symbol?.endsWith("_night")?Moon:Sun):condition==="storm"?CloudLightning:condition==="snow"?CloudSnow:condition==="rain"?CloudRain:condition==="fog"?CloudFog:condition==="cloudy"?Cloud:CloudSun;
 const time=(date:string)=>new Intl.DateTimeFormat(locale==="tr"?"tr-TR":"en-GB",{timeZone:"Europe/Istanbul",day:"2-digit",month:"2-digit",hour:"2-digit",minute:"2-digit"}).format(new Date(date));
 function save(){const next=normalizeWeatherCity(draft);if(!next||!weatherCitySlug(next)){setInvalid(true);return;}let saved=false;try{saved=writeWeatherCity(window.localStorage,subject,next);}catch{}setTemporary(!saved);setCity(next);setOpen(false);}
 return <><div className="dashboard-weather" aria-busy={busy}><Icon aria-hidden="true"/><div><div className="dashboard-weather-reading"><strong title={value?.forecastAt?t("weatherForecastAt",{time:time(value.forecastAt)}):undefined}>{value?.temperatureC!=null?new Intl.NumberFormat(locale,{maximumFractionDigits:1}).format(value.temperatureC)+"°C":"—°C"}</strong><button type="button" className="dashboard-weather-refresh" disabled={busy||!slug} onClick={()=>setRevision(n=>n+1)} aria-label={t("weatherRefresh")} title={t("weatherRefresh")}><RefreshCw size={14} className={busy?"animate-spin":""} aria-hidden="true"/></button></div><button type="button" className="dashboard-weather-city" aria-label={t("weatherChangeCity",{city})} onClick={()=>{setDraft(city);setInvalid(false);setOpen(true);}}><MapPin size={12} aria-hidden="true"/>{city}</button>
 <small role="status">{!slug?t("weatherCityUnsupported"):value?.temperatureC!=null?(stale?t("weatherStale"):t(({clear:"weatherConditionClear",partly:"weatherConditionPartly",cloudy:"weatherConditionCloudy",rain:"weatherConditionRain",snow:"weatherConditionSnow",storm:"weatherConditionStorm",fog:"weatherConditionFog",unknown:"weatherConditionUnknown"} as const)[condition])):busy?ui("loading"):t("weatherUnavailable")}</small>
 {value?.updatedAt&&<small>{t("weatherUpdated",{time:time(value.updatedAt)})}</small>}
 <small className="dashboard-weather-credit" title={t("weatherAttributionNote")}><a href="https://www.met.no/en/free-meteorological-data" target="_blank" rel="noreferrer">MET Norway</a> · <a href="https://creativecommons.org/licenses/by/4.0/" target="_blank" rel="noreferrer">CC BY 4.0</a> · {t("weatherForecast")}<span className="sr-only">. {t("weatherAttributionNote")}</span></small>
 {temporary&&<small role="status">{t("weatherCityTemporary")}</small>}</div></div>
 <Dialog open={open} onOpenChange={setOpen}><DialogContent><DialogHeader><DialogTitle>{t("weatherCityTitle")}</DialogTitle><DialogDescription>{t("weatherCityHelp")}</DialogDescription></DialogHeader><form onSubmit={e=>{e.preventDefault();save();}} className="space-y-4"><label className="block space-y-2"><span>{t("weatherCityLabel")}</span><select className="h-10 w-full rounded-md border border-input bg-background px-3" value={draft} onChange={e=>{setDraft(e.target.value);setInvalid(false);}} aria-invalid={invalid} aria-describedby={invalid?"weather-city-error":undefined}>{!weatherCitySlug(draft)&&<option value={draft} disabled>{draft}</option>}{weatherCities.map(c=><option key={c.id} value={c.name}>{c.name}</option>)}</select></label>{invalid&&<p id="weather-city-error" role="alert" className="text-sm text-destructive">{t("weatherCityInvalid")}</p>}<DialogFooter><Button type="button" variant="outline" onClick={()=>setOpen(false)}>{ui("cancel")}</Button><Button type="submit">{ui("save")}</Button></DialogFooter></form></DialogContent></Dialog></>;
}
