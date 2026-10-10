import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readCharge,readDuesPersonChoices,readCancellationReasons,readMoveOutPreview,readTenantDebtResult,moveOutApprovals} from '../src/features/dues/model.ts';
import {problemDetails} from '../src/services/problem.ts';
const id='9b962f3e-8812-459f-a42f-45ec6e3c6eba',other='24d39d57-021b-4705-87d3-98a091c95f23';
const party={chargeId:id,personId:other,occupancyId:id,personName:'Ada Kaya',type:'TENANT',startDate:'2026-09-01',endDate:null,isDebtor:true};
const charge={id,propertyId:id,assessmentId:id,unitId:other,buildingId:null,unitNumber:'1',buildingName:null,period:'2026-11',amount:'12.35',balance:'12.35',currency:'TRY',dueDate:'2026-11-30',description:'Dues',status:'OPEN',version:1,createdAt:'2026-10-09T00:00:00Z',cancellationReason:null,cancelledAt:null,partiesRecorded:true,partiesVisible:true,debtorType:'TENANT',parties:[party],cancellationReasonCode:null,cancellationReasonName:null};
const preview={occupancyId:id,version:1,endDate:'2026-10-15',eligibleCount:1,totalAmount:'12.35',skippedPaymentCount:0,approvalAvailable:true,items:[{id:other,version:2,period:'2026-11',amount:'12.35',dueDate:'2026-11-30'}]};
test('historical parties distinguish tenant, owner, legacy and role-restricted responses',()=>{
 assert.equal(readCharge(charge,id).parties[0].personName,'Ada Kaya');
 const hidden={...charge,partiesVisible:false,parties:[]};assert.deepEqual(readCharge(hidden,id),hidden);
 const legacy={...charge,partiesRecorded:false,debtorType:'UNIT',parties:[]};assert.deepEqual(readCharge(legacy,id),legacy);
 for(const invalid of [{...hidden,parties:[party]},{...legacy,parties:[party]},{...charge,parties:[]},{...charge,parties:[party,party]},{...charge,debtorType:'OWNER'},{...charge,parties:[{...party,type:'RESIDENT'}]},{...charge,parties:[{...party,chargeId:other}]},{...charge,parties:[{...party,startDate:'2026-02-30'}]},{...charge,parties:[{...party,startDate:'2026-11-02'}]},{...charge,parties:[{...party,endDate:'2026-10-31'}]}])assert.throws(()=>readCharge(invalid,id));
});
test('person options require debt-property scope, unique identities and bounded pages',()=>{
 const choices={page:1,pageSize:20,totalCount:1,items:[{id:other,propertyId:id,name:'Original Name'}]};assert.deepEqual(readDuesPersonChoices(choices,id),choices);
 for(const bad of [{...choices,items:[{...choices.items[0],propertyId:other}]},{...choices,items:[choices.items[0],choices.items[0]]},{...choices,pageSize:101},{...choices,items:[{...choices.items[0],name:''}]}])assert.throws(()=>readDuesPersonChoices(bad,id));
});
test('cancellation catalog rejects malformed or duplicate reasons',()=>{
 const reasons=[{id,code:'OTHER',name:'Diğer',requiresExplanation:true}];assert.deepEqual(readCancellationReasons(reasons),reasons);
 for(const bad of [[...reasons,...reasons],[{...reasons[0],requiresExplanation:'true'}],[{...reasons[0],code:'SQL secret'}],[{...reasons[0],id:'bad'}]])assert.throws(()=>readCancellationReasons(bad));
 assert.throws(()=>readCharge({...charge,cancellationReasonCode:'OTHER'},id));
});
test('move-out preview validates relationship date/version, unique debts and exact decimal total',()=>{
 assert.deepEqual(readMoveOutPreview(preview,id,1,'2026-10-15'),preview);
 for(const bad of [{...preview,occupancyId:other},{...preview,version:2},{...preview,endDate:'2026-10-16'},{...preview,totalAmount:'12.34'},{...preview,eligibleCount:2},{...preview,items:[{...preview.items[0],period:'2026-10'}]},{...preview,items:[{...preview.items[0],dueDate:'2026-11-31'}]},{...preview,items:[{...preview.items[0],version:0}]}])assert.throws(()=>readMoveOutPreview(bad,id,1,'2026-10-15'));
});
test('without consent no debts are submitted; date/version changes invalidate approval',()=>{
 assert.deepEqual(moveOutApprovals(preview,id,1,'2026-10-15',false),[]);
 assert.deepEqual(moveOutApprovals(preview,id,1,'2026-10-15',true),[{chargeId:other,version:2}]);
 for(const [p,occupancy,version,date] of [[null,id,1,'2026-10-15'],[preview,other,1,'2026-10-15'],[preview,id,2,'2026-10-15'],[preview,id,1,'2026-10-16']])assert.throws(()=>moveOutApprovals(p,occupancy,version,date,true));
 // Only listed IDs/versions are submitted, never an approve-all server flag.
 assert.equal(Object.keys(moveOutApprovals(preview,id,1,'2026-10-15',true)[0]).sort().join(','),'chargeId,version');
});
test('over-500 previews cannot be bulk-approved and post-save results require valid counts',()=>{
 const large={...preview,eligibleCount:501,approvalAvailable:false,items:[]};assert.deepEqual(readMoveOutPreview(large,id,1,'2026-10-15'),large);assert.throws(()=>moveOutApprovals(large,id,1,'2026-10-15',true));
 assert.throws(()=>readMoveOutPreview({...preview,approvalAvailable:false},id,1,'2026-10-15'));
 const result={cancelledCount:0,pendingCount:1,skippedPaymentCount:2};assert.deepEqual(readTenantDebtResult({tenantDebtResult:result}),result);
 for(const bad of [{tenantDebtResult:null},{tenantDebtResult:{...result,cancelledCount:-1}},{tenantDebtResult:{...result,pendingCount:'1'}}])assert.throws(()=>readTenantDebtResult(bad));
});
test('reason, debtor and approval validation uses safe localized feedback',()=>{
 const output=problemDetails(400,{errors:{ReasonId:['İptal nedeni seçin.'],Reason:['Diğer iptal nedeni için açıklama zorunludur.'],DebtorType:['Daire, kiracı veya malik muhatabı seçin.'],CancellationApprovals:['internal secret']}},'en');
 assert.deepEqual(output.errors,{reasonId:['Select a cancellation reason'],reason:['An explanation is required for Other.'],debtorType:['Choose unit, tenant or owner as debtor.'],cancellationApprovals:['Enter a valid value for this field.']});
 assert.match(problemDetails(409,{title:'Onaylanan borç değişti veya iptale uygun değil. Önizlemeyi yenileyin.'},'en').message,/Refresh the preview/);
});
