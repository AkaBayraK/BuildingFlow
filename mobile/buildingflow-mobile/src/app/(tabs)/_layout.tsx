import { Tabs } from 'expo-router';
import { StyleSheet, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { theme } from '../../theme';
import { useAuth } from '../../auth/AuthProvider';

const navy = '#062641';
type IconName = keyof typeof Ionicons.glyphMap;
// The selected tab shows a filled icon on a navy pill; the bar itself stays light.
function TabIcon({ name, focused }: { name: IconName; focused: boolean }) {
  const shown = (focused ? name.replace('-outline', '') : name) as IconName;
  return <View style={focused ? s.pillActive : s.pill}><Ionicons name={shown} size={21} color={focused ? '#fff' : theme.colors.muted} accessible={false} aria-hidden={true} /></View>;
}
const icon = (name: IconName) => function TabBarIcon({ focused }: { focused: boolean }) { return <TabIcon name={name} focused={focused} />; };

export default function TabLayout() {
  // Accounts without a verified person link only reach their own account; the server returns no property data either.
  const linked = (useAuth().session?.contexts.length ?? 0) > 0;
  return <Tabs screenOptions={{ headerShown: false, tabBarActiveTintColor: navy, tabBarInactiveTintColor: theme.colors.muted, tabBarStyle: s.bar, tabBarLabelStyle: s.label, tabBarItemStyle: s.item }}>
    <Tabs.Screen name="index" options={{ title: 'Ana sayfa', tabBarIcon: icon('home-outline') }} />
    <Tabs.Screen name="finance" options={{ href: linked ? undefined : null, title: 'Ödemeler', tabBarIcon: icon('wallet-outline') }} />
    <Tabs.Screen name="announcements" options={{ href: linked ? undefined : null, title: 'Bildirimler', tabBarIcon: icon('megaphone-outline') }} />
    <Tabs.Screen name="requests" options={{ href: linked ? undefined : null, title: 'Talepler', tabBarIcon: icon('construct-outline') }} />
    <Tabs.Screen name="profile" options={{ title: 'Profil', tabBarIcon: icon('person-outline') }} />
  </Tabs>;
}
const pill = { width: 52, height: 30, borderRadius: 15, alignItems: 'center', justifyContent: 'center' } as const;
const s = StyleSheet.create({
  bar: { minHeight: 74, backgroundColor: '#fff', borderTopColor: theme.colors.border, paddingTop: 8 },
  item: { paddingTop: 2 },
  label: { fontSize: 11, fontWeight: '700', marginTop: 4, marginBottom: 4 },
  pill,
  pillActive: { ...pill, backgroundColor: navy },
});
