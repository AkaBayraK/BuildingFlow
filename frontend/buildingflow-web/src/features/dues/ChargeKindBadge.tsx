"use client";
import {useDefinitionOptions} from "@/features/definitions/useDefinitionOptions";
import {chargeCategories} from "./charge-kinds";
import {useTranslations} from "next-intl";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {categoryStyle,kindStyle,type ChargeCategory,type ChargeKind} from "./charge-kind-style";
export function ChargeKindBadge({propertyId,kind="DUES",category}:{propertyId:string;kind?:ChargeKind;category?:ChargeCategory|null}){
 const definitions=useDefinitionOptions(propertyId,"CHARGE_CATEGORY",chargeCategories);
 const t=useTranslations("dues");const style=category?definitions.style(category,categoryStyle[category]):kindStyle[kind];
 return <ToneBadge tone={style.tone} icon={style.icon}>{category?t("kind_EXTRA")+" · "+definitions.label(category,t("category_"+category)):t("kind_"+kind)}</ToneBadge>;
}
