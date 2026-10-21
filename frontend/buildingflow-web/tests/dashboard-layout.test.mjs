import {test} from 'node:test';
import assert from 'node:assert/strict';
import {DEFAULT_LAYOUT,WIDGET_IDS,mergeLayout,isDefaultLayout,dropWidget,shiftWidget,resizeWidget,setWidgetHidden,readLayout,writeLayout,clearLayout} from '../src/features/dashboard/dashboard-layout.ts';
// Düzenlenebilir dashboard: kişiye özel sıra/genişlik/gizleme, eski veya bozuk kayıtlara dayanıklılık.
const ids=l=>l.map(w=>w.id);
const memory=()=>{const m=new Map();return {getItem:k=>m.get(k)??null,setItem:(k,v)=>m.set(k,String(v)),removeItem:k=>m.delete(k),m};};
test('default layout mirrors the previous fixed dashboard and covers every widget once',()=>{
 assert.deepEqual(ids(DEFAULT_LAYOUT),[...WIDGET_IDS]);
 assert.deepEqual(DEFAULT_LAYOUT.map(w=>w.size),[3,2,1,3,1,2]);
 assert.ok(isDefaultLayout(mergeLayout(null)));
});
test('saved layout merges with defaults: unknown, duplicate and invalid entries are dropped, new widgets appended',()=>{
 const l=mergeLayout([{id:'upcoming',size:3,hidden:true},{id:'ghost',size:1},{id:'upcoming',size:1},{id:'trend',size:9},'x',null]);
 assert.deepEqual(ids(l),['upcoming','trend','kpis','occupancy','recent','collection']);
 assert.deepEqual(l[0],{id:'upcoming',size:3,hidden:true});
 assert.equal(l[1].size,2,'invalid size falls back to default');
 for(const bad of [{},'nope',42])assert.ok(isDefaultLayout(mergeLayout(bad)));
});
test('drag forward takes the target place, drag backward goes before the target',()=>{
 const l=mergeLayout(null);
 assert.deepEqual(ids(dropWidget(l,'kpis','occupancy')),['trend','occupancy','kpis','recent','collection','upcoming']);
 assert.deepEqual(ids(dropWidget(l,'upcoming','trend')),['kpis','upcoming','trend','occupancy','recent','collection']);
 assert.deepEqual(ids(dropWidget(l,'trend','trend')),ids(l));
 assert.deepEqual(ids(dropWidget(l,'trend','ghost')),ids(l));
});
test('keyboard shift skips hidden or unavailable cards and stops at the ends',()=>{
 const l=setWidgetHidden(mergeLayout(null),'occupancy',true),visible=['kpis','trend','collection','upcoming'];
 assert.deepEqual(ids(shiftWidget(l,'trend',1,visible)),['kpis','occupancy','recent','collection','trend','upcoming']);
 assert.deepEqual(ids(shiftWidget(l,'kpis',-1,visible)),ids(l));
 assert.deepEqual(ids(shiftWidget(l,'upcoming',1,visible)),ids(l));
});
test('resize and hide change one card and leave the default check honest',()=>{
 const l=resizeWidget(mergeLayout(null),'trend',3);assert.equal(l.find(w=>w.id==='trend').size,3);assert.equal(isDefaultLayout(l),false);
 assert.ok(isDefaultLayout(resizeWidget(l,'trend',2)));
 assert.equal(setWidgetHidden(mergeLayout(null),'recent',true).find(w=>w.id==='recent').hidden,true);
});
test('layout is stored per account and survives storage failures',()=>{
 const s=memory(),l=dropWidget(mergeLayout(null),'upcoming','kpis');
 assert.ok(writeLayout(s,'u1',l));assert.deepEqual(readLayout(s,'u1'),l);assert.ok(isDefaultLayout(readLayout(s,'u2')));
 clearLayout(s,'u1');assert.ok(isDefaultLayout(readLayout(s,'u1')));
 s.setItem('buildingflow.dashboard-layout.u3','{broken');assert.ok(isDefaultLayout(readLayout(s,'u3')));
 const failing={getItem(){throw new Error('denied');},setItem(){throw new Error('denied');},removeItem(){throw new Error('denied');}};
 assert.ok(isDefaultLayout(readLayout(failing,'u1')));assert.equal(writeLayout(failing,'u1',l),false);assert.doesNotThrow(()=>clearLayout(failing,'u1'));
});
