"use client";
import {useCallback,useState} from "react";
import {useTranslations} from "next-intl";
import {useRows,useSearch} from "@/features/people/useRows";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {pageOf,directoryRow,type DirectoryRow} from "./model";
// Joint tenants are picked from the authorized, masked directory of the same tenant.
export function PersonPicker({tenant,id,value,onChange,disabled=false}:{tenant:string;id:string;value:DirectoryRow|null;onChange:(v:DirectoryRow|null)=>void;disabled?:boolean}){
 const t=useTranslations("personCenter"),ui=useTranslations("ui");const [term,setTerm]=useState("");const search=useSearch(term);const decode=useCallback((v:unknown)=>pageOf(v,directoryRow),[]);
 const rows=useRows<DirectoryRow>(`/api/v2/people?${new URLSearchParams({tenantId:tenant,search,page:"1",pageSize:"20"})}`,0,true,decode);const options=rows.data?.items.filter(r=>r.tenantId===tenant&&r.isEnabled&&r.kind==="NATURAL")??[];
 return <div className="space-y-2"><Label htmlFor={id+"-search"}>{t("jointTenant")}</Label><Input id={id+"-search"} type="search" value={term} disabled={disabled} placeholder={ui("personSearch")} onChange={e=>setTerm(e.target.value)}/><select id={id} aria-label={t("jointTenant")} className={toolbarSelectClass+" w-full"} disabled={disabled||rows.loading} value={value?.id??""} onChange={e=>onChange(options.find(o=>o.id===e.target.value)??null)}><option value="">{rows.loading?ui("loading"):t("noJointTenant")}</option>{value&&!options.some(o=>o.id===value.id)&&<option value={value.id}>{value.displayName}</option>}{options.map(o=><option key={o.id} value={o.id}>{o.displayName} · {o.maskedIdentity}</option>)}</select>{rows.error&&<MessageBanner>{rows.error}</MessageBanner>}</div>;
}
