"use client";
import {useEffect,useState} from "react";
import {useRouter} from "next/navigation";
import {useLocale,useTranslations} from "next-intl";
import {usePropertyScope} from "@/features/navigation/PropertyScope";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {Skeleton} from "@/components/ui/skeleton";
import {Button} from "@/components/ui/button";
import {PersonOccupancies} from "./PersonOccupancies";
import {WorkspaceHeading} from "@/components/management/WorkspacePresentation";
import {Users} from "lucide-react";
import {personName,type Person} from "./types";
export function PersonPage({propertyId,personId}:{propertyId:string;personId:string}){
 const t=useTranslations("ui");const m=useTranslations("management");const locale=useLocale();const router=useRouter();const scope=usePropertyScope();const api=useAuthorizedApi();const identity=useIdentity();const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));const [person,setPerson]=useState<Person|null>(null);const [error,setError]=useState("");const [revision,setRevision]=useState(0);
 useEffect(()=>{const controller=new AbortController();setPerson(null);setError("");if(manager)void api<Person>(`/api/v1/properties/${propertyId}/people/${personId}`,{signal:controller.signal}).then(value=>{if(!controller.signal.aborted)setPerson(value);}).catch(error=>{if(!controller.signal.aborted)setError(errorMessage(error,locale));});return()=>controller.abort();},[api,propertyId,personId,revision,locale,manager]);
 if(!manager)return <div className="py-10"><MessageBanner>{t("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 if(error||scope.error)return <div className="py-10"><MessageBanner action={<Button variant="outline" onClick={()=>{setRevision(n=>n+1);scope.refresh();}}>{t("tryAgain")}</Button>}>{error||scope.error}</MessageBanner></div>;
 if(!person||!scope.property)return <div className="py-10" role="status"><span className="sr-only">{t("loading")}</span><Skeleton className="h-24"/></div>;
 return <div className="py-8"><WorkspaceHeading title={t("unitRelationships")} description={m("multipleRolesExplanation")} scope={`${scope.property.name} / ${personName(person)}`} icon={Users}/><PersonOccupancies key={person.id} propertyId={propertyId} person={person} parentEnabled={scope.property.isEnabled} revision={revision} onChanged={()=>setRevision(n=>n+1)} onClose={()=>router.push(`/properties/${propertyId}/people`)}/></div>;
}
