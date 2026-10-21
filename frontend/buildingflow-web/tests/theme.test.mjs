import {test} from 'node:test';import assert from 'node:assert/strict';import vm from 'node:vm';
import {THEME_KEY,parseThemePreference,resolveTheme,readThemePreference,writeThemePreference,themeBootstrap} from '../src/features/theme/theme.ts';
test('theme preference supports three values, system follows OS, explicit choice overrides OS',()=>{
 for(const mode of ['light','dark','system'])assert.equal(parseThemePreference(mode),mode);
 for(const invalid of [null,undefined,'invalid','',{},'<script>'])assert.equal(parseThemePreference(invalid),'system');
 assert.equal(resolveTheme('system',true),'dark');assert.equal(resolveTheme('system',false),'light');assert.equal(resolveTheme('light',true),'light');assert.equal(resolveTheme('dark',false),'dark');
});
test('theme storage persists only reviewed enum values and tolerates blocked/corrupt storage',()=>{
 const values=new Map(),storage={getItem:k=>values.get(k)??null,setItem:(k,v)=>values.set(k,v)};assert.equal(readThemePreference(storage),'system');assert.equal(writeThemePreference(storage,'dark'),true);assert.equal(values.get(THEME_KEY),'dark');assert.equal(readThemePreference(storage),'dark');
 assert.equal(writeThemePreference(storage,'not-a-theme'),false);assert.equal(values.get(THEME_KEY),'dark');values.set(THEME_KEY,'corrupt');assert.equal(readThemePreference(storage),'system');
 const blocked={getItem(){throw Error('Blocked')},setItem(){throw Error('Blocked')}};assert.equal(readThemePreference(blocked),'system');assert.equal(writeThemePreference(blocked,'light'),false);
});
test('pre-paint bootstrap and runtime preference agree without evaluating stored input',()=>{
 for(const value of ['light','dark','system',null,'corrupt',"dark\";throw new Error('injection');//"])for(const systemDark of [false,true]){
  const classes=new Set(),root={dataset:{},style:{},classList:{toggle(name,on){on?classes.add(name):classes.delete(name);}}};
  vm.runInNewContext(themeBootstrap,{document:{documentElement:root},localStorage:{getItem:()=>value},matchMedia:()=>({matches:systemDark})});
  const expected=resolveTheme(parseThemePreference(value),systemDark);assert.equal(root.dataset.theme,expected);assert.equal(root.style.colorScheme,expected);assert.equal(classes.has('dark'),expected==='dark');
 }
 const root={dataset:{},style:{},classList:{toggle(){}}};vm.runInNewContext(themeBootstrap,{document:{documentElement:root},localStorage:{getItem(){throw Error('Blocked')}},matchMedia:()=>({matches:true})});assert.equal(root.dataset.theme,'dark');
});
