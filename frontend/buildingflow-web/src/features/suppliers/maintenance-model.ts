import {supplierId,readSupplier} from "./model.ts";
import {calendarDay} from "../maintenance/model.ts";
export const maintenanceFrequencies=["WEEKLY","MONTHLY","QUARTERLY","SEMIANNUAL","YEARLY"] as const;
export const nonWorkingDayRules=["NEXT_WORKING_DAY","PREVIOUS_WORKING_DAY","KEEP"] as const;
export const maintenanceTypes=["MAINTENANCE","CLEANING","INSPECTION","DRILL","OTHER"] as const;
export const occurrenceStatuses=["PLANNED","COMPLETED","SKIPPED"] as const;
export const maintenanceActions=["CREATE","END","COMPLETE","SKIP"] as const;
export type MaintenanceFrequency=typeof maintenanceFrequencies[number];
export type NonWorkingDayRule=typeof nonWorkingDayRules[number];
export type OccurrenceStatus=typeof occurrenceStatuses[number];
export type MaintenanceAction=typeof maintenanceActions[number];
export type Holiday={id:string;tenantId:string;date:string;name:string;version:number};
export type PreviewDate={sequence:number;nominalDate:string;plannedDate:string;shifted:boolean;conflict:boolean};
export type MaintenancePreview={dates:PreviewDate[];conflictCount:number};
export type MaintenanceSchedule={id:string;tenantId:string;supplierId:string;propertyId:string;propertyName:string;areaServiceId:string;serviceName:string;contractId:string|null;contractTitle:string|null;title:string;description:string|null;type:typeof maintenanceTypes[number];frequency:MaintenanceFrequency;startDate:string;endDate:string;reminderDays:number;nonWorkingDayRule:NonWorkingDayRule;status:"ACTIVE"|"ENDED";endReason:string|null;plannedCount:number;completedCount:number;skippedCount:number;overdueCount:number;version:number;createdAt:string};
export type MaintenanceOccurrence={id:string;scheduleId:string;propertyId:string;propertyName:string;scheduleTitle:string;sequence:number;nominalDate:string;plannedDate:string;maintenancePlanId:string;status:OccurrenceStatus;isOverdue:boolean;isReminderDue:boolean;completedOn:string|null;delayDays:number|null;completionNote:string|null;workOrderId:string|null;skipReason:string|null;version:number};
export type MaintenanceHistory={id:string;action:MaintenanceAction;appliedVersion:number;snapshot:string;createdAt:string};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const text=(v:unknown,max:number)=>typeof v==="string"&&!!v.trim()&&v.length<=max&&!/[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(v);
const optional=(v:unknown,max:number)=>v===null||text(v,max);
const int=(v:unknown,min=0,max=Number.MAX_SAFE_INTEGER)=>Number.isSafeInteger(v)&&Number(v)>=min&&Number(v)<=max;
const stamp=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const day=(v:unknown)=>calendarDay(v)&&String(v)>="2000-01-01"&&String(v)<="2100-12-31";
const dayNumber=(v:string)=>Date.parse(v+"T00:00:00Z")/86400000;
export function readHoliday(v:unknown,tenant:string):Holiday{if(!obj(v)||!supplierId(v.id)||v.tenantId!==tenant||!day(v.date)||!text(v.name,200)||!int(v.version,1))throw new Error("Invalid holiday");return v as Holiday;}
export function readHolidayMutation(v:unknown,tenant:string){if(!obj(v)||typeof v.replayed!=="boolean")throw new Error("Invalid holiday result");return {record:readHoliday(v.record,tenant),replayed:v.replayed};}
export function readPreview(v:unknown):MaintenancePreview{
 if(!obj(v)||!Array.isArray(v.dates)||v.dates.length>120||!int(v.conflictCount))throw new Error("Invalid maintenance preview");
 const dates=v.dates.map((d,i)=>{if(!obj(d)||d.sequence!==i+1||!day(d.nominalDate)||!day(d.plannedDate)||typeof d.shifted!=="boolean"||typeof d.conflict!=="boolean"||d.shifted!==(d.nominalDate!==d.plannedDate)||Math.abs(dayNumber(String(d.plannedDate))-dayNumber(String(d.nominalDate)))>31)throw new Error("Invalid preview date");return d as PreviewDate;});
 if(dates.filter(d=>d.conflict).length!==v.conflictCount)throw new Error("Invalid conflict count");return {dates,conflictCount:Number(v.conflictCount)};
}
export function readSchedule(v:unknown,tenant:string,supplier:string):MaintenanceSchedule{
 if(!obj(v)||!supplierId(v.id)||v.tenantId!==tenant||v.supplierId!==supplier||!supplierId(v.propertyId)||!supplierId(v.areaServiceId)||!text(v.propertyName,200)||!text(v.serviceName,200)||!text(v.title,200)||!optional(v.description,2000)||!int(v.version,1)||!stamp(v.createdAt))throw new Error("Invalid maintenance schedule");
 if((v.contractId===null)!==(v.contractTitle===null)||!(v.contractId===null||supplierId(v.contractId))||!optional(v.contractTitle,200)||!maintenanceTypes.includes(v.type as never)||!maintenanceFrequencies.includes(v.frequency as never)||!nonWorkingDayRules.includes(v.nonWorkingDayRule as never))throw new Error("Invalid maintenance schedule");
 if(!day(v.startDate)||!day(v.endDate)||String(v.endDate)<String(v.startDate)||dayNumber(String(v.endDate))-dayNumber(String(v.startDate))>731||!int(v.reminderDays,0,60))throw new Error("Invalid maintenance period");
 if(!["ACTIVE","ENDED"].includes(String(v.status))||(v.status==="ENDED")!==(v.endReason!==null)||!optional(v.endReason,1000)||[v.plannedCount,v.completedCount,v.skippedCount,v.overdueCount].some(n=>!int(n,0,120))||Number(v.overdueCount)>Number(v.plannedCount)||v.status==="ENDED"&&v.plannedCount!==0)throw new Error("Invalid maintenance status");
 return v as MaintenanceSchedule;
}
export function readOccurrence(v:unknown,schedule?:string):MaintenanceOccurrence{
 if(!obj(v)||!supplierId(v.id)||!supplierId(v.scheduleId)||schedule&&v.scheduleId!==schedule||!supplierId(v.propertyId)||!supplierId(v.maintenancePlanId)||!text(v.propertyName,200)||!text(v.scheduleTitle,200)||!int(v.sequence,1,120)||!day(v.nominalDate)||!day(v.plannedDate)||!int(v.version,1))throw new Error("Invalid maintenance occurrence");
 if(!occurrenceStatuses.includes(v.status as OccurrenceStatus)||typeof v.isOverdue!=="boolean"||typeof v.isReminderDue!=="boolean"||v.status!=="PLANNED"&&(v.isOverdue||v.isReminderDue)||v.isOverdue&&!v.isReminderDue)throw new Error("Invalid occurrence state");
 const completed=v.status==="COMPLETED";
 if(completed!==(day(v.completedOn)&&int(v.delayDays)&&text(v.completionNote,4000))||!completed&&(v.completedOn!==null||v.delayDays!==null||v.completionNote!==null||v.workOrderId!==null)||completed&&!(v.workOrderId===null||supplierId(v.workOrderId)))throw new Error("Invalid completion evidence");
 if(completed&&v.delayDays!==Math.max(0,dayNumber(String(v.completedOn))-dayNumber(String(v.plannedDate))))throw new Error("Invalid delay");
 if((v.status==="SKIPPED")!==text(v.skipReason,1000)||v.status!=="SKIPPED"&&v.skipReason!==null)throw new Error("Invalid skip reason");
 return v as MaintenanceOccurrence;
}
export function readScheduleMutation(v:unknown,tenant:string,supplier:string){if(!obj(v)||typeof v.replayed!=="boolean"||!int(v.appliedVersion,1))throw new Error("Invalid schedule result");const record=readSchedule(v.record,tenant,supplier),parent=readSupplier(v.supplier,tenant);if(parent.id!==supplier||Number(v.appliedVersion)>record.version||!v.replayed&&v.appliedVersion!==record.version)throw new Error("Invalid schedule version");return {record,supplier:parent,replayed:v.replayed};}
export function readOccurrenceMutation(v:unknown,id:string){if(!obj(v)||typeof v.replayed!=="boolean"||!int(v.appliedVersion,1))throw new Error("Invalid occurrence result");const record=readOccurrence(v.record);if(record.id!==id||Number(v.appliedVersion)>record.version||!v.replayed&&v.appliedVersion!==record.version)throw new Error("Invalid occurrence version");return {record,replayed:v.replayed};}
export function readMaintenanceHistory(v:unknown):MaintenanceHistory{if(!obj(v)||!supplierId(v.id)||!maintenanceActions.includes(v.action as MaintenanceAction)||!int(v.appliedVersion,1)||typeof v.snapshot!=="string"||v.snapshot.length>20000||!stamp(v.createdAt))throw new Error("Invalid maintenance history");return v as MaintenanceHistory;}
export type ScheduleForm={title:string;description:string;type:typeof maintenanceTypes[number];frequency:MaintenanceFrequency;startDate:string;endDate:string;reminderDays:string;rule:NonWorkingDayRule;contractId:string};
export function scheduleForm():ScheduleForm{return {title:"",description:"",type:"MAINTENANCE",frequency:"MONTHLY",startDate:"",endDate:"",reminderDays:"7",rule:"NEXT_WORKING_DAY",contractId:""};}
export function invalidScheduleField(form:ScheduleForm):keyof ScheduleForm|null{
 if(!text(form.title.trim(),200))return "title";if(form.description.trim()&&!text(form.description.trim(),2000))return "description";
 if(!day(form.startDate))return "startDate";if(!day(form.endDate)||form.endDate<form.startDate||dayNumber(form.endDate)-dayNumber(form.startDate)>731)return "endDate";
 if(!/^\d{1,2}$/.test(form.reminderDays)||Number(form.reminderDays)>60)return "reminderDays";return null;
}
export function scheduleBody(form:ScheduleForm,propertyId:string,areaServiceId:string){return {propertyId,areaServiceId,contractId:form.contractId||null,title:form.title.trim(),description:form.description.trim()||null,type:form.type,frequency:form.frequency,startDate:form.startDate,endDate:form.endDate,reminderDays:Number(form.reminderDays),nonWorkingDayRule:form.rule};}
