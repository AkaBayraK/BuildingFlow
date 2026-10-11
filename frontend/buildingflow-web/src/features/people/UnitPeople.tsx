'use client';
import {useEffect,useState} from 'react';
import Link from 'next/link';
import {useLocale,useTranslations} from 'next-intl';
import {ArrowUpRight} from 'lucide-react';
import {useAuthorizedApi,errorMessage,type Unit} from '@/services/api';
import {readUnitParties,type UnitPartiesResponse,type UnitPersonLink} from './unit-links';
export function useUnitPeople(propertyId:string,units:Unit[],enabled:boolean,revision:number){
 const api=useAuthorizedApi();const locale=useLocale();const key=propertyId+':'+units.map(unit=>unit.id+':'+unit.buildingId).sort().join(',');
 const [result,setResult]=useState<{key:string;data:UnitPartiesResponse|null;error:string;loading:boolean}|null>(null);
 useEffect(()=>{const controller=new AbortController();if(!enabled||!units.length){setResult(null);return;}
  setResult({key,data:null,error:'',loading:true});const query=new URLSearchParams();units.forEach(unit=>query.append('unitIds',unit.id));
  void api<unknown>(`/api/v1/properties/${propertyId}/unit-parties?${query}`,{signal:controller.signal}).then(value=>readUnitParties(value,propertyId,units))
   .then(data=>{if(!controller.signal.aborted)setResult({key,data,error:'',loading:false});})
   .catch(error=>{if(!controller.signal.aborted)setResult({key,data:null,error:errorMessage(error,locale),loading:false});});return()=>controller.abort();
 },[api,key,enabled,revision,locale]);
 return enabled&&result?.key===key?result:{data:null,error:'',loading:enabled&&units.length>0};
}
export function UnitPersonLinks({propertyId,links,loading,error}:{propertyId:string;links:UnitPersonLink[]|undefined;loading:boolean;error:boolean}){
 const t=useTranslations('ui');if(loading)return <span className="text-xs text-muted-foreground">{t('loading')}</span>;
 if(error||links===undefined)return <span className="text-xs text-muted-foreground">{t('unitPeopleUnavailable')}</span>;
 if(!links.length)return <span className="text-xs text-muted-foreground">{t('noCurrentUnitPerson')}</span>;
 return <div className="grid gap-1">{links.map(person=><Link key={person.personId} href={`/properties/${propertyId}/people/${person.personId}`} aria-label={t('openPersonDetails',{name:person.personName})} className="inline-flex min-h-11 min-w-0 items-center gap-1 rounded-md px-1 font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-primary"><span className="wrap-anywhere">{person.personName}</span><ArrowUpRight aria-hidden="true" className="size-3 shrink-0"/></Link>)}</div>;
}
