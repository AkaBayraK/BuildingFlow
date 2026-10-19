import {PersonModelScreen} from '../person-authorization/PersonModelScreen';
import { Stack, type ErrorBoundaryProps } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { Pressable, Text, View } from 'react-native';
import { Screen, styles } from '../components/Screen';
import { AuthProvider, useAuth } from '../auth/AuthProvider';
import { LoginScreen } from '../auth/LoginScreen';
import { useEffect } from 'react';
import { verifyNativeCache } from '../offline/verify';
import { ConsentProvider } from '../consents/ConsentProvider';

export function ErrorBoundary({ retry }: ErrorBoundaryProps) {
  return <Screen title="Bir sorun oluştu"><Text style={styles.text}>Ekran açılamadı. Yeniden deneyebilirsin.</Text><Pressable accessibilityRole="button" style={styles.button} onPress={() => void retry()}><Text style={styles.buttonText}>Tekrar dene</Text></Pressable></Screen>;
}
export default function RootLayout() {
  useEffect(() => {
    if (__DEV__ && process.env.EXPO_PUBLIC_VERIFY_OFFLINE === '1') void verifyNativeCache().then(result => { if (result) console.info('[BuildingFlow Mobil][Faz 07] native kontrol', result); }).catch(() => console.warn('[BuildingFlow Mobil][Faz 07] native kontrol başarısız'));
  }, []);
  return <AuthProvider><SessionRoutes /></AuthProvider>;
}

function SessionRoutes(){
 const auth=useAuth();
 const content=auth.state==='signedIn'
  ?process.env.EXPO_PUBLIC_PERSON_AUTHORIZATION_ENABLED==='true'
   ?process.env.EXPO_PUBLIC_PERSON_SYNC_ENABLED==='true'?<ConsentProvider><PersonModelScreen/></ConsentProvider>:<PersonModelScreen/>
   :<ConsentProvider><Stack screenOptions={{headerShown:false}}/></ConsentProvider>
  :<LoginScreen/>;
 return <View style={{flex:1}}><StatusBar style={auth.state==='signedIn'?'light':'dark'}/><View style={{flex:1}} accessibilityElementsHidden={auth.privacyLocked} importantForAccessibility={auth.privacyLocked?'no-hide-descendants':'auto'}>{content}</View>{auth.privacyLocked&&<View style={{position:'absolute',top:0,bottom:0,left:0,right:0,backgroundColor:'#f6f8fc',justifyContent:'center',alignItems:'center'}}><Text>Güvenli ekran</Text></View>}</View>;
}
