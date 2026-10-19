import {test} from "node:test";
import assert from "node:assert/strict";
import {readFileSync,readdirSync} from "node:fs";
import ts from "typescript";
import {parse} from "@formatjs/icu-messageformat-parser";
import {createTranslator} from "next-intl";
import {problemDetails,connectionError} from "../src/services/problem.ts";
import {translateUiText} from "../src/i18n/translate-text.ts";
import {resolveLocale,isLocale} from "../src/i18n/locale.ts";
import {formatDate,formatNumber,formatCurrency,formatPercent} from "../src/lib/format.ts";
import {isWorkspaceReturnPath} from "../src/auth/return-path.ts";
import {propertyIdFromPath,managementSection} from "../src/features/navigation/routes.ts";
import {messageSemantics,kindForStatus} from "../src/components/feedback/message-types.ts";
import {milestones} from "../src/features/roadmap/milestones.ts";
const catalogs=Object.fromEntries(['tr','en'].map(locale=>[locale,JSON.parse(readFileSync(new URL(`../messages/${locale}.json`,import.meta.url)))]));
function argumentsIn(ast){const values=new Set();function walk(nodes){for(const n of nodes){if([1,2,3,4,5,6].includes(n.type))values.add(n.value);if(n.options)for(const option of Object.values(n.options))walk(option.value);if(n.children)walk(n.children);}}walk(ast);return [...values].sort();}
test('TR/EN dictionaries have equal keys and ICU arguments; all translations can render',()=>{
 assert.deepEqual(Object.keys(catalogs.tr),Object.keys(catalogs.en));
 for(const ns of Object.keys(catalogs.tr)){
  assert.deepEqual(Object.keys(catalogs.tr[ns]).sort(),Object.keys(catalogs.en[ns]).sort(),ns);
  const translators=Object.fromEntries(['tr','en'].map(locale=>[locale,createTranslator({locale,messages:catalogs[locale][ns],onError:error=>{throw error;}})]));
  for(const key of Object.keys(catalogs.tr[ns])){
   const trArgs=argumentsIn(parse(catalogs.tr[ns][key]));assert.deepEqual(trArgs,argumentsIn(parse(catalogs.en[ns][key])),`${ns}.${key}`);
   const values=Object.fromEntries(trArgs.map(arg=>[arg,/^(count|total|completed|max|from|to|floors|units|buildings)$/.test(arg)?2:'Example']));
   for(const locale of ['tr','en'])assert.ok(translators[locale](key,values),`${locale}.${ns}.${key}`);
  }
 }
});
test('existing screens, metadata and roadmap use available message keys with required parameters',()=>{
 function walk(dir){for(const entry of readdirSync(dir,{withFileTypes:true})){const url=new URL(entry.name+(entry.isDirectory()?'/':''),dir);if(entry.isDirectory())walk(url);else if(entry.name.endsWith('.tsx')){
  const code=readFileSync(url,'utf8');const namespaces=new Map([["t","ui"]]);const sf=ts.createSourceFile(url.pathname,code,ts.ScriptTarget.Latest,true,ts.ScriptKind.TSX);
  function collect(node){if(ts.isVariableDeclaration(node)&&ts.isIdentifier(node.name)&&node.initializer){const init=ts.isAwaitExpression(node.initializer)?node.initializer.expression:node.initializer;if(ts.isCallExpression(init)&&["useTranslations","getTranslations"].includes(init.expression.getText(sf))&&ts.isStringLiteral(init.arguments[0]))namespaces.set(node.name.text,init.arguments[0].text);};ts.forEachChild(node,collect);}collect(sf);
  function visit(node){if(ts.isCallExpression(node)&&namespaces.has(node.expression.getText(sf))&&ts.isStringLiteral(node.arguments[0])){const ns=namespaces.get(node.expression.getText(sf)),key=node.arguments[0].text;assert.ok(key in catalogs.tr[ns],`${url.pathname}: ${ns}.${key}`);const params=argumentsIn(parse(catalogs.tr[ns][key]));if(params.length){assert.ok(node.arguments[1],`${key} requires ${params}`);if(ts.isObjectLiteralExpression(node.arguments[1])){const provided=node.arguments[1].properties.map(p=>p.name?.getText(sf));for(const param of params)assert.ok(provided.includes(param),`${key} missing ${param}`);}}}ts.forEachChild(node,visit);}visit(sf);
 }} }walk(new URL('../src/',import.meta.url));
 for(const locale of ['tr','en']){
  const t=createTranslator({locale,messages:catalogs[locale].ui,onError:error=>{throw error;}});
  for(const milestone of milestones){for(const key of [milestone.title,milestone.description,...milestone.tasks.flatMap(task=>[task.title,task.detail,...(task.verification?[task.verification]:[])])]){
   assert.ok(key in catalogs[locale].ui,key);
   assert.deepEqual(argumentsIn(parse(catalogs[locale].ui[key])),[],`${locale}.${key}: roadmap renders without arguments`);
   assert.ok(t(key),`${locale}.${key}`);
  }}
  assert.match(t('phase19Task2Detail'),/\/users\/\{id\}\/photo/);
  assert.match(t('phase19Task2Detail'),/\/people\/\{id\}\/photo/);
 }
});
test('locale validation accepts only supported languages and defaults safely',()=>{for(const value of ['en','tr'])assert.ok(isLocale(value));for(const value of [null,'EN','de','../en','en;path=/']){assert.equal(isLocale(value),false);assert.equal(resolveLocale(value),'tr');}});
test('ProblemDetails handles malformed external data and never displays unknown exceptions',()=>{
 for(const body of [null,[],{title:'SQL connection: secret',errors:'bad'},{errors:{name:'secret',__proto__:['secret']}}, {errors:{unexpected:['secret']}}]){
  assert.equal(problemDetails(400,body,'en').message,'Check the fields and try again.');assert.deepEqual(problemDetails(400,body,'en').errors,{});
 }
 for(const title of ['SQL connection: secret','__proto__','constructor','toString'])assert.equal(problemDetails(409,{title},'en').message,'This record has changed. Load the current record and try again.');
 assert.deepEqual(problemDetails(400,{errors:{Name:['SQL connection: secret']}},'en').errors,{name:['Enter a valid value for this field.']});
});
test('known domain conflicts and field errors are translated; HTTP semantics are preserved',()=>{
 assert.equal(problemDetails(409,{title:'Bu sitede aynı blok adı zaten var.'},'en').message,'A building with this name already exists in this property.');
 assert.deepEqual(problemDetails(400,{errors:{name:['Yönetim alanı adı en fazla 200 karakter olabilir.']}},'en').errors,{name:['Property name must be at most 200 characters.']});
 assert.deepEqual(problemDetails(400,{errors:{email:['Geçerli bir e-posta adresi girin.']}},'en').errors,{email:['Enter a valid email address.']});
 for(const status of [401,403,404,400,409,500])assert.notEqual(problemDetails(status,null,'en').message,problemDetails(status,null,'tr').message);
 assert.match(connectionError('en'),/Could not reach/);assert.equal(kindForStatus(409),'warning');assert.equal(kindForStatus(500),'error');
 assert.equal(new Set(Object.values(messageSemantics).map(value=>value.icon)).size,4);assert.equal(messageSemantics.error.role,'alert');assert.equal(messageSemantics.error.live,'assertive');assert.equal(messageSemantics.success.live,'polite');
});
test('date, number, currency and percentage displays follow locale without changing values',()=>{
 assert.equal(formatNumber(1234.5,'tr'),'1.234,5');assert.equal(formatNumber(1234.5,'en'),'1,234.5');assert.match(formatCurrency(12.5,'tr'),/12,50/);assert.match(formatCurrency(12.5,'en'),/12.50/);assert.equal(formatPercent(0.18,'tr'),'%18');assert.equal(formatPercent(0.18,'en'),'18%');assert.equal(formatDate('invalid','en'),'—');
});
test('separate menu routes survive OIDC returns and reject arbitrary suffixes',()=>{
 const property='9b962f3e-8812-459f-a42f-45ec6e3c6eba',building='24d39d57-021b-4705-87d3-98a091c95f23';
 for(const path of ['/units','/people'])assert.equal(isWorkspaceReturnPath(path),true,path);
 for(const path of ['/units/extra','/people/extra','/people?token=secret','/units#token=secret','//example.com/people','https://example.com/units'])assert.equal(isWorkspaceReturnPath(path),false,path);
 for(const suffix of ['/buildings','/units',`/buildings/${building}/units`,`/people/${building}`])assert.equal(isWorkspaceReturnPath(`/properties/${property}${suffix}`),true,suffix);
 for(const suffix of ['/people/no-id','/units/extra',`/buildings/${building}/units/extra`,'/units?token=secret'])assert.equal(isWorkspaceReturnPath(`/properties/${property}${suffix}`),false,suffix);
 assert.equal(propertyIdFromPath(`/properties/${property}/units`),property);assert.equal(propertyIdFromPath('/properties/invalid/units'),null);assert.equal(managementSection(`/properties/${property}/buildings/${building}/units`,property),'units');assert.equal(managementSection(`/properties/${property}/people/${building}`,property),'people');
});

test('visible feedback follows a changed language while preserving user names',()=>{
 assert.equal(translateUiText('Şehir zorunludur.','en'),'City is required.');
 assert.equal(translateUiText('City is required.','tr'),'Şehir zorunludur.');
 assert.equal(translateUiText('BF Test oluşturuldu.','en'),'BF Test created.');
 assert.equal(translateUiText('Geçerli bir e-posta adresi girin.','en'),'Enter a valid email address.');
});

test('login locale preference is shared and rejects unsupported external values',async()=>{
 const {resolvePreferredLocale}=await import('../src/i18n/locale.ts');
 assert.equal(resolvePreferredLocale('en','tr'),'en');
 assert.equal(resolvePreferredLocale('tr','en'),'tr');
 for(const invalid of ['de','EN','en;token=x',null,{},undefined])assert.equal(resolvePreferredLocale(invalid,'en'),'en');
 assert.equal(resolvePreferredLocale(undefined,undefined),'tr');
});
