import * as AuthSession from 'expo-auth-session';
import { env } from '@/lib/env';

const redirectUri = AuthSession.makeRedirectUri({
  scheme: 'pesmobile',
  path: 'auth/callback',
});

export async function authorizeWithProvider(loginHint?: string): Promise<string> {
  const discovery = await AuthSession.fetchDiscoveryAsync(env.authIssuer);
  if (!discovery.authorizationEndpoint || !discovery.tokenEndpoint) {
    throw new Error('identity_provider_unavailable');
  }

  const extraParams: Record<string, string> = {};
  if (env.authAudience) extraParams.audience = env.authAudience;
  if (loginHint?.trim()) extraParams.login_hint = loginHint.trim();

  const request = await AuthSession.loadAsync(
    {
      clientId: env.authClientId,
      redirectUri,
      responseType: AuthSession.ResponseType.Code,
      scopes: ['openid', 'profile', 'email', 'offline_access'],
      usePKCE: true,
      extraParams: Object.keys(extraParams).length ? extraParams : undefined,
    },
    discovery,
  );

  const result = await request.promptAsync(discovery);
  if (result.type !== 'success' || !result.params.code || !request.codeVerifier) {
    throw new Error(result.type === 'cancel' || result.type === 'dismiss' ? 'sign_in_cancelled' : 'sign_in_failed');
  }

  const token = await AuthSession.exchangeCodeAsync(
    {
      clientId: env.authClientId,
      code: result.params.code,
      redirectUri,
      extraParams: { code_verifier: request.codeVerifier },
    },
    discovery,
  );

  if (!token.accessToken) throw new Error('access_token_missing');
  return token.accessToken;
}
