import {test} from "node:test";
import assert from "node:assert/strict";
import {readFileSync,readdirSync} from "node:fs";
import {canDecide,canRefund,formatIban,isIban,normalizeIban,readBankAccount,readLedgerPage,readMerchant,readOnlinePayment,readOnlinePaymentPage,readProviders,readReceipt,readSettings,readSettlement} from "../src/features/online-payments/model.ts";
import {readPayment} from "../src/features/payments/model.ts";
import {errorCodes} from "../src/services/error-codes.ts";
const property="9b962f3e-8812-459f-a42f-45ec6e3c6eba",unit="24d39d57-021b-4705-87d3-98a091c95f23",charge="c89825a1-beb6-4850-a74d-0b59b5a77af3",payment="c89825a1-beb6-4850-a74d-0b59b5a77af4";
const iban="TR330006100519786457841326";
const account={id:payment,buildingId:null,buildingName:null,bankName:"Ziraat",accountHolder:"Site Yönetimi",iban,currency:"TRY",purpose:"ALL",isDefault:false,validFrom:"2026-01-01",validTo:null,status:"PENDING_APPROVAL",createdByMe:true,createdAt:"2026-10-23T10:00:00Z",decidedAt:null,statusReason:null,version:1};
const line={chargeId:charge,period:"2026-10",chargeKind:"EXTRA",chargeCategory:"REPAIR",description:"Çatı onarımı",kind:"PRINCIPAL",amount:"500.00"};
const intent={id:payment,unitId:unit,unitNumber:"5",buildingName:"A",amount:"525.00",currency:"TRY",status:"SUCCEEDED",providerCode:"SANDBOX",createdAt:"2026-10-23T10:00:00Z",expiresAt:"2026-10-23T10:30:00Z",statusChangedAt:"2026-10-23T10:05:00Z",paidDate:"2026-10-23",feeAmount:"5.25",paymentId:charge,receiptNumber:"BF-2026-000001",failureCode:null,version:2,
 lines:[line,{...line,kind:"PENALTY",amount:"25.00"}],events:[],refunds:[],issues:[]};

test("IBAN check mirrors the backend mod-97 rule for TR accounts only",()=>{
 assert.equal(isIban(iban),true);
 assert.equal(isIban(normalizeIban("tr33 0006 1005 1978 6457 8413 26")),true);
 for(const bad of ["TR330006100519786457841327","TR33000610051978645784132","DE89370400440532013000",""])assert.equal(isIban(bad),false);
 assert.equal(formatIban(iban),"TR33 0006 1005 1978 6457 8413 26");
});
test("bank account reader rejects invalid IBAN, inactive defaults and reversed validity",()=>{
 assert.equal(readBankAccount(account).replayed,false);
 for(const bad of [{iban:"TR330006100519786457841327"},{isDefault:true},{validTo:"2025-12-31"},{purpose:"OTHER"},{status:"DELETED"},{currency:"EUR"}])assert.throws(()=>readBankAccount({...account,...bad}));
 assert.equal(readBankAccount({...account,status:"ACTIVE",isDefault:true,decidedAt:"2026-10-23T11:00:00Z"}).isDefault,true);
});
test("dual control never lets the proposer approve; single approval lets the approver act",()=>{
 assert.equal(canDecide(true,true,true),false);
 assert.equal(canDecide(false,true,true),true);
 assert.equal(canDecide(true,true,false),true);
 assert.equal(canDecide(false,false,true),false);
});
test("merchant keeps only a secret reference and only sandbox",()=>{
 const merchant={id:payment,buildingId:null,buildingName:null,providerCode:"SANDBOX",label:"Site A",merchantId:"M-1",secretReference:"payments:sandbox:a",secretConfigured:true,environment:"SANDBOX",isDefault:true,isEnabled:true,lastCheckedAt:null,lastCheckSucceeded:null,version:1};
 assert.equal(readMerchant(merchant).environment,"SANDBOX");
 assert.throws(()=>readMerchant({...merchant,environment:"LIVE"}));
 assert.throws(()=>readMerchant({...merchant,secretReference:"sk_live 123/abc"}));
});
test("settings and providers are strictly shaped and scoped",()=>{
 const settings={propertyId:property,onlineEnabled:false,partialAllowed:false,showIbanToResidents:true,requireDualApproval:true,receiptPrefix:"BF",commissionPolicy:"ABSORBED",version:0,canEdit:true,canPropose:true};
 assert.equal(readSettings(settings,property).receiptPrefix,"BF");
 assert.throws(()=>readSettings(settings,unit));
 assert.throws(()=>readSettings({...settings,commissionPolicy:"PAYER"},property));
 assert.throws(()=>readSettings({...settings,receiptPrefix:"b"},property));
 assert.equal(readProviders([{code:"SANDBOX",name:"Sandbox",integrated:true,enabled:true,supportsRefund:true,environment:"SANDBOX",version:1}]).length,1);
 assert.throws(()=>readProviders([{code:"X",name:"X",integrated:false,enabled:true,supportsRefund:false,environment:"SANDBOX",version:1}]));
});
test("online payment covers extra charges and penalty lines; success needs a recorded payment",()=>{
 const read=readOnlinePayment(intent);
 assert.equal(read.lines[0].chargeCategory,"REPAIR");
 assert.throws(()=>readOnlinePayment({...intent,paymentId:null}));
 assert.throws(()=>readOnlinePayment({...intent,lines:[{...line,chargeKind:"EXTRA",chargeCategory:null}]}));
 assert.throws(()=>readOnlinePayment({...intent,lines:[]}));
 assert.equal(readOnlinePayment({...intent,events:undefined,refunds:undefined,issues:undefined}).refunds,null);
 const page=readOnlinePaymentPage({items:[intent],page:1,pageSize:10,totalCount:1,summary:{succeededAmount:"525.00",reviewAmount:"0.00",refundedAmount:"0.00",feeAmount:"5.25",pendingCount:0,reviewCount:0,currency:"TRY"}});
 assert.equal(page.items.length,1);
 assert.throws(()=>readOnlinePaymentPage({...page,items:[intent,intent],totalCount:2}));
});
test("only full refunds of settled or review payments without an open request are offered",()=>{
 assert.equal(canRefund(readOnlinePayment(intent)),true);
 assert.equal(canRefund(readOnlinePayment({...intent,status:"PENDING",paymentId:null,paidDate:null})),false);
 const refund={id:unit,intentId:payment,amount:"525.00",reason:"Mükerrer",status:"REQUESTED",requestedByMe:true,createdAt:"2026-10-23T11:00:00Z",decidedAt:null,decisionNote:null,failureCode:null,version:1};
 assert.equal(canRefund(readOnlinePayment({...intent,refunds:[refund]})),false);
 assert.equal(canRefund(readOnlinePayment({...intent,refunds:[{...refund,status:"FAILED",decidedAt:"2026-10-23T12:00:00Z"}]})),true);
});
test("ledger keeps signed counter entries and settlement belongs to the property",()=>{
 const entry={id:unit,unitId:unit,unitNumber:"5",type:"ONLINE_REFUND",amount:"-525.00",occurredOn:"2026-10-24",method:"ONLINE_CARD",paymentId:charge,intentId:payment,refundId:unit,receiptNumber:null,createdAt:"2026-10-24T09:00:00Z"};
 assert.equal(readLedgerPage({items:[entry],page:1,pageSize:20,totalCount:1,totals:{credits:"525.00",debits:"-525.00",net:"0.00",currency:"TRY"}}).items[0].amount,"-525.00");
 assert.throws(()=>readLedgerPage({items:[{...entry,type:"DELETE"}],page:1,pageSize:20,totalCount:1,totals:{credits:"0.00",debits:"0.00",net:"0.00",currency:"TRY"}}));
 const report={propertyId:property,from:"2026-10-01",to:"2026-10-24",currency:"TRY",openIssues:1,days:[{date:"2026-10-23",paymentCount:1,collected:"525.00",fees:"5.25",refunded:"0.00",chargedBack:"0.00",net:"519.75"}]};
 assert.equal(readSettlement(report,property).days.length,1);
 assert.throws(()=>readSettlement(report,unit));
});
test("receipt and manual payments accept the online method and receipt metadata",()=>{
 assert.equal(readReceipt({number:"BF-2026-000001",issuedAt:"2026-10-23T10:05:00Z",propertyName:"Site",unitNumber:"5",buildingName:"A",paidDate:"2026-10-23",method:"ONLINE_CARD",amount:"525.00",currency:"TRY",status:"RECORDED",lines:intent.lines}).lines.length,2);
 const record={id:payment,propertyId:property,unitId:unit,buildingId:null,unitNumber:"5",buildingName:null,requestId:payment,amount:"500.00",currency:"TRY",paidDate:"2026-10-23",method:"ONLINE_CARD",reference:"SBX-1",description:"Online ödeme",status:"RECORDED",version:1,createdAt:"2026-10-23T10:00:00Z",cancellationReason:null,cancelledAt:null,replayed:false,receiptNumber:"BF-2026-000001",bankAccountId:null,bankAccountIban:null,
  allocations:[{id:unit,chargeId:charge,period:"2026-10",amount:"500.00",kind:"PRINCIPAL",accrualId:null,chargeKind:"EXTRA",chargeCategory:"REPAIR"}]};
 assert.equal(readPayment(record,property).receiptNumber,"BF-2026-000001");
 assert.throws(()=>readPayment({...record,reference:null},property));
 assert.throws(()=>readPayment({...record,bankAccountId:unit},property));
 assert.equal(readPayment({...record,method:"BANK_TRANSFER",bankAccountId:unit,bankAccountIban:iban},property).bankAccountIban,iban);
});
test("online payment texts exist in TR and EN, every payment error code is translated and statuses carry icons",()=>{
 const tr=JSON.parse(readFileSync(new URL("../messages/tr.json",import.meta.url)));const en=JSON.parse(readFileSync(new URL("../messages/en.json",import.meta.url)));
 assert.deepEqual(Object.keys(tr.onlinePayments).sort(),Object.keys(en.onlinePayments).sort());
 for(const code of Object.keys(errorCodes).filter(code=>code.startsWith("BF_PAYMENT_"))){assert.ok(tr.apiErrors[code],code);assert.ok(en.apiErrors[code],code);}
 for(const status of ["PENDING","SUCCEEDED","REVIEW","FAILED","EXPIRED","CANCELLED","REFUNDED","CHARGED_BACK"])assert.ok(tr.onlinePayments["intent_"+status]);
 const style=readFileSync(new URL("../src/features/online-payments/payment-style.ts",import.meta.url),"utf8");
 for(const name of ["tabIcon","intentStyle","bankStatusStyle","purposeStyle","refundStyle","ledgerStyle","issueStyle","issueStatusStyle"])assert.match(style,new RegExp("export const "+name));
 const dir=new URL("../src/features/online-payments/",import.meta.url);
 for(const file of readdirSync(dir).filter(name=>name.endsWith(".tsx"))){const source=readFileSync(new URL(file,dir),"utf8");assert.doesNotMatch(source,/localStorage|console\.log|cardNumber|cvv/i,file);}
});
