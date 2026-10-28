import {contextKey,type Context} from '../auth/contracts.ts';
import {readMyVehicle,readGuest,readSpace,readUnits,calendarDay,type MyVehicle,type Guest,type Space,type UnitOption} from '../../../../shared/parking.ts';
export {vehicleTypes,localInput,guestBody,normalizePlate,readMyVehicle,readGuest} from '../../../../shared/parking.ts';
export const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v)&&!/^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(v);
export const canManage=(c:Context)=>['SUPER_ADMIN','SITE_ADMIN','MANAGER'].includes(c.role)&&c.unitId===null&&(!c.canonical||['MANAGEMENT','PLATFORM'].includes(c.canonical.kind));
export function permitted(c:Context){return canManage(c)||['OWNER','TENANT','RESIDENT'].includes(c.role)&&guid(c.unitId)&&(!c.canonical||c.canonical.kind===c.role);}
export function path(c:Context,page:number,suffix=''){
 if(!permitted(c)||!guid(c.propertyId)||!Number.isInteger(page)||page<1||page>10000)throw Error('Yetkili otopark bağlamı gerekli.');
 return '/api/v1/mobile/parking'+suffix+'?propertyId='+c.propertyId+'&role='+c.role+(c.unitId?'&unitId='+c.unitId:'')+(c.canonical?'&contextKind='+c.canonical.kind+'&contextId='+c.canonical.id:'')+'&page='+page;
}
export function authorizePath(c:Context,url:string,method:string){
 const u=new URL(url,'https://scope.invalid'),page=Number(u.searchParams.get('page')),suffix=u.pathname.slice('/api/v1/mobile/parking'.length);
 const valid=method==='GET'&&suffix===''||method==='POST'&&(!canManage(c)&&suffix==='/vehicles'||canManage(c)&&(suffix==='/guests'||/^\/guests\/[a-f0-9-]{36}\/(arrive|depart|cancel)$/.test(suffix)));
 if(!valid||url!==path(c,page,suffix))throw Error('Otopark işlemine izin verilmedi.');
}
export type Allocation={id:string;spaceCode:string;startDate:string;endDate:string|null};
export type Snapshot={userId:string;contextKey:string;propertyId:string;unitId:string|null;role:string;asOf:string;page:number;vehicleTotal:number;allocationTotal:number;guestTotal:number;unitTotal:number;spaceTotal:number;vehicles:MyVehicle[];allocations:Allocation[];guests:Guest[];units:UnitOption[];spaces:Space[];policyConfigured:boolean;guestMaxHours:number};
const bad=():never=>{throw Error('Otopark yanıtı doğrulanamadı.');};
export function parseSnapshot(v:unknown,user:string,c:Context,page:number):Snapshot{
 const r=v as Snapshot;if(!r||r.userId!==user||r.contextKey!==contextKey(c)||r.propertyId!==c.propertyId||r.unitId!==c.unitId||r.role!==c.role||r.page!==page||!Number.isFinite(Date.parse(r.asOf))||typeof r.policyConfigured!=='boolean'||!Number.isInteger(r.guestMaxHours)||r.guestMaxHours<0||r.guestMaxHours>168)bad();
 for(const key of ['vehicles','allocations','guests','units','spaces'] as const){const rows=r[key];if(!Array.isArray(rows)||rows.length>20||new Set(rows.map(x=>x.id)).size!==rows.length)bad();}
 for(const key of ['vehicleTotal','allocationTotal','guestTotal','unitTotal','spaceTotal'] as const)if(!Number.isSafeInteger(r[key])||r[key]<0)bad();
 if(canManage(c)?r.vehicles.length>0||r.allocations.length>0:r.guests.length>0||r.units.length>0||r.spaces.length>0)bad();
 const vehicles=r.vehicles.map(x=>{const v=readMyVehicle(x);if(v.unitId!==c.unitId)bad();return v;});
 const allocations=r.allocations.map(x=>{if(!guid(x.id)||!x.spaceCode||x.spaceCode.length>20||!calendarDay(x.startDate)||x.endDate!==null&&(!calendarDay(x.endDate)||x.endDate<x.startDate))bad();return{id:x.id,spaceCode:x.spaceCode,startDate:x.startDate,endDate:x.endDate};});
 const guests=r.guests.map(x=>{const g=readGuest(x);return {id:g.id,hostUnitId:g.hostUnitId,unitNumber:g.unitNumber,spaceId:g.spaceId,spaceCode:g.spaceCode,plate:g.plate,startsAt:g.startsAt,endsAt:g.endsAt,status:g.status,arrivedAt:g.arrivedAt,departedAt:g.departedAt,version:g.version,guestName:null,note:null};});
 const units=readUnits(r.units).map(u=>({id:u.id,number:u.number,buildingName:u.buildingName})),spaces=r.spaces.map(x=>{const s=readSpace(x);if(s.type!=='VISITOR')bad();return {id:s.id,facilityId:s.facilityId,levelId:s.levelId,code:s.code,type:s.type,allocatedUnitId:null,allocatedUnitNumber:null,version:s.version,isClosed:s.isClosed===true};});
 return {userId:user,contextKey:contextKey(c),propertyId:c.propertyId,unitId:c.unitId,role:c.role,asOf:r.asOf,page,vehicleTotal:r.vehicleTotal,allocationTotal:r.allocationTotal,guestTotal:r.guestTotal,unitTotal:r.unitTotal,spaceTotal:r.spaceTotal,vehicles,allocations,guests,units,spaces,policyConfigured:r.policyConfigured,guestMaxHours:r.guestMaxHours};
}
