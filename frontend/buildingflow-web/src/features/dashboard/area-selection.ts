import {parsePropertyPage} from "../../services/management-contracts.ts";
import type {Property} from "../../services/api";
export const dashboardRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"] as const;
export function selectedDashboardArea(items:readonly Property[],wanted:string|null){return items.find(p=>p.isEnabled&&p.id===wanted)?.id??items.find(p=>p.isEnabled)?.id??"";}
export async function dashboardAreas(api:(path:string,options:{signal:AbortSignal})=>Promise<unknown>,signal:AbortSignal){
 const items:Property[]=[];let total=0;
 for(let page=1;page===1||items.length<total;page++){
  signal.throwIfAborted();const result=parsePropertyPage(await api(`/api/v1/properties?page=${page}&pageSize=100&sortBy=name&sortDirection=asc`,{signal}));
  if(result.page!==page||result.pageSize!==100||page>1&&result.totalCount!==total||result.items.some(p=>!/^([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$/i.test(p.id)||p.id==="00000000-0000-0000-0000-000000000000"||items.some(x=>x.id===p.id))||result.items.length===0&&items.length<result.totalCount)throw new Error("Invalid dashboard area page");
  total=result.totalCount;items.push(...result.items);
 }
 return items.filter(p=>p.isEnabled);
}
