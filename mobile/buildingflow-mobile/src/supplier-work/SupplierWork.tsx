import {useState} from 'react';
import {StyleSheet,Text,TextInput,View} from 'react-native';
import {Action as Pressable} from '../components/Action';
import {Icon,styles} from '../components/Screen';
import {useAuth} from '../auth/AuthProvider';
import {selectPhoto} from '../profile/picker';
import {theme} from '../theme';
import {useSupplierWork} from './useSupplierWork';
import {nextStatuses,statusLabels,statusIcons,type WorkOrder,type WorkStatus} from './contracts';
import {canDiscard,canRetry,jobCommand,type Job} from './queue';
type IconName=Parameters<typeof Icon>[0]['name'];
const jobLabels:Record<Job['status'],string>={pending:'Gönderilmeyi bekliyor',sending:'Gönderiliyor',retry:'Bağlantı bekleniyor',sent:'Gönderildi',conflict:'Çakışma: iş emri başka yerde değişti',blocked:'Erişim reddedildi',failed:'Gönderilemedi',cancelled:'Vazgeçildi'};
const kindLabels={TRANSITION:'Durum',NOTE:'Saha notu',PHOTO:'Fotoğraf'} as const;
export function SupplierWork(){
 const auth=useAuth(),[page,setPage]=useState(1);const w=useSupplierWork(page);
 if(!w.allowed)return <View style={styles.emptyCard}><Icon name="lock-closed-outline" size={32}/><Text style={styles.emptyTitle}>Yönetim bağlamı gerekli</Text><Text style={styles.emptyDescription}>Tedarikçi işleri yalnız yeni yetki modelindeki yönetici çalışma alanlarında açılır.</Text></View>;
 const busyOrders=new Set(w.jobs.map(j=>j.work_order_id));
 return <View style={{gap:14}}>
  <View style={s.row}><View style={styles.flex}><Text style={styles.muted}>{w.data?'Liste tarihi: '+new Date(w.data.asOf).toLocaleString('tr-TR'):'Henüz doğrulanmış liste yok'}{w.offline?' · Çevrimdışı kopya':''}</Text></View><Pressable accessibilityRole="button" accessibilityLabel="Listeyi yenile" disabled={w.busy} onPress={w.refresh} style={s.iconButton}><Icon name="refresh-outline"/></Pressable></View>
  {!!w.error&&<Text accessibilityRole="alert" style={s.error}>{w.error}</Text>}
  {!!w.message&&<Text accessibilityLiveRegion="polite" style={styles.muted}>{w.message}</Text>}
  {w.jobs.length>0&&<View style={styles.card}><View style={s.row}><Icon name="cloud-upload-outline"/><Text style={[styles.cardTitle,styles.flex]}>Bekleyen işlemler</Text>{!auth.offline&&<Pressable accessibilityRole="button" disabled={w.sending} onPress={()=>void w.send()} style={s.smallButton}><Text style={s.smallButtonText}>{w.sending?'Gönderiliyor':'Şimdi gönder'}</Text></Pressable>}</View>
   <Text style={styles.muted}>İşlemler şifreli olarak cihazda saklanır ve bağlantı gelince aynı işlem kimliğiyle gönderilir. Çakışmada sunucudaki kayıt korunur; vazgeçip güncel listeden yeniden deneyin.</Text>
   {w.jobs.map(j=>{const cmd=jobCommand(j);return <View key={j.operation_id} style={s.job}><Text style={styles.text}>{cmd.title?cmd.title+' · ':''}{cmd.kind?kindLabels[cmd.kind]:''}{cmd.status?' → '+statusLabels[cmd.status]:''}</Text><Text style={[styles.muted,['conflict','blocked','failed'].includes(j.status)&&s.warn]}>{jobLabels[j.status]}</Text><View style={s.actions}>{canDiscard(j)&&<Pressable accessibilityRole="button" onPress={()=>void w.change(j.operation_id,'discard')} style={s.smallOutline}><Text style={s.outlineText}>Vazgeç</Text></Pressable>}{canRetry(j)&&!auth.offline&&<Pressable accessibilityRole="button" onPress={()=>void w.change(j.operation_id,'retry')} style={s.smallOutline}><Text style={s.outlineText}>Yeniden dene</Text></Pressable>}</View></View>;})}
  </View>}
  {!w.data&&!w.busy&&<View style={styles.card}><Icon name="cloud-offline-outline"/><Text style={styles.text}>{w.offline?'Bu sayfanın kullanılabilir çevrimdışı kopyası yok.':'Listeyi almak için yeniden deneyin.'}</Text></View>}
  {w.data&&!w.data.items.length&&<View style={styles.card}><Icon name="file-tray-outline"/><Text style={styles.text}>Bu yönetim alanında tedarikçi iş emri yok.</Text></View>}
  {w.data?.items.map(o=><OrderCard key={o.id} order={o} locked={busyOrders.has(o.id)} onQueue={(kind,status,text,photo)=>w.queue({kind,status,text,supplierId:o.supplierId,workOrderId:o.id,title:o.title,expectedVersion:o.version},o,photo)}/>)}
  {w.data&&w.data.total>20&&<View style={s.row}><Pressable accessibilityRole="button" disabled={w.busy||page===1} onPress={()=>setPage(n=>n-1)} style={s.smallOutline}><Text style={s.outlineText}>← Önceki</Text></Pressable><Text style={styles.muted}>Sayfa {page} / {Math.ceil(w.data.total/20)}</Text><Pressable accessibilityRole="button" disabled={w.busy||page*20>=w.data.total} onPress={()=>setPage(n=>n+1)} style={s.smallOutline}><Text style={s.outlineText}>Sonraki →</Text></Pressable></View>}
 </View>;
}
function OrderCard({order,locked,onQueue}:{order:WorkOrder;locked:boolean;onQueue:(kind:'TRANSITION'|'NOTE'|'PHOTO',status:WorkStatus|null,text:string,photo:string|null)=>Promise<void>}){
 const auth=useAuth(),[open,setOpen]=useState(false),[text,setText]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false);
 const next=auth.selected?nextStatuses(auth.selected,order.status):[];const closed=['CLOSED','REJECTED','CANCELLED'].includes(order.status);
 const photoAllowed=['DRAFT','ASSIGNED','ACCEPTED','IN_PROGRESS'].includes(order.status)&&order.photoCount<10;
 async function run(kind:'TRANSITION'|'NOTE'|'PHOTO',status:WorkStatus|null,camera=false){
  if(!text.trim()){setError(kind==='PHOTO'?'Fotoğraf için kısa bir açıklama yazın.':'Gerekçe veya not yazın.');return;}
  setBusy(true);setError('');
  try{const photo=kind==='PHOTO'?await auth.withDeviceAction(()=>selectPhoto(camera,false)):null;if(kind==='PHOTO'&&!photo)return;await onQueue(kind,status,text,photo);setText('');setOpen(false);}
  catch(e){setError(e instanceof Error?e.message:'İşlem kuyruğa alınamadı.');}finally{setBusy(false);}
 }
 return <View style={styles.card}>
  <Pressable accessibilityRole="button" accessibilityState={{expanded:open}} accessibilityLabel={order.title+', '+statusLabels[order.status]} onPress={()=>setOpen(v=>!v)}>
   <View style={s.row}><Icon name={statusIcons[order.status] as IconName}/><Text style={[styles.cardTitle,styles.flex]}>{order.title}</Text><Text style={[s.badge,order.isOverdue&&s.badgeWarn]}>{order.isOverdue?'SLA gecikti':statusLabels[order.status]}</Text></View>
   <Text style={styles.muted}>{order.supplierName} · {order.serviceName}{order.dueAt?' · SLA '+new Date(order.dueAt).toLocaleString('tr-TR'):''} · {order.photoCount} fotoğraf</Text>
  </Pressable>
  {locked&&<Text style={styles.muted}>Bu iş emri için bekleyen bir işlem var.</Text>}
  {open&&!locked&&!closed&&<View style={{gap:10}}>
   <Text style={styles.text} numberOfLines={4}>{order.description}</Text>
   <TextInput accessibilityLabel="Gerekçe, saha notu veya fotoğraf açıklaması" multiline maxLength={1000} editable={!busy} value={text} onChangeText={setText} placeholder="Gerekçe / not" style={s.input}/>
   {!!error&&<Text accessibilityRole="alert" style={s.error}>{error}</Text>}
   <View style={s.actions}>{next.map(st=><Pressable key={st} accessibilityRole="button" disabled={busy} onPress={()=>void run('TRANSITION',st)} style={st==='CANCELLED'||st==='REJECTED'?s.smallOutline:s.smallButton}><View style={s.row}><Icon name={statusIcons[st] as IconName} size={16} color={st==='CANCELLED'||st==='REJECTED'?theme.colors.primary:'#fff'}/><Text style={st==='CANCELLED'||st==='REJECTED'?s.outlineText:s.smallButtonText}>{statusLabels[st]}</Text></View></Pressable>)}</View>
   <View style={s.actions}><Pressable accessibilityRole="button" disabled={busy} onPress={()=>void run('NOTE',null)} style={s.smallOutline}><View style={s.row}><Icon name="document-text-outline" size={16}/><Text style={s.outlineText}>Not ekle</Text></View></Pressable>
    {photoAllowed&&<><Pressable accessibilityRole="button" disabled={busy} onPress={()=>void run('PHOTO',null,true)} style={s.smallOutline}><View style={s.row}><Icon name="camera-outline" size={16}/><Text style={s.outlineText}>Fotoğraf çek</Text></View></Pressable><Pressable accessibilityRole="button" disabled={busy} onPress={()=>void run('PHOTO',null,false)} style={s.smallOutline}><View style={s.row}><Icon name="images-outline" size={16}/><Text style={s.outlineText}>Galeriden</Text></View></Pressable></>}</View>
   <Text style={styles.muted}>{auth.offline?'Çevrimdışısınız: işlem cihazda şifreli bekler.':'İşlem hemen gönderilir; bağlantı koparsa cihazda bekler.'} Fotoğraf yalnız iş emri kanıtıdır ve 256 KB JPEG biçimine küçültülür.</Text>
  </View>}
 </View>;
}
const s=StyleSheet.create({row:{flexDirection:'row',alignItems:'center',gap:8},actions:{flexDirection:'row',flexWrap:'wrap',gap:8},error:{color:'#b42318',fontWeight:'600'},warn:{color:'#b45309'},iconButton:{minWidth:48,minHeight:48,alignItems:'center',justifyContent:'center'},
 smallButton:{backgroundColor:theme.colors.primary,borderRadius:12,minHeight:44,paddingHorizontal:14,justifyContent:'center'},smallButtonText:{color:'#fff',fontWeight:'700'},smallOutline:{borderWidth:1.5,borderColor:theme.colors.border,borderRadius:12,minHeight:44,paddingHorizontal:14,justifyContent:'center'},outlineText:{color:theme.colors.primary,fontWeight:'700'},
 badge:{backgroundColor:'#edf4ff',color:'#0866e8',borderRadius:10,paddingHorizontal:8,paddingVertical:3,fontSize:12,fontWeight:'700',overflow:'hidden'},badgeWarn:{backgroundColor:'#fffbeb',color:'#b45309'},
 job:{gap:4,paddingTop:8,borderTopWidth:1,borderTopColor:theme.colors.border},input:{minHeight:72,borderWidth:1.5,borderColor:theme.colors.border,borderRadius:12,padding:12,textAlignVertical:'top',color:theme.colors.text}});
