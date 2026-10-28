import {createNavigation} from '../src/features/navigation/navigation.ts';
import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {canConfirmCleanup,readBatches,readPreview,canRetry,isActive,qaPlannedTabs,readAiExplanation,readAiPlan,readAiStatus,qaRunStatuses,qaStepStatuses,qaTabs,readDetail,readOverview,readPage,readRun,readScenarios,runsQuery,scenarioMessageKey} from '../src/features/qa-center/model.ts';
import {qaDataKindIcon,qaPlannedIcon,qaRunStatusIcon,qaScenarioIcon,qaStepStatusIcon,qaTabIcon} from '../src/features/qa-center/qa-style.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url))).qaCenter;
const en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url))).qaCenter;
const run={id:'11111111-1111-4111-8111-111111111111',code:'qa-20261028T173000Z-abcdef',scenarioKey:'db.migrations',status:'FAILED',attempt:1,retryOfId:null,cancelRequested:false,
 queuedAt:'2026-10-28T17:30:00Z',startedAt:'2026-10-28T17:30:01Z',finishedAt:'2026-10-28T17:30:02Z',resultCode:'STEP_FAILED',passedSteps:1,failedSteps:1,timeoutSeconds:60,version:3};

test('QA run parsers accept the API contract and reject unknown statuses',()=>{
 assert.deepEqual(readRun(run),run);
 assert.throws(()=>readRun({...run,status:'DONE'}));
 assert.throws(()=>readRun({...run,version:-1}));
 const detail=readDetail({run,steps:[{sequence:1,stepKey:'database.connect',status:'PASSED',expected:'true',actual:'true',durationMs:4}]});
 assert.equal(detail.steps[0].status,'PASSED');
 assert.throws(()=>readDetail({run,steps:[{sequence:1,stepKey:'x',status:'BROKEN',expected:'a',actual:'b',durationMs:1}]}));
 assert.equal(readPage({items:[run],total:1,page:1,pageSize:10}).items.length,1);
 assert.deepEqual(readScenarios([{key:'db.migrations',timeoutSeconds:60,access:'READ_ONLY'}]),[{key:'db.migrations',timeoutSeconds:60,access:'READ_ONLY'}]);
 const overview=readOverview({environment:'Development',scenarioCount:3,endpointCount:405,activeRuns:0,runCounts:{PASSED:2},lastRun:null});
 assert.equal(overview.runCounts.PASSED,2);assert.equal(overview.runCounts.FAILED,0);
});

test('run actions follow the backend state machine and retry limit',()=>{
 assert.equal(isActive({...run,status:'QUEUED'}),true);assert.equal(isActive({...run,status:'RUNNING'}),true);assert.equal(isActive(run),false);
 for(const status of ['FAILED','CANCELLED','TIMED_OUT'])assert.equal(canRetry({...run,status}),true);
 for(const status of ['QUEUED','RUNNING','PASSED'])assert.equal(canRetry({...run,status}),false);
 assert.equal(canRetry({...run,attempt:5}),false);
 assert.equal(runsQuery(2,'ALL'),'/api/v1/qa-center/runs?page=2&pageSize=10');
 assert.equal(runsQuery(1,'TIMED_OUT'),'/api/v1/qa-center/runs?page=1&pageSize=10&status=TIMED_OUT');
});

test('every tab, status and scenario has an icon and TR/EN text',()=>{
 assert.deepEqual(Object.keys(tr).sort(),Object.keys(en).sort());
 for(const tab of qaTabs){assert.ok(qaTabIcon[tab]);assert.ok(tr[`tab_${tab}`]);}
 for(const [tab,phase] of qaPlannedTabs){assert.ok(qaPlannedIcon[tab]);assert.ok(tr[`tab_${tab}`]);assert.match(phase,/^\d{2}$/);}
 for(const status of qaRunStatuses){assert.ok(qaRunStatusIcon[status]);assert.ok(tr[`status_${status}`]);assert.ok(en[`status_${status}`]);}
 for(const status of qaStepStatuses){assert.ok(qaStepStatusIcon[status]);assert.ok(tr[`step_${status}`]);}
 for(const key of ['system.health','api.authorization','db.migrations']){assert.ok(qaScenarioIcon[key]);assert.ok(tr[scenarioMessageKey(key)]);assert.ok(en[scenarioMessageKey(key)+'_hint']);}
});

test('QA centre route is a safe OIDC return path',()=>{
 assert.equal(isWorkspaceReturnPath('/system-settings/qa-center'),true);
 assert.equal(isWorkspaceReturnPath('/system-settings'),true);
 for(const path of ['/system-settings/qa-center/x','/system-settings/qa-center?token=x'])assert.equal(isWorkspaceReturnPath(path),false);
});

test('property SMS screen survives a reload (QA FAZ 04 finding)',()=>{
 const p='11111111-1111-4111-8111-111111111111';
 assert.equal(isWorkspaceReturnPath(`/properties/${p}/sms`),true);
 assert.equal(isWorkspaceReturnPath(`/properties/${p}/sms/extra`),false);
});

test('AI plan is only accepted as an approval-required proposal and explanations keep run status',()=>{
 assert.deepEqual(readAiStatus({enabled:true,dailyLimit:20,usedToday:3}),{enabled:true,dailyLimit:20,usedToday:3});
 const plan=readAiPlan({scenarios:['db.migrations'],summary:'Şema',risks:[],source:'AKAAI',requiresApproval:true});
 assert.equal(plan.requiresApproval,true);
 assert.throws(()=>readAiPlan({scenarios:['db.migrations'],summary:'Şema',risks:[],source:'AKAAI',requiresApproval:false}));
 assert.throws(()=>readAiPlan({scenarios:[1],summary:'x',risks:[],source:'AKAAI',requiresApproval:true}));
 assert.equal(readAiExplanation({runId:'r',runStatus:'FAILED',explanation:'x',source:'AKAAI'}).runStatus,'FAILED');
 assert.throws(()=>readAiExplanation({runId:'r',runStatus:'GREEN',explanation:'x',source:'AKAAI'}));
 for(const key of ['aiProposal','aiApprove','aiExplain','aiExplanationTitle'])assert.ok(tr[key]&&en[key]);
});

test('cleanup needs a clean preview and the exact confirmation text; demo and blockers stay visible',()=>{
 const preview=readPreview({batchId:'b',code:'qa-data-x',kind:'TEST',canClean:true,items:[{order:1,entityType:'UnitOccupancy',count:2}],blockers:[],confirmation:'TEMIZLE qa-data-x',version:1});
 assert.equal(canConfirmCleanup(preview,'TEMIZLE qa-data-x'),true);
 assert.equal(canConfirmCleanup(preview,'temizle qa-data-x'),false);
 assert.equal(canConfirmCleanup({...preview,canClean:false},'TEMIZLE qa-data-x'),false);
 assert.throws(()=>readPreview({...preview,kind:'PROD'}));
 assert.equal(readBatches([{id:'b',code:'c',kind:'DEMO',environment:'Development',runCode:null,status:'ACTIVE',recordCount:35,generatedAt:'2026-10-28T19:00:00Z',cleanedAt:null,propertyId:'p',version:1}])[0].kind,'DEMO');
 for(const kind of ['DEMO','TEST']){assert.ok(qaDataKindIcon[kind]);assert.ok(tr[`kind_${kind}`]&&en[`kind_${kind}`]);}
 for(const code of ['DEMO_PROTECTED','FINANCE_HISTORY','UNREGISTERED_ROWS','ALREADY_CLEANED'])assert.ok(tr[`blocker_${code}`]&&en[`blocker_${code}`]);
});

test('sidebar shows the QA centre only to SUPER_ADMIN under system administration',()=>{
 const has=roles=>createNavigation({roles,sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/').groups.flatMap(g=>g.items).some(i=>i.id==='qa-center');
 assert.equal(has(['SUPER_ADMIN']),true);
 for(const role of ['SITE_ADMIN','MANAGER','ACCOUNTANT','RESIDENT'])assert.equal(has([role]),false,role);
 const item=createNavigation({roles:['SUPER_ADMIN'],sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/').groups.find(g=>g.id==='administration').items.find(i=>i.id==='qa-center');
 assert.equal(item.href,'/system-settings/qa-center');
});
