"use client";
import {useEffect,useRef,useState} from 'react';
import {useLocale,useTranslations} from 'next-intl';
import {CalendarPlus,RefreshCw,XCircle,Pencil} from 'lucide-react';
import {useAuthorizedApi,errorMessage,ApiError} from '@/services/api';
import {useNotification} from '@/components/feedback/Notifications';
import {Button} from '@/components/ui/button';import {Input} from '@/components/ui/input';import {Label} from '@/components/ui/label';
import {ToneBadge} from '@/components/management/WorkspacePresentation';
import {MessageBanner,FieldError} from '@/components/feedback/MessageBanner';
import {areaPath,readSchedule,readReservation,segments,istanbulToday,blankReservation,reservationErrors,reservationBody,type Area,type Context,type Schedule,type Reservation,type ReservationForm} from './model';
import {segmentStyle} from './type-style';
import {CommonAreaManagerCalendar} from './CommonAreaManagerCalendar';
import {CommonAreaMaintenance} from "./CommonAreaMaintenance";
export function CommonAreaSchedule({propertyId,context,area,canWrite,onChanged,onBusy}:{propertyId:string;context?:Context;area:Area;canWrite:boolean;onChanged:()=>void;onBusy?:(v:boolean)=>void}){
 const t=useTranslations('commonAreas'),ui=useTranslations('ui'),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();
 const manager=canWrite&&!context,[date,setDate]=useState(istanbulToday()),[schedule,setSchedule]=useState<Schedule|null>(null),[loadedAt,setLoadedAt]=useState<Date|null>(null);
 const [loading,setLoading]=useState(true),[error,setError]=useState(''),[revision,setRevision]=useState(0),[busy,setBusy]=useState(false),[form,setForm]=useState<ReservationForm>(()=>blankReservation(istanbulToday())),[errors,setErrors]=useState<string[]>([]),[editing,setEditing]=useState<Reservation|null>(null),[cancelling,setCancelling]=useState<Reservation|null>(null),[reason,setReason]=useState('');
 const lock=useRef(false),operation=useRef<{key:string;id:string}|null>(null);
 useEffect(()=>{const c=new AbortController();setLoading(true);setSchedule(null);setError('');
  void (async()=>{const q=new URLSearchParams({date});const s=readSchedule(await api<unknown>(areaPath(propertyId,context,area.id,'/schedule',q),{signal:c.signal}),area.id,date);
   if(!c.signal.aborted){setSchedule(s);setLoadedAt(new Date());}})().catch(e=>{if(!c.signal.aborted){setSchedule(null);setError(errorMessage(e,locale));}}).finally(()=>{if(!c.signal.aborted)setLoading(false);});
  return()=>c.abort();
 },[api,propertyId,context?.relationId,area.id,date,revision,manager,locale]);
 useEffect(()=>{if(!loading&&schedule){if(editing)document.getElementById('booking-startsAt')?.focus();else if(cancelling)document.getElementById('booking-reason')?.focus();}},[loading,schedule,editing,cancelling]);
 const set=<K extends keyof ReservationForm>(k:K,v:ReservationForm[K])=>{if(!busy){setForm(f=>({...f,[k]:v}));setErrors([]);}};
 async function create(e:React.FormEvent){e.preventDefault();if(lock.current||!manager)return;const draft={...form,date:editing?form.date:date};const invalid=reservationErrors(draft,editing?undefined:schedule??undefined);setErrors(invalid);if(invalid.length){document.getElementById('booking-'+invalid[0])?.focus();return;}
  const key=JSON.stringify(draft);if(operation.current?.key!==key)operation.current={key,id:crypto.randomUUID()};lock.current=true;setBusy(true);onBusy?.(true);
  try{readReservation(await api<unknown>(areaPath(propertyId,undefined,area.id,editing?'/reservations/'+editing.id:'/reservations'),{method:editing?'PUT':'POST',body:JSON.stringify({...reservationBody(draft,editing?.requestId??operation.current.id),version:editing?.version??0,unitId:editing?.unitId??null})}),propertyId,area.id);operation.current=null;const changed=!!editing;setEditing(null);setForm(blankReservation(draft.date));setDate(draft.date);notice.success(t(changed?'bookingUpdated':'bookingCreated'));setRevision(n=>n+1);onChanged();}
  catch(e){if(e instanceof ApiError&&e.status<500)operation.current=null;notice.error(errorMessage(e,locale));if(e instanceof ApiError&&e.status===409){setRevision(n=>n+1);if(editing){setEditing(null);setForm(blankReservation(date));}}}
  finally{lock.current=false;setBusy(false);onBusy?.(false);}
 }
 async function cancel(e:React.FormEvent){e.preventDefault();if(!cancelling||lock.current||!manager)return;if(!reason.trim()||reason.trim().length>500){setErrors(['reason']);document.getElementById('booking-reason')?.focus();return;}lock.current=true;setBusy(true);onBusy?.(true);
  try{readReservation(await api<unknown>(areaPath(propertyId,undefined,area.id,`/reservations/${cancelling.id}/cancel`),{method:'POST',body:JSON.stringify({reason:reason.trim(),version:cancelling.version})}),propertyId,area.id);setCancelling(null);setReason('');notice.success(t('bookingCancelled'));setRevision(n=>n+1);onChanged();}
  catch(e){notice.error(errorMessage(e,locale));if(e instanceof ApiError&&e.status===409){setCancelling(null);setReason('');}setRevision(n=>n+1);}finally{lock.current=false;setBusy(false);onBusy?.(false);}
 }
 const parts=schedule?segments(schedule):[];const time=loadedAt?new Intl.DateTimeFormat(locale,{hour:'2-digit',minute:'2-digit',second:'2-digit',timeZone:'Europe/Istanbul'}).format(loadedAt):'';
 return <section className="space-y-4" aria-label={t('schedule')}>
  {manager&&<CommonAreaManagerCalendar propertyId={propertyId} areaId={area.id} initialDate={date} revision={revision} busy={busy} onSelect={day=>{if(!busy){setDate(day);setForm(blankReservation(day));setEditing(null);setCancelling(null);}}} onEdit={r=>{if(!busy){setDate(r.date);setEditing(r);setCancelling(null);setErrors([]);setForm({date:r.date,startsAt:r.startsAt,endsAt:r.endsAt,partySize:String(r.partySize),note:r.note??''});}}} onCancel={r=>{if(!busy){setDate(r.date);setEditing(null);setCancelling(r);setReason('');setErrors([]);}}}/>}
  <div className="flex flex-wrap items-end gap-3"><div><Label htmlFor="schedule-date">{t('scheduleDate')}</Label><Input id="schedule-date" type="date" disabled={busy} value={date} onChange={e=>{if(e.target.value){setDate(e.target.value);setEditing(null);setForm(blankReservation(e.target.value));setCancelling(null);setErrors([]);}}}/></div><Button type="button" variant="outline" size="icon" aria-label={ui('refresh')} disabled={loading||busy} onClick={()=>setRevision(n=>n+1)}><RefreshCw aria-hidden className={loading?'animate-spin':''}/></Button></div>
  <MessageBanner kind="info">{t('scheduleNotice',{time:time||'—'})}</MessageBanner>
  {error&&<MessageBanner action={<Button type="button" variant="view" onClick={()=>setRevision(n=>n+1)}>{ui('tryAgain')}</Button>}>{error}</MessageBanner>}
  {loading&&!schedule?<p role="status">{ui('loading')}</p>:schedule&&<>
   {!schedule.reservable&&<MessageBanner kind="warning">{t('scheduleNotReservable')}</MessageBanner>}
   {parts.length?<ul className="space-y-2" aria-label={t('scheduleSlots')}>{parts.map(p=>{const s=segmentStyle[p.state];return <li key={p.startsAt} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border p-3"><span className="font-medium tabular-nums">{p.startsAt} – {p.endsAt}</span><ToneBadge tone={s.tone} icon={s.icon}>{p.state==='PARTIAL'?t('slotPartial',{left:schedule.capacity-p.reserved,capacity:schedule.capacity}):t('slot_'+p.state)}</ToneBadge></li>;})}</ul>:<p className="text-sm text-muted-foreground">{t('scheduleNoHours')}</p>}
  </>}
  {manager&&<CommonAreaMaintenance propertyId={propertyId} areaId={area.id} date={date} onBusy={onBusy} onChanged={()=>{setRevision(n=>n+1);onChanged();}}/>}
  {manager&&schedule&&<>
   {cancelling&&<form onSubmit={cancel} className="space-y-2 rounded-xl border p-4"><h3 className="font-semibold">{t('cancelBooking')} · {cancelling.date} {cancelling.startsAt}</h3><Label htmlFor="booking-reason">{t('cancelReason')} *</Label><Input id="booking-reason" autoFocus value={reason} maxLength={500} disabled={busy} aria-invalid={errors.includes('reason')} onChange={e=>{setReason(e.target.value);setErrors([]);}}/>{errors.includes('reason')&&<FieldError>{t('cancelReasonRequired')}</FieldError>}<div className="flex flex-wrap gap-2"><Button type="submit" variant="destructive" disabled={busy}><XCircle aria-hidden/>{t('cancelBooking')}</Button><Button type="button" variant="outline" disabled={busy} onClick={()=>{setCancelling(null);setReason('');}}>{ui('cancel')}</Button></div></form>}
   {schedule.reservable&&<form onSubmit={create} className="space-y-3 rounded-xl border p-4" noValidate><h3 className="flex items-center gap-2 font-semibold"><CalendarPlus aria-hidden className="size-4"/>{t(editing?'editBooking':'newBooking')}</h3>{editing&&<div><Label htmlFor="booking-date">{t('scheduleDate')} *</Label><Input id="booking-date" type="date" value={form.date} disabled={busy} onChange={e=>set('date',e.target.value)}/></div>}
    <div className="grid gap-3 sm:grid-cols-3">{(['startsAt','endsAt'] as const).map(k=><div key={k}><Label htmlFor={'booking-'+k}>{t(k==='startsAt'?'bookingStart':'bookingEnd')} *</Label><Input id={'booking-'+k} type="time" step={900} value={form[k]} disabled={busy} aria-invalid={errors.includes(k)} onChange={e=>set(k,e.target.value)}/></div>)}
     <div><Label htmlFor="booking-partySize">{t('partySize')} *</Label><Input id="booking-partySize" type="number" inputMode="numeric" min={1} max={schedule.capacity} value={form.partySize} disabled={busy} aria-invalid={errors.includes('partySize')} onChange={e=>set('partySize',e.target.value)}/></div></div>
    <div><Label htmlFor="booking-note">{t('bookingNote')}</Label><Input id="booking-note" value={form.note} maxLength={500} disabled={busy} onChange={e=>set('note',e.target.value)}/><p className="mt-1 text-xs text-muted-foreground">{t('bookingNoteHelp')}</p></div>
    {!!errors.length&&!errors.includes('reason')&&<div role="alert"><FieldError>{t('bookingInvalid')}</FieldError></div>}
    <Button type="submit" disabled={busy}>{busy?<RefreshCw aria-hidden className="animate-spin"/>:editing?<Pencil aria-hidden/>:<CalendarPlus aria-hidden/>}{t(editing?'saveBooking':'createBooking')}</Button>{editing&&<Button type="button" variant="outline" disabled={busy} onClick={()=>{setEditing(null);setForm(blankReservation(date));setErrors([]);}}>{ui('cancel')}</Button>}</form>}
  </>}
 </section>;
}
