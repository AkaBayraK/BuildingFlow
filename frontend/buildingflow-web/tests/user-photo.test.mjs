import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readUserPhoto,readUserPhotoResult,readUser} from '../src/features/users/model.ts';
import {preparePhoto,PhotoError,maxSourceBytes} from '../src/features/account/photo.ts';
const user='11111111-1111-4111-8111-111111111111',photo='22222222-2222-4222-8222-222222222222',operation='33333333-3333-4333-8333-333333333333';
const state={userId:user,photoId:photo,photoSha256:'a'.repeat(64),version:3,canReplace:true};
test('managed photo state is bound to target and validates version, digest and consent capability',()=>{
 assert.deepEqual(readUserPhoto(state,user),state);
 assert.equal(readUserPhoto({...state,photoId:null,photoSha256:null,version:0,canReplace:false},user).canReplace,false);
 for(const bad of [{...state,userId:photo},{...state,version:-1},{...state,version:0},{...state,version:1.5},{...state,photoSha256:null},{...state,photoId:null},{...state,photoSha256:'invalid'},{...state,canReplace:'true'},{...state,photoId:'bad'}])assert.throws(()=>readUserPhoto(bad,user));
 assert.deepEqual(readUserPhotoResult({operationId:operation,photo:state,replayed:true},user,operation),state);
 assert.throws(()=>readUserPhotoResult({operationId:photo,photo:state,replayed:false},user,operation));
 assert.throws(()=>readUserPhotoResult({operationId:operation,photo:state,replayed:'true'},user,operation));
});
test('directory photo capability is an optional strict boolean for existing clients',()=>{
 const entry={id:user,username:'fixture',email:null,firstName:null,lastName:null,enabled:true,emailVerified:false,createdAt:null,roles:['SITE_ADMIN'],propertyCount:0,linkedPeople:[],canEdit:false,photoId:null};
 assert.equal(readUser(entry).canManagePhoto,undefined);assert.equal(readUser({...entry,canManagePhoto:true}).canManagePhoto,true);
 assert.throws(()=>readUser({...entry,canManagePhoto:'true'}));
});
test('browser photo preparation rejects unsupported/oversized sources before decoding and closes >40MP decoded images',async()=>{
 await assert.rejects(()=>preparePhoto({type:'image/svg+xml',size:10}),e=>e instanceof PhotoError&&e.reason==='type');
 await assert.rejects(()=>preparePhoto({type:'image/jpeg',size:maxSourceBytes+1}),e=>e instanceof PhotoError&&e.reason==='size');
 const original=globalThis.createImageBitmap;let closed=false;
 try{globalThis.createImageBitmap=async()=>({width:8000,height:6000,close(){closed=true;}});
  await assert.rejects(()=>preparePhoto({type:'image/jpeg',size:1000}),e=>e instanceof PhotoError&&e.reason==='size');assert.equal(closed,true);
 }finally{if(original)globalThis.createImageBitmap=original;else delete globalThis.createImageBitmap;}
});
