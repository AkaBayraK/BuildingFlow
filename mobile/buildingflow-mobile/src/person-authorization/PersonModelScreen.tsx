import {Platform,ScrollView,StyleSheet,Text,View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import {useAuth} from '../auth/AuthProvider';
import {contextKey,roleLabels} from '../auth/contracts';
import {Action} from '../components/Action';
import {BuildingFlowMark} from '../components/BuildingFlowMark';
import {RequestForm} from '../components/RequestForm';
import {RequestBoard} from '../components/RequestBoard';
import {SyncPanel} from '../components/SyncPanel';
import {ProfileEditor} from '../components/ProfileEditor';
import {theme} from '../theme';
export function PersonModelScreen(){
 const auth=useAuth(),snapshot=auth.session?.authorization,c=auth.selected?.canonical,syncEnabled=process.env.EXPO_PUBLIC_PERSON_SYNC_ENABLED==='true';
 return <SafeAreaView style={s.safe}><View style={s.header}><BuildingFlowMark size={38} variant="light"/><Text accessibilityRole="header" style={s.brand}>BuildingFlow</Text></View><ScrollView contentContainerStyle={s.body}>
  <Text accessibilityRole="header" style={s.title}>Çalışma alanlarım</Text><Text style={s.text}>{auth.offline?'Çevrimdışı · Cihazdaki yetki kopyası':'Çevrimiçi doğrulanmış yetki kopyası'}</Text>
  {snapshot&&<Text style={s.muted}>Son doğrulama: {new Date(snapshot.issuedAt).toLocaleString('tr-TR')}{'\n'}Geçerlilik: {new Date(snapshot.expiresAt).toLocaleString('tr-TR')}</Text>}
  {!snapshot?<Text accessibilityRole="alert" style={s.text}>Yetki modeli doğrulanamadı. Yeniden giriş yap.</Text>:snapshot.contexts.length===0?<Text style={s.text}>Hesabına bağlı güncel çalışma alanı yok.</Text>:auth.session?.contexts.map(item=><Action key={contextKey(item)} accessibilityState={{selected:!!auth.selected&&contextKey(auth.selected)===contextKey(item)}} style={[s.card,auth.selected&&contextKey(auth.selected)===contextKey(item)&&s.selected]} onPress={()=>auth.choose(contextKey(item))}><Text style={s.cardTitle}>{item.propertyName}</Text><Text style={s.text}>{item.canonical?.tenantName} · {roleLabels[item.role]}{item.unitNumber?` · ${item.unitNumber}`:''}</Text></Action>)}
  {c&&<View style={s.card}><Text accessibilityRole="header" style={s.cardTitle}>Kendi bilgilerim</Text>{snapshot?.people.filter(p=>p.tenantId===c.tenantId).map(p=><Text key={p.id} style={s.text}>{p.displayName}</Text>)}<Text style={s.muted}>Kişi ve görev kayıtları bu ekranda salt okunurdur. Kesin finans işlemleri ve rol değişiklikleri çevrimiçi doğrulama gerektirir.</Text></View>}
  {syncEnabled&&<><Text accessibilityRole="header" style={s.cardTitle}>Hesap profili</Text><Text style={s.muted}>Bu kişisel uygulama profili, merkezi kişi veya görev kaydını değiştirmez. Değişiklikler sunucu onayına kadar taslaktır.</Text><ProfileEditor/>{c&&c.role!=='ACCOUNTANT'&&<View key={snapshot?.userId+':'+c.kind+':'+c.id} style={{gap:16}}><RequestForm/><SyncPanel/><RequestBoard/></View>}<Text style={s.muted}>Ödeme, kesin tahakkuk ve rol değişikliği çevrimiçi yetki ve sunucu onayı gerektirir; bu kuyruğa alınmaz.</Text></>}
  <Text style={s.muted}>Çevrimdışıyken sunucudaki bir yetki iptali anında öğrenilemez. Süre dolunca veya cihaz kilitlenince yeniden doğrulama gerekir.</Text>
  {!auth.offline&&Platform.OS!=='web'&&<Action style={s.button} onPress={()=>void auth.enableOffline()}><Text style={s.buttonText}>{auth.offlineEnabled?'Biyometrik çevrimdışı erişimi yeniden hazırla':'Çevrimdışı erişimi biyometriyle etkinleştir'}</Text></Action>}
  {!!auth.cacheNote&&<Text accessibilityLiveRegion="polite" style={s.muted}>{auth.cacheNote}</Text>}{!!auth.message&&<Text accessibilityLiveRegion="polite" style={s.text}>{auth.message}</Text>}
  <Action style={s.button} onPress={auth.retrySetup}><Text style={s.buttonText}>Çevrimiçi yetkileri yenile</Text></Action><Action style={s.card} onPress={()=>void auth.logout()}><Text style={s.cardTitle}>Çıkış yap ve cihaz verilerini temizle</Text></Action>
 </ScrollView></SafeAreaView>;
}
const s=StyleSheet.create({safe:{flex:1,backgroundColor:theme.colors.background},header:{padding:theme.spacing.large,backgroundColor:theme.colors.primary,flexDirection:'row',alignItems:'center',gap:12},brand:{color:theme.colors.surface,fontSize:22,fontWeight:'700'},body:{padding:theme.spacing.large,gap:16},title:{color:theme.colors.text,fontSize:25,fontWeight:'700'},text:{color:theme.colors.text,fontSize:15,lineHeight:23},muted:{color:theme.colors.muted,fontSize:13,lineHeight:21},card:{padding:16,borderRadius:theme.radius.card,borderWidth:1,borderColor:theme.colors.border,backgroundColor:theme.colors.surface,gap:8},selected:{borderColor:theme.colors.primary,borderWidth:2},cardTitle:{color:theme.colors.primary,fontSize:17,fontWeight:'700'},button:{backgroundColor:theme.colors.primary,padding:16,borderRadius:theme.radius.field},buttonText:{color:theme.colors.surface,fontWeight:'700',fontSize:15}});
