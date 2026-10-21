import {test} from 'node:test';
import assert from 'node:assert/strict';
import {collectionRatio,readCollections} from '../src/features/dashboard/collection.ts';
const row=(period='2026-10',totalAmount='158400.00',collectedAmount='123600.00',pendingAmount='22400.00',overdueAmount='12400.00')=>({period,totalAmount,collectedAmount,pendingAmount,overdueAmount});
test('collection ratio: same-cohort sum, empty is unknown, zero collected is zero, full collected is 100',()=>{
 assert.equal(collectionRatio(row()).percent,78);
 assert.deepEqual(collectionRatio(row('2026-10','0.00','0.00','0.00','0.00')),{ratio:0,percent:null});
 assert.equal(collectionRatio(row('2026-10','1.00','0.00','1.00','0.00')).percent,0);
 assert.equal(collectionRatio(row('2026-10','1.00','1.00','0.00','0.00')).percent,100);
 assert.equal(collectionRatio(row('2026-10','999999999999999999.99','499999999999999999.99','500000000000000000.00','0.00')).percent,50);
});
test('collection DTO: prior/current order, year boundary, valid cents and reconciled totals; legacy unavailable',()=>{
 const rows=[row('2026-09'),row()];assert.equal(readCollections(rows,'2026-10'),rows);
 assert.equal(readCollections([row('2025-12'),row('2026-01')],'2026-01').length,2);
 assert.equal(readCollections(undefined,'2026-10'),null);assert.equal(readCollections(null,'2026-10'),null);
 for(const value of [[],[row()],rows.toReversed(),[row('2026-09'),{...row(),totalAmount:'0.00'}],[row('2026-09'),{...row(),collectedAmount:'-1.00'}],[row('2026-09'),{...row(),pendingAmount:22400}],[row('2026-09'),{...row(),overdueAmount:'12400.001'}]])assert.throws(()=>readCollections(value,'2026-10'));
});
