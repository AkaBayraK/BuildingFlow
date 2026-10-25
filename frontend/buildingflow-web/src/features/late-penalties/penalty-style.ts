// One icon and tone per penalty tab/state (AGENTS.md icon rule); meaning is always carried by the label too.
import {Ban,BadgeCheck,ChartColumn,FilePen,Hourglass,ReceiptText,Repeat,Scale,ShieldOff,type LucideIcon} from "lucide-react";
import type {Tone} from "@/components/management/WorkspacePresentation";
import type {AccrualStatus,PolicyStatus} from "./model";
export type DuesTab="charges"|"overdue"|"policies"|"report";
export const tabIcon:Record<DuesTab,LucideIcon>={charges:ReceiptText,overdue:Hourglass,policies:Scale,report:ChartColumn};
export const policyStyle:Record<PolicyStatus,{icon:LucideIcon;tone:Tone}>={DRAFT:{icon:FilePen,tone:"neutral"},APPROVED:{icon:BadgeCheck,tone:"success"},CANCELLED:{icon:Ban,tone:"error"}};
export const accrualStyle:Record<AccrualStatus,{icon:LucideIcon;tone:Tone}>={ACTIVE:{icon:Hourglass,tone:"warning"},WAIVED:{icon:ShieldOff,tone:"info"},SUPERSEDED:{icon:Repeat,tone:"neutral"}};
