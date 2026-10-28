import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {closureBody,readClosure,istanbulInstant,localInput,istanbulDayWindow,normalizePlate,readPolicy,readFacilities,readSpace,readVehicle,readGuest,readPage,readPlates,readResidents,policyBody,vehicleBody,guestBody,allocationBody,spaceState,spaceTypes,vehicleStatuses,guestStatuses,vehicleTypes} from '../src/features/parking/model.ts';
import {spaceStyle,vehicleStatusStyle,guestStatusStyle,vehicleTypeIcon,tabIcon,parkingTabs} from '../src/features/parking/parking-style.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
import {managementSection} from '../src/features/navigation/routes.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';

const P='11111111-1111-4111-8111-111111111111',U='22222222-2222-4222-8222-222222222222',Q='33333333-3333-4333-8333-333333333333',R='44444444-4444-4444-8444-444444444444';
const vehicle={id:Q,personId:R,personName:'Ayşe Yılmaz',unitId:U,unitNumber:'1',plate:'34ABC123',type:'CAR',brand:'BMW',model:null,color:null,status:'PENDING',reviewedAt:null,reviewReason:null,version:1,createdAt:'2026-10-27T09:00:00Z'};

test('plate normalization matches the backend rule',()=>{
 assert.equal(normalizePlate(' 34 abc-123 '),'34ABC123');assert.equal(normalizePlate('b.ab.1234'),'BAB1234');
 for(const bad of ['','A','34ÇAB12','34ABC1234567X','34_ABC'])assert.equal(normalizePlate(bad),null,bad);
});

test('DTO readers reject foreign scope, unknown enums and broken shapes',()=>{
 assert.deepEqual(readVehicle(vehicle),vehicle);
 for(const patch of [{status:'DELETED'},{plate:'34 abc'},{version:0},{personName:''},{id:'x'}])assert.throws(()=>readVehicle({...vehicle,...patch}));
 const policy={propertyId:P,configured:true,maxVehiclesPerUnit:2,maxVehiclesPerPerson:2,maxSpacesPerUnit:1,guestMaxHours:24,retentionDays:30,version:1};
 assert.equal(readPolicy(policy,P).guestMaxHours,24);assert.throws(()=>readPolicy(policy,U));assert.throws(()=>readPolicy({...policy,maxVehiclesPerUnit:21},P));
 assert.equal(readPolicy({...policy,configured:false,maxVehiclesPerUnit:0,maxVehiclesPerPerson:0,maxSpacesPerUnit:0,guestMaxHours:0,version:0},P).configured,false);
 const facility={id:Q,buildingId:null,buildingName:null,name:'Kapalı',spaceCount:2,levels:[{id:R,facilityId:Q,name:'B1',sortOrder:-1,version:1}],version:1};
 assert.equal(readFacilities([facility])[0].levels[0].sortOrder,-1);assert.throws(()=>readFacilities([{...facility,levels:[{...facility.levels[0],facilityId:U}]}]));assert.throws(()=>readFacilities([facility,facility]));
 const space={id:R,facilityId:Q,levelId:null,code:'A01',type:'STANDARD',allocatedUnitId:U,allocatedUnitNumber:null,version:1};
 assert.throws(()=>readSpace(space),'allocation id and number go together');
 assert.equal(readPage({items:[vehicle],page:1,pageSize:20,totalCount:1},readVehicle).items.length,1);
 assert.throws(()=>readPage({items:[vehicle,vehicle],page:1,pageSize:20,totalCount:2},readVehicle));
 assert.throws(()=>readPage({items:[vehicle],page:1,pageSize:101,totalCount:1},readVehicle));
 assert.throws(()=>readGuest({id:Q,hostUnitId:U,unitNumber:'1',spaceId:null,spaceCode:null,plate:'34TST100',guestName:null,note:null,startsAt:'x',endsAt:'2026-10-27T10:00:00Z',status:'PLANNED',arrivedAt:null,departedAt:null,version:1}));
 assert.throws(()=>readResidents([{personId:R,name:'A',relation:'GUEST'}]));
});

test('the guard plate lookup carries no person fields',()=>{
 const rows=readPlates([{plate:'34ABC123',unitNumber:'1',buildingName:'A',type:'CAR'}]);assert.deepEqual(Object.keys(rows[0]).sort(),['buildingName','plate','type','unitNumber']);
 assert.throws(()=>readPlates([{plate:'34abc',unitNumber:'1',buildingName:null,type:'CAR'}]));
});

test('request bodies whitelist fields, normalize plates and keep the request id',()=>{
 assert.deepEqual(policyBody({maxVehiclesPerUnit:'2',maxVehiclesPerPerson:'3',maxSpacesPerUnit:'0',guestMaxHours:'24',retentionDays:'30'},4),{maxVehiclesPerUnit:2,maxVehiclesPerPerson:3,maxSpacesPerUnit:0,guestMaxHours:24,retentionDays:30,version:4});
 for(const patch of [{maxVehiclesPerUnit:'0'},{maxSpacesPerUnit:'21'},{guestMaxHours:'169'},{retentionDays:'366'},{retentionDays:'1.5'},{guestMaxHours:''}])
  assert.equal(policyBody({maxVehiclesPerUnit:'2',maxVehiclesPerPerson:'2',maxSpacesPerUnit:'1',guestMaxHours:'24',retentionDays:'30',...patch},1),null,JSON.stringify(patch));
 const form={personId:R,unitId:U,plate:'34 abc 123',type:'CAR',brand:' BMW ',model:'',color:'Siyah'};
 assert.deepEqual(vehicleBody(form,Q),{requestId:Q,personId:R,unitId:U,plate:'34ABC123',type:'CAR',brand:'BMW',model:null,color:'Siyah'});
 assert.equal(vehicleBody({...form,personId:''},Q),null);assert.equal(vehicleBody({...form,brand:'x'.repeat(51)},Q),null);
 const guest={hostUnitId:U,plate:'34 tst 100',guestName:'',startsAt:'2026-10-27T10:00',endsAt:'2026-10-27T14:00',spaceId:'',note:''};
 const body=guestBody(guest,Q,4);assert.equal(body.plate,'34TST100');assert.equal(body.spaceId,null);assert.equal(body.guestName,null);assert.equal(body.requestId,Q);
 assert.equal(guestBody({...guest,endsAt:'2026-10-27T14:01'},Q,4),null,'exceeds policy hours');assert.equal(guestBody({...guest,endsAt:guest.startsAt},Q,4),null);
 assert.deepEqual(allocationBody({spaceId:Q,unitId:U,vehicleId:'',startDate:'2026-11-01',endDate:''}),{spaceId:Q,unitId:U,vehicleId:null,startDate:'2026-11-01',endDate:null});
 assert.equal(allocationBody({spaceId:Q,unitId:U,vehicleId:'',startDate:'2026-11-02',endDate:'2026-11-01'}),null);
});

test('every enum has one icon/tone and the map never relies on colour alone',()=>{
 for(const v of spaceTypes)assert.ok(spaceStyle[v].icon&&spaceStyle[v].tone);for(const v of vehicleStatuses)assert.ok(vehicleStatusStyle[v].icon);
 for(const v of guestStatuses)assert.ok(guestStatusStyle[v].icon);for(const v of vehicleTypes)assert.ok(vehicleTypeIcon[v]);for(const v of parkingTabs)assert.ok(tabIcon[v]);
 const base={id:Q,facilityId:Q,levelId:null,code:'A',allocatedUnitId:null,allocatedUnitNumber:null,version:1};
 assert.equal(spaceState({...base,type:'VISITOR'}),'visitor');assert.equal(spaceState({...base,type:'STANDARD',allocatedUnitId:U,allocatedUnitNumber:'1'}),'allocated');assert.equal(spaceState({...base,type:'STANDARD'}),'free');
 const map=readFileSync(new URL('../src/features/parking/SpacesTab.tsx',import.meta.url),'utf8');assert.match(map,/aria-label=\{label\}/);
});

test('parking menu is a property module for managers and guards only; routes are whitelisted',()=>{
 for(const [role,visible] of [['SUPER_ADMIN',true],['SITE_ADMIN',true],['MANAGER',true],['SECURITY',true],['ACCOUNTANT',false]]){
  const model=createNavigation({roles:[role],sessionContexts:false,centralPeople:false,propertyId:P,property:{id:P,type:'SITE'}},`/properties/${P}/parking`);
  const item=model.groups.flatMap(g=>g.items).find(i=>i.id==='parking');assert.equal(!!item,visible,role);
  if(item){assert.equal(item.href,`/properties/${P}/parking`);assert.equal(item.active,true);assert.equal(item.disabled,false);assert.equal(item.icon,'Car');}
 }
 assert.equal(managementSection(`/properties/${P}/parking`,P),'parking');
 assert.ok(isWorkspaceReturnPath('/parking'));assert.ok(isWorkspaceReturnPath(`/properties/${P}/parking`));
 assert.equal(isWorkspaceReturnPath(`/properties/${P}/parking?plate=34ABC123`),false);assert.equal(isWorkspaceReturnPath(`/properties/${P}/parking/x`),false);
});

test('TR/EN parking dictionaries are complete and differ',()=>{
 const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url))).parking,en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url))).parking;
 assert.deepEqual(Object.keys(tr).sort(),Object.keys(en).sort());
 const sources=['ParkingWorkspace','VehiclesTab','SpacesTab','AllocationsTab','GuestsTab','ClosuresTab','RulesTab','UnitPicker'].map(f=>readFileSync(new URL(`../src/features/parking/${f}.tsx`,import.meta.url),'utf8')).join('\n');
 for(const [,key] of sources.matchAll(/\bt\("([A-Za-z_]+)"/g))assert.ok(key in tr,key);
 for(const prefix of ['tab_','space_','vehicle_','guest_','type_','state_','relation_','window_','policy_','policyHelp_'])assert.ok(Object.keys(tr).some(k=>k.startsWith(prefix)),prefix);
 for(const v of vehicleStatuses)assert.ok(tr[`vehicle_${v}`]&&en[`vehicle_${v}`]);for(const v of guestStatuses)assert.ok(tr[`guest_${v}`]);for(const v of spaceTypes)assert.ok(tr[`space_${v}`]);
 assert.notEqual(tr.title,en.title);
});


test('parking operations use Istanbul time independently of the host timezone',()=>{
 assert.equal(new Date(istanbulInstant('2026-10-27T10:00')).toISOString(),'2026-10-27T07:00:00.000Z');
 assert.equal(localInput(new Date('2026-10-27T23:30:00Z')),'2026-10-28T02:30');
 assert.deepEqual(istanbulDayWindow(new Date('2026-10-27T23:30:00Z')),{start:'2026-10-27T21:00:00.000Z',end:'2026-10-28T21:00:00.000Z'});
 assert.ok(Number.isNaN(istanbulInstant('2026-02-30T10:00')));
 assert.ok(Number.isNaN(istanbulInstant('2026-10-27T24:00')));
 const body=guestBody({hostUnitId:U,plate:'34ABC1',guestName:'',startsAt:'2026-10-27T10:00',endsAt:'2026-10-27T11:00',spaceId:'',note:''},Q,1);
 assert.equal(body.startsAt,'2026-10-27T07:00:00.000Z');
});

test('maintenance contracts enforce bounds, cancellation history and map priority',()=>{
 const closure={id:Q,spaceId:R,spaceCode:'A1',startsAt:'2026-10-27T07:00:00Z',endsAt:'2026-10-27T08:00:00Z',reason:'Repair',cancelledAt:null,cancellationReason:null,version:1};
 assert.deepEqual(readClosure(closure),closure);
 for(const patch of [{endsAt:closure.startsAt},{reason:''},{cancelledAt:closure.startsAt},{version:0}])assert.throws(()=>readClosure({...closure,...patch}));
 assert.deepEqual(closureBody({spaceId:R,startsAt:'2026-10-27T10:00',endsAt:'2026-10-27T11:00',reason:' Repair '},Q),{requestId:Q,spaceId:R,startsAt:'2026-10-27T07:00:00.000Z',endsAt:'2026-10-27T08:00:00.000Z',reason:'Repair'});
 assert.equal(closureBody({spaceId:R,startsAt:'2026-10-27T10:00',endsAt:'2026-10-27T10:00',reason:'Repair'},Q),null);
 assert.equal(spaceState({isClosed:true,type:'STANDARD',allocatedUnitId:U}),'closed');
});


test('report validates scope, complete ordered days and nonnegative numeric CSV cells',async()=>{
 const {readReport,reportCsv}=await import('../src/features/parking/report-model.ts');
 const raw={propertyId:P,from:'2026-10-27',through:'2026-10-27',asOf:'2026-10-27T09:00:00Z',capacity:20,allocatedSpaces:10,reservedGuestSpaces:2,manualGuestsInside:1,overdueGuests:1,scheduledOperationsEnabled:false,hardwareEnabled:false,days:[{day:'2026-10-27',entries:2,exits:1,reservations:3}]};
 const read=v=>readReport(v,P,raw.from,raw.through);const value=read(raw);
 assert.equal(reportCsv(value),'\ufeffday,manual_entries,manual_exits,reservations_started\r\n2026-10-27,2,1,3\r\n');
 for(const patch of [{propertyId:U},{hardwareEnabled:true},{capacity:-1},{days:[]},{days:[{...raw.days[0],day:'2026-10-26'}]},{days:[{...raw.days[0],entries:'=HYPERLINK(1)'}]}])assert.throws(()=>read({...raw,...patch}));
 assert.throws(()=>reportCsv({...value,days:[{...value.days[0],entries:-1}]}));
});
