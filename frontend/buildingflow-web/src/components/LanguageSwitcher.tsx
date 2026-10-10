"use client";
import {useState,useTransition} from "react";
import {Languages,LoaderCircle} from "lucide-react";
import {useLocale,useTranslations} from "next-intl";
import {setLocale} from "@/i18n/actions";
import {isLocale} from "@/i18n/locale";
import {useNotification} from "./feedback/Notifications";
export function LanguageSwitcher(){
 const locale=useLocale();const t=useTranslations("language");const [pending,start]=useTransition();const [choice,setChoice]=useState(locale);const notification=useNotification();
 async function change(value:string){if(!isLocale(value)||value===locale)return;setChoice(value);start(async()=>{try{await setLocale(value);}catch{setChoice(locale);notification.error(t("failed"));}});}
 return <label className="language-switcher"><span className="sr-only">{t("label")}</span>{pending?<LoaderCircle aria-hidden="true" className="size-4 animate-spin"/>:<Languages aria-hidden="true" className="size-4"/>}<select aria-label={t("label")} value={pending?choice:locale} disabled={pending} onChange={event=>void change(event.target.value)}><option value="tr">Türkçe</option><option value="en">English</option></select></label>;
}
