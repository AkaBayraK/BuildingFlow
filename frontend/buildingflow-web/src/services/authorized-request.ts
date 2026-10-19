import {problemDetails,connectionError,type ProblemFeedback} from "./problem.ts";
import type {NotificationMessage} from "../components/feedback/notification-text.ts";
export class ApiError extends Error {
 readonly status:number;readonly errors:Record<string,string[]>;readonly feedback?:ProblemFeedback;
 constructor(status:number,message:string,errors:Record<string,string[]>={},feedback?:ProblemFeedback){super(message);this.status=status;this.errors=errors;this.feedback=feedback;}
}
type Identity={token?:string;updateToken:(minimumValidity:number)=>Promise<unknown>};
type Reporter={error:(message:NotificationMessage)=>void;warning:(message:NotificationMessage)=>void};
// Single transport used by useAuthorizedApi; injected fetch enables real async cancellation tests.
export async function authorizedRequest<T>(identity:Identity,locale:string,notify:Reporter,baseUrl:string,path:string,options:RequestInit={},send:typeof fetch=fetch,responseType:"json"|"blob"="json",quiet:readonly number[]=[]):Promise<T>{
 options.signal?.throwIfAborted();
 try {await identity.updateToken(30);}
 catch {options.signal?.throwIfAborted();const problem=problemDetails(401,null,locale);notify.error(problem.message);throw new ApiError(401,problem.message,problem.errors,problem);}
 options.signal?.throwIfAborted();
 let response:Response;
 const headers=new Headers(options.headers);headers.set("Authorization",`Bearer ${identity.token}`);
 if(options.body instanceof FormData)headers.delete("Content-Type");else if(!headers.has("Content-Type"))headers.set("Content-Type","application/json");
 try {response=await send(`${baseUrl}${path}`,{...options,cache:"no-store",headers});}
 catch(error){if(!(error instanceof DOMException&&error.name==="AbortError")&&!options.signal?.aborted)notify.error(connectionError(locale));throw error;}
 if(!response.ok){
  const body:unknown=await response.json().catch(()=>null);options.signal?.throwIfAborted();
  const problem=problemDetails(response.status,body,locale);
  // Expected statuses (such as 404 for an account without a photo) are returned to the caller without a notification.
  if(!quiet.includes(response.status))notify[problem.severity]({text:problem.message,...(problem.code?{code:problem.code,parameters:problem.parameters}:{})});
  throw new ApiError(response.status,problem.message,problem.errors,problem);
 }
 if(response.status===204)return undefined as T;
 return (responseType==="blob"?response.blob():response.json()) as Promise<T>;
}
