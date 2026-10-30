import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createNavigation, isNavigationActive, navigationDuplicates} from '../src/features/navigation/navigation.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const P='77777777-7777-4777-8777-777777777777', Q='88888888-8888-4888-8888-888888888888';
const context=(patch={})=>({roles:[],sessionContexts:false,centralPeople:false,propertyId:null,property:null,...patch});
const entries=m=>[...m.groups.flatMap(g=>g.items),m.account,m.home];
const byId=(m,id)=>entries(m).find(i=>i.id===id);

test('role matrix preserves finance, personal data and users visibility without granting unknown roles',()=>{
 for(const role of [null,'RESIDENT','OWNER','UNKNOWN','ACCOUNTANT','MANAGER','SITE_ADMIN','SUPER_ADMIN']){
  const m=createNavigation(context({roles:role?[role]:[]}),'/');
  const manager=['MANAGER','SITE_ADMIN','SUPER_ADMIN'].includes(role),finance=manager||role==='ACCOUNTANT';
  for(const id of ['people','website','requests','sms'])assert.equal(!!byId(m,id),manager,`${role}:${id}`);
  for(const id of ['maintenance','meters','reports','dues','payments','expenses'])assert.equal(!!byId(m,id),finance,`${role}:${id}`);
  assert.equal(!!byId(m,'users'),['SITE_ADMIN','SUPER_ADMIN'].includes(role));
  assert.equal(!!byId(m,'consents'),manager);
  for(const id of ['dashboard','properties','units','announcements','documents'])assert.ok(byId(m,id));
 }
 assert.ok(byId(createNavigation(context({roles:['RESIDENT','ACCOUNTANT','MANAGER']}),'/'),'people'));
});

test('session flags retain platform-only users/admin links and central people destinations',()=>{
 for(const sessionContexts of [false,true])for(const centralPeople of [false,true])for(const role of ['SITE_ADMIN','MANAGER','SUPER_ADMIN','ACCOUNTANT']){
  const m=createNavigation(context({roles:[role],sessionContexts,centralPeople,propertyId:P,property:{id:P,type:'SITE'}}),'/');
  assert.equal(!!byId(m,'users'),role==='SUPER_ADMIN'||!sessionContexts&&role==='SITE_ADMIN');
  assert.equal(!!byId(m,'consents'),role!=='ACCOUNTANT'&&(!sessionContexts||role==='SUPER_ADMIN'));
  if(role!=='ACCOUNTANT')assert.equal(byId(m,'people').href,centralPeople?'/people':`/properties/${P}/people`);
  assert.equal(m.home.href,sessionContexts?'/workspaces':'/');
 }
});

test('scope comes from validated UUID and matching property; apartments never gain a building link',()=>{
 for(const type of ['APARTMENT','SITE','RESIDENCE','MIXED']){
  const m=createNavigation(context({propertyId:P,property:{id:P,type}}),`/properties/${P}`);
  assert.equal(!!byId(m,'buildings'),type!=='APARTMENT');
  assert.equal(byId(m,'units').label.key,type==='APARTMENT'?'apartments':'navUnits');
  assert.equal(byId(m,'units').href,`/properties/${P}/units`);
 }
 for(const property of [null,{id:Q,type:'SITE'}])assert.equal(byId(createNavigation(context({propertyId:P,property}),'/'),'buildings'),undefined);
 for(const propertyId of [null,'invalid',`${P}/units`]){
  const m=createNavigation(context({propertyId}),'/');assert.equal(byId(m,'units').href,'/units');assert.equal(byId(m,'overview'),undefined);
 }
});

test('nested and parent active state rejects sibling scopes and prefix collisions',()=>{
 const m=createNavigation(context({roles:['SUPER_ADMIN'],propertyId:P,property:{id:P,type:'SITE'}}),`/properties/${P}/buildings/${Q}/units`);
 assert.equal(byId(m,'units').active,true);assert.equal(byId(m,'buildings').active,false);assert.equal(byId(m,'overview').active,false);
 for(const path of [`/properties/${Q}/units`,`/properties/${P}/unitsevil`,`/properties/${P}/peopleevil`,'/properties-malformed'])assert.equal(isNavigationActive(byId(m,'units'),path),false);
 assert.equal(isNavigationActive(byId(m,'overview'),`/properties/${P}/unknown`),false);
 const root=createNavigation(context({roles:['MANAGER'],centralPeople:true,sessionContexts:true}),`/people/${Q}/${P}`);
 assert.equal(byId(root,'people').active,true);assert.equal(isNavigationActive(byId(root,'people'),'/peopleevil'),false);
 assert.equal(isNavigationActive(root.home,`/workspaces/property/${P}`),true);
 const parent={id:'parent',href:'/parent',disabled:false,active:false,icon:'Users',label:{namespace:'ui',key:'people'},children:[byId(root,'people')]};
 assert.equal(isNavigationActive(parent,`/people/${Q}`),true);
});

test('disabled placeholders stay routeless, unsearchable, and inactive; destinations keep OIDC compatibility',()=>{
 const m=createNavigation(context({roles:['SUPER_ADMIN'],propertyId:P,property:{id:P,type:'SITE'}}),'/');
 for(const id of ['definitions']){
  const item=byId(m,id);assert.equal(item.disabled,true);assert.equal(item.href,undefined);assert.equal(isNavigationActive(item,'/'),false);assert.ok(!m.searchable.some(i=>i.id===id));
 }
 assert.deepEqual(m.shortcuts.map(i=>i.id),['dashboard','properties','units','people']);
 // Yönetim > Sistem Ayarları sistem durumu ekranına açılır ve aranabilir.
 assert.equal(byId(m,'settings').href,'/system-settings');assert.equal(byId(m,'settings').disabled,false);assert.ok(m.searchable.some(i=>i.id==='settings'));
 for(const item of entries(m).filter(i=>!i.disabled))assert.equal(isWorkspaceReturnPath(item.href),item.id!=='home',item.href);
 // Scoped SMS was missing from the allowlist (reload sent users to /); QA FAZ 04 closed it. Root stays excluded.
 assert.equal(m.searchable.filter(i=>i.href==='/users').length,1);
 assert.equal(byId(m,'planned-users'),undefined);
});

test('duplicate detection covers nested IDs and normalized routes across the full role/flag matrix',()=>{
 for(let mask=0;mask<16;mask++)for(const sessionContexts of [true,false])for(const centralPeople of [true,false]){
  const roles=['SUPER_ADMIN','SITE_ADMIN','MANAGER','ACCOUNTANT'].filter((_,i)=>mask&(1<<i));
  const m=createNavigation(context({roles,sessionContexts,centralPeople,propertyId:P,property:{id:P,type:'SITE'}}),'/');assert.deepEqual(navigationDuplicates(entries(m)),[]);
 }
 const m=createNavigation(context(),'/');
 assert.deepEqual(navigationDuplicates([m.account,{...m.account,href:'/ACCOUNT/'}]),['id:account','route:/account']);
 assert.deepEqual(navigationDuplicates([{...m.home,children:[m.account]},m.account]),['id:account','route:/account']);
});
