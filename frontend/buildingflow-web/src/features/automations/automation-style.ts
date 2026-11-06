import {BellRing,Building2,CalendarCheck2,CalendarClock,CalendarDays,CircleAlert,CircleCheck,CircleSlash,Clock,FileSignature,Gauge,Mail,Megaphone,MessageSquareText,ReceiptText,ShieldAlert,Smartphone,Sparkles,TriangleAlert,Undo2,Wrench,Archive,FilePen,type LucideIcon} from "lucide-react";
import type {AutomationType,Channel,DispatchStatus} from "./model";
/** One icon per automation type, channel and dispatch status (AGENTS.md icon rule). */
export const typeIcon={DUES_DUE_REMINDER:CalendarClock,DUES_OVERDUE_REMINDER:TriangleAlert,PAYMENT_RECEIVED:ReceiptText,CUSTOM:BellRing} as const satisfies Record<AutomationType,LucideIcon>;
/** Icons a user-defined type may carry; the backend accepts exactly these names. */
export const customIcons={BellRing,Wrench,FileSignature,Building2,CalendarCheck2,Gauge,CalendarDays,Megaphone,ShieldAlert,Sparkles} as const satisfies Record<string,LucideIcon>;
export const iconFor=(type:AutomationType,icon:string|null)=>icon&&icon in customIcons?customIcons[icon as keyof typeof customIcons]:typeIcon[type];
export const typeStatusIcon={DRAFT:FilePen,PUBLISHED:CircleCheck,ARCHIVED:Archive} as const satisfies Record<"DRAFT"|"PUBLISHED"|"ARCHIVED",LucideIcon>;
export const channelIcon={IN_APP:Smartphone,SMS:MessageSquareText,EMAIL:Mail} as const satisfies Record<Channel,LucideIcon>;
export const statusIcon={DELIVERED:CircleCheck,QUEUED:Clock,SKIPPED:CircleSlash,FAILED:CircleAlert,CANCELLED:Undo2} as const satisfies Record<DispatchStatus,LucideIcon>;
export const statusVariant={DELIVERED:"default",QUEUED:"secondary",SKIPPED:"outline",FAILED:"destructive",CANCELLED:"outline"} as const satisfies Record<DispatchStatus,string>;
export const automationIcon=BellRing;
