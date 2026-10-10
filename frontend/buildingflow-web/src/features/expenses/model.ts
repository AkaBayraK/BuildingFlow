export const expenseCategories=["MAINTENANCE","CLEANING","STAFF","ENERGY","SECURITY","INSURANCE","ADMINISTRATION","OTHER"] as const;
export type ExpenseCategory=typeof expenseCategories[number];
export type ExpenseRecord={id:string;propertyId:string;requestId:string;category:ExpenseCategory;amount:string;currency:"TRY";occurredDate:string;description:string;documentReference:string|null;status:"RECORDED"|"CANCELLED";version:number;createdAt:string;cancellationReason:string|null;cancelledAt:string|null;replayed:boolean};
export type ExpensePage={items:ExpenseRecord[];page:number;pageSize:number;totalCount:number;summary:{recordedAmount:string;cancelledAmount:string;recordedCount:number;cancelledCount:number;currency:"TRY"}};
export type BudgetRecord={id:string;propertyId:string;period:string;category:ExpenseCategory;amount:string;currency:"TRY";version:number;revisions:{id:string;amount:string;sequence:number;explanation:string;createdAt:string}[];replayed:boolean;appliedRevision:number};
export type CategoryFinance={category:ExpenseCategory;expenseAmount:string;budgetAmount:string;variance:string;hasBudget:boolean;budgetId:string|null;budgetVersion:number|null};
export type FinanceSummary={propertyId:string;period:string;currency:"TRY";incomeAmount:string;expenseAmount:string;budgetAmount:string;netAmount:string;budgetVariance:string;categories:CategoryFinance[]};
const object=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==="object"&&!Array.isArray(v);
const text=(v:unknown):v is string=>typeof v==="string";
const guid=(v:unknown):v is string=>text(v)&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const integer=(v:unknown,min=0)=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=min;
const money=(v:unknown):v is string=>text(v)&&/^(?:0|[1-9][0-9]{0,17})\.[0-9]{2}$/.test(v);
const signed=(v:unknown):v is string=>text(v)&&/^-?(?:0|[1-9][0-9]{0,17})\.[0-9]{2}$/.test(v)&&v!=="-0.00";
const category=(v:unknown):v is ExpenseCategory=>text(v)&&(expenseCategories as readonly string[]).includes(v);
const period=(v:unknown):v is string=>text(v)&&/^[2-9][0-9]{3}-(?:0[1-9]|1[0-2])$/.test(v)&&v<"9999-01";
const date=(v:unknown):v is string=>text(v)&&/^[2-9][0-9]{3}-[0-9]{2}-[0-9]{2}$/.test(v)&&Number.isFinite(Date.parse(v))&&new Date(v).toISOString().slice(0,10)===v;
const timestamp=(v:unknown)=>text(v)&&Number.isFinite(Date.parse(v));
const explanation=(v:unknown)=>text(v)&&!!v.trim()&&v.length<=500;
export function financeCents(value:string):bigint{if(!signed(value))throw new Error("Invalid money");const negative=value.startsWith("-");const [whole,fraction]=value.replace(/^-/,'').split('.');const result=BigInt(whole)*BigInt(100)+BigInt(fraction);return negative?-result:result;}
export function normalizeFinanceAmount(value:string,allowZero=false):string|null{
 const clean=value.trim().replace(',','.');if(!/^[0-9]+(?:\.[0-9]{1,2})?$/.test(clean)||clean.length>20)return null;
 const [whole,fraction=""]=clean.split('.');const result=`${BigInt(whole)}.${fraction.padEnd(2,'0')}`;const amount=financeCents(result);
 return amount<(allowZero?BigInt(0):BigInt(1))||amount>BigInt(9999999999)?null:result;
}
export function readExpense(value:unknown,propertyId:string):ExpenseRecord{
 if(!object(value)||!["id","propertyId","requestId"].every(k=>guid(value[k]))||value.propertyId!==propertyId||!category(value.category)||!money(value.amount)||financeCents(value.amount)<=BigInt(0)||financeCents(value.amount)>BigInt(9999999999)||value.currency!=="TRY"||!date(value.occurredDate)||!explanation(value.description)||!(value.documentReference===null||text(value.documentReference)&&!!value.documentReference.trim()&&value.documentReference.length<=100)||!text(value.status)||!["RECORDED","CANCELLED"].includes(value.status)||!integer(value.version,1)||!timestamp(value.createdAt)||typeof value.replayed!=="boolean")throw new Error("Invalid expense response");
 if(value.status==="CANCELLED"?(!explanation(value.cancellationReason)||!timestamp(value.cancelledAt)):(value.cancellationReason!==null||value.cancelledAt!==null))throw new Error("Invalid expense cancellation history");
 return value as ExpenseRecord;
}
export function readExpensePage(value:unknown,propertyId:string):ExpensePage{
 if(!object(value)||!Array.isArray(value.items)||!integer(value.page,1)||!integer(value.pageSize,1)||Number(value.pageSize)>100||!integer(value.totalCount)||value.items.length>Number(value.pageSize)||value.items.length>Number(value.totalCount)||!object(value.summary))throw new Error("Invalid expense page");
 const rows=value.items.map(row=>readExpense(row,propertyId));if(new Set(rows.map(row=>row.id)).size!==rows.length)throw new Error("Duplicate expenses");
 const s=value.summary;if(!money(s.recordedAmount)||!money(s.cancelledAmount)||!integer(s.recordedCount)||!integer(s.cancelledCount)||Number(s.recordedCount)+Number(s.cancelledCount)!==value.totalCount||s.currency!=="TRY")throw new Error("Invalid expense summary");
 return value as ExpensePage;
}
export function readBudget(value:unknown,propertyId:string,expectedPeriod?:string,expectedCategory?:ExpenseCategory):BudgetRecord{
 if(!object(value)||!guid(value.id)||value.propertyId!==propertyId||!period(value.period)||(expectedPeriod&&value.period!==expectedPeriod)||!category(value.category)||(expectedCategory&&value.category!==expectedCategory)||!money(value.amount)||financeCents(value.amount)>BigInt(9999999999)||value.currency!=="TRY"||!integer(value.version,1)||typeof value.replayed!=="boolean"||!integer(value.appliedRevision,1)||!Array.isArray(value.revisions)||!value.revisions.length)throw new Error("Invalid budget response");
 const revisionCount=value.revisions.length;value.revisions.forEach((r,index)=>{if(!object(r)||!guid(r.id)||!money(r.amount)||financeCents(r.amount)>BigInt(9999999999)||r.sequence!==revisionCount-index||!explanation(r.explanation)||!timestamp(r.createdAt))throw new Error("Invalid budget history");});
 const revisions=value.revisions as BudgetRecord['revisions'];if(new Set(revisions.map(r=>r.id)).size!==revisions.length||value.amount!==revisions[0].amount||Number(value.appliedRevision)>revisions.length)throw new Error("Invalid current budget");
 return value as BudgetRecord;
}
export function readFinanceSummary(value:unknown,propertyId:string,expectedPeriod:string):FinanceSummary{
 if(!object(value)||value.propertyId!==propertyId||value.period!==expectedPeriod||!period(value.period)||value.currency!=="TRY"||!money(value.incomeAmount)||!money(value.expenseAmount)||!money(value.budgetAmount)||!signed(value.netAmount)||!signed(value.budgetVariance)||!Array.isArray(value.categories)||value.categories.length!==expenseCategories.length)throw new Error("Invalid monthly finance summary");
 const income=financeCents(value.incomeAmount),expense=financeCents(value.expenseAmount),budget=financeCents(value.budgetAmount);
 if(income-expense!==financeCents(value.netAmount)||budget-expense!==financeCents(value.budgetVariance))throw new Error("Invalid monthly totals");
 for(const row of value.categories){if(!object(row)||!category(row.category)||!money(row.expenseAmount)||!money(row.budgetAmount)||!signed(row.variance)||typeof row.hasBudget!=="boolean"||(row.hasBudget?(!guid(row.budgetId)||!integer(row.budgetVersion,1)):(row.budgetId!==null||row.budgetVersion!==null||row.budgetAmount!=="0.00"))||financeCents(row.budgetAmount)-financeCents(row.expenseAmount)!==financeCents(row.variance))throw new Error("Invalid category totals");}
 const rows=value.categories as CategoryFinance[];
 if(new Set(rows.map(r=>r.category)).size!==expenseCategories.length||rows.reduce((sum,r)=>sum+financeCents(r.expenseAmount),BigInt(0))!==expense||rows.reduce((sum,r)=>sum+financeCents(r.budgetAmount),BigInt(0))!==budget)throw new Error("Invalid category summary");
 return value as FinanceSummary;
}
