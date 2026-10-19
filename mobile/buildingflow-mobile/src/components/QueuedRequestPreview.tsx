import {useState} from 'react';
import {Image,Text,View} from 'react-native';
import {Action as Pressable} from './Action';
import {useAuth} from '../auth/AuthProvider';
import {contextKey} from '../auth/contracts';
import {validateCommand,categories,priorities} from '../sync/contracts';
import type {Job} from '../sync/outbox';
import {wireCommand} from '../requests/drafts';
import {styles} from './Screen';
export function QueuedRequestPreview({job}:{job:Job}){
 const a=useAuth(),[photo,setPhoto]=useState<string|null>(null),[error,setError]=useState(''),[busy,setBusy]=useState(false);
 if(!a.session||!a.selected)return null;let c;
 try{c=validateCommand(JSON.parse(job.payload),a.session,contextKey(a.selected));}catch{return null;}
 const load=async()=>{if(busy)return;setBusy(true);try{const cmd=await a.requestCache((db,files)=>wireCommand(db,files,a.session!.userId,contextKey(a.selected!),c));if('photoBase64' in cmd&&typeof cmd.photoBase64==='string')setPhoto('data:image/jpeg;base64,'+cmd.photoBase64);setError('');}catch{setError('Yerel fotoğraf okunamadı; talep merkez onayı olmadan gönderildi sayılmaz.');}finally{setBusy(false);}};
 return <View style={{gap:8}}><Text style={styles.muted}>{categories[c.category??'OTHER']} · {priorities[c.priority??'NORMAL']}</Text><Text style={styles.text} numberOfLines={4}>{c.description??'Talebi kaldırma işlemi'}</Text>{c.photoAction==='REPLACE'&&<>{photo&&<Image source={{uri:photo}} accessibilityLabel="Bekleyen talep fotoğrafı" style={{width:'100%',height:180,resizeMode:'contain',borderRadius:12}}/>}<Pressable accessibilityRole="button" disabled={busy} style={styles.button} onPress={()=>void load()}><Text style={styles.buttonText}>Kuyruktaki fotoğrafı görüntüle</Text></Pressable></>}{!!error&&<Text style={styles.muted}>{error}</Text>}</View>;
}
