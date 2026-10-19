"use client";
import {PersonProfileDetails} from "@/features/people/PersonProfileDetails";
import {useEffect,useState} from "react";
import {useRouter} from "next/navigation";
import {useLocale,useTranslations} from "next-intl";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {Skeleton} from "@/components/ui/skeleton";
import {Button} from "@/components/ui/button";
import {AccountAvatar} from "@/components/AccountAvatar";
import {PersonActionsPanel} from "./PersonActionsPanel";
import {PersonOccupancies} from "./PersonOccupancies";
import {RegeneratePersonCodeDialog} from "./PersonCode";
import {PersonConsents} from "@/features/consents/PersonConsents";
import {parsePerson} from "@/services/management-contracts";
import {WorkspaceHeading,DetailField,StatusBadge} from "@/components/management/WorkspacePresentation";
import {KeyRound,Users} from "lucide-react";
import {personName,type Person} from "./types";
export function PersonPage({propertyId,personId}:{propertyId:string;personId:string}){
 const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const router=useRouter();const scope=usePropertyScope();const api=useAuthorizedApi();const identity=useIdentity();const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));const [actionsOpen,setActionsOpen]=useState(false);const [codeOpen,setCodeOpen]=useState(false);const [person,setPerson]=useState<Person|null>(null);const [error,setError]=useState("");const [revision,setRevision]=useState(0);
 useEffect(()=>{const controller=new AbortController();setPerson(null);setError("");if(manager)void api<unknown>(`/api/v1/properties/${propertyId}/people/${personId}`,{signal:controller.signal}).then(value=>parsePerson(value,propertyId,personId)).then(value=>{if(!controller.signal.aborted)setPerson(value);}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));});return()=>controller.abort();},[api,propertyId,personId,revision,locale,manager]);
 if(!manager)return <div className="py-10"><MessageBanner>{t("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 if(error||scope.error)return <div className="py-10"><MessageBanner action={<Button variant="view" onClick={()=>{setRevision(n=>n+1);scope.refresh();}}>{t("tryAgain")}</Button>}>{error||scope.error}</MessageBanner></div>;
 if(!person||!scope.property)return <div className="py-10" role="status"><span className="sr-only">{t("loading")}</span><Skeleton className="h-24"/></div>;
 return <div className="py-8"><WorkspaceHeading title={personName(person)} description={m("multipleRolesExplanation")} scope={scope.property.name} icon={Users} action={<Button onClick={()=>setActionsOpen(true)}>{t("personActions")}</Button>}/><section aria-label={t("personInformation")} className="mt-6 rounded-2xl border bg-card p-5 shadow-sm"><div className="flex items-center justify-between gap-3"><div className="flex min-w-0 items-center gap-3">{/* İŞ19: the linked account's own photo (shared with mobile); initials when there is no account or photo. */}<AccountAvatar key={person.linkedPhotoId??"none"} path={person.linkedPhotoId?`/api/v1/properties/${propertyId}/people/${personId}/photo`:null} name={personName(person)} className="size-14 text-base"/><h2 className="text-base font-semibold">{t("personInformation")}</h2></div><div className="flex items-center gap-2"><StatusBadge enabled={person.isEnabled}/>{scope.property.isEnabled&&<Button variant="warn" size="sm" onClick={()=>setCodeOpen(true)}><KeyRound/>{t("personCodeRegenerate")}</Button>}</div></div><dl className="mt-4 grid gap-4 sm:grid-cols-2"><DetailField label={t("phone")}>{person.phoneNumber??t("noPhone")}</DetailField><DetailField label={t("email")}>{person.email??t("noEmail")}</DetailField><PersonProfileDetails person={person}/></dl></section><PersonConsents userId={person.linkedUserId}/>{codeOpen&&<RegeneratePersonCodeDialog person={person} propertyId={propertyId} onClose={()=>setCodeOpen(false)} onRenewed={setPerson}/>}{actionsOpen&&<PersonActionsPanel person={person} propertyId={propertyId} onClose={()=>setActionsOpen(false)}/>}<PersonOccupancies key={person.id} propertyId={propertyId} person={person} parentEnabled={scope.property.isEnabled} revision={revision} onChanged={()=>setRevision(n=>n+1)} onClose={()=>router.push(`/properties/${propertyId}/people`)}/></div>;
}
