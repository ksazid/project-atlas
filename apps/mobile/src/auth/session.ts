import * as SecureStore from 'expo-secure-store';

const ACCESS_TOKEN_KEY = 'atlas.access-token';
const BUSINESS_ID_KEY = 'atlas.business-id';

export type Session = { accessToken: string; businessId?: string };

export async function loadSession(): Promise<Session | null> {
  const accessToken = await SecureStore.getItemAsync(ACCESS_TOKEN_KEY);
  if (!accessToken) return null;
  const businessId = await SecureStore.getItemAsync(BUSINESS_ID_KEY);
  return { accessToken, businessId: businessId ?? undefined };
}

export async function saveSession(session: Session): Promise<void> {
  await SecureStore.setItemAsync(ACCESS_TOKEN_KEY, session.accessToken, {
    keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
  });
  if (session.businessId) await SecureStore.setItemAsync(BUSINESS_ID_KEY, session.businessId);
}

export async function clearSession(): Promise<void> {
  await Promise.all([
    SecureStore.deleteItemAsync(ACCESS_TOKEN_KEY),
    SecureStore.deleteItemAsync(BUSINESS_ID_KEY),
  ]);
}
