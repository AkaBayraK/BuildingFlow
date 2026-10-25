import type {PagedRows} from "@/services/api";
import type {ChargeCategory,ChargeKind} from "@/features/dues/charge-kinds";

export const intentStatuses=["PENDING","SUCCEEDED","REVIEW","FAILED","EXPIRED","CANCELLED","REFUNDED","CHARGED_BACK"] as const;
export const bankAccountStatuses=["PENDING_APPROVAL","ACTIVE","REJECTED","CLOSED"] as const;
export const bankAccountPurposes=["ALL","DUES","EXTRA"] as const;
export const refundStatuses=["REQUESTED","REJECTED","SUCCEEDED","FAILED"] as const;
export const ledgerTypes=["COLLECTION","COLLECTION_REVERSAL","UNALLOCATED_RECEIPT","ONLINE_REFUND","CHARGEBACK"] as const;
export const issueTypes=["DEBT_CHANGED","CHARGEBACK_UNREVERSED","PROVIDER_MISMATCH","REFUND_UNCONFIRMED"] as const;
export const issueStatuses=["OPEN","RESOLVED"] as const;
export type IntentStatus=typeof intentStatuses[number];
export type BankAccountStatus=typeof bankAccountStatuses[number];
export type BankAccountPurpose=typeof bankAccountPurposes[number];
export type RefundStatus=typeof refundStatuses[number];
export type LedgerType=typeof ledgerTypes[number];
export type IssueType=typeof issueTypes[number];
export type IssueStatus=typeof issueStatuses[number];

export type PaymentProvider={code:string;name:string;integrated:boolean;enabled:boolean;supportsRefund:boolean;environment:string;version:number};
export type PaymentSettings={propertyId:string;onlineEnabled:boolean;partialAllowed:boolean;showIbanToResidents:boolean;requireDualApproval:boolean;receiptPrefix:string;commissionPolicy:"ABSORBED";version:number;canEdit:boolean;canPropose:boolean};
export type BankAccount={id:string;buildingId:string|null;buildingName:string|null;bankName:string;accountHolder:string;iban:string;currency:"TRY";purpose:BankAccountPurpose;isDefault:boolean;validFrom:string;validTo:string|null;status:BankAccountStatus;createdByMe:boolean;createdAt:string;decidedAt:string|null;statusReason:string|null;version:number;replayed:boolean};
export type MerchantAccount={id:string;buildingId:string|null;buildingName:string|null;providerCode:string;label:string;merchantId:string;secretReference:string;secretConfigured:boolean;environment:"SANDBOX";isDefault:boolean;isEnabled:boolean;lastCheckedAt:string|null;lastCheckSucceeded:boolean|null;version:number;replayed:boolean};
export type IntentLine={chargeId:string;period:string;chargeKind:ChargeKind;chargeCategory:ChargeCategory|null;description:string;kind:"PRINCIPAL"|"PENALTY";amount:string};
export type IntentEvent={eventType:string;outcome:string|null;occurredAt:string;processedAt:string|null};
export type Refund={id:string;intentId:string;amount:string;reason:string;status:RefundStatus;requestedByMe:boolean;createdAt:string;decidedAt:string|null;decisionNote:string|null;failureCode:string|null;version:number;replayed:boolean};
export type ReconciliationIssue={id:string;intentId:string|null;type:IssueType;status:IssueStatus;detail:string;createdAt:string;resolvedAt:string|null;resolutionNote:string|null;version:number};
export type OnlinePayment={id:string;unitId:string;unitNumber:string;buildingName:string|null;amount:string;currency:"TRY";status:IntentStatus;providerCode:string;createdAt:string;expiresAt:string;statusChangedAt:string;paidDate:string|null;feeAmount:string|null;paymentId:string|null;receiptNumber:string|null;failureCode:string|null;version:number;lines:IntentLine[];events?:IntentEvent[]|null;refunds?:Refund[]|null;issues?:ReconciliationIssue[]|null};
export type OnlinePaymentPage=PagedRows<OnlinePayment>&{summary:{succeededAmount:string;reviewAmount:string;refundedAmount:string;feeAmount:string;pendingCount:number;reviewCount:number;currency:"TRY"}};
export type LedgerEntry={id:string;unitId:string;unitNumber:string;type:LedgerType;amount:string;occurredOn:string;method:"CASH"|"BANK_TRANSFER"|"ONLINE_CARD";paymentId:string|null;intentId:string|null;refundId:string|null;receiptNumber:string|null;createdAt:string};
export type LedgerPage=PagedRows<LedgerEntry>&{totals:{credits:string;debits:string;net:string;currency:"TRY"}};
export type SettlementDay={date:string;paymentCount:number;collected:string;fees:string;refunded:string;chargedBack:string;net:string};
export type SettlementReport={propertyId:string;from:string;to:string;currency:"TRY";days:SettlementDay[];openIssues:number};
export type ReconciliationRun={checked:number;settled:number;expired:number;issuesOpened:number};
export type ReceiptLine={period:string;chargeKind:ChargeKind;chargeCategory:ChargeCategory|null;description:string;kind:"PRINCIPAL"|"PENALTY";amount:string};
export type Receipt={number:string;issuedAt:string;propertyName:string;unitNumber:string;buildingName:string|null;paidDate:string;method:string;amount:string;currency:"TRY";status:string;lines:ReceiptLine[]};

const object=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const nullableGuid=(v:unknown)=>v===null||guid(v);
const text=(v:unknown):v is string=>typeof v==="string";
const filled=(v:unknown):v is string=>text(v)&&v.trim().length>0;
const nullableText=(v:unknown)=>v===null||text(v);
const bool=(v:unknown):v is boolean=>typeof v==="boolean";
const integer=(v:unknown,min=0):v is number=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=min;
const money=(v:unknown):v is string=>text(v)&&/^(?:0|[1-9][0-9]{0,17})\.[0-9]{2}$/.test(v);
const signedMoney=(v:unknown):v is string=>text(v)&&/^-?(?:0|[1-9][0-9]{0,17})\.[0-9]{2}$/.test(v);
const date=(v:unknown):v is string=>text(v)&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&new Date(v+"T00:00:00Z").toISOString().slice(0,10)===v;
const nullableDate=(v:unknown)=>v===null||date(v);
const timestamp=(v:unknown):v is string=>text(v)&&Number.isFinite(Date.parse(v));
const nullableTimestamp=(v:unknown)=>v===null||timestamp(v);
const oneOf=<T extends string>(values:readonly T[],v:unknown):v is T=>text(v)&&(values as readonly string[]).includes(v);
const kinds=["DUES","EXTRA"] as const;
const categories=["FIXTURE","REPAIR","HEATING","UTILITY","INSURANCE","OTHER"] as const;
function fail(name:string):never{throw new Error("Invalid online payment "+name);}
function page<T extends {id:string}>(v:unknown,name:string,read:(item:unknown)=>T):PagedRows<T>&Record<string,unknown>{
 if(!object(v)||!Array.isArray(v.items)||!integer(v.page,1)||!integer(v.pageSize,1)||v.pageSize>100||!integer(v.totalCount)||v.items.length>v.pageSize||v.items.length>v.totalCount)fail(name);
 const items=v.items.map(read);
 if(new Set(items.map(item=>item.id)).size!==items.length)fail(name+" rows");
 return {...v,items} as PagedRows<T>&Record<string,unknown>;
}

export function readProviders(v:unknown):PaymentProvider[]{
 if(!Array.isArray(v)||!v.every(p=>object(p)&&filled(p.code)&&filled(p.name)&&bool(p.integrated)&&bool(p.enabled)&&bool(p.supportsRefund)&&filled(p.environment)&&integer(p.version)&&(p.integrated||!p.enabled)))fail("providers");
 return v as PaymentProvider[];
}
export function readSettings(v:unknown,propertyId:string):PaymentSettings{
 if(!object(v)||v.propertyId!==propertyId||!["onlineEnabled","partialAllowed","showIbanToResidents","requireDualApproval","canEdit","canPropose"].every(k=>bool(v[k]))||
  !text(v.receiptPrefix)||!/^[A-Z0-9]{2,10}$/.test(v.receiptPrefix)||v.commissionPolicy!=="ABSORBED"||!integer(v.version))fail("settings");
 return v as PaymentSettings;
}
export function readBankAccount(v:unknown):BankAccount{
 if(!object(v)||!guid(v.id)||!nullableGuid(v.buildingId)||!nullableText(v.buildingName)||!filled(v.bankName)||!filled(v.accountHolder)||!text(v.iban)||!isIban(v.iban)||v.currency!=="TRY"||
  !oneOf(bankAccountPurposes,v.purpose)||!bool(v.isDefault)||!date(v.validFrom)||!nullableDate(v.validTo)||(v.validTo!==null&&(v.validTo as string)<v.validFrom)||!oneOf(bankAccountStatuses,v.status)||
  !bool(v.createdByMe)||!timestamp(v.createdAt)||!nullableTimestamp(v.decidedAt)||!nullableText(v.statusReason)||!integer(v.version,1)||(v.isDefault&&v.status!=="ACTIVE"))fail("bank account");
 return {...v,replayed:v.replayed===true} as BankAccount;
}
export function readMerchant(v:unknown):MerchantAccount{
 if(!object(v)||!guid(v.id)||!nullableGuid(v.buildingId)||!nullableText(v.buildingName)||!filled(v.providerCode)||!filled(v.label)||!filled(v.merchantId)||!text(v.secretReference)||!/^[A-Za-z0-9_:-]{1,100}$/.test(v.secretReference)||
  !bool(v.secretConfigured)||v.environment!=="SANDBOX"||!bool(v.isDefault)||!bool(v.isEnabled)||!nullableTimestamp(v.lastCheckedAt)||!(v.lastCheckSucceeded===null||bool(v.lastCheckSucceeded))||!integer(v.version,1))fail("merchant");
 return {...v,replayed:v.replayed===true} as MerchantAccount;
}
export const readBankAccountPage=(v:unknown)=>page(v,"bank accounts",readBankAccount) as PagedRows<BankAccount>;
export const readMerchantPage=(v:unknown)=>page(v,"merchants",readMerchant) as PagedRows<MerchantAccount>;
function isLine(v:unknown):v is IntentLine{
 return object(v)&&text(v.period)&&oneOf(kinds,v.chargeKind)&&(v.chargeKind==="EXTRA"?oneOf(categories,v.chargeCategory):v.chargeCategory==null)&&text(v.description)&&oneOf(["PRINCIPAL","PENALTY"],v.kind)&&money(v.amount);
}
export function readRefund(v:unknown):Refund{
 if(!object(v)||!guid(v.id)||!guid(v.intentId)||!money(v.amount)||!filled(v.reason)||!oneOf(refundStatuses,v.status)||!bool(v.requestedByMe)||!timestamp(v.createdAt)||!nullableTimestamp(v.decidedAt)||
  !nullableText(v.decisionNote)||!nullableText(v.failureCode)||!integer(v.version,1))fail("refund");
 return {...v,replayed:v.replayed===true} as Refund;
}
export function readIssue(v:unknown):ReconciliationIssue{
 if(!object(v)||!guid(v.id)||!nullableGuid(v.intentId)||!oneOf(issueTypes,v.type)||!oneOf(issueStatuses,v.status)||!text(v.detail)||!timestamp(v.createdAt)||!nullableTimestamp(v.resolvedAt)||!nullableText(v.resolutionNote)||
  !integer(v.version,1)||(v.status==="RESOLVED")!==(v.resolvedAt!==null))fail("issue");
 return v as ReconciliationIssue;
}
export const readIssuePage=(v:unknown)=>page(v,"issues",readIssue) as PagedRows<ReconciliationIssue>;
export function readOnlinePayment(v:unknown):OnlinePayment{
 if(!object(v)||!guid(v.id)||!guid(v.unitId)||!text(v.unitNumber)||!nullableText(v.buildingName)||!money(v.amount)||v.currency!=="TRY"||!oneOf(intentStatuses,v.status)||!filled(v.providerCode)||
  !timestamp(v.createdAt)||!timestamp(v.expiresAt)||!timestamp(v.statusChangedAt)||!nullableDate(v.paidDate)||!(v.feeAmount===null||money(v.feeAmount))||!nullableGuid(v.paymentId)||!nullableText(v.receiptNumber)||
  !nullableText(v.failureCode)||!integer(v.version,1)||!Array.isArray(v.lines)||!v.lines.length||!v.lines.every(line=>isLine(line)&&guid(line.chargeId)))fail("intent");
 if(v.status==="SUCCEEDED"&&(!guid(v.paymentId)||!date(v.paidDate)))fail("settled intent");
 const events=v.events==null?null:Array.isArray(v.events)&&v.events.every(e=>object(e)&&filled(e.eventType)&&nullableText(e.outcome)&&timestamp(e.occurredAt)&&nullableTimestamp(e.processedAt))?v.events:fail("events");
 const refunds=v.refunds==null?null:Array.isArray(v.refunds)?v.refunds.map(readRefund):fail("refunds");
 const issues=v.issues==null?null:Array.isArray(v.issues)?v.issues.map(readIssue):fail("issues");
 return {...v,events,refunds,issues} as OnlinePayment;
}
export function readOnlinePaymentPage(v:unknown):OnlinePaymentPage{
 const rows=page(v,"intents",readOnlinePayment);const s=rows.summary;
 if(!object(s)||!["succeededAmount","reviewAmount","refundedAmount","feeAmount"].every(k=>money(s[k]))||!integer(s.pendingCount)||!integer(s.reviewCount)||s.currency!=="TRY")fail("summary");
 return rows as OnlinePaymentPage;
}
function readLedgerEntry(v:unknown):LedgerEntry{
 if(!object(v)||!guid(v.id)||!guid(v.unitId)||!text(v.unitNumber)||!oneOf(ledgerTypes,v.type)||!signedMoney(v.amount)||!date(v.occurredOn)||!oneOf(["CASH","BANK_TRANSFER","ONLINE_CARD"],v.method)||
  !nullableGuid(v.paymentId)||!nullableGuid(v.intentId)||!nullableGuid(v.refundId)||!nullableText(v.receiptNumber)||!timestamp(v.createdAt))fail("ledger entry");
 return v as LedgerEntry;
}
export function readLedgerPage(v:unknown):LedgerPage{
 const rows=page(v,"ledger",readLedgerEntry);const totals=rows.totals;
 if(!object(totals)||!["credits","debits","net"].every(k=>signedMoney(totals[k]))||totals.currency!=="TRY")fail("ledger totals");
 return rows as LedgerPage;
}
export function readSettlement(v:unknown,propertyId:string):SettlementReport{
 if(!object(v)||v.propertyId!==propertyId||!date(v.from)||!date(v.to)||v.to<v.from||v.currency!=="TRY"||!integer(v.openIssues)||!Array.isArray(v.days)||
  !v.days.every(d=>object(d)&&date(d.date)&&integer(d.paymentCount)&&["collected","fees","refunded","chargedBack"].every(k=>money(d[k]))&&signedMoney(d.net)))fail("settlement");
 return v as SettlementReport;
}
export function readRun(v:unknown):ReconciliationRun{
 if(!object(v)||!["checked","settled","expired","issuesOpened"].every(k=>integer(v[k])))fail("run");
 return v as ReconciliationRun;
}
export function readReceipt(v:unknown):Receipt{
 if(!object(v)||!filled(v.number)||!timestamp(v.issuedAt)||!filled(v.propertyName)||!text(v.unitNumber)||!nullableText(v.buildingName)||!date(v.paidDate)||!filled(v.method)||!money(v.amount)||v.currency!=="TRY"||!filled(v.status)||
  !Array.isArray(v.lines)||!v.lines.length||!v.lines.every(isLine))fail("receipt");
 return v as Receipt;
}

export function normalizeIban(raw:string):string{return raw.replace(/\s+/g,"").toUpperCase();}
// Same ISO 13616 mod-97 rule as the backend; only TRY (TR) accounts are accepted.
export function isIban(value:string):boolean{
 if(!/^TR[0-9]{24}$/.test(value))return false;
 let rest=0;
 for(const char of value.slice(4)+"2927"+value.slice(2,4))rest=(rest*10+Number(char))%97;
 return rest===1;
}
export function formatIban(value:string):string{return value.replace(/(.{4})/g,"$1 ").trim();}
export function canRefund(payment:OnlinePayment):boolean{
 return (payment.status==="SUCCEEDED"||payment.status==="REVIEW")&&!(payment.refunds??[]).some(refund=>refund.status==="REQUESTED"||refund.status==="SUCCEEDED");
}
// Dual control mirrors the backend: whoever proposed a record never approves it.
export const canDecide=(createdByMe:boolean,canApprove:boolean,dualApproval=true)=>canApprove&&(!dualApproval||!createdByMe);
