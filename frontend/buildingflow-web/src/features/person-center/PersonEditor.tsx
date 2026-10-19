"use client";
import {useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {Button} from "@/components/ui/button";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {PersonFormFields} from "@/features/people/PersonFormFields";
import {person,personRoot,profileValues,valuesProfile,mutation,centerRecord,type Person} from "./model";
import {useCenterMutation} from "./hooks";
export function PersonEditor({tenant,target,onClose,onSaved}:{tenant:string;target:Person|null;onClose:()=>void;onSaved:()=>void}){
 const ui=useTranslations("ui"),t=useTranslations("personCenter"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const [record,setRecord]=useState(target),[values,setValues]=useState(()=>profileValues(target?.profile)),[enabled,setEnabled]=useState(target?.isEnabled??true),[reloadError,setReloadError]=useState("");const form=useRef<HTMLFormElement>(null);
 const state=useCenterMutation(()=>{notice.success(t("saved"));onSaved();onClose();});
 function change(k:string,v:string){setValues(old=>({...old,[k]:v}));}
 function classify(k:string,v:string){setValues(old=>({...old,[k]:v,identityNumber:"",...(k==="citizenship"?{countryCode:v==="DOMESTIC"?"TR":""}:{}),...(k==="personKind"&&v==="LEGAL"?{lastName:"",birthDate:"",gender:""}:{})}));}
 async function reload(){if(!record)return;try{const current=centerRecord(await api<unknown>(personRoot(tenant,record.id)+"/center"),tenant,record.id);if(!current.canEdit){onSaved();onClose();return;}setRecord(current.person);setValues(profileValues(current.person.profile));setEnabled(current.person.isEnabled);state.reset();setReloadError("");}catch(e){setReloadError(errorMessage(e,locale));}}
 const errors=Object.fromEntries(Object.entries(state.fields).map(([k,v])=>[k.replace(/^profile\./,"")==="kind"?"personKind":k.replace(/^profile\./,""),v]));
 return <Dialog open onOpenChange={v=>{if(!v&&!state.busy){onSaved();onClose();}}}><ManagementPanel className="person-editor-panel" showCloseButton={!state.busy}><DialogHeader><DialogTitle>{record?t("editPerson"):ui("newPerson")}</DialogTitle><DialogDescription>{t("profileHelp")}</DialogDescription></DialogHeader><form ref={form} onSubmit={e=>{e.preventDefault();if(!form.current?.reportValidity()||state.conflict&&!!record)return;void state.submit(personRoot(tenant,record?.id),record?"PUT":"POST",{profile:valuesProfile(values),...(record?{expectedVersion:record.version,isEnabled:enabled}:{})},v=>mutation(v,r=>person(r,tenant,record?.id)));}}><fieldset disabled={state.busy||state.ambiguous} className="border-0 p-0"><PersonFormFields central values={values} errors={errors} url="" busy={state.busy} onChange={change} onClassification={classify}/>{record&&<label className="my-4 flex items-center gap-3"><input type="checkbox" checked={enabled} onChange={e=>setEnabled(e.target.checked)}/>{ui("activeRecord")}</label>}</fieldset>{(state.error||reloadError)&&<MessageBanner className="my-4" action={state.conflict&&record?<Button type="button" disabled={state.busy} variant="outline" onClick={()=>void reload()}>{ui("loadCurrentRecord")}</Button>:undefined}>{reloadError||state.error}</MessageBanner>}{state.ambiguous&&<MessageBanner kind="warning">{t("ambiguous")}</MessageBanner>}<DialogFooter><Button type="button" variant="outline" disabled={state.busy} onClick={()=>{onSaved();onClose();}}>{ui("cancel")}</Button><Button type="submit" disabled={state.busy||state.conflict&&!!record}>{ui(state.busy?"saving":"save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}
