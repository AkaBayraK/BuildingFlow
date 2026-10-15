import {useState} from 'react';
import {ActivityIndicator,Pressable,StyleSheet,Text,View} from 'react-native';
import {Link} from 'expo-router';
import {useAuth} from '../auth/AuthProvider';
import {useManager} from '../manager/useManager';
import type {Section} from '../manager/contracts';
import {formatMoney} from '../owner/contracts';
import {Icon,Screen,SectionTitle,styles} from './Screen';
const labels:Record<Section,string>={overview:'Genel bakış',buildings:'Bina / bloklar',units:'Daireler',residents:'Sakinler',finance:'Aidat özeti',announcements:'Duyurular',requests:'Talepler'};
const states:Record<string,string>={OWNER:'Ev sahibi',TENANT:'Kiracı',RESIDENT:'Sakin',OPEN:'Açık borç',OVERDUE:'Gecikmiş',PAID:'Kapandı',CANCELLED:'İptal edildi',DRAFT:'Taslak',PUBLISHED:'Yayında',ARCHIVED:'Arşiv',RECORDED:'Merkez kaydı'};
export function ManagerScreen({section='overview'}:{section?:Section}){
 const auth=useAuth();return <Screen title={section==='overview'?'Yönetim':labels[section]} home={section==='overview'}><ManagerPanel key={auth.session?.userId+':'+auth.selected?.propertyId+':'+auth.selected?.role+':'+section} initialSection={section}/></Screen>;
}
function ManagerPanel({initialSection}:{initialSection:Section}){
 const [section,setSection]=useState<Section>(initialSection),[page,setPage]=useState(1);
 const {data:d,busy,error,refresh,offline}=useManager(section,page);
 const select=(s:Section)=>{setSection(s);setPage(1);};
 return <>
  {initialSection==='overview'&&<View style={s.tabs}>{(['overview','buildings','units','residents'] as Section[]).map(v=><Pressable key={v} accessibilityRole="button" accessibilityState={{selected:section===v}} onPress={()=>select(v)} style={[s.tab,section===v&&s.activeTab]}><Text style={section===v?s.activeLabel:styles.text}>{labels[v]}</Text></Pressable>)}</View>}
  <View style={s.row}><View style={styles.flex}><Text style={styles.cardTitle}>{labels[section]}</Text><Text style={styles.muted}>{d?'Veri tarihi: '+new Date(d.asOf).toLocaleString('tr-TR'):'Henüz doğrulanmış veri yok'}</Text></View><Pressable accessibilityRole="button" accessibilityLabel="Yönetim bilgilerini yenile" disabled={busy||offline} style={s.refresh} onPress={refresh}>{busy?<ActivityIndicator/>:<Icon name="refresh-outline"/>}</Pressable></View>
  {!!error&&<Text accessibilityRole="alert" style={s.error}>{error}</Text>}
  {!d&&page>1&&<Pressable accessibilityRole="button" style={styles.button} onPress={()=>setPage(1)}><Text style={styles.buttonText}>İlk sayfaya dön</Text></Pressable>}
  {!d&&!busy&&<View style={styles.card}><Icon name="cloud-offline-outline"/><Text style={styles.text}>{offline?'Bu sayfanın kullanılabilir çevrimdışı kopyası bulunmuyor.':'Bilgileri almak için yeniden deneyin.'}</Text></View>}
  {d&&<>
   <View style={s.property}><Icon name="business-outline" size={32} color="#8bd0d6"/><Text style={s.propertyName}>{d.propertyName}</Text><Text style={s.address}>{d.address}</Text><Text style={s.address}>Yetkili yönetim alanınız</Text></View>
   {section==='overview'?<>
    <View style={s.stats}>{([{label:'Bina / blok',value:d.summary.buildings,section:'buildings'},{label:'Daire',value:d.summary.units,section:'units'},{label:'Aktif kişi',value:d.summary.residents,section:'residents'}] as const).map(v=><Pressable key={v.section} accessibilityRole="button" style={s.stat} onPress={()=>select(v.section)}><Text style={s.count}>{v.value}</Text><Text style={styles.muted}>{v.label}</Text><Icon name="arrow-forward-outline" size={18}/></Pressable>)}</View>
    <View style={s.balance}><Text style={s.eyebrow}>BİNANIN AÇIK AİDAT BAKİYESİ</Text><Text style={s.balanceAmount}>{formatMoney(d.summary.balance)}</Text><Text style={s.overdue}>Gecikmiş: {formatMoney(d.summary.overdue)}</Text><Text style={styles.muted}>Tüm kayıtların merkez özeti. Bu tutar banka bakiyesi değildir.</Text><Link href="/finance" style={s.link}>Aidat detaylarına geç →</Link></View>
    <SectionTitle title="Yönetim kısayolları"/>
    <Link href="/announcements" style={styles.card}><View style={s.row}><Icon name="megaphone-outline"/><View style={styles.flex}><Text style={styles.cardTitle}>Duyurular</Text><Text style={styles.muted}>{d.summary.announcements} kayıt · Taslak, yayın ve arşiv</Text></View><Icon name="chevron-forward"/></View></Link>
    <Link href="/requests" style={styles.card}><View style={s.row}><Icon name="construct-outline" color="#a75c13"/><View style={styles.flex}><Text style={styles.cardTitle}>Gelen talepler</Text><Text style={styles.muted}>{d.summary.requests} merkez kaydı</Text></View><Icon name="chevron-forward"/></View></Link>
   </>:<>
    <Text style={styles.muted}>{d.total} {section==='residents'?'aktif kişi / daire ilişkisi':'kayıt'} · Sayfa {page}{d.total>0?' / '+Math.ceil(d.total/50):''} · En fazla 50 kayıt</Text>
    {section==='finance'&&<View style={s.balance}><Text style={s.eyebrow}>TOPLAM AÇIK AİDAT</Text><Text style={s.balanceAmount}>{formatMoney(d.summary.balance)}</Text><Text style={s.overdue}>Gecikmiş: {formatMoney(d.summary.overdue)}</Text><Text style={styles.muted}>Toplamlar yalnız bu sayfayı değil, tüm binayı kapsar.</Text></View>}
    {!d.rows.length&&<View style={styles.card}><Icon name="file-tray-outline"/><Text style={styles.text}>Bu sayfada gösterilecek kayıt yok.</Text></View>}
    {d.rows.map(r=><View key={r.id} style={styles.card}><View style={s.row}><Text style={[styles.cardTitle,styles.flex]}>{section==='units'?'Daire '+r.title:r.title}</Text>{r.state&&<Text style={[s.badge,['OVERDUE','CANCELLED'].includes(r.state)&&s.warning]}>{states[r.state]}</Text>}</View><Text style={styles.text}>{r.detail}</Text>{r.amount!==null&&<Text style={s.amount}>Kalan: {formatMoney(r.amount)}</Text>}</View>)}
    <View style={s.row}><Pressable accessibilityRole="button" disabled={busy||page===1} onPress={()=>setPage(n=>n-1)} style={[s.pageButton,(busy||page===1)&&s.disabled]}><Text style={s.activeLabel}>← Önceki</Text></Pressable><Pressable accessibilityRole="button" disabled={busy||page*50>=d.total||page>=10000} onPress={()=>setPage(n=>n+1)} style={[s.pageButton,(busy||page*50>=d.total||page>=10000)&&s.disabled]}><Text style={s.activeLabel}>Sonraki →</Text></Pressable></View>
    <Text style={styles.muted}>{section==='requests'?'Talepler merkez kayıtlarıdır. Atama, işleme alma ve kapatma durumu henüz bu modüle bağlı değildir.':section==='announcements'?'Yönetim yetkinizle taslaklar ve yönetim duyuruları da görünür. Mobil düzenleme ve yayınlama bu fazın kapsamında değildir.':section==='residents'?'Yalnız güncel, aktif daire ilişkileri gösterilir. Kimlik ve iletişim bilgileri bu mobil özette tutulmaz.':section==='finance'?'Tahsilat, iptal ve online ödeme bu ekrandan başlatılmaz; ayrıntılı finans işlemleri sonraki fazdadır.':'Aktif bina ve daireler gösterilir.'}</Text>
   </>}
  </>}
 </>;
}
const s=StyleSheet.create({tabs:{flexDirection:'row',flexWrap:'wrap',gap:8},tab:{minHeight:48,padding:12,borderRadius:12,backgroundColor:'#edf2f8',justifyContent:'center'},activeTab:{backgroundColor:'#082b49'},activeLabel:{color:'#fff',fontWeight:'600',fontSize:14},row:{flexDirection:'row',alignItems:'center',gap:10,flexWrap:'wrap'},refresh:{minHeight:48,minWidth:48,alignItems:'center',justifyContent:'center'},error:{backgroundColor:'#fff0ed',color:'#a43434',padding:14,borderRadius:12,lineHeight:22},property:{backgroundColor:'#082b49',borderRadius:22,padding:22,gap:10},propertyName:{color:'#fff',fontSize:24,fontWeight:'700'},address:{color:'#d5e6f0',fontSize:14,lineHeight:22},stats:{flexDirection:'row',flexWrap:'wrap',gap:10},stat:{flexGrow:1,flexBasis:90,backgroundColor:'#fff',borderWidth:1,borderColor:'#e5ebf2',borderRadius:18,padding:16,gap:7,minHeight:120},count:{fontSize:29,fontWeight:'700',color:'#082b49'},balance:{backgroundColor:'#e2f7f1',borderColor:'#b9e5d9',borderWidth:1,borderRadius:20,padding:20,gap:10},eyebrow:{fontSize:11,letterSpacing:1,color:'#087a68',fontWeight:'700'},balanceAmount:{fontSize:29,fontWeight:'700',color:'#082b49'},overdue:{color:'#875217',fontWeight:'600'},link:{color:'#2056bc',fontWeight:'600',paddingVertical:14,minHeight:48},badge:{fontSize:12,color:'#087a68',backgroundColor:'#dcf7ed',padding:8,borderRadius:8},warning:{color:'#a43434',backgroundColor:'#fff0ed'},amount:{fontSize:20,fontWeight:'700',color:'#082b49'},pageButton:{backgroundColor:'#082b49',padding:15,borderRadius:12,minHeight:48,flex:1,alignItems:'center'},disabled:{opacity:0.4}});
