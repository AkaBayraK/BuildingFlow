import type {Ref} from 'react';
import {StyleSheet,Text,TextInput,View,type TextInputProps} from 'react-native';
import {theme} from '../theme';
export function FormField({label,error,help,...input}:TextInputProps&{label:string;error?:string;help?:string;ref?:Ref<TextInput>}){
 return <View style={s.field}><Text style={s.label}>{label}</Text><TextInput accessibilityLabel={label} accessibilityHint={error??help} placeholderTextColor="#8593a6" style={[s.input,!!error&&s.invalid]} {...input}/>{error?<Text accessibilityLiveRegion="polite" style={s.error}>{error}</Text>:help?<Text style={s.help}>{help}</Text>:null}</View>;
}
const s=StyleSheet.create({
 field:{gap:6},label:{color:'#102342',fontSize:14,fontWeight:'600'},
 input:{minHeight:50,borderWidth:1,borderColor:'#c9d4e3',borderRadius:12,paddingHorizontal:14,fontSize:16,color:theme.colors.primary,backgroundColor:'#fff'},
 invalid:{borderColor:'#bf3b41'},error:{color:'#bf3b41',fontSize:13,lineHeight:18},help:{color:'#617187',fontSize:13,lineHeight:18},
});
