// Planlama & Bakımlar: runtime checks for GET/POST/PUT /api/v1/properties/{id}/maintenance-plans.
export const maintenanceTypes=["MAINTENANCE","CLEANING","INSPECTION","DRILL","OTHER"] as const;
export const maintenanceStatuses=["PLANNED","IN_PROGRESS","COMPLETED","CANCELLED"] as const;
export type MaintenanceType=typeof maintenanceTypes[number];
export type MaintenanceStatus=typeof maintenanceStatuses[number];
export type MaintenancePlan={id:string;propertyId:string;title:string;description:string|null;type:MaintenanceType;plannedDate:string;location:string|null;responsible:string|null;status:MaintenanceStatus;version:number;createdAt:string;modifiedAt:string};
export type MaintenancePage={items:MaintenancePlan[];page:number;pageSize:number;totalCount:number;typeCounts:{type:MaintenanceType;count:number}[]};
export type MaintenanceUpcoming={today:string;items:MaintenancePlan[];nextDays:number};
export const limits={title:200,description:2000,text:200} as const;
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const stamp=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const optional=(v:unknown,max:number)=>v===null||typeof v==="string"&&!!v.trim()&&v.length<=max;
export const calendarDay=(v:unknown):v is string=>typeof v==="string"&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&new Date(v+"T00:00:00Z").toISOString().slice(0,10)===v;
export function readPlan(v:unknown,property:string):MaintenancePlan{
 if(!obj(v)||!guid(v.id)||v.propertyId!==property||typeof v.title!=="string"||!v.title.trim()||v.title.length>limits.title||!optional(v.description,limits.description)||!optional(v.location,limits.text)||!optional(v.responsible,limits.text)
  ||!maintenanceTypes.includes(v.type as MaintenanceType)||!maintenanceStatuses.includes(v.status as MaintenanceStatus)||!calendarDay(v.plannedDate)||!int(v.version,1)||!stamp(v.createdAt)||!stamp(v.modifiedAt))throw new Error("Invalid maintenance plan");
 return v as MaintenancePlan;
}
function readPlans(v:unknown,property:string){if(!Array.isArray(v))throw new Error("Invalid plans");const items=v.map(r=>readPlan(r,property));if(new Set(items.map(i=>i.id)).size!==items.length)throw new Error("Duplicate plans");return items;}
export function readPlanPage(v:unknown,property:string):MaintenancePage{
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||Number(v.pageSize)>100||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize)||v.items.length>Number(v.totalCount))throw new Error("Invalid plan page");
 if(!Array.isArray(v.typeCounts)||v.typeCounts.length!==maintenanceTypes.length||!v.typeCounts.every((c,i)=>obj(c)&&c.type===maintenanceTypes[i]&&int(c.count)))throw new Error("Invalid plan counts");
 return {...v,items:readPlans(v.items,property)} as MaintenancePage;
}
export function readUpcoming(v:unknown,property:string,limit:number):MaintenanceUpcoming{
 if(!obj(v)||!calendarDay(v.today)||!int(v.nextDays))throw new Error("Invalid upcoming plans");
 const items=readPlans(v.items,property),today=v.today;
 if(items.length>limit||items.some((p,i)=>p.plannedDate<today||p.status==="COMPLETED"||p.status==="CANCELLED"||i>0&&items[i-1].plannedDate>p.plannedDate))throw new Error("Invalid upcoming order");
 return {today,items,nextDays:v.nextDays as number};
}
/** Whole calendar days from today to date (both YYYY-MM-DD); negative when past. */
export function daysUntil(date:string,today:string){return Math.round((Date.parse(date+"T00:00:00Z")-Date.parse(today+"T00:00:00Z"))/86400000);}
/** Body for POST/PUT; blank optional texts become null so the server never stores whitespace. */
export function planBody(form:{title:string;description:string;type:MaintenanceType;plannedDate:string;location:string;responsible:string;status:MaintenanceStatus},version:number){
 const text=(v:string)=>v.trim()||null;
 return {title:form.title.trim(),description:text(form.description),type:form.type,plannedDate:form.plannedDate,location:text(form.location),responsible:text(form.responsible),status:form.status,version};
}
export function validPlanForm(form:{title:string;description:string;plannedDate:string;location:string;responsible:string}){
 const t=form.title.trim();return !!t&&t.length<=limits.title&&form.description.trim().length<=limits.description&&form.location.trim().length<=limits.text&&form.responsible.trim().length<=limits.text&&calendarDay(form.plannedDate)&&form.plannedDate>="2000-01-01"&&form.plannedDate<="2100-12-31";
}
