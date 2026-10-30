import test from 'node:test';
import assert from 'node:assert/strict';
import {quotePreview,quoteComparison,readInquiry,readInquiryMutation,readInquirySummary,readQuoteCandidate,readInquiryHistory} from '../src/features/suppliers/quotes-model.ts';
const tenant='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',supplier='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',id='cccccccc-cccc-4ccc-8ccc-cccccccccccc',other='dddddddd-dddd-4ddd-8ddd-dddddddddddd';
const summary={id,tenantId:tenant,propertyId:id,propertyName:'Area',categoryId:id,title:'Annual',deadline:'2027-01-01',status:'OPEN',version:2};
const pending={invitationId:id,supplierId:supplier,supplierName:'A',areaServiceId:id,serviceName:'Maintenance',quoteId:other,netAmount:'100.00',vatRate:'20.00',vatAmount:'20.00',grossAmount:'120.00',currency:'TRY',validUntil:'2027-02-01',notes:'Includes parts',status:'PENDING',decisionReason:null,isOwnEntry:true};
const detail={inquiry:summary,description:'Service scope',cancellationReason:null,quotes:[pending],workOrder:null};
test('quote VAT preview uses exact cents, half-up rounding, zero rates and bounded totals',()=>{
 assert.deepEqual(quotePreview('0.05','10.00'),{vat:'0.01',gross:'0.06'});assert.deepEqual(quotePreview('100.00','0.00'),{vat:'0.00',gross:'100.00'});
 for(const [n,r] of [['0.00','20.00'],['-1.00','20.00'],['1.001','20.00'],['100.00','100.01'],['99999999.99','20.00'],['01.00','20.00']])assert.equal(quotePreview(n,r),null);
});
test('comparison sorts gross amounts only within each currency and preserves ties without conversion',()=>{
 const rows=[pending,{...pending,supplierName:'B',currency:'EUR',grossAmount:'900.00'},{...pending,supplierName:'C',grossAmount:'9.00'}];const groups=quoteComparison(rows);
 assert.deepEqual(groups.map(g=>g.currency),['TRY','EUR']);assert.deepEqual(groups[0].rows.map(r=>r.supplierName),['C','A']);assert.equal(groups[1].rows[0].grossAmount,'900.00');
});
test('inquiry DTO rejects foreign tenant, uninvited supplier, corrupted money and final state mismatches',()=>{
 assert.deepEqual(readInquiry(detail,tenant,supplier),detail);
 assert.throws(()=>readInquiry(detail,other,supplier));assert.throws(()=>readInquiry(detail,tenant,other));
 for(const patch of [{grossAmount:'119.99'},{currency:'GBP'},{netAmount:100},{vatRate:'100.01'},{quoteId:null},{status:'APPROVED'},{status:'REJECTED',decisionReason:null},{validUntil:'2027-02-30'},{isOwnEntry:'true'}])assert.throws(()=>readInquiry({...detail,quotes:[{...pending,...patch}]},tenant,supplier));
 assert.throws(()=>readInquiry({...detail,quotes:[pending,pending]},tenant,supplier));
 assert.throws(()=>readInquirySummary({...summary,deadline:'1999-12-31'},tenant));
});
test('awaiting quote cannot smuggle amounts and an awarded order must match approved snapshot',()=>{
 const awaiting={...pending,quoteId:null,netAmount:null,vatRate:null,vatAmount:null,grossAmount:null,currency:null,validUntil:null,notes:null,status:'AWAITING',isOwnEntry:false};assert.equal(readInquiry({...detail,quotes:[awaiting]},tenant,supplier).quotes[0].status,'AWAITING');
 assert.throws(()=>readInquiry({...detail,quotes:[{...awaiting,netAmount:'1.00'}]},tenant,supplier));
 const approved={...pending,status:'APPROVED',decisionReason:'Complete scope'},order={id,quoteId:other,status:'DRAFT',title:'Annual',amount:'120.00',currency:'TRY',version:1};
 const awarded={...detail,inquiry:{...summary,status:'AWARDED'},quotes:[approved],workOrder:order};assert.equal(readInquiry(awarded,tenant,supplier).workOrder.id,id);
 for(const patch of [{currency:'EUR'},{amount:'1.00'},{quoteId:id},{status:'PAID'}])assert.throws(()=>readInquiry({...awarded,workOrder:{...order,...patch}},tenant,supplier));
});
test('mutation replay keeps current version while rejecting future or inconsistent applied versions',()=>{
 assert.equal(readInquiryMutation({record:detail,replayed:true,appliedVersion:1},tenant,supplier).record.inquiry.version,2);
 assert.throws(()=>readInquiryMutation({record:detail,replayed:false,appliedVersion:1},tenant,supplier));assert.throws(()=>readInquiryMutation({record:detail,replayed:true,appliedVersion:3},tenant,supplier));
});
test('candidate and history DTOs reject unsafe IDs, action values and oversized snapshots',()=>{
 assert.equal(readQuoteCandidate({areaServiceId:id,supplierId:supplier,supplierName:'A',serviceName:'Service'}).supplierId,supplier);
 assert.throws(()=>readQuoteCandidate({areaServiceId:'bad',supplierId:supplier,supplierName:'A',serviceName:'Service'}));
 const history={id,action:'APPROVE',appliedVersion:4,snapshot:'{}',createdAt:'2026-10-29T12:00:00Z'};assert.deepEqual(readInquiryHistory(history),history);
 for(const patch of [{action:'PAY'},{appliedVersion:0},{snapshot:'x'.repeat(20001)}])assert.throws(()=>readInquiryHistory({...history,...patch}));
});
