import test from 'node:test';
import assert from 'node:assert/strict';
import {readUser,readUserPage,displayName,validUsername,strongPassword} from '../src/features/users/model.ts';
const user={id:'kc-1',username:'ayse',email:'a@example.test',firstName:'Ayşe',lastName:'Yılmaz',enabled:true,emailVerified:false,createdAt:'2026-10-15T18:00:00Z',roles:['RESIDENT'],propertyCount:1,linkedPeople:[{personId:'11111111-1111-4111-8111-111111111111',propertyId:'22222222-2222-4222-8222-222222222222',propertyName:'Gül',fullName:'Ayşe Yılmaz',version:2}],canEdit:true};
test('user contract and helpers',()=>{
 assert.equal(displayName(readUser(user)),'Ayşe Yılmaz');assert.equal(displayName(readUser({...user,firstName:null,lastName:null})),'ayse');
 assert.equal(readUserPage({items:[user],page:1,pageSize:20,totalCount:1}).items.length,1);
 for(const patch of [{roles:['ROOT']},{enabled:'yes'},{linkedPeople:[{personId:'x'}]},{propertyCount:-1}])assert.throws(()=>readUser({...user,...patch}));
 assert.ok(validUsername('ayse.yilmaz'));for(const bad of ['ab','Ayse','a b','.ayse'])assert.equal(validUsername(bad),false);
 assert.ok(strongPassword('Abcdefghij12'));assert.equal(strongPassword('abcdefghij12'),false);assert.equal(strongPassword('Abc12'),false);
});
