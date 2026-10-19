import {test} from 'node:test';
import assert from 'node:assert/strict';
import {themeAssets,siteAssets,managementHref} from '../src/lib/theme-assets.ts';
import {demoSite} from '../src/lib/demo.ts';
import {readSite} from '../src/lib/public-site.ts';
test('preview is explicitly demo and matches the public contract',()=>{assert.equal(demoSite.isDemo,true);assert.equal(readSite(demoSite,demoSite.siteKey).pages.length,1);assert.ok(demoSite.pages[0].sections.some(s=>s.type==='GALLERY'))});
test('fictional assets never bleed to a published tenant',()=>{assert.equal(themeAssets('another-site').hero,undefined);assert.equal(themeAssets('another-site',true).hero,'/images/residence.webp')});
test('tenant assets accept only local named images',()=>{siteAssets['test-one']={hero:'/images/residence.webp',logo:'https://tracker.example/logo.png'};assert.equal(themeAssets('test-one').hero,'/images/residence.webp');assert.equal(themeAssets('test-one').logo,undefined);assert.equal(themeAssets('test-two').hero,undefined);delete siteAssets['test-one']});
test('management link only uses configured origin with a fixed entry path',()=>{assert.equal(managementHref(undefined),'http://localhost:3003/');assert.equal(managementHref('https://management.example'),'https://management.example/');for(const v of ['javascript:alert(1)','https://user:pass@example.com','https://example.com/path','https://example.com?next=evil'])assert.equal(managementHref(v),null)});

test('site login keeps only a validated site and audience on the configured origin',()=>{
 assert.equal(managementHref('https://management.example','residence-one'),'https://management.example/website-entry/residence-one/all');
 assert.equal(managementHref(undefined,'residence-one','resident'),'http://localhost:3003/website-entry/residence-one/resident');
 for(const key of ['//evil','../x','x?return=https://evil','UPPER','ab','site#token'])assert.equal(managementHref(undefined,key),null);
 for(const origin of ['http://management.example','https://example.test/path','https://user@example.test','https://example.test?next=x'])assert.equal(managementHref(origin,'site-one'),null);
 assert.equal(managementHref(undefined,'site-one','SUPER_ADMIN'),null);
});
