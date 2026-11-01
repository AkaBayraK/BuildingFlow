import {expenseCategories,type ExpenseCategory} from '../expenses/model.ts';
export type ReportMonth={period:string;assessedAmount:string;outstandingAmount:string;receiptsAmount:string;expensesAmount:string};
// period is YYYY-MM for a month or YYYY for a calendar year; months is the yearly breakdown only. Older APIs omit both lists.
export type Report={propertyId:string;period:string;currency:'TRY';unitCount:number;activeUnitCount:number;occupiedUnitCount:number;vacantUnitCount:number;assessedAmount:string;outstandingAmount:string;receiptsAmount:string;expensesAmount:string;consumption:{kind:'WATER'|'ELECTRICITY'|'GAS';measurementUnit:'m³'|'kWh';readingCount:number;baselineCount:number;consumption:string}[];
 expenseCategories?:{category:ExpenseCategory;amount:string}[]|null;months?:ReportMonth[]|null};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown)=>Number.isSafeInteger(v)&&Number(v)>=0;
const money=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9][0-9]*)\.[0-9]{2}$/.test(v);
const cents=(v:string)=>BigInt(v.replace('.',''));
const sum=(values:string[])=>values.reduce((a,v)=>a+cents(v),BigInt(0));
export const MONTH_PERIOD=/^\d{4}-(0[1-9]|1[0-2])$/,YEAR_PERIOD=/^\d{4}$/;
const amountKeys=['assessedAmount','outstandingAmount','receiptsAmount','expensesAmount'] as const;
export function readReport(v:unknown,property:string,period:string):Report{
 const year=YEAR_PERIOD.test(period);
 if(!obj(v)||v.propertyId!==property||v.period!==period||!year&&!MONTH_PERIOD.test(period)||v.currency!=='TRY'||!['unitCount','activeUnitCount','occupiedUnitCount','vacantUnitCount'].every(k=>integer(v[k]))||Number(v.activeUnitCount)>Number(v.unitCount)||Number(v.occupiedUnitCount)+Number(v.vacantUnitCount)!==v.activeUnitCount||!amountKeys.every(k=>money(v[k]))||cents(String(v.outstandingAmount))>cents(String(v.assessedAmount))||!Array.isArray(v.consumption)||v.consumption.length!==3)throw new Error('Invalid report');
 v.consumption.forEach(r=>{if(!obj(r)||!['WATER','ELECTRICITY','GAS'].includes(String(r.kind))||r.measurementUnit!==(r.kind==='ELECTRICITY'?'kWh':'m³')||!integer(r.readingCount)||!integer(r.baselineCount)||Number(r.baselineCount)>Number(r.readingCount)||typeof r.consumption!=='string'||!/^(?:0|[1-9][0-9]*)\.[0-9]{3}$/.test(r.consumption))throw new Error('Invalid consumption');});
 if(new Set(v.consumption.map(r=>r.kind)).size!==3)throw new Error('Duplicate consumption');
 const categories=v.expenseCategories;
 if(categories!==undefined&&categories!==null&&(!Array.isArray(categories)||categories.length!==expenseCategories.length||!categories.every((r,i)=>obj(r)&&r.category===expenseCategories[i]&&money(r.amount))||sum(categories.map(r=>r.amount))!==cents(String(v.expensesAmount))))throw new Error('Invalid expense categories');
 const months=v.months;
 if(months!==undefined&&months!==null&&(!year||!Array.isArray(months)||months.length!==12||!months.every((r,i)=>obj(r)&&r.period===`${period}-${String(i+1).padStart(2,'0')}`&&amountKeys.every(k=>money(r[k])))||!amountKeys.every(k=>sum(months.map(r=>r[k]))===cents(String(v[k])))))throw new Error('Invalid monthly breakdown');
 return v as Report;
}
export type PeriodChoice='thisMonth'|'previousMonth'|'thisYear'|'month'|'year';
/** Quick choices resolve against the Europe/Istanbul business day; custom ones use what the user typed. */
export function resolvePeriod(choice:PeriodChoice,today:string,month:string,year:string):string{
 const [y,m]=today.split('-').map(Number);
 if(choice==='thisMonth')return today.slice(0,7);
 if(choice==='previousMonth'){const d=new Date(Date.UTC(y,m-2,1));return `${d.getUTCFullYear()}-${String(d.getUTCMonth()+1).padStart(2,'0')}`;}
 if(choice==='thisYear')return String(y);
 return choice==='month'?month:year;
}
export const validPeriod=(period:string)=>(MONTH_PERIOD.test(period)||YEAR_PERIOD.test(period))&&period>='2000'&&period<'9999';
/** Signed difference of two money strings for the net cash line; exact, no floating point. */
export function subtractMoney(a:string,b:string){const d=cents(a)-cents(b),text=(d<BigInt(0)?-d:d).toString().padStart(3,'0');return `${d<BigInt(0)?'-':''}${text.slice(0,-2)}.${text.slice(-2)}`;}
