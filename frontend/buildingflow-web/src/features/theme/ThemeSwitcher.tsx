"use client";
import {Sun,Moon,Monitor} from "lucide-react";
import {useTranslations} from "next-intl";
import {Button} from "@/components/ui/button";
import {DropdownMenu,DropdownMenuTrigger,DropdownMenuContent,DropdownMenuLabel,DropdownMenuRadioGroup,DropdownMenuRadioItem} from "@/components/ui/dropdown-menu";
import {useTheme} from "./ThemeProvider";
import {isThemePreference} from "./theme";
const options=[{value:"light",Icon:Sun},{value:"dark",Icon:Moon},{value:"system",Icon:Monitor}] as const;
export function ThemeSwitcher(){
 const t=useTranslations("theme"),theme=useTheme(),Icon=options.find(option=>option.value===theme.preference)!.Icon;
 const label=`${t("label")}: ${t(theme.preference)}`;
 return <DropdownMenu><DropdownMenuTrigger asChild><Button type="button" variant="ghost" size="icon" className="theme-trigger shrink-0" disabled={!theme.ready} aria-label={label} title={theme.temporary?t("temporary"):label}><Icon aria-hidden="true"/></Button></DropdownMenuTrigger><DropdownMenuContent align="end"><DropdownMenuLabel>{t("label")}</DropdownMenuLabel><DropdownMenuRadioGroup value={theme.preference} onValueChange={value=>{if(isThemePreference(value))theme.choose(value);}}>{options.map(option=><DropdownMenuRadioItem key={option.value} value={option.value}><option.Icon aria-hidden="true" className="size-4"/>{t(option.value)}</DropdownMenuRadioItem>)}</DropdownMenuRadioGroup>{theme.temporary&&<p role="status" className="max-w-60 px-2 py-1 text-xs text-muted-foreground">{t("temporary")}</p>}</DropdownMenuContent></DropdownMenu>;
}
