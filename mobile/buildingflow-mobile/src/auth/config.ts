import { Platform } from 'react-native';
import { validateApiUrl } from '../config/validate';
export function authConfig() {
  try {
    const issuer = validateApiUrl(Platform.OS === 'web' ? process.env.EXPO_PUBLIC_AUTH_WEB_ISSUER : process.env.EXPO_PUBLIC_AUTH_ISSUER, __DEV__);
    const api = validateApiUrl(Platform.OS === 'web' ? process.env.EXPO_PUBLIC_API_WEB_URL : process.env.EXPO_PUBLIC_API_URL, __DEV__);
    const clientId = process.env.EXPO_PUBLIC_AUTH_CLIENT_ID;
    if (!issuer || !api || !clientId || !new URL(issuer).pathname.endsWith('/realms/buildingflow')) return null;
    return { issuer, api, clientId };
  } catch { return null; }
}
