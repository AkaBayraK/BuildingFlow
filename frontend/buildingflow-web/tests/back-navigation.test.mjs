import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {propertySubpageBack} from '../src/features/navigation/back-target.ts';
const p='9b962f3e-8812-459f-a42f-45ec6e3c6eba',x='24d39d57-021b-4705-87d3-98a091c95f23';
test('property module screens return to the management area',()=>{
 for(const s of ['units','buildings','people','dues','payments','expenses','announcements','documents','surveys','meters','maintenance','requests','website','reports','sms'])assert.deepEqual(propertySubpageBack(`/properties/${p}/${s}`),{href:`/properties/${p}`,label:'backToProperty2'},s);
});
test('person detail returns to the people list',()=>{
 assert.deepEqual(propertySubpageBack(`/properties/${p}/people/${x}`),{href:`/properties/${p}/people`,label:'backToPeople'});
});
test('screens with their own back control get no second button',()=>{
 for(const path of [`/properties/${p}`,`/properties/${p}/`,`/properties/${p}/buildings/${x}`,`/properties/${p}/buildings/${x}/units`,'/properties','/dues'])assert.equal(propertySubpageBack(path),null,path);
});
test('back controls share the icon BackButton and its translations',()=>{
 const shared=readFileSync('src/components/navigation/BackButton.tsx','utf8');
 assert.match(shared,/<ArrowLeft aria-hidden="true"\/>/);
 for(const f of ['features/navigation/PropertySubpageBack','features/person-center/CenterDetail','features/properties/PropertyDetail','features/hierarchy/BuildingDetail','features/consents/ConsentDocumentPage','features/session-workspaces/SessionWorkspaces','features/navigation/ManagementPage'])assert.match(readFileSync(`src/${f}.tsx`,'utf8'),/<BackButton\b[^>]*label=\{/,f);
 const center=readFileSync('src/features/person-center/CenterDetail.tsx','utf8');
 assert.doesNotMatch(center,/variant="link"><Link href=\{`\/people\/\$\{tenant\}`\}>/,'people link is no longer a bare text link');
 assert.match(readFileSync('src/app/(workspace)/properties/[id]/layout.tsx','utf8'),/<PropertySubpageBack\/>/);
 const tr=JSON.parse(readFileSync('messages/tr.json','utf8')),en=JSON.parse(readFileSync('messages/en.json','utf8'));
 for(const k of ['backToProperty2','backToPeople'])assert.ok(tr.ui[k]&&en.ui[k],k);
 assert.ok(tr.personCenter.back&&en.personCenter.back);
});
