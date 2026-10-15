import {contextKey,type Context,type Session} from '../auth/contracts.ts';
import {validateCommand,type Category,type Command,type Priority} from '../sync/contracts.ts';
export function tenantRequest(session:Session,context:Context,description:string,operationId:string,recordId:string):Command{
 if(context.role!=='TENANT'||!context.unitId)throw Error('Kiracı daire bağlamı gerekli.');
 return validateCommand({operationId,recordId,propertyId:context.propertyId,unitId:context.unitId,role:'TENANT',action:'CREATE',expectedVersion:0,description},session,contextKey(context));
}
// Owners and tenants report for the selected unit only; category/priority are always explicit from the form.
export function unitRequest(session:Session,context:Context,value:{description:string;category:Category;priority:Priority},operationId:string,recordId:string):Command{
 if(!['OWNER','TENANT'].includes(context.role)||!context.unitId)throw Error('Daire bağlamı gerekli.');
 return validateCommand({operationId,recordId,propertyId:context.propertyId,unitId:context.unitId,role:context.role,action:'CREATE',expectedVersion:0,description:value.description,category:value.category,priority:value.priority},session,contextKey(context));
}
