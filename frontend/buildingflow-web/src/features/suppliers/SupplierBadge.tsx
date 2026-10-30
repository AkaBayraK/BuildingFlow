"use client";
import {useTranslations} from "next-intl";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {kindStyle,statusStyle} from "./supplier-style";
import type {SupplierKind,SupplierStatus} from "./model";
export function SupplierBadge({kind,status}:{kind?:SupplierKind;status?:SupplierStatus}){const t=useTranslations("suppliers"),value=kind??status;if(!value)return null;const style=kind?kindStyle[kind]:statusStyle[status!];return <ToneBadge tone={style.tone} icon={style.icon}>{t(value)}</ToneBadge>;}
