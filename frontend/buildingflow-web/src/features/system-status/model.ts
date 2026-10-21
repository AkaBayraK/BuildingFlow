// Sistem durumu sözleşmesi: GET /api/v1/system/status (TraceFlow ekranıyla aynı biçim).
export const componentTypes=["SERVICE","DATABASE","INFRASTRUCTURE","EXTERNAL_SERVICE"] as const;
export const componentStatuses=["UP","DEGRADED","DOWN","UNKNOWN","DISABLED"] as const;
export type ComponentType=typeof componentTypes[number];
export type ComponentStatus=typeof componentStatuses[number];
export type SystemStatusComponent={id:string;name:string;type:ComponentType;status:ComponentStatus;responseTimeMs:number|null;checkedAt:string;description:string};
export type SystemStatus={overallStatus:ComponentStatus;totalComponents:number;upComponents:number;degradedComponents:number;downComponents:number;unknownComponents:number;lastCheckedAt:string;environment:string;version:string;cluster:string;uptimeSeconds:number;components:SystemStatusComponent[]};
export const systemStatusPath="/api/v1/system/status";
export const systemSettingsHref="/system-settings";
// Yalnız backend politikasının da kabul ettiği roller durum göstergesini sorgular; görünürlük yetki kanıtı değildir.
export const systemStatusRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER"] as const;

const text=(value:unknown)=>typeof value==="string"?value:"";
const count=(value:unknown)=>typeof value==="number"&&Number.isFinite(value)&&value>=0?Math.floor(value):0;
const oneOf=<T extends string>(values:readonly T[],value:unknown,fallback:T):T=>values.includes(value as T)?value as T:fallback;

export function parseSystemStatus(value:unknown):SystemStatus{
 const body=(value&&typeof value==="object"?value:{}) as Record<string,unknown>;
 const components=(Array.isArray(body.components)?body.components:[]).flatMap(item=>{
  if(!item||typeof item!=="object")return [];
  const row=item as Record<string,unknown>;const id=text(row.id);if(!id)return [];
  return [{id,name:text(row.name)||id,type:oneOf(componentTypes,row.type,"SERVICE"),status:oneOf(componentStatuses,row.status,"UNKNOWN"),responseTimeMs:typeof row.responseTimeMs==="number"?row.responseTimeMs:null,checkedAt:text(row.checkedAt),description:text(row.description)}];
 });
 return {overallStatus:oneOf(componentStatuses,body.overallStatus,"UNKNOWN"),totalComponents:count(body.totalComponents),upComponents:count(body.upComponents),degradedComponents:count(body.degradedComponents),downComponents:count(body.downComponents),unknownComponents:count(body.unknownComponents),lastCheckedAt:text(body.lastCheckedAt),environment:text(body.environment),version:text(body.version),cluster:text(body.cluster),uptimeSeconds:count(body.uptimeSeconds),components};
}

export function filterComponents(components:readonly SystemStatusComponent[],type:"ALL"|ComponentType,status:"ALL"|ComponentStatus,query:string,locale:string){
 const q=query.trim().toLocaleLowerCase(locale);
 return components.filter(c=>(type==="ALL"||c.type===type)&&(status==="ALL"||c.status===status)&&(!q||`${c.name} ${c.description}`.toLocaleLowerCase(locale).includes(q)));
}

export function upCount(components:readonly SystemStatusComponent[],type:"ALL"|ComponentType){
 const items=components.filter(c=>type==="ALL"||c.type===type);
 return {current:items.filter(c=>c.status==="UP").length,total:items.length};
}

export const statusDot:Record<ComponentStatus,string>={UP:"bg-emerald-500",DEGRADED:"bg-amber-500",DOWN:"bg-red-500",UNKNOWN:"bg-slate-400",DISABLED:"bg-slate-400"};
export const statusText:Record<ComponentStatus,string>={UP:"text-emerald-600 dark:text-emerald-400",DEGRADED:"text-amber-600 dark:text-amber-400",DOWN:"text-red-600 dark:text-red-400",UNKNOWN:"text-slate-500",DISABLED:"text-slate-500"};
export const statusColor:Record<ComponentStatus,string>={UP:"#10b981",DEGRADED:"#f59e0b",DOWN:"#ef4444",UNKNOWN:"#cbd5e1",DISABLED:"#94a3b8"};
