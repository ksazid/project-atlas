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
        setError('Sign-in is temporarily unavailable. Please try again.');
      } else {
        setError('We could not sign you in securely. Please try again.');
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.eyebrow}>ATLAS</Text>
      <Text accessibilityRole="header" style={styles.title}>Welcome back</Text>
      <Text style={styles.body}>Sign in to continue to your business intelligence workspace.</Text>
      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Sign in"
        disabled={busy}
        onPress={signIn}
        style={({ pressed }) => [styles.button, pressed && styles.buttonPressed, busy && styles.buttonDisabled]}
      >
        {busy ? <ActivityIndicator color="#FFFFFF" /> : <Text style={styles.buttonText}>Sign in</Text>}
      </Pressable>
      <Text style={styles.security}>Your authentication remains provider-managed; Atlas does not store your password in the app.</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 24, gap: 18, backgroundColor: '#F8FAFC' },
  eyebrow: { fontSize: 12, fontWeight: '800', letterSpacing: 2, color: '#64748B' },
  title: { fontSize: 34, fontWeight: '800', color: '#0F172A' },
  body: { fontSize: 16, lineHeight: 24, color: '#475569' },
  error: { fontSize: 15, lineHeight: 22, fontWeight: '600', color: '#991B1B' },
  button: { minHeight: 52, borderRadius: 13, alignItems: 'center', justifyContent: 'center', backgroundColor: '#0F172A' },
  buttonPressed: { opacity: 0.88 },
  buttonDisabled: { opacity: 0.65 },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '800' },
  security: { fontSize: 12, lineHeight: 18, color: '#64748B', textAlign: 'center' },
});
