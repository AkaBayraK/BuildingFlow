// Request type icons: one icon and colour per category so a manager sees what the request is about at a glance.
// The mobile app (sync/contracts.ts) keeps the same mapping with Ionicons.
import {Building2,Droplet,Ellipsis,Flame,Hammer,Lightbulb,PaintRoller,ShieldCheck,Sparkles,Wrench,Zap,type LucideIcon} from "lucide-react";
import type {categories} from "./model";
export type Tone="blue"|"green"|"amber"|"teal"|"red"|"violet"|"neutral";
export const categoryStyle:Record<typeof categories[number],{icon:LucideIcon;tone:Tone}>={
 ELEVATOR:{icon:Wrench,tone:"red"},WATER:{icon:Droplet,tone:"blue"},ELECTRICITY:{icon:Zap,tone:"amber"},HEATING:{icon:Flame,tone:"red"},RENOVATION:{icon:PaintRoller,tone:"blue"},
 FAULT:{icon:Hammer,tone:"amber"},CLEANING:{icon:Sparkles,tone:"green"},SECURITY:{icon:ShieldCheck,tone:"violet"},COMMON_AREA:{icon:Building2,tone:"teal"},SUGGESTION:{icon:Lightbulb,tone:"amber"},OTHER:{icon:Ellipsis,tone:"neutral"},
};
/** Status badge colours: open red, in progress amber, resolved green, rejected neutral. */
export const statusTone={RECORDED:"red",IN_PROGRESS:"amber",RESOLVED:"green",REJECTED:"neutral"} as const satisfies Record<string,Tone>;
