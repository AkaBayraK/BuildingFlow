import {test} from 'node:test';
import assert from 'node:assert/strict';
import {accountProfileForSubject} from '../src/features/account/model.ts';
import {propertySelectionHref,createNavigation} from '../src/features/navigation/navigation.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const P='77777777-7777-4777-8777-777777777777',Q='88888888-8888-4888-8888-888888888888';

test('footer property chooser reuses canonical module entry paths without carrying a tenant ID',()=>{
 for(const section of ['units','people','dues','payments','expenses','announcements','documents','meters','reports','sms','requests','website']){
  const entry='/'+section;
  assert.equal(propertySelectionHref(`/properties/${P}/${section}`,P),entry);
  assert.equal(propertySelectionHref(entry,null),entry);
  assert.equal(isWorkspaceReturnPath(entry),true,entry);
 }
 assert.equal(propertySelectionHref(`/properties/${P}/buildings/${Q}/units`,P),'/units');
 assert.equal(propertySelectionHref(`/properties/${P}/people/${Q}`,P),'/people');
 assert.equal(propertySelectionHref(`/people/${P}/${Q}`,null),'/people');
});
test('footer falls back to the existing property picker for overview/building/unscoped or mismatched paths',()=>{
 for(const [path,id] of [[`/properties/${P}`,P],[`/properties/${P}/buildings`,P],[`/properties/${Q}/dues`,P],['/dashboard',null],['/account',null],['/properties',null],['//external.test',null],['/peopleevil',null]])assert.equal(propertySelectionHref(path,id),'/properties');
});
test('shared profile rejects old account data during a subject transition, including corrupt nested profiles',()=>{
 const profile={userId:P,firstName:'Ada',lastName:'DEMO',phone:null,photoId:null,photoSha256:null,version:1,modifiedAt:null};
 const account={userId:P,username:'fixture',email:null,roles:['SITE_ADMIN','ACCOUNTANT'],contexts:[],hasMore:false,profile};
 assert.equal(accountProfileForSubject(account,P),profile);
 for(const subject of [Q,undefined,''])assert.equal(accountProfileForSubject(account,subject),null);
 assert.equal(accountProfileForSubject(null,P),null);
 assert.equal(accountProfileForSubject({...account,profile:{...profile,userId:Q}},P),null);
 assert.equal(profile.userId,P,'the source account is not rewritten');
});
test('footer entry reuse does not expand multi-role or session-context menu visibility',()=>{
 for(const sessionContexts of [false,true]){
  const c={roles:['SITE_ADMIN','ACCOUNTANT'],sessionContexts,centralPeople:sessionContexts,propertyId:P,property:{id:P,type:'SITE'}};
  const m=createNavigation(c,`/properties/${P}/dues`),items=m.groups.flatMap(g=>g.items);
  assert.ok(items.some(i=>i.id==='dues'));assert.ok(items.some(i=>i.id==='people'));
  assert.equal(items.some(i=>i.id==='users'),!sessionContexts);
  assert.equal(propertySelectionHref(`/properties/${P}/dues`,P),'/dues');
 }
});
