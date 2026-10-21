import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readAnnouncement,readDocument,readContentPage,validFile,verifyDownload,maxFileBytes} from '../src/features/communication/model.ts';
import {authorizedRequest,ApiError} from '../src/services/authorized-request.ts';
import {problemDetails} from '../src/services/problem.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
import {managementSection} from '../src/features/navigation/routes.ts';
const property='9b962f3e-8812-459f-a42f-45ec6e3c6eba',other='24d39d57-021b-4705-87d3-98a091c95f23';
const a={id:other,propertyId:property,title:'Title',content:'Plain <script> text',audience:'ALL_MEMBERS',status:'DRAFT',version:1,createdAt:'2026-10-10T04:00:00Z',publishedAt:null,category:'GENERAL'};
const pdf=new Blob(['%PDF-1.7 test'],{type:'application/pdf'});const digest=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await pdf.arrayBuffer())),b=>b.toString(16).padStart(2,'0')).join('').toUpperCase();
const d={id:other,propertyId:property,requestId:other,announcementId:null,audience:'MANAGEMENT',fileName:'test.pdf',contentType:'application/pdf',size:pdf.size,sha256:digest,version:1,createdAt:a.createdAt,replayed:false};
test('announcement responses preserve scope and publication history',()=>{
 assert.deepEqual(readAnnouncement(a,property),a);assert.throws(()=>readAnnouncement(a,other));
 for(const v of [{...a,status:'unknown'},{...a,publishedAt:a.createdAt},{...a,status:'PUBLISHED'},{...a,version:0},{...a,title:''},{...a,content:'x'.repeat(10001)},{...a,audience:'TENANT'},{...a,category:'FIRE'},{...a,category:undefined},{...a,category:'general'}])assert.throws(()=>readAnnouncement(v,property));
 assert.equal(readAnnouncement({...a,status:'ARCHIVED',publishedAt:a.createdAt},property).status,'ARCHIVED');
});
test('document metadata does not accept foreign property or unsafe filenames',()=>{
 assert.deepEqual(readDocument(d,property),d);assert.throws(()=>readDocument(d,other));assert.throws(()=>readDocument(d,property,other));
 for(const v of [{...d,fileName:'../private.pdf'},{...d,fileName:'x\r\n.pdf'},{...d,size:maxFileBytes+1},{...d,size:0},{...d,sha256:'private-token'},{...d,contentType:'text/html'},{...d,announcementId:'invalid'}])assert.throws(()=>readDocument(v,property));
 assert.equal(readDocument({...d,announcementId:other},property,other).announcementId,other);
});
test('content pages reject duplicate rows and invalid counters',()=>{
 const page={items:[a],page:1,pageSize:10,totalCount:1};assert.deepEqual(readContentPage(page,v=>readAnnouncement(v,property)),page);
 for(const v of [{...page,items:[a,a],totalCount:2},{...page,pageSize:101},{...page,totalCount:0},{...page,page:0}])assert.throws(()=>readContentPage(v,row=>readAnnouncement(row,property)));
 const categories=['GENERAL','MEETING','DUES','MAINTENANCE','URGENT','OTHER'],counts=categories.map((category,i)=>({category,count:i}));
 assert.deepEqual(readContentPage({...page,categoryCounts:counts},v=>readAnnouncement(v,property)).categoryCounts,counts);
 assert.equal(readContentPage({...page,categoryCounts:null},v=>readAnnouncement(v,property)).categoryCounts,undefined);
 for(const bad of [counts.slice(1),[...counts].reverse(),counts.map(c=>({...c,count:-1})),counts.map(c=>({...c,count:1.5})),{}])assert.throws(()=>readContentPage({...page,categoryCounts:bad},v=>readAnnouncement(v,property)));
});
test('file selection validates supported extension MIME and size',()=>{
 assert.ok(validFile({name:'test.PDF',type:'application/pdf',size:maxFileBytes}));
 for(const v of [{name:'test.pdf',type:'text/html',size:20},{name:'../x.pdf',type:'application/pdf',size:20},{name:'x.pdf',type:'application/pdf',size:maxFileBytes+1},{name:'x.pdf',type:'application/pdf',size:0},{name:'x.svg',type:'image/svg+xml',size:20}])assert.equal(validFile(v),false);
});
test('download checks the actual type size and SHA256 before creating a browser URL',async()=>{
 assert.equal(await verifyDownload(pdf,d),true);assert.equal(await verifyDownload(new Blob(['%PDF-changed'],{type:'application/pdf'}),d),false);assert.equal(await verifyDownload(new Blob(['%PDF-1.7 test'],{type:'text/html'}),d),false);assert.equal(await verifyDownload(pdf,{...d,sha256:'0'.repeat(64)}),false);
});
test('multipart request keeps authorization and leaves the boundary to fetch',async()=>{
 const notices=[];const reporter={error:v=>notices.push(v),warning:v=>notices.push(v)};const form=new FormData();form.set('file',pdf,'test.pdf');
 const result=await authorizedRequest({token:'test',updateToken:async()=>true},'en',reporter,'http://test','/upload',{method:'POST',body:form,headers:{'Content-Type':'application/json','X-Test':'value'}},async(url,options)=>{const h=new Headers(options.headers);assert.equal(h.get('Authorization'),'Bearer test');assert.equal(h.get('Content-Type'),null);assert.equal(h.get('X-Test'),'value');assert.equal(options.body,form);return new Response(JSON.stringify(d),{status:201});});assert.deepEqual(result,d);assert.deepEqual(notices,[]);
});
test('authorized blob downloads retain common errors and abort behavior',async()=>{
 const notices=[];const reporter={error:v=>notices.push(v),warning:v=>notices.push(v)};const identity={token:'test',updateToken:async()=>true};
 const blob=await authorizedRequest(identity,'en',reporter,'http://test','/download',{},async()=>new Response(pdf),'blob');assert.ok(blob instanceof Blob);assert.equal(await verifyDownload(blob,d),true);
 await assert.rejects(authorizedRequest(identity,'en',reporter,'http://test','/download',{},async()=>new Response(JSON.stringify({code:'BF_RESOURCE_NOT_FOUND',message:'private-secret'}),{status:404}),'blob'),e=>e instanceof ApiError&&e.status===404&&!e.message.includes('private-secret'));assert.equal(notices.length,1);
 const controller=new AbortController();controller.abort();await assert.rejects(authorizedRequest(identity,'en',reporter,'http://test','/download',{signal:controller.signal},async()=>{assert.fail('no fetch');},'blob'));assert.equal(notices.length,1);
});
test('communication menus have exact OIDC return routes without tokens',()=>{
 for(const section of ['announcements','documents']){assert.ok(isWorkspaceReturnPath('/'+section));assert.ok(isWorkspaceReturnPath(`/properties/${property}/${section}`));assert.equal(managementSection(`/properties/${property}/${section}`,property),section);assert.equal(isWorkspaceReturnPath(`/properties/${property}/${section}?token=secret`),false);assert.equal(isWorkspaceReturnPath(`/properties/${property}/${section}/other`),false);}
});
test('413 and 503 errors and content validation are translated through the central registry',()=>{
 for(const [status,code] of [[413,'BF_DOCUMENT_TOO_LARGE'],[503,'BF_STORAGE_UNAVAILABLE']]){const error=problemDetails(status,{code,message:'private-secret'},'en');assert.equal(error.code,code);assert.equal(error.severity,'error');assert.ok(!error.message.includes('private-secret'));}
 const fields=problemDetails(400,{code:'BF_VALIDATION_FAILED',fieldIssues:{file:[{code:'BF_DOCUMENT_INVALID'}],content:[{code:'BF_CONTENT_INVALID'}]}},'en').errors;assert.equal(Object.keys(fields).length,2);
});
