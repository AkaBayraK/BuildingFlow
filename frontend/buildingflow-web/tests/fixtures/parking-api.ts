import {useCallback} from "react";
const P="11111111-1111-4111-8111-111111111111",S="22222222-2222-4222-8222-222222222222",C="33333333-3333-4333-8333-333333333333";
export class ApiError extends Error{constructor(public status:number){super("Fixture conflict");}}
export const errorMessage=()=>"İzole test çakışması (409)";
export const fixtureActions={conflict:()=>sessionStorage.setItem("parking-fixture-conflict","1")};
const start=new Date(Date.now()-3600000).toISOString(),end=new Date(Date.now()+3600000).toISOString();
let closures=[{id:C,spaceId:S,spaceCode:"A01",startsAt:start,endsAt:end,reason:"Zemin çizgisi yenileme",cancelledAt:null as string|null,cancellationReason:null as string|null,version:1}];
const page=(items:unknown[])=>({items,page:1,pageSize:100,totalCount:items.length});
const space={id:S,facilityId:C,levelId:null,code:"A01",type:"STANDARD",allocatedUnitId:null,allocatedUnitNumber:null,version:1,isClosed:true};
async function request(path:string,options?:RequestInit){
 await new Promise(r=>setTimeout(r,80));
 const url=new URL(path,location.origin),tail=url.pathname.split("/parking")[1];
 if(options?.method&&options.method!=="GET"){
  if(sessionStorage.getItem("parking-fixture-conflict")){sessionStorage.removeItem("parking-fixture-conflict");throw new ApiError(409);}
  const body=JSON.parse(String(options.body));
  if(tail?.endsWith("/cancel")){const id=tail.split("/")[2];closures=closures.map(c=>c.id===id?{...c,cancelledAt:new Date().toISOString(),cancellationReason:body.reason,version:c.version+1}:c);return closures.find(c=>c.id===id);}
  if(tail==="/report"){const from=url.searchParams.get("from")!,through=url.searchParams.get("through")!;return {propertyId:P,from,through,asOf:new Date().toISOString(),capacity:48,allocatedSpaces:24,reservedGuestSpaces:3,manualGuestsInside:5,overdueGuests:1,scheduledOperationsEnabled:false,hardwareEnabled:false,days:Array.from({length:(Date.parse(through)-Date.parse(from))/86400000+1},(_,i)=>({day:new Date(Date.parse(from)+i*86400000).toISOString().slice(0,10),entries:i%5,exits:i%3,reservations:i%6}))};}
 if(tail==="/closures"){const row={...body,id:crypto.randomUUID(),spaceCode:"A01",cancelledAt:null,cancellationReason:null,version:1};closures=[row,...closures];return row;}
 }
 if(tail==="/report"){const from=url.searchParams.get("from")!,through=url.searchParams.get("through")!;return {propertyId:P,from,through,asOf:new Date().toISOString(),capacity:48,allocatedSpaces:24,reservedGuestSpaces:3,manualGuestsInside:5,overdueGuests:1,scheduledOperationsEnabled:false,hardwareEnabled:false,days:Array.from({length:(Date.parse(through)-Date.parse(from))/86400000+1},(_,i)=>({day:new Date(Date.parse(from)+i*86400000).toISOString().slice(0,10),entries:i%5,exits:i%3,reservations:i%6}))};}
 if(tail==="/closures")return page(closures);
 if(tail==="/spaces"||tail==="/guest-spaces")return page([{...space,isClosed:closures.some(c=>!c.cancelledAt)}]);
 if(tail==="/guest-policy")return {configured:true,guestMaxHours:24};
 if(tail==="/policy")return {propertyId:P,configured:true,maxVehiclesPerUnit:2,maxVehiclesPerPerson:2,maxSpacesPerUnit:1,guestMaxHours:24,retentionDays:30,version:1};
 if(tail==="/facilities")return [{id:C,buildingId:null,buildingName:null,name:"Ortak Otopark",spaceCount:1,levels:[],version:1}];
 if(tail==="/vehicles"||tail==="/allocations"||tail==="/guests")return page([]);
 if(tail==="/plates"||tail==="/units")return [];
 throw new Error("Unsupported fixture route");
}
export function useAuthorizedApi(){return useCallback(request,[]);}
