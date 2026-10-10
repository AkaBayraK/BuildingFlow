import {test} from "node:test";
import assert from "node:assert/strict";
import {normalizeAmount,previewTotal,submissionKey,readCharge,readDuesPage,readAssessment,readUnitChoices} from "../src/features/dues/model.ts";
import {isWorkspaceReturnPath} from "../src/auth/return-path.ts";
import {managementSection,propertyIdFromPath} from "../src/features/navigation/routes.ts";
import {problemDetails} from "../src/services/problem.ts";
import {translateUiText} from "../src/i18n/translate-text.ts";
const id="9b962f3e-8812-459f-a42f-45ec6e3c6eba",other="24d39d57-021b-4705-87d3-98a091c95f23";
const charge={id,propertyId:id,assessmentId:id,unitId:other,buildingId:null,unitNumber:"1",buildingName:null,period:"2026-10",amount:"12.35",balance:"12.35",currency:"TRY",dueDate:"2026-10-31",description:"Dues",status:"OPEN",version:1,createdAt:"2026-10-08T00:00:00Z",cancellationReason:null,cancelledAt:null,partiesRecorded:false,partiesVisible:true,debtorType:"UNIT",parties:[],cancellationReasonCode:null,cancellationReasonName:null};
const summary={outstandingAmount:"37.05",overdueAmount:"0.00",cancelledAmount:"0.00",openCount:3,overdueCount:0,cancelledCount:0,currency:"TRY"};
test("money input accepts decimal comma or dot without rounding or grouping",()=>{
 for(const [input,expected] of [["12,35","12.35"],[" 00012.3 ","12.30"],["0.01","0.01"],["99999999,99","99999999.99"]])assert.equal(normalizeAmount(input),expected);
 for(const input of ["","0","-1","1.001","1,234.50","1.234,50","1e2","NaN","Infinity","100000000",".50","12."])assert.equal(normalizeAmount(input),null,input);
});
test("assessment preview preserves cents for fractional and maximum bulk amounts",()=>{
 assert.equal(previewTotal("0.10",3),"0.30");assert.equal(previewTotal("12.35",3),"37.05");assert.equal(previewTotal("99999999.99",500),"49999999995.00");
 for(const count of [0,501,1.5,NaN])assert.throws(()=>previewTotal("12.35",count));assert.throws(()=>previewTotal("12.351",2));
});
test("uncertain submission reuses the request ID only for unchanged normalized contents",()=>{
 let generated=0;const createId=()=>`request-${++generated}`;
 const first=submissionKey(null,"property-unit-period-amount",createId);
 assert.strictEqual(submissionKey(first,first.signature,createId),first);assert.equal(generated,1);
 const changed=submissionKey(first,"changed-period",createId);assert.notEqual(changed.id,first.id);assert.equal(generated,2);
});
test("debt responses reject malformed money, status, version and cross-property rows",()=>{
 assert.deepEqual(readCharge(charge,id),charge);
 for(const invalid of [null,[],{...charge,propertyId:other},{...charge,amount:12.35},{...charge,balance:"12.351"},{...charge,status:["OPEN"]},{...charge,status:"PAID"},{...charge,version:0},{...charge,buildingId:"bad"}])assert.throws(()=>readCharge(invalid,id));
 const page={items:[charge],page:1,pageSize:1,totalCount:3,summary};assert.deepEqual(readDuesPage(page,id).summary,summary);
 for(const invalid of [{...page,summary:null},{...page,summary:{...summary,outstandingAmount:37.05}},{...page,summary:{...summary,openCount:-1}},{...page,items:[{...charge,propertyId:other}]},{...page,pageSize:0}])assert.throws(()=>readDuesPage(invalid,id));
});
test("assessment acknowledgement and active unit choices validate external data",()=>{
 const value={id,propertyId:id,requestId:other,period:"2026-10",amount:"12.35",currency:"TRY",dueDate:"2026-10-31",description:"Dues",unitCount:1,totalAmount:"12.35",unitIds:[other],replayed:false};
 assert.equal(readAssessment(value,id).totalAmount,"12.35");assert.throws(()=>readAssessment({...value,unitCount:2},id));assert.throws(()=>readAssessment({...value,propertyId:other},id));
 const choices={items:[{id:other,buildingId:null,number:"1",buildingName:null,isEnabled:true}],page:1,pageSize:10,totalCount:1};assert.deepEqual(readUnitChoices(choices),choices);
 assert.throws(()=>readUnitChoices({...choices,items:[{...choices.items[0],isEnabled:"true"}]}));assert.throws(()=>readUnitChoices({...choices,items:null}));
});
test("dues menus and exact OIDC return paths preserve property scope",()=>{
 for(const path of ["/dues",`/properties/${id}/dues`])assert.equal(isWorkspaceReturnPath(path),true);
 for(const path of ["/dues/extra",`/properties/${id}/dues?token=secret`,`/properties/${id}/dues#code=secret`,`/sites/${id}/dues`,`/properties/${id}/dues/extra`])assert.equal(isWorkspaceReturnPath(path),false);
 assert.equal(propertyIdFromPath(`/properties/${id}/dues`),id);assert.equal(managementSection(`/properties/${id}/dues`,id),"dues");
});
test("financial errors retain field names, localize safely and follow language changes",()=>{
 const reason="İptal gerekçesi zorunludur ve en fazla 500 karakter olabilir.";
 assert.deepEqual(problemDetails(400,{errors:{Reason:[reason],UnitIds:["1–500 farklı bağımsız bölüm seçin."]}},"en").errors,{reason:["Cancellation reason is required and must be at most 500 characters."],unitIds:["Select 1–500 distinct units."]});
 assert.equal(problemDetails(409,{title:"Bu istek kimliği farklı bir tahakkuk için kullanılmış."},"en").message,"This request ID was used for a different assessment.");
 assert.equal(translateUiText(reason,"en"),"Cancellation reason is required and must be at most 500 characters.");
 assert.deepEqual(problemDetails(400,{errors:{Amount:["internal secret"],RequestId:["internal secret"]}},"en").errors,{amount:["Enter a valid value for this field."],requestId:["Enter a valid value for this field."]});
});
