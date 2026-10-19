import {useState} from 'react';
import {Image,Text,View} from 'react-native';
import {Action as Pressable} from './Action';
import {useAuth} from '../auth/AuthProvider';
import {styles} from './Screen';
export function RequestPhoto({id,photoId,sha}:{id:string;photoId:string;sha:string}){
 const auth=useAuth(),[uri,setUri]=useState<{uri:string;local:boolean}|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const load=async()=>{if(busy)return;setBusy(true);try{setUri(await auth.requestPhoto(id,photoId,sha));setError('');}catch(e){setError(e instanceof Error?e.message:'Fotoğraf alınamadı.');}finally{setBusy(false);}};
 return <View style={{gap:8}}>{uri?.local&&<Text style={styles.muted}>Çevrimdışı · Gönderdiğiniz yerel fotoğraf kopyası</Text>}{uri&&<Image source={{uri:uri.uri}} accessibilityLabel="Talebe eklenen fotoğraf" resizeMode="contain" style={{width:'100%',height:220,borderRadius:14}}/>}<Pressable accessibilityRole="button" disabled={busy} style={styles.button} onPress={()=>void load()}><Text style={styles.buttonText}>{busy?'Fotoğraf alınıyor…':uri?'Fotoğrafı yeniden aç':'Ekli fotoğrafı görüntüle'}</Text></Pressable>{!!error&&<Text accessibilityRole="alert" style={styles.muted}>{error}</Text>}</View>;
}
