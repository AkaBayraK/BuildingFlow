import test from 'node:test';import assert from 'node:assert/strict';
import {readAreaReport,readReservationPage,dateRange,moveDay} from '../../../shared/common-area-reports.ts';
const p='11111111-1111-4111-8111-111111111111',id='22222222-2222-4222-8222-222222222222',from='2026-10-27';
const m={confirmed:2,cancelled:1,openingMinutes:840,closedMinutes:120,availableMinutes:720,bookedMinutes:60,personMinutes:600,capacityMinutes:10080,occupancyPercent:'8.33',capacityUsePercent:'5.95'};
const report={propertyId:p,commonAreaId:id,from,to:from,asOf:'2026-10-27T06:00:00Z',areaVersion:2,capacity:14,opensAt:'08:00',closesAt:'22:00',currentConfiguration:true,paymentSupported:false,measuredDays:1,totals:m,days:[{date:from,metrics:m}]};
test('manager report validates scope, exact period, configuration and no fake payment support',()=>{
 assert.deepEqual(readAreaReport(report,p,id,from,from),report);
 for(const change of [{propertyId:id},{commonAreaId:p},{from:'2026-10-26'},{paymentSupported:true},{currentConfiguration:false},{measuredDays:0},{capacity:13},{days:[]},{days:[{date:'2026-10-28',metrics:m}]}])assert.throws(()=>readAreaReport({...report,...change},p,id,from,from));
});
test('report totals must cover all days and match raw minute denominators and percentages',()=>{
 for(const bad of [{...m,availableMinutes:721},{...m,bookedMinutes:721},{...m,occupancyPercent:'8.34'},{...m,personMinutes:-1},{...m,closedMinutes:null}])assert.throws(()=>readAreaReport({...report,totals:bad,days:[{date:from,metrics:bad}]},p,id,from,from));
 assert.throws(()=>readAreaReport({...report,totals:{...m,confirmed:1}},p,id,from,from));
 const second=moveDay(from,1),total={...m,confirmed:4,cancelled:2,openingMinutes:1680,closedMinutes:240,availableMinutes:1440,bookedMinutes:120,personMinutes:1200,capacityMinutes:20160};assert.equal(readAreaReport({...report,to:second,measuredDays:2,totals:total,days:[{date:from,metrics:m},{date:second,metrics:m}]},p,id,from,second).totals.confirmed,4);
});
test('unknown and closed denominators stay unknown; historical capacity use may exceed 100',()=>{
 const unknown={...m,openingMinutes:null,closedMinutes:null,availableMinutes:null,bookedMinutes:null,personMinutes:null,capacityMinutes:null,occupancyPercent:null,capacityUsePercent:null};
 assert.equal(readAreaReport({...report,opensAt:null,closesAt:null,measuredDays:0,totals:unknown,days:[{date:from,metrics:unknown}]},p,id,from,from).totals.occupancyPercent,null);
 const changed={...m,personMinutes:20160,capacityUsePercent:'200.00'};assert.equal(readAreaReport({...report,totals:changed,days:[{date:from,metrics:changed}]},p,id,from,from).totals.capacityUsePercent,'200.00');
});
test('range rejects impossible dates and over 93 inclusive days; leap and month rollover stay UTC',()=>{
 assert.equal(dateRange('2026-01-01','2026-04-03'),93);assert.throws(()=>dateRange('2026-01-01','2026-04-04'));assert.throws(()=>dateRange('2026-02-30','2026-03-01'));assert.throws(()=>dateRange('2026-02-01','2026-01-01'));assert.equal(moveDay('2028-02-28',1),'2028-02-29');assert.equal(moveDay('2026-10-31',1),'2026-11-01');
});
test('calendar list rejects foreign page, truncated count, outside period and mismatched status',()=>{
 const v={items:[],page:2,pageSize:10,totalCount:10};assert.equal(readReservationPage(v,p,id,2,10,from,from,'').totalCount,10);assert.throws(()=>readReservationPage(v,p,id,1,10,from,from,''));assert.throws(()=>readReservationPage({...v,totalCount:-1},p,id,2,10,from,from,''));
 const r={id,propertyId:p,commonAreaId:id,requestId:p,unitId:null,unitNumber:null,date:from,startsAt:'10:00',endsAt:'11:00',partySize:2,note:null,status:'CONFIRMED',cancelReason:null,cancelledAt:null,version:1,createdAt:report.asOf,modifiedAt:report.asOf,replayed:false};const first={items:[r],page:1,pageSize:10,totalCount:1};assert.equal(readReservationPage(first,p,id,1,10,from,from,'CONFIRMED').items.length,1);assert.throws(()=>readReservationPage(first,p,id,1,10,from,from,'CANCELLED'));assert.throws(()=>readReservationPage({...first,items:[{...r,date:'2026-10-26'}]},p,id,1,10,from,from,''));assert.throws(()=>readReservationPage({...first,items:[]},p,id,1,10,from,from,''));
});
test('percentage ties use exact integer rounding matching the server',()=>{
 const small={...m,confirmed:1,cancelled:0,openingMinutes:32,closedMinutes:0,availableMinutes:32,bookedMinutes:1,personMinutes:1,capacityMinutes:448,occupancyPercent:'3.13',capacityUsePercent:'0.22'};
 assert.equal(readAreaReport({...report,opensAt:'08:00',closesAt:'08:32',totals:small,days:[{date:from,metrics:small}]},p,id,from,from).totals.occupancyPercent,'3.13');
});
