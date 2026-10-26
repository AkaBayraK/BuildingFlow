import {ContactVerificationCard} from '../../contact-verification/ContactVerificationCard';
import {CommunicationCard} from '../../communication/CommunicationCard';
import { Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { Icon, Screen, SectionTitle, styles } from '../../components/Screen';
import {ProfileEditor} from '../../components/ProfileEditor';
import { SyncPanel } from '../../components/SyncPanel';
import { useAuth } from '../../auth/AuthProvider';
import { contextKey, roleLabels } from '../../auth/contracts';
import { PersonCodeCard } from '../../components/PersonCodeCard';
import { ConsentCenter } from '../../components/ConsentCenter';
export default function Profile() {
  const auth = useAuth();
  return <Screen title="Hesabım" subtitle="Mülklerin ve hesap bilgilerin">
    <ProfileEditor key={auth.session?.userId} />
    <ContactVerificationCard key={auth.session?.userId}/>
    <CommunicationCard key={'communication:'+auth.session?.userId}/>
    <SectionTitle title="Rızalarım" />
    <ConsentCenter />
    <SectionTitle title="Mülklerim ve rollerim" note={`${auth.session?.contexts.length ?? 0} bağlantı`} />
    <View style={s.contexts}>{auth.session?.contexts.map(c => { const selected = !!auth.selected && contextKey(auth.selected) === contextKey(c); return <Pressable key={contextKey(c)} accessibilityRole="button" accessibilityLabel={`${c.propertyName}, ${roleLabels[c.role]}${c.unitNumber ? `, Daire ${c.unitNumber}` : ''}`} accessibilityState={{ selected }} onPress={() => auth.choose(contextKey(c))} style={({ pressed }) => [s.context, selected && s.selected, pressed && { opacity: 0.75 }]}><View style={[s.buildingIcon, selected && { backgroundColor: '#dceaf9' }]}><Icon name={c.unitNumber ? 'home-outline' : 'business-outline'} color={selected ? '#2056bc' : '#617187'} /></View><View style={styles.flex}><Text style={s.property}>{c.propertyName}</Text><Text style={styles.muted}>{c.unitNumber ? `Daire ${c.unitNumber} · ` : ''}{roleLabels[c.role]}</Text>{selected && <Text style={s.selectedLabel}>Şu anda seçili</Text>}</View><Icon name={selected ? 'checkmark-circle' : 'chevron-forward'} color={selected ? '#2056bc' : '#8a98aa'} size={22} /></Pressable>; })}
    {!auth.session?.contexts.length && <View style={styles.emptyCard}><Icon name="home-outline" size={36} /><Text style={styles.cardTitle}>Henüz bağlı mülk yok</Text><Text style={styles.emptyDescription}>Yönetimin gönderdiği kişi kodunu aşağıya girerek ev bilgilerini açabilirsin. Kodun yoksa yöneticinle iletişime geç.</Text></View>}</View>
    {auth.session && !auth.session.contexts.some(c => c.role === 'SUPER_ADMIN' || c.role === 'SITE_ADMIN') && <PersonCodeCard linked={auth.session.contexts.length > 0} />}
    <SectionTitle title="Veri güncelliği" /><View style={styles.card}><View style={styles.locationRow}><Icon name={auth.offline ? 'cloud-offline-outline' : 'time-outline'} /><Text style={[styles.text, styles.flex]}>{auth.offline ? 'Kaydedilen kopyayı görüntülüyorsun' : 'Son hesap doğrulaması'}</Text></View><Text style={styles.muted}>{auth.verifiedAt ? new Date(auth.verifiedAt).toLocaleString('tr-TR') : 'Henüz doğrulanmadı'}</Text><Text style={styles.muted}>{Platform.OS === 'web' ? 'Tarayıcı önizlemesi çevrimdışı veriyi kalıcı saklamaz.' : 'Son doğrulanmış hesap ve mülk bilgilerin en fazla 24 saat çevrimdışı okunabilir. Çıkışta cihazdaki kopya silinir.'}</Text></View>
    <SyncPanel />
    <SectionTitle title="Oturum" /><View style={s.logoutCard}><Pressable accessibilityRole="button" accessibilityLabel="Çıkış yap ve hesap değiştir" onPress={() => void auth.logout()} style={({ pressed }) => [s.logout, pressed && { opacity: 0.7 }]}><Icon name="log-out-outline" color="#bf3b41" /><Text style={s.logoutText}>Çıkış yap</Text><Icon name="chevron-forward" color="#bf3b41" size={18} /></Pressable><Text style={s.logoutNote}>Başka bir hesap kullanmak için çıkış yapıp yeniden giriş yapabilirsin.</Text></View>
  </Screen>;
}
const s = StyleSheet.create({
  identity: { backgroundColor: '#fff', padding: 18, borderRadius: 20, flexDirection: 'row', alignItems: 'center', gap: 13, borderWidth: 1, borderColor: '#e5ebf2' }, avatar: { width: 56, height: 56, borderRadius: 28, backgroundColor: '#e1edf7', alignItems: 'center', justifyContent: 'center' }, initial: { fontSize: 23, fontWeight: '700', color: '#082b49' }, name: { fontSize: 18, fontWeight: '700', color: '#102342', lineHeight: 25 }, contexts: { gap: 10 },
  context: { padding: 16, backgroundColor: '#fff', borderRadius: 17, borderWidth: 1, borderColor: '#e5ebf2', flexDirection: 'row', gap: 12, alignItems: 'center', minHeight: 85 }, selected: { backgroundColor: '#f2f7ff', borderColor: '#7b9fda' }, buildingIcon: { width: 43, height: 48, borderRadius: 12, backgroundColor: '#f0f3f8', alignItems: 'center', justifyContent: 'center' }, property: { fontSize: 15, color: '#102342', fontWeight: '600', lineHeight: 22 }, selectedLabel: { color: '#2056bc', fontSize: 11, fontWeight: '600', marginTop: 5 },
  logoutCard: { backgroundColor: '#fff', borderRadius: 18, borderWidth: 1, borderColor: '#e5ebf2', padding: 17, gap: 4 }, logout: { flexDirection: 'row', alignItems: 'center', gap: 12, minHeight: 48 }, logoutText: { flex: 1, color: '#bf3b41', fontSize: 15, fontWeight: '600' }, logoutNote: { color: '#617187', fontSize: 13, lineHeight: 21 },
});
