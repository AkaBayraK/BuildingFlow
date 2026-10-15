import {useRef,useState} from 'react';
import {Platform,Pressable,StyleSheet,Text,TextInput,View} from 'react-native';
import * as Crypto from 'expo-crypto';
import {useAuth} from '../auth/AuthProvider';
import {tenantRequest} from '../owner/request';
import {SyncPanel} from './SyncPanel';
import {Icon,styles} from './Screen';
export function TenantRequestForm({onQueued}:{onQueued:()=>Promise<void>}){
 const auth=useAuth(),[description,setDescription]=useState(''),[busy,setBusy]=useState(false),[message,setMessage]=useState('');const lock=useRef(false);
 const submit=async()=>{
  const context=auth.selected;if(lock.current||context?.role!=='TENANT'||!context.unitId||!auth.session)return;
  if(!description.trim()||description.trim().length>2000){setMessage('Talep açıklaması 1–2000 karakter olmalı.');return;}
  lock.current=true;setBusy(true);setMessage('');
  try{await auth.queueCommand(tenantRequest(auth.session,context,description,Crypto.randomUUID(),Crypto.randomUUID()));setDescription('');setMessage(auth.offline?'Talep şifreli kuyruğa kaydedildi. Bağlantı gelince gönderilecek.':'Talep kuyruğa kaydedildi. Merkez onayını aşağıdaki eşitleme alanından izleyebilirsiniz.');await onQueued();}
  catch{setMessage('Talep kuyruğa kaydedilemedi. Açıklamanız korunuyor.');}
  finally{lock.current=false;setBusy(false);}
 };
 return <><View style={styles.card}><View style={s.row}><Icon name="construct-outline" color="#a75c13"/><Text style={styles.cardTitle}>Bakım / arıza talebi oluştur</Text></View><Text style={styles.muted}>Talep seçili dairenizden yönetime iletilir. Acil yardım hizmeti değildir.</Text>{Platform.OS==='web'?<Text style={styles.muted}>Güvenli çevrimdışı kuyruk için BuildingFlow mobil uygulamasını kullanın. Tarayıcıdan talep gönderimi bu fazda sunulmaz.</Text>:<><TextInput accessibilityLabel="Talep açıklaması" placeholder="Örneğin: Dairedeki su sayacında sızıntı var…" value={description} onChangeText={setDescription} maxLength={2000} multiline editable={!busy} style={s.input}/><Text style={styles.muted}>{description.length}/2000</Text><Pressable disabled={busy} accessibilityRole="button" accessibilityLabel="Talebi kuyruğa kaydet" onPress={()=>void submit()} style={[styles.button,busy&&{opacity:.5}]}><Text style={styles.buttonText}>{busy?'Kaydediliyor…':auth.offline?'Çevrimdışı kuyruğa kaydet':'Talebi gönder'}</Text></Pressable></>}{!!message&&<Text accessibilityRole="alert" style={styles.text}>{message}</Text>}</View><SyncPanel/></>;
}
const s=StyleSheet.create({row:{flexDirection:'row',gap:10,alignItems:'center'},input:{minHeight:120,textAlignVertical:'top',borderWidth:1,borderColor:'#dce5ef',borderRadius:12,padding:14,color:'#102342',fontSize:15,lineHeight:23}});
