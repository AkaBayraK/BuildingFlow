import {notFound} from "next/navigation";
import {getTranslations} from "next-intl/server";
import {SessionWorkspaces} from "@/features/session-workspaces/SessionWorkspaces";
import {MessageBanner} from "@/components/feedback/MessageBanner";
export const dynamic="force-dynamic";
export const metadata={robots:{index:false,follow:false}};
export default async function Page({params}:{params:Promise<{siteKey:string;audience:string}>}){
 const {siteKey,audience}=await params;
 if(!/^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$(?![\s\S])/.test(siteKey)||!['all','management','resident'].includes(audience))notFound();
 const t=await getTranslations("websiteEntry");
 if(process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED!=='true')return <MessageBanner kind="info">{t('unavailable')}</MessageBanner>;
 return <><p className="mx-auto max-w-5xl pt-4 text-sm text-muted-foreground">{t(audience==='all'?'all':audience==='management'?'management':'resident')}</p><SessionWorkspaces website={{siteKey,audience}}/></>;
}
