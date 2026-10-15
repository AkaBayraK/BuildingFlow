"use client";
import {useEffect,useState} from "react";
import {useTranslations,useLocale} from "next-intl";
import {Bell} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {DropdownMenu,DropdownMenuTrigger,DropdownMenuContent,DropdownMenuLabel} from "@/components/ui/dropdown-menu";
import {Button} from "@/components/ui/button";
import {parseMessages,type MessagePage} from "./messages";
import {formatDate} from "@/lib/format";
import {MessageBanner} from "@/components/feedback/MessageBanner";
export function NotificationInbox(){
 const t=useTranslations("ui");const locale=useLocale();const api=useAuthorizedApi();const [open,setOpen]=useState(false);const [page,setPage]=useState(1);const [revision,setRevision]=useState(0);const [data,setData]=useState<MessagePage|null>(null);const [error,setError]=useState('');const [busy,setBusy]=useState<string|null>(null);
 useEffect(()=>{if(!open)return;const controller=new AbortController();setData(null);setError('');void api<unknown>(`/api/v1/notifications?page=${page}`,{signal:controller.signal}).then(parseMessages).then(value=>{if(!controller.signal.aborted)setData(value);}).catch(e=>{if(!controller.signal.aborted)setError(errorMessage(e,locale));});return()=>controller.abort();},[api,open,page,revision,locale]);
 async function read(id:string){if(busy)return;setBusy(id);try{await api(`/api/v1/notifications/${id}/read`,{method:'PUT'});setRevision(n=>n+1);}catch(e){setError(errorMessage(e,locale));}finally{setBusy(null);}}
 return <DropdownMenu open={open} onOpenChange={setOpen}><DropdownMenuTrigger asChild><Button variant="ghost" size="icon" aria-label={t('shellNotifications')}><Bell size={20} aria-hidden="true"/></Button></DropdownMenuTrigger><DropdownMenuContent align="end" className="w-80 max-w-[calc(100vw-24px)]"><DropdownMenuLabel>{t('shellNotifications')}</DropdownMenuLabel><div className="max-h-[60dvh] space-y-3 overflow-auto p-3">{error?<MessageBanner action={<Button size="sm" onClick={()=>setRevision(n=>n+1)}>{t('tryAgain')}</Button>}>{error}</MessageBanner>:!data?<p role="status">{t('loading')}</p>:!data.items.length?<p className="text-sm">{t('personEmpty')}</p>:data.items.map(item=><article key={item.id} className="rounded-lg border p-3 text-sm"><strong>{item.title}</strong><p className="whitespace-pre-wrap break-words">{item.body}</p><small className="text-muted-foreground">{formatDate(item.createdAt,locale)}</small>{!item.readAt&&<Button size="sm" variant="outline" className="mt-2" disabled={!!busy} onClick={()=>void read(item.id)}>{t('personRead')}</Button>}</article>)}</div><div className="flex justify-between gap-2 p-2"><Button size="sm" variant="outline" disabled={!data||!!busy||page<=1} onClick={()=>setPage(n=>n-1)}>{t('previousPage')}</Button><Button size="sm" variant="outline" disabled={!data||!!busy||page*10>=data.totalCount} onClick={()=>setPage(n=>n+1)}>{t('nextPage')}</Button></div></DropdownMenuContent></DropdownMenu>;
}
