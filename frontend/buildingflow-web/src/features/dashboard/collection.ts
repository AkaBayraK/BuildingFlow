import {moneyCents} from "../payments/model.ts";
export type DashboardCollection={period:string;totalAmount:string;collectedAmount:string;pendingAmount:string;overdueAmount:string};
export function collectionRatio(row:DashboardCollection){
 const total=moneyCents(row.totalAmount),collected=moneyCents(row.collectedAmount);
 return {ratio:total>BigInt(0)?Number(collected*BigInt(10000)/total)/10000:0,percent:total>BigInt(0)?Number((collected*BigInt(100)+total/BigInt(2))/total):null};
}
export function readCollections(value:unknown,current:string):DashboardCollection[]|null{
 if(value===undefined||value===null)return null;
 if(!Array.isArray(value)||value.length!==2)throw new Error("Invalid collection periods");
 const [year,month]=current.split("-").map(Number),previous=new Date(Date.UTC(year,month-2,1));const prior=`${previous.getUTCFullYear()}-${String(previous.getUTCMonth()+1).padStart(2,"0")}`;
 for(const [i,row] of value.entries()){
  if(!row||typeof row!=="object"||row.period!==(i===0?prior:current))throw new Error("Invalid collection period");
  for(const key of ["totalAmount","collectedAmount","pendingAmount","overdueAmount"])if(typeof row[key]!=="string")throw new Error("Invalid collection amount");
  if(moneyCents(row.collectedAmount)+moneyCents(row.pendingAmount)+moneyCents(row.overdueAmount)!==moneyCents(row.totalAmount))throw new Error("Inconsistent collection totals");
 }
 return value as DashboardCollection[];
}
