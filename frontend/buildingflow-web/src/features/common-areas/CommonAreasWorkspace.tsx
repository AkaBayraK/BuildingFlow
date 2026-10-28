"use client";
import {useEffect,useMemo,useState} from 'react';
import {useLocale,useTranslations} from 'next-intl';
import type {ColumnDef} from '@tanstack/react-table';
import {Eye,Pencil,Plus,Shapes,LayoutGrid,List,Users,Clock3} from 'lucide-react';
import {useIdentity} from '@/auth/AuthGate';
import {usePropertyScope} from '@/features/navigation/PropertyScope';
import {useAuthorizedApi,errorMessage} from '@/services/api';
import {useSearch} from '@/features/people/useRows';
import {Button} from '@/components/ui/button';
import {RecordList} from '@/components/management/RecordList';
import {FilterChips} from '@/components/management/FilterChips';
import {WorkspaceHeading,ListToolbar,ToneBadge} from '@/components/management/WorkspacePresentation';
import {MessageBanner} from '@/components/feedback/MessageBanner';
import {formatDecimalCurrency,formatNumber} from '@/lib/format';
import {areaPath,readAreaPage,type Area,type AreaPage,type Context,areaTypes} from './model';
import {typeStyle,statusStyle,usageStyle,badgeTone} from './type-style';
import {CommonAreaPanel} from './CommonAreaPanel';
export function CommonAreasWorkspace(props:{propertyId:string;context?:Context;scopeName?:string}){const identity=useIdentity();return <Workspace key={(identity.subject??'')+':'+props.propertyId+':'+(props.context?.relationId??'management')} {...props}/>;}
function Workspace({propertyId,context,scopeName}:{propertyId:string;context?:Context;scopeName?:string}){
 const t=useTranslations('commonAreas'),ui=useTranslations('ui'),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity(),scope=usePropertyScope();
 const manager=!context&&['SUPER_ADMIN','SITE_ADMIN','MANAGER'].some(r=>identity.hasRealmRole(r)),canWrite=manager&&scope.property?.id===propertyId&&!!scope.property.isEnabled&&!scope.error;
 const [term,setTerm]=useState(''),search=useSearch(term),[type,setType]=useState(''),[status,setStatus]=useState(''),[page,setPage]=useState(1),[mode,setMode]=useState('cards'),[revision,setRevision]=useState(0);
 const [data,setData]=useState<AreaPage|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState(''),[panel,setPanel]=useState<{area?:Area;edit:boolean}|null>(null);
 const q=new URLSearchParams({page:String(page),pageSize:'12'});if(search)q.set('search',search);if(type)q.set('type',type);if(manager&&status)q.set('isEnabled',status);
 const path=areaPath(propertyId,context,undefined,'',q),allowed=!!context||manager;
 useEffect(()=>{setPage(1);setPanel(null);},[search,type,status]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();setData(null);setError('');setLoading(true);void api<unknown>(path,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readAreaPage(v,propertyId));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,path,propertyId,revision,allowed,locale]);
 useEffect(()=>{if(data&&page>Math.max(1,Math.ceil(data.totalCount/12)))setPage(Math.max(1,Math.ceil(data.totalCount/12)));},[data,page]);
 const refresh=()=>{setPanel(null);setRevision(n=>n+1);if(manager)scope.refresh();};
 const badge=(a:Area)=>{const s=statusStyle[a.isEnabled?'active':'inactive'];return <ToneBadge icon={s.icon} tone={badgeTone[s.tone]}>{t(a.isEnabled?'active':'inactive')}</ToneBadge>;};
 const kind=(a:Area)=>{const s=typeStyle[a.type];return <ToneBadge icon={s.icon} tone={badgeTone[s.tone]}>{t('type_'+a.type)}</ToneBadge>;};
 const actions=(a:Area)=><div className="flex flex-wrap gap-2"><Button variant="view" size="sm" aria-label={t('view')+' · '+a.name} onClick={()=>setPanel({area:a,edit:false})}><Eye aria-hidden/>{t('view')}</Button>{canWrite&&<Button variant="edit" size="sm" aria-label={t('edit')+' · '+a.name} onClick={()=>setPanel({area:a,edit:true})}><Pencil aria-hidden/>{t('edit')}</Button>}</div>;
 const columns=useMemo<ColumnDef<Area>[]>(()=>[{id:'name',header:t('name'),cell:({row})=><strong className="break-words">{row.original.name}</strong>},{id:'type',header:t('type'),cell:({row})=>kind(row.original)},{id:'capacity',header:t('capacity'),cell:({row})=>formatNumber(row.original.capacity,locale)},{id:'usage',header:t('usage'),cell:({row})=>{const Icon=usageStyle[row.original.usage];return <span className="inline-flex items-center gap-2"><Icon aria-hidden className="size-4"/>{t('usage_'+row.original.usage)}</span>;}},{id:'status',header:ui('status'),cell:({row})=>badge(row.original)},{id:'actions',header:ui('actions'),cell:({row})=>actions(row.original)}],[t,ui,locale,canWrite]);
 if(!allowed)return <MessageBanner>{t('selectContext')}</MessageBanner>;
 return <section className="space-y-5 py-8"><WorkspaceHeading title={t('title')} description={t(manager?'help':'readonly')} scope={scopeName??scope.property?.name??ui('loading')} icon={Shapes} action={manager?<Button disabled={!canWrite} onClick={()=>setPanel({edit:true})}><Plus aria-hidden/>{t('new')}</Button>:undefined}/>
  {manager&&scope.error&&<MessageBanner action={<Button variant="view" onClick={scope.refresh}>{ui('tryAgain')}</Button>}>{scope.error}</MessageBanner>}
  <div className="rounded-2xl border bg-card p-4 text-sm text-muted-foreground"><Clock3 aria-hidden className="mr-2 inline size-4"/>{t('scheduleHint')}</div>
  <div className="overflow-hidden rounded-2xl border bg-card shadow-sm"><div className="space-y-4 border-b p-4"><FilterChips label={t('type')} items={[{value:'',label:t('all'),icon:Shapes},...areaTypes.map(v=>({value:v,label:t('type_'+v),...typeStyle[v]}))]} value={type} onChange={setType}/>
   <div className="flex flex-wrap justify-between gap-3">{manager&&<FilterChips label={ui('status')} value={status} onChange={setStatus} items={[{value:'',label:t('all'),icon:Shapes},{value:'true',label:t('active'),...statusStyle.active},{value:'false',label:t('inactive'),...statusStyle.inactive}]}/>}
   <FilterChips label={t('presentation')} value={mode} onChange={setMode} items={[{value:'cards',label:t('cards'),icon:LayoutGrid},{value:'list',label:t('list'),icon:List}]}/></div></div>
   <ListToolbar term={term} onTerm={setTerm} searchLabel={t('search')} placeholder={t('search')} loading={loading} onRefresh={refresh} onReset={()=>{setTerm('');setType('');setStatus('');}} resetEnabled={!!term||!!type||!!status}/>
   <div className={mode==='cards'?'common-area-card-mode':''}><RecordList compact data={data} columns={columns} loading={loading} error={error} page={page} onPage={setPage} onRetry={refresh} empty={<><Shapes aria-hidden className="mx-auto mb-3 size-8"/><strong>{t('empty')}</strong><p className="mt-2">{t('emptyHelp')}</p></>}
    card={a=>{const Icon=typeStyle[a.type].icon;return <div className="space-y-4"><div className={'flex h-28 items-center justify-center rounded-xl bg-muted tone-'+typeStyle[a.type].tone}><Icon aria-hidden className="size-12 opacity-70"/></div><div className="flex items-start justify-between gap-3"><h2 className="break-words font-semibold">{a.name}</h2>{badge(a)}</div>{kind(a)}<div className="flex flex-wrap gap-3 text-xs text-muted-foreground"><span><Users aria-hidden className="mr-1 inline size-4"/>{formatNumber(a.capacity,locale)}</span><span><Clock3 aria-hidden className="mr-1 inline size-4"/>{a.opensAt?a.opensAt+' – '+a.closesAt:t('hoursUnset')}</span></div><p className="line-clamp-2 break-words text-sm text-muted-foreground">{a.description??t('noDescription')}</p><p className="text-sm font-medium">{formatDecimalCurrency(a.feeAmount,locale)} <span className="text-xs font-normal text-muted-foreground">{t('referenceFee')}</span></p>{actions(a)}</div>;}}/></div>
  </div>{panel&&<CommonAreaPanel key={panel.area?.id??'new'} propertyId={propertyId} context={context} target={panel.area} editing={panel.edit} canWrite={canWrite} propertyType={scope.property?.type} onClose={()=>setPanel(null)} onSaved={()=>{setRevision(n=>n+1);if(manager)scope.refresh();}}/>}</section>;
}
