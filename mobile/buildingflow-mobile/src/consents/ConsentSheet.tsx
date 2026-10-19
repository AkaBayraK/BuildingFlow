import {useState} from 'react';
import {Alert,Modal,Platform,ScrollView,StyleSheet,Text,View} from 'react-native';
import {Action as Pressable} from '../components/Action';
import {Icon} from '../components/Screen';
import {theme} from '../theme';
import {parseMarkup} from './markup';
import {MarkupView} from './MarkupView';
import {stateLabel,textOutdatedCode} from './contracts';
import type {Effective} from './store';
export type SheetMode='approve'|'view'|'gate';
const when=(value:string)=>new Date(value).toLocaleString('tr-TR',{dateStyle:'medium',timeStyle:'short'});
// What a withdrawal does, said before the person confirms it (İŞ17 server rules).
export function withdrawEffect(entry:Effective){
 const f=entry.item.status.feature;
 return f==='PROFILE_PHOTO'?'Profil fotoğrafınız merkezden ve bu cihazdan kaldırılır. İsterseniz daha sonra yeniden rıza verip yeni fotoğraf ekleyebilirsiniz.'
  :f==='REQUEST_PHOTO'?'Gönderdiğiniz talep fotoğrafları size artık gösterilmez ve bu cihazdan silinir. Yönetim talebin takibi için fotoğrafları saklamaya devam edebilir. Yeni taleplere fotoğraf ekleyemezsiniz.'
  :'Bu rızaya bağlı işlemler durdurulur. Geri çekme önceki işlemleri geçersiz kılmaz.';
}
export function confirmWithdraw(entry:Effective,onConfirm:()=>void){
 // react-native-web has no Alert dialog; the browser preview uses the platform confirm instead.
 if(Platform.OS==='web'){if(globalThis.confirm?.(entry.item.status.nameTr+' geri çekilsin mi?\n\n'+withdrawEffect(entry)))onConfirm();return;}
 Alert.alert(entry.item.status.nameTr+' geri çekilsin mi?',withdrawEffect(entry),[{text:'Vazgeç',style:'cancel'},{text:'Rızamı geri çek',style:'destructive',onPress:onConfirm}]);
}
// Full-screen text in the project's own format (no WebView, no script); approval stays disabled until the box is ticked.
export function ConsentSheet({entry,mode,visible=true,onDismiss,offline,busy,error,onApprove,onWithdraw,onClose}:{entry:Effective;mode:SheetMode;visible?:boolean;onDismiss?:()=>void;offline:boolean;busy:boolean;error:string;onApprove?:()=>Promise<void>|void;onWithdraw?:()=>Promise<void>|void;onClose:()=>void}){
 const [checked,setChecked]=useState(false);
 const status=entry.item.status,text=entry.item.currentText,information=status.kind==='INFORMATION';
 const readable=!!text&&!parseMarkup(text.body).error;
 const decided=entry.state==='ACTIVE'&&!(entry.problem?.error===textOutdatedCode);
 const canApprove=!!onApprove&&readable&&!decided;
 const canWithdraw=!!onWithdraw&&!information&&(entry.state==='ACTIVE'||entry.state==='OUTDATED')&&entry.pending!=='WITHDRAW';
 const tick=information?'Aydınlatma metnini okudum.':'Metni okudum; açık rızamı veriyorum.';
 const later=mode==='approve'?'Vazgeç, fotoğrafsız devam et':mode==='gate'?'Daha sonra':'Kapat';
 return <Modal visible={visible} onDismiss={onDismiss} animationType="slide" presentationStyle="pageSheet" onRequestClose={()=>{if(!busy)onClose();}}>
  <View style={s.page}>
   <ScrollView contentContainerStyle={s.scroll}>
    <View style={s.top}><Icon name={information?'information-circle-outline':'shield-checkmark-outline'} size={34}/><Text style={s.eyebrow}>{information?'AYDINLATMA METNİ':'AÇIK RIZA'}</Text></View>
    <Text accessibilityRole="header" style={s.title}>{status.nameTr}</Text>
    {text&&<Text style={s.meta}>Sürüm {text.textVersion} · {when(text.publishedAt)} yayımlandı</Text>}
    <View style={s.pill}><Text style={s.pillText}>{entry.pending==='APPROVE'?'Onayınız gönderilmeyi bekliyor':entry.pending==='WITHDRAW'?'Geri çekme gönderilmeyi bekliyor':stateLabel(status.kind,entry.state)}{entry.approvedVersion&&entry.state!=='WITHDRAWN'?` · ${information?'okunan':'onaylı'} sürüm ${entry.approvedVersion}`:''}</Text></View>
    {entry.problem?.error===textOutdatedCode&&<Text accessibilityRole="alert" style={s.warn}>Siz onay verirken metin güncellendi. Lütfen yeni sürümü okuyup yeniden karar verin.</Text>}
    {entry.state==='OUTDATED'&&!entry.pending&&<Text style={s.warn}>{information?'Bu metnin yeni sürümü yayımlandı. Değişiklikleri okumanızı öneririz.':'Bu metnin yeni sürümü yayımlandı. Önceki onayınız bu sürümü kapsamıyor.'}</Text>}
    {mode==='approve'&&<Text style={s.note}>Fotoğraf eklemek için bu rıza gerekiyor. Rıza vermek zorunda değilsiniz; fotoğrafsız devam edebilirsiniz.</Text>}
    {text?<View style={s.card}><MarkupView body={text.body}/></View>:<Text style={s.note}>Bu tür için henüz metin yayımlanmadı.</Text>}
   </ScrollView>
   <View style={s.footer}>
    {canApprove&&<Pressable accessibilityRole="checkbox" accessibilityState={{checked,disabled:busy}} accessibilityLabel={tick} disabled={busy} onPress={()=>setChecked(v=>!v)} style={s.check}>
     <Icon name={checked?'checkbox':'square-outline'} size={24} color={checked?theme.colors.primary:theme.colors.muted}/><Text style={s.checkText}>{tick}</Text></Pressable>}
    {!!error&&<Text accessibilityRole="alert" style={s.error}>{error}</Text>}
    {canApprove&&<Pressable accessibilityRole="button" accessibilityState={{disabled:!checked||busy}} disabled={!checked||busy} onPress={()=>void onApprove?.()} style={[s.primary,(!checked||busy)&&s.disabled]}><Text style={s.primaryText}>{busy?'Kaydediliyor…':information?'Okudum olarak kaydet':'Onaylıyorum'}</Text></Pressable>}
    {canApprove&&offline&&<Text style={s.note}>Çevrimdışısınız. Kararınız bu cihazda şifreli saklanır ve bağlantı gelince merkeze iletilir.</Text>}
    {canWithdraw&&<Pressable accessibilityRole="button" disabled={busy} onPress={()=>confirmWithdraw(entry,()=>void onWithdraw?.())} style={s.secondary}><Text style={s.danger}>Rızamı geri çek</Text></Pressable>}
    <Pressable accessibilityRole="button" disabled={busy} onPress={onClose} style={s.secondary}><Text style={s.link}>{later}</Text></Pressable>
   </View>
  </View>
 </Modal>;
}
const s=StyleSheet.create({page:{flex:1,backgroundColor:theme.colors.background},scroll:{padding:22,paddingTop:36,gap:12},top:{flexDirection:'row',alignItems:'center',gap:10},eyebrow:{fontSize:11,fontWeight:'700',letterSpacing:1,color:'#2384b5'},title:{fontSize:24,lineHeight:31,fontWeight:'700',color:theme.colors.primary},meta:{color:theme.colors.muted,fontSize:13,lineHeight:20},pill:{alignSelf:'flex-start',paddingHorizontal:12,paddingVertical:6,borderRadius:14,backgroundColor:'#eaf1f8'},pillText:{color:'#28466b',fontSize:12,fontWeight:'600'},card:{backgroundColor:'#fff',borderRadius:18,padding:18,borderWidth:1,borderColor:theme.colors.border},note:{color:theme.colors.muted,fontSize:13,lineHeight:21},warn:{color:'#805719',backgroundColor:'#fff7e7',padding:12,borderRadius:12,fontSize:13,lineHeight:21},footer:{padding:18,paddingBottom:34,gap:8,backgroundColor:'#fff',borderTopWidth:1,borderTopColor:theme.colors.border},check:{flexDirection:'row',alignItems:'center',gap:10},checkText:{flex:1,color:theme.colors.text,fontSize:15,lineHeight:22,fontWeight:'600'},primary:{backgroundColor:theme.colors.primary,minHeight:52,borderRadius:14,alignItems:'center',justifyContent:'center',padding:14},primaryText:{color:'#fff',fontSize:16,fontWeight:'600'},secondary:{minHeight:48,alignItems:'center',justifyContent:'center'},link:{color:'#2056bc',fontWeight:'600',fontSize:14},danger:{color:'#bf3b41',fontWeight:'600',fontSize:14},error:{color:'#9e333a',fontSize:13,lineHeight:21},disabled:{opacity:.45}});
