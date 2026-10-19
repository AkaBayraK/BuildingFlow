"use client";
import {useMemo} from "react";
import {useLocale,useTranslations} from "next-intl";
import {UserRound,Globe,VenusAndMars,Venus,Mars,IdCard,Phone,Bell,MapPin,type LucideIcon} from "lucide-react";
import {EmailInput,PhoneInput} from "@/components/forms/ContactInputs";
import {Input} from "@/components/ui/input";
import {Textarea} from "@/components/ui/textarea";
import {Label} from "@/components/ui/label";
import {FieldError} from "@/components/feedback/MessageBanner";
import {PersonAccountPicker} from "./PersonAccountPicker";
import {countryChoices} from "./countries";
type Props={central?:boolean;values:Record<string,string>;errors:Record<string,string[]>;url:string;busy:boolean;onChange:(key:string,value:string)=>void;onClassification:(key:string,value:string)=>void};
export function PersonFormFields({central=false,values,errors,url,busy,onChange,onClassification}:Props){
 const t=useTranslations('ui');const locale=useLocale();const countries=useMemo(()=>countryChoices(locale),[locale]);const natural=values.personKind==='NATURAL';const foreign=values.citizenship==='FOREIGN';
 const identityLabel=foreign?(natural?'personPassport':'personForeignCompanyId'):natural?'personNationalId':'personTaxId';
 const genderIcon=values.gender==='FEMALE'?Venus:values.gender==='MALE'?Mars:VenusAndMars;
 const errorFor=(key:string)=>errors[key]?.length?<FieldError id={`entity-error-${key}`}>{errors[key].join(' ')}</FieldError>:null;
 function field(key:string,label:string,{type='text',required=false,max,placeholder,Icon}:{type?:string;required?:boolean;max?:number;placeholder?:string;Icon?:LucideIcon}={}){
  return <div className="person-form-field"><Label htmlFor={`entity-${key}`}>{t(label)}{required?' *':''}</Label><div className="relative">{Icon&&<Icon aria-hidden="true" className="pointer-events-none absolute left-3 top-1/2 z-10 size-4 -translate-y-1/2 text-muted-foreground"/>}<Input id={`entity-${key}`} type={type} required={required} maxLength={max} value={values[key]} placeholder={placeholder?t(placeholder):undefined} aria-invalid={!!errors[key]?.length} aria-describedby={errors[key]?.length?`entity-error-${key}`:undefined} onChange={event=>onChange(key,event.target.value)} className={`person-form-input ${Icon?'pl-10':''}`}/></div>{errorFor(key)}</div>;
 }
 // İŞ19: phone and e-mail use the shared contact fields (guided +90 phone, e-mail keyboard); errors stay under the field.
 function contact(key:'phoneNumber'|'email',label:string,max:number,required=true){
  const Field=key==='phoneNumber'?PhoneInput:EmailInput;const errorId=errors[key]?.length?`entity-error-${key}`:undefined;
  return <div className="person-form-field"><Label htmlFor={`entity-${key}`}>{t(label)}{required?' *':''}</Label><Field id={`entity-${key}`} required={required} maxLength={max} value={values[key]} invalid={!!errors[key]?.length} describedBy={errorId} onValueChange={value=>onChange(key,value)} className="person-form-input"/>{errorFor(key)}</div>;
 }
 function select(key:string,label:string,options:string[][],Icon:LucideIcon,onSelect=onClassification){
  return <div className="person-form-field"><Label htmlFor={`entity-${key}`} className="flex items-center gap-2"><Icon aria-hidden="true" className="size-4 text-primary"/>{t(label)}</Label><select id={`entity-${key}`} className="person-form-select" value={values[key]} aria-invalid={!!errors[key]?.length} aria-describedby={errors[key]?.length?`entity-error-${key}`:undefined} onChange={event=>onSelect(key,event.target.value)}>{options.map(([value,text])=><option key={value} value={value}>{t(text)}</option>)}</select>{errorFor(key)}</div>;
 }
 function heading(label:string,Icon:LucideIcon){return <h3 className="person-form-section-title"><Icon aria-hidden="true"/><span>{t(label)}</span><span className="person-form-divider"/></h3>;}
 return <div className="person-form-sections">
  <div className="person-form-grid">{select('personKind','personKind',[['NATURAL','personNatural'],['LEGAL','personLegal']],UserRound)}{select('citizenship','personCitizenship',[['DOMESTIC','personDomestic'],['FOREIGN','personForeign']],Globe)}</div>
  <section aria-label={t('personIdentityHeading')}>{heading('personIdentityHeading',IdCard)}<div className="person-form-grid"><div className={natural?'':'sm:col-span-2'}>{field('firstName',natural?'firstName':'personCompanyName',{required:true,max:100,placeholder:natural?'personFirstNamePlaceholder':'personCompanyPlaceholder'})}</div>{natural&&field('lastName','lastName',{required:true,max:100,placeholder:'personLastNamePlaceholder'})}{field('identityNumber',identityLabel,{required:true,max:foreign?natural?30:50:natural?11:10,placeholder:foreign?'personForeignIdPlaceholder':natural?'personNationalIdPlaceholder':'personTaxIdPlaceholder'})}<div className="person-form-field"><Label htmlFor="entity-countryCode" className="flex items-center gap-2"><Globe aria-hidden="true" className="size-4 text-primary"/>{t('personCountryLabel')} *</Label><select id="entity-countryCode" className="person-form-select" value={values.countryCode} required disabled={!foreign} aria-invalid={!!errors.countryCode?.length} aria-describedby={errors.countryCode?.length?'entity-error-countryCode':undefined} onChange={e=>onChange('countryCode',e.target.value)}>{foreign&&<option value="">{t('personCountryChoose')}</option>}{countries.filter(c=>foreign?c.code!=='TR':c.code==='TR').map(c=><option key={c.code} value={c.code}>{c.flag} {c.name}</option>)}</select>{errorFor('countryCode')}</div>{natural&&<>{field('birthDate','personBirthDate',{type:'date'})}{select('gender','personGender',[['','notSpecified'],['FEMALE','personFemale'],['MALE','personMale'],['OTHER','personOther'],['UNSPECIFIED','personPreferNotToSay']],genderIcon,onChange)}</>}</div></section>
  <section aria-label={t('personContactHeading')}>{heading('personContactHeading',Phone)}<div className="person-form-grid">{contact('phoneNumber','phone',30,!central)}{contact('email','email',254,!central)}{!central&&<div className="person-form-field sm:col-span-2"><Label htmlFor="entity-address" className="flex items-center gap-2"><MapPin aria-hidden="true" className="size-4 text-primary"/>{t('address')}</Label><Textarea id="entity-address" maxLength={500} value={values.address} placeholder={t('personAddressPlaceholder')} aria-invalid={!!errors.address?.length} aria-describedby={errors.address?.length?'entity-error-address':undefined} onChange={e=>onChange('address',e.target.value)} className="min-h-20 rounded-lg bg-card shadow-none"/>{errorFor('address')}</div>}</div></section>
  {!central&&<section aria-label={t('personNotificationHeading')}>{heading('personNotificationHeading',Bell)}<PersonAccountPicker url={url} value={values.linkedUserId} onChange={value=>onChange('linkedUserId',value)} error={errors.linkedUserId} disabled={busy}/></section>}
 </div>;
}
