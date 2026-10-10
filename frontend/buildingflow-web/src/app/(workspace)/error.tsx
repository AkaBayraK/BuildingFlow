"use client";
import {useTranslations} from "next-intl";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {Button} from "@/components/ui/button";
export default function ErrorPage({reset}:{reset:()=>void}){const t=useTranslations("ui");return <div className="py-10"><MessageBanner action={<Button variant="outline" onClick={reset}>{t("tryAgain")}</Button>}>{t("unexpectedPage")}</MessageBanner></div>;}
