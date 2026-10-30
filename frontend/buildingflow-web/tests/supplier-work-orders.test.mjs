import test from 'node:test';import assert from 'node:assert/strict';
import {readWorkOrder,readWorkOrderSource,readWorkOrderPhoto,readWorkOrderMutation,readWorkOrderHistory,workOrderInstant,workOrderLocalTime} from '../src/features/suppliers/work-orders-model.ts';
import {workOrderNext} from '../src/features/suppliers/work-order-status.ts';
import {readInquiry} from '../src/features/suppliers/quotes-model.ts';
const tenant='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',supplier='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',id='cccccccc-cccc-4ccc-8ccc-cccccccccccc',property='dddddddd-dddd-4ddd-8ddd-dddddddddddd',stamp='2026-10-30T09:00:00Z';
const base={id,tenantId:tenant,supplierId:supplier,propertyId:property,propertyName:'Area',areaServiceId:id,serviceName:'Repair',title:'Repair elevator',description:'Work scope\nKeep evidence',source:'REQUEST',inquiryId:null,quoteId:null,requestId:property,maintenancePlanId:null,sourceVersion:1,sourceSnapshot:'Original source\nDescription',amount:null,currency:null,status:'DRAFT',dueAt:'2027-01-03T09:00:00Z',isOverdue:false,assignedAt:null,acceptedAt:null,startedAt:null,completedAt:null,verifiedAt:null,closedAt:null,completionNote:null,decisionReason:null,photoCount:0,version:1,createdAt:stamp,modifiedAt:stamp};
const parent={id:supplier,tenantId:tenant,name:'Supplier',kind:'COMPANY',description:null,status:'ACTIVE',statusReason:null,isEnabled:true,version:2,createdAt:stamp,modifiedAt:stamp};
test('work order DTO rejects foreign scope, implicit prices, missing source versions and unsafe timestamps',()=>{
 assert.deepEqual(readWorkOrder(base,tenant,supplier,id),base);
 assert.throws(()=>readWorkOrder(base,tenant,supplier,property));
 for(const patch of [{tenantId:property},{supplierId:property},{sourceVersion:null},{requestId:null},{maintenancePlanId:id},{amount:'0.00'},{currency:'TRY'},{description:'bad\u0000'},{status:'PAID'},{photoCount:11},{dueAt:'2027-02-30T12:00:00Z'},{closedAt:stamp}])assert.throws(()=>readWorkOrder({...base,...patch},tenant,supplier));
 const quote={...base,source:'QUOTE',inquiryId:id,quoteId:property,requestId:null,sourceVersion:null,amount:'120.00',currency:'TRY'};assert.equal(readWorkOrder(quote,tenant,supplier).amount,'120.00');assert.throws(()=>readWorkOrder({...quote,amount:null},tenant,supplier));
});
test('workflow DTO validates every timestamp and completion evidence without rewriting terminal states',()=>{
 const complete={...base,status:'COMPLETED',assignedAt:'2027-01-01T09:00:00Z',acceptedAt:'2027-01-01T10:00:00Z',startedAt:'2027-01-01T11:00:00Z',completedAt:'2027-01-02T09:00:00Z',completionNote:'Repaired'};
 assert.equal(readWorkOrder(complete,tenant,supplier).completionNote,'Repaired');
 for(const patch of [{acceptedAt:null},{startedAt:'2026-01-01T00:00:00Z'},{completionNote:null},{verifiedAt:stamp},{decisionReason:'Unexpected cancellation'},{isOverdue:true}])assert.throws(()=>readWorkOrder({...complete,...patch},tenant,supplier));
 assert.deepEqual(workOrderNext('COMPLETED'),['VERIFIED','CANCELLED']);assert.deepEqual(workOrderNext('CLOSED'),[]);assert.deepEqual(workOrderNext('REJECTED'),[]);
});
test('source choices remain bound to tenant, area, type and current version',()=>{
 const source={id,tenantId:tenant,propertyId:property,source:'MAINTENANCE',description:'Annual inspection',version:3};assert.deepEqual(readWorkOrderSource(source,tenant,property,'MAINTENANCE'),source);
 for(const patch of [{tenantId:property},{propertyId:tenant},{source:'REQUEST'},{version:0},{description:' '}])assert.throws(()=>readWorkOrderSource({...source,...patch},tenant,property,'MAINTENANCE'));
});
test('private work photos reject foreign areas and PDF evidence',()=>{
 const file={id,tenantId:tenant,supplierId:supplier,propertyId:property,propertyName:'Area',documentId:property,fileName:'evidence.jpg',contentType:'image/jpeg',size:30,sha256:'A'.repeat(64),expiresOn:null,previousDocumentId:null,revisionNumber:1,isCurrent:true,version:1,createdAt:stamp};
 const photo={id,file,caption:'Before repair',createdAt:stamp};assert.equal(readWorkOrderPhoto(photo,tenant,supplier,property).file.id,id);
 assert.throws(()=>readWorkOrderPhoto(photo,tenant,supplier,id));assert.throws(()=>readWorkOrderPhoto({...photo,file:{...file,fileName:'evidence.pdf',contentType:'application/pdf'}},tenant,supplier,property));
});
test('current work order replay accepts older applied version but rejects wrong record and future version',()=>{
 assert.equal(readWorkOrderMutation({record:{...base,version:4},supplier:parent,replayed:true,appliedVersion:1},tenant,supplier,id).record.version,4);
 for(const patch of [{replayed:false,appliedVersion:2},{replayed:true,appliedVersion:9}])assert.throws(()=>readWorkOrderMutation({record:base,supplier:parent,...patch},tenant,supplier,id));
 assert.throws(()=>readWorkOrderMutation({record:base,supplier:{...parent,id:property},replayed:false,appliedVersion:1},tenant,supplier,id));
});
test('Istanbul SLA input roundtrips without relying on the computer timezone',()=>{
 assert.equal(workOrderInstant('2027-01-01T12:30'),'2027-01-01T09:30:00.000Z');assert.equal(workOrderLocalTime('2027-01-01T09:30:00Z'),'2027-01-01T12:30');
 for(const value of ['2027-02-30T12:30','2027-01-01T24:00','2027-01-01T12:60','bad','1999-01-01T12:30'])assert.equal(workOrderInstant(value),null);
});
test('work history retains decision reasons but refuses unknown actions and corrupt snapshots',()=>{
 const history={id,action:'VERIFIED',appliedVersion:5,snapshot:'{"Reason":"Inspected by manager"}',createdAt:stamp};assert.equal(readWorkOrderHistory(history).reason,'Inspected by manager');
 for(const patch of [{action:'DRAFT'},{action:'PAY'},{snapshot:'bad JSON'},{appliedVersion:0}])assert.throws(()=>readWorkOrderHistory({...history,...patch}));
});
test('quote comparison keeps its approved snapshot when the linked work order is closed',()=>{
 const q={invitationId:id,supplierId:supplier,supplierName:'Supplier',areaServiceId:property,serviceName:'Repair',quoteId:property,netAmount:'100.00',vatRate:'20.00',vatAmount:'20.00',grossAmount:'120.00',currency:'TRY',validUntil:'2027-02-01',notes:'Scope',status:'APPROVED',decisionReason:'Approved',isOwnEntry:false};
 const detail={inquiry:{id,tenantId:tenant,propertyId:property,propertyName:'Area',categoryId:id,title:'Repair',deadline:'2027-01-01',status:'AWARDED',version:5},description:'Repair scope',cancellationReason:null,quotes:[q],workOrder:{id,quoteId:property,status:'CLOSED',title:'Repair',amount:'120.00',currency:'TRY',version:8}};
 assert.equal(readInquiry(detail,tenant,supplier).workOrder.status,'CLOSED');assert.throws(()=>readInquiry({...detail,workOrder:{...detail.workOrder,currency:'EUR'}},tenant,supplier));
});
