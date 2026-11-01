// Query parameters the assistant's source cards may set on other screens; anything else is ignored.
const guid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const requestStatuses=["RECORDED","IN_PROGRESS","RESOLVED","REJECTED"] as const;
const id=(value:string|null)=>value&&guid.test(value)&&value!=="00000000-0000-0000-0000-000000000000"?value.toLowerCase():null;
export function requestLink(search:string):{status:typeof requestStatuses[number]|null;request:string|null}{
 const params=new URLSearchParams(search);const status=params.get("status");
 return {status:requestStatuses.find(s=>s===status)??null,request:id(params.get("request"))};
}
export function surveyLink(search:string){return id(new URLSearchParams(search).get("survey"));}
export function supplierViewLink(search:string){return new URLSearchParams(search).get("view")==="alerts"?"alerts" as const:null;}
export function announcementDraftLink(search:string){return new URLSearchParams(search).get("assistantDraft")==="1";}
