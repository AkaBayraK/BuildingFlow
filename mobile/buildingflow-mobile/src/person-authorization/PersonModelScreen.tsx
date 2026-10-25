import {ContactVerificationCard} from '../contact-verification/ContactVerificationCard';
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
import {portfolioGroups,unitStatusLabels,unitSummary,type UnitSummary} from './model';
import {useEffect,useState} from 'react';
function SelectedSummary(){
 const auth=useAuth(),sel=auth.selected,c=sel?.canonical,key=sel?contextKey(sel):'',caps=auth.portfolio?.[key]?.capabilities??[];const [value,setValue]=useState<{key:string;summary:UnitSummary|null}>({key:'',summary:null});
 useEffect(()=>{
  if(!c||!c.unitId||auth.offline)return;const controller=new AbortController(),q=`?propertyId=${c.propertyId}&capacity=${c.kind}&relationId=${c.id}&page=1&pageSize=3`,base=`/api/v2/me/units/${c.unitId}/`;
  const read=async(section:string,allowed:boolean)=>{if(!allowed)return null;const r=await auth.meRequest(base+section+q,controller.signal);return r.ok?r.json():null;};
  void Promise.all([read('dues',caps.includes('VIEW_DUES')),read('announcements',caps.includes('VIEW_ANNOUNCEMENTS'))]).then(([d,a])=>{if(!controller.signal.aborted)setValue({key,summary:unitSummary(d,a,key)});}).catch(()=>{if(!controller.signal.aborted)setValue({key,summary:null});});
  return()=>controller.abort();
 // eslint-disable-next-line react-hooks/exhaustive-deps
 },[key,auth.offline,caps.join(',')]);
 if(!c?.unitId)return null;const summary=value.key===key?value.summary:null;
 if(auth.offline)return <Text style={s.muted}>Aidat ve duyuru özeti çevrimiçi görüntülenir.</Text>;
 if(!summary)return null;
 return <View style={s.card}><Text accessibilityRole="header" style={s.cardTitle}>Seçili daire özeti</Text>{summary.balance!==null&&<Text style={s.text}>Kalan borç: {summary.balance} TL · Gecikmiş: {summary.overdue} TL</Text>}{summary.announcements.length>0&&<><Text style={s.muted}>Son duyurular</Text>{summary.announcements.map((t,i)=><Text key={i} style={s.text}>• {t}</Text>)}</>}{summary.balance===null&&summary.announcements.length===0&&<Text style={s.muted}>Bu rol için gösterilecek özet yok.</Text>}</View>;
}
function Portfolio(){
 const auth=useAuth(),current=auth.selected?contextKey(auth.selected):null,groups=portfolioGroups(auth.session?.contexts??[]);
 const chip=(item:NonNullable<typeof auth.selected>)=>{const key=contextKey(item),on=key===current;return <Action key={key} accessibilityLabel={`${item.propertyName}${item.unitNumber?' '+item.unitNumber:''} ${roleLabels[item.role]} olarak aç`} accessibilityState={{selected:on}} style={[s.chip,on&&s.chipOn]} onPress={()=>auth.choose(key)}><Text style={[s.chipText,on&&s.chipTextOn]}>{roleLabels[item.role]}</Text></Action>;};
 return <View style={{gap:16}}>
  <View accessibilityRole="summary" style={s.indicator}><Text style={s.muted}>Seçili çalışma alanı</Text><Text style={s.cardTitle}>{auth.selected?`${auth.selected.propertyName}${auth.selected.unitNumber?' · '+auth.selected.unitNumber:''} · ${roleLabels[auth.selected.role]}`:'Aşağıdan bir daire veya görev seçin'}</Text></View>
  {groups.areas.length>0&&<Text accessibilityRole="header" style={s.cardTitle}>Portföyüm</Text>}
  {groups.areas.map(area=><View key={area.key} style={s.card}><Text accessibilityRole="header" style={s.cardTitle}>{area.propertyName}</Text><Text style={s.muted}>{area.tenantName}</Text>{area.units.map(unit=><View key={unit.key} style={s.unit}><Text style={s.text}>Daire {unit.unitNumber}{(()=>{const st=unit.contexts.map(c=>auth.portfolio?.[contextKey(c)]?.status).find(Boolean);return st?` · ${unitStatusLabels[st]}`:'';})()}</Text><View style={s.chips}>{unit.contexts.map(chip)}</View></View>)}</View>)}
  {groups.workspaces.length>0&&<><Text accessibilityRole="header" style={s.cardTitle}>Yönetim görevlerim</Text>{groups.workspaces.map(item=><View key={contextKey(item)} style={s.card}><Text style={s.cardTitle}>{item.propertyName}</Text><Text style={s.muted}>{item.canonical?.tenantName}</Text><View style={s.chips}>{chip(item)}</View></View>)}</>}
 </View>;
}
export function PersonModelScreen(){
 const auth=useAuth(),snapshot=auth.session?.authorization,c=auth.selected?.canonical,syncEnabled=process.env.EXPO_PUBLIC_PERSON_SYNC_ENABLED==='true';
 return <SafeAreaView style={s.safe}><View style={s.header}><BuildingFlowMark size={38} variant="light"/><Text accessibilityRole="header" style={s.brand}>BuildingFlow</Text></View><ScrollView contentContainerStyle={s.body}>
  <Text accessibilityRole="header" style={s.title}>Çalışma alanlarım</Text><Text style={s.text}>{auth.offline?'Çevrimdışı · Cihazdaki yetki kopyası':'Çevrimiçi doğrulanmış yetki kopyası'}</Text>
  {snapshot&&<Text style={s.muted}>Son doğrulama: {new Date(snapshot.issuedAt).toLocaleString('tr-TR')}{'\n'}Geçerlilik: {new Date(snapshot.expiresAt).toLocaleString('tr-TR')}</Text>}
  {!snapshot?<Text accessibilityRole="alert" style={s.text}>Yetki modeli doğrulanamadı. Yeniden giriş yap.</Text>:snapshot.contexts.length===0?<Text style={s.text}>Hesabına bağlı güncel çalışma alanı yok.</Text>:<Portfolio/>}
  <ContactVerificationCard key={auth.session?.userId} alwaysVisible/>
  <SelectedSummary/>
  {c&&<View style={s.card}><Text accessibilityRole="header" style={s.cardTitle}>Kendi bilgilerim</Text>{snapshot?.people.filter(p=>p.tenantId===c.tenantId).map(p=><Text key={p.id} style={s.text}>{p.displayName}</Text>)}<Text style={s.muted}>Kişi ve görev kayıtları bu ekranda salt okunurdur. Kesin finans işlemleri ve rol değişiklikleri çevrimiçi doğrulama gerektirir.</Text></View>}
  {syncEnabled&&<><Text accessibilityRole="header" style={s.cardTitle}>Hesap profili</Text><Text style={s.muted}>Bu kişisel uygulama profili, merkezi kişi veya görev kaydını değiştirmez. Değişiklikler sunucu onayına kadar taslaktır.</Text><ProfileEditor/>{c&&c.role!=='ACCOUNTANT'&&(auth.portfolio?.[auth.selected?contextKey(auth.selected):'']?.capabilities.includes('CREATE_REQUEST')??true)&&<View key={snapshot?.userId+':'+c.kind+':'+c.id} style={{gap:16}}><RequestForm/><SyncPanel/><RequestBoard/></View>}<Text style={s.muted}>Ödeme, kesin tahakkuk ve rol değişikliği çevrimiçi yetki ve sunucu onayı gerektirir; bu kuyruğa alınmaz.</Text></>}
  <Text style={s.muted}>Çevrimdışıyken sunucudaki bir yetki iptali anında öğrenilemez. Süre dolunca veya cihaz kilitlenince yeniden doğrulama gerekir.</Text>
  {!auth.offline&&Platform.OS!=='web'&&<Action style={s.button} onPress={()=>void auth.enableOffline()}><Text style={s.buttonText}>{auth.offlineEnabled?'Biyometrik çevrimdışı erişimi yeniden hazırla':'Çevrimdışı erişimi biyometriyle etkinleştir'}</Text></Action>}
  {!!auth.cacheNote&&<Text accessibilityLiveRegion="polite" style={s.muted}>{auth.cacheNote}</Text>}{!!auth.message&&<Text accessibilityLiveRegion="polite" style={s.text}>{auth.message}</Text>}
  <Action style={s.button} onPress={auth.retrySetup}><Text style={s.buttonText}>Çevrimiçi yetkileri yenile</Text></Action><Action style={s.card} onPress={()=>void auth.logout()}><Text style={s.cardTitle}>Çıkış yap ve cihaz verilerini temizle</Text></Action>
 </ScrollView></SafeAreaView>;
}
const s=StyleSheet.create({indicator:{padding:theme.spacing.medium,borderRadius:12,borderWidth:1,borderColor:theme.colors.primary,backgroundColor:theme.colors.surface,gap:4},unit:{gap:8,paddingTop:8},chips:{flexDirection:'row',flexWrap:'wrap',gap:8},chip:{minHeight:48,minWidth:48,paddingHorizontal:16,justifyContent:'center',borderRadius:24,borderWidth:1,borderColor:theme.colors.primary},chipOn:{backgroundColor:theme.colors.primary},chipText:{color:theme.colors.primary,fontWeight:'600'},chipTextOn:{color:theme.colors.surface},safe:{flex:1,backgroundColor:theme.colors.background},header:{padding:theme.spacing.large,backgroundColor:theme.colors.primary,flexDirection:'row',alignItems:'center',gap:12},brand:{color:theme.colors.surface,fontSize:22,fontWeight:'700'},body:{padding:theme.spacing.large,gap:16},title:{color:theme.colors.text,fontSize:25,fontWeight:'700'},text:{color:theme.colors.text,fontSize:15,lineHeight:23},muted:{color:theme.colors.muted,fontSize:13,lineHeight:21},card:{padding:16,borderRadius:theme.radius.card,borderWidth:1,borderColor:theme.colors.border,backgroundColor:theme.colors.surface,gap:8},selected:{borderColor:theme.colors.primary,borderWidth:2},cardTitle:{color:theme.colors.primary,fontSize:17,fontWeight:'700'},button:{backgroundColor:theme.colors.primary,padding:16,borderRadius:theme.radius.field},buttonText:{color:theme.colors.surface,fontWeight:'700',fontSize:15}});
