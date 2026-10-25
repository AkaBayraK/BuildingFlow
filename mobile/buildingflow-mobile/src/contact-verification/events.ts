type Event={user:string;kind:'CONTACT'|'PROFILE'};
const listeners=new Set<(event:Event)=>void>();
export function changed(user:string,kind:Event['kind']){listeners.forEach(listener=>listener({user,kind}));}
export function subscribe(listener:(event:Event)=>void){listeners.add(listener);return()=>{listeners.delete(listener);};}
