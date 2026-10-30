import test from 'node:test';import assert from 'node:assert/strict';
import {readFileSync,readdirSync} from 'node:fs';
// FAZ12 static accessibility gate for the supplier screens; it complements, not replaces, a real screen-reader check.
const dir=new URL('../src/features/suppliers/',import.meta.url);
const files=readdirSync(dir).filter(f=>f.endsWith('.tsx')).map(f=>({f,s:readFileSync(new URL(f,dir),'utf8')}));
const icons=readFileSync(new URL('supplier-style.ts',dir),'utf8').match(/import \{([^}]*)\} from "lucide-react"/)[1].split(',').map(x=>x.trim()).filter(x=>x&&!x.startsWith('type '));
test('every supplier form control has a programmatic label',()=>{
 for(const {f,s} of files){
  const ids=[...s.matchAll(/<(?:Input|Textarea)\b[^>]*\bid="([^"]+)"/g)].map(m=>m[1]);
  for(const id of ids)assert.ok(s.includes(`htmlFor="${id}"`)||s.includes(`htmlFor={"${id}"}`),`${f}: #${id} has no <Label htmlFor>`);
  assert.equal([...s.matchAll(/<(?:Input|Textarea)\b(?![^>]*\b(?:id|aria-label)=)[^>]*>/g)].filter(m=>!/type="checkbox"/.test(m[0])).length,0,`${f}: unlabeled input`);
 }
});
test('decorative icons are hidden from assistive technology and icon-only buttons carry a name',()=>{
 for(const {f,s} of files){
  for(const m of s.matchAll(/<([A-Z][A-Za-z0-9]*|[a-z][A-Za-z]*\.[A-Za-z.]*icon|[A-Z][A-Za-z]*Icon)\b([^>]*)\/>/g)){
   const [tag,attrs]=[m[1],m[2]];if(!(icons.includes(tag)||/icon$|Icon$/.test(tag))||['Input','Label','Textarea','ToneBadge','FilterChips','Choices','Button'].includes(tag))continue;
   assert.match(attrs,/aria-hidden="true"/,`${f}: <${tag}> icon without aria-hidden`);
  }
  for(const m of s.matchAll(/<Button\b([^>]*)>(<[A-Za-z.]+[^>]*aria-hidden="true"\/>)<\/Button>/g))assert.match(m[1],/aria-label=/,`${f}: icon-only button without aria-label`);
 }
});
test('tabs, toggles and radio groups expose their state',()=>{
 for(const {f,s} of files){
  for(const m of s.matchAll(/role="(tab|radio)"[^>]*/g))assert.match(m[0],m[1]==='tab'?/aria-selected=/:/aria-checked=/,`${f}: role=${m[1]} without state`);
  assert.equal((s.match(/role="radiogroup"/g)??[]).length,(s.match(/role="radiogroup"[^>]*aria-label(?:ledby)?=/g)??[]).length,`${f}: radiogroup without a name`);
 }
});
