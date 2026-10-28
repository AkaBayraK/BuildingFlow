import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {path,parseCopy,type Query} from './contracts';
import {readCopy,saveCopy,purgeCopies} from './cache';
export function useCommonAreas(q:Query,enabled=true){
 const a=useAuth(),c=a.selected,user=a.session?.userId;
 let url:string|null=null;try{if(c&&enabled)url=path(c,q,q.areaId?'/schedule':'');}catch{/* No authorized user-unit context. */}
 return useScopedRead({scope:user&&c&&url?JSON.stringify([user,contextKey(c),q]):null,offline:a.offline,
  read:()=>a.commonAreaCache(db=>readCopy(db,user!,c!,q,Date.now())).catch(()=>null),
  request:async()=>{const r=await a.commonAreaRequest(url!);return{status:r.status===404?403:r.status,json:async()=>parseCopy({userId:user,scope:contextKey(c!),query:q,asOf:new Date().toISOString(),page:q.areaId?null:await r.json(),schedule:q.areaId?await r.json():null},user!,c!,q)} as Response;},
  parse:v=>parseCopy(v,user!,c!,q),save:v=>a.commonAreaCache(db=>saveCopy(db,user!,c!,q,v,Date.now())).catch(()=>undefined),purge:()=>a.commonAreaCache(db=>purgeCopies(db,user!,c!)).catch(()=>undefined),
  error:'Ortak alanlar güncellenemedi. Bağlantınızı kontrol edin.',denied:'Ortak alan erişimi kaldırıldı. Yetkilerinizi yenileyin.'});
}
