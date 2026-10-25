import type Ionicons from '@expo/vector-icons/Ionicons';
import type {ContactState} from './contracts';
export const contactStyle={
 VERIFIED:{icon:'checkmark-circle-outline',label:'Doğrulanmış',color:'#16733b',background:'#e9f7ee'},
 PENDING:{icon:'time-outline',label:'Doğrulama bekliyor',color:'#805719',background:'#fff7e7'},
 UNVERIFIED:{icon:'shield-outline',label:'Doğrulanmamış',color:'#805719',background:'#fff7e7'},
 MISSING:{icon:'help-circle-outline',label:'Kayıtlı değil',color:'#617187',background:'#f0f3f8'},
 UNKNOWN:{icon:'alert-circle-outline',label:'Durum alınamadı',color:'#bf3b41',background:'#fff1f1'},
} satisfies Record<ContactState,{icon:keyof typeof Ionicons.glyphMap;label:string;color:string;background:string}>;
