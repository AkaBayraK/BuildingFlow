export type PenaltyMethod="DAILY_30"|"DAILY_ACTUAL_MONTH";
export type PenaltyLegalBasis="KMK_20"|"MANAGEMENT_PLAN"|"GENERAL_ASSEMBLY"|"COURT_DECISION";
export type PolicyStatus="DRAFT"|"APPROVED"|"CANCELLED";
export type AccrualStatus="ACTIVE"|"WAIVED"|"SUPERSEDED";
export type PenaltySwitch={id:string;effectiveFrom:string;apply:boolean;reason:string;createdBy:string;createdAt:string;replayed:boolean};
export type PenaltyPolicy={id:string;year:number;validFrom:string;validTo:string|null;monthlyRate:string;method:PenaltyMethod;legalBasis:PenaltyLegalBasis;legalReference:string;approvalDocumentId:string|null;status:PolicyStatus;approvedBy:string|null;approvedAt:string|null;statusReason:string|null;createdBy:string;createdAt:string;version:number;replayed:boolean};
export type PenaltySettings={propertyId:string;appliedToday:boolean;statutoryRate:string;statutoryReference:string;defaultMethod:PenaltyMethod;crystallizedThrough:string|null;canDraft:boolean;canApprove:boolean;switches:PenaltySwitch[];policies:PenaltyPolicy[]};
export type PenaltySegment={from:string;to:string;days:number;principal:string;monthlyRate:string;method:PenaltyMethod;policyId:string|null;amount:string};
export type PenaltyAccrual={id:string;from:string;to:string;amount:string;paid:string;balance:string;status:AccrualStatus;statusReason:string|null;statusChangedAt:string|null;version:number;segments:PenaltySegment[]};
export type PenaltyChargeDetail={chargeId:string;unitId:string;unitNumber:string;period:string;dueDate:string;principal:string;principalPaid:string;principalBalance:string;penaltyAccrued:string;penaltyPaid:string;penaltyWaived:string;penaltyBalance:string;pendingPenalty:string;totalDue:string;asOf:string;appliedToday:boolean;accruals:PenaltyAccrual[];pendingSegments:PenaltySegment[]};
export type PenaltyRow={id:string;chargeId:string;unitId:string;unitNumber:string;buildingName:string|null;period:string;dueDate:string;daysOverdue:number;principalBalance:string;penaltyBalance:string;pendingPenalty:string;totalDue:string};
export type PenaltyPage={items:PenaltyRow[];page:number;pageSize:number;totalCount:number;totals:{principalOverdue:string;penaltyOutstanding:string;penaltyCollected:string;penaltyWaived:string;currency:"TRY"}};
export type PenaltyPreviewLine={chargeId:string;period:string;dueDate:string;version:number;principalBalance:string;penaltyDue:string;suggestedPenalty:string;suggestedPrincipal:string};
export type PenaltyPreview={unitId:string;paidDate:string;applied:boolean;principalTotal:string;penaltyTotal:string;unallocated:string;lines:PenaltyPreviewLine[]};
export type PenaltyReport={propertyId:string;currency:"TRY";years:{year:number;accrued:string;collected:string;waived:string;outstanding:string}[];rateHistory:PenaltyPolicy[];switches:PenaltySwitch[]};
export type PenaltyRunResult={through:string;chargeCount:number;accrualCount:number;amount:string};

const object=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const text=(v:unknown):v is string=>typeof v==="string";
const integer=(v:unknown,min=0):v is number=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=min;
const money=(v:unknown):v is string=>text(v)&&/^(?:0|[1-9][0-9]{0,17})\.[0-9]{2}$/.test(v);
const rate=(v:unknown):v is string=>text(v)&&/^[0-5]\.[0-9]{2,4}$/.test(v)&&Number(v)>0&&Number(v)<=5;
const date=(v:unknown):v is string=>text(v)&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&new Date(v+"T00:00:00Z").toISOString().slice(0,10)===v;
const nullableDate=(v:unknown)=>v===null||date(v);
const timestamp=(v:unknown)=>text(v)&&Number.isFinite(Date.parse(v));
const methods:readonly string[]=["DAILY_30","DAILY_ACTUAL_MONTH"];
export const legalBases:readonly PenaltyLegalBasis[]=["KMK_20","MANAGEMENT_PLAN","GENERAL_ASSEMBLY","COURT_DECISION"];
function fail(name:string):never{throw new Error("Invalid late penalty "+name);}
export function cents(value:string):bigint{if(!money(value))fail("money");const [whole,fraction]=value.split(".");return BigInt(whole)*BigInt(100)+BigInt(fraction);}
export function fromCents(value:bigint):string{return `${value/BigInt(100)}.${(value%BigInt(100)).toString().padStart(2,"0")}`;}

function isSwitch(v:unknown):v is PenaltySwitch{return object(v)&&guid(v.id)&&date(v.effectiveFrom)&&typeof v.apply==="boolean"&&text(v.reason)&&v.reason.trim().length>0&&text(v.createdBy)&&timestamp(v.createdAt)&&typeof v.replayed==="boolean";}
export function readSwitch(v:unknown):PenaltySwitch{if(!isSwitch(v))fail("switch");return v;}
function isPolicy(v:unknown):v is PenaltyPolicy{
 if(!object(v)||!guid(v.id)||!date(v.validFrom)||!nullableDate(v.validTo)||(v.validTo!==null&&(v.validTo as string)<(v.validFrom as string))||!integer(v.year,2000)||v.year!==Number((v.validFrom as string).slice(0,4))||
  !rate(v.monthlyRate)||!methods.includes(v.method as string)||!legalBases.includes(v.legalBasis as PenaltyLegalBasis)||!text(v.legalReference)||!(v.approvalDocumentId===null||guid(v.approvalDocumentId))||
  !["DRAFT","APPROVED","CANCELLED"].includes(v.status as string)||!integer(v.version,1)||!timestamp(v.createdAt)||!text(v.createdBy))return false;
 return v.status==="APPROVED"?text(v.approvedBy)&&timestamp(v.approvedAt):v.approvedBy===null&&v.approvedAt===null;
}
export function readPolicy(v:unknown):PenaltyPolicy{if(!isPolicy(v))fail("policy");return v;}
export function readSettings(v:unknown,propertyId:string):PenaltySettings{
 if(!object(v)||v.propertyId!==propertyId||typeof v.appliedToday!=="boolean"||!rate(v.statutoryRate)||!text(v.statutoryReference)||!methods.includes(v.defaultMethod as string)||
  !nullableDate(v.crystallizedThrough)||typeof v.canDraft!=="boolean"||typeof v.canApprove!=="boolean"||!Array.isArray(v.switches)||!v.switches.every(isSwitch)||!Array.isArray(v.policies)||!v.policies.every(isPolicy))fail("settings");
 return v as PenaltySettings;
}
function isSegment(v:unknown):v is PenaltySegment{return object(v)&&date(v.from)&&date(v.to)&&v.to>=v.from&&integer(v.days,1)&&money(v.principal)&&rate(v.monthlyRate)&&methods.includes(v.method as string)&&(v.policyId===null||guid(v.policyId))&&text(v.amount)&&/^[0-9]{1,12}\.[0-9]{6}$/.test(v.amount);}
function isAccrual(v:unknown):v is PenaltyAccrual{
 return object(v)&&guid(v.id)&&date(v.from)&&date(v.to)&&money(v.amount)&&money(v.paid)&&money(v.balance)&&["ACTIVE","WAIVED","SUPERSEDED"].includes(v.status as string)&&
  (v.statusReason===null||text(v.statusReason))&&(v.statusChangedAt===null||timestamp(v.statusChangedAt))&&integer(v.version,1)&&Array.isArray(v.segments)&&v.segments.every(isSegment)&&
  cents(v.paid as string)<=cents(v.amount as string)&&(v.status==="ACTIVE"?cents(v.balance as string)===cents(v.amount as string)-cents(v.paid as string):cents(v.balance as string)===BigInt(0));
}
export function readAccrual(v:unknown):PenaltyAccrual{if(!isAccrual(v))fail("accrual");return v;}
export function readChargeDetail(v:unknown,chargeId:string):PenaltyChargeDetail{
 if(!object(v)||v.chargeId!==chargeId||!guid(v.unitId)||!text(v.unitNumber)||!text(v.period)||!date(v.dueDate)||!date(v.asOf)||typeof v.appliedToday!=="boolean"||
  !["principal","principalPaid","principalBalance","penaltyAccrued","penaltyPaid","penaltyWaived","penaltyBalance","pendingPenalty","totalDue"].every(k=>money(v[k]))||
  !Array.isArray(v.accruals)||!v.accruals.every(isAccrual)||!Array.isArray(v.pendingSegments)||!v.pendingSegments.every(isSegment))fail("charge");
 if(cents(v.totalDue as string)!==cents(v.principalBalance as string)+cents(v.penaltyBalance as string)+cents(v.pendingPenalty as string))fail("total");
 return v as PenaltyChargeDetail;
}
export function readPenaltyPage(v:unknown):PenaltyPage{
 if(!object(v)||!Array.isArray(v.items)||!integer(v.page,1)||!integer(v.pageSize,1)||v.pageSize>100||!integer(v.totalCount)||v.items.length>v.pageSize||!object(v.totals)||v.totals.currency!=="TRY"||
  !["principalOverdue","penaltyOutstanding","penaltyCollected","penaltyWaived"].every(k=>money((v.totals as Record<string,unknown>)[k])))fail("page");
 const items=v.items.map(row=>{
  if(!object(row)||!guid(row.chargeId)||!guid(row.unitId)||!text(row.unitNumber)||!(row.buildingName===null||text(row.buildingName))||!text(row.period)||!date(row.dueDate)||!integer(row.daysOverdue,1)||
   !["principalBalance","penaltyBalance","pendingPenalty","totalDue"].every(k=>money(row[k])))fail("row");
  return {...row,id:row.chargeId} as PenaltyRow;
 });
 if(new Set(items.map(row=>row.id)).size!==items.length)fail("rows");
 return {...(v as PenaltyPage),items};
}
export function readPreview(v:unknown,unitId:string,paidDate:string):PenaltyPreview{
 if(!object(v)||v.unitId!==unitId||v.paidDate!==paidDate||typeof v.applied!=="boolean"||!money(v.principalTotal)||!money(v.penaltyTotal)||!money(v.unallocated)||!Array.isArray(v.lines)||
  !v.lines.every(l=>object(l)&&guid(l.chargeId)&&text(l.period)&&date(l.dueDate)&&integer(l.version,1)&&["principalBalance","penaltyDue","suggestedPenalty","suggestedPrincipal"].every(k=>money(l[k]))))fail("preview");
 return v as PenaltyPreview;
}
export function readReport(v:unknown,propertyId:string):PenaltyReport{
 if(!object(v)||v.propertyId!==propertyId||v.currency!=="TRY"||!Array.isArray(v.years)||!v.years.every(y=>object(y)&&integer(y.year,2000)&&["accrued","collected","waived","outstanding"].every(k=>money(y[k])))||
  !Array.isArray(v.rateHistory)||!v.rateHistory.every(isPolicy)||!Array.isArray(v.switches)||!v.switches.every(isSwitch))fail("report");
 return v as PenaltyReport;
}
export function readRunResult(v:unknown):PenaltyRunResult{
 if(!object(v)||!date(v.through)||!integer(v.chargeCount)||!integer(v.accrualCount)||!money(v.amount))fail("run");
 return v as PenaltyRunResult;
}
// 0 < rate <= 5 with at most four decimals; the backend enforces the same statutory ceiling.
export function normalizeRate(raw:string):string|null{
 const value=raw.trim().replace(",",".");
 if(!/^[0-9](?:\.[0-9]{1,4})?$/.test(value))return null;
 const number=Number(value);return number>0&&number<=5?value:null;
}
// CSV is opened by Excel without a new dependency; formula-like cells are neutralised.
export function toCsv(rows:readonly (readonly (string|number)[])[]):string{
 const cell=(value:string|number)=>{let text=String(value);if(/^[=+\-@\t\r]/.test(text)&&!/^-?\d+(?:[.,]\d+)?$/.test(text))text="'"+text;return /[";\n\r]/.test(text)?`"${text.replaceAll('"','""')}"`:text;};
 return "﻿"+rows.map(row=>row.map(cell).join(";")).join("\r\n");
}
