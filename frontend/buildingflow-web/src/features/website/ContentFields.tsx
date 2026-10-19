"use client";
import {useTranslations} from 'next-intl';
import {Plus,Trash2} from 'lucide-react';
import {Button} from '@/components/ui/button';
import {Input} from '@/components/ui/input';
import {Textarea} from '@/components/ui/textarea';
import {Label} from '@/components/ui/label';
import {PhoneInput,EmailInput} from '@/components/forms/ContactInputs';
import {toolbarSelectClass} from '@/components/management/WorkspacePresentation';
import {object,type Field} from './model';
export type MediaOption={id:string;altText:string};
export function ContentFields({fields,value,onChange,prefix='block',media=[]}:{fields:Field[];value:Record<string,unknown>;onChange:(v:Record<string,unknown>)=>void;prefix?:string;media?:MediaOption[]}){
 const t=useTranslations('website'),ui=useTranslations('ui');
 return <div className="space-y-4">{fields.map(f=>{const id=`${prefix}-${f.key}`,v=value[f.key];const set=(next:unknown)=>onChange({...value,[f.key]:next});
 if(f.children&&f.limit){const rows=Array.isArray(v)?v.filter(object):[];return <fieldset key={id} className="space-y-3 rounded-xl border p-3"><legend className="px-1 font-medium">{t(`field_${f.key}`)}</legend>{rows.map((r,i)=><div key={i} className="space-y-3 rounded-lg border p-3"><ContentFields fields={f.children!} value={r} onChange={next=>set(rows.map((old,j)=>i===j?next:old))} prefix={`${id}-${i}`} media={media}/><Button type="button" variant="danger" onClick={()=>set(rows.filter((_,j)=>j!==i))}><Trash2/>{ui('delete')}</Button></div>)}<Button type="button" variant="outline" disabled={rows.length>=f.limit} onClick={()=>set([...rows,{}])}><Plus/>{t('addItem')}</Button></fieldset>}
 if(f.children)return <fieldset key={id} className="space-y-3 rounded-xl border p-3"><legend className="px-1 font-medium">{t(`field_${f.key}`)}</legend><label className="flex gap-2"><input type="checkbox" checked={object(v)} onChange={e=>set(e.target.checked?{}:undefined)}/>{t("enableLink")}</label>{object(v)&&<ContentFields fields={f.children} value={v} onChange={set} prefix={id} media={media}/>}</fieldset>;
 return <div key={id} className="space-y-1.5"><Label htmlFor={id}>{t(`field_${f.key}`)}{f.required?' *':''}</Label>{f.kind==='body'?<Textarea id={id} value={typeof v==='string'?v:''} required={f.required} maxLength={f.max} rows={f.max&&f.max>1000?10:4} onChange={e=>set(e.target.value)}/>:f.kind==='media'?<select id={id} className={toolbarSelectClass+' w-full'} value={typeof v==='string'?v:''} onChange={e=>set(e.target.value)}><option value="">{t('noMedia')}</option>{media.map(m=><option key={m.id} value={m.id}>{m.altText}</option>)}</select>:f.kind==='icon'?<select id={id} className={toolbarSelectClass+' w-full'} value={typeof v==='string'?v:''} onChange={e=>set(e.target.value)}><option value="">{t('noIcon')}</option>{['BUILDING','HOME','USERS','SHIELD','MANAGEMENT','LEAF','PIN','POOL','GYM','PARK','CAR','STAR'].map(v=><option key={v} value={v}>{t(`icon_${v}`)}</option>)}</select>:f.key==='phone'?<PhoneInput id={id} value={typeof v==='string'?v:''} onValueChange={set} maxLength={30}/>:f.key==='email'?<EmailInput id={id} value={typeof v==='string'?v:''} onValueChange={set} maxLength={120}/>:<Input id={id} value={typeof v==='string'||typeof v==='number'?v:''} required={f.required} maxLength={f.kind==='number'?undefined:f.max} min={f.kind==='number'?1:undefined} max={f.kind==='number'?f.max:undefined} type={f.kind==='number'?'number':'text'} onChange={e=>set(f.kind==='number'?(e.target.value?Number(e.target.value):''):e.target.value)}/>}</div>;
 })}</div>
}
