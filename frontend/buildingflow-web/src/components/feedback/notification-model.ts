import type {MessageKind} from "./message-types.ts";
export const notificationDuration = 8000;
export const notificationLimit = 4;
export type NotificationIdentity={id:number;kind:MessageKind;key:string};
export function appendNotification<T extends NotificationIdentity>(rows:T[],notice:T):T[]{
 if(rows.some(row=>row.kind===notice.kind&&row.key===notice.key))return rows;
 return [...rows.slice(-(notificationLimit-1)),notice];
}
// Injectable clock/scheduler keeps hover/focus pause and expiry testable without a second UI runtime.
export function notificationTimer(expire:()=>void, clock:()=>number=()=>Date.now(),
 schedule:(callback:()=>void,delay:number)=>ReturnType<typeof setTimeout>=(callback,delay)=>setTimeout(callback,delay),
 cancel:(id:ReturnType<typeof setTimeout>)=>void=id=>clearTimeout(id)){
 let remaining=notificationDuration;let started=clock();let handle:ReturnType<typeof setTimeout>|undefined;let disposed=false;
 const resume=()=>{if(disposed||handle!==undefined)return;started=clock();handle=schedule(()=>{handle=undefined;disposed=true;expire();},remaining);};
 const pause=()=>{if(handle===undefined)return;cancel(handle);handle=undefined;remaining=Math.max(0,remaining-(clock()-started));};
 const dispose=()=>{pause();disposed=true;};resume();return {pause,resume,dispose};
}
