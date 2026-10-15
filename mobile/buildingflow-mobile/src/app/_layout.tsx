import { Stack, type ErrorBoundaryProps } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { Pressable, Text } from 'react-native';
import { Screen, styles } from '../components/Screen';
import { AuthProvider, useAuth } from '../auth/AuthProvider';
import { LoginScreen } from '../auth/LoginScreen';
import { useEffect } from 'react';
import { verifyNativeCache } from '../offline/verify';

export function ErrorBoundary({ retry }: ErrorBoundaryProps) {
  return <Screen title="Bir sorun oluştu"><Text style={styles.text}>Ekran açılamadı. Yeniden deneyebilirsin.</Text><Pressable accessibilityRole="button" style={styles.button} onPress={() => void retry()}><Text style={styles.buttonText}>Tekrar dene</Text></Pressable></Screen>;
}
export default function RootLayout() {
  useEffect(() => {
    if (__DEV__ && process.env.EXPO_PUBLIC_VERIFY_OFFLINE === '1') void verifyNativeCache().then(result => { if (result) console.info('[BuildingFlow Mobil][Faz 07] native kontrol', result); }).catch(() => console.warn('[BuildingFlow Mobil][Faz 07] native kontrol başarısız'));
  }, []);
  return <AuthProvider><SessionRoutes /></AuthProvider>;
}

function SessionRoutes() { const auth = useAuth(); return <><StatusBar style={auth.state === 'signedIn' ? 'light' : 'dark'} />{auth.state === 'signedIn' ? <Stack screenOptions={{ headerShown: false }} /> : <LoginScreen />}</>; }
