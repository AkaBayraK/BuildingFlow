import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createNavigation,navigationDuplicates} from '../src/features/navigation/navigation.ts';
import {sidebarRows} from '../src/features/navigation/sidebar-layout.ts';
const P='77777777-7777-4777-8777-777777777777';
const flatten=rows=>rows.flatMap(row=>row.kind==='link'?[row.item]:row.items);
test('reference sidebar reorders presentation without dropping, duplicating or granting any model item',()=>{
 for(let mask=0;mask<16;mask++)for(const sessionContexts of [false,true])for(const centralPeople of [false,true])for(const type of [null,'APARTMENT','SITE']){
  const roles=['SUPER_ADMIN','SITE_ADMIN','MANAGER','ACCOUNTANT'].filter((_,i)=>mask&(1<<i));
  const model=createNavigation({roles,sessionContexts,centralPeople,propertyId:type?P:null,property:type?{id:P,type}:null},'/dashboard');
  const old=model.groups.flatMap(g=>g.items),rows=sidebarRows(model),items=flatten(rows);
  assert.deepEqual(items.map(i=>i.id).sort(),old.map(i=>i.id).sort());assert.deepEqual(navigationDuplicates(items),[]);
  for(const item of items)assert.equal(item,old.find(i=>i.id===item.id),'reuse exact authorized item');
  assert.equal(rows[0].id,'dashboard');assert.equal(rows[1].id,'properties');
  assert.ok(rows.find(r=>r.id==='property').items.some(i=>i.id==='units'));
  if(type==='APARTMENT')assert.ok(!items.some(i=>i.id==='buildings'));
 }
});
test('reference children retain planned and active state; role-restricted parents disappear when empty',()=>{
 const base={roles:['ACCOUNTANT'],sessionContexts:true,centralPeople:true,propertyId:P,property:{id:P,type:'SITE'}};
 const rows=sidebarRows(createNavigation(base,`/properties/${P}/payments`));
 assert.equal(rows.find(r=>r.id==='finance').items.find(i=>i.id==='payments').active,true);
 assert.ok(!rows.some(r=>r.id==='people'||r.id==='administration'||r.id==='website'));
 for(const item of rows.find(r=>r.id==='property').items.filter(i=>['shared-areas','parking','suppliers'].includes(i.id))){assert.equal(item.disabled,true);assert.equal(item.href,undefined);}
});
