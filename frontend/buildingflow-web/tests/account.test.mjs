import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {parseAccount,parseConsents,approveCommand,withdrawCommand,profileCommand,bodyMatches,allows,accountName,initials} from '../src/features/account/model.ts';
import {readUser} from '../src/features/users/model.ts';
const user='11111111-1111-4111-8111-111111111111',property='22222222-2222-4222-8222-222222222222',photo='33333333-3333-4333-8333-333333333333',textId='44444444-4444-4444-8444-444444444444';
const profile={userId:user,firstName:'Ada',lastName:'KAYA',phone:null,photoId:photo,photoSha256:'a'.repeat(64),version:3,modifiedAt:'2026-10-17T10:00:00Z'};
const account={userId:user,username:'ada',email:'ada@example.test',roles:['MANAGER'],contexts:[{propertyId:property,propertyName:'Güneş Sitesi',unitId:null,unitNumber:null,role:'MANAGER'}],hasMore:false,profile};
const body='Profil fotoğrafı metni';const sha=createHash('sha256').update(body).digest('hex');
const consentList=(state='NONE')=>({userId:user,items:[{status:{typeCode:'PROFILE_PHOTO',nameTr:'Profil fotoğrafı',nameEn:'Profile photo',kind:'EXPLICIT_CONSENT',feature:'PROFILE_PHOTO',state,approvedVersion:state==='NONE'?null:1,approvedAt:state==='NONE'?null:'2026-10-17T10:00:00Z',currentTextId:textId},currentText:{id:textId,textVersion:1,title:'Profil',body,bodySha256:sha,publishedAt:'2026-10-01T00:00:00Z'}}]});
test('account shape is accepted only for the signed-in user with known roles and a matching profile',()=>{
 const parsed=parseAccount(account,user);assert.equal(parsed.contexts[0].propertyName,'Güneş Sitesi');assert.equal(accountName(parsed.profile,'ada'),'Ada KAYA');assert.equal(initials('Ada KAYA'),'AK');assert.equal(initials('ırmak işık'),'Iİ');
 for(const bad of [{...account,userId:property},{...account,roles:['ROOT']},{...account,contexts:Array(501).fill(account.contexts[0])},{...account,profile:{...profile,photoSha256:null}},{...account,profile:{...profile,userId:property}}])assert.throws(()=>parseAccount(bad,user));
 assert.equal(accountName({...profile,firstName:'',lastName:''},'ada'),'ada');
});
test('consent approval carries the exact text and hash; withdrawal only the type',async()=>{
 const [c]=parseConsents(consentList(),user);assert.equal(allows(c),false);assert.equal(await bodyMatches(c.text,async v=>createHash('sha256').update(v).digest('hex')),true);
 assert.deepEqual(approveCommand(c,'op'),{operationId:'op',action:'APPROVE',typeCode:'PROFILE_PHOTO',textId,bodySha256:sha,appVersion:'web'});
 assert.deepEqual(withdrawCommand(c,'op2'),{operationId:'op2',action:'WITHDRAW',typeCode:'PROFILE_PHOTO'});
 assert.equal(allows(parseConsents(consentList('ACTIVE'),user)[0]),true);
 const tampered=consentList();tampered.items[0].currentText.id=photo;assert.throws(()=>parseConsents(tampered,user));
 assert.throws(()=>parseConsents({...consentList(),userId:property},user));
});
test('profile command sends the expected version and turns an empty phone into null',()=>{
 const p=parseAccount(account,user).profile;const command=profileCommand(p,'op',{firstName:'Ada',lastName:'KAYA',phone:'  '},'KEEP');
 assert.equal(command.expectedVersion,3);assert.equal(command.phone,null);assert.equal(command.photoAction,'KEEP');
});
test('managed users may carry a photo id, nothing else',()=>{
 const base={id:user,username:'ada',email:null,firstName:'Ada',lastName:'KAYA',enabled:true,emailVerified:false,createdAt:null,roles:['MANAGER'],propertyCount:1,linkedPeople:[],canEdit:true};
 assert.equal(readUser(base).photoId,undefined);assert.equal(readUser({...base,photoId:photo}).photoId,photo);assert.equal(readUser({...base,photoId:null}).photoId,null);
 assert.throws(()=>readUser({...base,photoId:'../x'}));
});
