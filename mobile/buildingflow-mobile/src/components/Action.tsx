import {Pressable,type PressableProps} from 'react-native';
// Keep native keyboard/touch behavior while exposing disabled state consistently.
export function Action({disabled,accessibilityState,style,...props}:PressableProps){
 const inactive=disabled??accessibilityState?.disabled??false;
 return <Pressable accessibilityRole="button" {...props} disabled={inactive} accessibilityState={{...accessibilityState,disabled:inactive}} style={state=>[typeof style==='function'?style(state):style,{minHeight:48,minWidth:48}]}/>;
}
