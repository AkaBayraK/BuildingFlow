import type {Unit,UnitType,PagedRows} from "./api";
import type {Person,Occupancy} from "../features/people/types";

type ObjectValue=Record<string,unknown>;
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
  return row as Unit;
}
export function parsePerson(value:unknown,propertyId:string):Person{
  const row=common(value,propertyId);string(row.firstName);string(row.lastName);integer(row.activeOccupancyCount);
  if(![row.phoneNumber,row.email].every(item=>item===null||typeof item==="string"))throw new Error("Invalid person contact");
  return row as Person;
}
export function parseOccupancyPage(value:unknown,propertyId:string,personId:string):PagedRows<Occupancy>{
  const page=object(value);integer(page.page,1);integer(page.pageSize,1);integer(page.totalCount);
  if(!Array.isArray(page.items)||page.items.length>(page.pageSize as number)||page.items.length>(page.totalCount as number))throw new Error("Invalid occupancy page");
  for(const item of page.items){
    const row=object(item);string(row.id);string(row.unitId);string(row.unitNumber);string(row.personName);integer(row.version,1);
    if(row.propertyId!==propertyId||row.personId!==personId||!['OWNER','TENANT','RESIDENT'].includes(String(row.type))||!['ACTIVE','UPCOMING','ENDED'].includes(String(row.status))||typeof row.isPrimaryContact!=="boolean"||!dateOnly(row.startDate)||!(row.endDate===null||dateOnly(row.endDate))||(typeof row.endDate==="string"&&row.endDate<(row.startDate as string))||!(row.buildingName===null||typeof row.buildingName==="string")||!(row.buildingId===null||typeof row.buildingId==="string"))throw new Error("Invalid occupancy scope");
  }
  return page as PagedRows<Occupancy>;
}

function dateOnly(value:unknown){
  if(typeof value!=="string"||!/^\d{4}-\d{2}-\d{2}$/.test(value))return false;
  const date=new Date(`${value}T00:00:00Z`);
  return Number.isFinite(date.getTime())&&date.toISOString().slice(0,10)===value;
}
