import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { BuildingFlowMark } from '../components/BuildingFlowMark';
import { SafeAreaView } from 'react-native-safe-area-context';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useAuth } from './AuthProvider';

export function LoginScreen() {
  const auth = useAuth();
  const working = auth.state === 'loading' || auth.state === 'working';
  return <SafeAreaView style={s.safe}>
    <ScrollView contentContainerStyle={s.page}>
      <View style={s.hero}>
        <View style={s.brandRow}><View style={s.brandMark}><BuildingFlowMark size={38} variant="light" /></View><Text style={s.brand}>Building<Text style={s.brandFlow}>Flow</Text></Text></View>
        <Text style={s.overline}>DAHA DÜZENLİ BİNALAR</Text>
        <Text accessibilityRole="header" style={s.heading}>Daha huzurlu{ '\n' }yaşam alanları.</Text>
        <Text style={s.heroText}>Evin, binan ve günlük yaşamın.{ '\n' }Hepsi tek bir yerde.</Text>
        <View style={s.roleRow}>{['Ev sahibi', 'Kiracı', 'Yönetici'].map(role => <View key={role} style={s.role}><Text style={s.roleText}>{role}</Text></View>)}</View>
      </View>
      <View style={s.body}>
        <Text accessibilityRole="header" style={s.title}>Evine hoş geldin</Text>
        <Text style={s.description}>Bina yaşamını kolaylaştıran hesabına güvenle giriş yap.</Text>
        <View style={s.features}>
          <Feature icon="wallet-outline" label="Aidat takibi" color="#08785b" background="#e1f5ec" />
          <Feature icon="megaphone-outline" label="Duyurular" color="#1859b4" background="#e5efff" />
          <Feature icon="construct-outline" label="Talepler" color="#855015" background="#fff0dc" />
        </View>
        {working ? <><ActivityIndicator color="#102342" accessibilityLabel="Oturum kontrol ediliyor" /><Pressable accessibilityRole="button" style={s.secondary} onPress={auth.cancel}><Text style={s.secondaryText}>Vazgeç</Text></Pressable></> : <Pressable accessibilityRole="button" disabled={!auth.ready} style={[s.primary, !auth.ready && { opacity: 0.55 }]} onPress={() => void auth.login()}><Text style={s.primaryText}>Giriş yap</Text><Ionicons name="arrow-forward" size={20} color="#fff" accessible={false} aria-hidden={true} /></Pressable>}
        {!!auth.message && <View style={s.message}><Ionicons name="information-circle-outline" size={22} color="#23466a" accessible={false} aria-hidden={true} /><Text accessibilityLiveRegion="polite" style={s.messageText}>{auth.message}</Text></View>}
        {auth.canRetry && <Pressable accessibilityRole="button" style={s.secondary} onPress={auth.retrySetup}><Text style={s.secondaryText}>Bağlantıyı yeniden dene</Text></Pressable>}
        <View style={s.security}><Ionicons name="shield-checkmark-outline" size={18} color="#52647a" accessible={false} aria-hidden={true} /><Text style={s.securityText}>Giriş bilgilerin güvenli BuildingFlow giriş sayfasında kullanılır.</Text></View>
        <Text style={s.footer}>İlk giriş için internet bağlantısı gerekir.</Text>
      </View>
    </ScrollView>
  </SafeAreaView>;
}
function Feature({ icon, label, color, background }: { icon: React.ComponentProps<typeof Ionicons>['name']; label: string; color: string; background: string }) {
  return <View style={s.feature}><View style={[s.featureIcon, { backgroundColor: background }]}><Ionicons name={icon} size={23} color={color} accessible={false} aria-hidden={true} /></View><Text style={s.featureLabel}>{label}</Text></View>;
}
const s = StyleSheet.create({
  safe: { flex: 1, backgroundColor: '#f6f8fc' }, page: { width: '100%', maxWidth: 560, alignSelf: 'center', padding: 16, gap: 24 },
  hero: { backgroundColor: '#082641', borderRadius: 28, padding: 24, gap: 16 },
  brandRow: { flexDirection: 'row', alignItems: 'center', gap: 12 }, brandMark: { width: 48, height: 48, borderRadius: 14, backgroundColor: '#234c6d', alignItems: 'center', justifyContent: 'center' },
  brand: { fontSize: 23, color: '#fff', fontWeight: '700', flexShrink: 1 }, brandFlow: { color: '#00cafa' }, overline: { fontSize: 11, fontWeight: '700', letterSpacing: 1.8, color: '#b5d4ee', marginTop: 8 },
  heading: { fontSize: 32, lineHeight: 40, fontWeight: '700', color: '#fff', letterSpacing: -0.8 }, heroText: { fontSize: 15, lineHeight: 23, color: '#cfdeeb' },
  roleRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: 4 }, role: { backgroundColor: '#234c6d', borderRadius: 20, paddingHorizontal: 12, paddingVertical: 7 }, roleText: { color: '#e8f3fc', fontSize: 12 },
  body: { paddingHorizontal: 8, gap: 16 }, title: { fontSize: 25, fontWeight: '700', color: '#102342', letterSpacing: -0.6 }, description: { fontSize: 16, lineHeight: 24, color: '#52647a' },
  features: { flexDirection: 'row', gap: 8, paddingVertical: 8 }, feature: { flex: 1, alignItems: 'center', gap: 10 }, featureIcon: { width: 50, height: 50, borderRadius: 16, justifyContent: 'center', alignItems: 'center' }, featureLabel: { color: '#102342', fontSize: 13, textAlign: 'center' },
  primary: { backgroundColor: '#082641', borderRadius: 14, minHeight: 54, padding: 16, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 12 }, primaryText: { color: '#fff', fontSize: 16, fontWeight: '600' },
  secondary: { borderWidth: 1, borderColor: '#c9d4e3', borderRadius: 14, minHeight: 48, padding: 14, alignItems: 'center' }, secondaryText: { color: '#102342', fontSize: 16 },
  message: { flexDirection: 'row', gap: 10, backgroundColor: '#eaf2fa', borderRadius: 14, padding: 14 }, messageText: { flex: 1, fontSize: 14, lineHeight: 22, color: '#23466a' }, security: { flexDirection: 'row', gap: 8, alignItems: 'flex-start' }, securityText: { flex: 1, fontSize: 13, lineHeight: 20, color: '#52647a' }, footer: { textAlign: 'center', color: '#52647a', fontSize: 12, marginBottom: 8 },
});
