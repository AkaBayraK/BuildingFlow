import test from 'node:test';
import assert from 'node:assert/strict';
import {initialContext,contextKey} from '../src/auth/contracts.ts';
import {portfolioGroups} from '../src/person-authorization/model.ts';
const t='11111111-1111-4111-8111-111111111111',site='22222222-2222-4222-8222-222222222222',apt='33333333-3333-4333-8333-333333333333';
const id=n=>`44444444-4444-4444-8444-${String(n).padStart(12,'0')}`;
const ctx=(n,kind,property,name,unit,number)=>{const canonical={id:id(n),kind,tenantId:t,tenantName:'Örnek',propertyId:property,propertyName:name,unitId:unit,unitNumber:number,role:kind==='MANAGEMENT'?'MANAGER':kind,startDate:'2026-01-01',endDate:null,canManagePeople:kind==='MANAGEMENT',canManageFinance:kind==='MANAGEMENT',canManageAssignments:false};return{propertyId:property,propertyName:name,unitId:unit,unitNumber:number,role:canonical.role,canonical};};
const d5=ctx(1,'OWNER',site,'Örnek Sitesi',id(90),'D5'),d12t=ctx(2,'TENANT',site,'Örnek Sitesi',id(91),'D12'),d12r=ctx(3,'RESIDENT',site,'Örnek Sitesi',id(91),'D12'),d3=ctx(4,'OWNER',apt,'Örnek Apartmanı',id(92),'D3'),duty=ctx(5,'MANAGEMENT',apt,'Örnek Apartmanı',null,null);
test('canonical mode opens a unit by itself only when it is remembered or the only context',()=>{
 assert.equal(initialContext([d5,d3],null,true),null);assert.equal(initialContext([d5],null,true),d5);assert.equal(initialContext([d5,d3],contextKey(d3),true),d3);
 assert.equal(initialContext([d5,d3],'gone',true),null);assert.equal(initialContext([d5,d3],null,false),d5);});
test('portfolio groups area then unit, keeps roles of one unit together and duties apart',()=>{
 const g=portfolioGroups([d12r,d5,duty,d3,d12t]);assert.deepEqual(g.areas.map(a=>a.propertyName),['Örnek Apartmanı','Örnek Sitesi']);
 const sitePart=g.areas[1];assert.deepEqual(sitePart.units.map(u=>u.unitNumber),['D5','D12']);assert.deepEqual(sitePart.units[1].contexts.map(c=>c.role),['RESIDENT','TENANT']);
 assert.deepEqual(g.workspaces,[duty]);assert.equal(new Set(g.areas.flatMap(a=>a.units.flatMap(u=>u.contexts.map(contextKey)))).size,4);});
import {unitSummary} from '../src/person-authorization/model.ts';
test('unit summary keeps totals and titles of the selected context only',()=>{const key='t:OWNER:r';const d={contextKey:key,summary:{amount:'10.00',paid:'0.00',balance:'10.00',overdue:'5.00'},rows:{items:[]}};const a={contextKey:key,rows:{items:[{title:'Su kesintisi'},{title:'Toplantı'}]}};
 assert.deepEqual(unitSummary(d,a,key),{balance:'10.00',overdue:'5.00',announcements:['Su kesintisi','Toplantı']});assert.deepEqual(unitSummary(null,null,key),{balance:null,overdue:null,announcements:[]});
 assert.throws(()=>unitSummary({...d,contextKey:'t:OWNER:x'},null,key));assert.throws(()=>unitSummary({...d,summary:{...d.summary,balance:10}},null,key));});
