import test from 'node:test';import assert from 'node:assert/strict';
import {readSupplierFile,readSupplierContract,readFileMutation,readContractMutation,supplierPage,contractForm,validContractForm,readContractHistory} from '../src/features/suppliers/documents-model.ts';
const tenant='9b962f3e-8812-459f-a42f-45ec6e3c6eba',supplier='24d39d57-021b-4705-87d3-98a091c95f23',property='0f6a6b3c-9d1e-4c6a-8b8e-6a1c2d3e4f50',id='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',stamp='2026-10-29T05:00:00Z';
const parent={id:supplier,tenantId:tenant,name:'Acme',kind:'COMPANY',description:null,status:'ACTIVE',statusReason:null,isEnabled:true,version:2,createdAt:stamp,modifiedAt:stamp};
const file={id,tenantId:tenant,supplierId:supplier,propertyId:property,documentId:property,fileName:'contract.pdf',contentType:'application/pdf',size:20,sha256:'A'.repeat(64),expiresOn:null,previousDocumentId:null,revisionNumber:1,isCurrent:true,version:1,createdAt:stamp,propertyName:'Area'};
const contract={id,tenantId:tenant,supplierId:supplier,propertyId:property,areaServiceId:property,title:'Annual',startDate:'2026-01-01',endDate:'2026-12-31',amount:'1000.00',currency:'TRY',reminderDays:30,documentId:null,previousContractId:null,status:'DRAFT',renewalDue:false,cancellationReason:null,version:1,createdAt:stamp,propertyName:'Area',serviceName:'Elevator'};
test('supplier file DTO rejects foreign scope, public/unsafe file shapes and invalid revision chain',()=>{
 assert.deepEqual(readSupplierFile(file,tenant,supplier),file);
 for(const patch of [{tenantId:supplier},{supplierId:tenant},{documentId:'x'},{fileName:'contract.exe'},{fileName:'../file.pdf'},{contentType:'text/html'},{size:10485761},{sha256:'x'},{previousDocumentId:property,revisionNumber:1},{previousDocumentId:null,revisionNumber:2},{expiresOn:'bad'},{isCurrent:null}])assert.throws(()=>readSupplierFile({...file,...patch},tenant,supplier));
});
test('contract DTO keeps exact TRY decimal strings, date bounds and cancellation reason',()=>{
 assert.deepEqual(readSupplierContract(contract,tenant,supplier),contract);
 for(const patch of [{tenantId:supplier},{supplierId:tenant},{areaServiceId:'x'},{amount:1000},{amount:'1.001'},{amount:'0.00'},{currency:'USD'},{endDate:'2025-12-31'},{reminderDays:366},{status:'PAID'},{status:'CANCELLED'},{renewalDue:'true'}])assert.throws(()=>readSupplierContract({...contract,...patch},tenant,supplier));
});
test('file and contract mutation results reject foreign parents and invalid replay versions',()=>{
 assert.equal(readFileMutation({record:file,supplier:parent,replayed:false},tenant,supplier).supplier.version,2);
 assert.throws(()=>readFileMutation({record:file,supplier:{...parent,id:property},replayed:false},tenant,supplier));
 assert.equal(readContractMutation({record:{...contract,version:3},supplier:parent,replayed:true,appliedVersion:1},tenant,supplier).record.version,3);
 assert.throws(()=>readContractMutation({record:contract,supplier:parent,replayed:false,appliedVersion:2},tenant,supplier));
 assert.throws(()=>supplierPage({items:[file,file],page:1,pageSize:10,totalCount:2},v=>readSupplierFile(v,tenant,supplier)));
});
test('contract form validates dates, localized exact amounts and reminder days without rounding',()=>{
 const form=contractForm(contract);assert.ok(validContractForm(form));assert.ok(validContractForm({...form,amount:'1234,56'}));
 for(const patch of [{title:' '},{amount:'1,001'},{amount:'0'},{endDate:'2025-01-01'},{reminderDays:'366'},{reminderDays:'-1'},{documentId:'invalid'}])assert.ok(!validContractForm({...form,...patch}));
});
test('contract history restricts actions, versions and immutable snapshot size',()=>{
 const history={id,action:'CREATE',appliedVersion:1,snapshot:'{}',createdAt:stamp};assert.deepEqual(readContractHistory(history),history);
 for(const patch of [{action:'PAY'},{appliedVersion:0},{snapshot:'x'.repeat(10001)},{createdAt:'bad'}])assert.throws(()=>readContractHistory({...history,...patch}));
});
