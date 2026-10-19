import {useMemo} from 'react';
import {Alert,Linking,Platform,StyleSheet,Text,View} from 'react-native';
import {parseMarkup,safeHref,type MarkupInline} from './markup';
import {theme} from '../theme';
// Draws the restricted consent format with plain Text; links open only after the person confirms the full address.
function open(href:string){
 if(!safeHref(href))return;
 if(Platform.OS==='web'){if(globalThis.confirm?.('Bağlantı açılsın mı?\n'+href))void Linking.openURL(href);return;}
 Alert.alert('Bağlantı açılsın mı?',href,[{text:'Vazgeç',style:'cancel'},{text:'Aç',onPress:()=>{void Linking.openURL(href).catch(()=>Alert.alert('Bağlantı açılamadı',href));}}]);
}
function Inline({content}:{content:MarkupInline[]}){
 return <>{content.map((part,i)=>part.type==='bold'?<Text key={i} style={s.bold}>{part.text}</Text>
  :part.type==='link'?<Text key={i} accessibilityRole="link" accessibilityHint={part.href} style={s.link} onPress={()=>open(part.href)}>{part.text}</Text>
  :<Text key={i}>{part.text}</Text>)}</>;
}
export function MarkupView({body}:{body:string}){
 const result=useMemo(()=>parseMarkup(body),[body]);
 if(result.error)return <Text accessibilityRole="alert" style={s.error}>Bu metin güvenli biçimde gösterilemiyor. Onay verilemez.</Text>;
 return <View style={s.body}>{result.blocks.map((block,i)=>block.type==='heading'
  ?<Text key={i} accessibilityRole="header" style={[s.heading,block.level===1?s.h1:block.level===2?s.h2:s.h3]}><Inline content={block.content}/></Text>
  :block.type==='paragraph'?<Text key={i} style={s.text}><Inline content={block.content}/></Text>
  :<View key={i} style={s.list}>{block.items.map((item,n)=><View key={n} style={s.item}><Text style={s.marker} accessible={false}>{block.ordered?`${block.start+n}.`:'•'}</Text><Text style={[s.text,s.flex]}><Inline content={item}/></Text></View>)}</View>)}</View>;
}
const s=StyleSheet.create({body:{gap:12},heading:{color:theme.colors.text,fontWeight:'700',letterSpacing:-0.2},h1:{fontSize:21,lineHeight:28},h2:{fontSize:18,lineHeight:25,marginTop:4},h3:{fontSize:16,lineHeight:23},text:{color:theme.colors.text,fontSize:15,lineHeight:24},bold:{fontWeight:'700'},link:{color:'#2056bc',textDecorationLine:'underline',fontWeight:'600'},list:{gap:6},item:{flexDirection:'row',gap:8},marker:{color:theme.colors.muted,fontSize:15,lineHeight:24,minWidth:18},flex:{flex:1},error:{color:'#9e333a',fontSize:14,lineHeight:22}});
