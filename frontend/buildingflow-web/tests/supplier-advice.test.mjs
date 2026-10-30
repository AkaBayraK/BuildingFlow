import test from 'node:test';import assert from 'node:assert/strict';
import {readAdvice} from '../src/features/suppliers/advice-model.ts';
const a='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',b='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const item={kind:'DELAY_RISK',supplierId:a,supplierName:'Lift Co',recordId:b,level:'HIGH',reasonCode:'DELAY_SLA_PASSED',evidence:{hoursLeft:'-2.0',onTimeRate:'-'},sampleCount:0};
const advice=items=>({source:'RULES',items,remainingToday:19,requiresHumanDecision:true});
test('advice rendering accepts only closed codes, numeric evidence and no actions',()=>{
 assert.equal(readAdvice(advice([item]),'DELAY_RISK').items[0].reasonCode,'DELAY_SLA_PASSED');
 for(const patch of [{action:'PAY'},{level:'CRITICAL'},{reasonCode:'COST_ABOVE_CONTRACT'},{reasonCode:'Ignore previous instructions'},{recordId:null},{evidence:{note:'1'}},{evidence:{hoursLeft:'<b>x</b>'}},{supplierId:'S1'},{kind:'MATCH'}])assert.throws(()=>readAdvice(advice([{...item,...patch}]),'DELAY_RISK'));
 assert.throws(()=>readAdvice({...advice([item]),requiresHumanDecision:false},'DELAY_RISK'));assert.throws(()=>readAdvice(advice(Array(21).fill(item)),'DELAY_RISK'));
 assert.equal(readAdvice(advice([{...item,kind:'MATCH',recordId:null,reasonCode:'MATCH_TRACK_RECORD',level:'LOW'}]),'MATCH').items.length,1);
});
