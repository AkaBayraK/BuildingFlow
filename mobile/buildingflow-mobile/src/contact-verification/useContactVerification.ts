import {useEffect,useRef,useState,useSyncExternalStore} from 'react';
import {AppState} from 'react-native';
import {usePathname} from 'expo-router';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {ContactController} from './controller';
import {changed,subscribe} from './events';
import {challengeExpired} from './contracts';
export function useContactVerification(alwaysVisible=false){
 const auth=useAuth(),user=auth.session?.userId,path=usePathname(),shown=alwaysVisible||path==='/profile';
 const live=useRef({auth,shown});useEffect(()=>{live.current={auth,shown};});
 const [controller]=useState(()=>new ContactController());
 useEffect(()=>{controller.configure({uuid:Crypto.randomUUID,now:Date.now,active:()=>{const {auth:a,shown:s}=live.current;return !!user&&a.session?.userId===user&&a.state==='signedIn'&&!a.offline&&!a.privacyLocked&&s&&(AppState.currentState===null||AppState.currentState==='active');},request:(p,b)=>live.current.auth.profileRequest(p,b),verified:()=>{if(user)changed(user,'PROFILE');}});},[controller,user]);
 const state=useSyncExternalStore(controller.subscribe,controller.snapshot,controller.snapshot),[now,setNow]=useState(Date.now),[epoch,setEpoch]=useState(0);
 const expiration=useRef('');
 const [emailFeedback,setEmailFeedback]=useState<{user:string|undefined;text:string;error:boolean}|null>(null);
 useEffect(()=>{void controller.refresh();return()=>controller.clear();},[controller,auth.offline,auth.privacyLocked,auth.state,auth.verifiedAt,user,shown]);
 useEffect(()=>{const sub=AppState.addEventListener('change',next=>{setEpoch(n=>n+1);if(next==='active')void controller.refresh();else controller.clear();});const unsubscribe=subscribe(event=>{if(event.user===user&&event.kind==='CONTACT')void controller.refresh();});return()=>{sub.remove();unsubscribe();controller.clear();};},[controller,user]);
 useEffect(()=>{const timer=setInterval(()=>setNow(Date.now()),1000);return()=>clearInterval(timer);},[]);
 const p=state.contact?.phone,expired=p?challengeExpired(p,now):false;
 useEffect(()=>{if(expired&&p?.expiresAt&&expiration.current!==p.expiresAt){expiration.current=p.expiresAt;void controller.refresh();}},[expired,p?.expiresAt,controller]);
 // Signed-out state has no user; a null feedback must not match it.
 const feedback=emailFeedback!==null&&emailFeedback.user===user?emailFeedback:null;
 return {...state,remaining:Math.max(0,Math.ceil((state.retryUntil-now)/1000)),expired,epoch,refresh:()=>controller.refresh(),execute:controller.execute.bind(controller),emailMessage:feedback?.text??'',emailError:!!feedback?.error,email:async()=>{setEmailFeedback(null);try{const result=await auth.emailAction(state.contact?.email.status==='UNVERIFIED'?'VERIFY_EMAIL':'UPDATE_EMAIL');await controller.refresh();setEmailFeedback({user,text:result?'Kimlik ekranından dönüldü. Güncel doğrulama durumu sunucudan okunur.':'E-posta işlemi iptal edildi.',error:false});return result;}catch{setEmailFeedback({user,text:'E-posta işlemi tamamlanamadı. Bağlantınızı ve hesabınızı kontrol edin.',error:true});return false;}}};
}
