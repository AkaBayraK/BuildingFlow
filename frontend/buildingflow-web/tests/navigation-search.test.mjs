import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createNavigation,navigationDuplicates} from '../src/features/navigation/navigation.ts';
import {normalizeNavigationText,searchNavigation} from '../src/features/navigation/search.ts';

const dictionaries=Object.fromEntries(['tr','en'].map(locale=>[locale,JSON.parse(readFileSync(`messages/${locale}.json`,'utf8'))]));
const label=locale=>item=>dictionaries[locale][item.label.namespace][item.label.key];
const P='77777777-7777-4777-8777-777777777777';
const context=(patch={})=>({roles:['SUPER_ADMIN'],sessionContexts:false,centralPeople:false,propertyId:P,property:{id:P,type:'SITE'},...patch});
const ids=items=>items.map(item=>item.id);

test('six semantic groups keep each enabled destination once and remove only the duplicate user placeholder',()=>{
 const m=createNavigation(context(),'/');
 assert.deepEqual(m.groups.map(g=>g.id),['overview','property','finance','operations','communication','administration']);
 assert.deepEqual(m.groups.map(g=>ids(g.items)),[
  ['dashboard'],['properties','overview','buildings','units','people','shared-areas','parking'],
  ['dues','payments','expenses','reports'],['requests','maintenance','meters','suppliers'],
  ['announcements','surveys','documents','website','sms'],['users','consents','definitions','settings']
 ]);
 const all=m.groups.flatMap(g=>g.items);
 assert.deepEqual(navigationDuplicates([...all,m.account,m.home]),[]);
 assert.equal(all.filter(item=>label('tr')(item)==='Kullanıcılar').length,1);
 for(const locale of ['tr','en'])for(const group of m.groups)assert.ok(label(locale)(group));
});

test('Turkish case, accents and Unicode composition match without depending on browser locale',()=>{
 for(const input of ['İLETİŞİM','ILETISIM','ıletışım','İletişim','I\u0307LETİŞİM'])assert.equal(normalizeNavigationText(input,'tr'),'iletisim');
 for(const locale of ['tr','en'])assert.equal(normalizeNavigationText('Iİıi ÇĞÖŞÜ',locale),'iiii cgosu');
 const m=createNavigation(context(),'/');
 for(const query of ['KİŞİLER','KISILER','kışılER','kişi\u0307ler'])assert.deepEqual(ids(searchNavigation(m.searchable,query,label('tr'),'tr')),['people']);
 for(const query of ['AÇIK RIZA','acik riza','açık   rıza'])assert.deepEqual(ids(searchNavigation(m.searchable,query,label('tr'),'tr')),['consents']);
});

test('menu search handles whitespace, unordered terms, empty and no-match states in both locales',()=>{
 const m=createNavigation(context(),'/');
 assert.deepEqual(ids(searchNavigation(m.searchable,' \n\t ',label('tr'),'tr')),ids(m.searchable));
 assert.deepEqual(ids(searchNavigation(m.searchable,'  bütçe   GİDER  ',label('tr'),'tr')),['expenses']);
 assert.deepEqual(ids(searchNavigation(m.searchable,'UNITS',label('en'),'en')),['units']);
 assert.deepEqual(searchNavigation(m.searchable,'no menu can match this',label('en'),'en'),[]);
 assert.deepEqual(searchNavigation(m.searchable,P,label('tr'),'tr'),[],'route and property identifiers are not searched');
});

test('search never exposes finance, people or administration entries hidden by the role/flag matrix',()=>{
 for(const role of ['RESIDENT','ACCOUNTANT','MANAGER','SITE_ADMIN','SUPER_ADMIN'])for(const sessionContexts of [false,true])for(const centralPeople of [false,true]){
  const m=createNavigation(context({roles:[role],sessionContexts,centralPeople}),'/');
  const search=query=>ids(searchNavigation(m.searchable,query,label('tr'),'tr'));
  assert.deepEqual(search('KİŞİLER'),role==='ACCOUNTANT'||role==='RESIDENT'?[]:['people']);
  assert.deepEqual(search('Aidat'),role==='RESIDENT'?[]:['dues']);
  assert.deepEqual(search('Kullanıcılar'),role==='SUPER_ADMIN'||role==='SITE_ADMIN'&&!sessionContexts?['users']:[]);
  assert.deepEqual(search('Tanımlar'),[]);assert.deepEqual(search('Tedarikçiler'),[]);
  assert.ok(m.groups.every(group=>group.items.length>0));
 }
 const resident=createNavigation(context({roles:['RESIDENT']}),'/');
 assert.equal(resident.groups.some(group=>group.id==='finance'||group.id==='administration'),false);
});
