"use client";
import {useTranslations} from "next-intl";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {categoryStyle,kindStyle,type ChargeCategory,type ChargeKind} from "./charge-kind-style";
export function ChargeKindBadge({kind="DUES",category}:{kind?:ChargeKind;category?:ChargeCategory|null}){
 const t=useTranslations("dues");const style=category?categoryStyle[category]:kindStyle[kind];
 return <ToneBadge tone={style.tone} icon={style.icon}>{category?t("kind_EXTRA")+" · "+t("category_"+category):t("kind_"+kind)}</ToneBadge>;
}
