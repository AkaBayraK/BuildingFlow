"use client";
import {usePathname} from "next/navigation";
import {useTranslations} from "next-intl";
import {BackButton} from "@/components/navigation/BackButton";
import {propertySubpageBack} from "./back-target";

export function PropertySubpageBack(){
 const t=useTranslations("ui");const target=propertySubpageBack(usePathname());
 return target?<div className="-mb-4 pt-6"><BackButton href={target.href} label={t(target.label)}/></div>:null;
}
