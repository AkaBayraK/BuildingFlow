"use client";
import {useTranslations} from "next-intl";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {capacityStyle,leaseStatusStyle,portfolioStyle,verificationStyle} from "./relation-style";
import type {Capacity,PortfolioStatus,Verification} from "./model";
export function CapacityBadge({value}:{value:Capacity}){const t=useTranslations("personCenter");const s=capacityStyle[value];return <ToneBadge tone={s.tone} icon={s.icon}>{t(value)}</ToneBadge>;}
export function VerificationBadge({value}:{value:Verification}){const t=useTranslations("personCenter");const s=verificationStyle[value];return <ToneBadge tone={s.tone} icon={s.icon}>{t("verification_"+value)}</ToneBadge>;}
export function PortfolioBadge({value}:{value:PortfolioStatus}){const t=useTranslations("personCenter");const s=portfolioStyle[value];return <ToneBadge tone={s.tone} icon={s.icon}>{t("portfolio_"+value)}</ToneBadge>;}
export function LeaseStatusBadge({value}:{value:string}){const t=useTranslations("personCenter");const s=leaseStatusStyle[value]??leaseStatusStyle.DISABLED;return <ToneBadge tone={s.tone} icon={s.icon}>{t(value)}</ToneBadge>;}
