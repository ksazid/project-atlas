import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { saveSession } from '@/auth/session';

export default function SignInScreen() {
  const [token, setToken] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function continueSecurely() {
    const value = token.trim();
    if (!value) {
      setError('A valid identity-provider access token is required.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await saveSession({ accessToken: value });
      router.replace('/create-business');
    } catch {
      setError('Secure session storage is unavailable. Please try again.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text accessibilityRole="header" style={styles.title}>Welcome to Atlas</Text>
      <Text style={styles.body}>Sign in through the configured identity provider. This runtime-disabled pilot shell accepts the resulting access token; no password or client secret is stored by Atlas.</Text>
      <TextInput
        accessibilityLabel="Identity provider access token"
        autoCapitalize="none"
        autoCorrect={false}
        multiline
        onChangeText={setToken}
        placeholder="Paste pilot access token"
        secureTextEntry
        style={styles.input}
        value={token}
      />
      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
      <Pressable accessibilityRole="button" disabled={busy} onPress={continueSecurely} style={styles.button}>
        {busy ? <ActivityIndicator /> : <Text style={styles.buttonText}>Continue securely</Text>}
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 24, gap: 16 },
  title: { fontSize: 32, fontWeight: '700' },
  body: { fontSize: 16, lineHeight: 24 },
  input: { minHeight: 96, borderWidth: 1, borderRadius: 12, padding: 14, textAlignVertical: 'top' },
  error: { fontSize: 15, fontWeight: '600' },
  button: { minHeight: 48, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: '#111827' },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '700' },
});
