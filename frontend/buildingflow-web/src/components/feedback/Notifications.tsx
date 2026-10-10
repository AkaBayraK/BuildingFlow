"use client";
import {useCallback,useContext,useEffect,useMemo,useRef,useState,type ReactNode} from "react";
import {useLocale,useTranslations} from "next-intl";
import {MessageBanner} from "./MessageBanner";
import type {MessageKind} from "./message-types";
import {FeedbackContext,NotificationSurfaceContext,type Notifications,type NotificationMessage} from "./FeedbackContext";
import {useFocusScopeBranch} from "@radix-ui/react-focus-scope";
import {Branch as DismissableLayerBranch} from "@radix-ui/react-dismissable-layer";
import {appendNotification,notificationTimer} from "./notification-model";
import {notificationText} from "./notification-text";
export {useNotification} from "./FeedbackContext";
// Use the focus registry already provided by the installed Radix Dialog stack.
export function NotificationFocusBridge(){useFocusScopeBranch(useContext(NotificationSurfaceContext));return null;}
type Notice={id:number;kind:MessageKind;key:string;message:NotificationMessage};
export function NotificationProvider({children}:{children:ReactNode}){
 const [notices,setNotices]=useState<Notice[]>([]);const [surface,setSurface]=useState<HTMLElement|null>(null);const serial=useRef(0);const t=useTranslations("feedback");const fieldBatch=useRef<string[]>([]);
 const remove=useCallback((id:number)=>setNotices(rows=>rows.filter(row=>row.id!==id)),[]);
 const push=useCallback((kind:MessageKind,message:NotificationMessage)=>{const key=notificationText(message,"tr");setNotices(rows=>appendNotification(rows,{id:++serial.current,kind,key,message}));},[]);
 const field=useCallback((text:string)=>{const batch=fieldBatch.current;batch.push(text);if(batch.length===1)queueMicrotask(()=>{const messages=[...new Set(fieldBatch.current)];fieldBatch.current=[];push("error",messages.join("\n"));});},[push]);
 const api=useMemo<Notifications>(()=>({success:message=>push("success",message),error:message=>push("error",message),warning:message=>push("warning",message),info:message=>push("info",message),field}),[push,field]);
 return <FeedbackContext.Provider value={api}><NotificationSurfaceContext.Provider value={surface}>{children}<DismissableLayerBranch asChild><section ref={setSurface} className="notification-stack" aria-live="off" aria-label={t("notifications")} data-notification-region>{notices.map(notice=><Toast key={notice.id} notice={notice} remove={remove}/>)}</section></DismissableLayerBranch></NotificationSurfaceContext.Provider></FeedbackContext.Provider>;
}
function Toast({notice,remove}:{notice:Notice;remove:(id:number)=>void}){
 const locale=useLocale();const timer=useRef<ReturnType<typeof notificationTimer>|null>(null);const hovered=useRef(false);const focused=useRef(false);
 useEffect(()=>{timer.current=notificationTimer(()=>remove(notice.id));return()=>timer.current?.dispose();},[notice.id,remove]);
 const pause=()=>timer.current?.pause();const resume=()=>{if(!hovered.current&&!focused.current)timer.current?.resume();};
 return <div onMouseEnter={()=>{hovered.current=true;pause();}} onMouseLeave={()=>{hovered.current=false;resume();}} onFocusCapture={()=>{focused.current=true;pause();}} onBlurCapture={event=>{if(!event.currentTarget.contains(event.relatedTarget)){focused.current=false;resume();}}}>
  <MessageBanner presentation="inline" kind={notice.kind} onClose={()=>remove(notice.id)}>{notificationText(notice.message,locale)}</MessageBanner>
 </div>;
}
