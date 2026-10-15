import {validateQuery,type Query as FinanceQuery} from '../finance/contracts';
import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { AppState, Platform } from 'react-native';
import Constants from 'expo-constants';
import * as AuthSession from 'expo-auth-session';
import * as WebBrowser from 'expo-web-browser';
import * as Network from 'expo-network';
import { clearCache, loadCache, saveCache, saveSelection, withSyncCache, withProfileCache } from '../offline/cache';
import { synchronize, summary, SyncError, type Summary } from '../sync/engine';
import { jobs, cancel as cancelQueued, retry as retryQueued, enqueue, type Job, type SyncDb } from '../sync/outbox';
import type { PrivateFiles } from '../profile/files';
import type { Command } from '../sync/contracts';
import { synchronizePhotos, PhotoSyncError } from '../requests/engine';
import { photoJobs as listPhotoJobs, submit as submitRequest, retry as retryPhotoJob, discard as discardPhotoJob, type PhotoJob } from '../requests/outbox';
import type { Draft } from '../requests/contracts';
import { authConfig } from './config';
import { contextKey, parseSession, selectContext, type Context, type Session } from './contracts';
import { readRefresh, writeRefresh } from './storage';

WebBrowser.maybeCompleteAuthSession();
const config = authConfig();
const cacheScope = config ? `${config.issuer}|${config.api}|${config.clientId}` : 'unconfigured';
const expoGo = Platform.OS !== 'web' && Constants.appOwnership === 'expo';
type State = 'loading' | 'signedOut' | 'signedIn' | 'working';
type Value = { financeCache:<T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; financeRequest:(query:FinanceQuery)=>Promise<Response>; managerCache: <T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; managerRequest:(section:string,page:number)=>Promise<Response>; residentCache: <T>(task:(db:SyncDb)=>Promise<T>)=>Promise<T>; residentRequest:()=>Promise<Response>; profileCache: <T>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>)=>Promise<T>; profileRequest: (path:string,body?:unknown)=>Promise<Response>; syncSummary: Summary | null; syncJobs: Job[]; photoJobs: PhotoJob[]; queueRequest: (command: Command, draft: Draft, photoOperations: string[]) => Promise<void>; retryPhoto: (id: string) => Promise<void>; discardPhoto: (id: string) => Promise<void>; syncBusy: boolean; syncMessage: string; synchronizeNow: () => Promise<void>; retryJob: (id: string) => Promise<void>; cancelJob: (id: string) => Promise<void>; queueCommand: (command: Command) => Promise<void>; offline: boolean; verifiedAt: number | null; cacheNote: string; state: State; message: string; session: Session | null; selected: Context | null; ready: boolean; canRetry: boolean; retrySetup: () => void; login: () => Promise<void>; logout: () => Promise<void>; cancel: () => void; choose: (key: string) => void };
const AuthContext = createContext<Value | null>(null);
const redirectUri = AuthSession.makeRedirectUri({ scheme: 'buildingflow-dev', path: 'auth', native: 'buildingflow-dev://auth' });
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>(!config || expoGo ? 'signedOut' : 'loading');
  const [message, setMessage] = useState('');
  const [syncSummary, setSyncSummary] = useState<Summary | null>(null);
  const [syncJobs, setSyncJobs] = useState<Job[]>([]);
  const [photoJobs, setPhotoJobs] = useState<PhotoJob[]>([]);
  const [syncScopeKey, setSyncScopeKey] = useState<string | null>(null);
  const [syncBusy, setSyncBusy] = useState(false);
  const [syncMessage, setSyncMessage] = useState('');
  const syncAbort = useRef<AbortController | null>(null);
  const syncRunner = useRef<() => Promise<void>>(async () => {});
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
    syncAbort.current?.abort(); setSyncSummary(null); setSyncJobs([]); setPhotoJobs([]); setSyncScopeKey(null); setSyncBusy(false); setSyncMessage('');
    generation.current++; token.current = null; activeUser.current=null; lastContext.current = null; setSession(null); setSelected(null); setState('signedOut');
    setOffline(false); setVerifiedAt(null); setCacheNote('');
    const results = await Promise.allSettled([writeRefresh(null), clearCache()]);
    if (results.some(r => r.status === 'rejected')) throw new Error('Cihaz verileri temizlenemedi.');
  }, []);
  const install = useCallback(async (next: AuthSession.TokenResponse, epoch: number) => {
    if (!config || !next.accessToken) throw new Error('Oturum yok.');
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 15000);
    let response: Response;
    try { response = await fetch(`${config.api}/api/v1/mobile/session`, { headers: { Authorization: `Bearer ${next.accessToken}` }, signal: controller.signal }); }
    finally { clearTimeout(timeout); }
    if (!response.ok) throw new Error('Oturum doğrulanamadı.');
    const verified = parseSession(await response.json());
    if(activeUser.current && activeUser.current!==verified.userId)throw Error('Oturum hesabı değişti.');
    if (epoch !== generation.current) return;
    await writeRefresh(next.refreshToken ?? null);
    if (epoch !== generation.current) { await writeRefresh(null); return; }
    activeUser.current=verified.userId; token.current = next; setSession(verified);
    const context = verified.contexts.find(c => contextKey(c) === lastContext.current) ?? verified.contexts[0] ?? null;
    lastContext.current = context ? contextKey(context) : null; setSelected(context);
    const now = Date.now(); setVerifiedAt(now); setOffline(false);
    try { const saved = await saveCache(verified, now, lastContext.current, cacheScope); if (epoch === generation.current) setCacheNote(Platform.OS !== 'web' && !saved ? 'Çevrimdışı kopya kaydedilemedi.' : ''); }
    catch { if (epoch === generation.current) setCacheNote('Çevrimdışı kopya kaydedilemedi.'); }
    if (epoch !== generation.current) return;
    setState('signedIn'); setMessage('');
  }, []);
  useEffect(() => {
    let alive = true;
    const epoch = generation.current;
    if (!config || expoGo) return;
    const timer = setTimeout(() => { if (alive && epoch === generation.current) { generation.current++; setState('signedOut'); setMessage('Giriş sunucusuna ulaşılamadı. Bağlantını kontrol et.'); } }, 15000);
    void (async () => {
      try {
        const network = await Network.getNetworkStateAsync();
        if (Platform.OS !== 'web' && (network.isConnected === false || network.isInternetReachable === false)) {
          const cached = await readRefresh() ? await loadCache(cacheScope) : null;
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
      } catch { if (alive && epoch === generation.current) { try { await clear(); } catch { /* Session remains hidden on storage failure. */ } setMessage('Oturum açılamadı. Bağlantını kontrol edip yeniden giriş yap.'); } }
      finally { clearTimeout(timer); }
    })();
    return () => { alive = false; clearTimeout(timer); };
  }, [boot, clear, install]);
  const renew = useCallback(async (force = false) => {
    const startEpoch = generation.current;
    if (offline) { generation.current++; setBoot(v => v + 1); return; }
    if (lock.current || !token.current || !config || !discovery) return;
    try {
      const network = await Network.getNetworkStateAsync();
      if (startEpoch !== generation.current) return;
      if (Platform.OS !== 'web' && (network.isConnected === false || network.isInternetReachable === false)) {
        const cached = await loadCache(cacheScope);
        if (startEpoch !== generation.current) return;
        if (cached) { token.current = null; setOffline(true); setVerifiedAt(cached.verifiedAt); return; }
        await clear(); setMessage('Çevrimdışı kopyanın süresi doldu. İnternete bağlanıp yeniden giriş yap.'); return;
      }
    } catch { /* Continue with online verification if network status is unknown. */ }
    if (startEpoch !== generation.current || lock.current || !token.current) return;
    lock.current = true; const epoch = generation.current;
    // Refresh in the background; a failed verification still clears the session.
    try {
      const current = token.current;
      if (!current) return;
      if ((force || current.shouldRefresh()) && !current.refreshToken) throw new Error('Refresh token missing.');
      const next = force || current.shouldRefresh() ? await AuthSession.refreshAsync({ clientId: config.clientId, refreshToken: current.refreshToken }, discovery) : current;
      await install(next, epoch);
    } catch { if (epoch === generation.current) { try { await clear(); } catch { /* Hidden until secure storage recovers. */ } setMessage('Oturum doğrulanamadı. Yeniden giriş yap.'); } }
    finally { lock.current = false; }
  }, [clear, discovery, install, offline]);
  useEffect(() => {
    if (state !== 'signedIn') return;
    const current = token.current;
    const delay = current?.expiresIn ? Math.max(1000, (current.issuedAt + current.expiresIn) * 1000 - Date.now() - 30000) : 60000;
    const timer = setTimeout(() => { void renew(true); }, offline && verifiedAt ? Math.max(1000, verifiedAt + 24 * 60 * 60 * 1000 - Date.now()) : delay);
    const sub = AppState.addEventListener('change', s => { if (s === 'active') void renew(); });
    const networkSub = Network.addNetworkStateListener(s => { const next=String(s.isConnected)+':'+String(s.isInternetReachable);const previous=lastNetworkState.current;lastNetworkState.current=next;if(previous!==null && previous!==next)void renew(); });
    return () => { clearTimeout(timer); sub.remove(); networkSub.remove(); };
  }, [renew, state, offline, verifiedAt]);
  const refreshSyncView = useCallback(async () => {
    if (Platform.OS === 'web' || !session || !selected || selected.role === 'ACCOUNTANT') return;
    const epoch = generation.current; const key = contextKey(selected);
    try {
      const value = await withSyncCache(cacheScope, session.userId, async db => ({ summary: await summary(db,session.userId,key), jobs: await jobs(db,session.userId,key), photos: await listPhotoJobs(db,session.userId,key) }), () => epoch === generation.current);
      if (epoch === generation.current && lastContext.current === key) { setSyncScopeKey(key); setSyncSummary(value.summary); setSyncJobs(value.jobs); setPhotoJobs(value.photos); }
    } catch { if (epoch === generation.current) setSyncMessage('Yerel eşitleme deposu açılamadı.'); }
  }, [session, selected]);
  const synchronizeNow = useCallback(async () => {
    if (Platform.OS === 'web' || state !== 'signedIn' || offline || !session || !selected || selected.role === 'ACCOUNTANT' || !config || !token.current || lock.current) return;
    if (token.current.shouldRefresh()) { await renew(true); return; }
    lock.current = true; const epoch = generation.current; const key = contextKey(selected); const controller = new AbortController();
    syncAbort.current = controller; setSyncBusy(true); setSyncMessage('');
    const active = () => epoch === generation.current && !controller.signal.aborted && lastContext.current === key;
    try {
      const accessToken = token.current.accessToken;
      const send = async (path:string, command?:unknown) => {
          const requestAbort = new AbortController(); const cancel = () => requestAbort.abort();
          controller.signal.addEventListener('abort',cancel,{once:true});
          const timeout=setTimeout(cancel,15000);
          try {
            const response=await fetch(config.api+path,{method:command?'POST':'GET',headers:{Authorization:'Bearer '+accessToken,...(command?{'Content-Type':'application/json'}:{})},body:command?JSON.stringify(command):undefined,signal:requestAbort.signal});
            const seconds=Number(response.headers.get('Retry-After'));
            return {status:response.status,json:()=>response.json(),retryAfter:Number.isFinite(seconds)&&seconds>0?seconds*1000:undefined};
          } finally {clearTimeout(timeout);controller.signal.removeEventListener('abort',cancel);}
      };
      const result = await synchronize({ now: Date.now, active, db: task => withSyncCache(cacheScope,session.userId,task,active), request: send },session,selected);
      if (!active()) return;
      // Photos upload only after their request command is acknowledged; failures stay queued per photo.
      try { await synchronizePhotos({ now: Date.now, active, store: task => withProfileCache(cacheScope,session.userId,task,active), request: send },result.session,selectContext(result.session,key)); }
      catch (error) { if (error instanceof PhotoSyncError && error.status===401) throw new SyncError(401); if (active()) setSyncMessage('Bazı fotoğraflar gönderilemedi. Kuyrukta korunuyor.'); }
      if (!active()) return;
      setSession(result.session); setSyncScopeKey(key); setSyncSummary(result.summary);
      // Persist fresh authorization so removed scopes cannot reappear offline.
      const now=Date.now();await saveCache(result.session,now,key,cacheScope); if(active())setVerifiedAt(now);
    } catch(error) {
      if (!active()) return;
      if (error instanceof SyncError && error.status===401) { await clear(); setMessage('Oturum sona erdi. Yeniden giriş yap.'); }
      else if (error instanceof SyncError && error.status===403) { setSyncBusy(false); generation.current++; setState('loading'); setBoot(v=>v+1); }
      else setSyncMessage('Eşitleme tamamlanamadı. Bekleyen işlemler korunuyor.');
    } finally {
      if (syncAbort.current===controller)syncAbort.current=null;
      lock.current=false;
      if(epoch===generation.current){setSyncBusy(false);await refreshSyncView();}
    }
  }, [state,offline,session,selected,renew,clear,refreshSyncView]);
  useEffect(() => { syncRunner.current=synchronizeNow; syncViewRunner.current=refreshSyncView; },[synchronizeNow,refreshSyncView]);
  const selectedScope = selected ? contextKey(selected) : null;
  useEffect(() => {
    void syncViewRunner.current();
    if (state!=='signedIn' || offline || Platform.OS==='web') return;
    void syncRunner.current();
    const timer=setInterval(()=>void syncRunner.current(),15000);
    return ()=>clearInterval(timer);
  },[state,offline,selectedScope]);
  const queueCommand = async (command:Command) => {
    if (!session || !selected || Platform.OS==='web') throw new Error('Mobil oturum gerekli.');
    const epoch=generation.current; const key=contextKey(selected);
    await withSyncCache(cacheScope,session.userId,db=>enqueue(db,session,key,command,Date.now()),()=>epoch===generation.current && lastContext.current===key);
    await refreshSyncView();void synchronizeNow();
  };
  const queueRequest = async (command:Command, draft:Draft, photoOperations:string[]) => {
    if (!session || !selected || Platform.OS==='web') throw new Error('Mobil oturum gerekli.');
    const epoch=generation.current; const key=contextKey(selected);
    await withProfileCache(cacheScope,session.userId,db=>submitRequest(db,session,key,command,draft,photoOperations,Date.now()),()=>epoch===generation.current && lastContext.current===key);
    await refreshSyncView();void synchronizeNow();
  };
  const changePhoto = async (id:string,retry:boolean) => {
    if (!session || !selected || syncBusy || Platform.OS==='web') return;
    const epoch=generation.current;const key=contextKey(selected);
    try {
      await withProfileCache(cacheScope,session.userId,(db,files)=>retry?retryPhotoJob(db,session.userId,key,id,Date.now()):discardPhotoJob(db,files,session.userId,key,id),()=>epoch===generation.current && lastContext.current===key);
      await refreshSyncView();if(retry)void synchronizeNow();
    } catch {setSyncMessage('Fotoğraf işlemi değiştirilemedi. Güncel durumunu kontrol et.');}
  };
  const changeJob = async (id:string,retry:boolean) => {
    if (!session || !selected || syncBusy || Platform.OS==='web') return;
    const epoch=generation.current;const key=contextKey(selected);
    try {
      await withSyncCache(cacheScope,session.userId,db=>(retry?retryQueued:cancelQueued)(db,session,key,id,Date.now()),()=>epoch===generation.current && lastContext.current===key);
      await refreshSyncView();if(retry)void synchronizeNow();
    } catch {setSyncMessage('Bu işlem değiştirilemedi. Güncel durumunu kontrol et.');}
  };
  const profileCache = async <T,>(task:(db:SyncDb,files:PrivateFiles)=>Promise<T>):Promise<T> => {
    if(!session || activeUser.current!==session.userId || state!=='signedIn')throw Error('Oturum gerekli.');
    const epoch=generation.current;
    return withProfileCache(cacheScope,session.userId,task,()=>epoch===generation.current);
  };
  const profileRequest = async (path:string,body?:unknown):Promise<Response> => {
    if(!(/^\/api\/v1\/mobile\/profile(?:\/commands|\/photo\/[a-f0-9-]{36})?$/.test(path) || /^\/api\/v1\/mobile\/(?:owner|tenant)\?propertyId=[a-f0-9-]{36}&unitId=[a-f0-9-]{36}$/.test(path)&&body===undefined || /^\/api\/v1\/mobile\/manager\?propertyId=[a-f0-9-]{36}&role=(?:SUPER_ADMIN|SITE_ADMIN|MANAGER)&section=(?:overview|buildings|units|residents|finance|announcements|requests)&page=[0-9]{1,5}$/.test(path)&&body===undefined || /^\/api\/v1\/mobile\/finance\?propertyId=[a-f0-9-]{36}&role=(?:SUPER_ADMIN|SITE_ADMIN|MANAGER|ACCOUNTANT|OWNER|TENANT)&view=(?:charges|payments)&page=[0-9]{1,5}(?:&unitId=[a-f0-9-]{36})?(?:&chargeId=[a-f0-9-]{36})?$/.test(path)&&body===undefined) || !session || state!=='signedIn' || offline || !config)throw Error('Çevrimiçi profil oturumu gerekli.');
    const epoch=generation.current;
    if(token.current?.shouldRefresh()) {
      // A parallel synchronization may own the refresh lock; keep the draft untouched.
      if(lock.current)throw Error('Oturum yenileniyor.');
      await renew(true);
    }
    if(epoch!==generation.current || activeUser.current!==session.userId || !token.current || token.current.shouldRefresh())throw Error('Oturum yenilenemedi.');
    const controller=new AbortController();profileAborts.current.add(controller);
    const timer=setTimeout(()=>controller.abort(),15000);
    try {
      const response=await fetch(config.api+path,{method:body===undefined?'GET':'POST',headers:{Authorization:'Bearer '+token.current.accessToken,...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body),signal:controller.signal});
      if(epoch!==generation.current)throw Error('Hesap değişti.');
      if(response.status===401){await clear();throw Error('Oturum sona erdi.');}
      return response;
    }finally{clearTimeout(timer);profileAborts.current.delete(controller);}
  };
  const financeCache=async<T,>(task:(db:SyncDb)=>Promise<T>):Promise<T>=>{
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
    if(!session || !selected || !['OWNER','TENANT'].includes(selected.role) || activeUser.current!==session.userId)throw Error('Malik veya kiracı bağlamı gerekli.');
    const epoch=generation.current,key=contextKey(selected);
    return withSyncCache(cacheScope,session.userId,task,()=>epoch===generation.current&&lastContext.current===key);
  };
  const residentRequest=async()=>{
    if(!selected || !['OWNER','TENANT'].includes(selected.role) || !selected.unitId)throw Error('Malik veya kiracı bağlamı gerekli.');
    const key=contextKey(selected);const response=await profileRequest('/api/v1/mobile/'+(selected.role==='TENANT'?'tenant':'owner')+'?propertyId='+selected.propertyId+'&unitId='+selected.unitId);
    if(lastContext.current!==key)throw Error('Mülk seçimi değişti.');return response;
  };
  const login = async () => {
    if (!config || !discovery || !request || expoGo || lock.current) return;
    lock.current = true; const epoch = ++generation.current; setState('working'); setMessage('');
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
    finally { lock.current = false; }
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
  const note = expoGo ? 'Güvenli giriş için BuildingFlow development build gerekli. Expo Go özel giriş dönüş adresini desteklemiyor.' : !config ? 'Giriş yapılandırması hazır değil.' : message;
  return <AuthContext.Provider value={{ financeCache, financeRequest, managerCache, managerRequest, residentCache, residentRequest, profileCache, profileRequest, syncSummary: selected && syncScopeKey===contextKey(selected) ? syncSummary : null, syncJobs: selected && syncScopeKey===contextKey(selected) ? syncJobs : [], photoJobs: selected && syncScopeKey===contextKey(selected) ? photoJobs : [], queueRequest, retryPhoto: id=>changePhoto(id,true), discardPhoto: id=>changePhoto(id,false), syncBusy, syncMessage, synchronizeNow, queueCommand, retryJob: id=>changeJob(id,true), cancelJob: id=>changeJob(id,false), offline, verifiedAt, cacheNote, state, message: note, session, selected, ready: !!request && !!discovery && !expoGo, canRetry: !!config && !expoGo && !!message && state === 'signedOut', retrySetup, login, logout, cancel, choose }}>{children}</AuthContext.Provider>;
}
export function useOptionalAuth() { return useContext(AuthContext); }
export function useAuth() { const v = useOptionalAuth(); if (!v) throw new Error('AuthProvider gerekli.'); return v; }
