import {parseAuthorization,readableOffline,type Authorization} from './model.ts';
import {CacheQueue} from '../offline/queue.ts';
export type DevicePorts={get:(key:string,authenticated?:boolean)=>Promise<string|null>;set:(key:string,value:string,authenticated?:boolean)=>Promise<void>;remove:(key:string)=>Promise<void>;biometrics:()=>boolean;random:()=>string;now:()=>number};
const secretKey='buildingflow.person.offline.v1',anchorKey='buildingflow.person.clock.v1',markerKey='buildingflow.person.enrolled.v1';
const denied=()=>Error('Çevrimdışı erişim doğrulanamadı. İnternete bağlanıp yeniden giriş yap.');
export function createDeviceAccess(ports:DevicePorts){
 const queue=new CacheQueue();
 const bound=(raw:string|null,userId:string,scope:string)=>{if(!raw)return null;const v=JSON.parse(raw);return v&&v.userId===userId&&v.scope===scope?v:null;};
 async function anchor(value:Authorization,scope:string){const a=bound(await ports.get(anchorKey),value.userId,scope);if(!a||!Number.isSafeInteger(a.seen)||!readableOffline(value,ports.now(),a.seen))throw denied();return a;}
 return{
  clear:()=>queue.clear(async()=>{await ports.remove(secretKey);await ports.remove(markerKey);await ports.remove(anchorKey);}),
  recordOnline:(input:Authorization,scope:string)=>queue.write(async()=>{const a=parseAuthorization(input);const now=ports.now();if(!readableOffline(a,now,0))throw denied();const raw=await ports.get(markerKey);if(raw&&!bound(raw,a.userId,scope)){await ports.remove(secretKey);await ports.remove(markerKey);}await ports.set(anchorKey,JSON.stringify({userId:a.userId,scope,seen:now}));},undefined),
  status:(userId:string,scope:string)=>queue.run(async()=>ports.biometrics()&&!!bound(await ports.get(markerKey),userId,scope)),
  enroll:(input:Authorization,scope:string)=>queue.write(async()=>{const a=parseAuthorization(input);if(!ports.biometrics())throw Error('Çevrimdışı erişim için cihazda biyometrik doğrulama gerekli.');await anchor(a,scope);const nonce=ports.random();if(!/^[0-9a-f]{64}$/.test(nonce))throw denied();await ports.set(secretKey,JSON.stringify({userId:a.userId,scope,nonce}),true);await ports.set(markerKey,JSON.stringify({userId:a.userId,scope}));return true;},false),
  unlock:(input:Authorization,scope:string)=>queue.run(async()=>{const a=parseAuthorization(input);if(!ports.biometrics())throw denied();await anchor(a,scope);if(!bound(await ports.get(markerKey),a.userId,scope))throw denied();const key=bound(await ports.get(secretKey,true),a.userId,scope);if(!key||typeof key.nonce!=='string'||!/^[0-9a-f]{64}$/.test(key.nonce))throw denied();await anchor(a,scope);await ports.set(anchorKey,JSON.stringify({userId:a.userId,scope,seen:ports.now()}));return true;})
 };
}
