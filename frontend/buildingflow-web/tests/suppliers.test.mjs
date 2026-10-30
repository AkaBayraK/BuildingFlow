import test from 'node:test';import assert from 'node:assert/strict';
import {readSupplierAreas,readSupplier,readSupplierPage,readSupplierMutation,supplierForm,supplierBody,validSupplier} from '../src/features/suppliers/model.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
const tenant='9b962f3e-8812-459f-a42f-45ec6e3c6eba',id='24d39d57-021b-4705-87d3-98a091c95f23';
const supplier={id,tenantId:tenant,name:'Acme',kind:'COMPANY',description:null,status:'DRAFT',statusReason:null,isEnabled:true,version:1,createdAt:'2026-10-28T12:00:00Z',modifiedAt:'2026-10-28T12:00:00Z'};
test('supplier decoder rejects foreign tenant, invalid kind/status/version and inconsistent disabled state',()=>{
 assert.deepEqual(readSupplier(supplier,tenant),supplier);
 for(const patch of [{tenantId:id},{id:'x'},{kind:'OTHER'},{status:'DONE'},{name:' '},{version:0},{description:'x'.repeat(2001)},{status:'INACTIVE'},{isEnabled:false},{statusReason:''},{modifiedAt:'never'}])assert.throws(()=>readSupplier({...supplier,...patch},tenant));
});
test('supplier pages reject duplicate rows, partial counters and unsafe replay version',()=>{
 const p={items:[supplier],page:1,pageSize:10,totalCount:1};assert.deepEqual(readSupplierPage(p,tenant),p);
 for(const patch of [{items:[supplier,supplier],totalCount:2},{totalCount:0},{page:0},{pageSize:101}])assert.throws(()=>readSupplierPage({...p,...patch},tenant));
 assert.equal(readSupplierMutation({record:{...supplier,version:2},replayed:true,appliedVersion:1},tenant).record.version,2);
 assert.throws(()=>readSupplierMutation({record:supplier,replayed:false,appliedVersion:2},tenant));
});
test('inactive and blocked forms require reason; normalized body keeps explicit version',()=>{
 const form=supplierForm(supplier);assert.ok(validSupplier(form));assert.ok(!validSupplier({...form,status:'INACTIVE'}));assert.ok(validSupplier({...form,status:'BLOCKED',statusReason:'Review'}));
 assert.deepEqual(supplierBody({...form,name:' Acme ',description:'  '},7),{name:'Acme',kind:'COMPANY',description:null,status:'DRAFT',statusReason:null,version:7});
});
test('supplier route is OIDC safe and only management roles receive the menu',()=>{
 assert.ok(isWorkspaceReturnPath('/suppliers'));for(const route of ['/suppliersevil','/suppliers?token=secret','/suppliers/unknown'])assert.ok(!isWorkspaceReturnPath(route));
 for(const role of ['SUPER_ADMIN','SITE_ADMIN','MANAGER','ACCOUNTANT','RESIDENT','SECURITY']){
  const nav=createNavigation({roles:[role],sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/suppliers');const item=nav.groups.flatMap(g=>g.items).find(i=>i.id==='suppliers');
  if(['SUPER_ADMIN','SITE_ADMIN','MANAGER'].includes(role)){assert.equal(item?.href,'/suppliers');assert.equal(item?.disabled,false);assert.equal(item?.active,true);}else assert.equal(item,undefined);
 }
});

test('supplier area DTO rejects cross-tenant/supplier ownership and duplicate rows',()=>{
 const row={id,tenantId:tenant,supplierId:id,propertyId:tenant,propertyName:'Area',categoryId:tenant,categoryName:'Maintenance',serviceName:'Elevator'};const p={items:[row],page:1,pageSize:10,totalCount:1};assert.equal(readSupplierAreas(p,tenant,id).items.length,1);
 for(const patch of [{tenantId:id},{supplierId:tenant},{propertyId:'x'},{categoryId:'x'},{serviceName:''}])assert.throws(()=>readSupplierAreas({...p,items:[{...row,...patch}]},tenant,id));
 assert.throws(()=>readSupplierAreas({...p,items:[row,row],totalCount:2},tenant,id));
});
