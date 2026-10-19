"use client";
import {useCallback,useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {useRows,useSearch} from "@/features/people/useRows";
import {formatDateOnly} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {readDuesPersonChoices,type DuesCharge,type DuesPersonChoice} from "./model";

export function ChargePeople({charge,detail=false}:{charge:DuesCharge;detail?:boolean}){
 const t=useTranslations('dues');const locale=useLocale();
 if(!charge.partiesVisible)return <p className="text-xs text-muted-foreground">{t('partiesRestricted')}</p>;
 if(!charge.partiesRecorded)return <p className="text-xs text-muted-foreground">{t('legacyParties')}</p>;
 const occupants=charge.parties.filter(p=>p.type!=='OWNER');const owners=charge.parties.filter(p=>p.type==='OWNER');const debtor=charge.parties.find(p=>p.isDebtor);
 return <div className="space-y-2 text-xs wrap-anywhere">
  {detail&&<p className="text-muted-foreground">{t('snapshotHelp',{date:formatDateOnly(charge.period+'-01',locale)})}</p>}
  <p><strong>{t('occupants')}: </strong>{occupants.length?occupants.map(p=>p.personName+' ('+t('party_'+p.type)+')').join(', '):t('noRecordedOccupants')}</p>
  <p><strong>{t('owners')}: </strong>{owners.length?owners.map(p=>p.personName).join(', '):t('noRecordedOwners')}</p>
  <p><strong>{t('debtor')}: </strong>{debtor?debtor.personName+' ('+t('party_'+debtor.type)+')':t('debtor_UNIT')}</p>
  {detail&&charge.parties.map(p=><p key={p.occupancyId} className="text-muted-foreground">{p.personName}: {formatDateOnly(p.startDate,locale)} — {p.endDate?formatDateOnly(p.endDate,locale):t('snapshotOpenEnd')}</p>)}
 </div>;
}

export function DuesPersonFilter({root,propertyId,selected,onChange,revision}:{root:string;propertyId:string;selected:DuesPersonChoice|null;onChange:(value:DuesPersonChoice|null)=>void;revision:number}){
 const t=useTranslations('dues');const ui=useTranslations('ui');const [term,setTerm]=useState('');const search=useSearch(term);const [page,setPage]=useState(1);const [retry,setRetry]=useState(0);
 const decoder=useCallback((value:unknown)=>readDuesPersonChoices(value,propertyId),[propertyId]);
 const rows=useRows<DuesPersonChoice>(root+'/person-choices?'+new URLSearchParams({page:String(page),pageSize:'20',search}),revision+retry,true,decoder);
 useEffect(()=>setPage(1),[search]);
 return <div className="space-y-2 sm:col-span-2 rounded-xl border p-3">
  <Label htmlFor="dues-person-search">{t('personSearch')}</Label><Input id="dues-person-search" value={term} maxLength={200} onChange={e=>setTerm(e.target.value)} placeholder={t('personSearch')}/>
  <Label htmlFor="dues-person">{t('personFilter')}</Label><select id="dues-person" className="h-9 w-full rounded-md border bg-background px-3 text-sm" disabled={rows.loading||!!rows.error} value={selected?.id??''} onChange={e=>onChange(rows.data?.items.find(p=>p.id===e.target.value)??null)}>
   <option value="">{t('allPeople')}</option>{selected&&!rows.data?.items.some(p=>p.id===selected.id)&&<option value={selected.id}>{selected.name}</option>}{rows.data?.items.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}
  </select>
  {rows.error?<MessageBanner>{rows.error}<Button type="button" variant="view" size="sm" onClick={()=>setRetry(n=>n+1)}>{ui('tryAgain')}</Button></MessageBanner>:<p className="text-xs text-muted-foreground" role="status">{rows.loading?ui('loading'):t('personOptionsHelp',{count:rows.data?.totalCount??0})}</p>}
  <div className="flex items-center justify-between gap-2"><Button type="button" variant="outline" size="sm" disabled={rows.loading||page===1} onClick={()=>setPage(n=>n-1)}>{ui('previous')}</Button><span className="text-xs">{page}</span><Button type="button" variant="outline" size="sm" disabled={rows.loading||!!rows.error||page*20>=(rows.data?.totalCount??0)} onClick={()=>setPage(n=>n+1)}>{ui('next')}</Button></div>
 </div>;
}
