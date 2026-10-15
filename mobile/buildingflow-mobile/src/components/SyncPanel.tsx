import { Alert, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { canRetryJob } from '../sync/outbox';
import { useAuth } from '../auth/AuthProvider';
import { Icon, styles } from './Screen';
const labels={pending:'Bekliyor',sending:'Gönderiliyor',retry:'Yeniden denenecek',sent:'Gönderildi',conflict:'Çakışma',blocked:'Yetki gerekiyor',failed:'Gönderilemedi',cancelled:'İptal edildi'};
export function SyncPanel() {
  const auth=useAuth();
  if(auth.selected?.role==='ACCOUNTANT' || !auth.selected)return null;
  const visible=auth.syncJobs.filter(j=>!['sent','cancelled'].includes(j.status)).slice(0,10);
  return <View style={styles.card}><View style={styles.locationRow}><Icon name={auth.offline?'cloud-offline-outline':'sync-outline'} /><Text accessibilityRole="header" style={styles.cardTitle}>Veri eşitleme</Text></View>
    {Platform.OS==='web'?<Text style={styles.muted}>Kalıcı işlem kuyruğu BuildingFlow mobil uygulamasında kullanılabilir.</Text>:<>
      <Text style={styles.muted}>{auth.offline?'Bağlantı geldiğinde bekleyen işlemler yeniden kontrol edilir.':auth.syncBusy?'Bilgilerin ve işlem kuyruğun kontrol ediliyor.':'Yalnız yetkili olduğun mülkün bilgileri eşitlenir.'}</Text>
      {auth.syncSummary && <View style={s.stats}><View style={s.stat}><Text style={s.number}>{auth.syncSummary.pending}</Text><Text style={styles.muted}>Bekleyen</Text></View><View style={s.stat}><Text style={s.number}>{auth.syncSummary.conflicts}</Text><Text style={styles.muted}>Çakışma</Text></View><View style={s.stat}><Text style={s.number}>{auth.syncSummary.failed+auth.syncSummary.blocked}</Text><Text style={styles.muted}>İlgi gerekiyor</Text></View></View>}
      {auth.syncSummary?.lastPulledAt && <Text style={styles.muted}>Son eşitleme: {new Date(auth.syncSummary.lastPulledAt).toLocaleString('tr-TR')}</Text>}
      {!auth.syncSummary && !auth.syncMessage && <Text style={styles.muted}>İşlem kuyruğu okunuyor…</Text>}
      {!!auth.syncMessage && <Text accessibilityRole="alert" style={s.error}>{auth.syncMessage}</Text>}
      <Pressable accessibilityRole="button" accessibilityState={{disabled:auth.offline || auth.syncBusy}} disabled={auth.offline || auth.syncBusy} style={[styles.button,(auth.offline || auth.syncBusy)&&s.disabled]} onPress={()=>void auth.synchronizeNow()}><Text style={styles.buttonText}>{auth.syncBusy?'Kontrol ediliyor…':'Şimdi eşitle'}</Text></Pressable>
      {visible.map(job=><View key={job.operation_id} style={s.job}><Text style={styles.text}>Talep işlemi · {labels[job.status]}</Text><Text style={styles.muted}>{job.status==='conflict'?'Kayıt sunucuda değişmiş. Güncel bilgi kontrol edilmeden üzerine yazılmaz.':job.status==='blocked'?'Bu işlem için erişimin yeniden doğrulanmalı.':job.attempts ? job.attempts+' gönderim denemesi':'Henüz gönderilmedi.'}</Text><View style={s.buttons}>
        {canRetryJob(job)&&<Pressable accessibilityRole="button" disabled={auth.syncBusy || auth.offline} style={s.secondary} onPress={()=>void auth.retryJob(job.operation_id)}><Text style={s.secondaryText}>Tekrar dene</Text></Pressable>}
        {job.status==='pending' && job.attempts===0 && <Pressable accessibilityRole="button" disabled={auth.syncBusy} style={s.secondary} onPress={()=>Alert.alert('Bekleyen işlem iptal edilsin mi?','Henüz gönderilmemiş bu işlem sunucuya iletilmeyecek.',[{text:'Vazgeç',style:'cancel'},{text:'İşlemi iptal et',style:'destructive',onPress:()=>void auth.cancelJob(job.operation_id)}])}><Text style={s.secondaryText}>İptal et</Text></Pressable>}
      </View></View>)}
    </>}
  </View>;
}
const s=StyleSheet.create({stats:{flexDirection:'row',gap:8},stat:{flex:1,padding:10,borderRadius:12,backgroundColor:'#f3f6fa',alignItems:'center'},number:{fontSize:24,fontWeight:'700',color:'#082b49'},disabled:{opacity:0.5},error:{color:'#9e333a',fontSize:13,lineHeight:21},job:{borderTopWidth:1,borderTopColor:'#e5ebf2',paddingTop:14,gap:5},buttons:{flexDirection:'row',gap:10},secondary:{padding:12,minHeight:48,justifyContent:'center',borderRadius:12,backgroundColor:'#edf3fc'},secondaryText:{color:'#2056bc',fontSize:14,fontWeight:'600'}});
