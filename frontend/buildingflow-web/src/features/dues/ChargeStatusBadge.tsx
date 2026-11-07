"use client";
import {useTranslations} from "next-intl";
import {CircleX,TriangleAlert,Clock,CheckCircle2,CircleDollarSign} from "lucide-react";
import {Badge} from "@/components/ui/badge";
import type {DuesCharge} from "./model";
// Paid green, overdue red, partial amber, open blue, cancelled muted; uses the shared message tone tokens (light + dark).
export const statusTone={
 PAID:"border-(--message-success-border) bg-(--message-success-bg) text-(--message-success-text)",
 OVERDUE:"border-(--message-error-border) bg-(--message-error-bg) text-(--message-error-text)",
 PARTIALLY_PAID:"border-(--message-warning-border) bg-(--message-warning-bg) text-(--message-warning-text)",
 OPEN:"border-(--message-info-border) bg-(--message-info-bg) text-(--message-info-text)",
 CANCELLED:"border-border bg-muted text-muted-foreground line-through decoration-1",
} as const;
export function ChargeStatusBadge({charge}:{charge:DuesCharge}){
 const t=useTranslations("dues");const Icon=charge.status==="PAID"?CheckCircle2:charge.status==="PARTIALLY_PAID"?CircleDollarSign:charge.status==="CANCELLED"?CircleX:charge.status==="OVERDUE"?TriangleAlert:Clock;
 return <Badge variant="outline" className={statusTone[charge.status]}><Icon aria-hidden="true" className="size-3"/>{t(charge.status)}</Badge>;
}
