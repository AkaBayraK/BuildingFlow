import {useState} from 'react';
import {StyleSheet,Text,View} from 'react-native';
import {Action as Pressable} from './Action';
import {Icon,styles} from './Screen';
import {useAuth} from '../auth/AuthProvider';
import {useConsents} from '../consents/ConsentProvider';
import {ConsentSheet,confirmWithdraw} from '../consents/ConsentSheet';
import {stateLabel,textOutdatedCode} from '../consents/contracts';
import {canDiscard,canRetry,type Effective} from '../consents/store';
const when=(value:string)=>new Date(value).toLocaleString('tr-TR',{dateStyle:'medium',timeStyle:'short'});
const tone={ACTIVE:{bg:'#e7f6ee',fg:'#1f6b43'},OUTDATED:{bg:'#fff3db',fg:'#7a5214'},WITHDRAWN:{bg:'#f1f3f6',fg:'#4c5b6e'},NONE:{bg:'#eef2f8',fg:'#3d5373'}} as const;
// T18.5 "Rızalarım": current state per type. The person sees no history; the centre keeps every approval and withdrawal.
export function ConsentCenter(){
 const auth=useAuth(),c=useConsents();
 const [open,setOpen]=useState<string|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const entry=c.items.find(e=>e.item.status.typeCode===open)??null;
 const act=async(e:Effective,action:'APPROVE'|'WITHDRAW')=>{setBusy(true);setError('');try{await c.decide(e,action);setOpen(null);}catch(x){setError(x instanceof Error?x.message:'Kaydedilemedi.');}finally{setBusy(false);}};
 const held=c.held.PROFILE_PHOTO+c.held.REQUEST_PHOTO;
 return <View style={styles.card}>
  <View style={styles.locationRow}><Icon name="shield-checkmark-outline"/><Text accessibilityRole="header" style={[styles.cardTitle,styles.flex]}>Aydınlatma ve açık rıza</Text></View>
  <Text style={styles.muted}>Fotoğraf eklemek isteğe bağlıdır ve açık rızanıza bağlıdır. Rızanızı istediğiniz zaman geri çekebilirsiniz.</Text>
  {!c.loaded&&<Text style={styles.muted}>Rıza bilgileri okunuyor…</Text>}
  {c.loaded&&!c.items.length&&<Text style={styles.muted}>{auth.offline?'Rıza bilgileri bu cihazda yok. İnternete bağlandığınızda gösterilir.':'Yayımlanmış rıza metni yok.'}</Text>}
  {!!c.message&&<Text accessibilityRole="alert" style={s.warn}>{c.message}</Text>}
  {held>0&&<Text style={s.warn}>Açık rıza bekleyen {held} fotoğraflı işlem var. İlgili rızayı verdiğinizde yeniden gönderilir.</Text>}
  {c.items.map(e=>{const st=e.item.status,t=tone[e.state],text=e.item.currentText;
   const detail=e.pending==='APPROVE'?'Onayınız bu cihazda; bağlantı gelince merkeze iletilecek.':e.pending==='WITHDRAW'?'Geri çekme bu cihazda; bağlantı gelince merkeze iletilecek.'
    :e.state==='ACTIVE'||e.state==='OUTDATED'?`Sürüm ${e.approvedVersion} · ${st.approvedAt?when(st.approvedAt):''} ${st.kind==='INFORMATION'?'okundu':'onaylandı'}${e.state==='OUTDATED'&&st.currentVersion?` · güncel sürüm ${st.currentVersion}`:''}`
    :text?`Güncel sürüm ${text.textVersion}`:'Metin henüz yayımlanmadı.';
   return <View key={st.typeCode} style={s.row}>
    <View style={s.head}><Text style={[styles.text,styles.flex,s.name]}>{st.nameTr}</Text><View style={[s.pill,{backgroundColor:t.bg}]}><Text style={[s.pillText,{color:t.fg}]}>{e.pending?'Gönderilmeyi bekliyor':stateLabel(st.kind,e.state)}</Text></View></View>
    <Text style={styles.muted}>{st.kind==='INFORMATION'?'Bilgilendirme · ':'Açık rıza · '}{detail}</Text>
    {e.problem&&<View style={s.problem}><Text style={s.problemText}>{e.problem.error===textOutdatedCode?'Siz onay verirken metin güncellendi; yeni sürüm için yeniden onay gerekiyor.':'Kararınız merkeze iletilemedi.'}</Text>
     <View style={s.buttons}>{canRetry(e.problem)&&<Pressable accessibilityRole="button" disabled={c.busy} onPress={()=>void c.retry(e.problem!.operation_id).catch(()=>{})} style={s.small}><Text style={s.link}>Yeniden gönder</Text></Pressable>}
      {canDiscard(e.problem)&&<Pressable accessibilityRole="button" disabled={c.busy} onPress={()=>void c.discard(e.problem!.operation_id).catch(()=>{})} style={s.small}><Text style={s.link}>Bildirimi kaldır</Text></Pressable>}</View></View>}
    <View style={s.buttons}>
     {text&&<Pressable accessibilityRole="button" accessibilityLabel={st.nameTr+' metnini oku'} onPress={()=>{setError('');setOpen(st.typeCode);}} style={s.small}><Icon name="document-text-outline" size={18}/><Text style={s.link}>{e.state==='ACTIVE'||e.pending?'Metni oku':st.kind==='INFORMATION'?'Oku ve kaydet':'Oku ve onayla'}</Text></Pressable>}
     {st.kind==='EXPLICIT_CONSENT'&&(e.state==='ACTIVE'||e.state==='OUTDATED')&&e.pending!=='WITHDRAW'&&<Pressable accessibilityRole="button" accessibilityLabel={st.nameTr+' rızasını geri çek'} disabled={busy} onPress={()=>confirmWithdraw(e,()=>void act(e,'WITHDRAW'))} style={s.small}><Text style={s.danger}>Geri çek</Text></Pressable>}
    </View>
   </View>;})}
  {!!error&&!entry&&<Text accessibilityRole="alert" style={s.warn}>{error}</Text>}
  <Pressable accessibilityRole="button" disabled={c.busy||auth.offline} onPress={()=>void c.refresh()} style={[s.small,(c.busy||auth.offline)&&s.disabled]}><Icon name="refresh-outline" size={18}/><Text style={s.link}>{c.busy?'Kontrol ediliyor…':'Rızalarımı yenile'}</Text></Pressable>
  {c.checkedAt&&<Text style={styles.muted}>Son kontrol: {new Date(c.checkedAt).toLocaleString('tr-TR')}</Text>}
  {entry&&<ConsentSheet key={entry.item.status.typeCode} entry={entry} mode="view" offline={auth.offline} busy={busy} error={error} onApprove={()=>act(entry,'APPROVE')} onWithdraw={()=>act(entry,'WITHDRAW')} onClose={()=>{if(!busy){setOpen(null);setError('');}}}/>}
 </View>;
}
const s=StyleSheet.create({row:{borderTopWidth:1,borderTopColor:'#e5ebf2',paddingTop:12,gap:6},head:{flexDirection:'row',alignItems:'center',gap:10},name:{fontWeight:'600'},pill:{paddingHorizontal:10,paddingVertical:5,borderRadius:12},pillText:{fontSize:12,fontWeight:'700'},buttons:{flexDirection:'row',flexWrap:'wrap',gap:8},small:{flexDirection:'row',alignItems:'center',gap:6,minHeight:44,paddingHorizontal:4},link:{color:'#2056bc',fontWeight:'600',fontSize:13},danger:{color:'#bf3b41',fontWeight:'600',fontSize:13},problem:{backgroundColor:'#fff7e7',borderRadius:12,padding:12,gap:6},problemText:{color:'#805719',fontSize:13,lineHeight:20},warn:{color:'#805719',fontSize:13,lineHeight:21},disabled:{opacity:.5}});
