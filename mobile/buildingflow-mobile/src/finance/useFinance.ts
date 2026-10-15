import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {useScopedRead} from '../data/useScopedRead';
import {parseFinance,type Query} from './contracts';
import {readFinance,writeFinance,purgeFinance} from './cache';
export function useFinance(q:Query){
 const a=useAuth(),user=a.session?.userId,c=a.selected;
 return useScopedRead({scope:user&&c?JSON.stringify([user,contextKey(c),q.view,q.page,q.chargeId]):null,offline:a.offline,
  read:()=>a.financeCache(db=>readFinance(db,user!,c!,q,Date.now())),request:()=>a.financeRequest(q),parse:v=>parseFinance(v,user!,c!,q),
  save:v=>a.financeCache(db=>writeFinance(db,user!,c!,q,v,Date.now())),purge:()=>a.financeCache(db=>purgeFinance(db,user!,c!)),
  error:'Finans bilgileri güncellenemedi. Bağlantınızı kontrol edip yeniden deneyin.',denied:'Bu finans bilgilerine erişiminiz artık geçerli değil.'});
}
