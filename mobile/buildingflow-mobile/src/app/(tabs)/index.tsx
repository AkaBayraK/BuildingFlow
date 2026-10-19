import {ManagerScreen} from '../../components/ManagerPanel';
import {managerRoles} from '../../manager/contracts';
import { StyleSheet, Text, View } from 'react-native';
import { Link } from 'expo-router';
import { Icon, Screen, styles } from '../../components/Screen';
import { useAuth } from '../../auth/AuthProvider';
import {OwnerPanel} from '../../components/OwnerPanel';
import { roleLabels } from '../../auth/contracts';
import { PersonCodeCard } from '../../components/PersonCodeCard';
export default function Home() {
  const { selected, session } = useAuth();
  if(session&&session.contexts.length===0)return <Screen title="Ana sayfa" home><View style={styles.emptyCard}><Icon name="person-circle-outline" size={36}/><Text style={styles.cardTitle}>Hesabın hazır</Text><Text style={styles.emptyDescription}>Şimdilik yalnız hesap bilgilerini görebilir ve güncelleyebilirsin. Kişi kodunu girince ev bilgilerin, ödemeler, duyurular ve talepler açılır.</Text></View><PersonCodeCard linked={false}/></Screen>;
  if(selected&&managerRoles.includes(selected.role))return <ManagerScreen section="overview"/>;
  if(selected&&['OWNER','TENANT'].includes(selected.role))return <Screen title="Ana sayfa" home><OwnerPanel key={selected.propertyId+selected.unitId+selected.role+session?.userId}/></Screen>;
  return <Screen title="Ana sayfa" home>
    <View style={s.propertyCard}>
      <View style={s.propertyVisual} accessible={false} aria-hidden={true}><View style={s.visualCircle} /><Icon name="business-outline" size={94} color="#adcddd" /><View style={s.visualLine} /><View style={s.visualLabel}><Icon name="home-outline" color="#d7eaf4" size={17} /><Text style={s.visualLabelText}>Bina yaşamın, tek yerde.</Text></View></View>
      <View style={s.propertyBody}><View style={styles.sectionHeading}><Text style={s.eyebrow}>{selected ? 'AKTİF MÜLKÜN' : 'MÜLK BAĞLANTISI'}</Text><Icon name="business-outline" size={18} color="#617187" /></View><Text style={s.propertyTitle}>{selected?.propertyName ?? 'Birlikte daha düzenli bir yaşam'}</Text><Text style={styles.muted}>{selected ? [selected.unitNumber ? `Daire ${selected.unitNumber}` : 'Bina yönetimi', roleLabels[selected.role]].join(' · ') : 'Hesabına henüz bir bina veya daire bağlanmamış.'}</Text><Link href="/profile" style={s.propertyLink}><Text style={s.propertyLinkText}>{selected ? 'Mülk ve rol seçimi' : 'Hesabımı görüntüle'}</Text><Icon name="arrow-forward" size={18} color="#2056bc" /></Link></View>
    </View>
    {selected && <View style={s.contextNote}><Icon name="layers-outline" color="#617187" size={18} /><Text style={[styles.muted, styles.flex]}>{session?.contexts.length ?? 0} yetkili mülk / rol bağlantın var. Hesabım bölümünden seçim yapabilirsin.</Text></View>}
  </Screen>;
}
const s = StyleSheet.create({
  propertyCard: { borderRadius: 21, overflow: 'hidden', backgroundColor: '#fff', borderWidth: 1, borderColor: '#e5ebf2' }, propertyVisual: { height: 158, backgroundColor: '#153d57', alignItems: 'center', justifyContent: 'center', overflow: 'hidden' }, visualCircle: { position: 'absolute', width: 170, height: 170, borderRadius: 85, backgroundColor: '#204c66', right: -34, top: -72 }, visualLine: { position: 'absolute', height: 1, backgroundColor: '#4b6b80', width: '75%', bottom: 37 }, visualLabel: { position: 'absolute', left: 16, bottom: 13, flexDirection: 'row', gap: 7, alignItems: 'center' }, visualLabelText: { color: '#d7eaf4', fontSize: 12 },
  propertyBody: { padding: 18, gap: 8 }, eyebrow: { color: '#617187', fontSize: 10, fontWeight: '700', letterSpacing: 1.4 }, propertyTitle: { color: '#102342', fontSize: 21, fontWeight: '700', lineHeight: 28, letterSpacing: -0.5 }, propertyLink: { minHeight: 48, paddingVertical: 13, flexDirection: 'row', alignItems: 'center', gap: 9 }, propertyLinkText: { color: '#2056bc', fontSize: 14, fontWeight: '600' },
  dailyTitle: { color: '#102342', fontSize: 15, fontWeight: '600' }, contextNote: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 4 },
});
