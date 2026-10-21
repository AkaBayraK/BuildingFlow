import test from 'node:test';import assert from 'node:assert/strict';import {readFileSync} from 'node:fs';
import {readSummary,readDetail,readPage,readOverview,readResults,participation,surveyIssues,surveyBody,retype,move,templateForm,surveyTemplates,csvCell,resultsCsv,surveyStates,surveyCategories,limits} from '../src/features/surveys/model.ts';
import {categoryStyle,stateTone,stateIcon} from '../src/features/surveys/category-style.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';import {managementSection} from '../src/features/navigation/routes.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
const p='9b962f3e-8812-459f-a42f-45ec6e3c6eba',id='24d39d57-021b-4705-87d3-98a091c95f23',id2='0f6a6b3c-9d1e-4c6a-8b8e-6a1c2d3e4f50',q1='5d0b8a0e-1c2f-4b7a-9e3d-2f6a7b8c9d01',o1='6e1c9b1f-2d3a-4c8b-8f4e-3a7b8c9d0e12',o2='7f2dac20-3e4b-4d9c-9a5f-4b8c9d0e1f23';
const survey={id,propertyId:p,title:'Hizmet Memnuniyeti',description:null,category:'SERVICE',startDate:'2026-10-01',endDate:'2026-10-31',audience:'ALL_RESIDENTS',responseMode:'PER_UNIT',isAnonymous:false,status:'PUBLISHED',state:'ACTIVE',responseCount:12,eligibleCount:48,questionCount:1,publishedAt:'2026-10-01T08:00:00Z',closedAt:null,version:2,createdAt:'2026-09-30T10:00:00Z',modifiedAt:'2026-10-01T08:00:00Z'};
const question={id:q1,position:1,text:'Temizlikten memnun musunuz?',type:'SINGLE_CHOICE',isRequired:true,options:[{id:o1,position:1,text:'Evet'},{id:o2,position:2,text:'Hayır'}]};
const zero=keys=>keys.map(key=>({key,count:0}));
test('survey DTO rejects foreign scope, impossible states and broken participation',()=>{
 assert.deepEqual(readSummary(survey,p),survey);assert.equal(participation(survey),25);assert.equal(participation({responseCount:0,eligibleCount:0}),0);
 for(const v of [{...survey,propertyId:id2},{...survey,state:'DRAFT'},{...survey,status:'CLOSED'},{...survey,category:'PETS'},{...survey,endDate:'2026-09-30'},{...survey,startDate:'2026-02-30'},{...survey,title:' '},{...survey,title:'x'.repeat(201)},{...survey,responseCount:49},{...survey,questionCount:31},{...survey,version:0},{...survey,audience:'GUESTS'}])assert.throws(()=>readSummary(v,p));
 assert.equal(readDetail({survey,questions:[question]},p).questions.length,1);
 for(const qs of [[],[{...question,position:2}],[{...question,options:[question.options[0]]}],[{...question,type:'TEXT'}]])assert.throws(()=>readDetail({survey,questions:qs},p));
});
test('pages and overview need every state/category count in order and consistent totals',()=>{
 const page={today:'2026-10-21',items:[survey],page:1,pageSize:10,totalCount:1,stateCounts:zero(surveyStates),categoryCounts:zero(surveyCategories)};
 assert.equal(readPage(page,p).items.length,1);
 for(const v of [{...page,items:[survey,survey],totalCount:2},{...page,stateCounts:zero(surveyStates).slice(1)},{...page,categoryCounts:zero([...surveyCategories].reverse())},{...page,pageSize:101},{...page,today:'21.10.2026'}])assert.throws(()=>readPage(v,p));
 const cats=surveyCategories.map(key=>({key,count:key==='SERVICE'?1:0}));
 const o={today:'2026-10-21',total:1,createdThisMonth:0,drafts:0,active:1,completed:0,expired:0,responses:12,eligible:48,completedResponses:0,completedEligible:0,categories:cats,topParticipation:[survey],lastCompleted:null};
 assert.equal(readOverview(o,p).active,1);
 for(const v of [{...o,total:2},{...o,responses:-1},{...o,topParticipation:[survey,survey,survey,survey]}])assert.throws(()=>readOverview(v,p));
});
test('results stay hidden without data and anonymous texts never carry a unit',()=>{
 const r={survey,hidden:false,minAnonymousResults:3,questions:[{id:q1,position:1,text:'Yorum',type:'TEXT',answered:1,options:[],ratingAverage:null,ratingCounts:[],texts:[{text:'Güzel',unitNumber:'A-1',submittedOn:'2026-10-02'}]}]};
 assert.equal(readResults(r,p).questions.length,1);
 assert.throws(()=>readResults({...r,hidden:true},p));
 assert.throws(()=>readResults({...r,survey:{...survey,isAnonymous:true}},p));
 assert.throws(()=>readResults({...r,questions:[{...r.questions[0],type:'RATING'}]},p));
});
test('form validation mirrors the server and the body trims text',()=>{
 const q={key:'q',text:' Soru ',type:'SINGLE_CHOICE',isRequired:true,options:['Evet','Hayır']};
 const f={title:' Anket ',description:'',category:'GENERAL',startDate:'2026-10-21',endDate:'2026-10-31',audience:'OWNERS',responseMode:'PER_ACCOUNT',isAnonymous:true,questions:[q]};
 assert.deepEqual(surveyIssues(f,true),[]);assert.deepEqual(surveyIssues({...f,questions:[]}),[]);assert.deepEqual(surveyIssues({...f,questions:[]},true),['questions']);
 assert.deepEqual(surveyIssues({...f,title:' ',endDate:'2026-10-20'}),['title','dates']);
 for(const options of [['Evet'],['Evet','evet'],['Evet',' '],Array.from({length:11},(_,i)=>'S'+i)])assert.deepEqual(surveyIssues({...f,questions:[{...q,options}]}),['question:0']);
 assert.deepEqual(surveyIssues({...f,questions:Array.from({length:limits.questions+1},()=>q)}).slice(0,1),['questions']);
 const body=surveyBody({...f,questions:[q,{...q,type:'RATING',options:['x']}]},3);
 assert.equal(body.title,'Anket');assert.equal(body.description,null);assert.equal(body.questions[0].text,'Soru');assert.deepEqual(body.questions[1].options,[]);assert.equal(body.version,3);
 assert.deepEqual(retype({...q,type:'TEXT',options:[]},'MULTIPLE_CHOICE').options,['','']);assert.deepEqual(retype(q,'RATING').options,[]);
 assert.deepEqual(move([1,2,3],0,2),[2,3,1]);assert.deepEqual(move([1,2,3],0,-1),[1,2,3]);
});
test('CSV export neutralises formulas and quotes every cell',()=>{
 for(const s of ['=1+1','+90','-2','@SUM(A1)','\tx'])assert.ok(csvCell(s).startsWith(`"'`),s);
 assert.equal(csvCell('a"b'),'"a""b"');assert.equal(csvCell(5),'"5"');
 const r={survey,hidden:false,minAnonymousResults:3,questions:[{id:q1,position:1,text:'=HYPERLINK("x")',type:'SINGLE_CHOICE',answered:1,options:[{id:o1,text:'Evet',count:1}],ratingAverage:null,ratingCounts:[],texts:[]}]};
 const csv=resultsCsv(r,{question:'Q',type:'T',answer:'A',count:'C',unit:'U',date:'D',types:{SINGLE_CHOICE:'Tek',MULTIPLE_CHOICE:'Çok',RATING:'Puan',TEXT:'Metin'},rating:n=>String(n)});
 assert.ok(csv.startsWith('﻿"Q";'));assert.ok(csv.includes(`"'=HYPERLINK(""x"")";"Tek";"Evet";"1"`));
});
test('templates, categories and states have TR/EN texts and valid forms',()=>{
 for(const locale of ['tr','en']){const s=JSON.parse(readFileSync(new URL(`../messages/${locale}.json`,import.meta.url))).surveys;const label=k=>{assert.equal(typeof s[k],'string',`${locale}:${k}`);assert.ok(s[k].trim(),`${locale}:${k}`);return s[k];};
  for(const tpl of surveyTemplates){const f=templateForm(tpl,'2026-10-21',label);assert.deepEqual(surveyIssues(f,true),[],tpl.id);assert.equal(f.endDate>f.startDate,true);}
  for(const c of surveyCategories)label(`category_${c}`);for(const st of surveyStates)label(`state_${st}`);}
 for(const c of surveyCategories)assert.match(categoryStyle[c].color,/^#[0-9a-f]{6}$/);
 assert.equal(new Set(surveyCategories.map(c=>categoryStyle[c].color)).size,surveyCategories.length);
 for(const st of surveyStates)assert.ok(stateTone[st]);
 for(const locale of ['tr','en']){const m=JSON.parse(readFileSync(new URL(`../messages/${locale}.json`,import.meta.url)));for(const c of ['BF_SURVEY_INVALID','BF_SURVEY_QUESTION_INVALID','BF_SURVEY_ANSWER_INVALID','BF_SURVEY_STATE','BF_SURVEY_ALREADY_ANSWERED','BF_SURVEY_NOT_OPEN'])assert.ok(m.apiErrors[c],`${locale}:${c}`);}
});
test('survey route, OIDC return allowlist and menu are manager-only',()=>{
 assert.ok(isWorkspaceReturnPath('/surveys'));assert.ok(isWorkspaceReturnPath('/properties/'+p+'/surveys'));assert.equal(managementSection('/properties/'+p+'/surveys',p),'surveys');
 assert.equal(isWorkspaceReturnPath('/properties/'+p+'/surveys?token=x'),false);
 for(const [role,visible] of [['SUPER_ADMIN',true],['SITE_ADMIN',true],['MANAGER',true],['ACCOUNTANT',false],['RESIDENT',false]]){
  const model=createNavigation({roles:[role],sessionContexts:false,centralPeople:false,propertyId:p,property:{id:p,type:'SITE'}},'/properties/'+p+'/surveys');
  const item=model.groups.flatMap(g=>g.items).find(i=>i.id==='surveys');assert.equal(!!item,visible,role);
  if(item){assert.equal(item.href,'/properties/'+p+'/surveys');assert.ok(item.active);}
 }
});
test('status tabs, state badges and every shared filter chip carry an icon',()=>{
 for(const st of surveyStates)assert.ok(stateIcon[st],st);
 assert.equal(new Set(surveyStates.map(st=>stateIcon[st])).size,surveyStates.length);
 for(const c of surveyCategories)assert.ok(categoryStyle[c].icon,c);
 const chips=readFileSync(new URL('../src/components/management/FilterChips.tsx',import.meta.url),'utf8');
 assert.match(chips,/icon:LucideIcon;/);assert.doesNotMatch(chips,/icon\?:/);
 const ws=readFileSync(new URL('../src/features/surveys/SurveysWorkspace.tsx',import.meta.url),'utf8');
 assert.match(ws,/icon:v\?stateIcon\[v\]:LayoutGrid/);
});
