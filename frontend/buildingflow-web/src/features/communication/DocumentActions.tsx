"use client";
import {useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Download,Upload,LoaderCircle} from "lucide-react";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {FieldError} from "@/components/feedback/MessageBanner";
import {toolbarSelectClass} from "@/components/management/WorkspacePresentation";
import {readDocument,validFile,verifyDownload,type DocumentRecord,type Audience,type Announcement} from "./model";

export function DownloadButton({record}:{record:DocumentRecord}){
 const t=useTranslations("communication");const api=useAuthorizedApi();const notice=useNotification();const locale=useLocale();const [busy,setBusy]=useState(false);const lock=useRef(false);
 async function download(){if(lock.current)return;lock.current=true;setBusy(true);try{
  const blob=await api<Blob>(`/api/v1/properties/${record.propertyId}/documents/${record.id}/download`,{},"blob");
  if(!await verifyDownload(blob,record)){notice.error(t("downloadInvalid"));return;}
  const url=URL.createObjectURL(blob);const anchor=document.createElement("a");anchor.href=url;anchor.download=record.fileName;document.body.append(anchor);anchor.click();anchor.remove();setTimeout(()=>URL.revokeObjectURL(url),10000);notice.success(t("downloaded"));
 }catch(error){if(!(error instanceof ApiError))notice.error(errorMessage(error,locale));}finally{lock.current=false;setBusy(false);}}
 return <Button variant="view" size="sm" disabled={busy} aria-label={`${t("download")} · ${record.fileName}`} onClick={download}>{busy?<LoaderCircle className="animate-spin"/>:<Download/>}{t("download")}</Button>;
}
export function DocumentUploader({propertyId,announcement,onSaved,onBusy}:{propertyId:string;announcement?:Announcement;onSaved:()=>void;onBusy?:(busy:boolean)=>void}){
 const t=useTranslations("communication");const ui=useTranslations("ui");const api=useAuthorizedApi();const notice=useNotification();const locale=useLocale();const [file,setFile]=useState<File|null>(null);const [audience,setAudience]=useState<Audience>("ALL_MEMBERS");const [busy,setBusy]=useState(false);const [invalid,setInvalid]=useState("");const [ambiguous,setAmbiguous]=useState(false);const lock=useRef(false);const request=useRef("");const input=useRef<HTMLInputElement>(null);
 async function upload(event:React.FormEvent){event.preventDefault();if(lock.current)return;if(!file||!validFile(file)){setInvalid(t("fileInvalid"));input.current?.focus();return;}
  if(!request.current)request.current=crypto.randomUUID();const form=new FormData();form.set("file",file);form.set("requestId",request.current);form.set("audience",announcement?.audience??audience);
  if(announcement){form.set("announcementId",announcement.id);form.set("announcementVersion",String(announcement.version));}
  lock.current=true;setBusy(true);onBusy?.(true);setAmbiguous(false);
  try{readDocument(await api<unknown>(`/api/v1/properties/${propertyId}/documents`,{method:"POST",body:form}),propertyId,announcement?.id);notice.success(t("uploaded"));request.current="";setFile(null);if(input.current)input.current.value="";onSaved();}
  catch(error){if(!(error instanceof ApiError))notice.error(errorMessage(error,locale));setAmbiguous(!(error instanceof ApiError)||error.status>=500);}
  finally{lock.current=false;setBusy(false);onBusy?.(false);}
 }
 return <form onSubmit={upload} noValidate className="space-y-4 rounded-xl border p-4"><fieldset disabled={busy} className="space-y-4"><div><Label htmlFor="document-file">{t("file")} *</Label><Input ref={input} id="document-file" type="file" accept="application/pdf,image/png,image/jpeg" className="h-auto min-h-11 py-2" aria-invalid={!!invalid} aria-describedby="document-file-help" onChange={event=>{setFile(event.target.files?.[0]??null);setInvalid("");request.current="";setAmbiguous(false);}}/><p id="document-file-help" className="mt-2 text-xs text-muted-foreground">{t("fileHelp")}</p>{invalid&&<FieldError>{invalid}</FieldError>}</div>
  {!announcement&&<div><Label htmlFor="document-audience">{t("audience")}</Label><select id="document-audience" className={toolbarSelectClass+" w-full"} value={audience} onChange={event=>{setAudience(event.target.value as Audience);request.current="";}}><option value="ALL_MEMBERS">{t("ALL_MEMBERS")}</option><option value="MANAGEMENT">{t("MANAGEMENT")}</option></select></div>}
 </fieldset>{ambiguous&&<p role="status" className="text-xs text-muted-foreground">{t("retryHelp")}</p>}<Button type="submit" disabled={busy}>{busy?<LoaderCircle className="animate-spin"/>:<Upload/>}{busy?ui("saving"):t("uploadDocument")}</Button></form>;
}
