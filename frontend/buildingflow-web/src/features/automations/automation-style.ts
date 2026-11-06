import {BellRing,CalendarClock,CircleAlert,CircleCheck,CircleSlash,Clock,Mail,MessageSquareText,ReceiptText,Smartphone,TriangleAlert,Undo2,type LucideIcon} from "lucide-react";
import type {AutomationType,Channel,DispatchStatus} from "./model";
/** One icon per automation type, channel and dispatch status (AGENTS.md icon rule). */
export const typeIcon={DUES_DUE_REMINDER:CalendarClock,DUES_OVERDUE_REMINDER:TriangleAlert,PAYMENT_RECEIVED:ReceiptText} as const satisfies Record<AutomationType,LucideIcon>;
export const channelIcon={IN_APP:Smartphone,SMS:MessageSquareText,EMAIL:Mail} as const satisfies Record<Channel,LucideIcon>;
export const statusIcon={DELIVERED:CircleCheck,QUEUED:Clock,SKIPPED:CircleSlash,FAILED:CircleAlert,CANCELLED:Undo2} as const satisfies Record<DispatchStatus,LucideIcon>;
export const statusVariant={DELIVERED:"default",QUEUED:"secondary",SKIPPED:"outline",FAILED:"destructive",CANCELLED:"outline"} as const satisfies Record<DispatchStatus,string>;
export const automationIcon=BellRing;
