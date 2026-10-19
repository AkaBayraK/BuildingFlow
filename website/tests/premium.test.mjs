import {test} from 'node:test';
import assert from 'node:assert/strict';
import {themeAssets,siteAssets,managementHref} from '../src/lib/theme-assets.ts';
import {demoSite} from '../src/lib/demo.ts';
import {readSite} from '../src/lib/public-site.ts';
test('preview is explicitly demo and matches the public contract',()=>{assert.equal(demoSite.isDemo,true);assert.equal(readSite(demoSite,demoSite.siteKey).pages.length,1);assert.ok(demoSite.pages[0].sections.some(s=>s.type==='GALLERY'))});
test('fictional assets never bleed to a published tenant',()=>{assert.equal(themeAssets('another-site').hero,undefined);assert.equal(themeAssets('another-site',true).hero,'/images/residence.webp')});
test('tenant assets accept only local named images',()=>{siteAssets['test-one']={hero:'/images/residence.webp',logo:'https://tracker.example/logo.png'};assert.equal(themeAssets('test-one').hero,'/images/residence.webp');assert.equal(themeAssets('test-one').logo,undefined);assert.equal(themeAssets('test-two').hero,undefined);delete siteAssets['test-one']});
test('management link only uses configured origin with a fixed login path',()=>{assert.equal(managementHref(undefined),'http://localhost:3003/login');assert.equal(managementHref('https://management.example'),'https://management.example/login');for(const v of ['javascript:alert(1)','https://user:pass@example.com','https://example.com/path','https://example.com?next=evil'])assert.equal(managementHref(v),null)});
