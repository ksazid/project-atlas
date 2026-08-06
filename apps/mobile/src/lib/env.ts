const DEFAULT_API_URL = 'http://localhost:5000';

function requireValidUrl(name: string, value: string | undefined, fallback?: string): string {
  const candidate = value?.trim() || fallback;
  if (!candidate) throw new Error(`${name} is required.`);
  try {
    return new URL(candidate).toString().replace(/\/$/, '');
  } catch {
    throw new Error(`${name} must be a valid absolute URL.`);
  }
}

function requireValue(name: string, value: string | undefined): string {
  const candidate = value?.trim();
  if (!candidate) throw new Error(`${name} is required.`);
  return candidate;
}

export const env = Object.freeze({
  apiUrl: requireValidUrl('EXPO_PUBLIC_API_URL', process.env.EXPO_PUBLIC_API_URL, DEFAULT_API_URL),
  authIssuer: requireValidUrl('EXPO_PUBLIC_AUTH_ISSUER', process.env.EXPO_PUBLIC_AUTH_ISSUER),
  authClientId: requireValue('EXPO_PUBLIC_AUTH_CLIENT_ID', process.env.EXPO_PUBLIC_AUTH_CLIENT_ID),
  authAudience: process.env.EXPO_PUBLIC_AUTH_AUDIENCE?.trim() || undefined,
});
