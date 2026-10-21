export const THEME_KEY="buildingflow.theme";
export const SYSTEM_THEME_QUERY="(prefers-color-scheme: dark)";
export type ThemePreference="light"|"dark"|"system";
export type ResolvedTheme="light"|"dark";
export const isThemePreference=(value:unknown):value is ThemePreference=>value==="light"||value==="dark"||value==="system";
export const parseThemePreference=(value:unknown):ThemePreference=>isThemePreference(value)?value:"system";
export const resolveTheme=(preference:ThemePreference,systemDark:boolean):ResolvedTheme=>preference==="system"?(systemDark?"dark":"light"):preference;
export function readThemePreference(storage:Pick<Storage,"getItem">):ThemePreference{try{return parseThemePreference(storage.getItem(THEME_KEY));}catch{return "system";}}
export function writeThemePreference(storage:Pick<Storage,"setItem">,preference:ThemePreference):boolean{if(!isThemePreference(preference))return false;try{storage.setItem(THEME_KEY,preference);return true;}catch{return false;}}
// Constant code only: storage values are validated, never interpolated into JavaScript/HTML.
// RootLayout supplies the per-request CSP nonce. Run before body paint to prevent a light flash.
export const themeBootstrap=`(()=>{let p="system",systemDark=false;try{const v=localStorage.getItem("buildingflow.theme");if(v==="light"||v==="dark"||v==="system")p=v;}catch{}try{systemDark=matchMedia("(prefers-color-scheme: dark)").matches;}catch{}const t=p==="system"?(systemDark?"dark":"light"):p,r=document.documentElement;r.dataset.theme=t;r.classList.toggle("dark",t==="dark");r.style.colorScheme=t;})();`;
