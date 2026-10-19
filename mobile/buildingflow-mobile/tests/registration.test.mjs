import test from 'node:test';
import assert from 'node:assert/strict';
import {normalizePhone,validateRegistration,validateLink,registrationBody,linkBody,parseRegistration,errorText,strongPassword} from '../src/auth/registration.ts';
const valid={username:'Ayse.Y',password:'Strong-Pass123',passwordConfirmation:'Strong-Pass123',firstName:' Ayşe ',lastName:'Yılmaz',phoneNumber:'0555 123 45 67',email:' Ayse@Example.test ',personCode:''};
test('phone normalization matches the server rules',()=>{
 for(const [input,out] of [['+90 555 123 45 67','+905551234567'],['0555 123 45 67','+905551234567'],['5551234567','+905551234567'],['0090 (555) 123-45-67','+905551234567']])assert.equal(normalizePhone(input),out);
 for(const bad of ['','555','+90 555 abc','12+34567890','+0123456789'])assert.equal(normalizePhone(bad),null);
});
test('registration validation covers every required field and optional code',()=>{
 assert.deepEqual(validateRegistration(valid),{});
 assert.deepEqual(Object.keys(validateRegistration({...valid,username:'a b',password:'weak',passwordConfirmation:'other',firstName:'',lastName:'',phoneNumber:'12',email:'x',personCode:'12'})).sort(),['email','firstName','lastName','password','passwordConfirmation','personCode','phoneNumber','username']);
 assert.equal(strongPassword('Abcdefghij12'),true);assert.equal(strongPassword('abcdefghij12'),false);
 const body=registrationBody('req-1',{...valid,personCode:' 482917 '});
 assert.deepEqual(body,{requestId:'req-1',username:'ayse.y',password:'Strong-Pass123',passwordConfirmation:'Strong-Pass123',firstName:'Ayşe',lastName:'YILMAZ',phoneNumber:'+905551234567',email:'ayse@example.test',personCode:'482917'});
 assert.equal(registrationBody('req-1',valid).personCode,null);
});
test('person link input requires code, names and phone; email optional',()=>{
 const link={personCode:'482917',firstName:'Ayşe',lastName:'Yılmaz',phoneNumber:'05551234567',email:''};
 assert.equal(validateLink(link),null);assert.equal(linkBody(link).email,null);assert.equal(linkBody(link).phoneNumber,'+905551234567');
 for(const patch of [{personCode:'012345'},{personCode:'48291'},{firstName:' '},{phoneNumber:'1'},{email:'bad'}])assert.notEqual(validateLink({...link,...patch}),null);
});
test('registration response and error texts are bounded',()=>{
 assert.equal(parseRegistration({userId:'u',username:'ayse',personLink:{status:'LINKED',propertyName:'Gül'}}).personLink.propertyName,'Gül');
 assert.equal(parseRegistration({userId:'u',username:'ayse',personLink:{status:'NONE'}}).personLink.propertyName,null);
 for(const bad of [null,{userId:'',username:'a',personLink:{status:'LINKED'}},{userId:'u',username:'a',personLink:{status:'DONE'}}])assert.throws(()=>parseRegistration(bad));
 assert.match(errorText('BF_MOBILE_LINK_LOCKED'),/15 dakika/);assert.match(errorText('<script>'),/İşlem tamamlanamadı/);
});
