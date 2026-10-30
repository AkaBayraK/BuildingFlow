import {supplierId} from "./model.ts";
export const adviceKinds=["MATCH","DELAY_RISK","COST_ANOMALY"] as const;
export const adviceLevels=["HIGH","MEDIUM","LOW"] as const;
export const adviceReasons=["MATCH_TRACK_RECORD","MATCH_INSUFFICIENT_SAMPLE","DELAY_SLA_PASSED","DELAY_SUPPLIER_TRACK_RECORD","DELAY_DEADLINE_CLOSE","COST_ABOVE_CONTRACT","COST_ABOVE_HISTORY"] as const;
export const evidenceKeys=["onTimeRate","activeContract","openOverdue","hoursLeft","elapsedShare","gross","median","contract"] as const;
export type AdviceKind=typeof adviceKinds[number];
export type AdviceItem={kind:AdviceKind;supplierId:string;supplierName:string;recordId:string|null;level:typeof adviceLevels[number];reasonCode:typeof adviceReasons[number];evidence:Partial<Record<typeof evidenceKeys[number],string>>;sampleCount:number;id:string};
export type Advice={source:string;items:AdviceItem[];remainingToday:number;requiresHumanDecision:true};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
// The page renders only closed codes and short numeric evidence; any action or free text from the server is rejected.
export function readAdvice(v:unknown,kind:AdviceKind):Advice{
 if(!obj(v)||typeof v.source!=="string"||!/^[A-Z_]{2,20}$/.test(v.source)||v.requiresHumanDecision!==true||!Number.isSafeInteger(v.remainingToday)||Number(v.remainingToday)<0||!Array.isArray(v.items)||v.items.length>20)throw new Error("Invalid advice");
 const items=v.items.map((x,i)=>{
  if(!obj(x)||x.kind!==kind||!supplierId(x.supplierId)||typeof x.supplierName!=="string"||!x.supplierName.trim()||x.supplierName.length>200||!(x.recordId===null||supplierId(x.recordId))||(kind==="MATCH")!==(x.recordId===null)||!adviceLevels.includes(x.level as never)||!adviceReasons.includes(x.reasonCode as never)||!String(x.reasonCode).startsWith(kind==="MATCH"?"MATCH_":kind==="DELAY_RISK"?"DELAY_":"COST_")||!Number.isSafeInteger(x.sampleCount)||Number(x.sampleCount)<0||"action" in x)throw new Error("Invalid advice item");
  if(!obj(x.evidence)||Object.entries(x.evidence).some(([k,e])=>!evidenceKeys.includes(k as never)||typeof e!=="string"||!/^[-0-9a-z.]{1,20}$/.test(e)))throw new Error("Invalid advice evidence");
  return {...(x as Omit<AdviceItem,"id">),id:String(i)};
 });
 return {source:v.source,items,remainingToday:Number(v.remainingToday),requiresHumanDecision:true};
}
