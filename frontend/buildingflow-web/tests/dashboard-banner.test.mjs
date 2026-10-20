import {test} from 'node:test';
import assert from 'node:assert/strict';
import {dashboardAreas,selectedDashboardArea} from '../src/features/dashboard/area-selection.ts';
import {bannerName,latestBanner,bannerFileValid} from '../src/features/dashboard/banner.ts';
const P='77777777-7777-4777-8777-777777777777',Q='88888888-8888-4888-8888-888888888888';
const property=(id=P,isEnabled=true)=>({id,isEnabled,name:'Fixture',city:'City',district:'District',address:'Address',version:1,createdAt:'2026-10-20T09:00:00Z',type:'APARTMENT',floorCount:5,buildingCount:0,unitCount:2,canChangeType:false});
const doc=(patch={})=>({id:P,propertyId:P,requestId:Q,announcementId:null,audience:'ALL_MEMBERS',fileName:bannerName,contentType:'image/jpeg',size:4,sha256:'A'.repeat(64),version:1,createdAt:'2026-10-20T09:00:00Z',replayed:false,...patch});
const page=items=>({items,page:1,pageSize:100,totalCount:items.length});
test('dashboard selection accepts only returned active areas; unknown URLs cannot expand access',()=>{
 const items=[property(P,false),property(Q)];for(const id of [P,Q,'https://evil'])assert.equal(selectedDashboardArea(items,id),Q);assert.equal(selectedDashboardArea([],P),'');
});
test('dashboard chooser reads later pages, rejects duplicate/invalid IDs and aborts before network',async()=>{
 const rows=Array.from({length:100},(_,i)=>property(`12345678-1234-4234-8234-${String(i).padStart(12,'0')}`));let count=0;
 const result=await dashboardAreas(async()=>({items:++count===1?rows:[property(Q)],page:count,pageSize:100,totalCount:101}),new AbortController().signal);assert.equal(result.length,101);assert.equal(result[100].id,Q);
 assert.deepEqual(await dashboardAreas(async()=>page([property(P,false)]),new AbortController().signal),[]);
 for(const response of [page([property('invalid')]),{...page([property()]),page:2},page([property(),property()])])await assert.rejects(dashboardAreas(async()=>response,new AbortController().signal));
 const c=new AbortController();c.abort();await assert.rejects(dashboardAreas(async()=>{assert.fail('network after abort');},c.signal));
});
test('shared banner requires exact standalone ALL_MEMBERS JPEG metadata and latest upload',()=>{
 assert.equal(latestBanner(page([doc(),doc({id:Q,createdAt:'2026-10-20T10:00:00Z'})]),P).id,Q);
 for(const patch of [{audience:'MANAGEMENT'},{announcementId:Q},{fileName:'x'+bannerName},{contentType:'application/pdf'}])assert.equal(latestBanner(page([doc(patch)]),P),null);
 assert.throws(()=>latestBanner(page([doc({propertyId:Q})]),P));assert.throws(()=>latestBanner(page([doc({sha256:'invalid'})]),P));assert.equal(latestBanner(page([]),P),null);
});
test('banner rejects SVG, empty and oversized inputs before image decoding or upload',()=>{
 for(const type of ['image/jpeg','image/png','image/webp'])assert.equal(bannerFileValid({size:100,type}),true);
 for(const f of [{size:0,type:'image/jpeg'},{size:10*1024*1024+1,type:'image/jpeg'},{size:100,type:'image/svg+xml'}])assert.equal(bannerFileValid(f),false);
});

import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
test('dashboard return preserves exactly one validated area filter and rejects extra auth or external query',()=>{
 assert.equal(isWorkspaceReturnPath(`/dashboard?property=${P}`),true);
 for(const v of [`/dashboard?property=00000000-0000-0000-0000-000000000000`,`/dashboard?property=${P}&token=secret`,`/dashboard?property=${P}&property=${Q}`,`/dashboard?property=${P}#x`,`/dashboard?property=${P}\n`,'/dashboard?property=https://outside','//outside/dashboard','/dashboard?property=invalid'])assert.equal(isWorkspaceReturnPath(v),false,v);
});
