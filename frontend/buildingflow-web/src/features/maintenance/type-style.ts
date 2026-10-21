// Planning type icons: same icon and colour language as the request types (requests/category-style.ts).
import {ClipboardCheck,Ellipsis,Flame,Sparkles,Wrench,type LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import type {MaintenanceStatus,MaintenanceType} from "./model";
export const typeStyle:Record<MaintenanceType,{icon:LucideIcon;tone:Tone}>={
 MAINTENANCE:{icon:Wrench,tone:"amber"},CLEANING:{icon:Sparkles,tone:"green"},INSPECTION:{icon:ClipboardCheck,tone:"violet"},DRILL:{icon:Flame,tone:"red"},OTHER:{icon:Ellipsis,tone:"neutral"},
};
/** Status badge colours: planned blue, in progress amber, completed green, cancelled neutral. */
export const statusTone={PLANNED:"blue",IN_PROGRESS:"amber",COMPLETED:"green",CANCELLED:"neutral"} as const satisfies Record<MaintenanceStatus,Tone>;
