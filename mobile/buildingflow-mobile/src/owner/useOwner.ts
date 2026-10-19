import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {parseSnapshot} from './contracts';
import {readOwner,writeOwner,purgeOwner} from './cache';
export function useOwner(){
 const auth=useAuth(),user=auth.session?.userId,c=auth.selected;
 const key=c?contextKey(c):null;
 return useScopedRead({scope:user&&c&&['OWNER','TENANT'].includes(c.role)?JSON.stringify([user,key]):null,offline:auth.offline,
  read:()=>auth.residentCache(db=>readOwner(db,user!,c!,Date.now())),request:()=>auth.residentRequest(),parse:v=>parseSnapshot(v,user!,c!),
  save:v=>auth.residentCache(db=>writeOwner(db,user!,c!,v,Date.now())),purge:()=>auth.residentCache(db=>purgeOwner(db,user!,c!)),
  error:'Mülk bilgileri alınamadı. Bağlantınızı kontrol edip yeniden deneyin.',denied:'Bu daire için erişiminiz artık geçerli değil.'});
}
