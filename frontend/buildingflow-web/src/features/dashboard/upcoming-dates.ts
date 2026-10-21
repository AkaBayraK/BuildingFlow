// Yaklaşan Önemli Tarihler: dues deadlines and open plans merged into one date-ordered list.
import {daysUntil,type MaintenancePlan} from "../maintenance/model.ts";
export type UpcomingDue={dueDate:string;chargeCount:number;amount:string};
export type UpcomingDate={kind:"dues";key:string;date:string;days:number;due:UpcomingDue}|{kind:"plan";key:string;date:string;days:number;plan:MaintenancePlan};
/** Same-day dues sort before plans so the payment deadline stays visible; plans keep their server order. */
export function upcomingDates(dues:readonly UpcomingDue[],plans:readonly MaintenancePlan[],today:string,limit=4):UpcomingDate[]{
 const rows:UpcomingDate[]=[...dues.map(d=>({kind:"dues" as const,key:"dues-"+d.dueDate,date:d.dueDate,days:daysUntil(d.dueDate,today),due:d})),...plans.map(p=>({kind:"plan" as const,key:"plan-"+p.id,date:p.plannedDate,days:daysUntil(p.plannedDate,today),plan:p}))];
 return rows.filter(r=>r.days>=0).sort((a,b)=>a.date<b.date?-1:a.date>b.date?1:a.kind===b.kind?0:a.kind==="dues"?-1:1).slice(0,limit);
}
