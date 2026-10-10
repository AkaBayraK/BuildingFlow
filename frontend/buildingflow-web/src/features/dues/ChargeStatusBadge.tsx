"use client";
import {useTranslations} from "next-intl";
import {CircleX,TriangleAlert,Clock,CheckCircle2,CircleDollarSign} from "lucide-react";
import {Badge} from "@/components/ui/badge";
import type {DuesCharge} from "./model";
export function ChargeStatusBadge({charge}:{charge:DuesCharge}){
 const t=useTranslations("dues");const Icon=charge.status==="PAID"?CheckCircle2:charge.status==="PARTIALLY_PAID"?CircleDollarSign:charge.status==="CANCELLED"?CircleX:charge.status==="OVERDUE"?TriangleAlert:Clock;
 return <Badge variant="outline" style={{color:charge.status==="CANCELLED"?"var(--destructive)":charge.status==="OVERDUE"?"var(--message-warning)":"var(--primary)"}}><Icon aria-hidden="true" className="size-3"/>{t(charge.status)}</Badge>;
}
