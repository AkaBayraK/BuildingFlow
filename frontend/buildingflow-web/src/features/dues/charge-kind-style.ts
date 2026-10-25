// One icon and tone per debt type and extra-charge category (AGENTS.md icon rule); the label always carries the meaning.
import {CalendarClock,Ellipsis,Flame,Hammer,Layers,Package,PlugZap,ReceiptText,ShieldCheck,type LucideIcon} from "lucide-react";
import type {Tone} from "@/components/management/WorkspacePresentation";
import type {ChargeCategory,ChargeKind} from "./charge-kinds";
export {chargeCategories,chargeKinds,type ChargeCategory,type ChargeKind} from "./charge-kinds";
export const kindStyle:Record<ChargeKind|"ALL",{icon:LucideIcon;tone:Tone}>={ALL:{icon:Layers,tone:"neutral"},DUES:{icon:CalendarClock,tone:"info"},EXTRA:{icon:ReceiptText,tone:"violet"}};
export const categoryStyle:Record<ChargeCategory,{icon:LucideIcon;tone:Tone}>={
 FIXTURE:{icon:Package,tone:"teal"},REPAIR:{icon:Hammer,tone:"warning"},HEATING:{icon:Flame,tone:"error"},
 UTILITY:{icon:PlugZap,tone:"info"},INSURANCE:{icon:ShieldCheck,tone:"success"},OTHER:{icon:Ellipsis,tone:"neutral"},
};
