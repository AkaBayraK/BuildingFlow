"use client";
import {useEffect,useMemo,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {ConsentMarkupView} from "@/features/consents/ConsentMarkupView";
import {parseMarkup} from "@/features/consents/markup";
import {formatDateTime} from "@/lib/format";
import {bodyMatches,type AccountConsent} from "./model";
import {sha256Hex} from "./photo";
// The person's own decision on the centre (channel WEB): the full text, a tick box, then approve; same rules as the mobile sheet.
export function ConsentDialog({consent,purpose,busy,error,onApprove,onClose}:{consent:AccountConsent;purpose?:string;busy:boolean;error:string;onApprove?:()=>void;onClose:()=>void}){
 const t=useTranslations("account"),locale=useLocale();const [checked,setChecked]=useState(false),[intact,setIntact]=useState<boolean|null>(null);
 const text=consent.text,blocks=useMemo(()=>text?parseMarkup(text.body):null,[text]);
 useEffect(()=>{if(!text)return;let live=true;void bodyMatches(text,sha256Hex).then(ok=>{if(live)setIntact(ok);}).catch(()=>{if(live)setIntact(false);});return()=>{live=false;};},[text]);
 const information=consent.kind==="INFORMATION",name=locale==="en"?consent.nameEn:consent.nameTr;
 const canApprove=!!onApprove&&!!text&&intact===true&&!blocks?.error&&consent.state!=="ACTIVE";
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}>
  <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
   <DialogHeader><DialogTitle>{name}</DialogTitle><DialogDescription>{text?t("textVersion",{version:text.textVersion,date:formatDateTime(text.publishedAt,locale)}):t("noText")}</DialogDescription></DialogHeader>
   {purpose&&<p className="rounded-lg bg-muted p-3 text-sm">{purpose}</p>}
   {intact===false&&<MessageBanner presentation="inline">{t("textNotVerified")}</MessageBanner>}
   {blocks&&!blocks.error&&<div className="rounded-xl border p-4"><ConsentMarkupView blocks={blocks.blocks}/></div>}
   {canApprove&&<label className="flex items-start gap-3 text-sm font-medium"><input type="checkbox" className="mt-1 size-4" checked={checked} disabled={busy} onChange={e=>setChecked(e.target.checked)}/>{information?t("tickInformation"):t("tickConsent")}</label>}
   {!!error&&<MessageBanner presentation="inline">{error}</MessageBanner>}
   <DialogFooter>
    <Button variant="outline" disabled={busy} onClick={onClose}>{purpose?t("continueWithout"):t("close")}</Button>
    {canApprove&&<Button disabled={!checked||busy} onClick={onApprove}>{busy?t("saving"):information?t("markRead"):t("approve")}</Button>}
   </DialogFooter>
  </DialogContent>
 </Dialog>;
}
