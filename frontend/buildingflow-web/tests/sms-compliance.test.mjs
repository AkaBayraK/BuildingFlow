import test from 'node:test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readSenderView,readPreview,readRecord,readPreferences,senderKeyword,senderShortCode,senderMersis,textMatches,approveCommand,withdrawCommand} from '../src/features/sms/compliance.ts';
const property='11111111-1111-4111-8111-111111111111',id='22222222-2222-4222-8222-222222222222';
const sender={id,propertyId:property,title:'Gül',mersisNumber:'0123456789012345',contactInformation:'c',optOutKeyword:'RET GUL',optOutShortCode:'4567',optOutEnabled:true,iysIntegrationEnabled:false,iysBrandCode:null,isEnabled:true,version:1};
const body='Onay metni',sha=createHash('sha256').update(body).digest('hex');
const record={recordId:id,senderId:id,recipientMasked:'+90********67',status:'APPROVED',source:'WEB',occurredAt:'2026-10-26T10:00:00Z',textVersion:1,iysStatus:'NOT_REQUIRED',replayed:false};
test('legal sender view is scoped to the requested Property',()=>{
 assert.equal(readSenderView({sender:null},property),null);
 assert.equal(readSenderView({sender},property).title,'Gül');
 assert.throws(()=>readSenderView({sender},null));
 assert.throws(()=>readSenderView({sender:{...sender,mersisNumber:'123'}},property));
 assert.throws(()=>readSenderView({},property));
});
test('sender form rules mirror the API',()=>{
 assert.ok(senderKeyword('RET GUL'));assert.ok(!senderKeyword('STOP'));assert.ok(!senderKeyword('RE'));
 assert.ok(senderShortCode('4567'));assert.ok(!senderShortCode('45a7'));
 assert.ok(senderMersis(''));assert.ok(senderMersis('0123456789012345'));assert.ok(!senderMersis('0123'));
});
test('preview must be consistent between allowed and blocked code',()=>{
 const ok={typeCode:'MARKETING',category:'MARKETING',body:'x',footer:'',encoding:'UCS2',characters:1,segments:1,allowed:true,blockedCode:null,legalSenderId:null};
 assert.equal(readPreview(ok).allowed,true);
 assert.throws(()=>readPreview({...ok,blockedCode:'OPTED_OUT'}));
 assert.equal(readPreview({...ok,allowed:false,blockedCode:'OPTED_OUT'}).blockedCode,'OPTED_OUT');
 assert.throws(()=>readPreview({...ok,encoding:'ASCII'}));
});
test('consent rows never carry a readable phone number or unknown source',()=>{
 assert.equal(readRecord(record).status,'APPROVED');
 assert.throws(()=>readRecord({...record,recipientMasked:'+905551234567'}));
 assert.throws(()=>readRecord({...record,source:'GUESS'}));
 assert.throws(()=>readRecord({...record,status:'MAYBE'}));
});
test('preferences require masked phone, hashed text and scope/property agreement',()=>{
 const prefs={verifiedPhoneMasked:'+90********67',operationalSmsEnabled:true,text:{version:1,title:'T',body,bodySha256:sha},items:[{senderId:id,scope:'PLATFORM',propertyId:null,senderTitle:'BF',status:'NONE',occurredAt:null,textVersion:null}]};
 assert.equal(readPreferences(prefs).items.length,1);
 assert.throws(()=>readPreferences({...prefs,verifiedPhoneMasked:'+905551234567'}));
 assert.throws(()=>readPreferences({...prefs,items:[{...prefs.items[0],propertyId:property}]}));
 assert.throws(()=>readPreferences({...prefs,text:{...prefs.text,bodySha256:'x'}}));
 assert.throws(()=>readPreferences({...prefs,operationalSmsEnabled:'yes'}));
});
test('approval sends the exact text version and hash; withdrawal sends none',async()=>{
 const text={version:1,title:'T',body,bodySha256:sha};
 assert.ok(await textMatches(text,async v=>createHash('sha256').update(v).digest('hex')));
 assert.ok(!await textMatches({...text,body:'değişti'},async v=>createHash('sha256').update(v).digest('hex')));
 assert.deepEqual(approveCommand(text,id),{operationId:id,approved:true,textVersion:1,textSha256:sha,channel:'WEB'});
 assert.deepEqual(withdrawCommand(id),{operationId:id,approved:false,channel:'WEB'});
});
