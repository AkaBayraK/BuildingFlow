import test from 'node:test';
import assert from 'node:assert/strict';
import {toCsv} from '../src/features/late-penalties/model.ts';

// QA FAZ 08/13: CSV exports open in Excel (BOM, ';', CRLF), keep signed amounts numeric and still neutralise formulas.
test('ledger and penalty exports keep Excel-compatible structure and numeric signed amounts',()=>{
 const csv=toCsv([['Tarih','Daire','Tür','Tutar','Makbuz'],['2026-10-28','A-1','Tahsilat','500.00','BF-2026-000001'],['2026-10-29','A-1','İade','-500.00','']]);
 assert.ok(csv.startsWith('﻿'));
 const lines=csv.slice(1).split('\r\n');
 assert.equal(lines.length,3);
 assert.deepEqual(lines[2].split(';'),['2026-10-29','A-1','İade','-500.00','']);
 assert.equal(lines[1].split(';')[3],'500.00');
 assert.ok(!csv.includes('\n\n'));
});
test('formula-like cells are neutralised while decimals with comma stay numbers',()=>{
 const [row]=toCsv([['=HYPERLINK("x")','+1+1','-1+1','@SUM(A1)','-12,50','\tx']]).slice(1).split('\r\n');
 assert.equal(row,`"'=HYPERLINK(""x"")";'+1+1;'-1+1;'@SUM(A1);-12,50;'\tx`);
});
