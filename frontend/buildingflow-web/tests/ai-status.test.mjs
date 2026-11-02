import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createNavigation} from '../src/features/navigation/navigation.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
import {aiComponentIds,aiComponentTypes,aiDetails,aiStatuses,filterAiComponents,parseAiStatus,safeConsoleUrl} from '../src/features/ai-status/model.ts';
import {aiStatusIcon,aiTypeIcon} from '../src/features/ai-status/ai-status-style.ts';
const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url))).aiStatus;
const en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url))).aiStatus;

const body={overallStatus:'UP',mode:'AKAAI',model:'llama3.2',modelProvider:'local-ollama',endpoint:'akaai.test:8090',locality:'EXTERNAL',consoleUrl:'http://console.akaai.test/models',
 activePromptCount:20,managementDailyLimit:30,residentEnabled:false,residentDailyLimit:10,qaCenterAiEnabled:true,totalComponents:99,upComponents:99,lastCheckedAt:'2026-11-02T10:00:00Z',
 components:[{id:'akaai-model',type:'MODEL',status:'UP',responseTimeMs:12,checkedAt:'2026-11-02T10:00:00Z',detail:'DEFAULT_MODEL_READY'},
  {id:'prompt-templates',type:'PROMPT',status:'UP',responseTimeMs:3,checkedAt:'2026-11-02T10:00:00Z',detail:'PROMPTS_READY',count:20},
  {id:'resident-assistant',type:'SERVICE',status:'DISABLED',responseTimeMs:null,checkedAt:'2026-11-02T10:00:00Z',detail:'CONSENT_GATE'},
  {id:'unknown-component',type:'MODEL',status:'UP',detail:'X'},{id:'qa-center-ai',type:'BOGUS',status:'BOGUS',detail:'<script>'}]};

test('parser keeps only known components and counts them itself',()=>{
 const s=parseAiStatus(body);
 assert.deepEqual(s.components.map(c=>c.id),['akaai-model','prompt-templates','resident-assistant','qa-center-ai']);
 assert.equal(s.totalComponents,4);assert.equal(s.upComponents,2);
 const qa=s.components.find(c=>c.id==='qa-center-ai');
 assert.deepEqual([qa.type,qa.status,qa.detail],['SERVICE','UNKNOWN','UNKNOWN']);
 assert.equal(s.components.find(c=>c.id==='prompt-templates').count,20);
 assert.equal(s.locality,'EXTERNAL');assert.equal(s.mode,'AKAAI');assert.equal(s.model,'llama3.2');
});

test('parser falls back safely for empty or hostile bodies',()=>{
 for(const value of [null,'x',[],{components:'x'}]){const s=parseAiStatus(value);assert.equal(s.overallStatus,'UNKNOWN');assert.equal(s.mode,'RULES');assert.equal(s.components.length,0);assert.equal(s.consoleUrl,null);assert.equal(s.endpoint,'-');}
 assert.equal(parseAiStatus({...body,locality:'MARS',mode:'X'}).locality,null);
});

test('console link opens only plain http(s) addresses',()=>{
 assert.equal(safeConsoleUrl('https://akaai.example/models'),'https://akaai.example/models');
 for(const value of ['javascript:alert(1)','https://user:pw@akaai.example/models','https://akaai.example/models?token=x','https://akaai.example/#x','/models',42,null])assert.equal(safeConsoleUrl(value),null,String(value));
});

test('filter uses component types',()=>{
 const s=parseAiStatus(body);
 assert.deepEqual(filterAiComponents(s.components,'MODEL').map(c=>c.id),['akaai-model']);
 assert.equal(filterAiComponents(s.components,'ALL').length,4);
});

test('every type, status, component and detail has an icon or TR/EN text',()=>{
 for(const type of ['ALL',...aiComponentTypes]){assert.ok(aiTypeIcon[type],type);assert.ok(tr[`filter_${type}`]&&en[`filter_${type}`],type);}
 for(const status of aiStatuses){assert.ok(aiStatusIcon[status],status);assert.ok(tr[`status_${status}`]&&en[`status_${status}`],status);}
 for(const id of aiComponentIds)assert.ok(tr[`component_${id}`]&&en[`component_${id}`],id);
 for(const detail of [...aiDetails,'UNKNOWN'])assert.ok(tr[`detail_${detail}`]&&en[`detail_${detail}`],detail);
 assert.deepEqual(Object.keys(tr).sort(),Object.keys(en).sort());
});

test('AI Status sits under system administration for management roles only',()=>{
 const items=roles=>createNavigation({roles,sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/system-settings/ai-status').groups.find(g=>g.id==='administration')?.items??[];
 for(const role of ['SUPER_ADMIN','SITE_ADMIN','MANAGER']){const item=items([role]).find(i=>i.id==='ai-status');assert.equal(item?.href,'/system-settings/ai-status',role);assert.equal(item.active,true);assert.equal(item.icon,'Gauge');}
 for(const role of ['ACCOUNTANT','SECURITY','RESIDENT'])assert.equal(items([role]).some(i=>i.id==='ai-status'),false,role);
 assert.equal(isWorkspaceReturnPath('/system-settings/ai-status'),true);
 for(const path of ['/system-settings/ai-status/x','/system-settings/ai-status?token=x'])assert.equal(isWorkspaceReturnPath(path),false);
});
