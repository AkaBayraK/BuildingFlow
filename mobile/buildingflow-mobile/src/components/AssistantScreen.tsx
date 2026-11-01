import {useCallback,useEffect,useRef,useState} from 'react';
import {StyleSheet,Text,TextInput,View} from 'react-native';
import {Link} from 'expo-router';
import {useAuth} from '../auth/AuthProvider';
import {useConsentPrompt} from '../consents/ConsentProvider';
import {Action} from './Action';
import {Card,Icon,Screen,styles} from './Screen';
import {theme} from '../theme';
import {assistantGate,assistantMessageLimit,canAsk,cardValue,chatBody,factLabels,readAnswer,readPrompts,readStatus,statusLabels,type AssistantAnswer,type AssistantGate,type AssistantPrompt,type AssistantStatus} from '../assistant/contracts';

type Turn={id:number;question:string;answer:AssistantAnswer|null;error:string};

// Asks the server whether the assistant may show at all; nothing is rendered while it is off or unknown.
export function useAssistantGate(){
 const auth=useAuth();const [result,setResult]=useState<{key:string;gate:AssistantGate;status:AssistantStatus|null}>({key:'',gate:'hidden',status:null}),[revision,setRevision]=useState(0);
 const selected=auth.selected;const eligible=!!selected&&!!selected.unitId&&['OWNER','TENANT'].includes(selected.role)&&!auth.offline;
 const key=eligible&&selected?selected.propertyId+':'+selected.unitId+':'+selected.role+':'+revision:'';
 useEffect(()=>{if(!key)return;let live=true;
  void auth.assistantRequest('status').then(async response=>{const body=await response.json().catch(()=>null);if(!live)return;const next=assistantGate(response.status,body);
   if(next==='ready'){try{setResult({key,gate:'ready',status:readStatus(body)});}catch{setResult({key,gate:'hidden',status:null});}}else setResult({key,gate:next,status:null});})
   .catch(()=>{if(live)setResult({key,gate:'hidden',status:null});});
  return()=>{live=false;};
 // eslint-disable-next-line react-hooks/exhaustive-deps
 },[key]);
 // A result for another unit, role or an older revision is never shown.
 const current=result.key===key&&key!=='';
 const setStatus=(update:(s:AssistantStatus|null)=>AssistantStatus|null)=>setResult(r=>({...r,status:update(r.status)}));
 return {gate:current?result.gate:'hidden' as AssistantGate,status:current?result.status:null,setStatus,refresh:()=>setRevision(n=>n+1)};
}

// Home entry: hidden while the feature is off; offers the existing consent flow when consent is missing.
export function AssistantEntry(){
 const {gate,refresh}=useAssistantGate(),prompt=useConsentPrompt();const [message,setMessage]=useState('');
 if(gate==='hidden')return null;
 const consent=async()=>{setMessage('');try{if(await prompt.ask('AI_ASSISTANT'))refresh();}catch{setMessage('Asistan rıza metni henüz yayınlanmadı veya alınamadı.');}};
 return <View style={s.entry}>
  <View style={s.entryRow}><Icon name="sparkles-outline"/><View style={styles.flex}><Text style={styles.cardTitle}>Asistan</Text><Text style={styles.muted}>Kendi borcunuz, talepleriniz ve duyurular hakkında kısa özet. Salt okunur.</Text></View></View>
  {gate==='ready'?<Link href="/assistant" asChild><Action style={s.primary} accessibilityLabel="Asistanı aç"><Text style={s.primaryText}>Asistanı aç</Text></Action></Link>
  :<Action style={s.primary} onPress={()=>void consent()} accessibilityLabel="Asistan için rıza metnini görüntüle"><Text style={s.primaryText}>Rıza metnini görüntüle</Text></Action>}
  {!!message&&<Text style={styles.muted} accessibilityRole="alert">{message}</Text>}
  {prompt.element}
 </View>;
}

export function AssistantScreen(){
 const auth=useAuth();const {gate,status,setStatus}=useAssistantGate();
 const [prompts,setPrompts]=useState<AssistantPrompt[]|null>(null),[turns,setTurns]=useState<Turn[]>([]),[message,setMessage]=useState(''),[busy,setBusy]=useState(false),[error,setError]=useState('');
 const lock=useRef(false),serial=useRef(0);
 useEffect(()=>{if(gate!=='ready')return;let live=true;void auth.assistantRequest('prompts').then(async r=>{if(!r.ok)throw Error();const v=readPrompts(await r.json());if(live)setPrompts(v.sort((a,b)=>a.sortOrder-b.sortOrder));}).catch(()=>{if(live)setError('Hazır sorular alınamadı.');});return()=>{live=false;};
 // eslint-disable-next-line react-hooks/exhaustive-deps
 },[gate]);
 const ask=useCallback(async(input:{promptCode:string;label:string}|{message:string})=>{
  if(lock.current||!auth.selected)return;lock.current=true;setBusy(true);const id=++serial.current;const question='label' in input?input.label:input.message.trim();
  setTurns(t=>[...t,{id,question,answer:null,error:''}]);
  try{
   const r=await auth.assistantRequest('chat',chatBody(auth.selected,'label' in input?{promptCode:input.promptCode}:{message:input.message}));
   const body=await r.json().catch(()=>null);
   if(!r.ok)throw Error(r.status===429?'Bugünkü soru sınırına ulaştınız.':r.status===403?'Asistan şu an kullanılamıyor veya rızanız geri çekildi.':'Soru gönderilemedi.');
   const answer=readAnswer(body);setTurns(t=>t.map(x=>x.id===id?{...x,answer}:x));setStatus(st=>st?{...st,quota:answer.quota}:st);if(!('label' in input))setMessage('');
  }catch(e){setTurns(t=>t.map(x=>x.id===id?{...x,error:e instanceof Error&&e.message?e.message:'Soru gönderilemedi.'}:x));}
  finally{lock.current=false;setBusy(false);}
 },[auth,setStatus]);
 if(gate!=='ready')return <Screen title="Asistan" subtitle="Salt okunur özet"><View style={styles.emptyCard}><Icon name="sparkles-outline" size={34}/><Text style={styles.emptyTitle}>Asistan kullanılamıyor</Text><Text style={styles.emptyDescription}>Asistan bu daire için açık değil veya rızanız gerekli. Ana sayfadaki asistan kartından devam edebilirsiniz.</Text></View></Screen>;
 const exhausted=status?.quota.remaining===0;
 return <Screen title="Asistan" subtitle="Salt okunur özet">
  <View style={s.badges}><Text style={s.badge}>Salt okunur</Text><Text style={s.badge}>{status?.mode==='AKAAI'?'Model: AkaAI':'Kural tabanlı'}</Text>{status&&<Text style={s.badge}>{`Bugün ${status.quota.usedToday}/${status.quota.dailyLimit}`}</Text>}</View>
  <Card title="Hazır sorular">
   {!prompts?<Text style={styles.muted}>{error||'Yükleniyor…'}</Text>:prompts.map(p=><Action key={p.code} style={s.prompt} disabled={busy||exhausted} onPress={()=>void ask({promptCode:p.code,label:p.textTr})} accessibilityLabel={p.textTr}><Icon name="chatbubble-ellipses-outline" size={18}/><Text style={[styles.text,styles.flex]}>{p.titleTr}</Text></Action>)}
   {exhausted&&<Text style={styles.muted} accessibilityRole="alert">Bugünkü soru sınırına ulaştınız.</Text>}
  </Card>
  {turns.map(turn=><Card key={turn.id} title={turn.question}>
   {turn.error?<Text style={s.error} accessibilityRole="alert">{turn.error}</Text>:!turn.answer?<Text style={styles.muted}>Veriler okunuyor…</Text>:<>
    <Text style={styles.text}>{turn.answer.answer}</Text>
    <Text style={styles.muted}>{turn.answer.source==='AKAAI'?'AkaAI özeti':'Kural tabanlı'}{turn.answer.modelFallback?' · model yanıtı kullanılmadı':''}</Text>
    {turn.answer.sources.map((c,i)=><Link key={c.key+i} href={c.href} asChild><Action style={s.source} accessibilityLabel={`${factLabels[c.key]??'Kaynak'}: ${cardValue(c)}`}>
     <View style={styles.flex}><Text style={s.sourceValue}>{cardValue(c)}</Text><Text style={styles.muted}>{factLabels[c.key]??'Kaynak'}{c.label?` · ${c.label}`:c.dimension&&statusLabels[c.dimension]?` · ${statusLabels[c.dimension]}`:''}</Text></View><Icon name="chevron-forward-outline" size={18}/></Action></Link>)}
   </>}
  </Card>)}
  <Card title="Soru sor">
   <TextInput accessibilityLabel="Sorunuz" style={s.input} value={message} onChangeText={setMessage} maxLength={assistantMessageLimit} multiline editable={!busy} placeholder="Örn. Borcum ne kadar?" placeholderTextColor={theme.colors.muted}/>
   <Text style={styles.muted}>Kişi adı veya telefon yazmayın. Sorular saklanmaz. {message.trim().length}/{assistantMessageLimit}</Text>
   <Action style={s.primary} disabled={busy||exhausted||!canAsk(message)} onPress={()=>void ask({message})} accessibilityLabel="Soruyu gönder"><Text style={s.primaryText}>Gönder</Text></Action>
  </Card>
 </Screen>;
}

const s=StyleSheet.create({
 entry:{backgroundColor:theme.colors.surface,borderRadius:theme.radius.card,borderWidth:1,borderColor:theme.colors.border,padding:16,gap:12,marginTop:12},
 entryRow:{flexDirection:'row',gap:12,alignItems:'center'},
 primary:{backgroundColor:theme.colors.primary,borderRadius:theme.radius.field,alignItems:'center',justifyContent:'center',paddingHorizontal:16},
 primaryText:{color:'#fff',fontWeight:'700',fontSize:15},
 badges:{flexDirection:'row',flexWrap:'wrap',gap:8,marginBottom:4},
 badge:{backgroundColor:'#e5efff',color:theme.colors.primary,borderRadius:12,paddingHorizontal:10,paddingVertical:4,fontSize:12,fontWeight:'700',overflow:'hidden'},
 prompt:{flexDirection:'row',alignItems:'center',gap:10,borderWidth:1,borderColor:theme.colors.border,borderRadius:theme.radius.field,paddingHorizontal:12,marginTop:8},
 source:{flexDirection:'row',alignItems:'center',gap:10,borderWidth:1,borderColor:theme.colors.border,borderRadius:theme.radius.field,padding:12,marginTop:8},
 sourceValue:{color:theme.colors.text,fontSize:16,fontWeight:'700'},
 input:{minHeight:72,borderWidth:1,borderColor:theme.colors.border,borderRadius:theme.radius.field,padding:12,color:theme.colors.text,textAlignVertical:'top'},
 error:{color:'#a12626'},
});
