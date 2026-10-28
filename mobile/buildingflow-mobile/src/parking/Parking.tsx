import {useEffect,useRef,useState} from 'react';
import {Platform,Text,View} from 'react-native';
import {useAuth} from '../auth/AuthProvider';
import {contextKey,roleLabels} from '../auth/contracts';
import {Screen,Card,Icon,styles} from '../components/Screen';
import {Action} from '../components/Action';
import {canManage,permitted,type Snapshot} from './contracts';
import {useParking} from './useParking';
import {Editor} from './Editor';
import {statuses} from './type-style';
import {path,readGuest} from './contracts';
export function Parking({onBack}:{onBack?:()=>void}){
 const a=useAuth(),c=a.selected,key=a.session?.userId+':'+(c?contextKey(c):'none');
 return <Screen title="Araçlar / Otopark" subtitle={c?c.propertyName+(c.unitNumber?' · Daire '+c.unitNumber:''):'Yetkili alan seçin'}>{onBack&&<Action style={styles.card} onPress={onBack}><Text style={styles.cardTitle}>← Çalışma alanlarım</Text></Action>}
  <Contexts/>{!c||!permitted(c)?<Text style={styles.text}>Bu bağlamda otopark yetkisi yok.</Text>:<Content key={key}/>}</Screen>;
}
function Contexts(){const a=useAuth(),[open,setOpen]=useState(false),[limit,setLimit]=useState(20);const rows=a.session?.contexts.filter(permitted)??[];
 return <View style={{gap:8}}><Action style={styles.card} onPress={()=>setOpen(v=>!v)}><Text style={styles.cardTitle}>Alan / daire değiştir</Text><Text style={styles.muted}>{a.selected?roleLabels[a.selected.role]:''}</Text></Action>{open&&<>{rows.slice(0,limit).map(c=><Action key={contextKey(c)} style={styles.card} accessibilityState={{selected:!!a.selected&&contextKey(a.selected)===contextKey(c)}} onPress={()=>{a.choose(contextKey(c));setOpen(false);}}><Icon name={c.unitId?"home-outline":"business-outline"}/><Text style={styles.text}>{c.propertyName}{c.unitNumber?' · '+c.unitNumber:''} · {roleLabels[c.role]}</Text></Action>)}{rows.length>limit&&<Action onPress={()=>setLimit(n=>n+20)}><Text style={styles.text}>Daha fazla bağlam</Text></Action>}</>}</View>;
}
export function ParkingStatus({status}:{status:keyof typeof statuses}){const v=statuses[status];return <View style={{flexDirection:'row',gap:8,alignItems:'center'}}><Icon name={v.icon} size={20}/><Text style={styles.muted}>{v.label}</Text></View>;}
function Content(){
 const a=useAuth(),c=a.selected!,[page,setPage]=useState(1),[editing,setEditing]=useState(false),[message,setMessage]=useState(''),[busy,setBusy]=useState(false),[stale,setStale]=useState(false),lock=useRef(false),alive=useRef(true);
 // The keyed parent removes all state when account/context changes.
 useEffect(()=>()=>{alive.current=false;},[]);
 const read=useParking(page),data=read.data,manager=canManage(c),fresh=!!data&&!read.offline&&!a.offline&&!read.busy&&!stale;
 const own=contextKey(c);const current=()=>alive.current&&a.selected&&contextKey(a.selected)===own;
 const [confirm,setConfirm]=useState<string|null>(null);
 async function change(id:string,version:number,action:string){if(lock.current||!fresh)return;lock.current=true;setBusy(true);setMessage('');
  try{const r=await a.parkingRequest(path(c,page,`/guests/${id}/${action}`),{version});if(!current())return;
   if(r.status===403||r.status===404){setEditing(false);setStale(true);read.refresh();setMessage('Erişim kaldırıldı. Yetkilerinizi yenileyin.');return;}
   if(r.status===409){setStale(true);setMessage('Kayıt veya müsaitlik değişti. Yenileyip yeniden inceleyin.');return;}
   if(!r.ok){setMessage('İşlem tamamlanamadı. Tekrar deneyin.');return;}readGuest(await r.json());if(current()){setConfirm(null);read.refresh();setMessage('İşlem sunucuda kaydedildi.');}
  }catch{if(current())setMessage('İşlem tamamlanamadı. Bağlantı ve yetkinizi kontrol edin.');}finally{lock.current=false;if(current())setBusy(false);}
 }
 const max=data?Math.max(data.vehicleTotal,data.allocationTotal,data.guestTotal,data.unitTotal,data.spaceTotal):0;
 return <View style={{gap:16}}>
  <Text style={styles.muted}>Sakinler kendi araçlarını ve daire park tahsislerini görür. Misafir/geçici park yönetici işlemidir; otomatik park hakkı verilmez.</Text>
  <Action style={styles.button} disabled={read.busy||busy} onPress={()=>{setStale(false);read.refresh();}}><Text style={styles.buttonText}>Yenile</Text></Action>
  {read.busy&&<Text style={styles.muted}>Otopark yükleniyor…</Text>}{!!read.error&&<Text accessibilityRole="alert" style={styles.text}>{read.error}</Text>}
  {!!message&&<Text accessibilityLiveRegion="polite" style={styles.text}>{message}</Text>}
  {data&&<><Text style={styles.muted}>{read.offline?'Cihazdaki kopya · Güncel olmayabilir':'Sunucudan doğrulandı'} · {new Date(data.asOf).toLocaleString('tr-TR',{timeZone:'Europe/Istanbul'})}</Text>
   {!manager&&<><Card title="Araçlarım"><Text style={styles.muted}>{data.vehicleTotal} kayıt</Text>{data.vehicles.length===0&&<Text style={styles.text}>Bu sayfada araç yok.</Text>}{data.vehicles.map(v=><View key={v.id} style={{gap:8,paddingVertical:10}}><Text style={styles.cardTitle}>{v.plate}</Text><Text style={styles.muted}>{[v.brand,v.model,v.color].filter(Boolean).join(' · ')}</Text><ParkingStatus status={v.status}/>{v.reviewReason&&<Text style={styles.muted}>{v.reviewReason}</Text>}</View>)}</Card>
    <Card title="Park tahsislerim"><Text style={styles.muted}>Bu daireye açıkça verilmiş tahsisler; fiziksel doluluk bilgisi değildir.</Text>{data.allocations.length===0&&<Text style={styles.text}>Bu sayfada tahsis yok.</Text>}{data.allocations.map(v=><View key={v.id} style={{gap:6}}><Text style={styles.cardTitle}>{v.spaceCode}</Text><Text style={styles.text}>{v.startDate} – {v.endDate??'Açık uçlu'}</Text></View>)}</Card></>}
   {manager&&<GuestList data={data} disabled={!fresh||busy} confirm={confirm} onConfirm={setConfirm} onAction={(id,v,action)=>void change(id,v,action)}/>}
   <View style={{flexDirection:'row',justifyContent:'space-between',gap:12}}><Action disabled={page===1||read.busy} onPress={()=>setPage(n=>n-1)}><Text style={styles.text}>← Önceki</Text></Action><Text style={styles.muted}>Sayfa {page}</Text><Action disabled={page*20>=max||read.busy} onPress={()=>setPage(n=>n+1)}><Text style={styles.text}>Sonraki →</Text></Action></View>
  </>}
  <Action style={styles.card} onPress={()=>setEditing(v=>!v)}><View style={{flexDirection:'row',gap:10}}><Icon name={manager?'people-outline':'car-outline'}/><Text style={styles.cardTitle}>{manager?'Misafir / geçici park kaydı':'Araç ekle'}</Text></View><Text style={styles.muted}>Offline kayıt yalnız taslaktır; bağlantıda ayrıca gönderim onayı gerekir.</Text></Action>
  {editing&&<Editor page={page} data={data} onSaved={()=>{setEditing(false);read.refresh();}}/>}
  {Platform.OS==='web'&&<Text style={styles.muted}>Tarayıcı önizlemesinde kalıcı hassas cache/taslak saklanmaz.</Text>}
 </View>;
}
function GuestList({data,disabled,confirm,onConfirm,onAction}:{data:Snapshot;disabled:boolean;confirm:string|null;onConfirm:(id:string|null)=>void;onAction:(id:string,version:number,action:string)=>void}){
 return <Card title="Misafir / Geçici park"><Text style={styles.muted}>İstanbul saati. Giriş/çıkış görevli tarafından elle kaydedilir. Sensör doluluğu yoktur; çıkış yapılmamış araç yerini korur.</Text>{data.guests.length===0&&<Text style={styles.text}>Bu sayfada misafir kaydı yok.</Text>}{data.guests.map(g=><View key={g.id} style={{gap:8,paddingVertical:12,borderTopWidth:1,borderColor:'#dbe3ef'}}><Text style={styles.cardTitle}>{g.plate}</Text><ParkingStatus status={g.status}/><Text style={styles.text}>Daire {g.unitNumber} · {g.spaceCode??'Yer ayrılmadı'}</Text><Text style={styles.muted}>{new Date(g.startsAt).toLocaleString('tr-TR',{timeZone:'Europe/Istanbul'})} – {new Date(g.endsAt).toLocaleString('tr-TR',{timeZone:'Europe/Istanbul'})}</Text>
  <Text style={styles.muted}>Manuel giriş: {g.arrivedAt?new Date(g.arrivedAt).toLocaleString('tr-TR',{timeZone:'Europe/Istanbul'}):'—'} · Çıkış: {g.departedAt?new Date(g.departedAt).toLocaleString('tr-TR',{timeZone:'Europe/Istanbul'}):'—'}</Text>
  {g.status==='PLANNED'&&<><Action style={styles.button} disabled={disabled||Date.parse(data.asOf)<Date.parse(g.startsAt)||Date.parse(data.asOf)>=Date.parse(g.endsAt)} onPress={()=>onAction(g.id,g.version,'arrive')}><Text style={styles.buttonText}>Manuel giriş kaydet</Text></Action><Action disabled={disabled} onPress={()=>onConfirm(g.id)}><Text style={styles.text}>Rezervasyonu iptal et</Text></Action>{confirm===g.id&&<Action style={styles.button} disabled={disabled} onPress={()=>onAction(g.id,g.version,'cancel')}><Text style={styles.buttonText}>İptali onayla</Text></Action>}</>}
  {g.status==='ARRIVED'&&<Action style={styles.button} disabled={disabled} onPress={()=>onAction(g.id,g.version,'depart')}><Text style={styles.buttonText}>Manuel çıkış kaydet</Text></Action>}
 </View>)}</Card>;
}
