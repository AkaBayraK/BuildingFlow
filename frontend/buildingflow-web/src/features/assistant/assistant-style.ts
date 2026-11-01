import {House,BriefcaseBusiness,ClipboardList,Hourglass,Megaphone,Star,CalendarCheck2,CalendarClock,ChartNoAxesCombined,CircleAlert,Hash,LayoutDashboard,ListChecks,Percent,ReceiptText,Timer,Wallet,Wrench,type LucideIcon} from "lucide-react";
import type {Tone as BadgeTone} from "@/components/management/WorkspacePresentation";
import type {Tone} from "@/features/requests/category-style";
import type {AssistantCategory,AssistantFactKind,AssistantTool} from "./model";

export const assistantCategoryAll:LucideIcon=ListChecks;
export const assistantToolStyle:Record<AssistantTool,{icon:LucideIcon;tone:Tone}>={
 DAILY_SUMMARY:{icon:LayoutDashboard,tone:"blue"},DUES_COLLECTION:{icon:ReceiptText,tone:"amber"},BUDGET_VARIANCE:{icon:ChartNoAxesCombined,tone:"teal"},
 UPCOMING_MAINTENANCE:{icon:CalendarCheck2,tone:"green"},OPEN_REQUESTS:{icon:Wrench,tone:"violet"},
 REQUEST_BACKLOG:{icon:Hourglass,tone:"red"},SUPPLIER_WATCH:{icon:BriefcaseBusiness,tone:"amber"},SURVEY_SUMMARY:{icon:ClipboardList,tone:"teal"},
 RESIDENT_BALANCE:{icon:Wallet,tone:"amber"},RESIDENT_REQUESTS:{icon:Wrench,tone:"violet"},RESIDENT_NOTICES:{icon:Megaphone,tone:"teal"}
};
export const assistantCategoryStyle:Record<AssistantCategory,{icon:LucideIcon;tone:Tone}>={
 OVERVIEW:{icon:LayoutDashboard,tone:"blue"},FINANCE:{icon:Wallet,tone:"amber"},OPERATIONS:{icon:CalendarCheck2,tone:"green"},REQUESTS:{icon:Wrench,tone:"violet"},COMMUNICATION:{icon:Megaphone,tone:"teal"},RESIDENT:{icon:House,tone:"green"}
};
export const assistantKindIcon:Record<AssistantFactKind,LucideIcon>={MONEY:Wallet,COUNT:Hash,PERCENT:Percent,DATE:CalendarClock,DAYS:Timer,SCORE:Star};
export const assistantDraftIcon:LucideIcon=Megaphone;
export const assistantWarningIcon:LucideIcon=CircleAlert;
export const assistantBadgeTone:Record<Tone,BadgeTone>={blue:"info",green:"success",amber:"warning",teal:"teal",red:"error",violet:"violet",neutral:"neutral"};
