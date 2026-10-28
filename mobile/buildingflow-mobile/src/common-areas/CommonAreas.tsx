import {useEffect,useRef,useState} from 'react';
import {Platform,Text,View} from 'react-native';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {Action} from '../components/Action';
import {FormField} from '../components/FormField';
import {Icon} from '../components/Screen';
import {useCommonAreas} from './useCommonAreas';
import {path,canManage,validDay,istanbulToday,segments,reservationBody,blankReservation,readReservations,readReservation,type Area,type Reservation} from './contracts';
import {loadDraft,storeDraft,removeDraft,type Draft} from './cache';
import {typeStyle,stateStyle,reservationStyle} from './type-style';
import {AreaEditor} from './Editor';
import {Button,styles as s} from './ui';
export function CommonAreas(){const a=useAuth(),c=a.selected;if(!c||!a.session)return <Text style={s.text}>Ortak alanlar için bir çalışma alanı seçin.</Text>;try{path(c,{page:1,areaId:null,date:istanbulToday()});}catch{return <Text style={s.text}>Ortak alanları görmek için portföyünüzden yetkili bir daire ilişkisi veya yönetici görevi seçin. Muhasebe görevi tek başına yönetim yetkisi sağlamaz.</Text>;}return <Workspace key={a.session.userId+contextKey(c)}/>;}
function Workspace(){
 const a=useAuth(),c=a.selected!,manager=canManage(c),[page,setPage]=useState(1),[area,setArea]=useState<Area|null>(null),[areaAsOf,setAreaAsOf]=useState(''),[date,setDate]=useState(istanbulToday()),[draftDate,setDraftDate]=useState(date),[editing,setEditing]=useState<Area|true|null>(null),[dateError,setDateError]=useState('');
 const list=useCommonAreas({page,areaId:null,date}),calendar=useCommonAreas({page:1,areaId:area?.id??null,date},!!area);
 const active=area?calendar:list;
 if([list.error,calendar.error].some(e=>e.includes('erişimi kaldırıldı')))return <View style={s.box}><Text accessibilityRole="alert" style={s.alert}>Ortak alan erişimi kaldırıldı. Yetkilerinizi yenileyin.</Text></View>;
 return <View style={s.gap}><View style={s.row}><Icon name="grid-outline"/><Text accessibilityRole="header" style={s.title}>Ortak alanlar</Text></View><Text style={s.muted}>{c.propertyName} · {manager?'Yönetici':'Salt okunur'}</Text>
 <Text style={s.muted}>Takvimde yalnız saat ve kullanım durumu gösterilir.</Text>
 {active.data&&<Text style={s.muted}>{active.offline?'Eski önbellek · Boş saat garantisi değildir':'Sunucudan güncellendi'}{'\n'}Son güncelleme: {new Date(active.data.asOf).toLocaleString('tr-TR')}</Text>}
 {a.offline&&<Text style={s.alert}>Çevrimdışı. Kesin rezervasyon ve iptal yapılamaz.</Text>}
 {a.offline&&!active.data&&!active.busy&&<Text style={s.muted}>Bu liste veya tarih için cihazda geçerli bir önbellek yok. İnternete bağlanıp güncelleyin.</Text>}
 {active.busy&&<Text accessibilityLiveRegion="polite" style={s.muted}>Yükleniyor…</Text>}{!!active.error&&<Text accessibilityRole="alert" style={s.alert}>{active.error}</Text>}
 <Button label="Güncelle" onPress={active.refresh} disabled={a.offline||active.busy}/>
 {editing&&<AreaEditor area={editing===true?undefined:editing} onClose={()=>setEditing(null)} onSaved={()=>{setEditing(null);setArea(null);list.refresh();}}/>}
 {!area?<>{manager&&<Button label="Ortak alan ekle" icon="add-circle-outline" onPress={()=>setEditing(true)} disabled={a.offline}/>}{list.data?.page?.items.map(item=><Action key={item.id} style={s.box} onPress={()=>{setArea(item);setAreaAsOf(list.data?.asOf??'');}} accessibilityLabel={item.name+' detay ve takvim'}><View style={s.row}><Icon name={typeStyle[item.type].icon}/><Text style={s.title}>{item.name}</Text></View><Text style={s.text}>{typeStyle[item.type].label} · {item.capacity} kişi</Text><Text style={s.muted}>{item.opensAt??'Saat belirtilmedi'}{item.closesAt?' – '+item.closesAt:''} · {item.isEnabled?'Aktif':'Pasif'}</Text></Action>)}{list.data?.page?.items.length===0&&<Text style={s.text}>Bu kapsamda ortak alan bulunamadı.</Text>}
 <View style={s.row}><Button label="Önceki" icon="chevron-back-outline" disabled={page===1} onPress={()=>setPage(p=>p-1)}/><Text style={s.muted}>Sayfa {page}</Text><Button label="Sonraki" icon="chevron-forward-outline" disabled={!list.data?.page||page*12>=list.data.page.totalCount} onPress={()=>setPage(p=>p+1)}/></View></>:<>
 <Button label="Alan listesine dön" icon="arrow-back-outline" onPress={()=>{setArea(null);setEditing(null);}}/>
 <View style={s.box}><View style={s.row}><Icon name={typeStyle[area.type].icon}/><Text accessibilityRole="header" style={s.title}>{area.name}</Text></View><Text style={s.muted}>Alan bilgisi son güncelleme: {areaAsOf?new Date(areaAsOf).toLocaleString('tr-TR'):'BİLGİ MEVCUT DEĞİL'}</Text><Text style={s.text}>{area.description||typeStyle[area.type].label}</Text><Text style={s.text}>Kapasite: {area.capacity} · Referans ücret: {area.feeAmount} TL</Text><Text style={s.muted}>{area.rules||'Ek kullanım kuralı belirtilmedi.'}</Text>{manager&&<Button label="Alanı düzenle" icon="create-outline" disabled={a.offline} onPress={()=>setEditing(area)}/>}</View>
 <FormField label="Takvim tarihi (YYYY-MM-DD)" value={draftDate} onChangeText={setDraftDate} maxLength={10}/><Button label="Tarihi göster" icon="calendar-outline" onPress={()=>{try{if(!validDay(draftDate))throw Error();setDate(draftDate);setDateError('');}catch{setDateError('Geçerli bir tarih girin (YYYY-MM-DD).');}}}/>
 {!!dateError&&<Text accessibilityRole="alert" style={s.alert}>{dateError}</Text>}
 {calendar.data?.schedule&&<View style={s.box}><Text accessibilityRole="header" style={s.title}>{date} · Kullanım saatleri</Text>{segments(calendar.data.schedule).map((v,i)=><View key={i} style={s.row}><Icon name={stateStyle[v.state].icon} color={stateStyle[v.state].color}/><Text style={[s.text,{color:stateStyle[v.state].color}]}>{v.startsAt} – {v.endsAt} · {stateStyle[v.state].label}</Text></View>)}{segments(calendar.data.schedule).length===0&&<Text style={s.muted}>Bu gün için saat aralığı tanımlı değil.</Text>}{!calendar.data.schedule.reservable&&<Text style={s.muted}>Bu alan rezervasyona açık değil.</Text>}</View>}
 {manager&&<Reservations key={area.id+date} area={area} date={date} refresh={calendar.refresh}/>}
 </>}
 </View>;
}
function Reservations({area,date,refresh}:{area:Area;date:string;refresh:()=>void}){
 const a=useAuth(),c=a.selected!,user=a.session!.userId,[form,setForm]=useState(blankReservation(date)),[requestId,setRequestId]=useState(Crypto.randomUUID()),[rows,setRows]=useState<Reservation[]>([]),[page,setPage]=useState(1),[more,setMore]=useState(false),[tick,setTick]=useState(0),[message,setMessage]=useState(''),[busy,setBusy]=useState(false),[cancelId,setCancelId]=useState<string|null>(null),[reason,setReason]=useState('');
 const mutationLock=useRef(false);
 const alive=useRef(true);useEffect(()=>{alive.current=true;return()=>{alive.current=false;};},[]);
 useEffect(()=>{let active=true;if(a.offline)return;void a.commonAreaRequest(path(c,{page,areaId:area.id,date},'/reservations')).then(async r=>{if(!r.ok)throw Error();const raw=await r.json(),v=readReservations(raw,c.propertyId,area.id);if(active){setRows(v);setMore(page*20<raw.totalCount);}}).catch(()=>{if(active){setRows([]);setMessage('Rezervasyon kayıtları alınamadı.');}});return()=>{active=false;};
 // eslint-disable-next-line react-hooks/exhaustive-deps
 },[a.offline,page,tick]);
 const update=(k:keyof typeof form,v:string)=>{setForm(f=>({...f,[k]:v}));setRequestId(Crypto.randomUUID());};
 async function run(task:()=>Promise<void>){if(mutationLock.current)return;mutationLock.current=true;setBusy(true);setMessage('');try{await task();}catch{if(alive.current)setMessage('İşlem doğrulanamadı. Kesinleşmiş saymayın; kayıtları güncelleyip aynı taslağı yeniden deneyin.');}finally{mutationLock.current=false;if(alive.current)setBusy(false);}}
 const draft=():Draft=>({areaId:area.id,requestId,date:form.date,startsAt:form.startsAt,endsAt:form.endsAt,partySize:form.partySize});
 async function create(){if(a.offline)return;if(Platform.OS!=='web')await a.commonAreaCache(db=>storeDraft(db,user,c,draft(),Date.now()));if(!alive.current)return;const body=reservationBody(form,requestId),r=await a.commonAreaRequest(path(c,{page:1,areaId:area.id,date}).split('?')[0]+'/reservations','POST',body);if(!alive.current)return;if(!r.ok){setMessage(r.status===409?'Saat dolmuş, bakım nedeniyle kapalı veya kayıt değişmiş olabilir. Takvimi güncelleyin.':'Sunucu işlemi kabul etmedi.');refresh();return;}readReservation(await r.json(),c.propertyId,area.id);try{await a.commonAreaCache(db=>removeDraft(db,user,c));}catch{/* Online result remains authoritative. */}if(alive.current){setMessage('Rezervasyon sunucuda doğrulandı.');setForm(blankReservation(date));setRequestId(Crypto.randomUUID());setTick(n=>n+1);refresh();}}
 return <View style={s.box}><Text accessibilityRole="header" style={s.title}>Yönetici rezervasyonu</Text><Text style={s.muted}>Taslak yer ayırmaz. Bağlantı gelince otomatik gönderilmez; yeniden kontrol edip onaylayın. Kişi bilgisi ve özel not bu formda tutulmaz.</Text>
 <FormField label="Başlangıç (HH:mm)" editable={!busy} value={form.startsAt} onChangeText={v=>update('startsAt',v)} maxLength={5}/><FormField label="Bitiş (HH:mm)" editable={!busy} value={form.endsAt} onChangeText={v=>update('endsAt',v)} maxLength={5}/><FormField label="Kişi sayısı" editable={!busy} value={form.partySize} keyboardType="number-pad" onChangeText={v=>update('partySize',v)} maxLength={5}/>
 {Platform.OS!=='web'&&<><Button label="Taslağı cihazda sakla" icon="save-outline" disabled={busy} onPress={()=>void run(async()=>{await a.commonAreaCache(db=>storeDraft(db,user,c,draft(),Date.now()));if(alive.current)setMessage('Taslak şifreli cihaz deposunda saklandı. Rezervasyon oluşturulmadı.');})}/><Button label="Saklanan taslağı aç" icon="document-outline" disabled={busy} onPress={()=>void run(async()=>{const d=await a.commonAreaCache(db=>loadDraft(db,user,c,Date.now()));if(!alive.current)return;if(!d||d.areaId!==area.id||d.date!==date){setMessage('Bu alan ve tarih için geçerli taslak yok.');return;}setForm({...blankReservation(date),...d});setRequestId(d.requestId);setMessage('Taslak açıldı. Güncel takvimi kontrol ederek online onaylayın.');})}/></>}
 <Button label="Online rezervasyonu onayla" icon="checkmark-circle-outline" disabled={busy||a.offline} onPress={()=>void run(create)}/>
 <Button label="Rezervasyonları yenile" disabled={busy||a.offline} onPress={()=>{setTick(n=>n+1);refresh();}}/>
 {(a.offline?[]:rows).map(r=><View key={r.id} style={s.box}><View style={s.row}><Icon name={reservationStyle[r.status].icon}/><Text style={s.text}>{r.startsAt} – {r.endsAt} · {r.partySize} kişi · {reservationStyle[r.status].label}</Text></View>{r.status==='CONFIRMED'&&<Button label="İptal işlemini aç" icon="close-circle-outline" disabled={busy||a.offline} onPress={()=>{setCancelId(r.id);setReason('');}}/>}{cancelId===r.id&&<><FormField label="İptal gerekçesi" editable={!busy} value={reason} onChangeText={setReason} maxLength={500}/><Button label="İptali online onayla" icon="close-circle-outline" disabled={busy||a.offline||!reason.trim()} onPress={()=>void run(async()=>{const result=await a.commonAreaRequest(path(c,{page:1,areaId:area.id,date}).split('?')[0]+'/reservations/'+r.id+'/cancel','POST',{reason:reason.trim(),version:r.version});if(!result.ok)throw Error();readReservation(await result.json(),c.propertyId,area.id);if(alive.current){setMessage('İptal sunucuda doğrulandı.');setCancelId(null);setTick(n=>n+1);refresh();}})}/></>}</View>)}
 <View style={s.row}><Button label="Önceki kayıtlar" icon="chevron-back-outline" disabled={page===1||a.offline} onPress={()=>setPage(p=>p-1)}/><Button label="Sonraki kayıtlar" icon="chevron-forward-outline" disabled={!more||a.offline} onPress={()=>setPage(p=>p+1)}/></View>
 {!!message&&<Text accessibilityLiveRegion="polite" style={s.alert}>{message}</Text>}
 </View>;
}
