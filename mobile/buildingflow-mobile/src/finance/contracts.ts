import {contextKey,type Context} from '../auth/contracts.ts';
import {parseCharge,money,date,time,id,type Charge} from '../owner/contracts.ts';
export const financeRoles=['SUPER_ADMIN','SITE_ADMIN','MANAGER','ACCOUNTANT','OWNER','TENANT'];
export type View='charges'|'payments';
export type Query={view:View;page:number;chargeId:string|null};
export type Payment={id:string;chargeId:string;unitNumber:string;period:string;amount:string;paidDate:string;cancelled:boolean;kind:'PRINCIPAL'|'PENALTY'};
// Penalty lines are calculated by the server only; the app displays them and never recomputes.
export type PenaltyLine={from:string;to:string;days:number;principal:string;monthlyRate:string;amount:string;status:'ACCRUED'|'PENDING'|'WAIVED'};
export type Snapshot={userId:string;contextKey:string;asOf:string;propertyName:string;view:View;page:number;chargeId:string|null;total:number;summary:{amount:string;paid:string;balance:string;overdue:string;penalty:string};charges:{charge:Charge;unitNumber:string}[];payments:Payment[];penaltyLines:PenaltyLine[]};
function text(v:unknown,max:number){if(typeof v!=='string'||!v.trim()||v.length>max)throw Error('Eksik finans bilgisi.');return v;}
export function validateQuery(c:Context,q:Query){
 if(!financeRoles.includes(c.role)||(['OWNER','TENANT'].includes(c.role)?c.unitId===null:c.unitId!==null)||!['charges','payments'].includes(q.view)||!Number.isSafeInteger(q.page)||q.page<1||q.page>10000)throw Error('Finans bağlamı geçersiz.');
 if(q.chargeId!==null)id(q.chargeId);
}
export function parseFinance(value:unknown,user:string,c:Context,q:Query):Snapshot{
 validateQuery(c,q);if(!value||typeof value!=='object')throw Error('Geçersiz finans yanıtı.');const v=value as Snapshot;
 if(v.userId!==user||v.contextKey!==contextKey(c)||v.view!==q.view||v.page!==q.page||v.chargeId!==q.chargeId||!Number.isSafeInteger(v.total)||v.total<0||!v.summary||!Array.isArray(v.charges)||!Array.isArray(v.payments))throw Error('Finans kapsamı eşleşmiyor.');
 const s=v.summary,summary={amount:money(s.amount),paid:money(s.paid),balance:money(s.balance),overdue:money(s.overdue),penalty:s.penalty===undefined?'0.00':money(s.penalty)};
 const cents=(m:string)=>BigInt(m.replace('.',''));
 if(cents(summary.paid)+cents(summary.balance)!==cents(summary.amount)||cents(summary.overdue)>cents(summary.balance))throw Error('Finans toplamları tutarsız.');
 const expected=Math.min(50,Math.max(0,v.total-(q.page-1)*50));
 if(v.charges.length!==(q.view==='charges'?expected:0)||v.payments.length!==(q.view==='payments'?expected:0))throw Error('Finans sayfası eksik.');
 const ids=new Set<string>();const unique=(value:string)=>{const result=id(value);if(ids.has(result))throw Error('Tekrarlanan finans kaydı.');ids.add(result);return result;};
 const charges=v.charges.map(r=>{const charge=parseCharge(r.charge);unique(charge.id);if(q.chargeId!==null&&charge.id!==q.chargeId)throw Error('Yanlış tahakkuk detayı.');return {charge,unitNumber:text(r.unitNumber,30)};});
 const payments=v.payments.map(p=>{const chargeId=id(p.chargeId);if(q.chargeId!==null&&chargeId!==q.chargeId||typeof p.cancelled!=='boolean'||!/^\d{4}-(0[1-9]|1[0-2])$/.test(p.period)||money(p.amount)==='0.00')throw Error('Tahsilat kaydı geçersiz.');const kind=p.kind??'PRINCIPAL';if(kind!=='PRINCIPAL'&&kind!=='PENALTY')throw Error('Tahsilat türü geçersiz.');return {id:unique(p.id),chargeId,unitNumber:text(p.unitNumber,30),period:p.period,amount:money(p.amount),paidDate:date(p.paidDate),cancelled:p.cancelled,kind};});
 const raw=(v as {penaltyLines?:unknown}).penaltyLines;if(raw!=null&&(!Array.isArray(raw)||(q.chargeId===null&&raw.length>0)||raw.length>500))throw Error('Gecikme detayı geçersiz.');
 const penaltyLines=(raw??[]).map((l:PenaltyLine)=>{if(!Number.isSafeInteger(l.days)||l.days<1||!['ACCRUED','PENDING','WAIVED'].includes(l.status)||typeof l.monthlyRate!=='string'||!/^[0-5]\.\d{2,4}$/.test(l.monthlyRate))throw Error('Gecikme detayı geçersiz.');return {from:date(l.from),to:date(l.to),days:l.days,principal:money(l.principal),monthlyRate:l.monthlyRate,amount:money(l.amount),status:l.status};});
 return {userId:user,contextKey:contextKey(c),asOf:time(v.asOf),propertyName:text(v.propertyName,200),...q,total:v.total,summary,charges,payments,penaltyLines};
}
