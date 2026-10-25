import {test} from 'node:test';
import assert from 'node:assert/strict';
import {phoneCommand,challengeExpired,parseContact,parseOtpResult,maskContact,challengeOpen,emailAction,contactReturnUri} from '../src/features/account/contact-verification.ts';
const id='11111111-1111-4111-8111-111111111111';
const value=()=>({version:2,email:{status:'VERIFIED',value:'owner@example.test',available:true,checkedAt:'2026-10-23T10:00:00Z'},phone:{status:'PENDING',profileValue:'+905551234567',verifiedValue:'+905554567890',pendingValue:'+905551234567',verifiedAt:'2026-10-22T10:00:00Z',canSendCode:true,deliveryStatus:'ACCEPTED',challengeId:id,expiresAt:'2026-10-23T10:05:00Z',attemptsRemaining:4,retryAfterSeconds:59}});
test('authoritative contact preserves previous proof, pending target and server limits',()=>{const c=parseContact(value());assert.equal(c.phone.verifiedValue,'+905554567890');assert.equal(c.phone.attemptsRemaining,4);assert.equal(c.phone.retryAfterSeconds,59);assert.ok(challengeOpen(c.phone));});
test('malformed or unknown verification states fail closed',()=>{
 for(const raw of [null,[],{...value(),version:-1},{...value(),email:{...value().email,status:'TRUSTED'}},{...value(),email:{...value().email,available:false}},{...value(),phone:{...value().phone,attemptsRemaining:6}},{...value(),phone:{...value().phone,deliveryStatus:'SUCCESS'}},{...value(),phone:{...value().phone,status:'VERIFIED',verifiedValue:null}},{...value(),phone:{...value().phone,challengeId:'../other'}}])assert.throws(()=>parseContact(raw));
});
test('HTTP 200 acceptance and uncertain delivery never become verified',()=>{
 for(const outcome of ['ACCEPTED','UNKNOWN','REJECTED','RATE_LIMITED']){const r=parseOtpResult({operationId:id,replayed:false,outcome,contact:value(),retryAfterSeconds:60},id);assert.equal(r.contact.phone.status,'PENDING');}
 assert.throws(()=>parseOtpResult({operationId:id,replayed:false,outcome:'SUCCESS',contact:value(),retryAfterSeconds:0},id));assert.throws(()=>parseOtpResult({operationId:id,replayed:false,outcome:'VERIFIED',contact:value(),retryAfterSeconds:0},'other'));
 for(const deliveryStatus of ['VERIFIED','CANCELLED','EXPIRED','LOCKED','FAILED','NOT_REQUESTED'])assert.equal(challengeOpen({...value().phone,deliveryStatus}),false);
});
test('targets are masked and email callback excludes auth queries and arbitrary return paths',()=>{assert.equal(maskContact('owner@example.test','email'),'o•••@example.test');assert.equal(maskContact('+905551234567','phone'),'+•• ••• ••• 45 67');assert.equal(maskContact(null,'phone'),'—');assert.equal(contactReturnUri('https://buildingflow.test/other?code=secret#token'),'https://buildingflow.test/account');assert.equal(emailAction('UNVERIFIED'),'VERIFY_EMAIL');assert.equal(emailAction('VERIFIED'),'UPDATE_EMAIL');});

test('cancel includes the server challenge and expiry never overrides a completed proof',()=>{
 const c=parseContact(value());assert.deepEqual(phoneCommand(c,id,'CANCEL'),{operationId:id,expectedVersion:2,action:'CANCEL',challengeId:id});assert.equal('challengeId' in phoneCommand(c,id,'SEND'),false);assert.equal(phoneCommand(c,id,'VERIFY','123456').code,'123456');
 assert.equal(challengeExpired(c.phone,Date.parse('2026-10-24')),true);assert.equal(challengeExpired({...c.phone,deliveryStatus:'VERIFIED'},Date.parse('2026-10-24')),false);
});
