import test from 'node:test';
import assert from 'node:assert/strict';
import {isCommunicationRequest,decisionPath,parsePreferences,approve,withdraw,preferenceError} from '../src/communication/contracts.ts';
const id='22222222-2222-4222-8222-222222222222',sha='a'.repeat(64);
const prefs={verifiedPhoneMasked:'+90********67',text:{version:1,title:'T',body:'B',bodySha256:sha},items:[{senderId:id,scope:'PLATFORM',propertyId:null,senderTitle:'BF',status:'NONE',occurredAt:null,textVersion:null}]};
test('only the own preference routes pass the mobile transport whitelist',()=>{
 assert.ok(isCommunicationRequest('/api/v1/mobile/communication-preferences',undefined));
 assert.ok(isCommunicationRequest(decisionPath(id),{}));
 assert.ok(!isCommunicationRequest(decisionPath(id),undefined));
 assert.ok(!isCommunicationRequest('/api/v1/mobile/communication-preferences',{}));
 assert.ok(!isCommunicationRequest('/api/v1/properties/'+id+'/sms/opt-outs',{}));
 assert.throws(()=>decisionPath('../x'));
});
test('preferences never carry a readable phone and keep scope/property consistent',()=>{
 assert.equal(parsePreferences(prefs).items[0].status,'NONE');
 assert.throws(()=>parsePreferences({...prefs,verifiedPhoneMasked:'+905551234567'}));
 assert.throws(()=>parsePreferences({...prefs,items:[{...prefs.items[0],propertyId:id}]}));
 assert.throws(()=>parsePreferences({...prefs,items:[{...prefs.items[0],status:'MAYBE'}]}));
});
test('approval sends the shown text version and hash as MOBILE; withdrawal sends none',()=>{
 assert.deepEqual(approve(prefs.text,id),{operationId:id,approved:true,textVersion:1,textSha256:sha,channel:'MOBILE'});
 assert.deepEqual(withdraw(id),{operationId:id,approved:false,channel:'MOBILE'});
 assert.match(preferenceError(409),/doğrulanmadı/);
});
