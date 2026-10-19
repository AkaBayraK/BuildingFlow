"use client";
import {useState,type ComponentProps} from "react";
import {useTranslations} from "next-intl";
import {Mail,Phone} from "lucide-react";
import {Input} from "@/components/ui/input";
import {cn} from "@/lib/utils";
import {formatPhone,normalizeEmail,phoneChars,validEmail,validPhone} from "@/lib/contact";

type Props={id:string;value:string;onValueChange:(value:string)=>void;required?:boolean;disabled?:boolean;invalid?:boolean;describedBy?:string;maxLength?:number;className?:string;hint?:boolean}&Omit<ComponentProps<"input">,"id"|"value"|"onChange"|"type">;

// İŞ19 shared phone field: phone keyboard, guided example, +90 and 3-3-2-2 grouping when the field is left.
export function PhoneInput({id,value,onValueChange,required,invalid,describedBy,maxLength=32,className,hint=true,onBlur,...rest}:Props){
 const t=useTranslations("ui");const [touched,setTouched]=useState(false);
 const wrong=touched&&!!value.trim()&&!validPhone(value);const help=`${id}-hint`,error=`${id}-format`;
 return <div className="space-y-1.5">
  <div className="relative"><Phone aria-hidden="true" className="pointer-events-none absolute left-3 top-1/2 z-10 size-4 -translate-y-1/2 text-muted-foreground"/>
   <Input {...rest} id={id} type="tel" inputMode="tel" autoComplete={rest.autoComplete??"tel"} required={required} maxLength={maxLength} placeholder={rest.placeholder??t("phoneFieldPlaceholder")}
    value={value} aria-invalid={invalid||wrong||undefined} aria-describedby={[hint?help:null,wrong?error:null,describedBy].filter(Boolean).join(" ")||undefined}
    onChange={e=>onValueChange(phoneChars(e.target.value))} onBlur={e=>{setTouched(true);onValueChange(formatPhone(value));onBlur?.(e);}} className={cn("pl-10 tabular-nums",className)}/></div>
  {hint&&<p id={help} className="text-xs text-muted-foreground">{t("phoneFieldHint")}</p>}
  {wrong&&<p id={error} role="status" className="text-xs font-medium text-destructive">{t("phoneFieldInvalid")}</p>}
 </div>;
}

// İŞ19 shared e-mail field: e-mail keyboard, no auto-capitalisation or spell check, trimmed and lower-cased when left.
export function EmailInput({id,value,onValueChange,required,invalid,describedBy,maxLength=254,className,hint=false,onBlur,...rest}:Props){
 const t=useTranslations("ui");const [touched,setTouched]=useState(false);
 const wrong=touched&&!!value.trim()&&!validEmail(value);const help=`${id}-hint`,error=`${id}-format`;
 return <div className="space-y-1.5">
  <div className="relative"><Mail aria-hidden="true" className="pointer-events-none absolute left-3 top-1/2 z-10 size-4 -translate-y-1/2 text-muted-foreground"/>
   <Input {...rest} id={id} type="email" inputMode="email" autoComplete={rest.autoComplete??"email"} autoCapitalize="none" autoCorrect="off" spellCheck={false} required={required} maxLength={maxLength} placeholder={rest.placeholder??t("emailFieldPlaceholder")}
    value={value} aria-invalid={invalid||wrong||undefined} aria-describedby={[hint?help:null,wrong?error:null,describedBy].filter(Boolean).join(" ")||undefined}
    onChange={e=>onValueChange(e.target.value.replace(/\s/g,""))} onBlur={e=>{setTouched(true);onValueChange(normalizeEmail(value));onBlur?.(e);}} className={cn("pl-10",className)}/></div>
  {hint&&<p id={help} className="text-xs text-muted-foreground">{t("emailFieldHint")}</p>}
  {wrong&&<p id={error} role="status" className="text-xs font-medium text-destructive">{t("emailFieldInvalid")}</p>}
 </div>;
}
