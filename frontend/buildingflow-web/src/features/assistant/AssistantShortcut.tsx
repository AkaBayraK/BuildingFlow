"use client";
import Link from "next/link";
import {usePathname} from "next/navigation";
import {useTranslations} from "next-intl";
import {Sparkles} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {Button} from "@/components/ui/button";
import {assistantRoles} from "./model";

// Header shortcut next to the theme and language controls; shown only to the roles the assistant API accepts.
export function AssistantShortcut(){
 const t=useTranslations("assistant"),identity=useIdentity(),path=usePathname();
 if(!assistantRoles.some(role=>identity.hasRealmRole(role)))return null;
 const active=path==="/assistant";
 return <Button asChild variant="ghost" size="icon" className="theme-trigger shrink-0"><Link href="/assistant" aria-label={t("openShortcut")} title={t("openShortcut")} aria-current={active?"page":undefined}><Sparkles aria-hidden="true"/></Link></Button>;
}
