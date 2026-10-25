import test from 'node:test';
import assert from 'node:assert/strict';
import {DatabaseSync} from 'node:sqlite';
import {migrate,writeSnapshot} from '../src/offline/database.ts';
import {parseFinance,validateQuery} from '../src/finance/contracts.ts';
import {readFinance,writeFinance,purgeFinance} from '../src/finance/cache.ts';
import {formatMoney} from '../src/owner/contracts.ts';
const user='finance-fixture',c={propertyId:'11111111-1111-1111-1111-111111111111',propertyName:'Fixture',unitId:'22222222-2222-2222-2222-222222222222',unitNumber:'1',role:'OWNER'};
const id='33333333-3333-3333-3333-333333333333',q={view:'charges',page:1,chargeId:null};
const charge={id,period:'2026-10',amount:'100.10',paid:'30.05',balance:'70.05',dueDate:'2026-10-10',status:'PARTIALLY_PAID'};
const data={userId:user,contextKey:c.propertyId+':'+c.unitId+':OWNER',asOf:'2026-10-15T00:00:00Z',propertyName:'Fixture',...q,total:1,summary:{amount:'100.10',paid:'30.05',balance:'70.05',overdue:'70.05'},charges:[{charge,unitNumber:'1'}],payments:[]};
const payment={id:'44444444-4444-4444-4444-444444444444',chargeId:id,unitNumber:'1',period:'2026-10',amount:'30.05',paidDate:'2026-10-10',cancelled:false};
const history={...data,view:'payments',charges:[],payments:[payment]};
async function fixture(){const sql=new DatabaseSync(':memory:');sql.exec('PRAGMA foreign_keys=ON');const db={execAsync:async s=>sql.exec(s),runAsync:async(s,...v)=>sql.prepare(s).run(...v),getFirstAsync:async(s,...v)=>sql.prepare(s).get(...v)??null};await migrate(db);await writeSnapshot(db,{userId:user,username:null,contexts:[c],hasMore:false},1000,'fixture',data.contextKey);return {sql,db};}
test('financial cents and huge totals never round through floating point',()=>{
 assert.equal(parseFinance(data,user,c,q).summary.balance,'70.05');assert.equal(formatMoney('99999999999999999999.99'),'99.999.999.999.999.999.999,99 TL');
 for(const s of [{...data.summary,balance:'70.04'},{...data.summary,overdue:'70.06'},{...data.summary,amount:'100.105'},{...data.summary,paid:'3e1'}])assert.throws(()=>parseFinance({...data,summary:s},user,c,q));
});
test('finance scope rejects foreign account/property/role/page/detail and malformed history',()=>{
 assert.throws(()=>parseFinance(data,'other',c,q));assert.throws(()=>parseFinance(data,user,{...c,role:'TENANT'},q));assert.throws(()=>parseFinance(data,user,{...c,propertyId:id},q));assert.throws(()=>parseFinance(data,user,c,{...q,page:2}));
 assert.throws(()=>parseFinance({...data,chargeId:payment.id},user,c,{...q,chargeId:payment.id}));
 assert.throws(()=>validateQuery({...c,role:'ACCOUNTANT'},q));assert.doesNotThrow(()=>validateQuery({...c,role:'ACCOUNTANT',unitId:null},q));
 assert.throws(()=>parseFinance({...history,payments:[{...payment,paidDate:'2026-02-30'}]},user,c,{...q,view:'payments'}));
 assert.throws(()=>parseFinance({...history,payments:[{...payment,cancelled:'false'}]},user,c,{...q,view:'payments'}));
 assert.throws(()=>parseFinance({...history,total:2,payments:[payment,payment]},user,c,{...q,view:'payments'}));
});
test('reversed allocation is retained separately without changing verified current totals',()=>{
 const p=parseFinance({...history,payments:[{...payment,cancelled:true}]},user,c,{...q,view:'payments'});assert.equal(p.payments[0].cancelled,true);assert.equal(p.summary.paid,'30.05');
 assert.throws(()=>parseFinance({...data,charges:[],total:1},user,c,q));assert.throws(()=>parseFinance({...data,charges:[{...data.charges[0],charge:{...charge,balance:'70.04'}}]},user,c,q));
});
test('cache isolates role and detail, drops private fields and keeps at most two view slots',async()=>{
 const {sql,db}=await fixture();try{
 await writeFinance(db,user,c,q,{...data,accessToken:'private',charges:[{...data.charges[0],charge:{...charge,identityNumber:'private'}}]},1000);
 assert.ok(await readFinance(db,user,c,q,1001));assert.equal(await readFinance(db,user,c,{...q,chargeId:id},1001),null);assert.equal(await readFinance(db,user,{...c,role:'TENANT'},q,1001),null);
 assert.ok(!sql.prepare('SELECT payload FROM owner_snapshots').get().payload.includes('private'));
 await writeFinance(db,user,c,{...q,view:'payments'},history,1001);
 await writeFinance(db,user,c,{...q,chargeId:id},{...data,chargeId:id},1002);assert.equal(await readFinance(db,user,c,q,1003),null);assert.equal(sql.prepare('SELECT COUNT(*) AS n FROM owner_snapshots').get().n,2);
 await purgeFinance(db,user,c);assert.equal(await readFinance(db,user,c,{...q,view:'payments'},1004),null);
 }finally{sql.close();}
});
test('expired, backwards-clock or older financial copies cannot replace current data',async()=>{
 const {sql,db}=await fixture();try{await writeFinance(db,user,c,q,data,1000);assert.equal(await readFinance(db,user,c,q,999),null);assert.equal(await readFinance(db,user,c,q,86401001),null);await assert.rejects(writeFinance(db,user,c,q,{...data,asOf:'2026-10-14T00:00:00Z'},1001));assert.equal((await readFinance(db,user,c,q,1001)).asOf,data.asOf);}finally{sql.close();}
});

test('contradictory paid/cancelled/open states cannot masquerade as confirmed settlement',()=>{
 for(const status of ['PAID','CANCELLED','OPEN'])assert.throws(()=>parseFinance({...data,charges:[{charge:{...charge,status},unitNumber:'1'}]},user,c,q));
 assert.doesNotThrow(()=>parseFinance({...data,charges:[{charge:{...charge,paid:'100.10',balance:'0.00',status:'PAID'},unitNumber:'1'}]},user,c,q));
});
test('late penalty is shown from server values only, separately from principal and detail lines',()=>{
 const withPenalty={...data,summary:{...data.summary,penalty:'12.50'},charges:[{charge:{...charge,penalty:'14.17'},unitNumber:'1'}]};
 const parsed=parseFinance(withPenalty,user,c,q);assert.equal(parsed.summary.penalty,'12.50');assert.equal(parsed.charges[0].charge.penalty,'14.17');assert.equal(parsed.charges[0].charge.balance,'70.05');
 assert.equal(parseFinance(data,user,c,q).charges[0].charge.penalty,'0.00');
 assert.throws(()=>parseFinance({...withPenalty,charges:[{charge:{...charge,penalty:'1.5'},unitNumber:'1'}]},user,c,q));
 const line={from:'2026-10-11',to:'2026-10-15',days:5,principal:'70.05',monthlyRate:'5.00',amount:'0.58',status:'PENDING'};
 const detail={...data,chargeId:id,penaltyLines:[line]};assert.equal(parseFinance(detail,user,c,{...q,chargeId:id}).penaltyLines[0].status,'PENDING');
 assert.throws(()=>parseFinance({...data,penaltyLines:[line]},user,c,q));
 assert.throws(()=>parseFinance({...detail,penaltyLines:[{...line,monthlyRate:'6.00'}]},user,c,{...q,chargeId:id}));
 const penaltyPayment={...history,payments:[{...payment,kind:'PENALTY'}]};assert.equal(parseFinance(penaltyPayment,user,c,{...q,view:'payments'}).payments[0].kind,'PENALTY');
 assert.throws(()=>parseFinance({...history,payments:[{...payment,kind:'FEE'}]},user,c,{...q,view:'payments'}));
});
