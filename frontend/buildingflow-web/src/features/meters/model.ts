export type Meter={id:string;propertyId:string;unitId:string|null;name:string;kind:"WATER"|"GAS"|"ELECTRICITY";measurementUnit:"m³"|"kWh";isEnabled:boolean;version:number;replayed:boolean};
export type Reading={id:string;propertyId:string;meterId:string;readingDate:string;value:string;consumption:string|null;version:number;revisions:{id:string;value:string;sequence:number;reason:string;createdAt:string}[];replayed:boolean};
export type Page<T>={items:T[];page:number;pageSize:number;totalCount:number};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const calendar=(v:unknown):v is string=>typeof v==='string'&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&!Number.isNaN(Date.parse(v+'T00:00:00Z'))&&new Date(v+'T00:00:00Z').toISOString().slice(0,10)===v;
const integer=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
export const indexValue=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9][0-9]{0,8})\.[0-9]{3}$/.test(v);
export function normalizedIndex(v:string){const s=v.trim().replace(',','.');if(!/^[0-9]{1,9}(?:\.[0-9]{1,3})?$/.test(s))return null;const [whole,fraction='']=s.split('.');return `${BigInt(whole)}.${fraction.padEnd(3,'0')}`;}
export function formatIndex(v:string,locale:string){if(!indexValue(v))throw new Error('Invalid index');const [whole,fraction]=v.split('.');return BigInt(whole).toLocaleString(locale)+(locale==='tr'?',':'.')+fraction;}
export function readMeter(v:unknown,property:string):Meter{
 if(!obj(v)||!guid(v.id)||v.propertyId!==property||!(v.unitId===null||guid(v.unitId))||typeof v.name!=='string'||!v.name.trim()||v.name.length>100||!['WATER','GAS','ELECTRICITY'].includes(String(v.kind))||v.measurementUnit!==(v.kind==='ELECTRICITY'?'kWh':'m³')||typeof v.isEnabled!=='boolean'||!integer(v.version,1)||typeof v.replayed!=='boolean')throw new Error('Invalid meter');return v as Meter;
}
export function readReading(v:unknown,property:string,meter:string):Reading{
 if(!obj(v)||!guid(v.id)||v.propertyId!==property||v.meterId!==meter||!calendar(v.readingDate)||!indexValue(v.value)||!(v.consumption===null||indexValue(v.consumption))||!integer(v.version,1)||typeof v.replayed!=='boolean'||!Array.isArray(v.revisions)||!v.revisions.length)throw new Error('Invalid reading');
 const revisions=v.revisions;revisions.forEach((r,i)=>{if(!obj(r)||!guid(r.id)||!indexValue(r.value)||r.sequence!==revisions.length-i||typeof r.reason!=='string'||!r.reason.trim()||r.reason.length>500||typeof r.createdAt!=='string'||Number.isNaN(Date.parse(r.createdAt)))throw new Error('Invalid revision');});
 if(new Set(v.revisions.map(r=>r.id)).size!==v.revisions.length||v.value!==v.revisions[0].value)throw new Error('Invalid current reading');return v as Reading;
}
export function readPage<T extends {id:string}>(v:unknown,reader:(row:unknown)=>T):Page<T>{
 if(!obj(v)||!Array.isArray(v.items)||!integer(v.page,1)||!integer(v.pageSize,1)||Number(v.pageSize)>100||!integer(v.totalCount)||v.items.length>Number(v.pageSize)||v.items.length>Number(v.totalCount))throw new Error('Invalid page');const items=v.items.map(reader);if(new Set(items.map(r=>r.id)).size!==items.length)throw new Error('Duplicate records');return {...v,items} as Page<T>;
}
