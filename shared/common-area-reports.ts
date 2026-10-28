import {guid,type Reservation,readReservations} from './common-areas.ts';
export type Metrics={confirmed:number;cancelled:number;openingMinutes:number|null;closedMinutes:number|null;availableMinutes:number|null;bookedMinutes:number|null;personMinutes:number|null;capacityMinutes:number|null;occupancyPercent:string|null;capacityUsePercent:string|null};
export type ReportDay={date:string;metrics:Metrics};
export type AreaReport={propertyId:string;commonAreaId:string;from:string;to:string;asOf:string;areaVersion:number;capacity:number;opensAt:string|null;closesAt:string|null;currentConfiguration:true;paymentSupported:false;measuredDays:number;totals:Metrics;days:ReportDay[]};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const natural=(v:unknown):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0;
export function calendarDay(v:unknown):v is string{if(typeof v!=='string'||!/^\d{4}-\d{2}-\d{2}$/.test(v)||v<'2000-01-01'||v>'2100-12-31')return false;const t=Date.parse(v+'T00:00:00Z');return Number.isFinite(t)&&new Date(t).toISOString().slice(0,10)===v;}
export function dateRange(from:string,to:string){if(!calendarDay(from)||!calendarDay(to)||to<from)throw Error('Invalid report range');const count=(Date.parse(to+'T00:00:00Z')-Date.parse(from+'T00:00:00Z'))/86400000+1;if(count>93)throw Error('Invalid report range');return count;}
export function moveDay(day:string,delta:number){if(!calendarDay(day)||!Number.isInteger(delta))throw Error('Invalid date');const d=new Date(day+'T00:00:00Z');d.setUTCDate(d.getUTCDate()+delta);return d.toISOString().slice(0,10);}
const minuteKeys=['openingMinutes','closedMinutes','availableMinutes','bookedMinutes','personMinutes','capacityMinutes'] as const;
function metrics(v:unknown):Metrics{
 if(!obj(v)||!natural(v.confirmed)||!natural(v.cancelled)||minuteKeys.some(k=>v[k]!==null&&!natural(v[k])))throw Error('Invalid metrics');
 const m=v as Metrics,unknown=m.openingMinutes===null;
 if(minuteKeys.some(k=>(m[k]===null)!==unknown))throw Error('Partial metrics');
 if(!unknown&&(m.closedMinutes!>m.openingMinutes!||m.availableMinutes!==m.openingMinutes!-m.closedMinutes!||m.bookedMinutes!>m.availableMinutes!))throw Error('Inconsistent metrics');
 const rate=(n:number|null,d:number|null)=>{if(n===null||d===null||d===0)return null;const a=BigInt(n)*BigInt(10000),b=BigInt(d);const v=a/b+(a%b*BigInt(2)>=b?BigInt(1):BigInt(0));return String(v/BigInt(100))+'.'+String(v%BigInt(100)).padStart(2,'0');};
 if(m.occupancyPercent!==rate(m.bookedMinutes,m.availableMinutes)||m.capacityUsePercent!==rate(m.personMinutes,m.capacityMinutes))throw Error('Inconsistent rate');
 return m;
}
export function readAreaReport(v:unknown,property:string,area:string,from:string,to:string):AreaReport{
 const count=dateRange(from,to);
 if(!obj(v)||!guid(property)||!guid(area)||v.propertyId!==property||v.commonAreaId!==area||v.from!==from||v.to!==to||typeof v.asOf!=='string'||!Number.isFinite(Date.parse(v.asOf))||!natural(v.areaVersion)||v.areaVersion<1||!natural(v.capacity)||v.capacity<1||v.capacity>10000||v.currentConfiguration!==true||v.paymentSupported!==false||!Array.isArray(v.days)||v.days.length!==count)throw Error('Invalid report');
 const time=(t:unknown)=>t===null||typeof t==='string'&&/^([01]\d|2[0-3]):[0-5]\d$/.test(t);
 if(!time(v.opensAt)||!time(v.closesAt)||(v.opensAt===null)!==(v.closesAt===null)||typeof v.opensAt==='string'&&typeof v.closesAt==='string'&&v.opensAt>=v.closesAt)throw Error('Invalid report hours');
 const hm=(t:string)=>Number(t.slice(0,2))*60+Number(t.slice(3));const opening=v.opensAt===null?null:hm(v.closesAt as string)-hm(v.opensAt as string);
 const days=v.days.map((d,i)=>{if(!obj(d)||d.date!==moveDay(from,i))throw Error('Invalid report day');const m=metrics(d.metrics);if(m.openingMinutes!==opening)throw Error('Invalid opening denominator');if(m.capacityMinutes!==null&&m.capacityMinutes!==m.availableMinutes!*Number(v.capacity))throw Error('Invalid capacity denominator');return{date:d.date as string,metrics:m};});
 const measured=days.filter(d=>d.metrics.openingMinutes!==null);if(v.measuredDays!==measured.length)throw Error('Invalid measured count');
 const totals=metrics(v.totals);for(const key of ['confirmed','cancelled',...minuteKeys] as const){const values=days.map(d=>d.metrics[key]).filter(x=>x!==null) as number[];const expected=values.length?values.reduce((a,b)=>a+b,0):key==='confirmed'||key==='cancelled'?0:null;if(totals[key]!==expected)throw Error('Inconsistent totals');}
 return{...v,totals,days} as AreaReport;
}
export type ReservationPage={items:Reservation[];page:number;pageSize:number;totalCount:number};
export function readReservationPage(v:unknown,property:string,area:string,page:number,pageSize:number,from:string,to:string,status:string):ReservationPage{
 dateRange(from,to);if(!obj(v)||v.page!==page||v.pageSize!==pageSize||!natural(v.totalCount))throw Error('Invalid reservation page');const items=readReservations(v,property,area);
 if(items.length!==Math.min(pageSize,Math.max(0,v.totalCount-(page-1)*pageSize))||items.some(r=>r.date<from||r.date>to||status&&r.status!==status))throw Error('Invalid reservation rows');return{items,page,pageSize,totalCount:v.totalCount};
}
