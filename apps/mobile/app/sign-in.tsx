import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { authorizeWithProvider } from '@/auth/provider';
import { saveSession } from '@/auth/session';

export default function SignInScreen() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function signIn() {
    setBusy(true);
    setError(null);
    try {
      const accessToken = await authorizeWithProvider();
      await saveSession({ accessToken });
      router.replace('/create-business');
    } catch (reason) {
      const code = reason instanceof Error ? reason.message : 'sign_in_failed';
      if (code === 'sign_in_cancelled') {
        setError('Sign-in was cancelled. No account changes were made.');
      } else if (code === 'identity_provider_unavailable') {
        setError('The identity provider is temporarily unavailable. Please try again.');
      } else {
        setError('We could not sign you in securely. Please try again.');
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text accessibilityRole="header" style={styles.title}>Welcome to Atlas</Text>
      <Text style={styles.body}>
        Sign in securely through the configured identity provider. Atlas uses Authorization Code with PKCE and never stores a client secret in the mobile app.
      </Text>
      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Sign in securely"
        disabled={busy}
        onPress={signIn}
        style={({ pressed }) => [styles.button, pressed && styles.buttonPressed, busy && styles.buttonDisabled]}
      >
        {busy ? <ActivityIndicator color="#FFFFFF" /> : <Text style={styles.buttonText}>Sign in securely</Text>}
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 24, gap: 18 },
  title: { fontSize: 32, fontWeight: '700' },
  body: { fontSize: 16, lineHeight: 24 },
  error: { fontSize: 15, lineHeight: 22, fontWeight: '600' },
  button: { minHeight: 48, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: '#111827' },
  buttonPressed: { opacity: 0.88 },
  buttonDisabled: { opacity: 0.65 },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '700' },
});
