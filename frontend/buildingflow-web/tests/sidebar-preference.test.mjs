import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readSidebarPreference,writeSidebarPreference,sidebarPreferenceKey} from '../src/features/navigation/sidebar-preference.ts';

test('sidebar preference accepts only the fixed collapsed value and defaults safely',()=>{
 for(const value of [null,'','true','false','expanded','{"collapsed":true}','invalid'])assert.equal(readSidebarPreference({getItem:key=>{assert.equal(key,sidebarPreferenceKey);return value;}}),false);
 assert.equal(readSidebarPreference({getItem:()=> 'collapsed'}),true);
 assert.equal(readSidebarPreference({getItem:()=>{throw new Error('Storage unavailable');}}),false);
});
test('sidebar preference writes only a versioned UI value, even when storage fails',()=>{
 const calls=[];const storage={setItem:(...args)=>calls.push(args)};
 writeSidebarPreference(storage,true);writeSidebarPreference(storage,false);
 assert.deepEqual(calls,[[sidebarPreferenceKey,'collapsed'],[sidebarPreferenceKey,'expanded']]);
 assert.doesNotThrow(()=>writeSidebarPreference({setItem:()=>{throw new Error('Quota');}},true));
});
