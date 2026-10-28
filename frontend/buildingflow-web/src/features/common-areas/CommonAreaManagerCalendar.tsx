"use client";
import {useEffect,useMemo,useState} from 'react';
import {useLocale,useTranslations} from 'next-intl';
import type {ColumnDef} from '@tanstack/react-table';
import {CalendarDays,CalendarRange,RefreshCw,Pencil,XCircle} from 'lucide-react';
import {useAuthorizedApi,errorMessage} from '@/services/api';
import {RecordList} from '@/components/management/RecordList';
import {FilterChips} from '@/components/management/FilterChips';
import {ToneBadge} from '@/components/management/WorkspacePresentation';
import {Button} from '@/components/ui/button';import {Input} from '@/components/ui/input';import {Label} from '@/components/ui/label';
import {MessageBanner} from '@/components/feedback/MessageBanner';
import {areaPath,type Reservation} from './model';
import {reservationStyle,calendarFilterStyle} from './type-style';
import {dateRange,moveDay,readAreaReport,readReservationPage,type AreaReport,type ReservationPage} from '../../../../../shared/common-area-reports';
export function CommonAreaManagerCalendar({propertyId,areaId,initialDate,revision,busy,onSelect,onEdit,onCancel}:{propertyId:string;areaId:string;initialDate:string;revision:number;busy:boolean;onSelect:(day:string)=>void;onEdit:(row:Reservation)=>void;onCancel:(row:Reservation)=>void}){
 const t=useTranslations('commonAreas'),ui=useTranslations('ui'),locale=useLocale(),api=useAuthorizedApi();
 const [from,setFrom]=useState(initialDate),[to,setTo]=useState(()=>moveDay(initialDate,6)),[range,setRange]=useState({from:initialDate,to:moveDay(initialDate,6)}),[status,setStatus]=useState(''),[page,setPage]=useState(1),[tick,setTick]=useState(0),[data,setData]=useState<{key:string;report:AreaReport;list:ReservationPage}|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState('');
 const key=JSON.stringify([propertyId,areaId,range,status,page]),current=data?.key===key?data:null;
 useEffect(()=>{const controller=new AbortController();setLoading(true);setError('');
  void Promise.all([api<unknown>(areaPath(propertyId,undefined,areaId,'/report',new URLSearchParams(range)),{signal:controller.signal}),api<unknown>(areaPath(propertyId,undefined,areaId,'/reservations',new URLSearchParams({from:range.from,to:range.to,page:String(page),pageSize:'10',...(status?{status}:{})})),{signal:controller.signal})]).then(([report,list])=>{const parsed=readAreaReport(report,propertyId,areaId,range.from,range.to),rows=readReservationPage(list,propertyId,areaId,page,10,range.from,range.to,status);if(!controller.signal.aborted)setData({key,report:parsed,list:rows});}).catch(e=>{if(!controller.signal.aborted){setData(null);setError(errorMessage(e,locale));}}).finally(()=>{if(!controller.signal.aborted)setLoading(false);});return()=>controller.abort();
 },[api,propertyId,areaId,range.from,range.to,status,page,revision,tick,locale,key]);
 const nf=new Intl.NumberFormat(locale),number=(v:number|null)=>v===null?'—':nf.format(v),percent=(v:string|null)=>v===null?'—':new Intl.NumberFormat(locale,{style:'percent',maximumFractionDigits:2}).format(Number(v)/100);
 const action=(r:Reservation)=><div className="flex flex-wrap gap-2"><Button size="sm" variant="view" disabled={busy} onClick={()=>onSelect(r.date)}><CalendarDays aria-hidden/>{t('showDay')}</Button>{r.status==='CONFIRMED'&&<><Button size="sm" variant="outline" disabled={busy} onClick={()=>onEdit(r)}><Pencil aria-hidden/>{t('edit')}</Button><Button size="sm" variant="danger" disabled={busy} onClick={()=>onCancel(r)}><XCircle aria-hidden/>{t('cancelBooking')}</Button></>}</div>;
 // Existing RecordList supplies shadcn table, TanStack rows and responsive cards.
 const columns:ColumnDef<Reservation>[]=useMemo(()=>[
  {accessorKey:'date',header:t('scheduleDate'),cell:({row})=><span className="tabular-nums">{row.original.date}<br/>{row.original.startsAt} – {row.original.endsAt}</span>},
  {accessorKey:'partySize',header:t('partySize')},{accessorKey:'status',header:ui('status'),cell:({row})=>{const v=reservationStyle[row.original.status];return <ToneBadge icon={v.icon} tone={v.tone}>{t('booking_'+row.original.status)}</ToneBadge>;}},
  {id:'actions',header:ui('actions'),cell:({row})=>action(row.original)},
 // eslint-disable-next-line react-hooks/exhaustive-deps
 ],[t,ui,busy,onSelect,onEdit,onCancel]);
 function apply(e:React.FormEvent){e.preventDefault();try{dateRange(from,to);setRange({from,to});setPage(1);setError('');}catch{setError(t('reportRangeInvalid'));}}
 return <section className="space-y-4 rounded-xl border bg-card p-4" aria-label={t('managerCalendar')}><h3 className="flex items-center gap-2 font-semibold"><CalendarRange aria-hidden className="size-5"/>{t('managerCalendar')}</h3>
 <form onSubmit={apply} className="flex flex-wrap items-end gap-3"><div><Label htmlFor="area-calendar-from">{t('reportFrom')}</Label><Input id="area-calendar-from" type="date" value={from} disabled={busy} onChange={e=>setFrom(e.target.value)}/></div><div><Label htmlFor="area-calendar-to">{t('reportTo')}</Label><Input id="area-calendar-to" type="date" value={to} disabled={busy} onChange={e=>setTo(e.target.value)}/></div><Button disabled={busy||loading} type="submit"><CalendarDays aria-hidden/>{t('showPeriod')}</Button><Button type="button" variant="outline" size="icon" aria-label={t('refreshReport')} disabled={busy||loading} onClick={()=>setTick(n=>n+1)}><RefreshCw aria-hidden/></Button></form>
 <MessageBanner kind="info">{t('reportDefinition')}</MessageBanner><MessageBanner kind="warning">{t('paymentUnavailable')}</MessageBanner>
 {error&&<MessageBanner>{error}</MessageBanner>}
 {current&&!loading&&!error&&<><p className="text-xs text-muted-foreground">{t('reportAsOf',{time:new Date(current.report.asOf).toLocaleString(locale,{timeZone:'Europe/Istanbul'}),measured:current.report.measuredDays,days:current.report.days.length})}</p><dl className="grid grid-cols-2 gap-3 sm:grid-cols-3">{[['reportConfirmed',number(current.report.totals.confirmed)],['reportCancelled',number(current.report.totals.cancelled)],['reportClosedMinutes',number(current.report.totals.closedMinutes)],['reportAvailableMinutes',number(current.report.totals.availableMinutes)],['reportBookedMinutes',number(current.report.totals.bookedMinutes)],['reportOccupancy',percent(current.report.totals.occupancyPercent)],['reportCapacityUse',percent(current.report.totals.capacityUsePercent)]].map(([label,value])=><div key={label} className="rounded-xl border bg-muted/30 p-3"><dt className="text-xs text-muted-foreground">{t(label)}</dt><dd className="mt-1 text-xl font-semibold tabular-nums">{value}</dd></div>)}</dl>
 <div className="grid grid-cols-2 gap-2 sm:grid-cols-4" aria-label={t('reportDays')}>{current.report.days.map(day=><Button key={day.date} variant="outline" className="h-auto min-h-12 flex-col whitespace-normal py-2 text-xs" disabled={busy} onClick={()=>onSelect(day.date)}><span className="flex items-center gap-1"><CalendarDays aria-hidden className="size-3"/>{day.date}</span><span>{t('dayOccupancy',{count:day.metrics.confirmed,rate:percent(day.metrics.occupancyPercent)})}</span></Button>)}</div></>}
 <FilterChips label={t('bookings')} disabled={busy} value={status} onChange={v=>{if(!busy){setStatus(v);setPage(1);}}} items={(['','CONFIRMED','CANCELLED'] as const).map(v=>({value:v,label:v?t('booking_'+v):t('all'),icon:calendarFilterStyle[v]}))}/><p className="text-xs text-muted-foreground">{t('reportFilterNotice')}</p>
 <RecordList data={current?.list??null} columns={columns} loading={loading} error={error} page={page} onPage={p=>{if(!busy)setPage(p);}} onRetry={()=>setTick(n=>n+1)} empty={t('noBookings')} card={r=><div className="space-y-2"><p className="font-medium">{r.date} · {r.startsAt} – {r.endsAt}</p><p>{t('partySizeValue',{count:r.partySize})}</p><ToneBadge icon={reservationStyle[r.status].icon} tone={reservationStyle[r.status].tone}>{t('booking_'+r.status)}</ToneBadge>{action(r)}</div>}/>
 </section>;
}
