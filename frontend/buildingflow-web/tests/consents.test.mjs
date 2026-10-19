import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {parseMarkup,safeHref,normalizeMarkup} from '../src/features/consents/markup.ts';
import {readType,readTypes,readText,readRecord,readDocument,readHistory,readPage} from '../src/features/consents/model.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const vectors=JSON.parse(readFileSync(new URL('../../../docs/api/consent-markup-vectors.json',import.meta.url),'utf8'));
const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url)));
const en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url)));
const id='22222222-2222-4222-8222-222222222222',type='6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4a02',text='33333333-3333-4333-8333-333333333333',hash='a'.repeat(64);

test('web consent parser matches every shared API vector',()=>{
 assert.ok(vectors.length>=16);
 for(const v of vectors){const result=parseMarkup(v.input);assert.equal(result.error,v.error,v.name);assert.deepEqual(result.blocks,v.blocks,v.name);}
});
test('limits, hidden characters and unsafe links are rejected before publishing',()=>{
 assert.equal(parseMarkup(null).error,'EMPTY');assert.equal(parseMarkup(' \n\n ').error,'EMPTY');
 assert.equal(parseMarkup('a'.repeat(50_001)).error,'TOO_LONG');assert.equal(parseMarkup('a\n'.repeat(2_000)+'a').error,'TOO_MANY_LINES');
 for(const hidden of ['a\u0000b','a‮b','﻿a','a\ud800b','a\udc00b','a\u0007b'])assert.equal(parseMarkup(hidden).error,'CONTROL_CHAR',JSON.stringify(hidden));
 assert.equal(parseMarkup('emoji 😀 ok').error,null);
 for(const href of ['javascript:alert(1)','http://example.com','https://user:pw@example.com','https://','data:text/html,x','mailto:not-an-email','HTTPS://example.com','https://'+'a'.repeat(2000)])assert.equal(safeHref(href),false,href);
 for(const href of ['https://example.com/kvkk?x=1#a','mailto:kvkk@example.com'])assert.equal(safeHref(href),true,href);
 assert.equal(parseMarkup('[x](javascript:alert(1))').error,'LINK');
 assert.equal(normalizeMarkup('a\r\nb\rc\td'),'a\nb\nc d');
});
test('consent contracts reject malformed, mismatched or impossible rows',()=>{
 const current={id:text,textVersion:2,title:'Metin',requiresReconsent:true,publishedAt:'2026-10-16T10:00:00Z',publishedBy:'admin'};
 const t={id:type,code:'PROFILE_PHOTO',nameTr:'Profil',nameEn:'Profile',kind:'EXPLICIT_CONSENT',isSystem:true,feature:'PROFILE_PHOTO',sortOrder:20,isEnabled:true,version:1,textCount:2,currentText:current};
 assert.equal(readTypes([t])[0].currentText.textVersion,2);
 for(const patch of [{code:'bad code'},{kind:'OTHER'},{isSystem:false},{textCount:0},{currentText:null},{version:0},{currentText:{...current,textVersion:0}}])assert.throws(()=>readType({...t,...patch}),JSON.stringify(patch));
 assert.equal(readType({...t,textCount:0,currentText:null}).currentText,null);
 const body={id:text,consentTypeId:type,typeCode:'PROFILE_PHOTO',textVersion:1,title:'T',body:'# A',bodySha256:hash,requiresReconsent:true,publishedAt:'2026-10-16T10:00:00Z',publishedBy:'admin'};
 assert.equal(readText(body,type).textVersion,1);assert.throws(()=>readText(body,id));assert.throws(()=>readText({...body,bodySha256:'A'.repeat(64)}));
 const record={id,ownerUserId:'user-1',approverName:'Ada Yılmaz',typeId:type,typeCode:'PROFILE_PHOTO',typeNameTr:'Profil',typeNameEn:'Profile',kind:'EXPLICIT_CONSENT',textId:text,textVersion:1,approvedAt:'2026-10-16T10:00:00Z',channel:'MOBILE',appVersion:'1.0.0',revokedAt:null,revokeReason:null,state:'OUTDATED'};
 assert.equal(readPage({items:[record],page:1,pageSize:20,totalCount:1},readRecord).items[0].state,'OUTDATED');
 assert.equal(readRecord({...record,revokedAt:'2026-10-17T10:00:00Z',revokeReason:'WITHDRAWN',state:'WITHDRAWN'}).state,'WITHDRAWN');
 for(const patch of [{state:'WITHDRAWN'},{revokedAt:'2026-10-17T10:00:00Z'},{revokedAt:'2026-10-17T10:00:00Z',revokeReason:'SUPERSEDED',state:'ACTIVE'},{channel:'SMS'},{state:'NONE'}])assert.throws(()=>readRecord({...record,...patch}),JSON.stringify(patch));
 const doc={record,title:'T',body:'# A',bodySha256:hash,evidenceSha256:hash,bodyIntact:true,evidenceIntact:false};
 assert.equal(readDocument(doc,id).evidenceIntact,false);assert.throws(()=>readDocument(doc,text));assert.throws(()=>readDocument({...doc,evidenceIntact:'no'},id));
 const summary={typeId:type,code:'PROFILE_PHOTO',nameTr:'Profil',nameEn:'Profile',kind:'EXPLICIT_CONSENT',state:'WITHDRAWN',approvedVersion:null,approvedAt:null,currentVersion:2};
 assert.equal(readHistory({userId:'user-1',summary:[summary],records:[record]},'user-1').summary[0].state,'WITHDRAWN');
 assert.throws(()=>readHistory({userId:'user-1',summary:[summary],records:[{...record,ownerUserId:'other'}]},'user-1'));
 assert.throws(()=>readHistory({userId:'user-1',summary:[{...summary,state:'ACTIVE'}],records:[]},'user-1'));
 assert.throws(()=>readHistory({userId:'other',summary:[],records:[]},'user-1'));
});
test('consent routes are valid return paths and all UI keys exist in TR and EN',()=>{
 assert.equal(isWorkspaceReturnPath('/consents'),true);assert.equal(isWorkspaceReturnPath(`/consents/records/${id}`),true);
 for(const bad of ['/consents/records/x','/consents?x=1',`/consents/records/${id}/edit`,'/consents/types'])assert.equal(isWorkspaceReturnPath(bad),false,bad);
 assert.deepEqual(Object.keys(tr.consents).sort(),Object.keys(en.consents).sort());assert.ok(tr.ui.navConsents&&en.ui.navConsents);
 const sources=['ConsentsWorkspace.tsx','ConsentDocumentPage.tsx'].map(f=>readFileSync(new URL(`../src/features/consents/${f}`,import.meta.url),'utf8')).join('\n');
 const used=[...sources.matchAll(/\bt\("([A-Za-z_]+)"/g)].map(m=>m[1]);
 for(const key of new Set(used))assert.ok(key in tr.consents,`missing consents.${key}`);
 for(const prefix of ['kind_','kindHelp_'])for(const k of ['INFORMATION','EXPLICIT_CONSENT'])assert.ok(tr.consents[prefix+k]);
 for(const s of ['ACTIVE','OUTDATED','WITHDRAWN','SUPERSEDED','NONE'])assert.ok(tr.consents['state_'+s]&&en.consents['state_'+s]);
 for(const e of ['EMPTY','TOO_LONG','TOO_MANY_LINES','CONTROL_CHAR','LINK'])assert.ok(tr.consents['markupError_'+e]&&en.consents['markupError_'+e]);
});
