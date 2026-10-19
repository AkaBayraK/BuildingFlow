import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readDomain,readDomains} from '../src/features/website/domain-model.ts';
import {contentSecurityPolicy} from '../src/security/policy.ts';
const d={id:'11111111-1111-4111-8111-111111111111',hostName:'site.example.com',kind:'CUSTOM',status:'PENDING',isPrimary:false,version:1,verifiedAt:null,lastCheckedAt:null,tlsCheckedAt:null,approvedAt:null,verificationToken:null};
test('domain contracts reject URLs, unknown states and malformed secrets',()=>{assert.equal(readDomain(d).hostName,d.hostName);assert.throws(()=>readDomain({...d,hostName:'https://evil.example'}));assert.throws(()=>readDomain({...d,status:'AUTO_APPROVED'}));assert.throws(()=>readDomain({...d,verificationToken:'bad'}));assert.throws(()=>readDomains(Array(11).fill(d)))});
test('preview forms have only an explicit configured website destination',()=>{const csp=contentSecurityPolicy('YWJj','https://api.example','https://id.example',true,'https://sites.example');assert.ok(csp.includes("form-action 'self' https://sites.example"));assert.throws(()=>contentSecurityPolicy('YWJj','https://api.example','https://id.example',true,'https://user:pass@evil.example'))});
