import test from 'node:test';import assert from 'node:assert/strict';
import {readPlan,readPlanPage,readUpcoming,daysUntil,planBody,validPlanForm,maintenanceTypes} from '../src/features/maintenance/model.ts';
import {upcomingDates} from '../src/features/dashboard/upcoming-dates.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';import {managementSection} from '../src/features/navigation/routes.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';import {sidebarRows} from '../src/features/navigation/sidebar-layout.ts';
const p='9b962f3e-8812-459f-a42f-45ec6e3c6eba',id='24d39d57-021b-4705-87d3-98a091c95f23',id2='0f6a6b3c-9d1e-4c6a-8b8e-6a1c2d3e4f50';
const plan={id,propertyId:p,title:'Jeneratör Bakımı',description:null,type:'MAINTENANCE',plannedDate:'2026-11-12',location:'Jeneratör Odası',responsible:'Teknik Ekip',status:'PLANNED',version:1,createdAt:'2026-10-21T10:00:00Z',modifiedAt:'2026-10-21T10:00:00Z'};
const counts=maintenanceTypes.map((type,i)=>({type,count:i}));
test('plan DTO rejects foreign scope, unknown type/status, bad dates and blank optional text',()=>{
 assert.deepEqual(readPlan(plan,p),plan);
 for(const v of [{...plan,propertyId:id},{...plan,type:'FIRE'},{...plan,status:'DONE'},{...plan,plannedDate:'2026-02-30'},{...plan,plannedDate:'12.11.2026'},{...plan,title:' '},{...plan,title:'x'.repeat(201)},{...plan,location:''},{...plan,description:'x'.repeat(2001)},{...plan,version:0},{...plan,modifiedAt:'never'}])assert.throws(()=>readPlan(v,p));
});
test('plan pages need all five type counts in order and unique rows',()=>{
 const page={items:[plan],page:1,pageSize:10,totalCount:1,typeCounts:counts};assert.deepEqual(readPlanPage(page,p).typeCounts,counts);
 for(const v of [{...page,items:[plan,plan],totalCount:2},{...page,typeCounts:counts.slice(1)},{...page,typeCounts:[...counts].reverse()},{...page,typeCounts:counts.map(c=>({...c,count:-1}))},{...page,pageSize:101},{...page,totalCount:0}])assert.throws(()=>readPlanPage(v,p));
});
test('upcoming plans are open, not in the past, ordered and limited',()=>{
 const later={...plan,id:id2,plannedDate:'2026-11-28',type:'DRILL'};const v={today:'2026-10-21',items:[plan,later],nextDays:0};
 assert.equal(readUpcoming(v,p,4).items.length,2);
 for(const bad of [{...v,items:[later,plan]},{...v,items:[{...plan,plannedDate:'2026-10-20'}]},{...v,items:[{...plan,status:'COMPLETED'}]},{...v,items:[{...plan,status:'CANCELLED'}]},{...v,nextDays:-1},{...v,today:'2026-13-01'}])assert.throws(()=>readUpcoming(bad,p,4));
 assert.throws(()=>readUpcoming(v,p,1));
});
test('dashboard list merges dues and plans by date, dues first on the same day, max four',()=>{
 assert.equal(daysUntil('2026-10-31','2026-10-19'),12);assert.equal(daysUntil('2026-10-19','2026-10-19'),0);assert.equal(daysUntil('2027-01-01','2026-12-31'),1);
 const dues=[{dueDate:'2026-10-31',chargeCount:120,amount:'245800.00'},{dueDate:'2026-11-12',chargeCount:1,amount:'10.00'}];
 const plans=[{...plan,plannedDate:'2026-11-05',title:'Genel Kurul',type:'OTHER'},plan,{...plan,id:id2,plannedDate:'2026-11-28',type:'DRILL'}];
 const rows=upcomingDates(dues,plans,'2026-10-19');
 assert.deepEqual(rows.map(r=>[r.kind,r.date]),[['dues','2026-10-31'],['plan','2026-11-05'],['dues','2026-11-12'],['plan','2026-11-12']]);
 assert.equal(rows[0].days,12);assert.equal(upcomingDates([],[],'2026-10-19').length,0);
 assert.deepEqual(upcomingDates([{dueDate:'2026-10-18',chargeCount:1,amount:'1.00'}],[],'2026-10-19'),[]);
});
test('form body trims text, sends null for blanks and validates limits',()=>{
 const form={title:'  Merdiven Temizliği ',description:'  ',type:'CLEANING',plannedDate:'2026-11-25',location:' A Blok ',responsible:'',status:'PLANNED'};
 assert.deepEqual(planBody(form,3),{title:'Merdiven Temizliği',description:null,type:'CLEANING',plannedDate:'2026-11-25',location:'A Blok',responsible:null,status:'PLANNED',version:3});
 assert.ok(validPlanForm(form));
 for(const bad of [{...form,title:' '},{...form,title:'x'.repeat(201)},{...form,plannedDate:''},{...form,plannedDate:'1999-12-31'},{...form,plannedDate:'2101-01-01'},{...form,location:'x'.repeat(201)},{...form,description:'x'.repeat(2001)}])assert.equal(validPlanForm(bad),false);
});
test('planning route, OIDC return allowlist and menu visibility follow the finance roles',()=>{
 assert.ok(isWorkspaceReturnPath('/maintenance'));assert.ok(isWorkspaceReturnPath('/properties/'+p+'/maintenance'));assert.equal(managementSection('/properties/'+p+'/maintenance',p),'maintenance');
 assert.equal(isWorkspaceReturnPath('/properties/'+p+'/maintenance?token=x'),false);
 for(const [role,visible] of [['SUPER_ADMIN',true],['SITE_ADMIN',true],['MANAGER',true],['ACCOUNTANT',true],['RESIDENT',false]]){
  const model=createNavigation({roles:[role],sessionContexts:false,centralPeople:false,propertyId:p,property:{id:p,type:'SITE'}},'/properties/'+p+'/maintenance');
  const item=model.groups.flatMap(g=>g.items).find(i=>i.id==='maintenance');assert.equal(!!item,visible,role);
  if(item){assert.equal(item.href,'/properties/'+p+'/maintenance');assert.ok(item.active);assert.ok(sidebarRows(model).some(r=>r.kind==='link'&&r.id==='maintenance'));}
 }
});
