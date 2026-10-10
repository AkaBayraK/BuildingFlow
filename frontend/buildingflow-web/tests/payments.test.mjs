import {test} from "node:test";
import assert from "node:assert/strict";
import {allocationTotal,moneyCents,readPayment,readPaymentPage} from "../src/features/payments/model.ts";
import {readCharge} from "../src/features/dues/model.ts";
import {isWorkspaceReturnPath} from "../src/auth/return-path.ts";
import {managementSection,propertyIdFromPath} from "../src/features/navigation/routes.ts";
import {problemDetails} from "../src/services/problem.ts";
const property="9b962f3e-8812-459f-a42f-45ec6e3c6eba",unit="24d39d57-021b-4705-87d3-98a091c95f23",chargeId="c89825a1-beb6-4850-a74d-0b59b5a77af3",partId="c89825a1-beb6-4850-a74d-0b59b5a77af4";
const record={id:property,propertyId:property,unitId:unit,buildingId:null,unitNumber:"1",buildingName:null,requestId:unit,amount:"12.35",currency:"TRY",paidDate:"2026-10-09",method:"CASH",reference:null,description:"Received cash",status:"RECORDED",version:1,createdAt:"2026-10-09T01:00:00Z",cancellationReason:null,cancelledAt:null,allocations:[{id:partId,chargeId,period:"2026-10",amount:"12.35"}],replayed:false};
test("payment distribution preserves cents and enforces positive bounded totals",()=>{
 assert.equal(allocationTotal(["0.10","0.20"]),"0.30");assert.equal(allocationTotal(["12.35","0.01"]),"12.36");assert.equal(allocationTotal(["99999999.98","0.01"]),"99999999.99");
 assert.equal(moneyCents("999999999999999999.99"),BigInt("99999999999999999999"));
 for(const values of [[],Array(101).fill("0.01"),["0.00"],["-1.00"],["12.351"],["12,35"],["1e2"],["100000000.00"],["99999999.99","0.01"]])assert.equal(allocationTotal(values),null);
});
test("payment receipts validate property scope, calendar, money and methods",()=>{
 assert.deepEqual(readPayment(record,property),record);
 assert.equal(readPayment({...record,method:"BANK_TRANSFER",reference:"BF-001"},property).reference,"BF-001");
 for(const invalid of [null,[],{...record,propertyId:unit},{...record,buildingId:"bad"},{...record,amount:12.35},{...record,amount:"12.351"},{...record,amount:"0.00"},{...record,paidDate:"2026-02-30"},{...record,paidDate:"1999-01-01"},{...record,currency:"EUR"},{...record,method:["CASH"]},{...record,method:"CARD"},{...record,method:"BANK_TRANSFER",reference:" "},{...record,status:["RECORDED"]},{...record,version:0},{...record,replayed:"true"}])assert.throws(()=>readPayment(invalid,property));
});
test("allocation receipts reject duplicated debts and mismatching financial totals",()=>{
 for(const allocations of [[],null,[{...record.allocations[0],amount:"12.34"}],[{...record.allocations[0],period:"2026-13"}],[{...record.allocations[0],chargeId:"bad"}],[record.allocations[0],{...record.allocations[0],id:unit}],[record.allocations[0],{...record.allocations[0],chargeId:unit}]])assert.throws(()=>readPayment({...record,allocations},property));
 const split={...record,allocations:[{...record.allocations[0],amount:"12.34"},{id:unit,chargeId:unit,period:"2026-09",amount:"0.01"}]};assert.equal(readPayment(split,property).allocations.length,2);
});
test("cancelled payment receipts preserve immutable allocation history and reason",()=>{
 const cancelled={...record,status:"CANCELLED",version:2,cancellationReason:"Wrong receipt",cancelledAt:"2026-10-09T02:00:00Z",replayed:true};assert.deepEqual(readPayment(cancelled,property).allocations,record.allocations);
 for(const invalid of [{...cancelled,cancellationReason:null},{...cancelled,cancelledAt:"invalid"},{...record,cancellationReason:"Wrong"},{...record,cancelledAt:"2026-10-09T02:00:00Z"}])assert.throws(()=>readPayment(invalid,property));
});
test("payment list summaries cover all filtered pages and reject invalid counts or scope",()=>{
 const page={items:[record],page:2,pageSize:1,totalCount:3,summary:{recordedAmount:"100.00",cancelledAmount:"12.35",recordedCount:2,cancelledCount:1,currency:"TRY"}};assert.equal(readPaymentPage(page,property).summary.recordedAmount,"100.00");
 for(const invalid of [{...page,pageSize:0},{...page,pageSize:101},{...page,totalCount:0},{...page,items:[{...record,propertyId:unit}]},{...page,pageSize:2,items:[record,record]},{...page,summary:{...page.summary,recordedAmount:100}},{...page,summary:{...page.summary,recordedCount:3}},{...page,summary:{...page.summary,cancelledCount:-1}}])assert.throws(()=>readPaymentPage(invalid,property));
});
test("debt status distinguishes partial, overdue, settled and reversed balances exactly",()=>{
 const debt={id:chargeId,propertyId:property,assessmentId:property,unitId:unit,buildingId:null,unitNumber:"1",buildingName:null,period:"2026-10",amount:"12.35",balance:"12.35",currency:"TRY",dueDate:"2026-10-31",description:"Dues",status:"OPEN",version:1,createdAt:record.createdAt,cancellationReason:null,cancelledAt:null,partiesRecorded:false,partiesVisible:true,debtorType:"UNIT",parties:[],cancellationReasonCode:null,cancellationReasonName:null};
 for(const [status,balance] of [["OPEN","12.35"],["PARTIALLY_PAID","12.34"],["OVERDUE","0.01"],["PAID","0.00"],["CANCELLED","0.00"]])assert.equal(readCharge({...debt,status,balance},property).balance,balance);
 for(const [status,balance] of [["OPEN","12.34"],["PARTIALLY_PAID","12.35"],["PARTIALLY_PAID","0.00"],["PAID","0.01"],["OVERDUE","0.00"],["CANCELLED","0.01"],["OVERDUE","12.36"]])assert.throws(()=>readCharge({...debt,status,balance},property));
 assert.throws(()=>readCharge({...debt,amount:"100000000.00",balance:"100000000.00"},property));
});
test("payments menu survives OIDC while external or extended return paths are refused",()=>{
 for(const path of ["/payments",`/properties/${property}/payments`])assert.equal(isWorkspaceReturnPath(path),true);
 for(const path of ["/payments/extra",`/properties/${property}/payments?token=secret`,`/properties/${property}/payments#code=secret`,`/sites/${property}/payments`,`/properties/${property}/payments/extra`,"/properties/invalid/payments","//example.test/payments"])assert.equal(isWorkspaceReturnPath(path),false);
 assert.equal(propertyIdFromPath(`/properties/${property}/payments`),property);assert.equal(managementSection(`/properties/${property}/payments`,property),"payments");
});
test("payment problems localize field errors without exposing unknown server details",()=>{
 const title="Bu istek kimliği farklı bir ödeme için kullanılmış.";
 assert.equal(problemDetails(409,{title},"en").message,"This request ID was used for a different payment.");
 const result=problemDetails(400,{errors:{PaidDate:["Ödeme tarihi gelecekte olamaz."],Reference:["Havalede referans zorunludur; en fazla 100 karakter girin."],Allocations:["internal credentials"],Method:["internal credentials"]}},"en");
 assert.match(result.errors.paidDate[0],/future/);assert.match(result.errors.reference[0],/100/);assert.deepEqual(result.errors.allocations,["Enter a valid value for this field."]);assert.deepEqual(result.errors.method,["Enter a valid value for this field."]);
});
