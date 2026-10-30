import test from 'node:test';import assert from 'node:assert/strict';
import {readAlertPage,readPerformance,alertsCsv,performanceCsv} from '../src/features/suppliers/insights-model.ts';
const a='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',b='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',c='cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const alert={kind:'CONTRACT_EXPIRY',supplierId:a,supplierName:'Lift Co',propertyId:b,propertyName:'Site',recordId:c,title:'Annual',date:'2027-01-10',days:10};
const preference={contractExpiry:true,workOrderOverdue:true,maintenanceDue:true,version:0};
const page=(items,pref=preference)=>({items,page:1,pageSize:20,totalCount:items.length,truncated:false,preference:pref});
const row={supplierId:a,supplierName:'Lift Co',completedWorkOrders:2,averageCompletionHours:'5.5',slaSampleCount:1,slaOnTimeCount:1,maintenanceSampleCount:2,maintenanceOnTimeCount:1,onTimeRate:'66.7',invoiceCount:1,invoicedTotal:'1200.00',score:'66.7',scoreSampleCount:3};
const report=items=>({from:'2026-08-01',to:'2026-10-30',propertyId:null,minimumSample:3,items,page:1,pageSize:20,totalCount:items.length});
test('alerts are validated for timing and never shown against the saved preference',()=>{
 assert.equal(readAlertPage(page([alert])).items[0].id,'CONTRACT_EXPIRY'+c);
 for(const patch of [{days:-1},{kind:'INVOICE'},{supplierId:'x'},{date:'2027-02-30'},{title:' '}])assert.throws(()=>readAlertPage(page([{...alert,...patch}])));
 assert.throws(()=>readAlertPage(page([{...alert,kind:'MAINTENANCE_OVERDUE',days:0}])));assert.equal(readAlertPage(page([{...alert,kind:'MAINTENANCE_OVERDUE',days:-3}])).items.length,1);
 assert.throws(()=>readAlertPage(page([alert],{...preference,contractExpiry:false})));
 assert.throws(()=>readAlertPage({...page([alert]),truncated:'no'}));
});
test('performance scores require the minimum sample and counts must be consistent',()=>{
 assert.equal(readPerformance(report([row])).items[0].score,'66.7');
 assert.equal(readPerformance(report([{...row,maintenanceSampleCount:1,maintenanceOnTimeCount:1,scoreSampleCount:2,onTimeRate:'100.0',score:null}])).items[0].score,null);
 for(const patch of [{maintenanceSampleCount:0,scoreSampleCount:1,onTimeRate:'100.0',score:'100.0'},{slaOnTimeCount:2},{scoreSampleCount:4},{onTimeRate:'101.0',score:'101.0'},{invoicedTotal:'12'},{score:'70.0'}])assert.throws(()=>readPerformance(report([{...row,...patch}])));
 assert.throws(()=>readPerformance({...report([row]),to:'2026-07-01'}));
});
test('CSV exports neutralise formula cells and keep insufficient samples explicit',()=>{
 const csv=alertsCsv([{...alert,title:'=HYPERLINK("http://x")',supplierName:'@cmd'}],{kind:'Tür',supplier:'Tedarikçi',property:'Alan',title:'Başlık',date:'Tarih',days:'Gün',kinds:{CONTRACT_EXPIRY:'Sözleşme',WORK_ORDER_OVERDUE:'İş',MAINTENANCE_OVERDUE:'Bakım',MAINTENANCE_DUE:'Bakım yakın'}});
 assert.ok(csv.startsWith('﻿'));assert.match(csv,/'=HYPERLINK/);assert.match(csv,/'@cmd/);assert.doesNotMatch(csv,/;=HYPERLINK/);
 const perf=performanceCsv(report([{...row,score:null,scoreSampleCount:2,maintenanceSampleCount:1,maintenanceOnTimeCount:1,onTimeRate:'100.0',supplierName:'-2+3'}]),{supplier:'S',completed:'C',hours:'H',sla:'SLA',maintenance:'M',rate:'R',invoices:'I',total:'T',score:'P',sample:'N',insufficient:'Yetersiz örnek'});
 assert.match(perf,/Yetersiz örnek/);assert.match(perf,/'-2\+3/);
});
