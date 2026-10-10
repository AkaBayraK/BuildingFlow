import type {UnitChoicesPage} from "./model";

export const assessmentSelectionLimit=500;
type Choice=UnitChoicesPage["items"][number];
export class UnitSelectionError extends Error {
 readonly code:"limit"|"changed";
 constructor(code:"limit"|"changed"){super(code);this.code=code;}
}

/** Add all requested rows or leave the existing selection unchanged. */
export function mergeUnitSelection(previous:ReadonlyMap<string,Choice>,items:readonly Choice[]):Map<string,Choice>{
 const next=new Map(previous);
 for(const item of items){if(!item.isEnabled)throw new UnitSelectionError("changed");next.set(item.id,item);}
 if(next.size>assessmentSelectionLimit)throw new UnitSelectionError("limit");
 return next;
}

/** Collect the complete current filter, never just the visible first page. */
export async function collectUnitChoices(
 load:(page:number,signal:AbortSignal)=>Promise<UnitChoicesPage>,expectedCount:number,signal:AbortSignal
):Promise<Choice[]>{
 if(!Number.isSafeInteger(expectedCount)||expectedCount<0)throw new UnitSelectionError("changed");
 if(expectedCount>assessmentSelectionLimit)throw new UnitSelectionError("limit");
 const items:Choice[]=[];const seen=new Set<string>();
 for(let page=1;page<=Math.ceil(expectedCount/100);page++){
  if(signal.aborted)throw new DOMException("Aborted","AbortError");
  const result=await load(page,signal);
  if(signal.aborted)throw new DOMException("Aborted","AbortError");
  if(result.page!==page||result.pageSize!==100||result.totalCount!==expectedCount||result.items.length!==Math.min(100,expectedCount-items.length))throw new UnitSelectionError("changed");
  for(const item of result.items){
   if(!item.isEnabled||seen.has(item.id))throw new UnitSelectionError("changed");
   seen.add(item.id);items.push(item);
  }
 }
 return items;
}
