import { Tabs } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';
import { theme } from '../../theme';

export default function TabLayout() {
  return <Tabs screenOptions={{ headerShown: false, tabBarActiveTintColor: theme.colors.primary, tabBarInactiveTintColor: theme.colors.muted, tabBarActiveBackgroundColor: '#f0f5fb', tabBarStyle: { minHeight: 68, backgroundColor: '#fff', borderTopColor: theme.colors.border, paddingTop: 7 }, tabBarLabelStyle: { fontSize: 10, fontWeight: '600', marginBottom: 5 }, tabBarItemStyle: { borderRadius: 12, marginHorizontal: 2 } }}>
    <Tabs.Screen name="index" options={{ title: 'Ana sayfa', tabBarIcon: ({ color }) => <Ionicons name="home-outline" size={23} color={color} accessible={false} aria-hidden={true} /> }} />
    <Tabs.Screen name="finance" options={{ title: 'Ödemeler', tabBarIcon: ({ color }) => <Ionicons name="wallet-outline" size={23} color={color} accessible={false} aria-hidden={true} /> }} />
    <Tabs.Screen name="announcements" options={{ title: 'Duyurular', tabBarIcon: ({ color }) => <Ionicons name="megaphone-outline" size={23} color={color} accessible={false} aria-hidden={true} /> }} />
    <Tabs.Screen name="requests" options={{ title: 'Talepler', tabBarIcon: ({ color }) => <Ionicons name="construct-outline" size={23} color={color} accessible={false} aria-hidden={true} /> }} />
    <Tabs.Screen name="profile" options={{ title: 'Profil', tabBarIcon: ({ color }) => <Ionicons name="person-outline" size={23} color={color} accessible={false} aria-hidden={true} /> }} />
  </Tabs>;
}
