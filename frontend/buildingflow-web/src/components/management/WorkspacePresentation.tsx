"use client";

import type {ReactNode} from "react";
import type {LucideIcon} from "lucide-react";
import {RefreshCw,Search,RotateCcw,CheckCircle2,PauseCircle,ShieldCheck,Building2,UserCog,Calculator,House,KeyRound,DoorOpen,UserRound} from "lucide-react";
import {useTranslations} from "next-intl";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Badge} from "@/components/ui/badge";
import {cn} from "@/lib/utils";

export function WorkspaceHeading({title,description,scope,icon:Icon,action}:{title:string;description:string;scope:string;icon:LucideIcon;action?:ReactNode}){
  return <div className="relative mb-6 overflow-hidden rounded-2xl bg-foreground p-6 text-primary-foreground sm:p-8">
    <Icon aria-hidden="true" className="pointer-events-none absolute -right-8 -bottom-10 size-48 rotate-[-12deg] text-primary-foreground/5"/>
    <div className="relative flex flex-wrap items-center justify-between gap-5">
      <div className="min-w-0 flex-1"><p className="mb-3 flex items-center gap-2 text-xs font-medium tracking-wide text-primary-foreground/75"><Icon aria-hidden="true" className="size-4 shrink-0"/><span className="wrap-anywhere">{scope}</span></p><h1 className="m-0 text-2xl font-bold tracking-tight text-inherit sm:text-3xl">{title}</h1><p className="mt-3 max-w-2xl text-sm leading-6 text-primary-foreground/75">{description}</p></div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  </div>;
}

export function SummaryCard({title,value,note,icon:Icon,compact=false}:{title:string;value:ReactNode;note:string;icon:LucideIcon;compact?:boolean}){
  return <div className={cn("flex min-w-0 items-start gap-4 rounded-xl border bg-card p-5 shadow-sm",compact&&"xl:flex-col xl:gap-3")}><span className="grid size-11 shrink-0 place-items-center rounded-xl bg-secondary text-primary"><Icon aria-hidden="true" className="size-5"/></span><div className="min-w-0"><p className="text-xs font-medium text-muted-foreground">{title}</p><p className="my-2 wrap-anywhere text-2xl leading-none font-bold tabular-nums text-card-foreground">{value}</p><p className="text-xs leading-5 text-muted-foreground">{note}</p></div></div>;
}

export function IdentityMark({children,icon:Icon,className}:{children?:ReactNode;icon?:LucideIcon;className?:string}){
  return <span aria-hidden="true" className={cn("grid size-10 shrink-0 place-items-center rounded-xl border border-primary/10 bg-secondary text-sm font-semibold text-primary",className)}>{Icon?<Icon className="size-5"/>:children}</span>;
}

export function ListToolbar({term,onTerm,searchLabel,placeholder,loading,onRefresh,onReset,resetEnabled,children}:{term:string;onTerm:(value:string)=>void;searchLabel:string;placeholder:string;loading:boolean;onRefresh:()=>void;onReset:()=>void;resetEnabled:boolean;children?:ReactNode}){
  const m=useTranslations("management");const t=useTranslations("ui");
  return <div className="flex flex-wrap items-center gap-3 border-b bg-muted/30 p-4 sm:px-5">
    <div className="relative min-w-0 basis-full sm:min-w-48 sm:flex-1 sm:basis-auto"><Search aria-hidden="true" className="pointer-events-none absolute left-3 top-3 size-4 text-muted-foreground"/><Input value={term} maxLength={200} onChange={event=>onTerm(event.target.value)} className="h-10 bg-card pl-9" placeholder={placeholder} aria-label={searchLabel}/></div>
    {children}
    <Button variant="outline" size="sm" disabled={!resetEnabled} onClick={onReset}><RotateCcw className="size-4"/>{m("reset")}</Button>
    <Button variant="view" size="icon" aria-label={t("refresh")} disabled={loading} onClick={onRefresh}><RefreshCw className={cn("size-4",loading&&"animate-spin")}/></Button>
  </div>;
}

export const toolbarSelectClass="h-10 min-w-0 max-w-full rounded-lg border bg-card px-3 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring";

export function DetailField({label,children}:{label:string;children:ReactNode}){
  return <div className="min-w-0 border-b py-4"><dt className="mb-1 text-xs text-muted-foreground">{label}</dt><dd className="text-sm font-medium wrap-anywhere">{children}</dd></div>;
}

export function StatusBadge({enabled}:{enabled:boolean}){
  const t=useTranslations("ui");const Icon=enabled?CheckCircle2:PauseCircle;
  return <Badge variant="secondary" className={enabled?"border border-(--message-success-border) bg-(--message-success-bg) text-(--message-success-text)":"border bg-muted text-muted-foreground"}><Icon aria-hidden="true" className="size-3"/>{enabled?t("active"):t("inactive")}</Badge>;
}

// İŞ19: coloured badge from the shared semantic tokens (no per-screen palette); an icon always accompanies the colour.
export type Tone="success"|"warning"|"info"|"error"|"violet"|"teal"|"neutral";
const tones:Record<Tone,string>={
  success:"border-(--message-success-border) bg-(--message-success-bg) text-(--message-success-text)",
  warning:"border-(--message-warning-border) bg-(--message-warning-bg) text-(--message-warning-text)",
  info:"border-(--message-info-border) bg-(--message-info-bg) text-(--message-info-text)",
  error:"border-(--message-error-border) bg-(--message-error-bg) text-(--message-error-text)",
  violet:"border-(--tone-violet-border) bg-(--tone-violet-bg) text-(--tone-violet-text)",
  teal:"border-(--tone-teal-border) bg-(--tone-teal-bg) text-(--tone-teal-text)",
  neutral:"border-border bg-muted text-muted-foreground",
};
export function ToneBadge({tone,icon:Icon,children}:{tone:Tone;icon?:LucideIcon;children:ReactNode}){
  return <Badge variant="secondary" className={cn("border",tones[tone])}>{Icon&&<Icon aria-hidden="true" className="size-3"/>}{children}</Badge>;
}
// One colour and icon per role everywhere (users list, my account, linked properties).
const roleTones:Record<string,{tone:Tone;icon:LucideIcon}>={
  SUPER_ADMIN:{tone:"violet",icon:ShieldCheck},SITE_ADMIN:{tone:"info",icon:Building2},MANAGER:{tone:"teal",icon:UserCog},
  ACCOUNTANT:{tone:"warning",icon:Calculator},RESIDENT:{tone:"neutral",icon:House},OWNER:{tone:"success",icon:KeyRound},TENANT:{tone:"info",icon:DoorOpen},
};
export function RoleBadge({role,label}:{role:string;label:string}){
  const style=roleTones[role]??{tone:"neutral" as Tone,icon:UserRound};
  return <ToneBadge tone={style.tone} icon={style.icon}>{label}</ToneBadge>;
}
