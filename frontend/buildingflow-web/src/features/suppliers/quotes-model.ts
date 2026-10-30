import {workOrderStatuses,type WorkOrderStatus} from "./work-order-status.ts";
import {supplierId} from "./model.ts";
import {calendarDay} from "../maintenance/model.ts";
export const currencies=["TRY","USD","EUR"] as const;
export type QuoteCurrency=typeof currencies[number];
export type InquirySummary={id:string;tenantId:string;propertyId:string;propertyName:string;categoryId:string;title:string;deadline:string;status:"OPEN"|"AWARDED"|"CANCELLED";version:number};
export type QuoteRow={invitationId:string;supplierId:string;supplierName:string;areaServiceId:string;serviceName:string;quoteId:string|null;netAmount:string|null;vatRate:string|null;vatAmount:string|null;grossAmount:string|null;currency:QuoteCurrency|null;validUntil:string|null;notes:string|null;status:"AWAITING"|"PENDING"|"APPROVED"|"REJECTED"|"NOT_SELECTED";decisionReason:string|null;isOwnEntry:boolean};
export type DraftOrder={id:string;quoteId:string;status:WorkOrderStatus;title:string;amount:string;currency:QuoteCurrency;version:number};
export type InquiryDetail={inquiry:InquirySummary;description:string;cancellationReason:string|null;quotes:QuoteRow[];workOrder:DraftOrder|null};
export type QuoteCandidate={areaServiceId:string;supplierId:string;supplierName:string;serviceName:string};
const object=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const text=(v:unknown,max:number):v is string=>typeof v==="string"&&!!v.trim()&&v.length<=max&&!/[\u0000-\u001f\u007f]/.test(v);
const integer=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const day=(v:unknown):v is string=>calendarDay(v)&&v>="2000-01-01"&&v<="2100-12-31";
export function quoteCents(v:unknown):bigint|null{if(typeof v!=="string"||!/^(?:0|[1-9]\d{0,7})\.\d{2}$/.test(v))return null;return BigInt(v.replace(".",""));}
export function quotePreview(net:string,rate:string){const cents=quoteCents(net),basis=quoteCents(rate);if(cents===null||cents<=BigInt(0)||basis===null||basis>BigInt(10000))return null;const vat=(cents*basis+BigInt(5000))/BigInt(10000),total=cents+vat;if(total>BigInt(9999999999))return null;const money=(v:bigint)=>`${v/BigInt(100)}.${String(v%BigInt(100)).padStart(2,"0")}`;return {vat:money(vat),gross:money(total)};}
export function readInquirySummary(v:unknown,tenant:string):InquirySummary{if(!object(v)||!supplierId(v.id)||v.tenantId!==tenant||!supplierId(v.propertyId)||!supplierId(v.categoryId)||!text(v.propertyName,200)||!text(v.title,200)||!day(v.deadline)||!["OPEN","AWARDED","CANCELLED"].includes(String(v.status))||!integer(v.version,1))throw new Error("Invalid inquiry");return v as InquirySummary;}
export function readQuoteCandidate(v:unknown):QuoteCandidate{if(!object(v)||!supplierId(v.areaServiceId)||!supplierId(v.supplierId)||!text(v.supplierName,200)||!text(v.serviceName,200))throw new Error("Invalid quote candidate");return v as QuoteCandidate;}
export function readInquiry(v:unknown,tenant:string,supplier:string):InquiryDetail{
 if(!object(v))throw new Error("Invalid inquiry detail");const inquiry=readInquirySummary(v.inquiry,tenant);
 if(!text(v.description,4000)||v.cancellationReason!==null&&!text(v.cancellationReason,1000)||inquiry.status==="CANCELLED"&&v.cancellationReason===null||!Array.isArray(v.quotes)||v.quotes.length<1||v.quotes.length>20)throw new Error("Invalid inquiry details");
 const quotes=v.quotes.map((q:unknown)=>{
  if(!object(q)||!supplierId(q.invitationId)||!supplierId(q.supplierId)||!supplierId(q.areaServiceId)||!text(q.supplierName,200)||!text(q.serviceName,200)||typeof q.isOwnEntry!=="boolean"||!["AWAITING","PENDING","APPROVED","REJECTED","NOT_SELECTED"].includes(String(q.status)))throw new Error("Invalid quote row");
  if(q.quoteId===null){if(q.status!=="AWAITING"||q.isOwnEntry||[q.netAmount,q.vatRate,q.vatAmount,q.grossAmount,q.currency,q.validUntil,q.notes,q.decisionReason].some(x=>x!==null))throw new Error("Invalid awaiting quote");}
  else{if(!supplierId(q.quoteId)||q.status==="AWAITING"||!currencies.includes(q.currency as QuoteCurrency)||!day(q.validUntil)||!text(q.notes,2000)||typeof q.netAmount!=="string"||typeof q.vatRate!=="string")throw new Error("Invalid quote values");const preview=quotePreview(q.netAmount,q.vatRate);if(!preview||preview.vat!==q.vatAmount||preview.gross!==q.grossAmount||["APPROVED","REJECTED"].includes(String(q.status))&&!text(q.decisionReason,1000)||!["APPROVED","REJECTED"].includes(String(q.status))&&q.decisionReason!==null)throw new Error("Invalid quote totals or decision");}
  return q as QuoteRow;
 });
 if(new Set(quotes.map(x=>x.supplierId)).size!==quotes.length||new Set(quotes.map(x=>x.invitationId)).size!==quotes.length||!quotes.some(x=>x.supplierId===supplier)||quotes.filter(x=>x.status==="APPROVED").length!==(inquiry.status==="AWARDED"?1:0))throw new Error("Invalid inquiry invitations");
 let workOrder:DraftOrder|null=null;if(v.workOrder!==null){const w=v.workOrder;if(!object(w)||!supplierId(w.id)||!supplierId(w.quoteId)||!workOrderStatuses.includes(w.status as WorkOrderStatus)||!text(w.title,200)||!integer(w.version,1))throw new Error("Invalid draft order");const selected=quotes.find(q=>q.quoteId===w.quoteId&&q.status==="APPROVED");if(!selected||selected.grossAmount!==w.amount||selected.currency!==w.currency||w.title!==inquiry.title)throw new Error("Invalid order snapshot");workOrder=w as DraftOrder;}
 return {inquiry,description:v.description,cancellationReason:v.cancellationReason as string|null,quotes,workOrder};
}
export function readInquiryMutation(v:unknown,tenant:string,supplier:string){if(!object(v)||typeof v.replayed!=="boolean"||!integer(v.appliedVersion,1))throw new Error("Invalid inquiry mutation");const record=readInquiry(v.record,tenant,supplier);if(Number(v.appliedVersion)>record.inquiry.version||!v.replayed&&v.appliedVersion!==record.inquiry.version)throw new Error("Invalid inquiry version");return {record,replayed:v.replayed,appliedVersion:Number(v.appliedVersion)};}
export function quoteComparison(rows:QuoteRow[]){return currencies.map(currency=>({currency,rows:rows.filter(q=>q.currency===currency&&q.quoteId!==null).sort((a,b)=>{const x=quoteCents(a.grossAmount)!,y=quoteCents(b.grossAmount)!;return x<y?-1:x>y?1:a.supplierName.localeCompare(b.supplierName);})})).filter(x=>x.rows.length);}
export function readInquiryHistory(v:unknown){if(!object(v)||!supplierId(v.id)||!["CREATE","QUOTE","APPROVE","REJECT","CANCEL","CONVERT"].includes(String(v.action))||!integer(v.appliedVersion,1)||typeof v.snapshot!=="string"||v.snapshot.length>20000||typeof v.createdAt!=="string"||!Number.isFinite(Date.parse(v.createdAt)))throw new Error("Invalid inquiry history");return v as {id:string;action:"CREATE"|"QUOTE"|"APPROVE"|"REJECT"|"CANCEL"|"CONVERT";appliedVersion:number;snapshot:string;createdAt:string};}
