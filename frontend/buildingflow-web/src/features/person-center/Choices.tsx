"use client";
import {useCallback,useState} from "react";
import {useTranslations} from "next-intl";
import {useRows} from "@/features/people/useRows";
import {Button} from "@/components/ui/button";
import {Label} from "@/components/ui/label";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {pageOf,type Workspace} from "./model";
export function Choices<T extends Workspace>({path,value,onChange,parse,label,id,filter,disabled=false}:{path:string;value:string;onChange:(value:T|null)=>void;parse:(v:unknown)=>T;label:string;id:string;filter?:(v:T)=>boolean;disabled?:boolean}){
 const ui=useTranslations("ui"),t=useTranslations("personCenter");const [page,setPage]=useState(1),[revision,setRevision]=useState(0);const decode=useCallback((v:unknown)=>pageOf(v,parse),[parse]);const rows=useRows<T>(`${path}?page=${page}&pageSize=20`,revision,!!path,decode);
 const options=rows.data?.items.filter(v=>!filter||filter(v))??[];
 return <div className="space-y-2"><Label htmlFor={id}>{label} *</Label><select id={id} required className={toolbarSelectClass+" w-full"} disabled={disabled||rows.loading||!!rows.error} value={value} onChange={e=>onChange(options.find(v=>v.id===e.target.value)??null)}><option value="">{rows.loading?ui("loading"):t("choose")}</option>{value&&!options.some(v=>v.id===value)&&<option value={value}>{t("selected")}</option>}{options.map(v=><option key={v.id} value={v.id}>{v.name}</option>)}</select>{rows.error&&<MessageBanner action={<Button type="button" variant="view" disabled={disabled} onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button>}>{rows.error}</MessageBanner>}<div className="flex gap-2"><Button type="button" variant="outline" size="sm" disabled={disabled||rows.loading||page===1} onClick={()=>setPage(n=>n-1)}>{ui("previousPage")}</Button><Button type="button" variant="outline" size="sm" disabled={disabled||rows.loading||!!rows.error||!rows.data||page*20>=rows.data.totalCount} onClick={()=>setPage(n=>n+1)}>{ui("nextPage")}</Button></div></div>;
}
