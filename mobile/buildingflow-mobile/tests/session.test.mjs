import test from 'node:test';
import assert from 'node:assert/strict';
import { parseSession, selectContext, contextKey } from '../src/auth/contracts.ts';
const context={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Örnek',unitId:'22222222-2222-2222-2222-222222222222',unitNumber:'1',role:'OWNER'};
const session={userId:'test-user',username:'Örnek',contexts:[context],hasMore:false};
test('verified multi-role contexts remain distinct',()=>{
 const value=parseSession({...session,contexts:[context,{...context,role:'TENANT'}]});
 assert.notEqual(contextKey(value.contexts[0]),contextKey(value.contexts[1]));
});
test('unknown roles and missing owner unit fail closed',()=>{
 assert.throws(()=>parseSession({...session,contexts:[{...context,role:'ROOT'}]}));
 assert.throws(()=>parseSession({...session,contexts:[{...context,unitId:null}]}));
});
test('foreign context selection is rejected',()=>assert.throws(()=>selectContext(session,'foreign')));
test('truncated and anonymous responses are rejected',()=>{
 assert.throws(()=>parseSession({...session,hasMore:true}));
 assert.throws(()=>parseSession({...session,userId:''}));
});
