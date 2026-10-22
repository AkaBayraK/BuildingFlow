// One icon and tone per capacity, verification, portfolio status and lease status (AGENTS.md icon rule).
import {Archive,CalendarClock,CircleCheckBig,CirclePlay,Clock,DoorClosed,FileSignature,House,KeyRound,KeySquare,LayoutGrid,PauseCircle,ShieldCheck,ShieldX,Users,type LucideIcon} from "lucide-react";
import type {Tone} from "@/components/management/WorkspacePresentation";
import type {Capacity,PortfolioStatus,Verification} from "./model";
type Style={icon:LucideIcon;tone:Tone};
export const capacityStyle:Record<Capacity,Style>={OWNER:{icon:KeyRound,tone:"info"},TENANT:{icon:FileSignature,tone:"warning"},RESIDENT:{icon:House,tone:"teal"}};
export const allCapacities:Style={icon:LayoutGrid,tone:"neutral"};
export const verificationStyle:Record<Verification,Style>={VERIFIED:{icon:ShieldCheck,tone:"success"},LEGACY:{icon:Archive,tone:"neutral"},PENDING:{icon:Clock,tone:"warning"},REJECTED:{icon:ShieldX,tone:"error"}};
export const portfolioStyle:Record<PortfolioStatus,Style>={OWNER_OCCUPIED:{icon:House,tone:"success"},RENTED_OUT:{icon:KeySquare,tone:"info"},OWNER_ONLY:{icon:DoorClosed,tone:"neutral"},TENANT_RESIDENT:{icon:House,tone:"teal"},TENANT_NOT_RESIDENT:{icon:FileSignature,tone:"warning"},RESIDENT_ONLY:{icon:Users,tone:"violet"}};
export const leaseStatusStyle:Record<string,Style>={ACTIVE:{icon:CirclePlay,tone:"success"},UPCOMING:{icon:CalendarClock,tone:"info"},ENDED:{icon:CircleCheckBig,tone:"neutral"},DISABLED:{icon:PauseCircle,tone:"neutral"}};
