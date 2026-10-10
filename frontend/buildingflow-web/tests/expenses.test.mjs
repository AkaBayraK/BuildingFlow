import {test} from 'node:test';
import assert from 'node:assert/strict';
import {expenseCategories,financeCents,normalizeFinanceAmount,readExpense,readExpensePage,readBudget,readFinanceSummary} from '../src/features/expenses/model.ts';
import {formatDecimalCurrency} from '../src/lib/format.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
import {managementSection} from '../src/features/navigation/routes.ts';
import {problemDetails} from '../src/services/problem.ts';
const property='9b962f3e-8812-459f-a42f-45ec6e3c6eba',other='24d39d57-021b-4705-87d3-98a091c95f23';
const expense={id:property,propertyId:property,requestId:other,category:'ENERGY',amount:'12.35',currency:'TRY',occurredDate:'2026-10-01',description:'Electricity',documentReference:null,status:'RECORDED',version:1,createdAt:'2026-10-01T10:00:00Z',cancellationReason:null,cancelledAt:null,replayed:false};
const budget={id:property,propertyId:property,period:'2026-10',category:'ENERGY',amount:'0.00',currency:'TRY',version:2,revisions:[{id:property,amount:'0.00',sequence:2,explanation:'No budget',createdAt:expense.createdAt},{id:other,amount:'12.35',sequence:1,explanation:'Original',createdAt:expense.createdAt}],replayed:true,appliedRevision:1};
const summary={propertyId:property,period:'2026-10',currency:'TRY',incomeAmount:'3.10',expenseAmount:'12.35',budgetAmount:'20.00',netAmount:'-9.25',budgetVariance:'7.65',categories:expenseCategories.map(category=>({category,expenseAmount:category==='ENERGY'?'12.35':'0.00',budgetAmount:category==='ENERGY'?'20.00':'0.00',variance:category==='ENERGY'?'7.65':'0.00',hasBudget:category==='ENERGY',budgetId:category==='ENERGY'?property:null,budgetVersion:category==='ENERGY'?1:null}))};
test('expense and budget money normalization preserves cents and explicit zero plans',()=>{
 assert.equal(normalizeFinanceAmount('0012,35'),'12.35');assert.equal(normalizeFinanceAmount('0'),null);assert.equal(normalizeFinanceAmount('0',true),'0.00');assert.equal(normalizeFinanceAmount('99999999.99'),'99999999.99');
 for(const v of ['-1','1e2','1.001','1,234.50','100000000',''])assert.equal(normalizeFinanceAmount(v,true),null);
 assert.equal(financeCents('-0.35'),-BigInt(35));assert.equal(financeCents('0.00'),BigInt(0));assert.throws(()=>financeCents('-0.00'));
});
test('currency display preserves large totals and negative values below one unit',()=>{
 assert.match(formatDecimalCurrency('999999999999999999.99','en'),/999,999,999,999,999,999\.99/);
 assert.match(formatDecimalCurrency('999999999999999999.99','tr'),/999\.999\.999\.999\.999\.999,99/);
 assert.match(formatDecimalCurrency('-0.35','en'),/-.*0\.35/);assert.match(formatDecimalCurrency('-0.35','tr'),/-.*0,35/);
});
test('expense DTOs enforce scope, real calendar and immutable cancellation history',()=>{
 assert.deepEqual(readExpense(expense,property),expense);assert.equal(readExpense({...expense,status:'CANCELLED',cancellationReason:'Correction',cancelledAt:expense.createdAt},property).amount,'12.35');
 for(const value of [{...expense,propertyId:other},{...expense,category:'0'},{...expense,amount:'0.00'},{...expense,amount:12.35},{...expense,occurredDate:'2026-02-30'},{...expense,version:0},{...expense,status:'CANCELLED'},{...expense,cancellationReason:'hidden'}])assert.throws(()=>readExpense(value,property));
});
test('filtered expense summaries cover every result rather than just the visible page',()=>{
 const page={items:[expense],page:1,pageSize:1,totalCount:3,summary:{recordedAmount:'36.00',cancelledAmount:'1.00',recordedCount:2,cancelledCount:1,currency:'TRY'}};assert.deepEqual(readExpensePage(page,property),page);
 assert.throws(()=>readExpensePage({...page,summary:{...page.summary,recordedCount:3}},property));assert.throws(()=>readExpensePage({...page,items:[expense,expense],pageSize:2},property));
});
test('budget replay can refer to an old revision without changing the current amount',()=>{
 assert.deepEqual(readBudget(budget,property,'2026-10','ENERGY'),budget);
 for(const value of [{...budget,period:'2026-13'},{...budget,amount:'12.35'},{...budget,appliedRevision:3},{...budget,revisions:[]},{...budget,revisions:[budget.revisions[1],budget.revisions[0]]}])assert.throws(()=>readBudget(value,property));
 assert.throws(()=>readBudget(budget,property,'2026-09','ENERGY'));assert.throws(()=>readBudget(budget,other));
});
test('monthly summary validates category totals, plans, scope and signed variance',()=>{
 assert.deepEqual(readFinanceSummary(summary,property,'2026-10'),summary);
 for(const value of [{...summary,netAmount:'3.10'},{...summary,budgetVariance:'20.00'},{...summary,categories:summary.categories.slice(1)},{...summary,categories:summary.categories.map(row=>row.category==='ENERGY'?{...row,variance:'20.00'}:row)},{...summary,categories:summary.categories.map(row=>row.category==='STAFF'?{...row,budgetAmount:'1.00'}:row)}])assert.throws(()=>readFinanceSummary(value,property,'2026-10'));
 assert.throws(()=>readFinanceSummary(summary,property,'2026-09'));assert.throws(()=>readFinanceSummary(summary,other,'2026-10'));
});
test('expenses have independent menus and exact safe OIDC return routes',()=>{
 assert.ok(isWorkspaceReturnPath('/expenses'));assert.ok(isWorkspaceReturnPath(`/properties/${property}/expenses`));assert.equal(managementSection(`/properties/${property}/expenses`,property),'expenses');
 for(const url of ['/expenses?token=secret','//evil.test/expenses',`/properties/${property}/expenses/other`])assert.equal(isWorkspaceReturnPath(url),false);
});
test('new backend validation fields and warnings use the shared localized contract',()=>{
 const body={code:'BF_VALIDATION_FAILED',fieldIssues:{category:[{code:'BF_EXPENSE_CATEGORY_INVALID'}],occurredDate:[{code:'BF_EXPENSE_DATE_INVALID'}],explanation:[{code:'BF_DUES_ERROR_DESCRIPTION'}]}};
 const parsed=problemDetails(400,body,'en');assert.equal(Object.keys(parsed.errors).length,3);assert.match(parsed.errors.category[0],/category/);
 assert.equal(problemDetails(409,{code:'BF_EXPENSE_ALREADY_CANCELLED',message:'private-secret'},'en').severity,'warning');
});
