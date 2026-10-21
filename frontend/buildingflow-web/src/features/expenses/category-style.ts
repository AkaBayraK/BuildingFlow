// Expense category icons: same icon and colour language as the request types (requests/category-style.ts).
import {Briefcase,Ellipsis,ShieldCheck,Sparkles,Umbrella,UserRound,Wrench,Zap,type LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import type {ExpenseCategory} from "./model";
export const categoryStyle:Record<ExpenseCategory,{icon:LucideIcon;tone:Tone}>={
 MAINTENANCE:{icon:Wrench,tone:"red"},CLEANING:{icon:Sparkles,tone:"green"},STAFF:{icon:UserRound,tone:"blue"},ENERGY:{icon:Zap,tone:"amber"},
 SECURITY:{icon:ShieldCheck,tone:"violet"},INSURANCE:{icon:Umbrella,tone:"teal"},ADMINISTRATION:{icon:Briefcase,tone:"blue"},OTHER:{icon:Ellipsis,tone:"neutral"},
};
