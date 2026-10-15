import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {parseManager,type Section} from './contracts';
import {readManager,writeManager,purgeManager} from './cache';
export function useManager(section:Section,page:number){
 const a=useAuth(),user=a.session?.userId,c=a.selected;
 return useScopedRead({scope:user&&c?JSON.stringify([user,contextKey(c),section,page]):null,offline:a.offline,
  read:()=>a.managerCache(db=>readManager(db,user!,c!,section,page,Date.now())),request:()=>a.managerRequest(section,page),parse:v=>parseManager(v,user!,c!,section,page),
  save:v=>a.managerCache(db=>writeManager(db,user!,c!,section,page,v,Date.now())),purge:()=>a.managerCache(db=>purgeManager(db,user!,c!)),
  error:'Yönetim bilgileri güncellenemedi. Bağlantınızı kontrol edip yeniden deneyin.',denied:'Bu yönetim alanına erişiminiz artık geçerli değil.'});
}
