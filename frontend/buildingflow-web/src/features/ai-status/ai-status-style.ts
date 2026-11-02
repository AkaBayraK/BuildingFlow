import {BrainCircuit,CheckCircle2,CircleAlert,CircleHelp,CircleMinus,CircleX,Cpu,Gauge,ListChecks,ServerCog,ShieldCheck,type LucideIcon} from "lucide-react";
import type {AiComponentType,AiStatusValue} from "./model.ts";

export const aiTypeIcon:Record<"ALL"|AiComponentType,LucideIcon>={ALL:Gauge,CONFIGURATION:ShieldCheck,SERVICE:ServerCog,MODEL:Cpu,PROMPT:ListChecks};
export const aiStatusIcon:Record<AiStatusValue,LucideIcon>={UP:CheckCircle2,DEGRADED:CircleAlert,DOWN:CircleX,UNKNOWN:CircleHelp,DISABLED:CircleMinus};
export const aiOverallIcon=BrainCircuit;
export const aiStatusText:Record<AiStatusValue,string>={UP:"text-emerald-600 dark:text-emerald-400",DEGRADED:"text-amber-600 dark:text-amber-400",DOWN:"text-red-600 dark:text-red-400",UNKNOWN:"text-slate-500",DISABLED:"text-slate-500"};
export const aiStatusDot:Record<AiStatusValue,string>={UP:"bg-emerald-500",DEGRADED:"bg-amber-500",DOWN:"bg-red-500",UNKNOWN:"bg-slate-400",DISABLED:"bg-slate-400"};
