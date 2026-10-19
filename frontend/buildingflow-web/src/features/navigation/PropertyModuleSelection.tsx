"use client";
import {useTranslations} from "next-intl";
import {useIdentity} from "@/auth/AuthGate";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {PropertiesWorkspace} from "@/features/properties/PropertiesWorkspace";

export function PropertyModuleSelection({section}:{section:"units"|"people"|"dues"|"payments"|"expenses"|"announcements"|"documents"|"meters"|"reports"|"sms"|"requests"|"website"}){
 const t=useTranslations("ui");const identity=useIdentity();
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
 if((section==="dues"||section==="payments"||section==="expenses"||section==="meters"||section==="reports") && !["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(role=>identity.hasRealmRole(role)))return <div className="py-10"><h1>{t(section==="meters"?"navMeters":section==="reports"?"navReports":section==="expenses"?"navExpenses":section==="payments"?"navPayments":"navDues")}</h1><MessageBanner>{t("financeRoleRequired")}</MessageBanner></div>;
 if((section==="website"||section==="people"||section==="sms"||section==="requests") && !manager)return <div className="py-10"><h1>{t(section==="website"?"navWebsite":section==="requests"?"shellRequests":section==="sms"?"navSms":"people")}</h1><MessageBanner>{t("aManagementRoleIsRequiredToViewPersonalInformation")}</MessageBanner></div>;
 return <PropertiesWorkspace destination={section}/>;
}
