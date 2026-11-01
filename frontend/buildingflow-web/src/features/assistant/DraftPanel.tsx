"use client";
import {useRef,useState} from "react";
import {useRouter} from "next/navigation";
import {useLocale,useTranslations} from "next-intl";
import {ExternalLink,Lock,ShieldCheck,Sparkles,WandSparkles} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Label} from "@/components/ui/label";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {ToneBadge,toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {assistantDraftIcon} from "./assistant-style";
import {announcementCategories,assistantNoteLimit,assistantPath,canDraft,draftBody,draftEditorHref,draftStorageKey,readDraft,type AnnouncementDraftCategory,type AssistantDraft,type AssistantQuota} from "./model";

// Produces a draft only. Saving and publishing stay in the announcement editor, always by a person.
export function DraftPanel({propertyId,disabled,onQuota}:{propertyId:string;disabled:boolean;onQuota:(quota:AssistantQuota)=>void}){
 const t=useTranslations("assistant"),c=useTranslations("communication"),locale=useLocale() as "tr"|"en",api=useAuthorizedApi(),router=useRouter();
 const [note,setNote]=useState(""),[category,setCategory]=useState<AnnouncementDraftCategory>("GENERAL"),[draft,setDraft]=useState<AssistantDraft|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState("");
 const lock=useRef(false);const Icon=assistantDraftIcon;
 async function generate(e:React.FormEvent){
  e.preventDefault();if(lock.current||!canDraft(note))return;lock.current=true;setBusy(true);setError("");setDraft(null);
  try{const value=readDraft(await api<unknown>(`${assistantPath}/announcement-draft`,{method:"POST",body:JSON.stringify(draftBody(propertyId,locale,note,category))}));setDraft(value);onQuota(value.quota);}
  catch(err){setError(errorMessage(err,locale));}finally{lock.current=false;setBusy(false);}
 }
 function openEditor(value:AssistantDraft){
  try{sessionStorage.setItem(draftStorageKey(propertyId),JSON.stringify({title:value.title,content:value.content,category:value.category}));}catch{setError(t("draftStorageFailed"));return;}
  router.push(draftEditorHref(propertyId));
 }
 return <section className="space-y-3 rounded-2xl border bg-card p-4" aria-labelledby="assistant-draft-title">
  <h2 id="assistant-draft-title" className="flex items-center gap-2 font-semibold"><Icon aria-hidden="true" className="size-4"/>{t("draftTitle")}</h2>
  <p className="text-xs text-muted-foreground">{t("draftHelp")}</p>
  <form onSubmit={generate} className="space-y-3"><fieldset disabled={busy||disabled} className="space-y-3">
   <div><Label htmlFor="assistant-draft-note">{t("draftNote")}</Label>
    <textarea id="assistant-draft-note" rows={3} maxLength={assistantNoteLimit} value={note} onChange={e=>setNote(e.target.value)} placeholder={t("draftPlaceholder")} aria-describedby="assistant-draft-note-help"
     className="w-full rounded-lg border bg-background p-3 text-sm focus-visible:outline-2 focus-visible:outline-ring"/>
    <p id="assistant-draft-note-help" className="mt-1 text-xs text-muted-foreground">{t("draftNoteHelp",{count:note.trim().length,max:assistantNoteLimit})}</p></div>
   <div><Label htmlFor="assistant-draft-category">{t("draftCategory")}</Label>
    <select id="assistant-draft-category" className={toolbarSelectClass+" w-full"} value={category} onChange={e=>setCategory(e.target.value as AnnouncementDraftCategory)}>{announcementCategories.map(value=><option key={value} value={value}>{c(`category_${value}` as never)}</option>)}</select></div>
   <Button type="submit" disabled={busy||disabled||!canDraft(note)} className="w-full"><WandSparkles aria-hidden="true"/>{busy?t("thinking"):t("draftGenerate")}</Button>
  </fieldset></form>
  {error&&<MessageBanner presentation="inline" announce={false}>{error}</MessageBanner>}
  {draft&&<div className="space-y-2 rounded-xl border border-dashed p-3 text-sm" aria-live="polite">
   <div className="flex flex-wrap gap-1"><ToneBadge tone={draft.source==="AKAAI"?"violet":"teal"} icon={draft.source==="AKAAI"?Sparkles:ShieldCheck}>{t(`source_${draft.source}`)}</ToneBadge>
    {draft.masked&&<ToneBadge tone="warning" icon={Lock}>{t("draftMasked")}</ToneBadge>}{draft.modelFallback&&<ToneBadge tone="warning" icon={ShieldCheck}>{t("fallback")}</ToneBadge>}</div>
   <p className="font-semibold wrap-anywhere">{draft.title}</p><p className="whitespace-pre-line wrap-anywhere">{draft.content}</p>
   <p className="text-xs text-muted-foreground">{t("draftNotSaved")}</p>
   <Button type="button" variant="view" className="w-full" onClick={()=>openEditor(draft)}><ExternalLink aria-hidden="true"/>{t("draftOpenEditor")}</Button>
  </div>}
 </section>;
}
