export const workOrderStatuses=["DRAFT","ASSIGNED","ACCEPTED","IN_PROGRESS","COMPLETED","VERIFIED","CLOSED","REJECTED","CANCELLED"] as const;
export type WorkOrderStatus=typeof workOrderStatuses[number];
export function workOrderNext(status:WorkOrderStatus):WorkOrderStatus[]{return ({DRAFT:["ASSIGNED","CANCELLED"],ASSIGNED:["ACCEPTED","REJECTED","CANCELLED"],ACCEPTED:["IN_PROGRESS","CANCELLED"],IN_PROGRESS:["COMPLETED","CANCELLED"],COMPLETED:["VERIFIED","CANCELLED"],VERIFIED:["CLOSED"],CLOSED:[],REJECTED:[],CANCELLED:[]} as Record<WorkOrderStatus,WorkOrderStatus[]>)[status];}
