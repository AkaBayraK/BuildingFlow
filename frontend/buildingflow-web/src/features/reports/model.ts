export type Report={propertyId:string;period:string;currency:'TRY';unitCount:number;activeUnitCount:number;occupiedUnitCount:number;vacantUnitCount:number;assessedAmount:string;outstandingAmount:string;receiptsAmount:string;expensesAmount:string;consumption:{kind:'WATER'|'ELECTRICITY'|'GAS';measurementUnit:'m³'|'kWh';readingCount:number;baselineCount:number;consumption:string}[]};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown)=>Number.isSafeInteger(v)&&Number(v)>=0;
const money=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9][0-9]*)\.[0-9]{2}$/.test(v);
const cents=(v:string)=>BigInt(v.replace('.',''));
export function readReport(v:unknown,property:string,period:string):Report{
 if(!obj(v)||v.propertyId!==property||v.period!==period||!/^\d{4}-(0[1-9]|1[0-2])$/.test(period)||v.currency!=='TRY'||!['unitCount','activeUnitCount','occupiedUnitCount','vacantUnitCount'].every(k=>integer(v[k]))||Number(v.activeUnitCount)>Number(v.unitCount)||Number(v.occupiedUnitCount)+Number(v.vacantUnitCount)!==v.activeUnitCount||!money(v.assessedAmount)||!money(v.outstandingAmount)||!money(v.receiptsAmount)||!money(v.expensesAmount)||cents(v.outstandingAmount)>cents(v.assessedAmount)||!Array.isArray(v.consumption)||v.consumption.length!==3)throw new Error('Invalid report');
 v.consumption.forEach(r=>{if(!obj(r)||!['WATER','ELECTRICITY','GAS'].includes(String(r.kind))||r.measurementUnit!==(r.kind==='ELECTRICITY'?'kWh':'m³')||!integer(r.readingCount)||!integer(r.baselineCount)||Number(r.baselineCount)>Number(r.readingCount)||typeof r.consumption!=='string'||!/^(?:0|[1-9][0-9]*)\.[0-9]{3}$/.test(r.consumption))throw new Error('Invalid consumption');});
 if(new Set(v.consumption.map(r=>r.kind)).size!==3)throw new Error('Duplicate consumption');return v as Report;
}
