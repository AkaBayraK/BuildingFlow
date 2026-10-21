import type {DashboardSummary} from "./model";
/** Input is the validated dashboard summary; pending is not part of the API contract. */
export function occupancyRing(data:Pick<DashboardSummary,"unitCount"|"occupiedUnitCount"|"vacantUnitCount"|"unitTypes">){
 const ratio=data.unitCount>0?data.occupiedUnitCount/data.unitCount:0;
 return {ratio,percent:data.unitCount>0?Math.round(ratio*100):null,occupied:data.occupiedUnitCount,vacant:data.vacantUnitCount,pending:null,apartmentOnly:data.unitCount>0&&data.unitTypes.every(row=>row.type==="APARTMENT"||row.count===0)};
}
