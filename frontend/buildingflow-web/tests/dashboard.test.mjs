import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {readDashboard,share,TREND_MONTHS} from '../src/features/dashboard/model.ts';
import {pickSlogan,SLOGAN_COUNT} from '../src/features/dashboard/slogans.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
// İŞ20 Dashboard: response contract, rotating slogan/background choice, menu order and route.
const P='77777777-7777-4777-8777-777777777777';
const valid=()=>({propertyId:P,today:'2026-10-17',currency:'TRY',unitCount:3,occupiedUnitCount:1,vacantUnitCount:2,receiptsThisMonth:'25.00',overdueAmount:'125.00',overdueChargeCount:2,upcomingAmount:'100.00',upcomingChargeCount:2,requests:{recorded:1,inProgress:1},
 trend:['2026-05','2026-06','2026-07','2026-08','2026-09','2026-10'].map((period,i)=>({period,receipts:i===5?'25.00':'0.00',expenses:'0.00'})),
 unitTypes:[['APARTMENT',2],['OFFICE',0],['SHOP',1],['VILLA',0],['WAREHOUSE',0],['WORKSHOP',0]].map(([type,count])=>({type,count})),
 upcomingDues:[{dueDate:'2026-10-22',chargeCount:2,amount:'100.00'},{dueDate:'2026-12-01',chargeCount:1,amount:'80.00'}],maintenanceAvailable:false});
test('dashboard summary is accepted only when counts, money, months and lists agree',()=>{
 assert.equal(readDashboard(valid(),P).unitCount,3);assert.equal(TREND_MONTHS,6);
 assert.equal(readDashboard({...valid(),requests:null},P).requests,null);
 const broken=[{propertyId:'x'},{occupiedUnitCount:2},{overdueAmount:'1.5'},{overdueChargeCount:0},{requests:{recorded:1}},{receiptsThisMonth:'30.00'},{currency:'EUR'},{maintenanceAvailable:'no'}];
 for(const patch of broken)assert.throws(()=>readDashboard({...valid(),...patch},P),JSON.stringify(patch));
 const v=valid();v.trend=v.trend.slice(1);assert.throws(()=>readDashboard(v,P));
 const w=valid();w.trend[0].period='2026-04';assert.throws(()=>readDashboard(w,P));
 const u=valid();u.unitTypes[0].count=5;assert.throws(()=>readDashboard(u,P));
 const d=valid();d.upcomingDues.reverse();assert.throws(()=>readDashboard(d,P));
 const e=valid();e.upcomingDues[0].dueDate='2026-10-01';assert.throws(()=>readDashboard(e,P));
 assert.equal(share(208,240),87);assert.equal(share(1,0),0);
});
test('one slogan and one background per page load, always in range',()=>{
 assert.equal(SLOGAN_COUNT,8);assert.equal(pickSlogan(0),1);assert.equal(pickSlogan(.999999),8);assert.equal(pickSlogan(NaN),1);assert.equal(pickSlogan(1),1);
 const tr=JSON.parse(readFileSync('messages/tr.json','utf8')),en=JSON.parse(readFileSync('messages/en.json','utf8'));
 for(let i=1;i<=SLOGAN_COUNT;i++){assert.ok(tr.ui[`shellSlogan${i}`],`tr ${i}`);assert.ok(en.ui[`shellSlogan${i}`],`en ${i}`);}
 assert.deepEqual(Object.keys(tr.dashboard).sort(),Object.keys(en.dashboard).sort());
 const scenes=readFileSync('src/features/dashboard/scenes.tsx','utf8');assert.equal((scenes.match(/\{id:"/g)||[]).length,6);assert.doesNotMatch(scenes,/<image|href=|url\(http/,'scenes are drawn, not loaded');
});
test('menu: Dashboard first, Genel bakış last, no sidebar footer text; slogan sits in the top bar',()=>{
 const s=readFileSync('src/components/WorkspaceShell.tsx','utf8');
 const model=createNavigation({roles:['SUPER_ADMIN'],sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/dashboard');
 assert.equal(model.groups[0].items[0].href,'/dashboard');assert.equal(model.home.href,'/');
 assert.match(s,/model.groups.map/);assert.match(s,/sidebar-end[\s\S]*home\.label/);
 assert.doesNotMatch(s,/sidebar-footer|shellTagline|v1\.0\.0/);assert.match(s,/<kbd>⌘ K<\/kbd><\/button><p className="workspace-slogan"/);
 assert.match(s,/className="brand" href="\/dashboard"/);assert.equal(model.shortcuts[0].href,'/dashboard');
 assert.equal(isWorkspaceReturnPath('/dashboard'),true);assert.equal(isWorkspaceReturnPath('/dashboard/x'),false);
});
