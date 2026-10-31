import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {catalogKeys,definitionBody,definitionForm,definitionFormErrors,definitionIconKeys,definitionKeys,definitionLabelKey,definitionLevels,definitionTones,definitionValuePath,filterLists,overrideName,readDefinitionValue,readDefinitions,sortedValues,definitionOptions,readPropertyDefinitions,propertyDefinitionValuePath,readCancellationReasons,reasonFormErrors,reasonBody} from '../src/features/definitions/model.ts';
import {readSupplierCategoryRow} from '../src/features/suppliers/model.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url),'utf8')),en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url),'utf8'));
const at=(d,path)=>path.split('.').reduce((o,k)=>o?.[k],d);
const value=(code,patch={})=>({code,nameTr:null,nameEn:null,iconKey:null,tone:null,sortOrder:0,isEnabled:true,locked:code==='OTHER',version:0,...patch});
const list=(key,patch={})=>({key,group:'REQUEST',level:'PROPERTY',source:'MobileRequest.Categories',plannedPhase:'TNM_02',editable:true,values:[value('ELEVATOR'),value('OTHER',{sortOrder:1})],...patch});
const body=(patch={})=>({lists:[list('REQUEST_CATEGORY')],catalogs:[{key:'CONSENT_TYPES',group:'COMMUNICATION',scope:'SYSTEM',route:'/consents'},{key:'DUES_CANCELLATION_REASONS',group:'FINANCE',scope:'SYSTEM',route:null}],...patch});
const ctx=roles=>({roles,sessionContexts:false,centralPeople:false,propertyId:null,property:null});
const find=(m,id)=>m.groups.flatMap(g=>g.items).find(i=>i.id===id);

test('TNM01 menu: Tanımlar is a routed management link with OIDC return support',()=>{
 for(const role of ['SUPER_ADMIN','SITE_ADMIN','MANAGER']){const item=find(createNavigation(ctx([role]),'/definitions'),'definitions');assert.equal(item.href,'/definitions');assert.equal(item.active,true);}
 for(const role of ['ACCOUNTANT','SECURITY','RESIDENT'])assert.equal(find(createNavigation(ctx([role]),'/'),'definitions'),undefined,role);
 assert.equal(isWorkspaceReturnPath('/definitions'),true);assert.equal(isWorkspaceReturnPath('/definitions/x'),false);
});

test('TNM01 contract: strict shape, unknown keys ignored, bad codes and phase rules rejected',()=>{
 const ok=readDefinitions(body());assert.equal(ok.lists.length,1);assert.equal(ok.catalogs[1].route,null);
 assert.equal(readDefinitions(body({lists:[list('FUTURE_LIST'),list('REQUEST_CATEGORY')]})).lists.length,1);
 for(const bad of [null,{lists:[]},{catalogs:[]},body({lists:[list('REQUEST_CATEGORY',{values:[value('elevator')]})]}),body({lists:[list('REQUEST_CATEGORY',{values:[]})]}),body({lists:[list('REQUEST_CATEGORY',{values:[value('A'),value('A')]})]}),body({lists:[list('REQUEST_CATEGORY',{values:['ELEVATOR']})]}),
  body({lists:[list('UNIT_TYPE',{level:'FIXED',plannedPhase:null,editable:true})]}),body({lists:[list('EXPENSE_CATEGORY',{plannedPhase:'TNM_05',editable:false,values:[value('ENERGY',{version:1})]})]}),
  body({lists:[list('REQUEST_CATEGORY',{level:'FIXED'})]}),body({lists:[list('REQUEST_CATEGORY',{level:'SYSTEM',plannedPhase:null})]}),body({lists:[list('REQUEST_CATEGORY',{group:'OTHER'})]}),
  body({catalogs:[{key:'CONSENT_TYPES',group:'COMMUNICATION',scope:'SYSTEM',route:'https://evil.example'}]})])assert.throws(()=>readDefinitions(bad));
 assert.deepEqual(filterLists(ok.lists,'FINANCE','ALL'),[]);assert.equal(filterLists(ok.lists,'REQUEST','PROPERTY').length,1);
});

test('TNM01 labels reuse module dictionaries; page texts exist in TR and EN',()=>{
 const samples={PROPERTY_TYPE:'RESIDENCE',COMMON_AREA_TYPE:'POOL',UNIT_TYPE:'VILLA',CHARGE_KIND:'EXTRA',CHARGE_CATEGORY:'FIXTURE',EXPENSE_CATEGORY:'ENERGY',PAYMENT_METHOD:'CASH',REQUEST_CATEGORY:'ELEVATOR',REQUEST_PRIORITY:'HIGH',MAINTENANCE_TYPE:'DRILL',MAINTENANCE_FREQUENCY:'QUARTERLY',VEHICLE_TYPE:'VAN',PARKING_SPACE_TYPE:'EV_CHARGING',SUPPLIER_KIND:'COMPANY',ANNOUNCEMENT_CATEGORY:'URGENT',SURVEY_CATEGORY:'SOCIAL'};
 assert.deepEqual(Object.keys(samples).sort(),[...definitionKeys].sort());
 for(const [key,value] of Object.entries(samples)){const {namespace,key:k}=definitionLabelKey(key,value);for(const d of [tr,en])assert.equal(typeof at(d,`${namespace}.${k}`),'string',`${key}:${namespace}.${k}`);}
 const page=Object.keys(tr.definitions);assert.deepEqual(page.sort(),Object.keys(en.definitions).sort());
 for(const key of definitionKeys)assert.ok(page.includes(`list_${key}`));
 for(const key of catalogKeys)assert.ok(page.includes(`catalog_${key}`)&&page.includes(`catalogNote_${key}`));
 for(const level of definitionLevels)assert.ok(page.includes(`level_${level}`)&&page.includes(`levelHint_${level}`));
});

test('TNM01 icon rule: every list, group, level, value and catalog has an icon mapping',()=>{
 const style=readFileSync(new URL('../src/features/definitions/definition-style.ts',import.meta.url),'utf8');
 for(const key of [...definitionKeys,...catalogKeys])assert.match(style,new RegExp(`\\b${key}:`),key);
 const view=readFileSync(new URL('../src/features/definitions/DefinitionsWorkspace.tsx',import.meta.url),'utf8');
 assert.match(view,/FilterChips/);assert.doesNotMatch(view,/fetch\(/);
});

test('TNM02 value contract: overrides, icon/tone allowlist, locked fallback and ordering',()=>{
 assert.deepEqual(readDefinitionValue(value('WATER',{nameTr:'Su',iconKey:'droplet',tone:'blue',sortOrder:4,isEnabled:false,version:3})).iconKey,'droplet');
 for(const bad of [value('X',{iconKey:'<svg>'}),value('X',{tone:'pink'}),value('X',{sortOrder:1000}),value('X',{sortOrder:1.5}),value('X',{nameTr:''}),value('X',{nameTr:'a'.repeat(101)}),value('X',{version:-1}),value('OTHER',{isEnabled:false}),{...value('X'),locked:undefined}])assert.throws(()=>readDefinitionValue(bad));
 assert.deepEqual(sortedValues([value('B',{sortOrder:2}),value('A',{sortOrder:2}),value('C',{sortOrder:0})]).map(v=>v.code),['C','B','A']);
 assert.equal(overrideName(value('A',{nameTr:'Tr',nameEn:'En'}),'en'),'En');assert.equal(overrideName(value('A',{nameTr:'Tr'}),'en'),null);assert.equal(overrideName(value('A',{nameTr:'Tr'}),'tr'),'Tr');
 assert.equal(definitionValuePath('REQUEST_CATEGORY','ELEVATOR'),'/api/v1/definitions/REQUEST_CATEGORY/values/ELEVATOR');
 const backend=readFileSync(new URL('../../../backend/src/BuildingFlow.Application/Definitions.cs',import.meta.url),'utf8');
 const set=name=>[...backend.slice(backend.indexOf(name)).split(';')[0].matchAll(/"([a-z0-9-]+)"/g)].map(m=>m[1]);
 assert.deepEqual(set('IconKeys ='),[...definitionIconKeys]);assert.deepEqual(set('Tones ='),[...definitionTones]);
 const style=readFileSync(new URL('../src/features/definitions/definition-style.ts',import.meta.url),'utf8');
 for(const key of definitionIconKeys)assert.match(style,new RegExp(`(?:"${key}"|\\b${key}):[A-Z]`),key);
});

test('TNM02 editor form: blank names fall back, order and lock validated, body carries version',()=>{
 const form=definitionForm(value('WATER',{sortOrder:4}));assert.deepEqual(definitionFormErrors(form,false),[]);
 assert.deepEqual(definitionBody({...form,nameTr:'  Su  ',nameEn:'   '},7),{nameTr:'Su',nameEn:null,iconKey:null,tone:null,sortOrder:4,isEnabled:true,version:7});
 assert.deepEqual(definitionFormErrors({...form,sortOrder:'1000'},false),['sortOrder']);assert.deepEqual(definitionFormErrors({...form,sortOrder:'-1'},false),['sortOrder']);
 assert.deepEqual(definitionFormErrors({...form,nameTr:'a'.repeat(101)},false),['nameTr']);
 assert.deepEqual(definitionFormErrors({...form,isEnabled:false},true),['isEnabled']);
 const view=readFileSync(new URL('../src/features/definitions/DefinitionsWorkspace.tsx',import.meta.url),'utf8');
 assert.match(view,/ManagementPanel/);assert.match(view,/definitionEditorRole/);assert.match(view,/loadCurrentRecord/);
});

test('TNM03 area contract and form options: hidden/passive leave forms, current code stays, fallback keeps module list',()=>{
 const P='77777777-7777-4777-8777-777777777777';
 const av=(code,patch={})=>({...value(code),isHidden:false,selectable:true,...patch});
 const area={propertyId:P,lists:[{key:'REQUEST_CATEGORY',group:'REQUEST',level:'PROPERTY',values:[av('WATER',{sortOrder:0}),av('ELEVATOR',{sortOrder:1,isHidden:true,selectable:false,version:1}),av('FAULT',{sortOrder:2,isEnabled:false,selectable:false}),av('OTHER',{sortOrder:3})]},{key:'FUTURE',group:'REQUEST',level:'SYSTEM',values:[]}]};
 const read=readPropertyDefinitions(area,P);assert.equal(read.lists.length,1);
 const codes=['ELEVATOR','WATER','FAULT','CLEANING','OTHER'];
 assert.deepEqual(definitionOptions(codes,read.lists[0]),['WATER','OTHER','CLEANING']);
 assert.deepEqual(definitionOptions(codes,read.lists[0],'ELEVATOR'),['WATER','ELEVATOR','OTHER','CLEANING']);
 assert.deepEqual(definitionOptions(codes,undefined),codes);
 for(const bad of [{...area,propertyId:'x'},{propertyId:P,lists:[{...area.lists[0],values:[av('OTHER',{isHidden:true,selectable:false})]}]},{propertyId:P,lists:[{...area.lists[0],values:[av('WATER',{isHidden:true,selectable:true})]}]},{propertyId:P,lists:[{...area.lists[0],level:'FIXED'}]}])assert.throws(()=>readPropertyDefinitions(bad,P));
 assert.equal(propertyDefinitionValuePath(P,'REQUEST_CATEGORY','WATER'),`/api/v1/properties/${P}/definitions/REQUEST_CATEGORY/values/WATER`);
 for(const [file,key] of [['requests/RequestsWorkspace.tsx','REQUEST_CATEGORY'],['requests/RequestsWorkspace.tsx','REQUEST_PRIORITY'],['maintenance/MaintenancePanel.tsx','MAINTENANCE_TYPE'],['common-areas/CommonAreaPanel.tsx','COMMON_AREA_TYPE'],['parking/VehiclesTab.tsx','VEHICLE_TYPE'],['communication/CommunicationPanels.tsx','ANNOUNCEMENT_CATEGORY'],['surveys/SurveyEditor.tsx','SURVEY_CATEGORY']])
  assert.match(readFileSync(new URL(`../src/features/${file}`,import.meta.url),'utf8'),new RegExp(`useDefinitionOptions\\(propertyId,["']${key}["']`),`${file}:${key}`);
});

test('TNM04 catalogues: reason and supplier category contracts, form rules and views',()=>{
 const id='11111111-1111-4111-8111-111111111111',reason=(patch={})=>({id,code:'WRONG_AMOUNT',name:'Yanlış tutar',requiresExplanation:false,isEnabled:true,locked:false,usageCount:3,version:1,...patch});
 assert.equal(readCancellationReasons([reason()])[0].usageCount,3);
 for(const bad of [[reason({code:'x'})],[reason({version:0})],[reason({locked:true,isEnabled:false})],[reason({usageCount:-1})],[reason(),reason({id:'22222222-2222-4222-8222-222222222222'})],{}])assert.throws(()=>readCancellationReasons(bad));
 const form={code:' BOARD ',name:' Kurul ',requiresExplanation:true,isEnabled:true};
 assert.deepEqual(reasonFormErrors({...form,code:'b'},true),['code']);assert.deepEqual(reasonFormErrors(form,true),[]);assert.deepEqual(reasonFormErrors({...form,code:'BOARD'},true),[]);assert.deepEqual(reasonFormErrors({...form,name:' '},false),['name']);
 assert.deepEqual(reasonBody({...form,code:'BOARD'},0,true),{code:'BOARD',name:'Kurul',requiresExplanation:true,isEnabled:true,version:0});assert.equal('code' in reasonBody(form,2,false),false);
 assert.equal(readSupplierCategoryRow({id,name:'Asansör',isEnabled:false,serviceCount:2,version:3}).serviceCount,2);
 for(const bad of [{id,name:'',isEnabled:true,serviceCount:0,version:1},{id,name:'A',isEnabled:true,serviceCount:0,version:0},{id:'x',name:'A',isEnabled:true,serviceCount:0,version:1}])assert.throws(()=>readSupplierCategoryRow(bad));
 assert.match(readFileSync(new URL('../src/features/suppliers/supplier-style.ts',import.meta.url),'utf8'),/categories:\{icon:/);
 assert.match(readFileSync(new URL('../src/features/definitions/DefinitionsWorkspace.tsx',import.meta.url),'utf8'),/CancellationReasonsPanel canEdit=\{canEdit\}/);
});

test('TNM05 finance choices preserve codes/history while hiding unavailable new selections',()=>{
 const P='77777777-7777-4777-8777-777777777777';
 const av=(code,patch={})=>({...value(code),isHidden:false,selectable:true,...patch});
 const read=readPropertyDefinitions({propertyId:P,lists:[{key:'EXPENSE_CATEGORY',group:'FINANCE',level:'PROPERTY',values:[av('OTHER'),av('ENERGY',{isHidden:true,selectable:false}),av('STAFF',{isEnabled:false,selectable:false})]},{key:'CHARGE_CATEGORY',group:'FINANCE',level:'PROPERTY',values:[av('REPAIR',{nameTr:'Onarım',nameEn:'Repair',iconKey:'wrench',sortOrder:0}),av('OTHER',{sortOrder:1})]}]},P);
 assert.deepEqual(definitionOptions(['ENERGY','STAFF','OTHER'],read.lists[0]),['OTHER']);
 assert.deepEqual(definitionOptions(['ENERGY','STAFF','OTHER'],read.lists[0],'ENERGY'),['OTHER','ENERGY']);
 assert.equal(overrideName(read.lists[1].values[0],'tr'),'Onarım');
 assert.equal(overrideName(read.lists[1].values[0],'en'),'Repair');
 assert.deepEqual(definitionOptions(['OTHER','REPAIR'],read.lists[1]),['REPAIR','OTHER']);
});
