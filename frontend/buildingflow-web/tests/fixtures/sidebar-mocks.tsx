import {propertyIdFromPath} from '../../src/features/navigation/routes';
import {useEffect,useState,type ComponentProps,type ReactNode} from 'react';

// Isolated fixture facts only. This module is never imported by production code.
export function usePathname(){const [path,setPath]=useState(window.location.pathname);useEffect(()=>{const sync=()=>setPath(window.location.pathname);window.addEventListener('popstate',sync);return()=>window.removeEventListener('popstate',sync);},[]);return path;}
export function navigate(path:string){window.history.pushState(null,'',path);window.dispatchEvent(new PopStateEvent('popstate'));}
export default function Link({href,onNavigate,children,onClick,...props}:Omit<ComponentProps<'a'>,'href'> & {href:string;onNavigate?:()=>void}){return <a {...props} href={href} onClick={event=>{onClick?.(event);if(event.defaultPrevented)return;event.preventDefault();onNavigate?.();navigate(href);}}>{children}</a>;}
export function useRouter(){return {replace:navigate,refresh:()=>{}};}
export function PropertyScopeProvider({children}:{children:ReactNode}){return children;}
export function usePropertyScope(){const propertyId=propertyIdFromPath(usePathname());return {propertyId,property:propertyId?{id:propertyId,type:'APARTMENT' as const,name:propertyId.startsWith('7777')?'Deneme Apartmanı':'İkinci Alan'}:null,loading:false,error:'',refresh:()=>{}};}
export function NotificationInbox(){return <button aria-label="Bildirimler" className="min-h-11 min-w-11">♧</button>;}
