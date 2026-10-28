import {StyleSheet,Text} from 'react-native';
import {Action} from '../components/Action';
import {Icon} from '../components/Screen';
import {theme} from '../theme';
export const styles=StyleSheet.create({box:{padding:16,gap:12,borderRadius:18,borderWidth:1,borderColor:theme.colors.border,backgroundColor:'#fff'},row:{flexDirection:'row',alignItems:'center',flexWrap:'wrap',gap:10},title:{fontSize:21,fontWeight:'700',color:'#102342'},text:{fontSize:15,lineHeight:23,color:'#253b56'},muted:{fontSize:13,lineHeight:20,color:'#617187'},button:{minHeight:48,padding:12,borderRadius:12,borderWidth:1,borderColor:'#c9d4e3',flexDirection:'row',alignItems:'center',gap:8},alert:{fontSize:14,lineHeight:22,color:'#9b3039'},gap:{gap:12}});
const s=styles;
export function Button({label,icon='refresh-outline',onPress,disabled=false}:{label:string;icon?:Parameters<typeof Icon>[0]['name'];onPress:()=>void;disabled?:boolean}){return <Action disabled={disabled} style={[s.button,{opacity:disabled?0.5:1}]} onPress={onPress}><Icon name={icon}/><Text style={s.text}>{label}</Text></Action>;}
