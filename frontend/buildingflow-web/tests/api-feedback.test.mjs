import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {errorCodes} from '../src/services/error-codes.ts';
import {codedMessage,translateCode,problemDetails} from '../src/services/problem.ts';
import {ApiError,authorizedRequest} from '../src/services/authorized-request.ts';
import {notificationText} from '../src/components/feedback/notification-text.ts';
import {appendNotification,notificationTimer,notificationDuration} from '../src/components/feedback/notification-model.ts';
const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url)));
const en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url)));
const backend=readFileSync(new URL('../../../backend/src/BuildingFlow.Application/ErrorCatalog.cs',import.meta.url),'utf8');
test('all backend public codes have status, severity and matching TR/EN frontend translations',()=>{
 const codes=[...backend.matchAll(/new\("(BF_[A-Z0-9_]+)"/g)].map(match=>match[1]);
 assert.equal(new Set(codes).size,codes.length);assert.deepEqual(codes.sort(),Object.keys(errorCodes).sort());
 assert.deepEqual(codes,Object.keys(tr.apiErrors).sort());assert.deepEqual(codes,Object.keys(en.apiErrors).sort());
 for(const code of codes){const definition=errorCodes[code];const parameters=Object.fromEntries(definition.parameters.map(name=>[name,name==='field'?'propertyName':200]));
  const message=codedMessage({code,parameters},definition.status);assert.ok(message);assert.notEqual(translateCode(message,'tr'),translateCode(message,'en'));
  const parsed=problemDetails(definition.status,{code,parameters,message:'private-secret',severity:'success'},'en');assert.equal(parsed.severity,definition.severity);assert.equal(parsed.message,translateCode(message,'en'));assert.ok(!parsed.message.includes('private-secret'));
 }
});
test('field error parameters are localized and raw fallback text never overrides codes',()=>{
 const body={code:'BF_VALIDATION_FAILED',message:'private-secret',fieldIssues:{name:[{code:'BF_FIELD_TOO_LONG',parameters:{field:'propertyName',max:200},message:'private-secret'}]}};
 assert.deepEqual(problemDetails(400,body,'en').errors,{name:['Property name must be at most 200 characters.']});
 assert.deepEqual(problemDetails(400,body,'tr').errors,{name:['Yönetim alanı adı en fazla 200 karakter olabilir.']});
 assert.equal(problemDetails(404,{code:'BF_RESOURCE_NOT_FOUND',message:'private-secret'},'en').code,'BF_RESOURCE_NOT_FOUND');
});
test('unknown codes, wrong statuses and untrusted parameter values fall back safely',()=>{
 for(const code of ['__proto__','constructor','BF_NEW_PRIVATE']){const parsed=problemDetails(409,{code,title:'Bu sitede aynı blok adı zaten var.',message:'private-secret'},'en');assert.equal(parsed.code,null);assert.match(parsed.message,/record has changed/);}
 for(const parameters of [{field:'private-secret',max:200},{field:'propertyName',max:'200'},{field:'propertyName',max:0},{field:'propertyName',max:1001},{field:'propertyName',max:1.5},{field:'propertyName'},{field:'propertyName',max:200,token:'private-secret'},[]])assert.equal(codedMessage({code:'BF_FIELD_TOO_LONG',parameters},400),null);
 assert.equal(codedMessage({code:'BF_RESOURCE_NOT_FOUND'},500),null);
 assert.deepEqual(problemDetails(400,{code:'BF_NEW_PRIVATE',errors:{name:['private-secret']}},'en').errors,{});
 assert.deepEqual(problemDetails(400,{fieldIssues:{token:[{code:'BF_FIELD_REQUIRED',parameters:{field:'propertyName'}}]}},'en').errors,{});
});
test('notification queue deduplicates and keeps a bounded list without refreshing unrelated timers',()=>{
 const first={id:1,key:'message',kind:'error'};const rows=[first];assert.equal(appendNotification(rows,{...first,id:2}),rows);
 let queue=[];for(let id=1;id<=6;id++)queue=appendNotification(queue,{id,key:`message${id}`,kind:'warning'});assert.deepEqual(queue.map(row=>row.id),[3,4,5,6]);
});
test('all notification kinds expire at eight seconds; pause resumes only remaining time and dispose cancels',()=>{
 assert.equal(notificationDuration,8000);let now=0;let serial=0;const jobs=new Map();let expired=0;
 const schedule=(fn,delay)=>{const id=++serial;jobs.set(id,{fn,at:now+delay});return id;};const cancel=id=>jobs.delete(id);
 const advance=ms=>{now+=ms;for(const [id,job]of jobs)if(job.at<=now){jobs.delete(id);job.fn();}};
 const timer=notificationTimer(()=>expired++,()=>now,schedule,cancel);advance(3000);timer.pause();advance(20000);assert.equal(expired,0);timer.resume();timer.resume();assert.equal(jobs.size,1);advance(4999);assert.equal(expired,0);advance(1);assert.equal(expired,1);
 timer.resume();assert.equal(jobs.size,0);const other=notificationTimer(()=>expired++,()=>now,schedule,cancel);other.dispose();advance(10000);assert.equal(expired,1);
});

test('grouped field messages and parameterized notices follow a changed language',()=>{
 const fields='Daire numarası zorunludur.\nKat zorunludur.';
 const english=notificationText(fields,'en');assert.equal(english,'Apartment number is required.\nFloor is required.');assert.equal(notificationText(english,'tr'),fields);
 const value={text:'ignored server fallback',code:'BF_FIELD_REQUIRED_MAX',parameters:{field:'firstName',max:100}};
 assert.equal(notificationText(value,'en'),'First name is required and must be at most 100 characters.');
 assert.equal(notificationText(value,'tr'),'Ad zorunludur ve en fazla 100 karakter olabilir.');
 assert.equal(notificationText('Ad zorunludur ve en fazla 100 karakter olabilir.','en'),notificationText(value,'en'));
});

test('the central transport reports coded warnings, keeps fields, and redacts raw failures',async()=>{
 const notices=[];const reporter={error:value=>notices.push(['error',value]),warning:value=>notices.push(['warning',value])};const identity={token:'test-token',updateToken:async()=>true};
 const send=async(url,options)=>{assert.equal(url,'http://local.test/api');assert.equal(options.cache,'no-store');assert.equal(new Headers(options.headers).get('Authorization'),'Bearer test-token');return new Response(JSON.stringify({code:'BF_RECORD_CHANGED',message:'private-secret'}),{status:409});};
 await assert.rejects(authorizedRequest(identity,'en',reporter,'http://local.test','/api',{},send),error=>error instanceof ApiError&&error.status===409&&error.feedback.code==='BF_RECORD_CHANGED'&&!error.message.includes('private-secret'));
 assert.equal(notices.length,1);assert.equal(notices[0][0],'warning');assert.equal(notices[0][1].code,'BF_RECORD_CHANGED');
 await assert.rejects(authorizedRequest(identity,'en',reporter,'http://local.test','/api',{},async()=>new Response(JSON.stringify({code:'BF_VALIDATION_FAILED',fieldIssues:{name:[{code:'BF_FIELD_REQUIRED',parameters:{field:'propertyName'}}]}}),{status:400})),error=>error instanceof ApiError&&error.errors.name[0]==='Property name is required.');
 await assert.rejects(authorizedRequest(identity,'en',reporter,'http://local.test','/api',{},async()=>{throw new Error('private-secret');}));assert.ok(!JSON.stringify(notices).includes('private-secret'));
});
test('abort before refresh, during refresh and during problem parsing never produces a notice',async()=>{
 const notices=[];const reporter={error:value=>notices.push(value),warning:value=>notices.push(value)};
 const before=new AbortController();before.abort();await assert.rejects(authorizedRequest({updateToken:async()=>{assert.fail('refresh must not run');}},'en',reporter,'http://local.test','/api',{signal:before.signal}));
 const refresh=new AbortController();await assert.rejects(authorizedRequest({updateToken:async()=>{refresh.abort();throw new Error('expired');}},'en',reporter,'http://local.test','/api',{signal:refresh.signal}));
 const body=new AbortController();await assert.rejects(authorizedRequest({token:'test',updateToken:async()=>true},'en',reporter,'http://local.test','/api',{signal:body.signal},async()=>({ok:false,status:400,json:async()=>{body.abort();throw new DOMException('Aborted','AbortError');}})));
 assert.deepEqual(notices,[]);
});
