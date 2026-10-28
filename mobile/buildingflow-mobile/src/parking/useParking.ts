import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {path,parseSnapshot,permitted} from './contracts';
import {readCopy,saveCopy,purge} from './cache';
export function useParking(page:number){
 const a=useAuth(),c=a.selected,user=a.session?.userId;const allowed=!!c&&permitted(c);
 return useScopedRead({scope:allowed&&user?JSON.stringify([user,contextKey(c!),page]):null,offline:a.offline,
  read:()=>a.parkingCache(db=>readCopy(db,user!,c!,page,Date.now())).catch(()=>null),
  request:async()=>{const r=await a.parkingRequest(path(c!,page));return{status:r.status===404?403:r.status,json:()=>r.json()} as Response;},
  parse:v=>parseSnapshot(v,user!,c!,page),save:v=>a.parkingCache(db=>saveCopy(db,user!,c!,page,v,Date.now())),purge:()=>a.parkingCache(db=>purge(db,user!,c!)),
  error:'Otopark güncellenemedi. Kopya eski olabilir; bağlantı kurup yenileyin.',denied:'Otopark erişimi kaldırıldı. Çevrimiçi yetkilerinizi yenileyin.'});
}
