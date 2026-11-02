import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {integrationHubBase,integrationHubHref,integrationHubRoles} from '../src/features/integration-hub/hub-link.ts';
import {createNavigation} from '../src/features/navigation/navigation.ts';
import {isWorkspaceReturnPath} from '../src/auth/return-path.ts';
const context=(roles)=>({roles,sessionContexts:false,centralPeople:false,propertyId:null,property:null});
const find=(m,id)=>m.groups.flatMap(g=>g.items).find(i=>i.id===id);

test('hub base accepts only plain http(s) origins and strips trailing slashes',()=>{
 assert.equal(integrationHubBase('http://localhost:28030/'),'http://localhost:28030');
 assert.equal(integrationHubBase('https://hub.example.test/aih/'),'https://hub.example.test/aih');
 for(const bad of [undefined,'','javascript:alert(1)','ftp://hub','https://u:p@hub.test','https://hub.test/?x=1','https://hub.test/#a','not a url'])assert.equal(integrationHubBase(bad),null,String(bad));
});

test('hub link carries idp, host, locale, theme and a query-free return address only',()=>{
 const href=integrationHubHref({base:'http://localhost:28030',screen:'smtp',locale:'en',theme:'dark',returnUrl:'http://localhost:3003/integrations?code=secret#state=x'});
 const url=new URL(href);
 assert.equal(url.origin+url.pathname,'http://localhost:28030/admin/email/providers');
 assert.equal(url.searchParams.get('idp'),'buildingflow');
 assert.equal(url.searchParams.get('host'),'BuildingFlow');
 assert.equal(url.searchParams.get('locale'),'en');
 assert.equal(url.searchParams.get('theme'),'dark');
 assert.equal(url.searchParams.get('return'),'http://localhost:3003/integrations');
 assert.ok(!href.includes('secret'));
 assert.equal(new URL(integrationHubHref({base:'http://h',screen:'deliveries',locale:'de',theme:'light',returnUrl:'javascript:x'})).searchParams.get('locale'),'tr');
 assert.equal(new URL(integrationHubHref({base:'http://h',screen:'deliveries',locale:'tr',theme:'light',returnUrl:'javascript:x'})).searchParams.get('return'),null);
 assert.equal(integrationHubHref({base:null,screen:'smtp',locale:'tr',theme:'light',returnUrl:''}),null);
});

test('integrations menu is visible to manager roles in administration and survives OIDC return',()=>{
 for(const role of ['SUPER_ADMIN','SITE_ADMIN','MANAGER']){const item=find(createNavigation(context([role]),'/integrations'),'integrations');assert.equal(item.href,'/integrations');assert.equal(item.icon,'Plug');assert.equal(item.active,true);}
 for(const role of ['ACCOUNTANT','SECURITY','RESIDENT'])assert.equal(find(createNavigation(context([role]),'/'),'integrations'),undefined,role);
 assert.deepEqual([...integrationHubRoles],['SUPER_ADMIN','SITE_ADMIN','MANAGER']);
 assert.equal(isWorkspaceReturnPath('/integrations'),true);
 for(const p of ['/integrations/x','/integrations?return=https://evil'])assert.equal(isWorkspaceReturnPath(p),false,p);
});

test('integration hub texts exist in Turkish and English with identical keys',()=>{
 const [tr,en]=['tr','en'].map(l=>JSON.parse(readFileSync(new URL(`../messages/${l}.json`,import.meta.url))));
 const keys=o=>Object.entries(o).flatMap(([k,v])=>typeof v==='object'?keys(v).map(x=>`${k}.${x}`):[k]).sort();
 assert.deepEqual(keys(tr.integrationHub),keys(en.integrationHub));
 assert.ok(tr.ui.navIntegrations&&en.ui.navIntegrations);
});
