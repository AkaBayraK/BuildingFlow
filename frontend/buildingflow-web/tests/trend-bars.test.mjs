import {test} from 'node:test';
import assert from 'node:assert/strict';
import {trendScale,trendBarLayout} from '../src/features/dashboard/trend-bars.ts';
test('trend axis contains all nonnegative values, including zero and fractional amounts',()=>{
 for(const values of [[0,0],[125400,75000],[.01,.02],[5000000,1]]){
  const scale=trendScale(values);assert.ok(scale.max>0);assert.ok(scale.max>=Math.max(...values));assert.equal(scale.empty,values.every(v=>v===0));
 }
 assert.equal(trendScale([125400]).max,200000);
});
test('paired bars remain distinct and inside the plot at phone and desktop widths',()=>{
 for(const width of [240,280,640,1100])for(const count of [3,6]){
  const l=trendBarLayout(width,count);assert.ok(l.bar>0);assert.ok(l.gap>0);
  for(let i=0;i<count;i++){
   const center=l.center(i),left=center-l.gap/2-l.bar,right=center+l.gap/2+l.bar;
   assert.ok(left>=l.left);assert.ok(right<=width-l.right);if(i<count-1)assert.ok(right<l.center(i+1)-l.gap/2-l.bar);
  }
 }
});
