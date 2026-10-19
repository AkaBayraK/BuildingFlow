export const statuses=["RECORDED","IN_PROGRESS","RESOLVED","REJECTED"] as const;
export const categories=["FAULT","CLEANING","SECURITY","COMMON_AREA","SUGGESTION","OTHER"] as const;
export const priorities=["LOW","NORMAL","HIGH"] as const;
export type Status=typeof statuses[number];
export type RequestRow={id:string;propertyId:string;unitId:string|null;unitNumber:string|null;description:string;category:typeof categories[number];priority:typeof priorities[number];status:Status;response:string|null;hasHistory:boolean;version:number;createdAt:string;modifiedAt:string;photoId:string|null;photoSha256:string|null};
export type History={id:string;fromStatus:Status;toStatus:Status;response:string;createdAt:string};
export type PhotoConsent={state:"GRANTED"|"WITHDRAWN"|"NONE";recordId:string|null;textVersion:number|null;approvedAt:string|null;withdrawnAt:string|null};
export type Detail={record:RequestRow;history:History[];replayed:boolean;photoConsent:PhotoConsent|null};
export type Page={items:RequestRow[];page:number;pageSize:number;totalCount:number};
const uuid=(v:unknown):v is string=>typeof v==="string"&&/^[a-f0-9]{8}(-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i.test(v)&&!/^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(v);
const text=(v:unknown,max:number):v is string=>typeof v==="string"&&!!v.trim()&&v.length<=max;
const time=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const obj=(v:unknown):Record<string,unknown>=>{if(!v||typeof v!=="object"||Array.isArray(v))throw Error("Invalid request response");return v as Record<string,unknown>;};
export function transitions(status:Status):Status[]{return status==="RECORDED"?["IN_PROGRESS","REJECTED"]:status==="IN_PROGRESS"?["RESOLVED","REJECTED"]:["RECORDED"];}
export function readRow(value:unknown,property:string):RequestRow {
 const r=obj(value);
 if(!uuid(r.id)||r.propertyId!==property||!(r.unitId===null||uuid(r.unitId))||!(r.unitNumber===null||text(r.unitNumber,100))||!text(r.description,2000)||!categories.includes(r.category as RequestRow["category"])||!priorities.includes(r.priority as RequestRow["priority"])||!statuses.includes(r.status as Status)||!(r.response===null||text(r.response,2000))||typeof r.hasHistory!=="boolean"||!Number.isSafeInteger(r.version)||(r.version as number)<1||!time(r.createdAt)||!time(r.modifiedAt)||!(r.photoId===null?r.photoSha256===null:uuid(r.photoId)&&typeof r.photoSha256==="string"&&/^[a-f0-9]{64}$/i.test(r.photoSha256))||(!r.hasHistory&&(r.status!=="RECORDED"||r.response!==null))||(r.hasHistory&&r.response===null))throw Error("Invalid request scope or shape");
 return r as RequestRow;
}
export function readPage(value:unknown,property:string,page:number):Page {
 const p=obj(value);if(p.page!==page||p.pageSize!==10||!Number.isSafeInteger(p.totalCount)||(p.totalCount as number)<0||!Array.isArray(p.items)||p.items.length>10||p.items.length>(p.totalCount as number))throw Error("Invalid request page");
 const items=p.items.map(v=>readRow(v,property));if(new Set(items.map(r=>r.id)).size!==items.length)throw Error("Duplicate request");return {...p,items} as Page;
}
export function readDetail(value:unknown,property:string,id:string):Detail {
 const d=obj(value),record=readRow(d.record,property);if(record.id!==id||!Array.isArray(d.history)||typeof d.replayed!=="boolean")throw Error("Invalid request detail");
 const history=d.history.map(v=>{const h=obj(v);if(!uuid(h.id)||!statuses.includes(h.fromStatus as Status)||!transitions(h.fromStatus as Status).includes(h.toStatus as Status)||!text(h.response,2000)||!time(h.createdAt))throw Error("Invalid request history");return h as History;});
 const photoConsent=readPhotoConsent(d.photoConsent,record);
 if(new Set(history.map(h=>h.id)).size!==history.length||record.hasHistory!==!!history.length||history.some((h,i)=>i>0&&h.fromStatus!==history[i-1].toStatus)||history.length&&(history.at(-1)!.toStatus!==record.status||history.at(-1)!.response!==record.response))throw Error("Inconsistent request history");return {record,history,replayed:d.replayed,photoConsent};
}
// T17.5: the consent in effect when the photo was uploaded; absent for requests without a photo.
function readPhotoConsent(value:unknown,record:RequestRow):PhotoConsent|null {
 // Older APIs omit the field; a photo that is no longer current has none.
 if(value===undefined||value===null)return null;
 const c=obj(value);
 if(record.photoId===null||!["GRANTED","WITHDRAWN","NONE"].includes(c.state as string)||!(c.recordId===null||uuid(c.recordId))||!(c.textVersion===null||Number.isSafeInteger(c.textVersion)&&(c.textVersion as number)>=1)||!(c.approvedAt===null||time(c.approvedAt))||!(c.withdrawnAt===null||time(c.withdrawnAt))
  ||(c.recordId===null)!==(c.textVersion===null)||(c.recordId===null)!==(c.approvedAt===null)||(c.state==="WITHDRAWN")!==(c.withdrawnAt!==null)||(c.state==="GRANTED"&&c.recordId===null)||(c.state==="NONE"&&c.recordId!==null))throw Error("Invalid photo consent");
 return c as PhotoConsent;
}
