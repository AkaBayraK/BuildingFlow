import {areaPath,guid,readAreaPage,readSchedule,type AreaPage,type Schedule} from '../../../../shared/common-areas.ts';
import {contextKey,type Context} from '../auth/contracts.ts';
export {areaTypes,blankArea,areaBody,areaErrors,blankReservation,reservationErrors,reservationBody,istanbulToday,segments,readReservations,readReservation,type Area,type Reservation} from '../../../../shared/common-areas.ts';
export const managers=['SUPER_ADMIN','SITE_ADMIN','MANAGER'];
export const canManage=(c:Context)=>managers.includes(c.role)&&(!c.canonical||['MANAGEMENT','PLATFORM'].includes(c.canonical.kind));
export type Query={page:number;areaId:string|null;date:string};
export function validDay(v:string){return /^\d{4}-\d{2}-\d{2}$/.test(v)&&new Date(v+'T00:00:00Z').toISOString().slice(0,10)===v;}
export function path(c:Context,q:Query,suffix=''){
 if(!Number.isSafeInteger(q.page)||q.page<1||q.page>10000||!validDay(q.date)||q.areaId!==null&&!guid(q.areaId))throw Error('Geçersiz ortak alan sorgusu.');
 const own=c.canonical;
 if(!canManage(c)&&(!own||!['OWNER','TENANT','RESIDENT'].includes(own.kind)||!c.unitId))throw Error('Yetkili daire seçin.');
 const context=canManage(c)?undefined:{propertyId:c.propertyId,unitId:c.unitId!,capacity:own!.kind,relationId:own!.id};
 return areaPath(c.propertyId,context,q.areaId??undefined,suffix,new URLSearchParams(q.areaId?suffix==='/schedule'?{date:q.date}:suffix==='/reservations'?{from:q.date,to:q.date,page:String(q.page),pageSize:'20'}:{}:{page:String(q.page),pageSize:'12'}));
}
// Reconstruct every allowed URL from the selected context; callers cannot substitute scope/query fields.
export function authorizePath(c:Context,url:string,method:string){
 const u=new URL(url,'https://scope.invalid'),parts=u.pathname.split('/'),index=parts.indexOf('common-areas'),id=parts[index+1]??null,suffix=parts.slice(index+2).join('/');
 const q={page:Number(u.searchParams.get('page')??1),areaId:id,date:u.searchParams.get('date')??u.searchParams.get('from')??'2000-01-01'};
 if(method==='GET'&&(suffix!=='reservations'||canManage(c))&&['','schedule','reservations'].includes(suffix)&&url===path(c,q,suffix?'/'+suffix:''))return;
 if(!canManage(c)||!['POST','PUT'].includes(method)||u.search)return void fail();
 const root=areaPath(c.propertyId);
 if(method==='POST'&&url===root||method==='PUT'&&guid(id)&&url===root+'/'+id||method==='POST'&&guid(id)&&url===root+'/'+id+'/reservations'||method==='POST'&&guid(id)&&guid(parts[index+3])&&url===root+'/'+id+'/reservations/'+parts[index+3]+'/cancel')return;
 fail();
}
function fail():never{throw Error('Ortak alan işlemine izin verilmedi.');}
export type Copy={userId:string;scope:string;query:Query;asOf:string;page:AreaPage|null;schedule:Schedule|null};
export function parseCopy(raw:unknown,userId:string,c:Context,q:Query):Copy{
 const v=raw as Copy;if(!v||v.userId!==userId||v.scope!==contextKey(c)||JSON.stringify(v.query)!==JSON.stringify(q)||!Number.isFinite(Date.parse(v.asOf)))throw Error('Önbellek kapsamı geçersiz.');
 const page=q.areaId?null:readAreaPage(v.page,c.propertyId),schedule=q.areaId?readSchedule(v.schedule,q.areaId,q.date):null;
 if(page&&(page.page!==q.page||page.pageSize!==12))throw Error('Liste sayfası eşleşmedi.');
 // Only explicitly selected fields reach storage. Schedules contain no personal data.
 return{userId,scope:contextKey(c),query:q,asOf:v.asOf,page,schedule};
}
