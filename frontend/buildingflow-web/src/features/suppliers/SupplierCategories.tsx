"use client";
import {useCallback,useState} from "react";
import {useTranslations} from "next-intl";
import {CheckCircle2,PauseCircle,Pencil,RefreshCw,Save,X} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {ToneBadge} from "@/components/management/WorkspacePresentation";
import {pageOf,type Workspace} from "@/features/person-center/model";
import {useRows} from "@/features/people/useRows";
import {useCenterMutation} from "@/features/person-center/hooks";
import {relationStyle} from "./supplier-style";
import {readSupplierCategoryRow,type SupplierCategoryRow} from "./model";
const CategoryIcon=relationStyle.category.icon;

// TNM 04: yönetim şirketi kapsamındaki hizmet kategorilerini yeniden adlandırma ve pasife alma.
export function SupplierCategories({tenant}:{tenant:Workspace}){
 const t=useTranslations("suppliers"),ui=useTranslations("ui"),root=`/api/v2/tenants/${tenant.id}`;
 const [revision,setRevision]=useState(0),[page,setPage]=useState(1),[editing,setEditing]=useState<SupplierCategoryRow|null>(null),[name,setName]=useState(""),[enabled,setEnabled]=useState(true),[invalid,setInvalid]=useState(false);
 const decode=useCallback((v:unknown)=>pageOf(v,readSupplierCategoryRow),[]);
 const rows=useRows<SupplierCategoryRow>(`${root}/supplier-categories/manage?page=${page}&pageSize=20`,revision,true,decode);
 const save=useCenterMutation(()=>{setRevision(n=>n+1);setEditing(null);});
 function start(row:SupplierCategoryRow){setEditing(row);setName(row.name);setEnabled(row.isEnabled);setInvalid(false);save.reset();}
 async function submit(e:React.FormEvent){e.preventDefault();if(!editing)return;if(!name.trim()||name.trim().length>100){setInvalid(true);return;}await save.submit(`${root}/supplier-categories/${editing.id}`,"PUT",{name:name.trim(),isEnabled:enabled,version:editing.version},readSupplierCategoryRow);}
 const pages=rows.data?Math.max(1,Math.ceil(rows.data.totalCount/rows.data.pageSize)):1;
 return <section className="space-y-4 rounded-2xl border bg-card p-4 shadow-sm" aria-labelledby="supplier-categories-title">
  <div><h2 id="supplier-categories-title" className="flex items-center gap-2 font-semibold"><CategoryIcon aria-hidden="true"/>{t("view_categories")}</h2><p className="text-sm text-muted-foreground">{t("categoriesHelp")}</p></div>
  {rows.error?<MessageBanner>{rows.error}</MessageBanner>:rows.loading?<p role="status">{ui("loading")}</p>:rows.data&&rows.data.items.length===0?<p className="text-sm text-muted-foreground">{t("noCategories")}</p>:<ul className="space-y-2">{rows.data?.items.map(row=><li key={row.id} className={"flex flex-wrap items-center gap-2 rounded-xl border p-3"+(row.isEnabled?"":" opacity-70")}>
   <span className="request-type-icon tone-teal"><CategoryIcon aria-hidden="true"/></span>
   <div className="min-w-0 flex-1"><p className="font-medium wrap-anywhere">{row.name}</p><p className="text-xs text-muted-foreground">{t("categoryServiceCount",{count:row.serviceCount})}</p></div>
   <ToneBadge tone={row.isEnabled?"success":"neutral"} icon={row.isEnabled?CheckCircle2:PauseCircle}>{row.isEnabled?ui("active"):ui("inactive")}</ToneBadge>
   <Button type="button" variant="edit" size="sm" disabled={save.busy} onClick={()=>start(row)} aria-label={t("editCategory",{name:row.name})}><Pencil aria-hidden="true"/>{t("edit")}</Button>
  </li>)}</ul>}
  {pages>1&&<div className="flex items-center justify-end gap-2"><Button type="button" variant="outline" size="sm" disabled={page<=1} onClick={()=>setPage(p=>p-1)}>{ui("previous")}</Button><span className="text-sm">{page}/{pages}</span><Button type="button" variant="outline" size="sm" disabled={page>=pages} onClick={()=>setPage(p=>p+1)}>{ui("next")}</Button></div>}
  {editing&&<form noValidate onSubmit={submit} className="space-y-4 rounded-xl border p-4"><fieldset disabled={save.busy} className="space-y-4">
   <div><Label htmlFor="supplier-category-edit-name">{t("categoryName")}</Label><Input id="supplier-category-edit-name" autoFocus maxLength={100} value={name} onChange={e=>setName(e.target.value)} aria-invalid={invalid||!!save.fields.name}/>{(invalid||save.fields.name)&&<FieldError>{save.fields.name?.join(" ")||t("invalid")}</FieldError>}</div>
   <div role="radiogroup" aria-labelledby="supplier-category-status"><p id="supplier-category-status" className="mb-2 text-sm font-medium">{ui("status")}</p><div className="flex flex-wrap gap-2">
    <Button type="button" role="radio" aria-checked={enabled} variant={enabled?"default":"outline"} onClick={()=>setEnabled(true)}><CheckCircle2 aria-hidden="true"/>{ui("active")}</Button>
    <Button type="button" role="radio" aria-checked={!enabled} variant={!enabled?"default":"outline"} onClick={()=>setEnabled(false)}><PauseCircle aria-hidden="true"/>{ui("inactive")}</Button>
   </div><p className="mt-1 text-xs text-muted-foreground">{t("categoryPassiveHelp")}</p></div>
  </fieldset>
  {save.error&&<MessageBanner kind={save.conflict?"warning":"error"} action={save.conflict?<Button type="button" variant="view" onClick={()=>{setRevision(n=>n+1);setEditing(null);}}><RefreshCw/>{ui("loadCurrentRecord")}</Button>:undefined}>{save.error}</MessageBanner>}
  <div className="flex flex-wrap justify-end gap-2"><Button type="button" variant="outline" disabled={save.busy} onClick={()=>setEditing(null)}><X aria-hidden="true"/>{ui("close")}</Button><Button type="submit" disabled={save.busy}><Save aria-hidden="true"/>{t("save")}</Button></div>
  </form>}
 </section>;
}
