import Link from "next/link";
import {getTranslations} from "next-intl/server";
import {Button} from "@/components/ui/button";
import {FileQuestion} from "lucide-react";
export default async function NotFound(){const t=await getTranslations("ui");return <main className="signed-out"><FileQuestion aria-hidden="true" className="mx-auto size-12 text-muted-foreground"/><h1>{t("pageNotFound")}</h1><Button asChild><Link href="/">{t("overview")}</Link></Button></main>;}
