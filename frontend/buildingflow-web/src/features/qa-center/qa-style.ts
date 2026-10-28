import {Ban,CircleCheck,CircleDashed,CircleX,ClipboardList,Database,Eye,FileSearch,Gauge,Hourglass,LayoutDashboard,ListChecks,Loader,MinusCircle,Play,Settings2,ShieldCheck,Sparkles,Timer,Trash2,Bug,type LucideIcon} from "lucide-react";
import type {QaRunStatus,QaStepStatus,QaTab} from "./model";

export const qaTabIcon:Record<QaTab,LucideIcon>={overview:LayoutDashboard,inventory:FileSearch,scenarios:ListChecks,assistant:Sparkles,runs:Play,live:Eye};
export const qaPlannedIcon:Record<string,LucideIcon>={data:Database,results:ClipboardList,defects:Bug,cleanup:Trash2,settings:Settings2};
export const qaRunStatusIcon:Record<QaRunStatus,LucideIcon>={QUEUED:Hourglass,RUNNING:Loader,PASSED:CircleCheck,FAILED:CircleX,CANCELLED:Ban,TIMED_OUT:Timer};
export const qaStepStatusIcon:Record<QaStepStatus,LucideIcon>={PASSED:CircleCheck,FAILED:CircleX,SKIPPED:MinusCircle};
export const qaRunStatusTone:Record<QaRunStatus,string>={QUEUED:"bg-slate-100 text-slate-700 dark:bg-slate-900 dark:text-slate-200",RUNNING:"bg-blue-100 text-blue-700 dark:bg-blue-950/50 dark:text-blue-200",
 PASSED:"bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-200",FAILED:"bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-200",
 CANCELLED:"bg-zinc-100 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200",TIMED_OUT:"bg-amber-100 text-amber-800 dark:bg-amber-950/50 dark:text-amber-200"};
export const qaScenarioIcon:Record<string,LucideIcon>={"system.health":Gauge,"api.authorization":ShieldCheck,"db.migrations":Database};
export const qaDefaultScenarioIcon=CircleDashed;
