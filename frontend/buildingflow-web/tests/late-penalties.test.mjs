import {test} from "node:test";
import assert from "node:assert/strict";
import {readFileSync} from "node:fs";
import {normalizeRate,readChargeDetail,readPenaltyPage,readPolicy,readPreview,readSettings,toCsv} from "../src/features/late-penalties/model.ts";
import {readPayment} from "../src/features/payments/model.ts";
import {errorCodes} from "../src/services/error-codes.ts";
const property="9b962f3e-8812-459f-a42f-45ec6e3c6eba",unit="24d39d57-021b-4705-87d3-98a091c95f23",charge="c89825a1-beb6-4850-a74d-0b59b5a77af3",accrual="c89825a1-beb6-4850-a74d-0b59b5a77af4";
const policy={id:accrual,year:2027,validFrom:"2027-01-01",validTo:null,monthlyRate:"5.00",method:"DAILY_30",legalBasis:"KMK_20",legalReference:"KMK m.20",approvalDocumentId:null,status:"APPROVED",approvedBy:"admin",approvedAt:"2026-10-23T10:00:00Z",statusReason:null,createdBy:"manager",createdAt:"2026-10-23T09:00:00Z",version:2,replayed:false};
const segment={from:"2026-02-01",to:"2026-03-02",days:30,principal:"3000.00",monthlyRate:"5.00",method:"DAILY_30",policyId:null,amount:"150.000000"};

test("policy reader keeps the statutory ceiling, year and approval audit consistent",()=>{
 assert.equal(readPolicy(policy).monthlyRate,"5.00");
 for(const bad of [{monthlyRate:"5.01"},{monthlyRate:"0.00"},{year:2026},{validTo:"2026-12-31"},{status:"APPROVED",approvedBy:null},{status:"DRAFT"},{method:"MONTHLY"},{legalBasis:"OTHER"}])
  assert.throws(()=>readPolicy({...policy,...bad}));
 assert.equal(readPolicy({...policy,status:"DRAFT",approvedBy:null,approvedAt:null}).status,"DRAFT");
});
test("settings must belong to the selected property and default to not applied",()=>{
 const settings={propertyId:property,appliedToday:false,statutoryRate:"5.00",statutoryReference:"634 sayılı KMK m.20",defaultMethod:"DAILY_30",crystallizedThrough:null,canDraft:true,canApprove:false,switches:[],policies:[policy]};
 assert.equal(readSettings(settings,property).appliedToday,false);
 assert.throws(()=>readSettings(settings,unit));
 assert.throws(()=>readSettings({...settings,switches:[{id:unit,effectiveFrom:"2026-10-23",apply:true,reason:" ",createdBy:"a",createdAt:"2026-10-23T10:00:00Z",replayed:false}]},property));
});
test("charge detail totals must add up from principal, crystallized and pending penalty",()=>{
 const detail={chargeId:charge,unitId:unit,unitNumber:"1",period:"2026-01",dueDate:"2026-01-31",principal:"3000.00",principalPaid:"0.00",principalBalance:"3000.00",penaltyAccrued:"150.00",penaltyPaid:"0.00",penaltyWaived:"0.00",penaltyBalance:"150.00",pendingPenalty:"50.00",totalDue:"3200.00",asOf:"2026-03-12",appliedToday:true,
  accruals:[{id:accrual,from:"2026-02-01",to:"2026-03-02",amount:"150.00",paid:"0.00",balance:"150.00",status:"ACTIVE",statusReason:null,statusChangedAt:null,version:1,segments:[segment]}],pendingSegments:[]};
 assert.equal(readChargeDetail(detail,charge).totalDue,"3200.00");
 assert.throws(()=>readChargeDetail({...detail,totalDue:"3150.00"},charge));
 assert.throws(()=>readChargeDetail(detail,unit));
 assert.throws(()=>readChargeDetail({...detail,accruals:[{...detail.accruals[0],status:"WAIVED"}]},charge));
});
test("overdue page and preview are strictly shaped",()=>{
 const row={chargeId:charge,unitId:unit,unitNumber:"1",buildingName:null,period:"2026-01",dueDate:"2026-01-31",daysOverdue:40,principalBalance:"3000.00",penaltyBalance:"0.00",pendingPenalty:"200.00",totalDue:"3200.00"};
 const page=readPenaltyPage({items:[row],page:1,pageSize:20,totalCount:1,totals:{principalOverdue:"3000.00",penaltyOutstanding:"0.00",penaltyCollected:"0.00",penaltyWaived:"0.00",currency:"TRY"}});
 assert.equal(page.items[0].id,charge);
 assert.throws(()=>readPenaltyPage({items:[row,row],page:1,pageSize:20,totalCount:2,totals:page.totals}));
 const preview={unitId:unit,paidDate:"2026-03-12",applied:true,principalTotal:"3000.00",penaltyTotal:"200.00",unallocated:"0.00",lines:[{chargeId:charge,period:"2026-01",dueDate:"2026-01-31",version:1,principalBalance:"3000.00",penaltyDue:"200.00",suggestedPenalty:"200.00",suggestedPrincipal:"800.00"}]};
 assert.equal(readPreview(preview,unit,"2026-03-12").lines.length,1);
 assert.throws(()=>readPreview(preview,unit,"2026-03-11"));
});
test("payments accept penalty lines per accrual next to the principal line of the same debt",()=>{
 const record={id:property,propertyId:property,unitId:unit,buildingId:null,unitNumber:"1",buildingName:null,requestId:unit,amount:"3200.00",currency:"TRY",paidDate:"2026-03-12",method:"CASH",reference:null,description:"Tahsilat",status:"RECORDED",version:1,createdAt:"2026-03-12T10:00:00Z",cancellationReason:null,cancelledAt:null,replayed:false,
  allocations:[{id:unit,chargeId:charge,period:"2026-01",amount:"3000.00",kind:"PRINCIPAL",accrualId:null},{id:accrual,chargeId:charge,period:"2026-01",amount:"150.00",kind:"PENALTY",accrualId:accrual},{id:charge,chargeId:charge,period:"2026-01",amount:"50.00",kind:"PENALTY",accrualId:property}]};
 assert.equal(readPayment(record,property).allocations.length,3);
 assert.throws(()=>readPayment({...record,allocations:[record.allocations[0],{...record.allocations[1],accrualId:null}]},property));
 assert.throws(()=>readPayment({...record,amount:"3150.00",allocations:[record.allocations[0],record.allocations[1],{...record.allocations[2],accrualId:accrual,amount:"0.01"}]},property));
});
test("rates and CSV export are safe without new dependencies",()=>{
 assert.equal(normalizeRate("4,5"),"4.5");assert.equal(normalizeRate("5"),"5");
 for(const bad of ["0","5.01","6","-1","1.00001","abc"])assert.equal(normalizeRate(bad),null);
 const csv=toCsv([["=SUM(A1)","a;b",'x"y',12]]);
 assert.ok(csv.startsWith("﻿'=SUM(A1);\"a;b\";\"x\"\"y\";12"));
});
test("every late penalty error code has TR and EN text and the UI keys exist in both locales",()=>{
 const tr=JSON.parse(readFileSync(new URL("../messages/tr.json",import.meta.url)));const en=JSON.parse(readFileSync(new URL("../messages/en.json",import.meta.url)));
 const codes=Object.keys(errorCodes).filter(code=>code.startsWith("BF_LATE_PENALTY_"));
 assert.equal(codes.length,13);
 for(const code of codes){assert.ok(tr.apiErrors[code]);assert.ok(en.apiErrors[code]);}
 assert.deepEqual(Object.keys(tr.latePenalties).sort(),Object.keys(en.latePenalties).sort());
});
