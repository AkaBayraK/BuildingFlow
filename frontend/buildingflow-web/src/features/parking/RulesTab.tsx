"use client";
import {useEffect,useState} from "react";
import {useTranslations} from "next-intl";
import {LoaderCircle,RefreshCw,Save,Settings2} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {limits,policyBody,readPolicy,type Policy} from "./model";
import {parkingRoot,useParkingAction} from "./useParking";

const fields=[["maxVehiclesPerUnit",1,limits.maxQuota],["maxVehiclesPerPerson",1,limits.maxQuota],["maxSpacesPerUnit",0,limits.maxQuota],["guestMaxHours",1,limits.maxGuestHours],["retentionDays",1,limits.maxRetention]] as const;
type Form=Record<typeof fields[number][0],string>;
const toForm=(p:Policy):Form=>p.configured?{maxVehiclesPerUnit:String(p.maxVehiclesPerUnit),maxVehiclesPerPerson:String(p.maxVehiclesPerPerson),maxSpacesPerUnit:String(p.maxSpacesPerUnit),guestMaxHours:String(p.guestMaxHours),retentionDays:String(p.retentionDays)}
 :{maxVehiclesPerUnit:"2",maxVehiclesPerPerson:"2",maxSpacesPerUnit:"1",guestMaxHours:"24",retentionDays:String(p.retentionDays)};

export function RulesTab({propertyId,policy,canWrite,onSaved,onReload}:{propertyId:string;policy:Policy|null;canWrite:boolean;onSaved:()=>void;onReload:()=>void}){
 const t=useTranslations("parking");
 const [form,setForm]=useState<Form|null>(policy?toForm(policy):null),[invalid,setInvalid]=useState(false);const action=useParkingAction();
 useEffect(()=>{if(policy){setForm(toForm(policy));action.setStale(false);}},[policy,action.setStale]);
 if(!policy||!form)return <p className="py-8 text-center text-muted-foreground">{t("loading")}</p>;
 async function save(e:React.FormEvent){e.preventDefault();const body=policyBody(form!,policy!.version);if(!body){setInvalid(true);return;}
  const saved=await action.run(`${parkingRoot(propertyId)}/policy`,"PUT",body,v=>readPolicy(v,propertyId),t("policySaved"));if(saved)onSaved();}
 return <form onSubmit={save} noValidate className="max-w-3xl space-y-5 rounded-2xl border bg-card p-5 shadow-sm" aria-label={t("tab_rules")}>
  <h2 className="flex items-center gap-2 text-lg font-bold"><Settings2 aria-hidden="true" className="size-5 text-primary"/>{t("rulesTitle")}</h2>
  {!policy.configured&&<MessageBanner kind="info">{t("policyRequired")}</MessageBanner>}
  {action.stale&&<MessageBanner kind="warning" action={<Button type="button" variant="view" size="sm" onClick={onReload}><RefreshCw/>{t("loadCurrent")}</Button>}>{t("staleHelp")}</MessageBanner>}
  <fieldset disabled={action.busy||!canWrite} className="grid gap-4 sm:grid-cols-2">
   {fields.map(([key,min,max])=><div key={key}><Label htmlFor={`policy-${key}`}>{t(`policy_${key}`)} *</Label><Input id={`policy-${key}`} type="number" inputMode="numeric" min={min} max={max} value={form[key]} aria-invalid={invalid} aria-describedby={`policy-${key}-help`} onChange={e=>{setForm(f=>({...f!,[key]:e.target.value}));setInvalid(false);}}/><p id={`policy-${key}-help`} className="mt-1 text-xs text-muted-foreground">{t(`policyHelp_${key}`,{min,max})}</p></div>)}
  </fieldset>
  <p className="text-xs text-muted-foreground">{t("retentionNote")}</p>
  {invalid&&<FieldError>{t("policyInvalid")}</FieldError>}
  {canWrite&&<Button type="submit" disabled={action.busy||action.stale}>{action.busy?<LoaderCircle className="animate-spin"/>:<Save/>}{t("save")}</Button>}
 </form>;
}
