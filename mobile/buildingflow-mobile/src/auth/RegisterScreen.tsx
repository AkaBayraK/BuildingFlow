import {useRef,useState} from 'react';
import {ActivityIndicator,KeyboardAvoidingView,Platform,ScrollView,StyleSheet,Text,View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import * as Crypto from 'expo-crypto';
import Ionicons from '@expo/vector-icons/Ionicons';
import {Action as Pressable} from '../components/Action';
import {FormField} from '../components/FormField';
import {useAuth} from './AuthProvider';
import {validateRegistration,type FieldErrors,type RegistrationInput,type RegistrationResult} from './registration';
const empty:RegistrationInput={username:'',password:'',passwordConfirmation:'',firstName:'',lastName:'',phoneNumber:'',email:'',personCode:''};
export function RegisterScreen({onBack}:{onBack:()=>void}){
 const auth=useAuth();
 const [input,setInput]=useState(empty),[errors,setErrors]=useState<FieldErrors>({}),[busy,setBusy]=useState(false),[message,setMessage]=useState(''),[done,setDone]=useState<RegistrationResult|null>(null);
 const attempt=useRef<{key:string;id:string}|null>(null);
 const set=(key:keyof RegistrationInput)=>(value:string)=>{setInput(v=>({...v,[key]:value}));setErrors(e=>({...e,[key]:undefined}));};
 async function submit(){
  if(busy)return;setMessage('');const found=validateRegistration(input);setErrors(found);if(Object.keys(found).length>0){setMessage('Kırmızı alanları düzelt.');return;}
  const key=JSON.stringify({...input,password:'',passwordConfirmation:''});if(attempt.current?.key!==key)attempt.current={key,id:Crypto.randomUUID()};
  setBusy(true);
  try{setDone(await auth.register(input,attempt.current.id));setInput(empty);}catch(error){setMessage(error instanceof Error?error.message:'Kayıt tamamlanamadı.');}finally{setBusy(false);}
 }
 if(done)return <SafeAreaView style={s.safe}><ScrollView contentContainerStyle={s.page}><View style={s.card}><Ionicons name="checkmark-circle-outline" size={44} color="#08785b" accessible={false} aria-hidden={true}/><Text accessibilityRole="header" style={s.title}>Hesabın oluşturuldu</Text>
  <Text style={s.text}>{done.personLink.status==='LINKED'?`Kişi kodun eşleşti${done.personLink.propertyName?`: ${done.personLink.propertyName}`:''}. Giriş yaptığında ev bilgilerin açılacak.`:done.personLink.status==='NONE'?'Kişi kodu girmedin. Giriş yaptıktan sonra yalnız hesap bilgilerini görürsün; kodu Hesabım bölümünden girebilirsin.':done.personLink.status==='LOCKED'?'Çok fazla hatalı kod denemesi yapıldı. 15 dakika sonra Hesabım bölümünden kodu yeniden dene.':'Kişi kodu veya bilgilerin kayıtla uyuşmadı. Giriş yaptıktan sonra Hesabım bölümünden kodu yeniden girebilirsin.'}</Text>
  <Text style={s.text}>Giriş için kullanıcı adın: <Text style={s.strong}>{done.username}</Text></Text>
  <Pressable style={s.primary} onPress={()=>{onBack();void auth.login();}}><Text style={s.primaryText}>Giriş yap</Text></Pressable></View></ScrollView></SafeAreaView>;
 return <SafeAreaView style={s.safe}><KeyboardAvoidingView style={s.flex} behavior={Platform.OS==='ios'?'padding':undefined}><ScrollView contentContainerStyle={s.page} keyboardShouldPersistTaps="handled">
  <Pressable accessibilityLabel="Girişe dön" style={s.back} onPress={onBack} disabled={busy}><Ionicons name="chevron-back" size={22} color="#102342" accessible={false} aria-hidden={true}/><Text style={s.backText}>Girişe dön</Text></Pressable>
  <Text accessibilityRole="header" style={s.title}>Hesap oluştur</Text>
  <Text style={s.text}>İlk kayıt için internet gerekir. Kişi kodu yönetimin SMS ile gönderdiği 6 haneli koddur; yoksa sonra da girebilirsin.</Text>
  <View style={s.card}>
   <FormField label="Kullanıcı adı" autoCapitalize="none" autoCorrect={false} autoComplete="username" textContentType="username" maxLength={50} editable={!busy} value={input.username} onChangeText={set('username')} error={errors.username} help="Küçük harf, rakam, nokta, alt çizgi veya tire."/>
   <FormField label="Parola" secureTextEntry autoCapitalize="none" autoComplete="new-password" textContentType="newPassword" maxLength={128} editable={!busy} value={input.password} onChangeText={set('password')} error={errors.password} help="En az 12 karakter; büyük harf, küçük harf ve rakam."/>
   <FormField label="Parola (tekrar)" secureTextEntry autoCapitalize="none" autoComplete="new-password" textContentType="newPassword" maxLength={128} editable={!busy} value={input.passwordConfirmation} onChangeText={set('passwordConfirmation')} error={errors.passwordConfirmation}/>
   <FormField label="Ad" autoComplete="given-name" textContentType="givenName" maxLength={80} editable={!busy} value={input.firstName} onChangeText={set('firstName')} error={errors.firstName}/>
   <FormField label="Soyad" autoComplete="family-name" textContentType="familyName" maxLength={80} editable={!busy} value={input.lastName} onChangeText={set('lastName')} error={errors.lastName}/>
   <FormField label="Cep telefonu" keyboardType="phone-pad" autoComplete="tel" textContentType="telephoneNumber" maxLength={20} placeholder="05xx xxx xx xx" editable={!busy} value={input.phoneNumber} onChangeText={set('phoneNumber')} error={errors.phoneNumber}/>
   <FormField label="E-posta" keyboardType="email-address" autoCapitalize="none" autoComplete="email" textContentType="emailAddress" maxLength={254} editable={!busy} value={input.email} onChangeText={set('email')} error={errors.email}/>
   <FormField label="Kişi kodu (isteğe bağlı)" keyboardType="number-pad" maxLength={6} editable={!busy} value={input.personCode} onChangeText={v=>set('personCode')(v.replace(/[^0-9]/g,''))} error={errors.personCode} help="Ad, soyad, telefon ve e-posta yönetimdeki kaydınla aynı olmalı."/>
  </View>
  {!!message&&<View style={s.message}><Text accessibilityLiveRegion="polite" style={s.messageText}>{message}</Text></View>}
  <Pressable style={s.primary} disabled={busy} onPress={()=>void submit()}>{busy?<ActivityIndicator color="#fff" accessibilityLabel="Kayıt gönderiliyor"/>:<Text style={s.primaryText}>Hesabı oluştur</Text>}</Pressable>
  <Text style={s.note}>Parolan cihazda saklanmaz; giriş BuildingFlow giriş sayfasında yapılır.</Text>
 </ScrollView></KeyboardAvoidingView></SafeAreaView>;
}
const s=StyleSheet.create({
 safe:{flex:1,backgroundColor:'#f6f8fc'},flex:{flex:1},page:{width:'100%',maxWidth:560,alignSelf:'center',padding:16,gap:16},
 back:{flexDirection:'row',alignItems:'center',gap:4,minHeight:44},backText:{color:'#102342',fontSize:15},
 title:{fontSize:25,fontWeight:'700',color:'#102342'},text:{fontSize:15,lineHeight:23,color:'#52647a'},strong:{fontWeight:'700',color:'#102342'},
 card:{backgroundColor:'#fff',borderRadius:20,borderWidth:1,borderColor:'#e5ebf2',padding:18,gap:14},
 primary:{backgroundColor:'#082641',borderRadius:14,minHeight:54,padding:16,alignItems:'center',justifyContent:'center'},primaryText:{color:'#fff',fontSize:16,fontWeight:'600'},
 message:{backgroundColor:'#fdecec',borderRadius:14,padding:14},messageText:{color:'#8a2328',fontSize:14,lineHeight:21},note:{color:'#617187',fontSize:13,lineHeight:19,textAlign:'center'},
});
