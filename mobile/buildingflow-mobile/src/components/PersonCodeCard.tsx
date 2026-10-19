import {useState} from 'react';
import {ActivityIndicator,StyleSheet,Text,View} from 'react-native';
import {Action as Pressable} from './Action';
import {FormField} from './FormField';
import {Icon,styles} from './Screen';
import {useAuth} from '../auth/AuthProvider';
import {validateLink,type PersonLinkInput} from '../auth/registration';
export function PersonCodeCard({linked,defaults}:{linked:boolean;defaults?:Partial<PersonLinkInput>}){
 const auth=useAuth();
 const [input,setInput]=useState<PersonLinkInput>({personCode:'',firstName:defaults?.firstName??'',lastName:defaults?.lastName??'',phoneNumber:defaults?.phoneNumber??'',email:defaults?.email??''});
 const [busy,setBusy]=useState(false),[message,setMessage]=useState(''),[success,setSuccess]=useState(false);
 const set=(key:keyof PersonLinkInput)=>(value:string)=>{setInput(v=>({...v,[key]:value}));setMessage('');setSuccess(false);};
 async function submit(){
  if(busy)return;const invalid=validateLink(input);if(invalid){setMessage(invalid);return;}
  setBusy(true);setMessage('');
  try{await auth.linkPerson(input);setSuccess(true);setInput(v=>({...v,personCode:''}));}catch(error){setMessage(error instanceof Error?error.message:'Eşleşme tamamlanamadı.');}finally{setBusy(false);}
 }
 return <View style={styles.card}>
  <View style={styles.locationRow}><Icon name="key-outline"/><Text accessibilityRole="header" style={[styles.cardTitle,styles.flex]}>{linked?'Başka bir kişi kodu ekle':'Kişi kodunu gir'}</Text></View>
  <Text style={styles.muted}>{linked?'Başka bir dairede de kaydın varsa o kodu girerek bağlantı ekleyebilirsin.':'Yönetimin SMS ile gönderdiği 6 haneli kodu gir. Bilgilerin kayıtla eşleşince ev sahibi, kiracı veya sakin ekranların ve senkron açılır.'}</Text>
  {auth.offline?<Text style={styles.muted}>Kod girmek için internet bağlantısı gerekir.</Text>:<View style={s.form}>
   <FormField label="Kişi kodu" keyboardType="number-pad" maxLength={6} editable={!busy} value={input.personCode} onChangeText={v=>set('personCode')(v.replace(/[^0-9]/g,''))}/>
   <FormField label="Ad" maxLength={80} editable={!busy} value={input.firstName} onChangeText={set('firstName')}/>
   <FormField label="Soyad" maxLength={80} editable={!busy} value={input.lastName} onChangeText={set('lastName')}/>
   <FormField label="Cep telefonu" keyboardType="phone-pad" maxLength={20} editable={!busy} value={input.phoneNumber} onChangeText={set('phoneNumber')}/>
   <FormField label="E-posta" keyboardType="email-address" autoCapitalize="none" maxLength={254} editable={!busy} value={input.email} onChangeText={set('email')} help="Yönetimdeki kaydında e-posta varsa aynısını gir."/>
   {!!message&&<Text accessibilityLiveRegion="polite" style={s.error}>{message}</Text>}
   {success&&<Text accessibilityLiveRegion="polite" style={s.success}>Kişi kaydınla eşleşti. Ev bilgilerin yüklendi.</Text>}
   <Pressable style={styles.button} disabled={busy} onPress={()=>void submit()}>{busy?<ActivityIndicator color="#fff" accessibilityLabel="Kod kontrol ediliyor"/>:<Text style={styles.buttonText}>Kodu doğrula</Text>}</Pressable>
  </View>}
 </View>;
}
const s=StyleSheet.create({form:{gap:12},error:{color:'#bf3b41',fontSize:14,lineHeight:20},success:{color:'#08785b',fontSize:14,lineHeight:20}});
