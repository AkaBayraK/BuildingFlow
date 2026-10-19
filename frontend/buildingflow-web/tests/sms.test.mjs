import test from 'node:test';
import assert from 'node:assert/strict';
import {segments,placeholders,preview,readOverview,readMessage,readTemplate,readPage,readCredit} from '../src/features/sms/model.ts';
const property='11111111-1111-4111-8111-111111111111',id='22222222-2222-4222-8222-222222222222';
test('SMS segments follow GSM-7 and UCS-2 limits',()=>{
 assert.deepEqual(segments(''),{encoding:'GSM-7',length:0,segments:0});
 assert.equal(segments('a'.repeat(160)).segments,1);assert.equal(segments('a'.repeat(161)).segments,2);
 assert.equal(segments('€'.repeat(80)).segments,1);assert.equal(segments('€'.repeat(81)).length,162);
 assert.equal(segments('Kayıt'+'a'.repeat(65)).encoding,'UCS-2');assert.equal(segments('Kayıt'+'a'.repeat(66)).segments,2);
});
test('template placeholders and preview',()=>{
 assert.deepEqual(placeholders('Sayın {{fullName}}, {{ code }} {{code}}'),['fullName','code']);
 assert.equal(preview('Kod {{code}} {{other}}',{code:'482917'}),'Kod 482917 {{other}}');
});
test('SMS contracts reject unsafe or malformed rows',()=>{
 const account={id,propertyId:property,name:'Site',provider:'MOCK',senderHeader:'BUILDING',username:null,hasSecret:true,apiUrl:null,creditBalance:5,lowCreditThreshold:10,isLowCredit:true,isEnabled:true,version:1};
 const type={code:'PERSON_REGISTERED',purpose:'TRANSACTIONAL',parameters:['code'],secretParameters:['code'],defaultContent:'{{code}}'};
 assert.equal(readOverview({propertyId:property,account,types:[type]},property).account.creditBalance,5);
 assert.equal(readOverview({propertyId:property,account:null,types:[]},property).account,null);
 assert.throws(()=>readOverview({propertyId:property,account:{...account,provider:'NETGSM'},types:[]},property));
 assert.throws(()=>readOverview({propertyId:property,account:{...account,secret:'x',hasSecret:'yes'},types:[]},property));
 const message={id,typeCode:'MANUAL',purpose:'OPERATIONAL',personId:null,phoneMasked:'+90********67',message:'Hi',segments:1,creditCost:1,status:'SENT',provider:'MOCK',providerMessageId:'MOCK-1',errorCode:null,attempts:1,createdAt:'2026-10-15T18:00:00Z',sentAt:'2026-10-15T18:00:01Z',createdBy:'u'};
 assert.equal(readPage({items:[message],page:1,pageSize:10,totalCount:1},readMessage).items[0].status,'SENT');
 for(const patch of [{phoneMasked:'+905551234567'},{status:'DELIVERED'},{segments:0}])assert.throws(()=>readMessage({...message,...patch}));
 assert.throws(()=>readPage({items:[message,message],page:1,pageSize:1,totalCount:2},readMessage));
 assert.throws(()=>readTemplate({id:null,typeCode:'MANUAL',name:'x',content:'y',purpose:'OPERATIONAL',isEnabled:true,isSystemDefault:false,estimatedSegments:1,version:0}));
 assert.throws(()=>readCredit({id,type:'LOAD',quantity:0,balanceAfter:0,unitPrice:null,currency:null,referenceNo:null,description:null,messageId:null,createdAt:'2026-10-15T18:00:00Z',createdBy:'u'}));
});
