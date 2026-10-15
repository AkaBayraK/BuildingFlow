import test from 'node:test';
import assert from 'node:assert/strict';
import {readUnitFilter,unitListHref,readUnitParties} from '../src/features/people/unit-links.ts';
import {parsePerson,parseOccupancyPage} from '../src/services/management-contracts.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const p='9b962f3e-8812-459f-a42f-45ec6e3c6eba',b='24d39d57-021b-4705-87d3-98a091c95f23',u='16568d58-6626-42e1-ac9c-8c608b7e9975',person='af5ecf17-c5bc-4205-9247-d1e84f67fd18';
const units=[{id:u,buildingId:null},{id:b,buildingId:b}];
const response=()=>({propertyId:p,asOfDate:'2026-10-11',items:[{unitId:u,buildingId:null,owners:[{personId:person,personName:'Test Owner'}],tenants:[]},{unitId:b,buildingId:b,owners:[],tenants:[{personId:person,personName:'Test Tenant'}]}]});
test('person to unit links retain exact identity and optional building scope',()=>{
 assert.equal(unitListHref({propertyId:p,buildingId:null,unitId:u}),`/properties/${p}/units?unitId=${u}`);
 assert.equal(unitListHref({propertyId:p,buildingId:b,unitId:u}),`/properties/${p}/buildings/${b}/units?unitId=${u}`);
 for(const patch of [{unitId:'1'},{unitId:'00000000-0000-0000-0000-000000000000'},{propertyId:'//evil.test'},{buildingId:'../people'}])assert.throws(()=>unitListHref({propertyId:p,buildingId:null,unitId:u,...patch}));
});
test('invalid or duplicate unit filters fail closed instead of becoming an unfiltered list',()=>{
 assert.deepEqual(readUnitFilter(undefined),{invalid:false});assert.deepEqual(readUnitFilter(u.toUpperCase()),{unitId:u,invalid:false});
 for(const value of ['', '1', '../people', '00000000-0000-0000-0000-000000000000',[u], [u,b]])assert.deepEqual(readUnitFilter(value),{invalid:true});
});
test('OIDC return retains only a canonical unit UUID query and rejects token or redirect suffixes',()=>{
 for(const buildingId of [null,b]){const path=unitListHref({propertyId:p,buildingId,unitId:u});assert.equal(isWorkspaceReturnPath(path),true);for(const bad of [path+'&token=secret',path+'&unitId='+b,path+'#code=secret',path+'&redirect=https://evil.test',path.replace('unitId=','personId='),path.replace(u,'00000000-0000-0000-0000-000000000000')])assert.equal(isWorkspaceReturnPath(bad),false,bad);}
 assert.equal(isWorkspaceReturnPath(`/properties/${p}/people?unitId=${u}`),false);
 assert.equal(isWorkspaceReturnPath(`/properties/${p}/units?unitId=%31${u.slice(1)}`),false);
});
test('batched current parties keep all owners tenants and empty relationships',()=>{
 const value=response();value.items[0].owners.push({personId:b,personName:'Shared Owner'});assert.equal(readUnitParties(value,p,units).items[0].owners.length,2);
 assert.deepEqual(readUnitParties(value,p,units).items[1].owners,[]);assert.deepEqual(readUnitParties(value,p,units).items[0].tenants,[]);
});
test('party decoder rejects foreign property unit and building scopes or partial page responses',()=>{
 for(const mutate of [v=>v.propertyId=b,v=>v.items[0].unitId=person,v=>v.items[0].buildingId=b,v=>v.items.pop(),v=>v.items.push(v.items[0]),v=>v.items[1].unitId=u]){const value=response();mutate(value);assert.throws(()=>readUnitParties(value,p,units));}
});
test('party decoder rejects malformed person links duplicate people and invalid calendar dates',()=>{
 for(const mutate of [v=>v.asOfDate='2026-02-30',v=>v.items[0].owners[0].personId='bad',v=>v.items[0].owners[0].personName=' ',v=>v.items[0].owners[0].personName='x'.repeat(202),v=>v.items[0].owners.push(v.items[0].owners[0]),v=>v.items[0].tenants=null]){const value=response();mutate(value);assert.throws(()=>readUnitParties(value,p,units));}
});

test('occupancy link DTOs reject malformed route IDs before rendering',()=>{
 const row={id:b,propertyId:p,personId:person,unitId:u,buildingId:null,unitNumber:'1',personName:'Fixture',buildingName:null,type:'OWNER',startDate:'2026-10-01',endDate:null,isPrimaryContact:false,status:'ACTIVE',version:1};
 const page=value=>({items:[value],page:1,pageSize:10,totalCount:1});assert.equal(parseOccupancyPage(page(row),p,person).items.length,1);
 for(const patch of [{id:'bad'},{unitId:'../people'},{buildingId:'javascript:alert(1)'},{unitId:'00000000-0000-0000-0000-000000000000'},{propertyId:b},{personId:b}])assert.throws(()=>parseOccupancyPage(page({...row,...patch}),p,person));
});

test('person detail links never accept a different person in the same property',()=>{
 const row={id:person,propertyId:p,firstName:'Fixture',lastName:'Person',phoneNumber:null,email:null,isEnabled:true,version:1,createdAt:'2026-10-11T00:00:00Z',activeOccupancyCount:1};
 assert.equal(parsePerson(row,p,person).id,person);assert.throws(()=>parsePerson({...row,id:b},p,person));assert.throws(()=>parsePerson({...row,id:'bad'},p));
});
