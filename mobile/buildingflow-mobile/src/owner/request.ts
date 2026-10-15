import {contextKey,type Context,type Session} from '../auth/contracts.ts';
import {validateCommand,type Command} from '../sync/contracts.ts';
export function tenantRequest(session:Session,context:Context,description:string,operationId:string,recordId:string):Command{
 if(context.role!=='TENANT'||!context.unitId)throw Error('Kiracı daire bağlamı gerekli.');
 return validateCommand({operationId,recordId,propertyId:context.propertyId,unitId:context.unitId,role:'TENANT',action:'CREATE',expectedVersion:0,description},session,contextKey(context));
}
