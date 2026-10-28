// Araçlar/Otopark: runtime checks and request bodies for /api/v1/properties/{id}/parking (docs/api/parking.md).
export const spaceTypes=["STANDARD","ACCESSIBLE","VISITOR","EV_CHARGING","MOTORCYCLE"] as const;
export const vehicleTypes=["CAR","MOTORCYCLE","VAN","OTHER"] as const;
export const vehicleStatuses=["PENDING","APPROVED","REJECTED","PASSIVE"] as const;
export const guestStatuses=["PLANNED","ARRIVED","DEPARTED","CANCELLED"] as const;
export const relations=["OWNER","TENANT","RESIDENT"] as const;
export type SpaceType=typeof spaceTypes[number];
export type VehicleType=typeof vehicleTypes[number];
export type VehicleStatus=typeof vehicleStatuses[number];
export type GuestStatus=typeof guestStatuses[number];
export type Page<T>={items:T[];page:number;pageSize:number;totalCount:number};
export type Policy={propertyId:string;configured:boolean;maxVehiclesPerUnit:number;maxVehiclesPerPerson:number;maxSpacesPerUnit:number;guestMaxHours:number;retentionDays:number;version:number};
export type Level={id:string;facilityId:string;name:string;sortOrder:number;version:number};
export type Facility={id:string;buildingId:string|null;buildingName:string|null;name:string;spaceCount:number;levels:Level[];version:number};
export type Space={id:string;facilityId:string;levelId:string|null;code:string;type:SpaceType;allocatedUnitId:string|null;allocatedUnitNumber:string|null;version:number;isClosed?:boolean};
export type Vehicle={id:string;personId:string;personName:string;unitId:string;unitNumber:string;plate:string;type:VehicleType;brand:string|null;model:string|null;color:string|null;status:VehicleStatus;reviewedAt:string|null;reviewReason:string|null;version:number;createdAt:string};
export type Allocation={id:string;spaceId:string;spaceCode:string;unitId:string;unitNumber:string;vehicleId:string|null;plate:string|null;startDate:string;endDate:string|null;version:number};
export type Guest={id:string;hostUnitId:string;unitNumber:string;spaceId:string|null;spaceCode:string|null;plate:string;guestName:string|null;note:string|null;startsAt:string;endsAt:string;status:GuestStatus;arrivedAt:string|null;departedAt:string|null;version:number};
export type Closure={id:string;spaceId:string;spaceCode:string;startsAt:string;endsAt:string;reason:string;cancelledAt:string|null;cancellationReason:string|null;version:number};
export type UnitOption={id:string;number:string;buildingName:string|null};
export type ResidentOption={personId:string;name:string;relation:typeof relations[number]};
export type Plate={plate:string;unitNumber:string;buildingName:string|null;type:VehicleType};
export const limits={facility:100,level:50,code:20,text:50,reason:500,guestName:100,note:500,plate:12,maxQuota:20,maxGuestHours:168,maxRetention:365,pageSize:100} as const;

const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const optGuid=(v:unknown)=>v===null||guid(v);
const int=(v:unknown,min=0,max=Number.MAX_SAFE_INTEGER)=>Number.isSafeInteger(v)&&Number(v)>=min&&Number(v)<=max;
const text=(v:unknown,max:number)=>typeof v==="string"&&!!v.trim()&&v.length<=max;
const optText=(v:unknown,max:number)=>v===null||text(v,max);
const stamp=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const optStamp=(v:unknown)=>v===null||stamp(v);
const oneOf=<T extends string>(values:readonly T[],v:unknown):v is T=>values.includes(v as T);
export const calendarDay=(v:unknown):v is string=>typeof v==="string"&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&Number.isFinite(Date.parse(v+"T00:00:00Z"))&&new Date(v+"T00:00:00Z").toISOString().slice(0,10)===v;
const plateText=(v:unknown)=>typeof v==="string"&&/^[A-Z0-9]{2,12}$/.test(v);
function fail(kind:string):never{throw new Error(`Invalid parking ${kind}`);}

/** Same rule as the backend VehiclePlate: drop spaces, dashes and dots, upper-case, 2–12 Latin letters/digits. */
export function normalizePlate(value:string):string|null{const plate=value.replace(/[\s.-]/g,"").toUpperCase();return /^[A-Z0-9]{2,12}$/.test(plate)?plate:null;}

export function readGuestPolicy(v:unknown):{configured:boolean;guestMaxHours:number}{
 if(!obj(v)||typeof v.configured!=="boolean"||!int(v.guestMaxHours,v.configured?1:0,limits.maxGuestHours))fail("guest policy");
 return {configured:v.configured,guestMaxHours:v.guestMaxHours as number};
}
export function readPolicy(v:unknown,property:string):Policy{
 if(!obj(v)||v.propertyId!==property||typeof v.configured!=="boolean"||!int(v.maxVehiclesPerUnit,0,limits.maxQuota)||!int(v.maxVehiclesPerPerson,0,limits.maxQuota)||!int(v.maxSpacesPerUnit,0,limits.maxQuota)
  ||!int(v.guestMaxHours,0,limits.maxGuestHours)||!int(v.retentionDays,1,limits.maxRetention)||!int(v.version,v.configured?1:0))fail("policy");
 return v as Policy;
}
function readLevel(v:unknown,facility?:string):Level{
 if(!obj(v)||!guid(v.id)||!guid(v.facilityId)||(facility!==undefined&&v.facilityId!==facility)||!text(v.name,limits.level)||!int(v.sortOrder,-10,50)||!int(v.version,1))fail("level");
 return v as Level;
}
export const readLevelResponse=(v:unknown)=>readLevel(v);
export function readFacility(v:unknown):Facility{
 if(!obj(v)||!guid(v.id)||!optGuid(v.buildingId)||!optText(v.buildingName,200)||!text(v.name,limits.facility)||!int(v.spaceCount)||!int(v.version,1)||!Array.isArray(v.levels))fail("facility");
 return {...v,levels:v.levels.map(l=>readLevel(l,v.id as string))} as Facility;
}
export function readFacilities(v:unknown):Facility[]{if(!Array.isArray(v)||v.length>limits.pageSize)fail("facilities");const items=v.map(readFacility);unique(items);return items;}
export function readSpace(v:unknown):Space{
 if(!obj(v)||!guid(v.id)||!guid(v.facilityId)||!optGuid(v.levelId)||!text(v.code,limits.code)||!oneOf(spaceTypes,v.type)||!optGuid(v.allocatedUnitId)||!optText(v.allocatedUnitNumber,50)||(v.allocatedUnitId===null)!==(v.allocatedUnitNumber===null)||!int(v.version,1)||(v.isClosed!==undefined&&typeof v.isClosed!=="boolean"))fail("space");
 return v as Space;
}
export function readClosure(v:unknown):Closure{
 if(!obj(v)||!guid(v.id)||!guid(v.spaceId)||!text(v.spaceCode,limits.code)||!stamp(v.startsAt)||!stamp(v.endsAt)||Date.parse(v.endsAt as string)<=Date.parse(v.startsAt as string)||!text(v.reason,limits.reason)||!optStamp(v.cancelledAt)||!optText(v.cancellationReason,limits.reason)||(v.cancelledAt===null)!==(v.cancellationReason===null)||!int(v.version,1))fail("closure");
 return v as Closure;
}
export function readVehicle(v:unknown):Vehicle{
 if(!obj(v)||!guid(v.id)||!guid(v.personId)||!text(v.personName,201)||!guid(v.unitId)||!text(v.unitNumber,50)||!plateText(v.plate)||!oneOf(vehicleTypes,v.type)||!optText(v.brand,limits.text)||!optText(v.model,limits.text)||!optText(v.color,limits.text)
  ||!oneOf(vehicleStatuses,v.status)||!optStamp(v.reviewedAt)||!optText(v.reviewReason,limits.reason)||!int(v.version,1)||!stamp(v.createdAt))fail("vehicle");
 return v as Vehicle;
}
export function readAllocation(v:unknown):Allocation{
 if(!obj(v)||!guid(v.id)||!guid(v.spaceId)||!text(v.spaceCode,limits.code)||!guid(v.unitId)||!text(v.unitNumber,50)||!optGuid(v.vehicleId)||!(v.plate===null||plateText(v.plate))||!calendarDay(v.startDate)||!(v.endDate===null||calendarDay(v.endDate))||!int(v.version,1))fail("allocation");
 return v as Allocation;
}
export function readGuest(v:unknown):Guest{
 if(!obj(v)||!guid(v.id)||!guid(v.hostUnitId)||!text(v.unitNumber,50)||!optGuid(v.spaceId)||!optText(v.spaceCode,limits.code)||!plateText(v.plate)||!optText(v.guestName,limits.guestName)||!optText(v.note,limits.note)
  ||!stamp(v.startsAt)||!stamp(v.endsAt)||!oneOf(guestStatuses,v.status)||!optStamp(v.arrivedAt)||!optStamp(v.departedAt)||!int(v.version,1))fail("guest");
 return v as Guest;
}
function unique<T extends {id:string}>(items:T[]){if(new Set(items.map(i=>i.id)).size!==items.length)fail("duplicates");}
export function readPage<T extends {id:string}>(v:unknown,read:(row:unknown)=>T):Page<T>{
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1,limits.pageSize)||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize)||v.items.length>Number(v.totalCount))fail("page");
 const items=v.items.map(read);unique(items);return {items,page:v.page as number,pageSize:v.pageSize as number,totalCount:v.totalCount as number};
}
export function readUnits(v:unknown):UnitOption[]{
 if(!Array.isArray(v)||v.length>50||!v.every(u=>obj(u)&&guid(u.id)&&text(u.number,50)&&optText(u.buildingName,200)))fail("units");
 return v as UnitOption[];
}
export function readResidents(v:unknown):ResidentOption[]{
 if(!Array.isArray(v)||v.length>50||!v.every(r=>obj(r)&&guid(r.personId)&&text(r.name,201)&&oneOf(relations,r.relation)))fail("residents");
 return v as ResidentOption[];
}
export function readPlates(v:unknown):Plate[]{
 if(!Array.isArray(v)||v.length>50||!v.every(p=>obj(p)&&plateText(p.plate)&&text(p.unitNumber,50)&&optText(p.buildingName,200)&&oneOf(vehicleTypes,p.type)))fail("plates");
 return v as Plate[];
}

const blank=(v:string)=>v.trim()||null;
export function policyBody(form:{maxVehiclesPerUnit:string;maxVehiclesPerPerson:string;maxSpacesPerUnit:string;guestMaxHours:string;retentionDays:string},version:number){
 const n=(v:string)=>/^\d{1,3}$/.test(v.trim())?Number(v.trim()):NaN;
 const body={maxVehiclesPerUnit:n(form.maxVehiclesPerUnit),maxVehiclesPerPerson:n(form.maxVehiclesPerPerson),maxSpacesPerUnit:n(form.maxSpacesPerUnit),guestMaxHours:n(form.guestMaxHours),retentionDays:n(form.retentionDays),version};
 const ok=body.maxVehiclesPerUnit>=1&&body.maxVehiclesPerUnit<=limits.maxQuota&&body.maxVehiclesPerPerson>=1&&body.maxVehiclesPerPerson<=limits.maxQuota&&body.maxSpacesPerUnit>=0&&body.maxSpacesPerUnit<=limits.maxQuota
  &&body.guestMaxHours>=1&&body.guestMaxHours<=limits.maxGuestHours&&body.retentionDays>=1&&body.retentionDays<=limits.maxRetention;
 return ok?body:null;
}
export function vehicleBody(form:{personId:string;unitId:string;plate:string;type:VehicleType;brand:string;model:string;color:string},requestId:string){
 const plate=normalizePlate(form.plate);const long=[form.brand,form.model,form.color].some(v=>v.trim().length>limits.text);
 if(!plate||!guid(form.personId)||!guid(form.unitId)||long)return null;
 return {requestId,personId:form.personId,unitId:form.unitId,plate,type:form.type,brand:blank(form.brand),model:blank(form.model),color:blank(form.color)};
}
export function guestBody(form:{hostUnitId:string;plate:string;guestName:string;startsAt:string;endsAt:string;spaceId:string;note:string},requestId:string,maxHours:number){
 const plate=normalizePlate(form.plate),start=istanbulInstant(form.startsAt),end=istanbulInstant(form.endsAt);
 if(!plate||!guid(form.hostUnitId)||!Number.isFinite(start)||!Number.isFinite(end)||end<=start||end-start>maxHours*3600000||form.guestName.trim().length>limits.guestName||form.note.trim().length>limits.note||(form.spaceId&&!guid(form.spaceId)))return null;
 return {requestId,hostUnitId:form.hostUnitId,plate,guestName:blank(form.guestName),startsAt:new Date(start).toISOString(),endsAt:new Date(end).toISOString(),spaceId:form.spaceId||null,note:blank(form.note)};
}
export function allocationBody(form:{spaceId:string;unitId:string;vehicleId:string;startDate:string;endDate:string}){
 if(!guid(form.spaceId)||!guid(form.unitId)||(form.vehicleId&&!guid(form.vehicleId))||!calendarDay(form.startDate)||(form.endDate&&(!calendarDay(form.endDate)||form.endDate<form.startDate)))return null;
 return {spaceId:form.spaceId,unitId:form.unitId,vehicleId:form.vehicleId||null,startDate:form.startDate,endDate:form.endDate||null};
}
/** Today's allocations decide the map colour; a space without one is free unless it is a visitor space. */
export function spaceState(space:Space):"closed"|"allocated"|"visitor"|"accessible"|"free"{
 return space.isClosed?"closed":space.allocatedUnitId?"allocated":space.type==="VISITOR"?"visitor":space.type==="ACCESSIBLE"?"accessible":"free";
}
/** Business inputs always use Europe/Istanbul, regardless of the device's time zone. */
export function istanbulInstant(value:string):number{
 if(!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)||!calendarDay(value.slice(0,10))||Number(value.slice(11,13))>23||Number(value.slice(14,16))>59)return NaN;
 return Date.parse(value+":00+03:00");
}
export function localInput(date:Date){return new Date(date.getTime()+3*3600000).toISOString().slice(0,16);}
export function istanbulDayWindow(now=new Date()){
 const day=localInput(now).slice(0,10),start=istanbulInstant(day+"T00:00");
 return {start:new Date(start).toISOString(),end:new Date(start+86400000).toISOString()};
}
export function closureBody(form:{spaceId:string;startsAt:string;endsAt:string;reason:string},requestId:string){
 const start=istanbulInstant(form.startsAt),end=istanbulInstant(form.endsAt);
 if(!guid(form.spaceId)||!guid(requestId)||!Number.isFinite(start)||!Number.isFinite(end)||end<=start||!text(form.reason.trim(),limits.reason))return null;
 return {requestId,spaceId:form.spaceId,startsAt:new Date(start).toISOString(),endsAt:new Date(end).toISOString(),reason:form.reason.trim()};
}

export type MyVehicle={id:string;unitId:string;plate:string;type:VehicleType;brand:string|null;model:string|null;color:string|null;status:VehicleStatus;reviewReason:string|null;version:number};
export function readMyVehicle(v:unknown):MyVehicle{
 if(!obj(v)||!guid(v.id)||!guid(v.unitId)||!plateText(v.plate)||!oneOf(vehicleTypes,v.type)||!oneOf(vehicleStatuses,v.status)||!optText(v.brand,50)||!optText(v.model,50)||!optText(v.color,50)||!optText(v.reviewReason,500)||!int(v.version,1))fail("own vehicle");
 return {id:v.id,unitId:v.unitId,plate:v.plate as string,type:v.type,status:v.status,brand:v.brand as string|null,model:v.model as string|null,color:v.color as string|null,reviewReason:v.reviewReason as string|null,version:v.version as number};
}
