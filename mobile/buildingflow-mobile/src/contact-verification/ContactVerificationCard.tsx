import {useEffect,useRef,useState} from 'react';
import {Keyboard,StyleSheet,Text,TextInput,View} from 'react-native';
import {useAuth} from '../auth/AuthProvider';
import {Icon} from '../components/Screen';
import {Action} from '../components/Action';
import {FormField} from '../components/FormField';
import {theme} from '../theme';
import {formatPhone,phoneChars} from '../profile/phone';
import {challengeOpen,maskContact,type ContactState,type Outcome} from './contracts';
import {contactStyle} from './contact-style';
import {useContactVerification} from './useContactVerification';
const messages:Record<Outcome,string>={ACCEPTED:'SMS isteği alındı. Kod gelirse girin; numaranız henüz doğrulanmadı.',UNKNOWN:'Gönderim sonucu belirsiz. Kod geldiyse kullanabilirsiniz. Yeniden göndermeden önce süreyi bekleyin.',FAILED:'SMS gönderilemedi. Daha sonra tekrar deneyin.',UNAVAILABLE:'SMS doğrulama servisi şu anda kullanılamıyor.',TOO_SOON:'Yeniden gönderim için sunucunun belirttiği süreyi bekleyin.',RATE_LIMITED:'Gönderim sınırına ulaşıldı. Belirtilen süre sonunda tekrar deneyin.',REJECTED:'Kod hatalı. Kalan deneme sayısını kontrol edin.',LOCKED:'Deneme hakkı doldu. Yeniden gönderim açıldığında yeni kod isteyin.',EXPIRED:'Kodun süresi doldu. Yeni kod isteyin.',CANCELLED:'Bekleyen kod iptal edildi. Numaranız kayıtlı kalır.',VERIFIED:'Telefon numaranız doğrulandı.',ALREADY_VERIFIED:'Bu numara zaten doğrulanmış.'};
function Status({value}:{value:ContactState}){const style=contactStyle[value];return <View style={[s.status,{backgroundColor:style.background}]}><Icon name={style.icon} color={style.color} size={18}/><Text style={{color:style.color,fontWeight:'600',flexShrink:1}}>{style.label}</Text></View>;}
function Button({title,icon,disabled,onPress}:{title:string;icon:Parameters<typeof Icon>[0]['name'];disabled?:boolean;onPress:()=>void}){return <Action accessibilityLabel={title} disabled={disabled} onPress={onPress} style={[s.button,disabled&&s.disabled]}><Icon name={icon} color="#fff" size={19}/><Text style={s.buttonText}>{title}</Text></Action>;}
export function ContactVerificationCard({alwaysVisible=false}:{alwaysVisible?:boolean}){
 const model=useContactVerification(alwaysVisible),auth=useAuth();const key=[auth.session?.userId,auth.offline,auth.privacyLocked,model.epoch,model.expired,model.contact?.version,model.contact?.phone.challengeId,model.contact?.phone.status].join(':');
 return <ContactForm key={key} model={model}/>;
}
function ContactForm({model}:{model:ReturnType<typeof useContactVerification>}){
 const auth=useAuth(),[phone,setPhone]=useState(model.contact?.phone.pendingValue??model.contact?.phone.profileValue??model.contact?.phone.verifiedValue??''),[code,setCode]=useState(''),[note,setNote]=useState(''),[emailBusy,setEmailBusy]=useState(false),[localError,setLocalError]=useState('');
 const codeInput=useRef<TextInput>(null),phoneInput=useRef<TextInput>(null),emailLock=useRef(false);
 const contact=model.contact,p=contact?.phone,blocked=auth.offline||auth.state!=='signedIn'||auth.privacyLocked;
 useEffect(()=>{if(['ACCEPTED','UNKNOWN','REJECTED'].includes(model.outcome??''))codeInput.current?.focus();},[model.outcome]);
 async function command(action:'SEND'|'VERIFY'|'CANCEL'|'STAGE'){
  if(blocked||model.busy||emailLock.current)return;
  const target=formatPhone(phone);if(action==='STAGE'&&!/^\+905\d{9}$/.test(target.replace(/\s/g,''))){setLocalError('Doğrulama için Türkiye +90 cep numarası girin.');phoneInput.current?.focus();return;}
  setLocalError('');setNote('');
  await model.execute(action,action==='VERIFY'?code:action==='STAGE'?target:'');setCode('');
  if(action==='SEND'||action==='VERIFY')codeInput.current?.focus();else Keyboard.dismiss();
 }
 async function email(){if(emailLock.current||blocked)return;emailLock.current=true;setEmailBusy(true);setCode('');setLocalError('');
  try{const completed=await model.email();setNote(completed?'Kimlik ekranından dönüldü. Güncel doğrulama durumu sunucudan okunur.':'E-posta işlemi iptal edildi.');}
  catch{setLocalError('E-posta işlemi tamamlanamadı. Bağlantınızı ve hesabınızı kontrol edin.');}
  finally{emailLock.current=false;setEmailBusy(false);}
 }
 const open=p&&challengeOpen(p)&&!model.expired&&!blocked;
 const busy=model.busy||emailBusy;
 return <View style={s.card} accessibilityLabel="İletişim doğrulaması">
  <View style={s.row}><Icon name="shield-checkmark-outline"/><Text accessibilityRole="header" style={s.title}>İletişim doğrulaması</Text></View>
  <Text style={s.help}>Hesabınıza aittir; mülk veya rol seçmeniz gerekmez. Telefon doğrulaması isteğe bağlıdır.</Text>
  {blocked?<View style={s.row}><Icon name="cloud-offline-outline"/><Text style={[s.help,s.flex]}>Güncel doğrulama için çevrimiçi ve açık bir oturum gerekir. Kod işlemleri çevrimdışı kaydedilmez.</Text></View>:<Button title={model.busy?'Durum okunuyor…':'Doğrulama durumunu yenile'} icon="refresh-outline" disabled={busy} onPress={()=>void model.refresh()}/>}
  {!!(localError||model.error)&&<Text accessibilityRole="alert" accessibilityLiveRegion="polite" style={s.error}>{localError||model.error}</Text>}
  {!!model.emailMessage&&<Text accessibilityRole={model.emailError?'alert':undefined} accessibilityLiveRegion="polite" style={model.emailError?s.error:s.help}>{model.emailMessage}</Text>}
  {!!note&&<Text accessibilityLiveRegion="polite" style={s.help}>{note}</Text>}
  {contact&&!blocked&&<>
   <View style={s.channel}><View style={s.row}><Icon name="mail-outline"/><Text style={s.heading}>E-posta</Text></View><Status value={contact.email.status}/><Text style={s.target}>{maskContact(contact.email.value,'email')}</Text><Text style={s.help}>E-posta doğrulaması ve değişikliği güvenli kimlik ekranında yapılır. Yeni adres onaylanana kadar eski adresiniz korunur; bekleyen adres kimlik ekranındadır.</Text><Button title={contact.email.status==='UNVERIFIED'?'E-postayı doğrula / yeniden gönder':'E-postayı değiştir'} icon="mail-outline" disabled={busy||!contact.email.available} onPress={()=>void email()}/></View>
   <View style={s.channel}><View style={s.row}><Icon name="call-outline"/><Text style={s.heading}>Cep telefonu</Text></View><Status value={p!.status}/><Text style={s.label}>Doğrulanmış numara</Text><Text style={s.target}>{maskContact(p!.verifiedValue,'phone')}</Text><Text style={s.label}>Bekleyen numara</Text><Text style={s.target}>{maskContact(p!.pendingValue,'phone')}</Text>
    <FormField ref={phoneInput} label="Doğrulanacak cep telefonu" value={phone} onChangeText={v=>setPhone(phoneChars(v))} onEndEditing={()=>setPhone(v=>formatPhone(v))} keyboardType="phone-pad" autoComplete="tel" maxLength={32} editable={!busy} help="Türkiye +90 cep numarası. Yeni numara doğrulanana kadar eski doğrulama korunur."/>
    <Button title="Numarayı hazırla" icon="create-outline" disabled={busy||!phone.trim()} onPress={()=>void command('STAGE')}/>
    {!p!.canSendCode&&<Text style={s.help}>{p!.status==='VERIFIED'?'Bu numara doğrulanmış. Değiştirmek için yeni numarayı hazırlayın.':'Kod gönderimi kullanılamıyor. Numarayı kontrol edin veya servis açıldığında tekrar deneyin. Alınmış geçerli kod kullanılabilir.'}</Text>}
    <Button title={p!.challengeId?'Kodu yeniden gönder':'SMS kodu gönder'} icon="paper-plane-outline" disabled={busy||model.remaining>0||!p!.canSendCode} onPress={()=>void command('SEND')}/>
    {model.remaining>0&&<Text style={s.help}>Yeniden gönderim: {model.remaining} saniye</Text>}
    {p!.expiresAt&&<Text style={s.help}>Son geçerlilik: {new Date(p!.expiresAt).toLocaleString('tr-TR')}</Text>}
    {open&&<><Text style={s.label}>Kalan deneme: {p!.attemptsRemaining}</Text><FormField ref={codeInput} label="6 haneli doğrulama kodu" value={code} onChangeText={v=>setCode(v.replace(/[^0-9]/g,'').slice(0,6))} keyboardType="number-pad" autoComplete="sms-otp" textContentType="oneTimeCode" maxLength={6} editable={!busy} returnKeyType="done" onSubmitEditing={()=>{if(code.length===6)void command('VERIFY');}} help="SMS ile aldığınız 6 rakamı girin. Kod kaydedilmez."/><Button title="Telefonu doğrula" icon="checkmark-circle-outline" disabled={busy||code.length!==6} onPress={()=>void command('VERIFY')}/></>}
    {p!.challengeId&&['ACCEPTED','UNKNOWN'].includes(p!.deliveryStatus)&&<Button title="Bekleyen kodu iptal et" icon="close-circle-outline" disabled={busy} onPress={()=>void command('CANCEL')}/>}
    {(model.outcome||model.expired)&&<Text accessibilityLiveRegion="polite" style={s.help}>{messages[model.expired?'EXPIRED':model.outcome!]}</Text>}
   </View>
  </>}
 </View>;
}
const s=StyleSheet.create({card:{backgroundColor:theme.colors.surface,borderWidth:1,borderColor:theme.colors.border,borderRadius:theme.radius.card,padding:20,gap:14},row:{flexDirection:'row',alignItems:'center',gap:9},flex:{flex:1},title:{fontSize:18,fontWeight:'700',color:theme.colors.primary,flexShrink:1},heading:{fontSize:16,fontWeight:'600',color:theme.colors.primary},label:{fontSize:13,color:theme.colors.muted},target:{fontSize:16,fontWeight:'600',color:theme.colors.text},help:{fontSize:13,lineHeight:21,color:theme.colors.muted},error:{fontSize:14,lineHeight:22,color:'#bf3b41'},channel:{gap:12,paddingTop:14,borderTopWidth:1,borderColor:theme.colors.border},status:{alignSelf:'flex-start',flexDirection:'row',alignItems:'center',gap:6,paddingHorizontal:10,paddingVertical:7,borderRadius:16},button:{minHeight:48,backgroundColor:theme.colors.primary,paddingHorizontal:14,paddingVertical:12,borderRadius:12,flexDirection:'row',alignItems:'center',justifyContent:'center',gap:8},buttonText:{color:theme.colors.surface,fontWeight:'600',fontSize:14,flexShrink:1},disabled:{opacity:.5}});
