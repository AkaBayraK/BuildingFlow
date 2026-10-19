// İŞ20 Dashboard: runtime check of GET /api/v1/properties/{id}/dashboard before anything is drawn.
export const UNIT_TYPES=["APARTMENT","OFFICE","SHOP","VILLA","WAREHOUSE","WORKSHOP"] as const;
export type DashboardSummary={propertyId:string;today:string;currency:"TRY";unitCount:number;occupiedUnitCount:number;vacantUnitCount:number;
 receiptsThisMonth:string;overdueAmount:string;overdueChargeCount:number;upcomingAmount:string;upcomingChargeCount:number;
 requests:{recorded:number;inProgress:number}|null;trend:{period:string;receipts:string;expenses:string}[];
 unitTypes:{type:typeof UNIT_TYPES[number];count:number}[];upcomingDues:{dueDate:string;chargeCount:number;amount:string}[];maintenanceAvailable:boolean};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const int=(v:unknown)=>Number.isSafeInteger(v)&&Number(v)>=0;
const money=(v:unknown):v is string=>typeof v==="string"&&/^(?:0|[1-9][0-9]*)\.[0-9]{2}$/.test(v);
const day=(v:unknown):v is string=>typeof v==="string"&&/^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/.test(v);
const month=(v:string,offset:number)=>{const [y,m]=v.split("-").map(Number);const d=new Date(Date.UTC(y,m-1+offset,1));return `${d.getUTCFullYear()}-${String(d.getUTCMonth()+1).padStart(2,"0")}`;};
export const TREND_MONTHS=6;
export function readDashboard(v:unknown,propertyId:string):DashboardSummary{
 const bad=(what:string)=>{throw new Error("Invalid dashboard: "+what);};
 if(!obj(v)||v.propertyId!==propertyId||!day(v.today)||v.currency!=="TRY")bad("header");
 const s=v as Record<string,unknown>;
 if(!["unitCount","occupiedUnitCount","vacantUnitCount","overdueChargeCount","upcomingChargeCount"].every(k=>int(s[k]))||Number(s.occupiedUnitCount)+Number(s.vacantUnitCount)!==s.unitCount)bad("counts");
 if(![s.receiptsThisMonth,s.overdueAmount,s.upcomingAmount].every(money)||(s.overdueChargeCount===0)!==(s.overdueAmount==="0.00")||(s.upcomingChargeCount===0)!==(s.upcomingAmount==="0.00"))bad("amounts");
 if(!(s.requests===null||obj(s.requests)&&int(s.requests.recorded)&&int(s.requests.inProgress)&&Object.keys(s.requests).length===2))bad("requests");
 const today=String(s.today),current=today.slice(0,7);
 if(!Array.isArray(s.trend)||s.trend.length!==TREND_MONTHS||!s.trend.every((r,i)=>obj(r)&&r.period===month(current,i-TREND_MONTHS+1)&&money(r.receipts)&&money(r.expenses)))bad("trend");
 if((s.trend as {receipts:string}[])[TREND_MONTHS-1].receipts!==s.receiptsThisMonth)bad("receipts");
 if(!Array.isArray(s.unitTypes)||s.unitTypes.length!==UNIT_TYPES.length||!s.unitTypes.every((r,i)=>obj(r)&&r.type===UNIT_TYPES[i]&&int(r.count))||(s.unitTypes as {count:number}[]).reduce((a,r)=>a+r.count,0)!==s.unitCount)bad("unitTypes");
 if(!Array.isArray(s.upcomingDues)||s.upcomingDues.length>4||!s.upcomingDues.every((r,i,a)=>obj(r)&&day(r.dueDate)&&r.dueDate>=today&&(i===0||String(a[i-1].dueDate)<r.dueDate)&&int(r.chargeCount)&&Number(r.chargeCount)>0&&money(r.amount)&&r.amount!=="0.00"))bad("upcomingDues");
 if(typeof s.maintenanceAvailable!=="boolean")bad("maintenance");
 return s as unknown as DashboardSummary;
}
/** Whole-number share for tiles; 0 when there is nothing to divide. */
export const share=(part:number,total:number)=>total>0?Math.round(part*100/total):0;
/** Money string to a plain number for drawing only; amounts shown as text always use the exact string formatter. */
export const plot=(v:string)=>Number(v);
