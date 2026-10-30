import {supplierId} from "./model.ts";
import {calendarDay} from "../maintenance/model.ts";
import {normalizeAmount} from "../dues/model.ts";
export const invoiceStatuses=["RECORDED","APPROVED","PAID","RECONCILED","CANCELLED"] as const;
export const invoiceCategories=["MAINTENANCE","CLEANING","STAFF","ENERGY","SECURITY","INSURANCE","ADMINISTRATION","OTHER"] as const;
export const invoiceActions=["CREATE","APPROVE","PAY","RECONCILE","CANCEL"] as const;
export type InvoiceStatus=typeof invoiceStatuses[number];
export type InvoiceCategory=typeof invoiceCategories[number];
export type InvoiceAction=typeof invoiceActions[number];
export type SupplierInvoice={id:string;tenantId:string;supplierId:string;propertyId:string;propertyName:string;number:string;issueDate:string;dueDate:string;netAmount:string;vatRate:string;vatAmount:string;grossAmount:string;currency:"TRY";category:InvoiceCategory;description:string;workOrderId:string|null;contractId:string|null;correctsInvoiceId:string|null;expenseId:string|null;status:InvoiceStatus;isOverdue:boolean;recordedByMe:boolean;paidOn:string|null;paymentReference:string|null;reconciledOn:string|null;reconciliationNote:string|null;cancellationReason:string|null;version:number;createdAt:string};
export type InvoiceSummary={recordedTotal:string;approvedTotal:string;paidTotal:string;reconciledTotal:string;overdueCount:number};
export type InvoiceHistory={id:string;action:InvoiceAction;appliedVersion:number;snapshot:string;createdAt:string};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const text=(v:unknown,max:number)=>typeof v==="string"&&!!v.trim()&&v.length<=max&&!/[\u0000-\u001f\u007f]/.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const stamp=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const day=(v:unknown)=>calendarDay(v)&&String(v)>="2000-01-01"&&String(v)<="2100-12-31";
const nullable=(v:unknown,check:(x:unknown)=>boolean)=>v===null||check(v);
const money=(v:unknown)=>typeof v==="string"&&normalizeAmount(v)===v;
const cents=(v:string)=>BigInt(v.replace(".",""));
export function vatPreview(net:string,rate:string){
 const n=normalizeAmount(net);if(!n||!/^(\d{1,3})(\.\d{1,2})?$/.test(rate))return null;const r=Number(rate);if(r>100)return null;
 const rateBasis=BigInt(Math.round(r*100)),netCents=cents(n),vat=(netCents*rateBasis+BigInt(5000))/BigInt(10000),gross=netCents+vat;if(gross>BigInt(9999999999))return null;
 const fmt=(x:bigint)=>`${x/BigInt(100)}.${String(x%BigInt(100)).padStart(2,"0")}`;return {vat:fmt(vat),gross:fmt(gross)};
}
export function readInvoice(v:unknown,tenant:string,supplier:string):SupplierInvoice{
 if(!obj(v)||!supplierId(v.id)||v.tenantId!==tenant||v.supplierId!==supplier||!supplierId(v.propertyId)||!text(v.propertyName,200)||!text(v.number,50)||!day(v.issueDate)||!day(v.dueDate)||String(v.dueDate)<String(v.issueDate)||!int(v.version,1)||!stamp(v.createdAt))throw new Error("Invalid supplier invoice");
 if(!money(v.netAmount)||!money(v.grossAmount)||typeof v.vatAmount!=="string"||!/^\d+\.\d{2}$/.test(v.vatAmount)||typeof v.vatRate!=="string"||!/^\d{1,3}\.\d{2}$/.test(v.vatRate)||Number(v.vatRate)>100||cents(String(v.netAmount))+cents(v.vatAmount)!==cents(String(v.grossAmount))||vatPreview(String(v.netAmount),v.vatRate)?.vat!==v.vatAmount||v.currency!=="TRY")throw new Error("Invalid invoice amounts");
 if(!invoiceCategories.includes(v.category as InvoiceCategory)||!text(v.description,400)||![v.workOrderId,v.contractId,v.correctsInvoiceId,v.expenseId].every(x=>nullable(x,supplierId))||!invoiceStatuses.includes(v.status as InvoiceStatus)||typeof v.isOverdue!=="boolean"||typeof v.recordedByMe!=="boolean")throw new Error("Invalid invoice links");
 const s=v.status as InvoiceStatus,booked=["APPROVED","PAID","RECONCILED"].includes(s),paid=s==="PAID"||s==="RECONCILED";
 if(booked!==(v.expenseId!==null)&&s!=="CANCELLED"||paid!==(day(v.paidOn)&&text(v.paymentReference,100))||!paid&&(v.paidOn!==null||v.paymentReference!==null)||paid&&String(v.paidOn)<String(v.issueDate))throw new Error("Invalid invoice payment state");
 if((s==="RECONCILED")!==(day(v.reconciledOn)&&text(v.reconciliationNote,1000))||s!=="RECONCILED"&&(v.reconciledOn!==null||v.reconciliationNote!==null)||s==="RECONCILED"&&String(v.reconciledOn)<String(v.paidOn))throw new Error("Invalid reconciliation");
 if((s==="CANCELLED")!==text(v.cancellationReason,1000)||s!=="CANCELLED"&&v.cancellationReason!==null||v.isOverdue&&!["RECORDED","APPROVED"].includes(s))throw new Error("Invalid invoice cancellation");
 return v as SupplierInvoice;
}
export function readInvoicePage(v:unknown,tenant:string,supplier:string){
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||Number(v.pageSize)>100||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize)||!obj(v.summary))throw new Error("Invalid invoice page");
 const s=v.summary;if(![s.recordedTotal,s.approvedTotal,s.paidTotal,s.reconciledTotal].every(x=>typeof x==="string"&&/^\d+\.\d{2}$/.test(x))||!int(s.overdueCount))throw new Error("Invalid invoice summary");
 const items=v.items.map(x=>readInvoice(x,tenant,supplier));if(new Set(items.map(x=>x.id)).size!==items.length)throw new Error("Duplicate invoices");
 return {items,page:Number(v.page),pageSize:Number(v.pageSize),totalCount:Number(v.totalCount),summary:s as InvoiceSummary};
}
export function readInvoiceMutation(v:unknown,tenant:string,supplier:string,id?:string){if(!obj(v)||typeof v.replayed!=="boolean"||!int(v.appliedVersion,1))throw new Error("Invalid invoice result");const record=readInvoice(v.record,tenant,supplier);if(id&&record.id!==id||Number(v.appliedVersion)>record.version||!v.replayed&&v.appliedVersion!==record.version)throw new Error("Invalid invoice version");return {record,replayed:v.replayed};}
export function readInvoiceHistory(v:unknown):InvoiceHistory{if(!obj(v)||!supplierId(v.id)||!invoiceActions.includes(v.action as InvoiceAction)||!int(v.appliedVersion,1)||typeof v.snapshot!=="string"||v.snapshot.length>20000||!stamp(v.createdAt))throw new Error("Invalid invoice history");return v as InvoiceHistory;}
export function invoiceNumberKey(value:string){return value.trim().toUpperCase().replace(/[\s\-/.]/g,"");}
export type InvoiceForm={number:string;issueDate:string;dueDate:string;netAmount:string;vatRate:string;category:InvoiceCategory;description:string;contractId:string;workOrderId:string};
export function invoiceForm():InvoiceForm{return {number:"",issueDate:"",dueDate:"",netAmount:"",vatRate:"20",category:"MAINTENANCE",description:"",contractId:"",workOrderId:""};}
export function invalidInvoiceField(form:InvoiceForm,today:string):keyof InvoiceForm|null{
 if(!text(form.number.trim(),50)||!invoiceNumberKey(form.number))return "number";if(!day(form.issueDate)||form.issueDate>today)return "issueDate";if(!day(form.dueDate)||form.dueDate<form.issueDate)return "dueDate";
 if(!normalizeAmount(form.netAmount))return "netAmount";if(!vatPreview(form.netAmount,form.vatRate))return "vatRate";if(!text(form.description.trim(),400))return "description";return null;
}
export function invoiceBody(form:InvoiceForm,propertyId:string,correctsInvoiceId:string|null){const rate=Number(form.vatRate).toFixed(2);return {propertyId,number:form.number.trim(),issueDate:form.issueDate,dueDate:form.dueDate,netAmount:normalizeAmount(form.netAmount),vatRate:rate,category:form.category,description:form.description.trim(),workOrderId:form.workOrderId||null,contractId:form.contractId||null,correctsInvoiceId};}
