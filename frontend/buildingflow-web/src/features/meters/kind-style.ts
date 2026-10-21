// Meter kind icons: same icon and colour language as the request types (requests/category-style.ts).
import {Droplet,Flame,Zap,type LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import type {Meter} from "./model";
export const meterKinds=["WATER","ELECTRICITY","GAS"] as const satisfies readonly Meter["kind"][];
export const kindStyle:Record<Meter["kind"],{icon:LucideIcon;tone:Tone}>={WATER:{icon:Droplet,tone:"blue"},ELECTRICITY:{icon:Zap,tone:"amber"},GAS:{icon:Flame,tone:"red"}};
