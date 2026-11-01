import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {assistantGate,canAsk,cardValue,chatBody,factLabels,readAnswer,readPrompts,readStatus} from '../src/assistant/contracts.ts';
import {parseList} from '../src/consents/contracts.ts';
const P='11111111-1111-4111-8111-111111111111',U='22222222-2222-4222-8222-222222222222';
const quota={dailyLimit:10,usedToday:1,remaining:9};
const card=(over={})=>({tool:'RESIDENT_BALANCE',key:'myBalance',value:'320.00',kind:'MONEY',dimension:null,from:'2026-11-01',to:'2026-11-01',href:'/finance',ref:null,label:null,...over});
const answer=(over={})=>({status:'ANSWERED',tool:'RESIDENT_BALANCE',source:'RULES',modelFallback:false,answer:'Toplam borcunuz 320,00 TL.',sources:[card()],quota,readOnly:true,...over});

test('the assistant stays hidden unless the server allows it; missing consent offers the consent flow',()=>{
 assert.equal(assistantGate(200,{}),'ready');
 assert.equal(assistantGate(403,{code:'BF_CONSENT_REQUIRED'}),'consent');
 for(const [status,body] of [[403,{code:'BF_ASSISTANT_RESIDENT_DISABLED'}],[403,null],[401,{code:'BF_CONSENT_REQUIRED'}],[500,{}],[404,null]])assert.equal(assistantGate(status,body),'hidden');
 assert.equal(readStatus({enabled:true,mode:'RULES',quota,tools:['RESIDENT_BALANCE'],readOnly:true}).quota.dailyLimit,10);
 for(const bad of [{enabled:false},{readOnly:false},{mode:'GPT'},{quota:{...quota,dailyLimit:0}}])assert.throws(()=>readStatus({enabled:true,mode:'RULES',quota,tools:[],readOnly:true,...bad}));
});

test('answers accept only resident tools, in-app routes and numeric card values',()=>{
 assert.equal(readAnswer(answer()).sources[0].href,'/finance');
 assert.equal(readAnswer(answer({status:'UNSUPPORTED',tool:null,sources:[]})).status,'UNSUPPORTED');
 for(const bad of [answer({tool:'OPEN_REQUESTS'}),answer({sources:[card({href:'/properties/x/dues'})]}),answer({sources:[card({href:'https://evil.example'})]}),answer({sources:[card({value:'Zeynep'})]}),
  answer({sources:[card({tool:'RESIDENT_NOTICES'})]}),answer({readOnly:false}),answer({status:'UNSUPPORTED',tool:null}),answer({answer:'x'.repeat(1201)})])assert.throws(()=>readAnswer(bad));
 assert.equal(cardValue(card()),'320,00 TL');assert.equal(cardValue(card({kind:'DAYS',value:'4'})),'4 gün');
 for(const key of ['myBalance','myOverdue','myNextDueDate','myRequestsByStatus','announcementDate','documentDate'])assert.ok(factLabels[key]);
});

test('prompts and chat bodies stay inside the selected owner or tenant unit',()=>{
 const prompts=readPrompts([{id:'a1a55150-0001-4000-8000-000000000021',code:'MY_BALANCE',category:'RESIDENT',toolKey:'RESIDENT_BALANCE',titleTr:'Borcum',titleEn:'Balance',textTr:'Borcum?',textEn:'Balance?',sortOrder:210,isEnabled:true,version:1}]);
 assert.equal(prompts[0].toolKey,'RESIDENT_BALANCE');
 assert.throws(()=>readPrompts([{code:'OVERDUE_DUES',toolKey:'DUES_COLLECTION',titleTr:'x',textTr:'x',sortOrder:1,isEnabled:true}]));
 assert.deepEqual(chatBody({propertyId:P,unitId:U,role:'OWNER'},{promptCode:'MY_BALANCE'}),{propertyId:P,unitId:U,role:'OWNER',locale:'tr',promptCode:'MY_BALANCE'});
 assert.deepEqual(chatBody({propertyId:P,unitId:U,role:'TENANT'},{message:'  Borcum?  '}),{propertyId:P,unitId:U,role:'TENANT',locale:'tr',message:'Borcum?'});
 for(const context of [{propertyId:P,unitId:null,role:'OWNER'},{propertyId:P,unitId:U,role:'MANAGER'},{propertyId:P,unitId:U,role:'RESIDENT'}])assert.throws(()=>chatBody(context,{promptCode:'MY_BALANCE'}));
 assert.equal(canAsk(''),false);assert.equal(canAsk('x'.repeat(501)),false);assert.equal(canAsk('a\u0000'),false);assert.equal(canAsk('Borcum ne kadar?'),true);
});

test('consent list accepts the AI assistant feature and photo clean-up never runs for it',()=>{
 const source=readFileSync(new URL('../src/consents/store.ts',import.meta.url),'utf8');
 for(const fn of ['releaseHeld','heldCount','dropLocalPhotos'])assert.match(source,new RegExp(`export async function ${fn}\\([^)]*\\)\\{\\n if\\(!photoFeatures\\.includes\\(feature\\)\\)return`));
 assert.equal(typeof parseList,'function');
 const auth=readFileSync(new URL('../src/auth/AuthProvider.tsx',import.meta.url),'utf8');
 assert.match(auth,/assistant\\\/\(\?:status\|prompts\)\$\/\.test\(path\)&&body===undefined/);assert.match(auth,/path==='\/api\/v1\/mobile\/assistant\/chat'&&body!==undefined/);
 const layout=readFileSync(new URL('../src/app/(tabs)/_layout.tsx',import.meta.url),'utf8');assert.match(layout,/name="assistant" options=\{\{href:null/);
});
