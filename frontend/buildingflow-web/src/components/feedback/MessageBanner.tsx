"use client";
import {Children,isValidElement,useEffect,type ReactNode} from "react";
import {CircleCheck,CircleX,TriangleAlert,Info,X} from "lucide-react";
import {useLocale,useTranslations} from "next-intl";
import {translateUiText} from "@/i18n/translate-text";
import {Button} from "@/components/ui/button";
import {cn} from "@/lib/utils";
import {messageSemantics,type MessageKind} from "./message-types";
import {useNotification} from "./FeedbackContext";
const icons={success:CircleCheck,error:CircleX,warning:TriangleAlert,info:Info};
function splitContent(children:ReactNode){
 const text:string[]=[];const actions:ReactNode[]=[];
 function walk(nodes:ReactNode){Children.forEach(nodes,node=>{if(typeof node==="string"||typeof node==="number")text.push(String(node));else if(isValidElement<{children?:ReactNode;onClick?:unknown;href?:unknown;asChild?:boolean}>(node)){
  if(node.type===Button||node.type==="button"||node.type==="a"||node.props.onClick||node.props.href||node.props.asChild)actions.push(node);else walk(node.props.children);
 }});}walk(children);return {text:text.join(" ").trim(),actions};
}
export function MessageBanner({kind="error",children,title,action,onClose,className,id,announce=true,presentation="notification"}:{kind?:MessageKind;children:ReactNode;title?:string;action?:ReactNode;onClose?:()=>void;className?:string;id?:string;announce?:boolean;presentation?:"notification"|"inline"}){
 const locale=useLocale();const t=useTranslations("feedback");const Icon=icons[kind];const semantics=messageSemantics[kind];
 const notify=useNotification();const content=splitContent(children);const text=translateUiText(content.text,locale);const floating=presentation==="notification"&&(kind==="error"||kind==="warning");
 useEffect(()=>{if(floating&&text)notify[kind](text);},[floating,text,kind,notify]);
 if(floating)return <><span id={id} className="sr-only">{text}</span>{(action||content.actions.length>0)&&<div className={cn("feedback-actions",className)}>{action}{content.actions}</div>}</>;
 return <div id={id} data-message-kind={kind} role={announce?semantics.role:undefined} aria-live={announce?semantics.live:undefined} aria-atomic={announce?true:undefined} className={cn("message-banner",className)}>
  <Icon aria-hidden="true" className="message-icon size-5 shrink-0"/>
  <div className="min-w-0 flex-1"><strong className="block text-sm">{title ?? t(kind)}</strong><div className="mt-1 text-sm leading-6 wrap-anywhere">{Children.map(children,child=>typeof child==="string"?translateUiText(child,locale):child)}</div>{action && <div className="mt-3">{action}</div>}</div>
  {onClose && <Button type="button" size="icon-sm" variant="ghost" aria-label={t("dismiss")} onClick={onClose}><X aria-hidden="true"/></Button>}
 </div>;
}
export function FieldError({id,children}:{id?:string;children:ReactNode}){
 const locale=useLocale();const notify=useNotification();const text=translateUiText(splitContent(children).text,locale);
 useEffect(()=>{if(text)notify.field(text);},[text,notify]);
 return <p id={id} className="field-error sr-only">{text}</p>;
}
