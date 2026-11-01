import test from 'node:test';import assert from 'node:assert/strict';import {readReport} from '../src/features/reports/model.ts';
import {formatConsumption} from '../src/lib/format.ts';
const propertyId='9b962f3e-8812-459f-a42f-45ec6e3c6eba';
const report={propertyId,period:'2026-09',currency:'TRY',unitCount:3,activeUnitCount:2,occupiedUnitCount:1,vacantUnitCount:1,assessedAmount:'100.00',outstandingAmount:'75.00',receiptsAmount:'0.00',expensesAmount:'12.35',consumption:['WATER','ELECTRICITY','GAS'].map(kind=>({kind,measurementUnit:kind==='ELECTRICITY'?'kWh':'m³',readingCount:0,baselineCount:0,consumption:'0.000'}))};
test('report DTO verifies scope, period and occupancy counters',()=>{assert.deepEqual(readReport(report,propertyId,'2026-09'),report);for(const r of [{...report,propertyId:'other'},{...report,period:'2026-10'},{...report,occupiedUnitCount:2},{...report,outstandingAmount:'100.01'},{...report,receiptsAmount:0}])assert.throws(()=>readReport(r,propertyId,'2026-09'));});
test('report preserves unknown baseline and separate units of measurement',()=>{assert.throws(()=>readReport({...report,consumption:[report.consumption[0],report.consumption[0],report.consumption[2]]},propertyId,'2026-09'));assert.throws(()=>readReport({...report,consumption:report.consumption.map(r=>({...r,baselineCount:1}))},propertyId,'2026-09'));});
test('aggregate quantities format in TR/EN without losing three decimals or large integers',()=>{
 assert.equal(formatConsumption('0.000','tr'),'0,000');
 assert.equal(formatConsumption('1234.567','tr'),'1.234,567');
 assert.equal(formatConsumption('1234.567','en'),'1,234.567');
 assert.equal(formatConsumption('9007199254740993.001','en'),'9,007,199,254,740,993.001');
 for(const value of ['-1.000','1.00','1.0001','1e3','1,000.000','01.000'])assert.throws(()=>formatConsumption(value));
});
test('physical totals preserve distinct kinds, unknown baselines and independent expense amount',()=>{
 const source={...report,expensesAmount:'12.35',consumption:[{kind:'WATER',measurementUnit:'m³',readingCount:2,baselineCount:1,consumption:'1234.567'},{kind:'ELECTRICITY',measurementUnit:'kWh',readingCount:1,baselineCount:0,consumption:'1000000000.001'},{kind:'GAS',measurementUnit:'m³',readingCount:1,baselineCount:1,consumption:'0.000'}]};
 const result=readReport(source,propertyId,'2026-09');
 assert.deepEqual(result.consumption,source.consumption);
 assert.equal(result.expensesAmount,'12.35');
 assert.equal(formatConsumption(result.consumption[1].consumption,'tr'),'1.000.000.000,001');
});
test('yearly report: twelve months that add up, category totals, quick periods',async()=>{
 const {resolvePeriod,validPeriod,subtractMoney}=await import('../src/features/reports/model.ts');
 const months=Array.from({length:12},(_,i)=>({period:`2026-${String(i+1).padStart(2,'0')}`,assessedAmount:i===8?'100.00':'0.00',outstandingAmount:i===8?'75.00':'0.00',receiptsAmount:i===9?'25.00':'0.00',expensesAmount:i===8?'12.35':'0.00'}));
 const categories=['MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER'].map(category=>({category,amount:category==='ENERGY'?'12.35':'0.00'}));
 const year={...report,period:'2026',receiptsAmount:'25.00',months,expenseCategories:categories};
 assert.equal(readReport(year,propertyId,'2026').months.length,12);
 assert.equal(readReport({...report,expenseCategories:categories},propertyId,'2026-09').expenseCategories.length,8);
 assert.equal(readReport({...report,period:'2026'},propertyId,'2026').months,undefined,'older API without the breakdown');
 for(const bad of [{...year,months:months.slice(1)},{...year,months:months.map((m,i)=>i===0?{...m,receiptsAmount:'1.00'}:m)},{...year,months:months.toReversed()},{...year,expenseCategories:categories.slice(1)},{...year,expenseCategories:categories.map(c=>({...c,amount:'1.00'}))}])assert.throws(()=>readReport(bad,propertyId,'2026'));
 assert.throws(()=>readReport({...report,months},propertyId,'2026-09'),'monthly report has no breakdown');
 assert.equal(resolvePeriod('thisMonth','2026-01-15','',''),'2026-01');assert.equal(resolvePeriod('previousMonth','2026-01-15','',''),'2025-12');assert.equal(resolvePeriod('thisYear','2026-01-15','',''),'2026');assert.equal(resolvePeriod('year','2026-01-15','','2024'),'2024');assert.equal(resolvePeriod('month','2026-01-15','2025-03',''),'2025-03');
 for(const p of ['2026','2026-11','2000-01'])assert.ok(validPeriod(p),p);for(const p of ['1999','9999','26','2026-13',''])assert.ok(!validPeriod(p),p);
 assert.equal(subtractMoney('25.00','12.35'),'12.65');assert.equal(subtractMoney('0.00','12.35'),'-12.35');assert.equal(subtractMoney('0.05','0.05'),'0.00');
});
