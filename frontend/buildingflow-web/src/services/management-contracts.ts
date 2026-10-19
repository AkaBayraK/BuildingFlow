import {validateDetailResponse} from "../features/property-details/model.ts";
import type {Property,PropertyPage,PropertyType,Building} from "./api";
import type {Unit,UnitType,PagedRows} from "./api";
import type {Person,Occupancy} from "../features/people/types";

type ObjectValue=Record<string,unknown>;
function uuid(value:unknown){return typeof value==="string"&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)&&value!=="00000000-0000-0000-0000-000000000000";}
function object(value:unknown):ObjectValue{
  if(!value||typeof value!=="object"||Array.isArray(value))throw new Error("Invalid management response");
  return value as ObjectValue;
}
function string(value:unknown){if(typeof value!=="string"||!value.trim())throw new Error("Invalid management string");return value;}
function integer(value:unknown,min=0){if(typeof value!=="number"||!Number.isSafeInteger(value)||value<min)throw new Error("Invalid management integer");return value;}
function common(value:unknown,propertyId:string){
  const row=object(value);string(row.id);string(row.createdAt);integer(row.version,1);
  if(row.propertyId!==propertyId||typeof row.isEnabled!=="boolean"||!Number.isFinite(Date.parse(row.createdAt as string)))throw new Error("Invalid management scope");
  return row;
}
export function parseUnit(value:unknown,propertyId:string,buildingId:string|null):Unit{
  const row=common(value,propertyId);string(row.number);integer(row.floor,-2147483648);
  const types:UnitType[]=["APARTMENT","OFFICE","SHOP","VILLA","WAREHOUSE","WORKSHOP"];
  if((row.floor as number)>2147483647||row.buildingId!==buildingId||!types.includes(row.type as UnitType)||typeof row.grossSquareMeters!=="number"||!Number.isFinite(row.grossSquareMeters)||row.grossSquareMeters<=0)throw new Error("Invalid unit response");
  validateDetailResponse(row,"unit");
  return row as Unit;
}
export function parsePerson(value:unknown,propertyId:string,personId?:string):Person{
  const row=common(value,propertyId);if(!uuid(row.id)||(personId&&row.id!==personId))throw new Error("Invalid person identity");string(row.firstName);if(row.personKind!=="LEGAL")string(row.lastName);else if(typeof row.lastName!=="string")throw new Error("Invalid legal name");integer(row.activeOccupancyCount);
  if(![row.phoneNumber,row.email].every(item=>item===null||typeof item==="string"))throw new Error("Invalid person contact");
  if(row.personKind!=null){
    if(!["NATURAL","LEGAL"].includes(String(row.personKind))||!["DOMESTIC","FOREIGN"].includes(String(row.citizenship))||typeof row.countryCode!=="string"||!/^[A-Z]{2}$/.test(row.countryCode)||typeof row.identityNumber!=="string"||!row.identityNumber.trim()||row.birthDate!=null&&!dateOnly(row.birthDate)||row.gender!=null&&!["FEMALE","MALE","OTHER","UNSPECIFIED"].includes(String(row.gender)))throw new Error("Invalid person profile");
    if((row.citizenship==="DOMESTIC"?row.countryCode!=="TR":row.countryCode==="TR")||row.personKind==="LEGAL"&&(row.birthDate!=null||row.gender!=null))throw new Error("Invalid person profile combination");
    if(row.citizenship==="DOMESTIC"?!(row.personKind==="LEGAL"?/^[0-9]{10}$/:/^[1-9][0-9]{10}$/).test(String(row.identityNumber)):row.personKind==="NATURAL"&&!/^[A-Za-z0-9-]{6,30}$/.test(String(row.identityNumber)))throw new Error("Invalid person identifier");
  }
  for(const key of ["address","linkedUserId","propertyName"])if(row[key]!=null&&typeof row[key]!=="string")throw new Error("Invalid person field");
  // İŞ19: a photo id only together with a linked account.
  if(row.linkedPhotoId!=null&&(!uuid(row.linkedPhotoId)||typeof row.linkedUserId!=="string"))throw new Error("Invalid person photo");
  if(row.personCodeHint!=null&&(typeof row.personCodeHint!=="string"||!/^[0-9]{2}$/.test(row.personCodeHint))||row.personCodeIssuedAt!=null&&(typeof row.personCodeIssuedAt!=="string"||Number.isNaN(Date.parse(row.personCodeIssuedAt)))||row.issuedPersonCode!=null&&(typeof row.issuedPersonCode!=="string"||!/^[1-9][0-9]{5}$/.test(row.issuedPersonCode)))throw new Error("Invalid person code");
  if(row.registrationSms!=null){const sms=object(row.registrationSms);if(!["QUEUED","FAILED","SKIPPED"].includes(String(sms.status))||!(sms.messageId===null||uuid(sms.messageId))||!(sms.errorCode===null||typeof sms.errorCode==="string"))throw new Error("Invalid registration SMS");}
  if(row.units!=null){if(!Array.isArray(row.units))throw new Error("Invalid person units");for(const unit of row.units){const u=object(unit);if(!uuid(u.unitId)||typeof u.number!=="string"||!['OWNER','TENANT','RESIDENT'].includes(String(u.role))||!(u.buildingId===null||uuid(u.buildingId))||!(u.buildingName===null||typeof u.buildingName==='string'))throw new Error("Invalid person unit");}}
  return row as Person;
}
export function parseOccupancyPage(value:unknown,propertyId:string,personId:string):PagedRows<Occupancy>{
  const page=object(value);integer(page.page,1);integer(page.pageSize,1);integer(page.totalCount);
  if(!Array.isArray(page.items)||page.items.length>(page.pageSize as number)||page.items.length>(page.totalCount as number))throw new Error("Invalid occupancy page");
  for(const item of page.items){
    const row=object(item);string(row.id);string(row.unitId);string(row.unitNumber);string(row.personName);integer(row.version,1);
    if(!uuid(row.id)||!uuid(row.unitId)||!(row.buildingId===null||uuid(row.buildingId))||row.propertyId!==propertyId||row.personId!==personId||!['OWNER','TENANT','RESIDENT'].includes(String(row.type))||!['ACTIVE','UPCOMING','ENDED'].includes(String(row.status))||typeof row.isPrimaryContact!=="boolean"||!dateOnly(row.startDate)||!(row.endDate===null||dateOnly(row.endDate))||(typeof row.endDate==="string"&&row.endDate<(row.startDate as string))||!(row.buildingName===null||typeof row.buildingName==="string")||!(row.buildingId===null||typeof row.buildingId==="string"))throw new Error("Invalid occupancy scope");
  }
  return page as PagedRows<Occupancy>;
}

function dateOnly(value:unknown){
  if(typeof value!=="string"||!/^\d{4}-\d{2}-\d{2}$/.test(value))return false;
  const date=new Date(`${value}T00:00:00Z`);
  return Number.isFinite(date.getTime())&&date.toISOString().slice(0,10)===value;
}

export function parseProperty(value:unknown,id?:string):Property{const row=object(value);string(row.id);string(row.name);string(row.city);string(row.district);string(row.address);integer(row.version,1);integer(row.buildingCount);integer(row.unitCount);if((id&&row.id!==id)||!['APARTMENT','SITE','RESIDENCE','MIXED'].includes(String(row.type))||typeof row.isEnabled!=='boolean'||typeof row.canChangeType!=='boolean'||!Number.isFinite(Date.parse(String(row.createdAt)))||!(row.floorCount===null||Number.isSafeInteger(row.floorCount)&&Number(row.floorCount)>0))throw new Error('Invalid property response');validateDetailResponse(row,'property',String(row.type));return row as Property;}
export function parseBuilding(value:unknown,propertyId:string):Building{const row=common(value,propertyId);string(row.name);integer(row.unitCount);if(!(row.floorCount===null||Number.isSafeInteger(row.floorCount)&&Number(row.floorCount)>0))throw new Error('Invalid building floor count');validateDetailResponse(row,'building');return row as Building;}
export function parsePropertyPage(value:unknown):PropertyPage{const page=object(value);integer(page.page,1);integer(page.pageSize,1);integer(page.totalCount);if(!Array.isArray(page.items)||page.items.length>Number(page.pageSize)||page.items.length>Number(page.totalCount)||new Set(page.items.map((v:unknown)=>parseProperty(v).id)).size!==page.items.length)throw new Error('Invalid property page');return page as PropertyPage;}

export function parseManagementPage<T extends {id:string}>(value:unknown,decode:(row:unknown)=>T):PagedRows<T>{const page=object(value);integer(page.page,1);integer(page.pageSize,1);integer(page.totalCount);if(!Array.isArray(page.items)||page.items.length>Number(page.pageSize)||page.items.length>Number(page.totalCount))throw new Error('Invalid management page');const items=page.items.map(decode);if(new Set(items.map(v=>v.id)).size!==items.length)throw new Error('Duplicate management rows');return {...page,items} as PagedRows<T>;}
