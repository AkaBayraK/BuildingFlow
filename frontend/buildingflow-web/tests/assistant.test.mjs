import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {downmix,encodeWav,mergeTranscript,pickRecorderType,readTranscript,readVoicePreference,readVoiceStatus,transcribeForm,voiceRoles,assistantCategories,assistantFactKinds,assistantTools,canAsk,canDraft,chatBody,dimensionKey,draftBody,draftEditorHref,draftStorageKey,emptyPromptForm,promptBody,promptFormValid,readAnswer,readDraft,readPrompts,readStatus,takeStoredDraft} from '../src/features/assistant/model.ts';
import {announcementDraftLink,requestLink,supplierViewLink,surveyLink} from '../src/features/assistant/deep-links.ts';
import {assistantCategoryStyle,assistantKindIcon,assistantToolStyle} from '../src/features/assistant/assistant-style.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
import {sidebarRows} from '../src/features/navigation/sidebar-layout.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
import {errorCodes} from '../src/services/error-codes.ts';
const tr=JSON.parse(readFileSync(new URL('../messages/tr.json',import.meta.url)));
const en=JSON.parse(readFileSync(new URL('../messages/en.json',import.meta.url)));
const backend=readFileSync(new URL('../../../backend/src/BuildingFlow.Application/Assistant.cs',import.meta.url),'utf8');
const P='11111111-1111-4111-8111-111111111111',Q='22222222-2222-4222-8222-222222222222';
const quota={dailyLimit:30,usedToday:2,remaining:28};
const card=(over={})=>({tool:'DUES_COLLECTION',key:'overdueAmount',value:'100.00',kind:'MONEY',dimension:null,from:'2026-10-01',to:'2026-11-01',href:`/properties/${P}/dues`,...over});
const answer=(over={})=>({status:'ANSWERED',tool:'DUES_COLLECTION',source:'RULES',modelFallback:false,answer:'Vadesi geçmiş 1 borç kaydında toplam 100,00 TL gecikmede.',sources:[card()],quota,readOnly:true,...over});

test('assistant readers accept the API contract and reject unknown tools, kinds, links and shapes',()=>{
 assert.deepEqual(readStatus({enabled:true,mode:'RULES',quota,tools:['DAILY_SUMMARY'],canManagePrompts:false,readOnly:true}).tools,['DAILY_SUMMARY']);
 for(const bad of [{mode:'GPT'},{readOnly:false},{tools:['RAW_SQL']},{quota:{...quota,dailyLimit:0}}])
  assert.throws(()=>readStatus({enabled:true,mode:'RULES',quota,tools:[],canManagePrompts:false,readOnly:true,...bad}));
 assert.equal(readAnswer(answer(),P).sources[0].value,'100.00');
 assert.equal(readAnswer(answer({status:'UNSUPPORTED',tool:null,sources:[]}),P).status,'UNSUPPORTED');
 for(const bad of [answer({status:'DONE'}),answer({tool:'PEOPLE'}),answer({readOnly:false}),answer({status:'UNSUPPORTED',tool:null}),answer({sources:[card({tool:'OPEN_REQUESTS'})]}),
  answer({sources:[card({href:'https://evil.example/x'})]}),answer({sources:[card({href:`/properties/${Q}/dues`})]}),answer({sources:[card({href:`/properties/${P}/people`})]}),
  answer({sources:[card({kind:'NAME'})]}),answer({sources:[card({value:'Ahmet Yılmaz'})]}),answer({sources:[card({dimension:'<script>'})]}),answer({answer:'x'.repeat(1201)})])
  assert.throws(()=>readAnswer(bad,P));
 assert.equal(readAnswer(answer({sources:[card({tool:'DAILY_SUMMARY',href:`/dashboard?property=${P}`})],tool:'DAILY_SUMMARY'}),P).sources.length,1);
 const prompt={id:'a1a55150-0001-4000-8000-000000000001',code:'TODAY_FOCUS',category:'OVERVIEW',toolKey:'DAILY_SUMMARY',titleTr:'Bugün',titleEn:'Today',textTr:'Bugün?',textEn:'Today?',sortOrder:10,isEnabled:true,version:1};
 assert.equal(readPrompts([prompt])[0].code,'TODAY_FOCUS');
 for(const bad of [{toolKey:'SQL'},{category:'PEOPLE'},{code:'lower'},{id:'1'}])assert.throws(()=>readPrompts([{...prompt,...bad}]));
});

test('chat body sends either a prompt code or trimmed free text, never both, and limits input',()=>{
 assert.deepEqual(chatBody(P,'tr',{promptCode:'TODAY_FOCUS'}),{propertyId:P,promptCode:'TODAY_FOCUS',locale:'tr'});
 assert.deepEqual(chatBody(P,'en',{message:'  Overdue?  '}),{propertyId:P,message:'Overdue?',locale:'en'});
 assert.equal(canAsk(''),false);assert.equal(canAsk('   '),false);assert.equal(canAsk('a'.repeat(501)),false);assert.equal(canAsk('ok\u0000'),false);
 assert.equal(canAsk('Satır 1\nSatır 2'),true);
});

test('prompt form validation mirrors the backend rules',()=>{
 const form={...emptyPromptForm(),code:'month_check',titleTr:'Ay',titleEn:'Month',textTr:'Ay?',textEn:'Month?'};
 assert.equal(promptBody(form,3).code,'MONTH_CHECK');assert.equal(promptBody(form,3).version,3);assert.equal(promptFormValid(form),true);
 for(const bad of [{code:'1X'},{code:'A'},{sortOrder:'1000'},{sortOrder:'-1'},{titleTr:' '},{textEn:'x'.repeat(501)}])assert.equal(promptFormValid({...form,...bad}),false);
});

test('menu shows the assistant under the overview for the four reader roles only',()=>{
 for(const roles of [['SUPER_ADMIN'],['SITE_ADMIN'],['MANAGER'],['ACCOUNTANT']]){
  const model=createNavigation({roles,sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/assistant');
  const overview=model.groups.find(g=>g.id==='overview');assert.deepEqual(overview.items.slice(0,2).map(i=>i.id),['dashboard','assistant']);
  const item=overview.items[1];assert.equal(item.href,'/assistant');assert.equal(item.icon,'Sparkles');assert.equal(item.active,true);
  assert.deepEqual(sidebarRows(model).slice(0,2).map(r=>r.id),['dashboard','assistant']);
 }
 for(const roles of [['SECURITY'],['RESIDENT'],[]]){
  const model=createNavigation({roles,sessionContexts:false,centralPeople:false,propertyId:null,property:null},'/assistant');
  assert.ok(!model.groups.flatMap(g=>g.items).some(i=>i.id==='assistant'));
 }
 for(const value of ['/assistant',`/assistant?property=${P}`])assert.equal(isWorkspaceReturnPath(value),true);
 for(const value of ['/assistant?property=x','/assistant?property=00000000-0000-0000-0000-000000000000',`/assistant?property=${P}&x=1`,'/assistant/admin'])assert.equal(isWorkspaceReturnPath(value),false);
});

test('every tool, category, fact key and error code has an icon and TR/EN text',()=>{
 const facts=[...new Set([...backend.matchAll(/new(?: AssistantFact)?\("([a-z][a-zA-Z0-9]+)", /g)].map(m=>m[1]).concat([...backend.matchAll(/new\(prefix \+ "([A-Za-z]+)"/g)].flatMap(m=>['collection','collectionYear'].map(p=>p+m[1]))))];
 assert.ok(facts.length>=30,'fact keys parsed from the backend');
 for(const dictionary of [tr,en]){
  for(const key of facts)assert.ok(dictionary.assistant[`fact_${key}`],`fact_${key}`);
  for(const tool of assistantTools)assert.ok(dictionary.assistant[`tool_${tool}`]);
  for(const category of assistantCategories)assert.ok(dictionary.assistant[`category_${category}`]);
  for(const mode of ['AKAAI','RULES']){assert.ok(dictionary.assistant[`mode_${mode}`]);assert.ok(dictionary.assistant[`source_${mode}`]);}
  assert.ok(dictionary.ui.navAssistant);assert.ok(dictionary.dashboard.askAssistant);
 }
 assert.deepEqual(Object.keys(tr.assistant).sort(),Object.keys(en.assistant).sort());
 for(const tool of assistantTools)assert.ok(assistantToolStyle[tool].icon);
 for(const category of assistantCategories)assert.ok(assistantCategoryStyle[category].icon);
 for(const kind of assistantFactKinds)assert.ok(assistantKindIcon[kind]);
 const constants=Object.fromEntries([...backend.matchAll(/(\w+) = "([A-Z_]+)"/g)].map(m=>[m[1],m[2]]));
 const all=backend.match(/IReadOnlyList<string> All = \[([^\]]+)\]/)[1].split(',').map(name=>constants[name.trim()]).concat(['RESIDENT_BALANCE','RESIDENT_REQUESTS','RESIDENT_NOTICES']);
 assert.deepEqual(all,[...assistantTools]);
 assert.equal(errorCodes.BF_ASSISTANT_BUDGET.status,429);assert.equal(errorCodes.BF_ASSISTANT_PROMPT_DUPLICATE.status,409);
});

test('category dimensions reuse the owning module dictionaries and keep periods as-is',()=>{
 assert.deepEqual(dimensionKey(card({tool:'OPEN_REQUESTS',dimension:'ELEVATOR',href:`/properties/${P}/requests`})),{namespace:'requests',key:'category_ELEVATOR'});
 assert.deepEqual(dimensionKey(card({tool:'BUDGET_VARIANCE',dimension:'ENERGY'})),{namespace:'expenses',key:'ENERGY'});
 assert.deepEqual(dimensionKey(card({tool:'UPCOMING_MAINTENANCE',dimension:'INSPECTION'})),{namespace:'maintenance',key:'type_INSPECTION'});
 assert.equal(dimensionKey(card({dimension:'2026-11'})),null);assert.equal(dimensionKey(card({dimension:'2026'})),null);
 for(const code of ['ELEVATOR','WATER','OTHER'])assert.ok(tr.requests[`category_${code}`]&&en.requests[`category_${code}`]);
 for(const code of ['MAINTENANCE','CLEANING','INSPECTION','DRILL','OTHER'])assert.ok(tr.maintenance[`type_${code}`]&&en.maintenance[`type_${code}`]);
 for(const code of ['MAINTENANCE','CLEANING','STAFF','ENERGY','SECURITY','INSURANCE','ADMINISTRATION','OTHER'])assert.ok(tr.expenses[code]&&en.expenses[code]);
});

test('FAZ 2 cards carry aliases and labels, link only to known screens, and keep the model alias out of display',()=>{
 const R='33333333-3333-4333-8333-333333333333';
 const backlog=card({tool:'REQUEST_BACKLOG',key:'openRequestAge',value:'9',kind:'DAYS',dimension:'ELEVATOR',href:`/properties/${P}/requests?request=${R}`,ref:'T1',label:'12 numaralı bölüm talebi'});
 const parsed=readAnswer(answer({tool:'REQUEST_BACKLOG',sources:[backlog,card({tool:'REQUEST_BACKLOG',key:'requestsByStatus',kind:'COUNT',value:'1',dimension:'RESOLVED',href:`/properties/${P}/requests?status=RESOLVED`})]}),P);
 assert.equal(parsed.sources[0].ref,'T1');assert.equal(parsed.sources[0].label,'12 numaralı bölüm talebi');assert.equal(parsed.sources[1].ref,null);
 assert.deepEqual(dimensionKey(parsed.sources[1]),{namespace:'requests',key:'status_RESOLVED'});
 for(const href of ['/suppliers?view=alerts','/suppliers',`/properties/${P}/surveys?survey=${R}`])
  assert.equal(readAnswer(answer({tool:'SUPPLIER_WATCH',sources:[card({tool:'SUPPLIER_WATCH',href})]}),P).sources[0].href,href);
 for(const href of ['/suppliers?view=all','/suppliers/x',`/properties/${P}/requests?status=DONE`,`/properties/${P}/requests?request=x`,`/properties/${Q}/surveys?survey=${R}`,'//evil.example/suppliers'])
  assert.throws(()=>readAnswer(answer({tool:'SUPPLIER_WATCH',sources:[card({tool:'SUPPLIER_WATCH',href})]}),P),href);
 for(const ref of ['t1','T1000','<b>','PEOPLE'])assert.throws(()=>readAnswer(answer({sources:[card({ref})]}),P));
 assert.equal(readAnswer(answer({tool:'SURVEY_SUMMARY',sources:[card({tool:'SURVEY_SUMMARY',kind:'SCORE',value:'4.25',href:`/properties/${P}/surveys`})]}),P).sources[0].kind,'SCORE');
});

test('announcement draft is read strictly, travels once through session storage and is never marked saved',()=>{
 const draft={title:'Su kesintisi',content:'Değerli sakinlerimiz,\n\nSu kesintisi var.',category:'MAINTENANCE',source:'RULES',modelFallback:false,masked:true,quota,saved:false};
 assert.equal(readDraft(draft).category,'MAINTENANCE');
 for(const bad of [{saved:true},{category:'PARTY'},{title:''},{content:'x'.repeat(2001)},{source:'GPT'}])assert.throws(()=>readDraft({...draft,...bad}));
 assert.deepEqual(draftBody(P,'tr','  not  ','URGENT'),{propertyId:P,note:'not',category:'URGENT',locale:'tr'});
 assert.equal(canDraft(''),false);assert.equal(canDraft('x'.repeat(1001)),false);assert.equal(canDraft('ok\u0007'),false);assert.equal(canDraft('Su kesintisi'),true);
 const store=new Map();const storage={getItem:k=>store.get(k)??null,removeItem:k=>store.delete(k)};
 store.set(draftStorageKey(P),JSON.stringify({title:'T',content:'C',category:'GENERAL'}));
 assert.deepEqual(takeStoredDraft(storage,P),{title:'T',content:'C',category:'GENERAL'});assert.equal(takeStoredDraft(storage,P),null);assert.equal(store.size,0);
 for(const raw of ['{bad',JSON.stringify({title:'',content:'C',category:'GENERAL'}),JSON.stringify({title:'T',content:'C',category:'PARTY'})]){store.set(draftStorageKey(P),raw);assert.equal(takeStoredDraft(storage,P),null);assert.equal(store.size,0);}
 assert.equal(draftEditorHref(P),`/properties/${P}/announcements?assistantDraft=1`);
});

test('deep links accept only the parameters the assistant sets',()=>{
 const R='33333333-3333-4333-8333-333333333333';
 assert.deepEqual(requestLink(`?status=IN_PROGRESS&request=${R}`),{status:'IN_PROGRESS',request:R});
 assert.deepEqual(requestLink('?status=DONE&request=1'),{status:null,request:null});
 assert.deepEqual(requestLink('?request=00000000-0000-0000-0000-000000000000'),{status:null,request:null});
 assert.equal(surveyLink(`?survey=${R}`),R);assert.equal(surveyLink('?survey=../x'),null);
 assert.equal(supplierViewLink('?view=alerts'),'alerts');assert.equal(supplierViewLink('?view=performance'),null);
 assert.equal(announcementDraftLink('?assistantDraft=1'),true);assert.equal(announcementDraftLink('?assistantDraft=true'),false);
});

test('header shortcut opens the assistant for reader roles only and sits in the workspace header',()=>{
 const shell=readFileSync(new URL('../src/components/WorkspaceShell.tsx',import.meta.url),'utf8');
 assert.match(shell,/beforeLanguage=\{<><DashboardAreaPicker\/><AssistantShortcut\/><\/>\}/);
 const shortcut=readFileSync(new URL('../src/features/assistant/AssistantShortcut.tsx',import.meta.url),'utf8');
 assert.match(shortcut,/assistantRoles\.some/);assert.match(shortcut,/href="\/assistant"/);assert.match(shortcut,/aria-label=\{t\("openShortcut"\)\}/);assert.match(shortcut,/aria-hidden="true"/);
 assert.ok(tr.assistant.openShortcut&&en.assistant.openShortcut);
});

test('source cards start collapsed behind an accessible per-answer toggle',()=>{
 const page=readFileSync(new URL('../src/features/assistant/AssistantPage.tsx',import.meta.url),'utf8');
 assert.match(page,/useState\(false\);const panel=`assistant-sources-\$\{id\}`/);
 assert.match(page,/aria-expanded=\{open\} aria-controls=\{panel\}/);assert.match(page,/<div id=\{panel\} hidden=\{!open\}>/);
 assert.match(page,/truncate[^>]*title=/);
 for(const d of [tr,en])for(const k of ['sourcesToggle','sourcesShow','sourcesHide'])assert.ok(d.assistant[k]);
 assert.match(tr.assistant.sourcesToggle,/\{count\}/);
});

test('voice input is management-only, server-transcribed and never sends on its own',()=>{
 assert.deepEqual([...voiceRoles],['SUPER_ADMIN','SITE_ADMIN','MANAGER']);
 assert.deepEqual(readVoiceStatus({enabled:false,reason:'PROVIDER_NOT_CONFIGURED',maxSeconds:60,maxBytes:10485760,acceptedTypes:[]}).reason,'PROVIDER_NOT_CONFIGURED');
 for(const bad of [{enabled:true,reason:'PROVIDER_NOT_CONFIGURED'},{enabled:false,reason:null},{enabled:true,reason:null,maxSeconds:0},{enabled:'yes',reason:null}])
  assert.throws(()=>readVoiceStatus({maxSeconds:60,maxBytes:1,...bad}));
 assert.equal(readTranscript({text:'Borcum ne kadar?',quota:{dailyLimit:30,usedToday:1,remaining:29}}).text,'Borcum ne kadar?');
 assert.throws(()=>readTranscript({text:'x'.repeat(501),quota:{dailyLimit:30,usedToday:1,remaining:29}}));
 assert.equal(mergeTranscript('',' Merhaba '),'Merhaba');assert.equal(mergeTranscript('Bu ay','geciken aidat'),'Bu ay geciken aidat');assert.equal(mergeTranscript('x',''),'x');assert.equal(mergeTranscript('a'.repeat(499),'bcd').length,500);
 assert.equal(pickRecorderType(t=>t==='audio/mp4'),'audio/mp4');assert.equal(pickRecorderType(()=>false),null);assert.equal(pickRecorderType(()=>true),'audio/webm;codecs=opus');
 const form=transcribeForm('11111111-1111-4111-8111-111111111111',new Blob(['x'],{type:'audio/webm'}),1234.6,'tr');
 assert.deepEqual([...form.keys()].sort(),['audio','durationMs','locale','propertyId']);assert.equal(form.get('durationMs'),'1235');
 assert.deepEqual(readVoicePreference(null),{voice:false,readAloud:false});assert.deepEqual(readVoicePreference('{bad'),{voice:false,readAloud:false});assert.deepEqual(readVoicePreference('{"voice":true,"readAloud":true}'),{voice:true,readAloud:true});
 const controls=readFileSync(new URL('../src/features/assistant/VoiceControls.tsx',import.meta.url),'utf8');
 assert.doesNotMatch(controls,/SpeechRecognition|webkitSpeech/);assert.match(controls,/MediaRecorder/);assert.match(controls,/role="switch" aria-checked=\{checked\}/);assert.match(controls,/voiceRoles\.some/);
 assert.doesNotMatch(controls,/\/chat/);
 for(const d of [tr,en])for(const k of ['voiceTitle','readAloudTitle','voiceOn','voiceOff','voiceProviderMissing','voiceListening'])assert.ok(d.assistant[k],k);
});

test('recordings are re-encoded on the device as 16 kHz mono WAV for whisper.cpp',()=>{
 const left=new Float32Array(48000).fill(0.5),right=new Float32Array(48000).fill(-0.5);
 const mono=downmix([left,right],48000);assert.equal(mono.length,16000);assert.ok(Math.abs(mono[100])<1e-6);
 assert.equal(downmix([new Float32Array(32000).fill(0.25)],32000).length,16000);assert.equal(downmix([],48000).length,0);
 const wav=new Uint8Array(encodeWav(new Float32Array([0,1,-1,2])));const ascii=(a,b)=>String.fromCharCode(...wav.slice(a,b));
 assert.equal(ascii(0,4),'RIFF');assert.equal(ascii(8,12),'WAVE');assert.equal(wav.length,44+8);
 const view=new DataView(wav.buffer);assert.equal(view.getUint32(24,true),16000);assert.equal(view.getUint16(22,true),1);assert.equal(view.getInt16(46,true),32767);assert.equal(view.getInt16(48,true),-32768);assert.equal(view.getInt16(50,true),32767);
});
