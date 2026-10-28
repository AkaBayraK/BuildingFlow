import {useEffect,useRef,useState} from 'react';
import {Platform,Text,View} from 'react-native';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {Card,Icon,styles} from '../components/Screen';
import {Action} from '../components/Action';
import {FormField} from '../components/FormField';
import {path,canManage,guestBody,localInput,readGuest,readMyVehicle,type Snapshot} from './contracts';
import {saveDraft,loadDraft,removeDraft,parseDraft,type Draft} from './cache';
import {types} from './type-style';
export function Editor({page,data,onSaved}:{page:number;data:Snapshot|null;onSaved:()=>void}){
 const a=useAuth(),c=a.selected!,user=a.session!.userId,manager=canManage(c),alive=useRef(true),lock=useRef(false);
 const [form,setForm]=useState<Draft>(()=>({kind:manager?'guest':'vehicle',requestId:Crypto.randomUUID(),plate:'',type:'CAR',brand:'',model:'',color:'',hostUnitId:'',spaceId:'',startsAt:localInput(new Date()),endsAt:localInput(new Date(Date.now()+3600000))})),[message,setMessage]=useState(''),[busy,setBusy]=useState(false),[conflicted,setConflicted]=useState(false);
 useEffect(()=>()=>{alive.current=false;},[]);
 const set=(key:keyof Draft,value:string)=>{setConflicted(false);setForm(f=>({...f,[key]:value,requestId:Crypto.randomUUID()}));};
 async function local(action:'save'|'load'|'remove'){
  if(lock.current)return;lock.current=true;setBusy(true);setMessage('');
  try{await a.parkingCache(async db=>{if(action==='save')await saveDraft(db,user,c,form,Date.now());else if(action==='remove')await removeDraft(db,user,c);else {const d=await loadDraft(db,user,c,Date.now());if(alive.current&&d)setForm(d);}});if(alive.current)setMessage(action==='save'?'Şifreli cihaz taslağı kaydedildi; sunucuya gönderilmedi.':action==='load'?'Varsa bu bağlamın taslağı açıldı.':'Cihaz taslağı kaldırıldı.');
  }catch{if(alive.current)setMessage('Taslak kullanılamadı. Native şifreli depo ve güncel hesap/alan yetkisi gerekli.');}finally{lock.current=false;if(alive.current)setBusy(false);}
 }
 async function submit(){
  if(lock.current||a.offline)return;let body:unknown;
  try{const d=parseDraft(form,c);body=manager?guestBody({...d,guestName:'',note:''},d.requestId,data?.policyConfigured?data.guestMaxHours:0):{requestId:d.requestId,propertyId:c.propertyId,unitId:c.unitId,plate:d.plate,type:d.type,brand:d.brand||null,model:d.model||null,color:d.color||null};if(!body)throw Error();}catch{setMessage('Plaka, gerekli alanlar ve geçerli İstanbul saat aralığını kontrol edin.');return;}
  lock.current=true;setBusy(true);setMessage('');
  try{if(Platform.OS!=='web')await a.parkingCache(db=>saveDraft(db,user,c,form,Date.now()));if(!alive.current)return;const r=await a.parkingRequest(path(c,page,manager?'/guests':'/vehicles'),body);if(!alive.current)return;
   if(r.status===403||r.status===404){setConflicted(true);if(alive.current){setForm(f=>({...f,plate:'',brand:'',model:'',color:''}));setMessage('Erişim kaldırıldı. Yetkilerinizi çevrimiçi yenileyin.');}return;}
   if(!r.ok){if(r.status===409)setConflicted(true);const code=await r.json().then(v=>v?.code).catch(()=>null);const labels:Record<string,string>={BF_PARKING_SPACE_OCCUPIED:'Yer/plaka artık uygun değil. Listeyi yenileyin.',BF_PARKING_QUOTA_EXCEEDED:'Araç kotası veya misafir süresi sınırı aşıldı.',BF_PARKING_PLATE_TAKEN:'Bu plaka zaten kayıtlı.',BF_PARKING_REQUEST_REUSED:'İstek içeriği değişti. Yeni taslak hazırlayın.',BF_RECORD_CHANGED:'Kayıt değişti. Listeyi yenileyin.'};setMessage(labels[String(code)]??(r.status===409?'Durum değişti. Yenileyip yeniden inceleyin.':'Sunucu kaydı onaylamadı. Girdileri ve bağlantıyı kontrol edin.'));return;}
   const v=await r.json();if(manager)readGuest(v);else readMyVehicle(v);if(!alive.current)return;
   let removed=true;if(Platform.OS!=='web')try{await a.parkingCache(db=>removeDraft(db,user,c));}catch{removed=false;}
   if(alive.current){setMessage(removed?'Sunucu kaydı onayladı.':'Sunucu onayladı, cihaz taslağı temizlenemedi. Aynı UUID tekrarında kayıt çoğalmaz.');if(removed)onSaved();}
  }catch{if(alive.current)setMessage('Sunucu sonucu doğrulanamadı. İçeriği değiştirmeden aynı istek kimliğiyle yeniden deneyin.');}finally{lock.current=false;if(alive.current)setBusy(false);}
 }
 return <Card title={manager?'Misafir / geçici park taslağı':'Araç taslağı'}>
  <Text style={styles.muted}>Seçili alan: {c.propertyName}{c.unitNumber?' · '+c.unitNumber:''}. Taslak bu hesap ve bağlama özeldir; işlem kuyruğuna alınmaz, otomatik gönderilmez.</Text>
  <FormField label="Plaka" value={form.plate} editable={!busy} autoCapitalize="characters" maxLength={16} onChangeText={v=>set('plate',v)}/>
  {!manager&&<><View style={{flexDirection:'row',flexWrap:'wrap',gap:8}}>{(Object.keys(types) as (keyof typeof types)[]).map(k=><Action key={k} style={[styles.card,{padding:10,borderColor:form.type===k?'#2056bc':'#dbe3ef',backgroundColor:form.type===k?'#e8f0ff':'#fff'}]} disabled={busy} accessibilityState={{selected:form.type===k}} onPress={()=>set('type',k)}><Icon name={types[k].icon}/><Text style={styles.text}>{types[k].label}</Text></Action>)}</View>
   <FormField label="Marka (isteğe bağlı)" value={form.brand} editable={!busy} maxLength={50} onChangeText={v=>set('brand',v)}/><FormField label="Model (isteğe bağlı)" value={form.model} editable={!busy} maxLength={50} onChangeText={v=>set('model',v)}/><FormField label="Renk (isteğe bağlı)" value={form.color} editable={!busy} maxLength={50} onChangeText={v=>set('color',v)}/></>}
  {manager&&<><Text style={styles.text}>Ev sahibi daire · Seçenek sayfası {page}</Text>{data?.units.map(u=><Action key={u.id} style={styles.card} disabled={busy} accessibilityState={{selected:form.hostUnitId===u.id}} onPress={()=>set('hostUnitId',u.id)}><Text style={styles.text}>{u.buildingName?u.buildingName+" · ":""}Daire {u.number}</Text></Action>)}
   {form.hostUnitId&&!data?.units.some(u=>u.id===form.hostUnitId)&&<Text style={styles.muted}>Taslakta seçili daire var; sunucu gönderimde yeniden doğrular.</Text>}
   <Text style={styles.text}>Ziyaretçi park yeri (isteğe bağlı) · Sayfa {page}</Text><Action disabled={busy} accessibilityState={{selected:!form.spaceId}} onPress={()=>set('spaceId','')}><Text style={styles.text}>Park yeri ayırma</Text></Action>{data?.spaces.map(s=><Action key={s.id} style={styles.card} disabled={busy} accessibilityState={{selected:form.spaceId===s.id}} onPress={()=>set('spaceId',s.id)}><Text style={styles.text}>{s.code}{s.isClosed?' · Şu an bakımda':''}</Text></Action>)}
   <FormField label="Başlangıç · İstanbul (YYYY-MM-DDTHH:mm)" value={form.startsAt} editable={!busy} onChangeText={v=>set('startsAt',v)}/><FormField label="Bitiş · İstanbul (YYYY-MM-DDTHH:mm)" value={form.endsAt} editable={!busy} onChangeText={v=>set('endsAt',v)}/><Text style={styles.muted}>Yer seçilmeyen kayıt kapasite ayırmaz. Ad/özel not mobilde tutulmaz.</Text></>}
  {Platform.OS!=='web'&&<View style={{gap:8}}><Action style={styles.card} disabled={busy} onPress={()=>void local('save')}><Text style={styles.cardTitle}>Cihaza taslak kaydet</Text></Action><Action disabled={busy} onPress={()=>void local('load')}><Text style={styles.text}>Bu bağlamın taslağını yükle</Text></Action><Action disabled={busy} onPress={()=>void local('remove')}><Text style={styles.text}>Cihaz taslağını kaldır</Text></Action></View>}
  {!!message&&<Text accessibilityLiveRegion="polite" style={styles.text}>{message}</Text>}
  <Action style={styles.button} disabled={busy||conflicted||a.offline||manager&&(!data?.policyConfigured)} onPress={()=>void submit()}><Text style={styles.buttonText}>{busy?'İşleniyor…':manager?'Sunucuda rezervasyon oluştur':'Aracı onaya gönder'}</Text></Action><Text style={styles.muted}>Kesin sonuç, kota/çakışma ve yetki kontrolleri sunucu onayına bağlıdır.</Text>
 </Card>;
}
