import {useEffect,useRef,useState} from 'react';
import {StyleSheet,Switch,Text,View} from 'react-native';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {Icon} from '../components/Screen';
import {Action} from '../components/Action';
import {theme} from '../theme';
import {approve,decisionPath,parseItem,parsePreferences,preferenceError,preferencesPath,statusLabels,withdraw,type Item,type Preferences} from './contracts';

// Nothing here is cached or queued offline: a preference counts only once the server has recorded it.
export function CommunicationCard(){
 const auth=useAuth(),online=auth.state==='signedIn'&&!auth.offline&&!auth.privacyLocked;
 const [data,setData]=useState<Preferences|null>(null),[error,setError]=useState(''),[busy,setBusy]=useState(false),[reading,setReading]=useState<Item|null>(null),[ticked,setTicked]=useState(false),[check,setCheck]=useState<{body:string;ok:boolean}|null>(null);
 const pending=useRef(new Map<string,{key:string;id:string}>()),lock=useRef(false);
 const [revision,setRevision]=useState(0);
 useEffect(()=>{if(!online)return;let live=true;
  void auth.profileRequest(preferencesPath).then(async r=>{if(!r.ok)throw r.status;return parsePreferences(await r.json());})
   .then(next=>{if(live){setData(next);setError('');}}).catch(e=>{if(live){setData(null);setError(preferenceError(typeof e==='number'?e:0));}});
  return()=>{live=false;};},[auth,online,revision]);
 const body=reading?data?.text.body:undefined,sha=data?.text.bodySha256;
 useEffect(()=>{if(body===undefined)return;let live=true;void Crypto.digestStringAsync(Crypto.CryptoDigestAlgorithm.SHA256,body).then(h=>{if(live)setCheck({body,ok:h===sha});}).catch(()=>{if(live)setCheck({body,ok:false});});return()=>{live=false;};},[body,sha]);
 const intact=body!==undefined&&check?.body===body?check.ok:null;
 async function decide(item:Item,approved:boolean){if(!data||lock.current||!online)return;lock.current=true;setBusy(true);setError('');
  const key=approved?'APPROVE:'+data.text.bodySha256:'WITHDRAW',last=pending.current.get(item.senderId),id=last?.key===key?last.id:Crypto.randomUUID();pending.current.set(item.senderId,{key,id});
  try{const r=await auth.profileRequest(decisionPath(item.senderId),approved?approve(data.text,id):withdraw(id));if(!r.ok){if(r.status<500)pending.current.delete(item.senderId);throw r.status;}
   const saved=parseItem(await r.json());pending.current.delete(item.senderId);setData({...data,items:data.items.map(i=>i.senderId===saved.senderId?saved:i)});setReading(null);setTicked(false);}
  catch(e){setError(preferenceError(typeof e==='number'?e:0));}finally{lock.current=false;setBusy(false);}}
 return <View style={s.card} accessibilityLabel="İletişim tercihlerim">
  <View style={s.row}><Icon name="notifications-outline"/><Text accessibilityRole="header" style={s.title}>İletişim tercihlerim</Text></View>
  <Text style={s.help}>{'Tanıtım ve kampanya SMS\'leri isteğe bağlıdır. Aidat, bakım ve güvenlik SMS\'leri bu tercihten etkilenmez.'}</Text>
  {!online&&<View style={s.row}><Icon name="cloud-offline-outline"/><Text style={[s.help,s.flex]}>Tercihler yalnız çevrimiçi değiştirilir; çevrimdışı seçim kaydedilmez.</Text></View>}
  {!!error&&<Text accessibilityRole="alert" accessibilityLiveRegion="polite" style={s.error}>{error}</Text>}
  {online&&data&&<>
   <Text style={s.help}>{data.verifiedPhoneMasked?`Doğrulanmış telefon: ${data.verifiedPhoneMasked}`:'Tanıtım SMS izni için önce telefonunuzu doğrulayın.'}</Text>
   {data.items.length===0&&<Text style={s.help}>{'Size tanıtım SMS\'i gönderebilecek bir gönderici yok.'}</Text>}
   {data.items.map(item=><View key={item.senderId} style={s.item}>
    <View style={s.row}><Icon name="business-outline"/><Text style={[s.heading,s.flex]}>{item.senderTitle}</Text></View>
    <View style={s.row}><Icon name={item.status==='APPROVED'?'checkmark-circle-outline':item.status==='REJECTED'?'close-circle-outline':'ellipse-outline'} size={18}/><Text style={s.label}>{statusLabels[item.status]}{item.scope==='PLATFORM'?' · BuildingFlow':' · Yönetim alanı'}</Text></View>
    {item.status==='APPROVED'?<Action accessibilityLabel={`${item.senderTitle} tanıtım SMS iznini geri çek`} disabled={busy} onPress={()=>void decide(item,false)} style={[s.button,s.danger,busy&&s.disabled]}><Icon name="arrow-undo-outline" color="#fff" size={18}/><Text style={s.buttonText}>İzni geri çek</Text></Action>
     :<Action accessibilityLabel={`${item.senderTitle} için izin metnini oku`} disabled={busy||!data.verifiedPhoneMasked} onPress={()=>{setTicked(false);setReading(item);}} style={[s.button,(busy||!data.verifiedPhoneMasked)&&s.disabled]}><Icon name="reader-outline" color="#fff" size={18}/><Text style={s.buttonText}>Okuyup izin ver</Text></Action>}
    {reading?.senderId===item.senderId&&<View style={s.text}>
     <Text style={s.heading}>{data.text.title} · sürüm {data.text.version}</Text><Text style={s.help}>{data.text.body}</Text>
     {intact===false&&<Text accessibilityRole="alert" style={s.error}>Metin doğrulanamadı. Durumu yenileyin.</Text>}
     <View style={s.row}><Switch accessibilityLabel="Metni okudum, tanıtım SMS'i almayı kabul ediyorum" value={ticked} disabled={busy||intact!==true} onValueChange={setTicked}/><Text style={[s.help,s.flex]}>{'Metni okudum; tanıtım SMS\'i almayı kabul ediyorum.'}</Text></View>
     <Action accessibilityLabel="Tanıtım SMS iznini onayla" disabled={busy||!ticked||intact!==true} onPress={()=>void decide(item,true)} style={[s.button,(busy||!ticked||intact!==true)&&s.disabled]}><Icon name="checkmark-circle-outline" color="#fff" size={18}/><Text style={s.buttonText}>{busy?'Kaydediliyor…':'Onayla'}</Text></Action>
    </View>}
   </View>)}
  </>}
  {online&&<Action accessibilityLabel="İletişim tercihlerini yenile" disabled={busy} onPress={()=>setRevision(n=>n+1)} style={[s.button,s.neutral,busy&&s.disabled]}><Icon name="refresh-outline" size={18}/><Text style={s.neutralText}>Yenile</Text></Action>}
 </View>;
}
const s=StyleSheet.create({card:{backgroundColor:theme.colors.surface,borderWidth:1,borderColor:theme.colors.border,borderRadius:theme.radius.card,padding:20,gap:12},row:{flexDirection:'row',alignItems:'center',gap:9},flex:{flex:1},
 title:{fontSize:18,fontWeight:'700',color:theme.colors.primary,flexShrink:1},heading:{fontSize:16,fontWeight:'600',color:theme.colors.primary},label:{fontSize:13,color:theme.colors.muted},help:{fontSize:13,lineHeight:21,color:theme.colors.muted},
 error:{fontSize:14,lineHeight:22,color:'#bf3b41'},item:{borderWidth:1,borderColor:theme.colors.border,borderRadius:14,padding:14,gap:10},text:{gap:10,paddingTop:6},
 button:{minHeight:48,borderRadius:12,backgroundColor:theme.colors.primary,flexDirection:'row',alignItems:'center',justifyContent:'center',gap:8,paddingHorizontal:14},danger:{backgroundColor:'#bf3b41'},neutral:{backgroundColor:'transparent',borderWidth:1,borderColor:theme.colors.border},
 disabled:{opacity:0.5},buttonText:{color:'#fff',fontWeight:'600',fontSize:15},neutralText:{color:theme.colors.primary,fontWeight:'600',fontSize:15}});
