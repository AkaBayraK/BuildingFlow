import test from 'node:test';import assert from 'node:assert/strict';
import {readInvoice,readInvoicePage,readInvoiceMutation,readInvoiceHistory,vatPreview,invoiceNumberKey,invalidInvoiceField,invoiceBody,invoiceForm} from '../src/features/suppliers/invoices-model.ts';
const tenant='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',supplier='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',id='cccccccc-cccc-4ccc-8ccc-cccccccccccc',property='dddddddd-dddd-4ddd-8ddd-dddddddddddd',stamp='2026-10-30T09:00:00Z';
const base={id,tenantId:tenant,supplierId:supplier,propertyId:property,propertyName:'Area',number:'INV-1',issueDate:'2027-01-05',dueDate:'2027-02-05',netAmount:'1000.00',vatRate:'20.00',vatAmount:'200.00',grossAmount:'1200.00',currency:'TRY',category:'MAINTENANCE',description:'Elevator repair',workOrderId:null,contractId:null,correctsInvoiceId:null,expenseId:null,status:'RECORDED',isOverdue:false,recordedByMe:true,paidOn:null,paymentReference:null,reconciledOn:null,reconciliationNote:null,cancellationReason:null,version:1,createdAt:stamp};
test('VAT preview uses exact cents with half-up rounding and rejects out-of-range totals',()=>{
 assert.deepEqual(vatPreview('0.05','10'),{vat:'0.01',gross:'0.06'});assert.deepEqual(vatPreview('1000','20.00'),{vat:'200.00',gross:'1200.00'});assert.deepEqual(vatPreview('100','0'),{vat:'0.00',gross:'100.00'});
 for(const [n,r] of [['0','20'],['100','100.01'],['99999999.99','20'],['1.001','20'],['100','1.005']])assert.equal(vatPreview(n,r),null);
});
test('invoice DTO rejects foreign scope, inconsistent money and lifecycle evidence',()=>{
 assert.deepEqual(readInvoice(base,tenant,supplier),base);
 for(const patch of [{tenantId:property},{supplierId:property},{grossAmount:'1200.01'},{vatAmount:'199.99',grossAmount:'1199.99'},{currency:'EUR'},{vatRate:'100.50'},{dueDate:'2027-01-01'},{status:'APPROVED'},{paidOn:'2027-01-10'},{cancellationReason:'x'},{status:'PAYABLE'},{category:'PAYROLL'},{expenseId:'bad'}])assert.throws(()=>readInvoice({...base,...patch},tenant,supplier));
 const paid={...base,status:'PAID',expenseId:id,paidOn:'2027-01-10',paymentReference:'EFT-1',recordedByMe:false};assert.equal(readInvoice(paid,tenant,supplier).status,'PAID');
 for(const patch of [{paidOn:'2027-01-01'},{paymentReference:null},{expenseId:null},{isOverdue:true},{reconciledOn:'2027-01-11'}])assert.throws(()=>readInvoice({...paid,...patch},tenant,supplier));
 assert.equal(readInvoice({...paid,status:'RECONCILED',reconciledOn:'2027-01-11',reconciliationNote:'Matched'},tenant,supplier).status,'RECONCILED');
 assert.throws(()=>readInvoice({...paid,status:'RECONCILED',reconciledOn:'2027-01-09',reconciliationNote:'Before payment'},tenant,supplier));
 assert.equal(readInvoice({...base,status:'CANCELLED',expenseId:id,cancellationReason:'Wrong VAT'},tenant,supplier).status,'CANCELLED');
});
test('page summary and mutation versions are validated',()=>{
 const page={items:[base],page:1,pageSize:10,totalCount:1,summary:{recordedTotal:'1200.00',approvedTotal:'0.00',paidTotal:'0.00',reconciledTotal:'0.00',overdueCount:0}};
 assert.equal(readInvoicePage(page,tenant,supplier).summary.recordedTotal,'1200.00');
 assert.throws(()=>readInvoicePage({...page,summary:{...page.summary,paidTotal:'1'}},tenant,supplier));assert.throws(()=>readInvoicePage({...page,items:[base,base],totalCount:2},tenant,supplier));
 assert.equal(readInvoiceMutation({record:{...base,version:3},replayed:true,appliedVersion:1},tenant,supplier,id).record.version,3);
 assert.throws(()=>readInvoiceMutation({record:base,replayed:false,appliedVersion:2},tenant,supplier));assert.throws(()=>readInvoiceMutation({record:base,replayed:false,appliedVersion:1},tenant,supplier,property));
 assert.equal(readInvoiceHistory({id,action:'PAY',appliedVersion:3,snapshot:'{}',createdAt:stamp}).action,'PAY');assert.throws(()=>readInvoiceHistory({id,action:'TRANSFER',appliedVersion:3,snapshot:'{}',createdAt:stamp}));
});
test('invoice form normalizes numbers, refuses future issue dates and sends exact strings',()=>{
 assert.equal(invoiceNumberKey(' inv-2027/001.a '),'INV2027001A');
 const form={...invoiceForm(),number:' INV 1 ',issueDate:'2027-01-05',dueDate:'2027-02-05',netAmount:'1000,5',vatRate:'18',description:' Repair '};
 assert.equal(invalidInvoiceField(form,'2027-01-10'),null);assert.equal(invalidInvoiceField(form,'2027-01-04'),'issueDate');
 assert.equal(invalidInvoiceField({...form,number:' - '},'2027-01-10'),'number');assert.equal(invalidInvoiceField({...form,dueDate:'2027-01-01'},'2027-01-10'),'dueDate');assert.equal(invalidInvoiceField({...form,vatRate:'101'},'2027-01-10'),'vatRate');assert.equal(invalidInvoiceField({...form,description:' '},'2027-01-10'),'description');
 assert.deepEqual(invoiceBody(form,property,null),{propertyId:property,number:'INV 1',issueDate:'2027-01-05',dueDate:'2027-02-05',netAmount:'1000.50',vatRate:'18.00',category:'MAINTENANCE',description:'Repair',workOrderId:null,contractId:null,correctsInvoiceId:null});
});
