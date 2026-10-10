import {test} from "node:test";
import assert from "node:assert/strict";
import {collectUnitChoices,mergeUnitSelection,UnitSelectionError} from "../src/features/dues/unit-selection.ts";
const unit=number=>({id:`10000000-0000-0000-0000-${String(number).padStart(12,"0")}`,buildingId:null,number:String(number),buildingName:null,isEnabled:true});
const choices=count=>Array.from({length:count},(_,i)=>unit(i+1));
const page=(items,number)=>({items:items.slice((number-1)*100,number*100),page:number,pageSize:100,totalCount:items.length});
test("all matching units are selected across every page up to the 500-unit limit",async()=>{
 for(const count of [48,248,500]){
  const items=choices(count),calls=[];
  const result=await collectUnitChoices(async(number,signal)=>{calls.push(number);assert.equal(signal.aborted,false);return page(items,number);},count,new AbortController().signal);
  assert.deepEqual(result,items);assert.equal(calls.length,Math.ceil(count/100));assert.equal(calls.at(-1),Math.ceil(count/100));
 }
});
test("selecting a page or another filter deduplicates and retains other selections",()=>{
 const previous=new Map([[unit(1).id,unit(1)],[unit(501).id,unit(501)]]);
 const next=mergeUnitSelection(previous,choices(48));assert.equal(next.size,49);assert.ok(next.has(unit(501).id));assert.equal(previous.size,2);
 const again=mergeUnitSelection(next,choices(48));assert.equal(again.size,49);
});
test("over-limit bulk operations preserve the entire existing selection",async()=>{
 const previous=new Map(choices(500).map(value=>[value.id,value]));
 assert.throws(()=>mergeUnitSelection(previous,[unit(501)]),error=>error instanceof UnitSelectionError&&error.code==="limit");assert.equal(previous.size,500);assert.equal(previous.has(unit(501).id),false);
 let calls=0;await assert.rejects(collectUnitChoices(async()=>{calls++;return page([],1);},501,new AbortController().signal),error=>error.code==="limit");assert.equal(calls,0);
 assert.equal(mergeUnitSelection(previous,choices(500)).size,500);
});
test("changed pagination, repeated IDs or disabled results never produce a partial selection",async()=>{
 const items=choices(148);
 for(const alter of [value=>({...value,totalCount:147}),value=>({...value,page:1}),value=>({...value,pageSize:10}),value=>({...value,items:value.items.slice(1)}),value=>({...value,items:[items[0],...value.items.slice(1)]}),value=>({...value,items:[{...value.items[0],isEnabled:false},...value.items.slice(1)]})]){
  await assert.rejects(collectUnitChoices(async number=>number===2?alter(page(items,number)):page(items,number),148,new AbortController().signal),error=>error.code==="changed");
 }
 const previous=new Map([[unit(1).id,unit(1)]]);assert.throws(()=>mergeUnitSelection(previous,[{...unit(2),isEnabled:false}]),error=>error.code==="changed");assert.equal(previous.size,1);
});
test("failed or cancelled bulk reads cannot return the previously fetched first page",async()=>{
 const items=choices(148),controller=new AbortController();let calls=0;
 await assert.rejects(collectUnitChoices(async number=>{calls++;controller.abort();return page(items,number);},148,controller.signal),error=>error.name==="AbortError");assert.equal(calls,1);
 await assert.rejects(collectUnitChoices(async number=>{if(number===2)throw new Error("Network unavailable");return page(items,number);},148,new AbortController().signal),/Network unavailable/);
});
