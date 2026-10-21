"use client";
import {createContext,useCallback,useContext,useEffect,useRef,useState} from "react";
import {THEME_KEY,SYSTEM_THEME_QUERY,parseThemePreference,readThemePreference,resolveTheme,writeThemePreference,type ThemePreference,type ResolvedTheme} from "./theme";
type ThemeContextValue={preference:ThemePreference;resolved:ResolvedTheme;ready:boolean;temporary:boolean;choose:(value:ThemePreference)=>void};
const ThemeContext=createContext<ThemeContextValue|null>(null);
export function ThemeProvider({children}:{children:React.ReactNode}){
 const[preference,setPreference]=useState<ThemePreference>("system"),[resolved,setResolved]=useState<ResolvedTheme>("light"),[ready,setReady]=useState(false),[temporary,setTemporary]=useState(false);
 const current=useRef<ThemePreference>("system"),media=useRef<MediaQueryList|null>(null);
 const apply=useCallback((value:ThemePreference)=>{current.current=value;const next=resolveTheme(value,media.current?.matches??false);const root=document.documentElement;root.dataset.theme=next;root.classList.toggle("dark",next==="dark");root.style.colorScheme=next;setPreference(value);setResolved(next);},[]);
 useEffect(()=>{
  const query=window.matchMedia(SYSTEM_THEME_QUERY);media.current=query;let value:ThemePreference="system";
  try{value=readThemePreference(window.localStorage);}catch{setTemporary(true);}
  apply(value);setReady(true);
  const systemChanged=()=>apply(current.current);
  const stored=(event:StorageEvent)=>{if(event.key===THEME_KEY||event.key===null)apply(parseThemePreference(event.newValue));};
  query.addEventListener("change",systemChanged);window.addEventListener("storage",stored);
  return()=>{query.removeEventListener("change",systemChanged);window.removeEventListener("storage",stored);media.current=null;};
 },[apply]);
 const choose=(value:ThemePreference)=>{apply(value);let saved=false;try{saved=writeThemePreference(window.localStorage,value);}catch{}setTemporary(!saved);};
 return <ThemeContext.Provider value={{preference,resolved,ready,temporary,choose}}>{children}</ThemeContext.Provider>;
}
export function useTheme(){const context=useContext(ThemeContext);if(!context)throw Error("ThemeProvider required");return context;}
