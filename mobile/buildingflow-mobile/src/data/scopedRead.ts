export type ReadPorts<T>={active:()=>boolean;offline:boolean;cache:boolean;read:()=>Promise<T|null>;request:()=>Promise<{status:number;json:()=>Promise<unknown>}>;parse:(v:unknown)=>T;save:(v:T)=>Promise<unknown>;purge:()=>Promise<unknown>;value:(v:T|null,cached:boolean)=>void;error:(denied:boolean)=>void};
// Every asynchronous boundary checks the account/context/query lifecycle before applying data.
export async function loadScoped<T>(p:ReadPorts<T>){
 let denied=false;
 try{
  if(!p.active())return;
  if(p.cache){try{const v=await p.read();if(!p.active())return;if(v)p.value(v,true);}catch{if(!p.active())return;p.value(null,false);await p.purge();if(p.offline)throw Error();}}
  if(!p.active()||p.offline)return;
  const response=await p.request();if(!p.active())return;
  if(response.status===401||response.status===403){denied=true;p.value(null,false);if(p.cache)await p.purge();if(p.active())p.error(true);return;}
  if(response.status!==200)throw Error();
  const v=p.parse(await response.json());if(!p.active())return;
  if(p.cache)await p.save(v);if(p.active())p.value(v,false);
 }catch{if(p.active())p.error(denied);}
}
