import {supplierId} from "./model.ts";
import {calendarDay} from "../maintenance/model.ts";
import {toCsv} from "../late-penalties/model.ts";
export const alertKinds=["CONTRACT_EXPIRY","WORK_ORDER_OVERDUE","MAINTENANCE_OVERDUE","MAINTENANCE_DUE"] as const;
export type AlertKind=typeof alertKinds[number];
export type SupplierAlert={kind:AlertKind;supplierId:string;supplierName:string;propertyId:string;propertyName:string;recordId:string;title:string;date:string;days:number};
export type AlertPreference={contractExpiry:boolean;workOrderOverdue:boolean;maintenanceDue:boolean;version:number};
export type PerformanceRow={supplierId:string;supplierName:string;completedWorkOrders:number;averageCompletionHours:string|null;slaSampleCount:number;slaOnTimeCount:number;maintenanceSampleCount:number;maintenanceOnTimeCount:number;onTimeRate:string|null;invoiceCount:number;invoicedTotal:string;score:string|null;scoreSampleCount:number};
export type PerformanceReport={from:string;to:string;propertyId:string|null;minimumSample:number;items:PerformanceRow[];page:number;pageSize:number;totalCount:number};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const text=(v:unknown,max:number)=>typeof v==="string"&&!!v.trim()&&v.length<=max&&!/[\u0000-\u001f\u007f]/.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const percent=(v:unknown)=>typeof v==="string"&&/^\d{1,3}\.\d$/.test(v)&&Number(v)<=100;
function page(v:unknown){if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||Number(v.pageSize)>100||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize)||v.items.length>Number(v.totalCount))throw new Error("Invalid supplier insight page");return v as Record<string,unknown>&{items:unknown[]};}
export function readPreference(v:unknown):AlertPreference{if(!obj(v)||typeof v.contractExpiry!=="boolean"||typeof v.workOrderOverdue!=="boolean"||typeof v.maintenanceDue!=="boolean"||!int(v.version))throw new Error("Invalid alert preference");return {contractExpiry:v.contractExpiry,workOrderOverdue:v.workOrderOverdue,maintenanceDue:v.maintenanceDue,version:Number(v.version)};}
export function readAlert(v:unknown):SupplierAlert{
 if(!obj(v)||!alertKinds.includes(v.kind as AlertKind)||![v.supplierId,v.propertyId,v.recordId].every(supplierId)||!text(v.supplierName,200)||!text(v.propertyName,200)||!text(v.title,200)||!calendarDay(v.date)||!Number.isSafeInteger(v.days))throw new Error("Invalid supplier alert");
 const days=Number(v.days);if(v.kind==="CONTRACT_EXPIRY"&&days<0||v.kind==="MAINTENANCE_OVERDUE"&&days>=0||v.kind==="MAINTENANCE_DUE"&&days<0||v.kind==="WORK_ORDER_OVERDUE"&&days>0)throw new Error("Invalid alert timing");
 return v as SupplierAlert;
}
export function readAlertPage(v:unknown){const p=page(v);if(typeof p.truncated!=="boolean")throw new Error("Invalid alert page");const preference=readPreference(p.preference);const items=p.items.map(readAlert);
 if(items.some(a=>a.kind==="CONTRACT_EXPIRY"&&!preference.contractExpiry||a.kind==="WORK_ORDER_OVERDUE"&&!preference.workOrderOverdue||a.kind.startsWith("MAINTENANCE")&&!preference.maintenanceDue))throw new Error("Alert ignores preference");
 return {items:items.map(a=>({...a,id:a.kind+a.recordId})),page:Number(p.page),pageSize:Number(p.pageSize),totalCount:Number(p.totalCount),truncated:p.truncated,preference};}
export function readPerformanceRow(v:unknown,minimum:number):PerformanceRow{
 if(!obj(v)||!supplierId(v.supplierId)||!text(v.supplierName,200)||![v.completedWorkOrders,v.slaSampleCount,v.slaOnTimeCount,v.maintenanceSampleCount,v.maintenanceOnTimeCount,v.invoiceCount,v.scoreSampleCount].every(x=>int(x)))throw new Error("Invalid performance row");
 if(Number(v.slaOnTimeCount)>Number(v.slaSampleCount)||Number(v.slaSampleCount)>Number(v.completedWorkOrders)||Number(v.maintenanceOnTimeCount)>Number(v.maintenanceSampleCount)||Number(v.scoreSampleCount)!==Number(v.slaSampleCount)+Number(v.maintenanceSampleCount))throw new Error("Invalid performance sample");
 if(!(v.averageCompletionHours===null||typeof v.averageCompletionHours==="string"&&/^\d+\.\d$/.test(v.averageCompletionHours))||!(v.onTimeRate===null?Number(v.scoreSampleCount)===0:percent(v.onTimeRate))||typeof v.invoicedTotal!=="string"||!/^\d+\.\d{2}$/.test(v.invoicedTotal))throw new Error("Invalid performance values");
 if(Number(v.scoreSampleCount)<minimum?v.score!==null:v.score!==v.onTimeRate)throw new Error("Score shown without enough samples");
 return v as PerformanceRow;
}
export function readPerformance(v:unknown):PerformanceReport{const p=page(v);if(!calendarDay(p.from)||!calendarDay(p.to)||String(p.to)<String(p.from)||!(p.propertyId===null||supplierId(p.propertyId))||!int(p.minimumSample,1))throw new Error("Invalid performance report");const items=p.items.map(x=>readPerformanceRow(x,Number(p.minimumSample)));if(new Set(items.map(x=>x.supplierId)).size!==items.length)throw new Error("Duplicate performance row");return {...(p as unknown as PerformanceReport),items};}
export function alertsCsv(rows:readonly SupplierAlert[],label:{kind:string;supplier:string;property:string;title:string;date:string;days:string;kinds:Record<AlertKind,string>}){return toCsv([[label.kind,label.supplier,label.property,label.title,label.date,label.days],...rows.map(a=>[label.kinds[a.kind],a.supplierName,a.propertyName,a.title,a.date,a.days])]);}
export function performanceCsv(report:PerformanceReport,label:{supplier:string;completed:string;hours:string;sla:string;maintenance:string;rate:string;invoices:string;total:string;score:string;sample:string;insufficient:string}){
 return toCsv([[label.supplier,label.completed,label.hours,label.sla,label.maintenance,label.rate,label.invoices,label.total,label.score,label.sample],...report.items.map(r=>[r.supplierName,r.completedWorkOrders,r.averageCompletionHours??"",`${r.slaOnTimeCount}/${r.slaSampleCount}`,`${r.maintenanceOnTimeCount}/${r.maintenanceSampleCount}`,r.onTimeRate??"",r.invoiceCount,r.invoicedTotal,r.score??label.insufficient,r.scoreSampleCount])]);
}
