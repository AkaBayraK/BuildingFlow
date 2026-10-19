import {deviceAccess} from '../person-authorization/device';
import {readableOffline} from '../person-authorization/model';
import type {Snapshot} from '../offline/database';
import {purgeUnauthorizedNotices} from '../notices/store';
import {validateQuery as validateNoticeQuery,type Query as NoticeQuery,type Kind as NoticeKind} from '../notices/contracts';
import * as Crypto from 'expo-crypto';
import {queueDraft as storeRequestDraft,wireCommand,cleanRequestFiles,readPhotoCopy,savePhotoCopy,localSubmittedPhoto,type Draft} from '../requests/drafts';
import {base64Bytes,bytesBase64} from '../profile/contracts';
import {flushConsents,ConsentError} from '../consents/engine';
import {consentChanged,digestText} from '../consents/events';
import {validateQuery,type Query as FinanceQuery} from '../finance/contracts';
import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { AppState, Platform } from 'react-native';
import Constants from 'expo-constants';
import * as AuthSession from 'expo-auth-session';
import * as WebBrowser from 'expo-web-browser';
import * as Network from 'expo-network';
import { clearCache, loadCache, saveCache, saveSelection, withSyncCache, withProfileCache } from '../offline/cache';
import { synchronize, summary, SyncError, type Summary } from '../sync/engine';
import { visibleJobs, cancel as cancelQueued, retry as retryQueued, enqueue, type Job, type SyncDb } from '../sync/outbox';
import type { PrivateFiles } from '../profile/files';
import type { Command } from '../sync/contracts';
import { authConfig } from './config';
import { contextKey, parseSession, sessionFromAuthorization, selectContext, type Context, type Session } from './contracts';
import { readRefresh, writeRefresh } from './storage';
import { errorText, linkBody, parseRegistration, registrationBody, responseCode, type PersonLinkInput, type RegistrationInput, type RegistrationResult } from './registration';

WebBrowser.maybeCompleteAuthSession();
const config = authConfig();
const canonicalMode=process.env.EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED==='true';
const canonicalSync=canonicalMode&&process.env.EXPO_PUBLIC_PERSON_SYNC_ENABLED==='true';
const cacheScope = config ? `${config.issuer}|${config.api}|${config.clientId}` : 'unconfigured';
const expoGo = Platform.OS !== 'web' && Constants.appOwnership === 'expo';
type State = 'loading' | 'signedOut' | 'signedIn' | 'working' | 'locked';
type Value = { withDeviceAction:<T>(task:()=>Promise<T>)=>Promise<T>; privacyLocked:boolean; canUnlockOffline:boolean; unlockOffline:()=>Promise<void>; offlineEnabled:boolean; enableOffline:()=>Promise<void>; noticeCache:<T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; noticeRequest:(q:NoticeQuery)=>Promise<Response>; noticeRead:(kind:NoticeKind,id:string)=>Promise<Response>; noticePreference:(enabled:boolean,version:number)=>Promise<Response>; requestCache:<T>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>)=>Promise<T>; queueRequestDraft:(draft:Draft)=>Promise<void>; requestPhoto:(id:string,photoId:string,sha:string)=>Promise<{uri:string;local:boolean}>; financeCache:<T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; financeRequest:(query:FinanceQuery)=>Promise<Response>; managerCache: <T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; managerRequest:(section:string,page:number)=>Promise<Response>; residentCache: <T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; residentRequest:()=>Promise<Response>; profileCache: <T>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>)=>Promise<T>; profileRequest: (path:string,body?:unknown)=>Promise<Response>; syncSummary: Summary | null; syncJobs: Job[]; syncBusy: boolean; syncMessage: string; synchronizeNow: () => Promise<void>; retryJob: (id: string) => Promise<void>; cancelJob: (id: string) => Promise<void>; queueCommand: (command: Command) => Promise<void>; offline: boolean; verifiedAt: number | null; cacheNote: string; state: State; message: string; session: Session | null; selected: Context | null; ready: boolean; canRetry: boolean; retrySetup: () => void; login: () => Promise<void>; logout: () => Promise<void>; register: (input: RegistrationInput, requestId: string) => Promise<RegistrationResult>; linkPerson: (input: PersonLinkInput) => Promise<void>; cancel: () => void; choose: (key: string) => void };
const AuthContext = createContext<Value | null>(null);
class TimeoutError extends Error {}
// Keycloak issues 5-minute access tokens; expo-auth-session's default 10-minute margin would treat every token as expired.
const REFRESH_MARGIN_SECONDS = 30;
async function waitForUnlock(isLocked: () => boolean, limit = 15000) { const until = Date.now() + limit; while (isLocked() && Date.now() < until) await new Promise(r => setTimeout(r, 200)); }
function needsRefresh(token: AuthSession.TokenResponse): boolean { return !!token.refreshToken && !AuthSession.TokenResponse.isTokenFresh(token, -REFRESH_MARGIN_SECONDS); }
function withTimeout<T>(task: Promise<T>, ms: number): Promise<T> {
  return new Promise<T>((resolve, reject) => { const timer = setTimeout(() => reject(new TimeoutError('Zaman aşımı.')), ms); task.then(v => { clearTimeout(timer); resolve(v); }, e => { clearTimeout(timer); reject(e); }); });
}
const redirectScheme = process.env.EXPO_PUBLIC_AUTH_REDIRECT_SCHEME || 'buildingflow-dev';
const redirectUri = AuthSession.makeRedirectUri({ scheme: redirectScheme, path: 'auth', native: redirectScheme+'://auth' });
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>(!config || expoGo ? 'signedOut' : 'loading');
  const [message, setMessage] = useState('');
  const [privacyLocked,setPrivacyLocked]=useState(false);
  const [canUnlockOffline,setCanUnlockOffline]=useState(false);
  const [offlineEnabled,setOfflineEnabled]=useState(false);
  const pendingOffline=useRef<Snapshot|null>(null);
  const deviceBusy=useRef(false);
  const nativeAction=useRef(false);
  const presentationLocked=useRef(false);
  const [syncSummary, setSyncSummary] = useState<Summary | null>(null);
  const [syncJobs, setSyncJobs] = useState<Job[]>([]);
  const [syncScopeKey, setSyncScopeKey] = useState<string | null>(null);
  const [syncBusy, setSyncBusy] = useState(false);
  const [syncMessage, setSyncMessage] = useState('');
  const syncAbort = useRef<AbortController | null>(null);
  const syncRunner = useRef<() => Promise<void>>(async () => {});
  const syncRetry = useRef(false);
  const lockOwner = useRef<'renew' | 'sync' | 'login' | null>(null);
  const syncViewRunner = useRef<() => Promise<void>>(async () => {});
  const lastNetworkState = useRef<string | null>(null);
  const [offline, setOffline] = useState(false);
  const [verifiedAt, setVerifiedAt] = useState<number | null>(null);
  const [cacheNote, setCacheNote] = useState('');
  const [session, setSession] = useState<Session | null>(null);
  const [selected, setSelected] = useState<Context | null>(null);
  const [discovery, setDiscovery] = useState<AuthSession.DiscoveryDocument | null>(null);
  const [boot, setBoot] = useState(0);
  const token = useRef<AuthSession.TokenResponse | null>(null);
  const generation = useRef(0);
  const activeUser = useRef<string | null>(null);
  const lock = useRef(false);
  const lastContext = useRef<string | null>(null);
  const [request, , promptAsync] = AuthSession.useAuthRequest({ clientId: config?.clientId ?? 'unconfigured', redirectUri, responseType: AuthSession.ResponseType.Code, scopes: ['openid', 'profile'], usePKCE: true, codeChallengeMethod: AuthSession.CodeChallengeMethod.S256, extraParams: { prompt: 'login', ui_locales: 'tr' } }, discovery);
  const profileAborts = useRef(new Set<AbortController>());
  const clear = useCallback(async () => {
    profileAborts.current.forEach(c=>c.abort()); profileAborts.current.clear();
    syncAbort.current?.abort(); setSyncSummary(null); setSyncJobs([]); setSyncScopeKey(null); setSyncBusy(false); setSyncMessage('');
    presentationLocked.current=true;pendingOffline.current=null;setCanUnlockOffline(false);setOfflineEnabled(false);
    generation.current++; token.current = null; activeUser.current=null; lastContext.current = null; setSession(null); setSelected(null); setState('signedOut');
    setOffline(false); setVerifiedAt(null); setCacheNote('');
    const results = await Promise.allSettled([writeRefresh(null), clearCache(),deviceAccess.clear()]);
    if (results.some(r => r.status === 'rejected')) throw new Error('Cihaz verileri temizlenemedi.');
  }, []);
  const install = useCallback(async (next: AuthSession.TokenResponse, epoch: number) => {
    if (!config || !next.accessToken) throw new Error('Oturum yok.');
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 15000);
    let response: Response;
    try { response = await fetch(config.api+(canonicalMode?'/api/v2/mobile/authorization':'/api/v1/mobile/session'), { headers: { Authorization: `Bearer ${next.accessToken}` }, signal: controller.signal }); }
    finally { clearTimeout(timeout); }
    if (!response.ok) throw new Error('Oturum doğrulanamadı.');
    const verified = canonicalMode?sessionFromAuthorization(await response.json()):parseSession(await response.json());
    if(activeUser.current && activeUser.current!==verified.userId)throw Error('Oturum hesabı değişti.');
    if (epoch !== generation.current) return;
    await writeRefresh(next.refreshToken ?? null);
    if (epoch !== generation.current) { await writeRefresh(null); return; }
    activeUser.current=verified.userId; token.current = next; setSession(verified);
    const context = verified.contexts.find(c => contextKey(c) === lastContext.current) ?? verified.contexts[0] ?? null;
    lastContext.current = context ? contextKey(context) : null; setSelected(context);
    const now = Date.now(); setVerifiedAt(now); setOffline(false);
    try { const saved = await saveCache(verified, now, lastContext.current, cacheScope); if(epoch!==generation.current)return; if(saved&&verified.authorization){await deviceAccess.recordOnline(verified.authorization,cacheScope);const enabled=await deviceAccess.status(verified.userId,cacheScope);if(epoch===generation.current)setOfflineEnabled(enabled);} if(saved&&!canonicalMode&&Platform.OS!=='web')await withSyncCache(cacheScope,verified.userId,db=>purgeUnauthorizedNotices(db,verified),()=>epoch===generation.current); if (epoch === generation.current) setCacheNote(Platform.OS !== 'web' && !saved ? 'Çevrimdışı kopya kaydedilemedi.' : ''); }
    catch { if (epoch === generation.current) setCacheNote('Çevrimdışı kopya kaydedilemedi.'); }
    if (epoch !== generation.current) return;
    presentationLocked.current=false;pendingOffline.current=null;setCanUnlockOffline(false);setState('signedIn'); setMessage('');
  }, []);
  useEffect(() => {
    let alive = true;
    const epoch = generation.current;
    if (!config || expoGo) return;
    const timer = setTimeout(() => { if (alive && epoch === generation.current) { generation.current++; setState('signedOut'); setMessage('Giriş sunucusuna ulaşılamadı. Bağlantını kontrol et.'); } }, 15000);
    void (async () => {
      let attemptedOffline=false;
      try {
        const network = await Network.getNetworkStateAsync();
        if (Platform.OS !== 'web' && (network.isConnected === false || network.isInternetReachable === false)) {
          attemptedOffline=true;
          const cached = await readRefresh() ? await loadCache(cacheScope) : null;
          if(canonicalMode){
            if(!alive||epoch!==generation.current)return;
            if(!cached?.session.authorization){pendingOffline.current=null;setSession(null);setSelected(null);setCanUnlockOffline(false);setState('signedOut');setMessage('Geçerli yetki kopyası yok. İnternete bağlanıp yeniden giriş yap.');return;}
            const enabled=await deviceAccess.status(cached.session.userId,cacheScope);if(!alive||epoch!==generation.current)return;pendingOffline.current=cached;setCanUnlockOffline(enabled);setOffline(true);setState('locked');setMessage('Çevrimdışı bilgilerini açmak için cihaz biyometrisini doğrula.');return;
          }
          if(cached?.session.authorization){setState('signedOut');setMessage('Yetki modeli değişti. İnternete bağlanıp yeniden giriş yap.');return;}
          if (!cached) throw new Error('Çevrimdışı kopya yok veya süresi doldu.');
          if (!alive || epoch !== generation.current) return;
          token.current = null;
          const context = cached.session.contexts.find(c => contextKey(c) === cached.selectedKey) ?? cached.session.contexts[0] ?? null;
          lastContext.current = context ? contextKey(context) : null;
          activeUser.current=cached.session.userId; setSession(cached.session); setSelected(context); setVerifiedAt(cached.verifiedAt); setOffline(true); setState('signedIn'); setMessage('');
          return;
        }
        // Online authentication errors never reopen cached access.
        const d = await AuthSession.fetchDiscoveryAsync(config.issuer);
        if (!alive || epoch !== generation.current) return;
        setDiscovery(d);
        const stored = await readRefresh();
        if (!alive || epoch !== generation.current) return;
        if (!stored) { await clearCache(); setState('signedOut'); return; }
        try { lastContext.current = (await loadCache(cacheScope))?.selectedKey ?? null; } catch { /* Online verification remains authoritative. */ }
        await install(await AuthSession.refreshAsync({ clientId: config.clientId, refreshToken: stored }, d), epoch);
      } catch { if (alive && epoch === generation.current) { if(canonicalMode&&attemptedOffline){setSession(null);setSelected(null);setCanUnlockOffline(false);setState('signedOut');}else{try { await clear(); } catch { /* Session remains hidden on storage failure. */ }} setMessage('Oturum açılamadı. Bağlantını kontrol edip yeniden giriş yap.'); } }
      finally { clearTimeout(timer); }
    })();
    return () => { alive = false; clearTimeout(timer); };
  }, [boot, clear, install]);
  const renew = useCallback(async (force = false,deviceReturn=false) => {
    if(nativeAction.current&&!deviceReturn)return;
    const startEpoch = generation.current;
    if (offline) { generation.current++; setBoot(v => v + 1); return; }
    if (lock.current || !token.current || !config || !discovery) return;
    try {
      const network = await Network.getNetworkStateAsync();
      if (startEpoch !== generation.current) return;
      if (Platform.OS !== 'web' && (network.isConnected === false || network.isInternetReachable === false)) {
        const cached = await loadCache(cacheScope);
        if (startEpoch !== generation.current) return;
        if(canonicalMode){
          token.current=null;pendingOffline.current=cached?.session.authorization?cached:null;setSession(null);setSelected(null);const enabled=!!cached?.session.authorization&&await deviceAccess.status(cached.session.userId,cacheScope);if(startEpoch!==generation.current)return;setCanUnlockOffline(enabled);setOffline(true);setState('locked');setMessage('Çevrimdışı erişim için cihaz doğrulaması gerekiyor.');return;
        }
        if (cached) { token.current = null; setOffline(true); setVerifiedAt(cached.verifiedAt); return; }
        await clear(); setMessage('Çevrimdışı kopyanın süresi doldu. İnternete bağlanıp yeniden giriş yap.'); return;
      }
    } catch { /* Continue with online verification if network status is unknown. */ }
    if (startEpoch !== generation.current || lock.current || !token.current) return;
    lock.current = true; lockOwner.current = 'renew'; const epoch = generation.current;
    // Refresh in the background; a failed verification still clears the session.
    try {
      const current = token.current;
      if (!current) return;
      if ((force || needsRefresh(current)) && !current.refreshToken) throw new Error('Refresh token missing.');
      const next = force || needsRefresh(current) ? await withTimeout(AuthSession.refreshAsync({ clientId: config.clientId, refreshToken: current.refreshToken }, discovery), 15000) : current;
      await install(next, epoch);
    } catch (error) {
      // An unreachable sign-in server is not a rejected session: keep the user signed in and let the next attempt retry.
      if (error instanceof TimeoutError) { if (epoch === generation.current) setSyncMessage('Giriş sunucusuna ulaşılamadı. Bağlantını kontrol edip tekrar dene.'); return; }
      if (epoch === generation.current) { try { await clear(); } catch { /* Hidden until secure storage recovers. */ } setMessage('Oturum doğrulanamadı. Yeniden giriş yap.'); } }
    finally { lock.current = false; lockOwner.current = null; }
  }, [clear, discovery, install, offline]);
  useEffect(() => {
    if (state !== 'signedIn') return;
    const current = token.current;
    const delay = current?.expiresIn ? Math.max(1000, (current.issuedAt + current.expiresIn) * 1000 - Date.now() - 30000) : 60000;
    const timer = setTimeout(() => { void renew(true); }, offline && verifiedAt ? Math.max(1000, (session?.authorization?Date.parse(session.authorization.expiresAt):verifiedAt + 24 * 60 * 60 * 1000) - Date.now()) : delay);
    const sub = AppState.addEventListener('change', s => { if (s === 'active') void renew(); });
    const networkSub = Network.addNetworkStateListener(s => { const next=String(s.isConnected)+':'+String(s.isInternetReachable);const previous=lastNetworkState.current;lastNetworkState.current=next;if(previous!==null && previous!==next)void renew(); });
    return () => { clearTimeout(timer); sub.remove(); networkSub.remove(); };
  }, [renew, state, offline, verifiedAt,session]);
  useEffect(()=>{
    if(!canonicalMode||state!=='signedIn'||!session?.authorization)return;
    const authorization=session.authorization;let seen=verifiedAt??Date.now();
    const check=()=>{
      const now=Date.now();
      if(readableOffline(authorization,now,seen)){seen=now;return;}
      presentationLocked.current=true;generation.current++;token.current=null;pendingOffline.current=null;
      profileAborts.current.forEach(c=>c.abort());syncAbort.current?.abort();
      setSession(null);setSelected(null);setCanUnlockOffline(false);setState('signedOut');
      setMessage('Yetki kopyasının süresi doldu veya cihaz saati değişti. Çevrimiçi doğrula.');
    };
    check();const deadline=setTimeout(check,Math.max(1,Date.parse(authorization.expiresAt)-Date.now()));
    const timer=setInterval(check,1000);return()=>{clearTimeout(deadline);clearInterval(timer);};
  },[state,session,verifiedAt]);
  const refreshSyncView = useCallback(async () => {
    if (canonicalMode&&!canonicalSync) return;
    if (Platform.OS === 'web' || !session || !selected || selected.role === 'ACCOUNTANT') return;
    const epoch = generation.current; const key = contextKey(selected);
    try {
      const value = await withSyncCache(cacheScope, session.userId, async db => ({ summary: await summary(db,session.userId,key), jobs: await visibleJobs(db,session.userId,key) }), () => epoch === generation.current);
      if (epoch === generation.current && lastContext.current === key) { setSyncScopeKey(key); setSyncSummary(value.summary); setSyncJobs(value.jobs); }
    } catch { if (epoch === generation.current) setSyncMessage('Yerel eşitleme deposu açılamadı.'); }
  }, [session, selected]);
  const synchronizeNow = useCallback(async () => {
    if (nativeAction.current || canonicalMode&&!canonicalSync || Platform.OS === 'web' || state !== 'signedIn' || offline || !session || !selected || selected.role === 'ACCOUNTANT' || !config || !token.current) return;
    if (lock.current) { if (!syncRetry.current) { syncRetry.current = true; setSyncMessage(lockOwner.current === 'sync' ? 'Önceki eşitleme hâlâ sürüyor; birkaç saniye içinde tekrar denenecek.' : lockOwner.current === 'login' ? 'Giriş sürüyor; eşitleme birkaç saniye içinde tekrar denenecek.' : 'Oturum yenileniyor; eşitleme birkaç saniye içinde tekrar denenecek.'); setTimeout(() => { syncRetry.current = false; void syncRunner.current(); }, 3000); } return; }
    // A refreshed token re-renders the provider; run the synchronization again with the new session instead of silently stopping.
    if (needsRefresh(token.current)) { await renew(true); setTimeout(() => void syncRunner.current(), 0); return; }
    lock.current = true; lockOwner.current = 'sync'; const epoch = generation.current; const key = contextKey(selected); const controller = new AbortController();
    syncAbort.current = controller; setSyncBusy(true); setSyncMessage('');
    // A stalled network step must not hold the shared lock forever; aborting cancels the pending requests.
    const watchdog = setTimeout(() => controller.abort(), 60000);
    const active = () => epoch === generation.current && !controller.signal.aborted && lastContext.current === key;
    try {
      const accessToken = token.current.accessToken;
      const call = async (path:string, body?:unknown) => {
        const requestAbort = new AbortController(); const cancel = () => requestAbort.abort();
        controller.signal.addEventListener('abort',cancel,{once:true});
        const timeout=setTimeout(cancel,15000);
        try {
          const response=await fetch(config.api+path,{method:body===undefined?'GET':'POST',headers:{Authorization:'Bearer '+accessToken,...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body),signal:requestAbort.signal});
          const seconds=Number(response.headers.get('Retry-After'));
          return {status:response.status,json:()=>response.json(),retryAfter:Number.isFinite(seconds)&&seconds>0?seconds*1000:undefined};
        } finally {clearTimeout(timeout);controller.signal.removeEventListener('abort',cancel);}
      };
      // İŞ18: queued consent decisions go first, so a photo approved offline is not refused for missing consent.
      try {
        const flushed=await flushConsents({store:task=>withProfileCache(cacheScope,session.userId,task,active),request:call,now:Date.now,active,digest:digestText},session.userId);
        if(flushed.sent||flushed.refreshed)consentChanged();
      } catch(error) { if(error instanceof ConsentError&&error.status===401)throw new SyncError(401); /* Consent problems never stop request sync; held photos wait. */ }
      const result = await synchronize({
        now: Date.now, active,
        reconcile: fresh=>withProfileCache(cacheScope,session.userId,async(db,files)=>{await cleanRequestFiles(db,files,fresh);if(!canonicalMode)await purgeUnauthorizedNotices(db,fresh);},active),
        db: task => withSyncCache(cacheScope,session.userId,task,active),
        request: async (path, command) => {
          const requestAbort = new AbortController(); const cancel = () => requestAbort.abort();
          controller.signal.addEventListener('abort',cancel,{once:true});
          const timeout=setTimeout(cancel,15000);
          try {
            const wire=command?await withProfileCache(cacheScope,session.userId,(db,files)=>wireCommand(db,files,session.userId,key,command),active):undefined;
            const response=await fetch(config.api+path,{method:command?'POST':'GET',headers:{Authorization:'Bearer '+accessToken,...(command?{'Content-Type':'application/json'}:{})},body:wire?JSON.stringify(wire):undefined,signal:requestAbort.signal});
            const seconds=Number(response.headers.get('Retry-After'));
            return {status:response.status,json:()=>response.json(),retryAfter:Number.isFinite(seconds)&&seconds>0?seconds*1000:undefined};
          } finally {clearTimeout(timeout);controller.signal.removeEventListener('abort',cancel);}
        }
      },session,selected);
      if (!active()) return;
      await withProfileCache(cacheScope,session.userId,(db,files)=>cleanRequestFiles(db,files,result.session),active);
      setSession(result.session);setSelected(result.session.contexts.find(c=>contextKey(c)===key)??null); setSyncScopeKey(key); setSyncSummary(result.summary);
      // Persist fresh authorization so removed scopes cannot reappear offline.
      const now=Date.now();await saveCache(result.session,now,key,cacheScope);if(active()&&result.session.authorization)await deviceAccess.recordOnline(result.session.authorization,cacheScope); if(active())setVerifiedAt(now);
    } catch(error) {
      if (controller.signal.aborted && epoch===generation.current && lastContext.current===key) { setSyncMessage('Eşitleme zaman aşımına uğradı. Bağlantını kontrol edip tekrar dene.'); return; }
      if (!active()) return;
      if (error instanceof SyncError && error.status===401) { await clear(); setMessage('Oturum sona erdi. Yeniden giriş yap.'); }
      else if (error instanceof SyncError && error.status===403) { setSyncBusy(false); generation.current++; setState('loading'); setBoot(v=>v+1); }
      else setSyncMessage('Eşitleme tamamlanamadı. Bekleyen işlemler korunuyor.');
    } finally {
      clearTimeout(watchdog);
      if (syncAbort.current===controller)syncAbort.current=null;
      lock.current=false; lockOwner.current=null;
      if(epoch===generation.current){setSyncBusy(false);await refreshSyncView();}
    }
  }, [state,offline,session,selected,renew,clear,refreshSyncView]);
  useEffect(() => { syncRunner.current=synchronizeNow; syncViewRunner.current=refreshSyncView; },[synchronizeNow,refreshSyncView]);
  const selectedScope = selected ? contextKey(selected) : null;
  useEffect(() => {
    void syncViewRunner.current();
    if (canonicalMode&&!canonicalSync || state!=='signedIn' || offline || Platform.OS==='web') return;
    void syncRunner.current();
    const timer=setInterval(()=>{if(AppState.currentState==='active')void syncRunner.current();},15000);
    const listener=AppState.addEventListener('change',s=>{if(s==='active')void syncRunner.current();});
    return ()=>{clearInterval(timer);listener.remove();};
  },[state,offline,selectedScope]);
  const queueCommand = async (command:Command) => {
    if(canonicalMode&&!canonicalSync)throw Error('Bu yetki modelinde çevrimdışı işlemler henüz açılmadı.');
    if(canonicalMode&&(presentationLocked.current||!session?.authorization||!readableOffline(session.authorization,Date.now(),verifiedAt??0)))throw Error('Çevrimiçi yetki doğrulaması gerekli.');
    if(selected?.canonical)command={...command,contextKind:selected.canonical.kind,contextId:selected.canonical.id};
    if (!session || !selected || Platform.OS==='web') throw new Error('Mobil oturum gerekli.');
    const epoch=generation.current; const key=contextKey(selected);
    await withSyncCache(cacheScope,session.userId,db=>enqueue(db,session,key,command,Date.now()),()=>epoch===generation.current && lastContext.current===key);
    await refreshSyncView();void synchronizeNow();
  };
  const changeJob = async (id:string,retry:boolean) => {
    if(canonicalMode&&!canonicalSync)throw Error('İşlem kuyruğu henüz açılmadı.');
    if (!session || !selected || syncBusy || Platform.OS==='web') return;
    const epoch=generation.current;const key=contextKey(selected);
    try {
      await withSyncCache(cacheScope,session.userId,db=>(retry?retryQueued:cancelQueued)(db,session,key,id,Date.now()),()=>epoch===generation.current && lastContext.current===key);
      await withProfileCache(cacheScope,session.userId,(db,files)=>cleanRequestFiles(db,files,session),()=>epoch===generation.current&&lastContext.current===key);
      await refreshSyncView();if(retry)void synchronizeNow();
    } catch {setSyncMessage('Bu işlem değiştirilemedi. Güncel durumunu kontrol et.');}
  };
  const profileCache = async <T,>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>):Promise<T> => {
    if(canonicalMode&&(presentationLocked.current||!canonicalSync||!session?.authorization||!readableOffline(session.authorization,Date.now(),verifiedAt??0)))throw Error('Güncel mobil işlem yetkisi gerekli.');
    if(!session || activeUser.current!==session.userId || state!=='signedIn')throw Error('Oturum gerekli.');
    const epoch=generation.current;
    return withProfileCache(cacheScope,session.userId,task,()=>epoch===generation.current);
  };
  const profileRequest = async (path:string,body?:unknown):Promise<Response> => {
    if(canonicalMode&&(!canonicalSync||!(/^\/api\/v1\/mobile\/(?:profile(?:\/.*)?|consents(?:\/commands)?|sync\/requests\/[a-f0-9-]{36}\/photo(?:\?.*)?)$/.test(path))))throw Error('Bu işlem yeni yetki modelinde kapalı.');
    if(!(/^\/api\/v1\/mobile\/notices\/(?:read|preference)$/.test(path)&&body!==undefined || /^\/api\/v1\/mobile\/notices\?propertyId=[a-f0-9-]{36}&role=(?:SUPER_ADMIN|SITE_ADMIN|MANAGER|ACCOUNTANT|OWNER|TENANT|RESIDENT)&kind=(?:ANNOUNCEMENT|MESSAGE)&page=[0-9]{1,5}(?:&unitId=[a-f0-9-]{36})?(?:&itemId=[a-f0-9-]{36})?$/.test(path)&&body===undefined || /^\/api\/v1\/mobile\/sync\/requests\/[a-f0-9-]{36}\/photo(?:\?contextKind=(?:PLATFORM|MANAGEMENT|OWNER|TENANT|RESIDENT)&contextId=[a-f0-9-]{36})?$/.test(path)&&body===undefined || /^\/api\/v1\/mobile\/profile(?:\/commands|\/photo\/[a-f0-9-]{36})?$/.test(path) || path==='/api/v1/mobile/consents'&&body===undefined || path==='/api/v1/mobile/consents/commands'&&body!==undefined || path==='/api/v1/mobile/person-links'&&body!==undefined || /^\/api\/v1\/mobile\/(?:owner|tenant)\?propertyId=[a-f0-9-]{36}&unitId=[a-f0-9-]{36}$/.test(path)&&body===undefined || /^\/api\/v1\/mobile\/manager\?propertyId=[a-f0-9-]{36}&role=(?:SUPER_ADMIN|SITE_ADMIN|MANAGER)&section=(?:overview|buildings|units|residents|finance|announcements|requests)&page=[0-9]{1,5}$/.test(path)&&body===undefined || /^\/api\/v1\/mobile\/finance\?propertyId=[a-f0-9-]{36}&role=(?:SUPER_ADMIN|SITE_ADMIN|MANAGER|ACCOUNTANT|OWNER|TENANT)&view=(?:charges|payments)&page=[0-9]{1,5}(?:&unitId=[a-f0-9-]{36})?(?:&chargeId=[a-f0-9-]{36})?$/.test(path)&&body===undefined) || !session || state!=='signedIn' || offline || !config)throw Error('Çevrimiçi profil oturumu gerekli.');
    const epoch=generation.current;
    if(token.current && needsRefresh(token.current)) {
      // Wait for a refresh already in flight instead of failing every parallel screen request.
      if(lock.current)await waitForUnlock(()=>lock.current);
      if(token.current && needsRefresh(token.current)){if(lock.current)throw Error('Oturum yenileniyor.');await renew(true);}
    }
    if(epoch!==generation.current || activeUser.current!==session.userId || !token.current || needsRefresh(token.current))throw Error('Oturum yenilenemedi.');
    const controller=new AbortController();profileAborts.current.add(controller);
    const timer=setTimeout(()=>controller.abort(),15000);
    try {
      const response=await fetch(config.api+path,{method:body===undefined?'GET':'POST',headers:{Authorization:'Bearer '+token.current.accessToken,...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body),signal:controller.signal});
      if(epoch!==generation.current)throw Error('Hesap değişti.');
      if(response.status===401){await clear();throw Error('Oturum sona erdi.');}
      return response;
    }finally{clearTimeout(timer);profileAborts.current.delete(controller);}
  };
  const noticeCache=async<T,>(task:(db:SyncDb)=>Promise<T>):Promise<T>=>{
    if(canonicalMode)throw Error('Eski önbellek bu yetki modelinde kullanılamaz.');
    if(!session||!selected||activeUser.current!==session.userId)throw Error('Bildirim oturumu gerekli.');
    const epoch=generation.current,key=contextKey(selected);return withSyncCache(cacheScope,session.userId,task,()=>epoch===generation.current&&lastContext.current===key);
  };
  const noticeRequest=async(q:NoticeQuery)=>{
    if(!selected)throw Error('Mülk seçimi gerekli.');validateNoticeQuery(q);
    const key=contextKey(selected);const response=await profileRequest('/api/v1/mobile/notices?propertyId='+selected.propertyId+'&role='+selected.role+'&kind='+q.kind+'&page='+q.page+(selected.unitId?'&unitId='+selected.unitId:'')+(q.itemId?'&itemId='+q.itemId:''));if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const noticeRead=async(kind:NoticeKind,id:string)=>{
    if(!selected)throw Error('Mülk seçimi gerekli.');validateNoticeQuery({kind,page:1,itemId:id});const key=contextKey(selected);
    const response=await profileRequest('/api/v1/mobile/notices/read',{propertyId:selected.propertyId,unitId:selected.unitId,role:selected.role,kind,id});if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const noticePreference=async(enabled:boolean,version:number)=>{
    if(!selected||typeof enabled!=='boolean'||!Number.isSafeInteger(version)||version<0)throw Error('Bildirim tercihi geçersiz.');const key=contextKey(selected);
    const response=await profileRequest('/api/v1/mobile/notices/preference',{propertyId:selected.propertyId,unitId:selected.unitId,role:selected.role,highlightsEnabled:enabled,expectedVersion:version});if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const requestCache=async<T,>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>):Promise<T>=>{
    if(canonicalMode&&(presentationLocked.current||!canonicalSync||!session?.authorization||!readableOffline(session.authorization,Date.now(),verifiedAt??0)))throw Error('Güncel mobil işlem yetkisi gerekli.');
    if(!session||!selected||selected.role==='ACCOUNTANT'||activeUser.current!==session.userId)throw Error('Yetkili talep oturumu gerekli.');
    const epoch=generation.current,key=contextKey(selected);return withProfileCache(cacheScope,session.userId,task,()=>epoch===generation.current&&lastContext.current===key);
  };
  const queueRequestDraft=async(draft:Draft)=>{
    if(!session||!selected)throw Error('Talep bağlamı gerekli.');
    await requestCache((db,files)=>storeRequestDraft(db,files,session,selected,draft,Date.now()));
    await refreshSyncView();void synchronizeNow();
  };
  const requestPhoto=async(id:string,photoId:string,sha:string)=>{
    if(!/^[a-f0-9-]{36}$/.test(id)||!/^[a-f0-9-]{36}$/.test(photoId)||!/^[a-f0-9]{64}$/i.test(sha))throw Error('Talep fotoğrafı geçersiz.');
    const digest=async(b:Uint8Array)=>Array.from(new Uint8Array(await Crypto.digest(Crypto.CryptoDigestAlgorithm.SHA256,new Uint8Array(b).buffer)),x=>x.toString(16).padStart(2,'0')).join('');
    const cached=await requestCache(async(db,files)=>{try{const value=await readPhotoCopy(db,files,session!.userId,contextKey(selected!),id,photoId,sha,Date.now());if(value&&await digest(base64Bytes(value))===sha.toLowerCase())return value;}catch{}return null;});
    if(cached&&offline)return {uri:'data:image/jpeg;base64,'+cached,local:false};
    if(offline){const original=await requestCache((db,files)=>localSubmittedPhoto(db,files,session!.userId,contextKey(selected!),id,photoId,sha));if(original)return {uri:'data:image/jpeg;base64,'+original,local:true};throw Error('Bu fotoğraf henüz cihaza indirilmemiş. Bağlantı kurunca yeniden deneyin.');}
    const response=await profileRequest('/api/v1/mobile/sync/requests/'+id+'/photo'+(selected?.canonical?'?contextKind='+selected.canonical.kind+'&contextId='+selected.canonical.id:''));if(!response.ok)throw Error('Fotoğraf alınamadı veya erişim sona erdi.');
    const bytes=new Uint8Array(await response.arrayBuffer());if(bytes.length>262144||await digest(bytes)!==sha.toLowerCase())throw Error('Fotoğraf bütünlüğü doğrulanamadı.');
    const value=bytesBase64(bytes);await requestCache(async(db,files)=>{
      await savePhotoCopy(db,files,session!.userId,contextKey(selected!),id,photoId,sha,value,Date.now());
      // A verified server copy replaces the encrypted upload copy for this same record.
      const pending=await db.getAllAsync<{operation_id:string;body_file:string;payload:string}>("SELECT f.operation_id,f.body_file,o.payload FROM request_files f JOIN sync_outbox o ON o.operation_id=f.operation_id WHERE f.user_id=? AND o.status='sent'",session!.userId);
      for(const f of pending)if(JSON.parse(f.payload).recordId===id){await files.drop(f.body_file);await db.runAsync('DELETE FROM request_files WHERE operation_id=? AND user_id=?',f.operation_id,session!.userId);}
    });return {uri:'data:image/jpeg;base64,'+value,local:false};
  };
  const financeCache=async<T,>(task:(db:SyncDb)=>Promise<T>):Promise<T>=>{
    if(canonicalMode)throw Error('Eski önbellek bu yetki modelinde kullanılamaz.');
    if(!session||!selected||activeUser.current!==session.userId)throw Error('Finans oturumu gerekli.');
    validateQuery(selected,{view:'charges',page:1,chargeId:null});
    const epoch=generation.current,key=contextKey(selected);
    return withSyncCache(cacheScope,session.userId,task,()=>epoch===generation.current&&lastContext.current===key);
  };
  const financeRequest=async(q:FinanceQuery)=>{
    if(!selected)throw Error('Finans bağlamı gerekli.');validateQuery(selected,q);
    const key=contextKey(selected);const response=await profileRequest('/api/v1/mobile/finance?propertyId='+selected.propertyId+'&role='+selected.role+'&view='+q.view+'&page='+q.page+(selected.unitId?'&unitId='+selected.unitId:'')+(q.chargeId?'&chargeId='+q.chargeId:''));
    if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const managerCache=async<T,>(task:(db:SyncDb)=>Promise<T>):Promise<T>=>{
    if(canonicalMode)throw Error('Eski önbellek bu yetki modelinde kullanılamaz.');
    if(!session||!selected||!['SUPER_ADMIN','SITE_ADMIN','MANAGER'].includes(selected.role)||selected.unitId!==null||activeUser.current!==session.userId)throw Error('Yönetici bağlamı gerekli.');
    const epoch=generation.current,key=contextKey(selected);
    return withSyncCache(cacheScope,session.userId,task,()=>epoch===generation.current&&lastContext.current===key);
  };
  const managerRequest=async(section:string,page:number)=>{
    if(!selected||!['SUPER_ADMIN','SITE_ADMIN','MANAGER'].includes(selected.role)||selected.unitId!==null||!['overview','buildings','units','residents','finance','announcements','requests'].includes(section)||!Number.isInteger(page)||page<1||page>10000)throw Error('Yönetici bağlamı gerekli.');
    const key=contextKey(selected);const response=await profileRequest('/api/v1/mobile/manager?propertyId='+selected.propertyId+'&role='+selected.role+'&section='+section+'&page='+page);
    if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const residentCache=async<T,>(task:(db:SyncDb)=>Promise<T>):Promise<T>=>{
    if(canonicalMode)throw Error('Eski önbellek bu yetki modelinde kullanılamaz.');
    if(!session || !selected || !['OWNER','TENANT'].includes(selected.role) || activeUser.current!==session.userId)throw Error('Malik veya kiracı bağlamı gerekli.');
    const epoch=generation.current,key=contextKey(selected);
    return withSyncCache(cacheScope,session.userId,task,()=>epoch===generation.current&&lastContext.current===key);
  };
  const residentRequest=async()=>{
    if(!selected || !['OWNER','TENANT'].includes(selected.role) || !selected.unitId)throw Error('Malik veya kiracı bağlamı gerekli.');
    const key=contextKey(selected);const response=await profileRequest('/api/v1/mobile/'+(selected.role==='TENANT'?'tenant':'owner')+'?propertyId='+selected.propertyId+'&unitId='+selected.unitId);
    if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const register = async (input: RegistrationInput, requestId: string): Promise<RegistrationResult> => {
    if(canonicalMode)throw Error('Yeni yetki modelinde kayıt/eşleşme önce sunucuda hazırlanmalıdır.');
    if (!config) throw new Error(errorText(null));
    const controller = new AbortController(); const timer = setTimeout(() => controller.abort(), 20000);
    try {
      const response = await fetch(`${config.api}/api/v1/mobile/registrations`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(registrationBody(requestId, input)), signal: controller.signal });
      if (!response.ok) throw new Error(errorText(await responseCode(response)));
      return parseRegistration(await response.json());
    } catch (error) { throw error instanceof Error && error.name !== 'AbortError' && !(error instanceof TypeError) ? error : new Error(errorText('BF_IDENTITY_UNAVAILABLE')); }
    finally { clearTimeout(timer); }
  };
  const linkPerson = async (input: PersonLinkInput): Promise<void> => {
    const response = await profileRequest('/api/v1/mobile/person-links', linkBody(input));
    if (!response.ok) throw new Error(errorText(await responseCode(response)));
    // The new person link changes the verified property/role contexts; reload them from the server.
    await renew(true);
  };
  const login = async () => {
    if (!config || !discovery || !request || expoGo || lock.current) return;
    lock.current = true; lockOwner.current = 'login'; const epoch = ++generation.current; setState('working'); setMessage('');
    let stage = 'Tarayıcı girişi';
    try {
      const result = await promptAsync();
      if (epoch !== generation.current) return;
      if (result.type !== 'success') { setState('signedOut'); setMessage(result.type === 'error' ? 'Giriş tamamlanamadı. Yeniden dene.' : 'Giriş iptal edildi.'); return; }
      if (!request.codeVerifier || !result.params.code) throw new Error('Kod eksik.');
      stage = 'Oturum anahtarı alma';
      const next = await AuthSession.exchangeCodeAsync({ clientId: config.clientId, code: result.params.code, redirectUri, extraParams: { code_verifier: request.codeVerifier } }, discovery);
      stage = 'API oturum doğrulaması';
      await install(next, epoch);
    } catch { if (epoch === generation.current) { try { await clear(); } catch { /* Never fall back to plain storage. */ } setMessage(`${stage} tamamlanamadı. Bağlantını kontrol edip yeniden dene.`); } }
    finally { lock.current = false; lockOwner.current = null; }
  };
  const cancel = () => {
    void clear().catch(() => setMessage('Güvenli depo temizlenemedi. Yeniden dene.')); setMessage('Giriş iptal edildi.');
    try { WebBrowser.dismissAuthSession(); } catch { /* Browser already closed. */ }
  };
  const retrySetup = () => { generation.current++; setState('loading'); setMessage(''); setDiscovery(null); setBoot(v => v + 1); };
  const logout = async () => {
    const refresh = token.current?.refreshToken;
    try { await clear(); } catch { setMessage('Güvenli depodaki oturum silinemedi. Uygulamayı kapatıp yeniden dene.'); return; }
    if (refresh && config) {
      try {
        const controller = new AbortController(); const timer = setTimeout(() => controller.abort(), 10000);
        try {
          const response = await fetch(`${config.issuer}/protocol/openid-connect/logout`, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: new URLSearchParams({ client_id: config.clientId, refresh_token: refresh }).toString(), signal: controller.signal });
          if (!response.ok) throw new Error('Logout failed.');
        } finally { clearTimeout(timer); }
      }
      catch { setMessage('Cihaz oturumu kapandı. Sunucu oturumunun kapatılması doğrulanamadı.'); }
    }
  };
  const choose = (key: string) => { if (session) { const context = selectContext(session, key); syncAbort.current?.abort(); lastContext.current = key; setSelected(context); void saveSelection(key, cacheScope).catch(() => setCacheNote('Mülk seçimi cihazda saklanamadı.')); } };
  const unlockOffline=async()=>{
    if(!canonicalMode||!canUnlockOffline||state!=='locked'||deviceBusy.current)return;
    deviceBusy.current=true;
    const epoch=generation.current;setMessage('Cihaz doğrulanıyor.');
    try{const cached=await loadCache(cacheScope);if(!cached?.session.authorization||cached.session.userId!==pendingOffline.current?.session.userId||!await readRefresh())throw Error('Yetki kopyası yok.');await deviceAccess.unlock(cached.session.authorization,cacheScope);if(epoch!==generation.current)return;const context=cached.session.contexts.find(c=>contextKey(c)===cached.selectedKey)??cached.session.contexts[0]??null;activeUser.current=cached.session.userId;token.current=null;lastContext.current=context?contextKey(context):null;setSession(cached.session);setSelected(context);setVerifiedAt(cached.verifiedAt);setOffline(true);presentationLocked.current=false;setCanUnlockOffline(false);setState('signedIn');setMessage('');}
    catch{if(epoch===generation.current)setMessage('Cihaz doğrulanamadı veya yetki süresi doldu. Yeniden deneyebilir ya da internete bağlanabilirsin.');}finally{deviceBusy.current=false;}
  };
  // The OS picker may make the app inactive/background. Keep its form mounted behind
  // the privacy overlay; verify again before exposing or saving the returned image.
  const withDeviceAction=async<T,>(task:()=>Promise<T>):Promise<T>=>{
    if(!canonicalMode)return task();
    if(nativeAction.current||presentationLocked.current||state!=='signedIn'||!session?.authorization)throw Error('Fotoğraf işlemi için açık oturum gerekli.');
    const epoch=generation.current,key=lastContext.current;
    if(lock.current)await waitForUnlock(()=>lock.current);
    if(lock.current||epoch!==generation.current||presentationLocked.current)throw Error('Eşitleme sürüyor veya oturum kilitlendi. Yeniden deneyin.');
    const current=token.current;
    nativeAction.current=true;setPrivacyLocked(true);
    try{
      const result=await task();
      if(epoch!==generation.current||AppState.currentState!=='active')throw Error('Oturum değişti.');
      if(offline)await deviceAccess.unlock(session.authorization,cacheScope);
      else{await renew(true,true);if(!token.current||token.current===current)throw Error('Çevrimiçi oturum doğrulanamadı.');}
      if(epoch!==generation.current||lastContext.current!==key||AppState.currentState!=='active')throw Error('Çalışma alanı değişti veya uygulama arka planda.');
      return result;
    }catch(error){
      if(epoch===generation.current){presentationLocked.current=true;generation.current++;token.current=null;pendingOffline.current=null;setSession(null);setSelected(null);setCanUnlockOffline(false);setState('locked');setMessage('Fotoğraf dönüşünde oturum doğrulanamadı. Bağlantıyı yeniden deneyin.');}
      throw error;
    }finally{nativeAction.current=false;setPrivacyLocked(AppState.currentState!=='active');}
  };
  const enableOffline=async()=>{
    if(!canonicalMode||offline||state!=='signedIn'||!session?.authorization||!token.current||deviceBusy.current)return;
    deviceBusy.current=true;
    const epoch=generation.current;try{await deviceAccess.enroll(session.authorization,cacheScope);if(epoch===generation.current){setOfflineEnabled(true);setCacheNote('Çevrimdışı erişim cihaz biyometrisiyle etkinleştirildi.');}}
    catch{if(epoch===generation.current)setCacheNote('Biyometrik erişim etkinleştirilemedi. Güvenli mobil build ve kayıtlı biyometri gerekiyor.');}finally{deviceBusy.current=false;}
  };
  useEffect(()=>{
    if(!canonicalMode)return;
    const sub=AppState.addEventListener('change',next=>{
      setPrivacyLocked(next!=='active'||nativeAction.current);
      if(next==='background'&&!nativeAction.current&&(state==='signedIn'||state==='locked'&&deviceBusy.current)){
        presentationLocked.current=true;generation.current++;profileAborts.current.forEach(c=>c.abort());syncAbort.current?.abort();token.current=null;
        pendingOffline.current=session&&verifiedAt?{session,verifiedAt,selectedKey:lastContext.current}:null;
        setSession(null);setSelected(null);setState('locked');setCanUnlockOffline(offlineEnabled);setMessage('Bilgilerin kilitlendi. Yeniden doğrula.');
      }else if(next==='active'&&state==='locked'&&!deviceBusy.current){setBoot(v=>v+1);}
      else if(next==='active'&&session?.authorization&&!readableOffline(session.authorization,Date.now(),verifiedAt??0)){setSession(null);setSelected(null);setCanUnlockOffline(false);setState('signedOut');setMessage('Yetki kopyasının süresi doldu. Çevrimiçi giriş yap.');}
    });return()=>sub.remove();
  },[state,session,verifiedAt,offlineEnabled]);
  const note = expoGo ? 'Güvenli giriş için BuildingFlow development build gerekli. Expo Go özel giriş dönüş adresini desteklemiyor.' : !config ? 'Giriş yapılandırması hazır değil.' : message;
  return <AuthContext.Provider value={{ withDeviceAction,privacyLocked,canUnlockOffline,unlockOffline,offlineEnabled,enableOffline,noticeCache, noticeRequest, noticeRead, noticePreference, requestCache, queueRequestDraft, requestPhoto, financeCache, financeRequest, managerCache, managerRequest, residentCache, residentRequest, profileCache, profileRequest, register, linkPerson, syncSummary: selected && syncScopeKey===contextKey(selected) ? syncSummary : null, syncJobs: selected && syncScopeKey===contextKey(selected) ? syncJobs : [], syncBusy, syncMessage, synchronizeNow, queueCommand, retryJob: id=>changeJob(id,true), cancelJob: id=>changeJob(id,false), offline, verifiedAt, cacheNote, state, message: note, session, selected, ready: !!request && !!discovery && !expoGo, canRetry: !!config && !expoGo && !!message && (state === 'signedOut'||state==='locked'), retrySetup, login, logout, cancel, choose }}>{children}</AuthContext.Provider>;
}
export function useOptionalAuth() { return useContext(AuthContext); }
export function useAuth() { const v = useOptionalAuth(); if (!v) throw new Error('AuthProvider gerekli.'); return v; }
