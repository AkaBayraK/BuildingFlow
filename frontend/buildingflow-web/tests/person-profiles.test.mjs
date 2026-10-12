import {test} from 'node:test';
import assert from 'node:assert/strict';
import {parsePerson} from '../src/services/management-contracts.ts';
import {parseMessages} from '../src/features/people/messages.ts';
const id='11111111-1111-4111-8111-111111111111',property='22222222-2222-4222-8222-222222222222';
const person={id,propertyId:property,firstName:'Fixture',lastName:'Example',phoneNumber:'+905551234567',email:'fixture@example.test',isEnabled:true,version:1,createdAt:'2026-10-12T00:00:00Z',activeOccupancyCount:0,personKind:'NATURAL',citizenship:'DOMESTIC',countryCode:'TR',identityNumber:'12345678901',birthDate:'1990-01-01',gender:'FEMALE',address:'Fixture',linkedUserId:null,propertyName:'Fixture',units:[]};
test('profile decoder preserves legacy unknowns legal names and personal details',()=>{
 assert.equal(parsePerson(person,property).birthDate,'1990-01-01');assert.equal(parsePerson({...person,personKind:'LEGAL',lastName:'',identityNumber:'1234567890',birthDate:null,gender:null},property).lastName,'');assert.equal(parsePerson({...person,personKind:null,identityNumber:null},property).identityNumber,null);
 for(const patch of [{countryCode:'invalid'},{personKind:'OTHER'},{birthDate:'1990-02-31'},{gender:'invalid'},{units:[{unitId:'wrong'}]},{propertyId:id}])assert.throws(()=>parsePerson({...person,...patch},property));
});
test('notification decoder rejects foreign scopes malformed payload and duplicate entries',()=>{
 const item={id,propertyId:property,personId:id,title:'Fixture',body:'Message',createdAt:'2026-10-12T00:00:00Z',readAt:null};const page={items:[item],page:1,pageSize:10,totalCount:1};assert.equal(parseMessages(page,property,id).items.length,1);
 for(const v of [{...page,items:[{...item,propertyId:id}]},{...page,items:[{...item,title:{}}]},{...page,items:[item,item],totalCount:2},{...page,page:0}])assert.throws(()=>parseMessages(v,property,id));
});
