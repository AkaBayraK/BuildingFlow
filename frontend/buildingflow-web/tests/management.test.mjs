import {test} from "node:test";
import assert from "node:assert/strict";
import {parseUnit,parsePerson,parseOccupancyPage} from "../src/services/management-contracts.ts";

const propertyId="11111111-1111-4111-8111-111111111111",buildingId="22222222-2222-4222-8222-222222222222",personId="33333333-3333-4333-8333-333333333333";
const common={id:"44444444-4444-4444-8444-444444444444",propertyId,version:1,createdAt:"2026-10-08T12:00:00Z",isEnabled:true};
const unit={...common,buildingId:null,number:"B1",floor:-1,grossSquareMeters:120.25,type:"APARTMENT"};
const person={...common,id:personId,firstName:"Test",lastName:"Person",phoneNumber:null,email:null,activeOccupancyCount:2};
const relation={id:"55555555-5555-4555-8555-555555555555",propertyId,personId,unitId:unit.id,unitNumber:unit.number,personName:"Test Person",buildingId:null,buildingName:null,type:"OWNER",status:"ACTIVE",startDate:"2026-10-01",endDate:null,isPrimaryContact:true,version:1};

test("unit details accept direct apartments and six building unit types without inferring occupancy",()=>{
  assert.deepEqual(parseUnit(unit,propertyId,null),unit);
  for(const type of ['APARTMENT','OFFICE','SHOP','VILLA','WAREHOUSE','WORKSHOP'])assert.equal(parseUnit({...unit,buildingId,type},propertyId,buildingId).type,type);
  assert.equal(parseUnit({...unit,isEnabled:false},propertyId,null).isEnabled,false);
  assert.equal('occupancy' in parseUnit(unit,propertyId,null),false);
});
test("unit details reject wrong property/building scope and malformed numerical data",()=>{
  for(const patch of [{propertyId:buildingId},{buildingId},{type:'UNKNOWN'},{version:0},{version:1.5},{floor:2147483648},{floor:1.5},{grossSquareMeters:NaN},{grossSquareMeters:0},{grossSquareMeters:'120.25'},{createdAt:'invalid'},{isEnabled:'true'}])assert.throws(()=>parseUnit({...unit,...patch},propertyId,null));
  for(const value of [null,[],{},'unit'])assert.throws(()=>parseUnit(value,propertyId,null));
});
test("person details retain optional contacts and reject foreign scope or malformed relationship counts",()=>{
  assert.deepEqual(parsePerson(person,propertyId),person);
  assert.equal(parsePerson({...person,email:'test@example.invalid',phoneNumber:'+90 555 000 00 00'},propertyId).email,'test@example.invalid');
  for(const patch of [{propertyId:buildingId},{firstName:''},{lastName:null},{email:{}},{phoneNumber:42},{activeOccupancyCount:-1},{activeOccupancyCount:1.5}])assert.throws(()=>parsePerson({...person,...patch},propertyId));
});
test("relationship preview preserves coexisting roles, dates and historical statuses",()=>{
  const items=[relation,{...relation,type:'TENANT',isPrimaryContact:false},{...relation,type:'RESIDENT',status:'ENDED',endDate:'2026-10-07'}];
  const page={items,page:1,pageSize:5,totalCount:3};
  assert.deepEqual(parseOccupancyPage(page,propertyId,personId),page);
  assert.deepEqual(parseOccupancyPage(page,propertyId,personId).items.map(row=>row.type),['OWNER','TENANT','RESIDENT']);
});
test("relationship preview rejects foreign person/property, unknown roles and invalid calendar dates",()=>{
  for(const patch of [{personId:buildingId},{propertyId:buildingId},{type:'ADMIN'},{status:'OCCUPIED'},{startDate:'2026-02-30'},{endDate:'2026-09-30'},{startDate:'2026-10-01T00:00:00Z'},{isPrimaryContact:'true'},{buildingName:{}},{unitNumber:null}])assert.throws(()=>parseOccupancyPage({items:[{...relation,...patch}],page:1,pageSize:5,totalCount:1},propertyId,personId));
  for(const value of [null,{items:[],page:0,pageSize:5,totalCount:0},{items:[relation],page:1,pageSize:5,totalCount:0},{items:'bad',page:1,pageSize:5,totalCount:1}])assert.throws(()=>parseOccupancyPage(value,propertyId,personId));
});
