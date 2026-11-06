export const automationTypes=["DUES_DUE_REMINDER","DUES_OVERDUE_REMINDER","PAYMENT_RECEIVED","CUSTOM"] as const;
export const triggers=["DATE_OFFSET","EVENT","CALENDAR"] as const;
export type Trigger=typeof triggers[number];
export type AutomationType=typeof automationTypes[number];
export const channels=["IN_APP","SMS","EMAIL"] as const;
export type Channel=typeof channels[number];
export const dispatchStatuses=["DELIVERED","QUEUED","SKIPPED","FAILED","CANCELLED"] as const;
export type DispatchStatus=typeof dispatchStatuses[number];
export type AutomationTypeInfo={type:AutomationType;defaultName:string;defaultOffsets:number[];minOffset:number;maxOffset:number;variables:string[];defaultTitle:string;defaultBody:string;smsTypeCode:string;customTypeId:string|null;icon:string|null;trigger:Trigger|null;sourceCode:string|null};
export type AutomationExecution={id:string;definitionId:string;type:AutomationType;businessDate:string;trigger:"SCHEDULE"|"EVENT"|"MANUAL";status:"SUCCEEDED"|"FAILED"|"DEAD_LETTER";attempts:number;startedAt:string;finishedAt:string|null;nextAttemptAt:string|null;evaluated:number;dispatched:number;skipped:number;errorCode:string|null;ruleVersion:number;correlationId:string};
export type Automation={id:string;propertyId:string;type:AutomationType;status:"ACTIVE"|"PAUSED";name:string;startsOn:string|null;endsOn:string|null;sendHour:number;quietStartHour:number;quietEndHour:number;offsetDays:number[];inApp:boolean;sms:boolean;email:boolean;titleTemplate:string;bodyTemplate:string;ruleVersion:number;activatedAt:string|null;nextRunAt:string|null;lastExecution:AutomationExecution|null;version:number;customTypeId:string|null;customTypeName:string|null;icon:string|null};
export type ChannelCount={channel:Channel;delivered:number;queued:number;skipped:number;failed:number;cancelled:number};
export type AutomationHealth={outboxPending:number;outboxRetrying:number;smsQueued:number;smsFailedToday:number;executionsFailed:number;executionsDeadLetter:number;lastExecutionAt:string|null;emailChannelAvailable:boolean;smsAccountEnabled:boolean;pushAvailable:boolean};
export type AutomationOverview={propertyId:string;today:string;types:AutomationTypeInfo[];definitions:Automation[];activeCount:number;pausedCount:number;executionsToday:number;channels:ChannelCount[];health:AutomationHealth};
export type Dispatch={id:string;definitionId:string;type:AutomationType;channel:Channel;status:DispatchStatus;reasonCode:string|null;unitNumber:string|null;period:string|null;offsetDays:number|null;amount:number|null;recipientMasked:string|null;title:string|null;createdAt:string;correlationId:string;ruleVersion:number;smsStatus:string|null;smsAttempts:number|null;smsErrorCode:string|null};
export type PreviewItem={unitNumber:string;period:string|null;balance:number;dueDate:string|null;offsetDays:number|null;recipients:number;channels:Channel[];title:string;body:string};
export type AutomationPreview={definitionId:string;date:string;total:number;truncated:boolean;items:PreviewItem[]};
export type Revision={id:string;ruleVersion:number;action:string;reason:string;snapshot:string;createdAt:string;createdBy:string};
export type Condition={field:string;operator:"EQ"|"IN"|"NOT_IN";values:string[]};
export type SourceInfo={code:string;fields:{name:string;options:string[]}[];dateFields:string[];triggers:Trigger[];recipientScopes:string[];variables:string[]};
export type CustomType={id:string;propertyId:string|null;isPlatform:boolean;code:string;name:string;icon:string;sourceCode:string;conditions:Condition[];trigger:Trigger;dateField:string|null;defaultOffsets:number[];dayOfMonth:number|null;dayOfWeek:number|null;recipientScopes:string[];defaultTitle:string;defaultBody:string;status:"DRAFT"|"PUBLISHED"|"ARCHIVED";typeVersion:number;ruleCount:number;canEdit:boolean;version:number};
export type TypePreview={records:number;recipients:number;truncated:boolean;items:PreviewItem[]};
export type Page<T>={items:T[];page:number;pageSize:number;totalCount:number};

const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const hour=(v:unknown)=>Number.isSafeInteger(v)&&Number(v)>=0&&Number(v)<=23;
const text=(v:unknown,nullable=false)=>typeof v==="string"||nullable&&v===null;
const instant=(v:unknown,nullable=false)=>nullable&&v===null||typeof v==="string"&&!Number.isNaN(Date.parse(v));
const day=(v:unknown,nullable=false)=>nullable&&v===null||typeof v==="string"&&/^\d{4}-\d{2}-\d{2}$/.test(v);
const ints=(v:unknown):v is number[]=>Array.isArray(v)&&v.every(x=>Number.isSafeInteger(x));
const oneOf=<T extends string>(values:readonly T[],v:unknown):v is T=>typeof v==="string"&&(values as readonly string[]).includes(v);
function fail(kind:string):never{throw new Error(`Invalid automation ${kind}`);}
// Recipients arrive masked; a response that carries a full phone number is refused rather than rendered.
const masked=(v:unknown)=>v===null||typeof v==="string"&&!/[0-9]{5}/.test(v);

const strings=(v:unknown):v is string[]=>Array.isArray(v)&&v.every(x=>typeof x==="string");
export function readType(v:unknown):AutomationTypeInfo{
 if(!obj(v)||!oneOf(automationTypes,v.type)||!text(v.defaultName)||!ints(v.defaultOffsets)||!Number.isSafeInteger(v.minOffset)||!Number.isSafeInteger(v.maxOffset)||!strings(v.variables)||!text(v.defaultTitle)||!text(v.defaultBody)||!text(v.smsTypeCode))fail("type");
 const custom=v.type==="CUSTOM";
 if(custom?!guid(v.customTypeId)||!text(v.icon)||!oneOf(triggers,v.trigger)||!text(v.sourceCode):(v.customTypeId??null)!==null)fail("type");
 return {...v,customTypeId:(v.customTypeId as string|null|undefined)??null,icon:(v.icon as string|null|undefined)??null,trigger:(v.trigger as Trigger|null|undefined)??null,sourceCode:(v.sourceCode as string|null|undefined)??null} as AutomationTypeInfo;
}
export function readSource(v:unknown):SourceInfo{
 if(!obj(v)||!text(v.code)||!Array.isArray(v.fields)||!v.fields.every(f=>obj(f)&&text(f.name)&&strings(f.options))||!strings(v.dateFields)||!Array.isArray(v.triggers)||!v.triggers.every(x=>oneOf(triggers,x))||!strings(v.recipientScopes)||!strings(v.variables))fail("source");
 return v as SourceInfo;
}
const condition=(v:unknown)=>obj(v)&&text(v.field)&&oneOf(["EQ","IN","NOT_IN"] as const,v.operator)&&strings(v.values);
export function readCustomType(v:unknown,propertyId:string):CustomType{
 if(!obj(v)||!guid(v.id)||!(v.propertyId===null||v.propertyId===propertyId)||typeof v.isPlatform!=="boolean"||v.isPlatform!==(v.propertyId===null)||!text(v.code)||!text(v.name)||!text(v.icon)||!text(v.sourceCode)||!Array.isArray(v.conditions)||!v.conditions.every(condition)||!oneOf(triggers,v.trigger)||!text(v.dateField,true)||!ints(v.defaultOffsets)||!(v.dayOfMonth===null||int(v.dayOfMonth,1))||!(v.dayOfWeek===null||int(v.dayOfWeek,1))||!strings(v.recipientScopes)||!text(v.defaultTitle)||!text(v.defaultBody)||!oneOf(["DRAFT","PUBLISHED","ARCHIVED"] as const,v.status)||!int(v.typeVersion,1)||!int(v.ruleCount)||typeof v.canEdit!=="boolean"||!int(v.version,1))fail("custom type");
 return v as CustomType;
}
export function readTypePreview(v:unknown):TypePreview{
 if(!obj(v)||!int(v.records)||!int(v.recipients)||typeof v.truncated!=="boolean"||!Array.isArray(v.items))fail("type preview");
 return {records:Number(v.records),recipients:Number(v.recipients),truncated:v.truncated,items:readPreview({definitionId:"00000000-0000-0000-0000-000000000000",date:"2000-01-01",total:0,truncated:false,items:v.items}).items};
}
export function readExecution(v:unknown):AutomationExecution{
 if(!obj(v)||!guid(v.id)||!guid(v.definitionId)||!oneOf(automationTypes,v.type)||!day(v.businessDate)||!oneOf(["SCHEDULE","EVENT","MANUAL"] as const,v.trigger)||!oneOf(["SUCCEEDED","FAILED","DEAD_LETTER"] as const,v.status)||!int(v.attempts)||!instant(v.startedAt)||!instant(v.finishedAt,true)||!instant(v.nextAttemptAt,true)||!int(v.evaluated)||!int(v.dispatched)||!int(v.skipped)||!text(v.errorCode,true)||!int(v.ruleVersion,1)||!guid(v.correlationId))fail("execution");
 return v as AutomationExecution;
}
export function readAutomation(v:unknown,propertyId:string):Automation{
 if(!obj(v)||!guid(v.id)||v.propertyId!==propertyId||!oneOf(automationTypes,v.type)||!oneOf(["ACTIVE","PAUSED"] as const,v.status)||!text(v.name)||!day(v.startsOn,true)||!day(v.endsOn,true)||!hour(v.sendHour)||!hour(v.quietStartHour)||!hour(v.quietEndHour)||!ints(v.offsetDays)||typeof v.inApp!=="boolean"||typeof v.sms!=="boolean"||typeof v.email!=="boolean"||!text(v.titleTemplate)||!text(v.bodyTemplate)||!int(v.ruleVersion,1)||!instant(v.activatedAt,true)||!instant(v.nextRunAt,true)||!int(v.version,1)||(v.type==="CUSTOM")!==guid(v.customTypeId)||!text(v.customTypeName??null,true)||!text(v.icon??null,true))fail("definition");
 return {...v,customTypeId:(v.customTypeId as string|null|undefined)??null,customTypeName:(v.customTypeName as string|null|undefined)??null,icon:(v.icon as string|null|undefined)??null,lastExecution:v.lastExecution===null?null:readExecution(v.lastExecution)} as Automation;
}
export function readOverview(v:unknown,propertyId:string):AutomationOverview{
 if(!obj(v)||v.propertyId!==propertyId||!day(v.today)||!Array.isArray(v.types)||!Array.isArray(v.definitions)||!int(v.activeCount)||!int(v.pausedCount)||!int(v.executionsToday)||!Array.isArray(v.channels)||!obj(v.health))fail("overview");
 const h=v.health;
 if(!int(h.outboxPending)||!int(h.outboxRetrying)||!int(h.smsQueued)||!int(h.smsFailedToday)||!int(h.executionsFailed)||!int(h.executionsDeadLetter)||!instant(h.lastExecutionAt,true)||typeof h.emailChannelAvailable!=="boolean"||typeof h.smsAccountEnabled!=="boolean"||typeof h.pushAvailable!=="boolean")fail("health");
 const counts=v.channels.map(c=>{if(!obj(c)||!oneOf(channels,c.channel)||!int(c.delivered)||!int(c.queued)||!int(c.skipped)||!int(c.failed)||!int(c.cancelled))fail("channel");return c as ChannelCount;});
 return {propertyId,today:String(v.today),types:v.types.map(readType),definitions:v.definitions.map(d=>readAutomation(d,propertyId)),activeCount:Number(v.activeCount),pausedCount:Number(v.pausedCount),executionsToday:Number(v.executionsToday),channels:counts,health:h as AutomationHealth};
}
export function readDispatch(v:unknown):Dispatch{
 if(!obj(v)||!guid(v.id)||!guid(v.definitionId)||!oneOf(automationTypes,v.type)||!oneOf(channels,v.channel)||!oneOf(dispatchStatuses,v.status)||!text(v.reasonCode,true)||!text(v.unitNumber,true)||!day(v.period,true)||!(v.offsetDays===null||Number.isSafeInteger(v.offsetDays))||!(v.amount===null||typeof v.amount==="number")||!masked(v.recipientMasked)||!text(v.title,true)||!instant(v.createdAt)||!guid(v.correlationId)||!int(v.ruleVersion,1)||!text(v.smsStatus,true)||!(v.smsAttempts===null||int(v.smsAttempts))||!text(v.smsErrorCode,true))fail("dispatch");
 return v as Dispatch;
}
export function readPreview(v:unknown):AutomationPreview{
 if(!obj(v)||!guid(v.definitionId)||!day(v.date)||!int(v.total)||typeof v.truncated!=="boolean"||!Array.isArray(v.items))fail("preview");
 const items=v.items.map(i=>{if(!obj(i)||!text(i.unitNumber)||!day(i.period,true)||typeof i.balance!=="number"||!day(i.dueDate,true)||!(i.offsetDays===null||Number.isSafeInteger(i.offsetDays))||!int(i.recipients)||!Array.isArray(i.channels)||!i.channels.every(c=>oneOf(channels,c))||!text(i.title)||!text(i.body))fail("preview item");return i as PreviewItem;});
 return {definitionId:String(v.definitionId),date:String(v.date),total:Number(v.total),truncated:v.truncated,items};
}
export function readRevision(v:unknown):Revision{
 if(!obj(v)||!guid(v.id)||!int(v.ruleVersion,1)||!text(v.action)||!text(v.reason)||!text(v.snapshot)||!instant(v.createdAt)||!text(v.createdBy))fail("revision");
 return v as Revision;
}
export function readPage<T>(v:unknown,read:(row:unknown)=>T):Page<T>{
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize))fail("page");
 return {items:v.items.map(read),page:Number(v.page),pageSize:Number(v.pageSize),totalCount:Number(v.totalCount)};
}

export const placeholders=(value:string)=>[...new Set([...value.matchAll(/\{\{\s*([A-Za-z0-9_]+)\s*\}\}/g)].map(m=>m[1]))];
export const render=(value:string,values:Record<string,string>)=>value.replace(/\{\{\s*([A-Za-z0-9_]+)\s*\}\}/g,(match,name:string)=>values[name]??match);
export const unknownVariables=(value:string,allowed:readonly string[])=>placeholders(value).filter(p=>!allowed.includes(p));
export function parseOffsets(value:string):number[]|null{
 const parts=value.split(",").map(x=>x.trim()).filter(Boolean);
 if(parts.some(p=>!/^[+-]?\d{1,2}$/.test(p)))return null;
 return [...new Set(parts.map(Number))].sort((a,b)=>a-b);
}
export function quiet(start:number,end:number,hour:number){return start!==end&&(start<end?hour>=start&&hour<end:hour>=start||hour<end);}
/** Sample values used only for the editor preview; real messages are rendered by the server from current debt. */
export const sampleValues:Record<string,string>={today:"06.11.2026",requestCategory:"Asansör",requestPriority:"Yüksek",requestStatus:"Kaydedildi",createdDate:"05.11.2026",daysOpen:"1",contractTitle:"Asansör bakım sözleşmesi",startDate:"01.01.2026",endDate:"31.12.2026",maintenanceTitle:"Asansör periyodik bakımı",maintenanceType:"Bakım",plannedDate:"08.11.2026",location:"A blok",meterName:"Su sayacı",meterKind:"Su",lastReadingDate:"01.10.2026",commonAreaName:"Toplantı salonu",reservationDate:"10.11.2026",startsAt:"18:00",endsAt:"20:00",propertyName:"Altunbayrak Rezidans",unitNumber:"22",periodLabel:"Ocak 2026",chargeLabel:"aidat",amount:"1.000,00 TL",balance:"1.000,00 TL",dueDate:"31.01.2026",daysLeft:"2",dueText:"son ödemeye 2 gün kaldı",daysOverdue:"7",paidAmount:"1.000,00 TL",paidDate:"29.01.2026",unitBalance:"0,00 TL"};
