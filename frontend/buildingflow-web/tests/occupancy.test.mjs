import {test} from 'node:test';
import assert from 'node:assert/strict';
import {occupancyRing} from '../src/features/dashboard/occupancy.ts';
const summary=(total,occupied,type='APARTMENT')=>({unitCount:total,occupiedUnitCount:occupied,vacantUnitCount:total-occupied,unitTypes:[{type,count:total}]});
test('occupancy ring uses occupied units, keeps empty totals undefined and never invents pending',()=>{
 const example=occupancyRing(summary(96,88));assert.equal(example.percent,92);assert.equal(example.occupied,88);assert.equal(example.vacant,8);assert.equal(example.pending,null);
 for(const [total,filled,ratio,percent] of [[0,0,0,null],[10,0,0,0],[10,10,1,100],[100,1,.01,1]]){
  const value=occupancyRing(summary(total,filled));assert.equal(value.ratio,ratio);assert.equal(value.percent,percent);assert.equal(value.pending,null);assert.equal(value.occupied+value.vacant,total);
 }
});
test('mixed unit counts retain unit terminology instead of labelling shops as apartments',()=>{
 assert.equal(occupancyRing(summary(96,88)).apartmentOnly,true);
 assert.equal(occupancyRing(summary(0,0)).apartmentOnly,false);
 assert.equal(occupancyRing({...summary(96,88),unitTypes:[{type:'APARTMENT',count:90},{type:'SHOP',count:6}]}).apartmentOnly,false);
});
