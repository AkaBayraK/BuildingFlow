import {CalendarCheck2,CalendarClock,ChartNoAxesCombined,CircleAlert,Hash,LayoutDashboard,ListChecks,Percent,ReceiptText,Timer,Wallet,Wrench,type LucideIcon} from "lucide-react";
import type {Tone as BadgeTone} from "@/components/management/WorkspacePresentation";
import type {Tone} from "@/features/requests/category-style";
import type {AssistantCategory,AssistantFactKind,AssistantTool} from "./model";

export const assistantCategoryAll:LucideIcon=ListChecks;
export const assistantToolStyle:Record<AssistantTool,{icon:LucideIcon;tone:Tone}>={
 DAILY_SUMMARY:{icon:LayoutDashboard,tone:"blue"},DUES_COLLECTION:{icon:ReceiptText,tone:"amber"},BUDGET_VARIANCE:{icon:ChartNoAxesCombined,tone:"teal"},
 UPCOMING_MAINTENANCE:{icon:CalendarCheck2,tone:"green"},OPEN_REQUESTS:{icon:Wrench,tone:"violet"}
};
export const assistantCategoryStyle:Record<AssistantCategory,{icon:LucideIcon;tone:Tone}>={
 OVERVIEW:{icon:LayoutDashboard,tone:"blue"},FINANCE:{icon:Wallet,tone:"amber"},OPERATIONS:{icon:CalendarCheck2,tone:"green"},REQUESTS:{icon:Wrench,tone:"violet"}
};
export const assistantKindIcon:Record<AssistantFactKind,LucideIcon>={MONEY:Wallet,COUNT:Hash,PERCENT:Percent,DATE:CalendarClock,DAYS:Timer};
export const assistantWarningIcon:LucideIcon=CircleAlert;
export const assistantBadgeTone:Record<Tone,BadgeTone>={blue:"info",green:"success",amber:"warning",teal:"teal",red:"error",violet:"violet",neutral:"neutral"};
