import * as SecureStore from 'expo-secure-store';
import * as Crypto from 'expo-crypto';
import {createDeviceAccess} from './device-policy';
const normal={keychainAccessible:SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY};
const protectedOptions={...normal,requireAuthentication:true,authenticationPrompt:'BuildingFlow çevrimdışı erişimini doğrula'};
export const deviceAccess=createDeviceAccess({
 get:(key,authenticated=false)=>SecureStore.getItemAsync(key,authenticated?protectedOptions:normal),
 set:(key,value,authenticated=false)=>SecureStore.setItemAsync(key,value,authenticated?protectedOptions:normal),
 remove:key=>SecureStore.deleteItemAsync(key),biometrics:()=>SecureStore.canUseBiometricAuthentication(),
 random:()=>Array.from(Crypto.getRandomBytes(32),b=>b.toString(16).padStart(2,'0')).join(''),now:Date.now
});
