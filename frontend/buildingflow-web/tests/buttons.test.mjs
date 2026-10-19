import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync,readdirSync,statSync} from 'node:fs';
import {join} from 'node:path';
// İŞ19 soft button standard (option A): tonal variants by meaning, no white action buttons, solid red only for the final confirm.
const files=d=>readdirSync(d).flatMap(n=>{const p=join(d,n);return statSync(p).isDirectory()?(p.endsWith('components/ui')?[]:files(p)):p.endsWith('.tsx')?[p]:[];});
const buttons=()=>files('src').flatMap(f=>[...readFileSync(f,'utf8').matchAll(/<Button\b([^>]*)>([\s\S]*?)<\/Button>/g)].map(m=>({f,attrs:m[1],body:m[2]})));
test('button component defines the tonal variants from the shared tokens',()=>{
 const src=readFileSync('src/components/ui/button.tsx','utf8'),css=readFileSync('src/app/globals.css','utf8');
 for(const v of ['view','edit','neutral','warn','danger','field'])assert.match(src,new RegExp(`\\n\\s+${v}:`),v);
 for(const k of ['view','edit','neutral','warn','danger'])for(const p of ['bg','hover','border','text'])assert.match(css,new RegExp(`--action-${k}-${p}:#`),`${k}-${p}`);
 assert.doesNotMatch(src.match(/outline:\s*"[^"]*"/)[0],/bg-background/,'outline is grey tonal, not white');
});
test('screens map buttons to the variant of their meaning',()=>{
 const all=buttons();assert.ok(all.length>150);
 const wrong=(re,ok)=>all.filter(b=>re.test(b.body)&&/variant="/.test(b.attrs)&&!ok.some(v=>b.attrs.includes(`variant="${v}"`))).map(b=>`${b.f}: ${b.body.slice(0,60)}`);
 assert.deepEqual(wrong(/^\s*(\{[^}]*\})?<(RefreshCw|Eye|History)\b/,['view','default']),[]);
 assert.deepEqual(wrong(/^\s*<(Pencil|UserPen)\b/,['edit','default']),[]);
 assert.deepEqual(wrong(/^\s*<(Trash2|Unlink)\b/,['danger','destructive']),[]);
 assert.deepEqual(wrong(/(tryAgain)/,['view','default']),[]);
 assert.deepEqual(all.filter(b=>/variant="secondary"/.test(b.attrs)).map(b=>b.f),[],'secondary is not used for buttons');
});
test('account page: remove photo and withdraw are tonal, only the confirm dialog stays solid red',()=>{
 const s=readFileSync('src/features/account/AccountPage.tsx','utf8');
 assert.match(s,/variant="danger"[^<]*onClick=\{\(\)=>setRemoving\(true\)\}/);
 assert.match(s,/variant="danger"[^<]*withdraw\(c\)/);
 assert.match(s,/variant="warn"[^<]*UPDATE_PASSWORD/);
 assert.equal((s.match(/variant="destructive"/g)||[]).length,1);
});
