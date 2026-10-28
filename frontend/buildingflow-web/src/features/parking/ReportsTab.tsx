"use client";
import {useState} from "react";
import {useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {BarChart3,Download,RefreshCw} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {RecordList} from "@/components/management/RecordList";
import {SummaryCard} from "@/components/management/WorkspacePresentation";
import {readReport,reportCsv,type ReportDay} from "./report-model";
import {parkingRoot,useParkingRead} from "./useParking";
export function ReportsTab({propertyId,revision}:{propertyId:string;revision:number}){
 const t=useTranslations("parking"),today=new Date(Date.now()+10800000).toISOString().slice(0,10);
 const [from,setFrom]=useState(today.slice(0,8)+'01'),[through,setThrough]=useState(today),[range,setRange]=useState({from:today.slice(0,8)+'01',through:today}),[local,setLocal]=useState(0),[page,setPage]=useState(1);
 const valid=from>='2000-01-01'&&through>=from&&(Date.parse(through)-Date.parse(from))/86400000<=365;
 const list=useParkingRead(`${parkingRoot(propertyId)}/report?from=${range.from}&through=${range.through}`,v=>readReport(v,propertyId,range.from,range.through),revision+local),data=list.data?.propertyId===propertyId&&list.data.from===range.from&&list.data.through===range.through?list.data:null;
 const columns:ColumnDef<ReportDay>[]=[{accessorKey:'day',header:t('reportDay')},{accessorKey:'entries',header:t('reportEntries')},{accessorKey:'exits',header:t('reportExits')},{accessorKey:'reservations',header:t('reportReservations')}];
 function download(){if(!data||list.loading||list.error)return;const url=URL.createObjectURL(new Blob([reportCsv(data)],{type:'text/csv;charset=utf-8'}));const a=document.createElement('a');a.href=url;a.download=`parking-${range.from}-${range.through}.csv`;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
 return <section className="space-y-5"><p className="text-sm text-muted-foreground">{t('reportHelp')}</p>
  <form className="flex flex-wrap items-end gap-3" onSubmit={e=>{e.preventDefault();if(valid){setRange({from,through});setPage(1);setLocal(n=>n+1);}}}>
   <div><Label htmlFor="parking-report-from">{t('reportFrom')}</Label><Input id="parking-report-from" type="date" min="2000-01-01" value={from} onChange={e=>setFrom(e.target.value)} required/></div>
   <div><Label htmlFor="parking-report-through">{t('reportThrough')}</Label><Input id="parking-report-through" type="date" min={from} value={through} onChange={e=>setThrough(e.target.value)} required/></div>
   <Button disabled={!valid||list.loading}><RefreshCw aria-hidden="true"/>{t('refresh')}</Button><Button type="button" variant="outline" disabled={!data||list.loading||!!list.error} onClick={download}><Download aria-hidden="true"/>{t('reportCsv')}</Button>
  </form>
  {data&&!list.loading&&!list.error&&<><div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">{(['capacity','allocatedSpaces','reservedGuestSpaces','manualGuestsInside','overdueGuests'] as const).map(k=><SummaryCard key={k} icon={BarChart3} title={t(`report_${k}`)} value={String(data[k])} note={t('reportCurrent')}/>)}</div>
  <p className="text-xs text-muted-foreground">{t(data.scheduledOperationsEnabled?'reportWorkerEnabled':'reportWorkerDisabled')} {t('reportHardwareDisabled')}</p></>}
  <RecordList compact data={data?{items:data.days.slice((page-1)*20,page*20),totalCount:data.days.length,page,pageSize:20}:null} columns={columns} loading={list.loading} error={list.error} page={page} onPage={setPage} onRetry={()=>setLocal(n=>n+1)} empty={<p>{t('reportEmpty')}</p>} card={d=><div><strong>{d.day}</strong><p>{t('reportEntries')}: {d.entries}</p><p>{t('reportExits')}: {d.exits}</p><p>{t('reportReservations')}: {d.reservations}</p></div>}/>
 </section>;
}
