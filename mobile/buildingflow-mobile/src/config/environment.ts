import { validateApiUrl } from './validate';
export function getEnvironment() {
  return { apiUrl: validateApiUrl(process.env.EXPO_PUBLIC_API_URL, __DEV__) };
}
