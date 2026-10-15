import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
const key = 'buildingflow.mobile.refresh.v1';
// Web sessions stay in memory. Never persist credentials to browser storage.
export async function readRefresh() { return Platform.OS === 'web' ? null : SecureStore.getItemAsync(key); }
export async function writeRefresh(value: string | null) {
  if (Platform.OS === 'web') return;
  if (value === null) await SecureStore.deleteItemAsync(key);
  else await SecureStore.setItemAsync(key, value, { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY });
}
