import type { ReactNode } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Link } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';
import { BuildingFlowMark } from './BuildingFlowMark';
import { theme } from '../theme';
import { useOptionalAuth } from '../auth/AuthProvider';
import { roleLabels } from '../auth/contracts';
export function Icon({ name, color = theme.colors.primary, size = 22 }: { name: keyof typeof Ionicons.glyphMap; color?: string; size?: number }) { return <Ionicons name={name} color={color} size={size} accessible={false} aria-hidden={true} />; }
export function Screen({ title, subtitle, home = false, children }: { title: string; subtitle?: string; home?: boolean; children: ReactNode }) {
  const auth = useOptionalAuth(); const username = auth?.session?.username ?? 'BuildingFlow';
  return <SafeAreaView style={styles.safe} edges={['top', 'left', 'right']}><ScrollView contentContainerStyle={styles.scroll}>
    <View style={styles.hero}>
      <View style={styles.brandRow}><View style={styles.brandLockup}><BuildingFlowMark size={32} variant="light" /><Text style={styles.brand}>Building<Text style={styles.brandFlow}>Flow</Text></Text></View><Link href="/profile" accessibilityLabel="Hesabım" style={styles.accountLink}><Icon name="person-outline" size={21} color="#fff" /></Link></View>
      {home ? <View style={styles.welcome}><View style={styles.avatar}><Text style={styles.avatarText}>{username.slice(0, 1).toLocaleUpperCase('tr-TR')}</Text></View><View style={styles.flex}><Text style={styles.heroEyebrow}>Merhaba,</Text><Text accessibilityRole="header" style={styles.homeTitle}>{username}</Text><Text style={styles.heroSubtitle}>{auth?.selected ? roleLabels[auth.selected.role] : 'BuildingFlow hesabın'}</Text></View></View> : <View style={styles.pageHeading}><Text accessibilityRole="header" style={styles.title}>{title}</Text>{subtitle && <Text style={styles.heroSubtitle}>{subtitle}</Text>}</View>}
    </View><View style={styles.content}>
      {auth?.offline && <View style={styles.offlineBanner}><Icon name="cloud-offline-outline" color="#805719" size={22} /><View style={styles.flex}><Text style={styles.offlineTitle}>Çevrimdışı · Salt okunur</Text><Text style={styles.offlineText}>Son doğrulama: {auth.verifiedAt ? new Date(auth.verifiedAt).toLocaleString('tr-TR') : 'Bilinmiyor'}. Yetkiler ve bilgiler değişmiş olabilir.</Text></View></View>}
      {!!auth?.cacheNote && <Text accessibilityRole="alert" style={styles.muted}>{auth.cacheNote}</Text>}
      {children}</View>
  </ScrollView></SafeAreaView>;
}
export function Card({ title, children }: { title: string; children: ReactNode }) { return <View style={styles.card}><Text accessibilityRole="header" style={styles.cardTitle}>{title}</Text>{children}</View>; }
export function SectionTitle({ title, note }: { title: string; note?: string }) { return <View style={styles.sectionHeading}><Text accessibilityRole="header" style={styles.cardTitle}>{title}</Text>{note && <Text style={styles.muted}>{note}</Text>}</View>; }
export function ModuleScreen({ title, subtitle, icon, description, items, tint = '#e5efff' }: { title: string; subtitle: string; icon: keyof typeof Ionicons.glyphMap; description: string; items: string[]; tint?: string }) {
  const auth = useOptionalAuth();
  return <Screen title={title} subtitle={subtitle}>{auth?.selected && <View style={styles.locationRow}><Icon name="business-outline" size={18} /><Text style={[styles.muted, styles.flex]}>{auth.selected.propertyName}{auth.selected.unitNumber ? ` · Daire ${auth.selected.unitNumber}` : ''}</Text></View>}
    <View style={styles.emptyCard}><View style={[styles.emptyIcon, { backgroundColor: tint }]}><Icon name={icon} size={35} /></View><Text accessibilityRole="header" style={styles.emptyTitle}>Yakında burada</Text><Text style={styles.emptyDescription}>{description}</Text><View style={styles.statusPill}><View style={styles.statusDot} /><Text style={styles.statusText}>Henüz kullanıma açılmadı</Text></View></View>
    <SectionTitle title="Bu alanda neler olacak?" /><View style={styles.card}>{items.map((item, index) => <View key={item} style={[styles.featureRow, index > 0 && styles.rowBorder]}><View style={styles.featureIcon}><Icon name={icon} size={19} /></View><Text style={[styles.text, styles.flex]}>{item}</Text><Icon name="lock-closed-outline" color={theme.colors.muted} size={16} /></View>)}</View>
  </Screen>;
}
export const styles = StyleSheet.create({
  offlineBanner: { backgroundColor: '#fff3db', borderWidth: 1, borderColor: '#efd4a0', borderRadius: 16, padding: 14, flexDirection: 'row', alignItems: 'center', gap: 11 }, offlineTitle: { color: '#6b4916', fontSize: 14, fontWeight: '700', lineHeight: 22 }, offlineText: { color: '#79551e', fontSize: 12, lineHeight: 19 },
  safe: { flex: 1, backgroundColor: '#062641' }, scroll: { flexGrow: 1, backgroundColor: theme.colors.background },
  hero: { backgroundColor: '#062641', paddingHorizontal: 22, paddingTop: 13, paddingBottom: 29, gap: 24 },
  brandRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', width: '100%', maxWidth: 520, alignSelf: 'center' }, brandLockup: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  brand: { color: '#fff', fontSize: 16, fontWeight: '700', letterSpacing: -0.4 }, brandFlow: { color: '#00cafa' }, accountLink: { padding: 13, borderRadius: 24, backgroundColor: '#153c58', minWidth: 48, minHeight: 48 },
  welcome: { flexDirection: 'row', alignItems: 'center', gap: 14, width: '100%', maxWidth: 520, alignSelf: 'center' },
  avatar: { width: 54, height: 54, backgroundColor: '#d7eaf4', borderRadius: 27, alignItems: 'center', justifyContent: 'center', borderWidth: 3, borderColor: '#335b76' }, avatarText: { color: '#082b49', fontSize: 23, fontWeight: '700' }, flex: { flex: 1, minWidth: 0 },
  heroEyebrow: { color: '#c0d4e2', fontSize: 14, lineHeight: 21 }, homeTitle: { color: '#fff', fontSize: 23, fontWeight: '700', lineHeight: 31, letterSpacing: -0.5 }, heroSubtitle: { color: '#c0d4e2', fontSize: 14, lineHeight: 22, marginTop: 4 },
  pageHeading: { width: '100%', maxWidth: 520, alignSelf: 'center' }, title: { color: '#fff', fontSize: 28, fontWeight: '700', letterSpacing: -0.7 },
  content: { padding: 20, gap: 18, width: '100%', maxWidth: 560, alignSelf: 'center', paddingBottom: 32 },
  card: { backgroundColor: theme.colors.surface, borderRadius: 20, padding: 18, gap: 12, borderWidth: 1, borderColor: theme.colors.border }, cardTitle: { color: theme.colors.text, fontSize: 18, fontWeight: '700', letterSpacing: -0.3 }, sectionHeading: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10, flexWrap: 'wrap' },
  text: { color: theme.colors.text, fontSize: 15, lineHeight: 23 }, muted: { color: theme.colors.muted, fontSize: 13, lineHeight: 21 },
  button: { backgroundColor: theme.colors.primary, borderRadius: 14, minHeight: 52, padding: 15, alignItems: 'center', justifyContent: 'center' }, buttonText: { color: '#fff', fontSize: 16, fontWeight: '600' }, locationRow: { flexDirection: 'row', gap: 9, alignItems: 'center' },
  emptyCard: { backgroundColor: '#fff', borderRadius: 22, paddingHorizontal: 22, paddingVertical: 30, alignItems: 'center', gap: 14, borderWidth: 1, borderColor: theme.colors.border }, emptyIcon: { width: 78, height: 78, borderRadius: 26, alignItems: 'center', justifyContent: 'center', marginBottom: 5 }, emptyTitle: { fontSize: 22, fontWeight: '700', color: theme.colors.text, textAlign: 'center' }, emptyDescription: { color: theme.colors.muted, fontSize: 15, lineHeight: 24, textAlign: 'center' },
  statusPill: { flexDirection: 'row', alignItems: 'center', gap: 7, paddingHorizontal: 12, paddingVertical: 8, borderRadius: 20, backgroundColor: '#f0f4f8', marginTop: 7 }, statusDot: { width: 6, height: 6, borderRadius: 3, backgroundColor: '#64748b' }, statusText: { color: '#506176', fontSize: 12, fontWeight: '600' },
  featureRow: { flexDirection: 'row', alignItems: 'center', gap: 12, minHeight: 54 }, featureIcon: { width: 36, height: 36, borderRadius: 11, backgroundColor: '#f0f4fa', alignItems: 'center', justifyContent: 'center' }, rowBorder: { borderTopWidth: 1, borderTopColor: theme.colors.border, paddingTop: 12 },
});
