export function trendScale(values:readonly number[]){
 const max=Math.max(0,...values.filter(Number.isFinite));if(max===0)return {max:100,empty:true};
 const raw=max/4,power=10**Math.floor(Math.log10(raw)),unit=raw/power;const step=(unit<=1?1:unit<=2?2:unit<=5?5:10)*power;
 return {max:step*4,empty:false};
}
export function trendBarLayout(width:number,count:number){
 const left=48,right=12,plotWidth=Math.max(1,width-left-right),slot=plotWidth/Math.max(1,count),gap=Math.min(5,slot*.08),bar=Math.min(34,(slot*.72-gap)/2);
 return {left,right,slot,bar,gap,center:(index:number)=>left+slot*(index+.5)};
}
