import {ManagerScreen} from '../../components/ManagerPanel';
import {managerRoles} from '../../manager/contracts';
import { StyleSheet, Text, View } from 'react-native';
import { Link } from 'expo-router';
import { Icon, Screen, SectionTitle, styles } from '../../components/Screen';
import { useAuth } from '../../auth/AuthProvider';
import {OwnerPanel} from '../../components/OwnerPanel';
import { roleLabels } from '../../auth/contracts';
const actions = [
  { href: '/requests' as const, label: 'Talepler', icon: 'construct-outline' as const, tint: '#fff0df', color: '#a75c13' },
  { href: '/finance' as const, label: 'Ödemeler', icon: 'wallet-outline' as const, tint: '#dcf7ed', color: '#087a68' },
  { href: '/announcements' as const, label: 'Duyurular', icon: 'megaphone-outline' as const, tint: '#e2edff', color: '#2056bc' },
  { href: '/profile' as const, label: 'Hesabım', icon: 'person-outline' as const, tint: '#eee8ff', color: '#6544a3' },
];
export default function Home() {
  const { selected, session } = useAuth();
  if(selected&&managerRoles.includes(selected.role))return <ManagerScreen section="overview"/>;
  if(selected&&['OWNER','TENANT'].includes(selected.role))return <Screen title="Ana sayfa" home><View style={s.actions}>{actions.map(a=><Link key={a.href} href={a.href} style={s.action}><View style={s.actionContent}><View style={[s.actionCircle,{backgroundColor:a.tint}]}><Icon name={a.icon} color={a.color} size={23}/></View><Text style={s.actionLabel}>{a.href==='/finance'?'Borçlar':a.label}</Text></View></Link>)}</View><OwnerPanel key={selected.propertyId+selected.unitId+selected.role+session?.userId}/></Screen>;
  return <Screen title="Ana sayfa" home>
    <View style={s.actions}>{actions.map(a => <Link key={a.href} href={a.href} style={s.action} accessibilityLabel={a.label}><View style={s.actionContent}><View style={[s.actionCircle, { backgroundColor: a.tint }]}><Icon name={a.icon} color={a.color} size={23} /></View><Text style={s.actionLabel}>{a.label}</Text></View></Link>)}</View>
    <View style={s.propertyCard}>
      <View style={s.propertyVisual} accessible={false} aria-hidden={true}><View style={s.visualCircle} /><Icon name="business-outline" size={94} color="#adcddd" /><View style={s.visualLine} /><View style={s.visualLabel}><Icon name="home-outline" color="#d7eaf4" size={17} /><Text style={s.visualLabelText}>Bina yaşamın, tek yerde.</Text></View></View>
      <View style={s.propertyBody}><View style={styles.sectionHeading}><Text style={s.eyebrow}>{selected ? 'AKTİF MÜLKÜN' : 'MÜLK BAĞLANTISI'}</Text><Icon name="business-outline" size={18} color="#617187" /></View><Text style={s.propertyTitle}>{selected?.propertyName ?? 'Birlikte daha düzenli bir yaşam'}</Text><Text style={styles.muted}>{selected ? [selected.unitNumber ? `Daire ${selected.unitNumber}` : 'Bina yönetimi', roleLabels[selected.role]].join(' · ') : 'Hesabına henüz bir bina veya daire bağlanmamış.'}</Text><Link href="/profile" style={s.propertyLink}><Text style={s.propertyLinkText}>{selected ? 'Mülk ve rol seçimi' : 'Hesabımı görüntüle'}</Text><Icon name="arrow-forward" size={18} color="#2056bc" /></Link></View>
    </View>
    <SectionTitle title="Günlük yaşam" />
    <View style={s.dailyCard}>{actions.slice(0, 3).map((a, i) => <Link key={a.href} href={a.href} style={[s.dailyLink, i > 0 && s.divider]}><View style={s.dailyRow}><View style={[s.dailyIcon, { backgroundColor: a.tint }]}><Icon name={a.icon} color={a.color} size={21} /></View><View style={styles.flex}><Text style={s.dailyTitle}>{a.label}</Text><Text style={styles.muted}>{i === 0 ? 'Bakım ve arıza bildirimleri' : i === 1 ? 'Aidat ve ödeme takibi' : 'Binandan haberler'}</Text></View><Icon name="chevron-forward" size={18} color="#617187" /></View></Link>)}</View>
    {selected && <View style={s.contextNote}><Icon name="layers-outline" color="#617187" size={18} /><Text style={[styles.muted, styles.flex]}>{session?.contexts.length ?? 0} yetkili mülk / rol bağlantın var. Hesabım bölümünden seçim yapabilirsin.</Text></View>}
  </Screen>;
}
const s = StyleSheet.create({
  actions: { flexDirection: 'row', backgroundColor: '#fff', borderRadius: 20, paddingVertical: 15, paddingHorizontal: 4, borderWidth: 1, borderColor: '#e5ebf2' }, action: { flex: 1, minHeight: 72, padding: 3 }, actionContent: { alignItems: 'center', gap: 9 }, actionCircle: { width: 48, height: 48, borderRadius: 24, alignItems: 'center', justifyContent: 'center' }, actionLabel: { color: '#102342', fontSize: 11, fontWeight: '600', textAlign: 'center' },
  propertyCard: { borderRadius: 21, overflow: 'hidden', backgroundColor: '#fff', borderWidth: 1, borderColor: '#e5ebf2' }, propertyVisual: { height: 158, backgroundColor: '#153d57', alignItems: 'center', justifyContent: 'center', overflow: 'hidden' }, visualCircle: { position: 'absolute', width: 170, height: 170, borderRadius: 85, backgroundColor: '#204c66', right: -34, top: -72 }, visualLine: { position: 'absolute', height: 1, backgroundColor: '#4b6b80', width: '75%', bottom: 37 }, visualLabel: { position: 'absolute', left: 16, bottom: 13, flexDirection: 'row', gap: 7, alignItems: 'center' }, visualLabelText: { color: '#d7eaf4', fontSize: 12 },
  propertyBody: { padding: 18, gap: 8 }, eyebrow: { color: '#617187', fontSize: 10, fontWeight: '700', letterSpacing: 1.4 }, propertyTitle: { color: '#102342', fontSize: 21, fontWeight: '700', lineHeight: 28, letterSpacing: -0.5 }, propertyLink: { minHeight: 48, paddingVertical: 13, flexDirection: 'row', alignItems: 'center', gap: 9 }, propertyLinkText: { color: '#2056bc', fontSize: 14, fontWeight: '600' },
  dailyCard: { backgroundColor: '#fff', borderWidth: 1, borderColor: '#e5ebf2', borderRadius: 20, paddingHorizontal: 16 }, dailyLink: { paddingVertical: 15, minHeight: 76 }, dailyRow: { flexDirection: 'row', alignItems: 'center', gap: 12 }, dailyIcon: { width: 42, height: 42, borderRadius: 13, alignItems: 'center', justifyContent: 'center' }, dailyTitle: { color: '#102342', fontSize: 15, fontWeight: '600' }, divider: { borderTopWidth: 1, borderTopColor: '#edf1f6' }, contextNote: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 4 },
});
