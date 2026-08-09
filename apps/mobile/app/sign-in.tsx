import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { authorizeWithProvider } from '@/auth/provider';
import { saveSession } from '@/auth/session';

const GREEN = '#26A864';
const INK = '#101828';

export default function SignInScreen() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loginHint, setLoginHint] = useState('');

  async function signIn() {
    setBusy(true); setError(null);
    try {
      const accessToken = await authorizeWithProvider(loginHint);
      await saveSession({ accessToken });
      router.replace('/create-business');
    } catch (reason) {
      const code = reason instanceof Error ? reason.message : 'sign_in_failed';
      setError(code === 'sign_in_cancelled' ? 'Sign-in was cancelled. No account changes were made.' : code === 'identity_provider_unavailable' ? 'Sign-in is temporarily unavailable. Please try again.' : 'We could not sign you in securely. Please try again.');
    } finally { setBusy(false); }
  }

  return (
    <View style={styles.container}>
      <View style={styles.mintGlow}/>
      <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={() => router.back()} style={styles.back}><Text style={styles.backText}>←</Text></Pressable>
      <View style={styles.shieldBubble}><View style={styles.shieldInner}><Text style={styles.shieldText}>✓</Text></View></View>

      <View style={styles.content}>
        <Text accessibilityRole="header" style={styles.title}>Welcome back 👋</Text>
        <Text style={styles.subtitle}>Sign in to continue to Atlas</Text>

        <View style={styles.fieldBlock}>
          <Text style={styles.label}>Email or phone</Text>
          <View style={styles.inputShell}><Text style={styles.leadingIcon}>✉</Text><TextInput accessibilityLabel="Email or phone" autoCapitalize="none" autoCorrect={false} keyboardType="email-address" value={loginHint} onChangeText={setLoginHint} placeholder="you@example.com" placeholderTextColor="#9AA3AA" style={styles.input}/></View>
        </View>

        <View style={styles.fieldBlock}>
          <Text style={styles.label}>Password</Text>
          <View style={styles.inputShell}><Text style={styles.leadingIcon}>▣</Text><Text style={styles.providerManaged}>Handled securely by your identity provider</Text><Text style={styles.eye}>◉</Text></View>
          <Text style={styles.forgot}>Forgot password?</Text>
        </View>

        {error ? <View style={styles.errorBox}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}

        <Pressable accessibilityRole="button" accessibilityLabel="Sign in" disabled={busy} onPress={signIn} style={({ pressed }) => [styles.primary, pressed && styles.pressed, busy && styles.disabled]}>
          {busy ? <ActivityIndicator color="#FFFFFF"/> : <><Text style={styles.primaryText}>Sign in</Text><Text style={styles.arrow}>→</Text></>}
        </Pressable>

        <View style={styles.orRow}><View style={styles.rule}/><Text style={styles.orText}>or</Text><View style={styles.rule}/></View>

        <Pressable disabled={busy} onPress={signIn} style={({ pressed }) => [styles.providerButton, pressed && styles.pressed]}>
          <View style={styles.providerLogo}><Text style={styles.providerLogoText}>A</Text></View><Text style={styles.providerButtonText}>Continue with secure provider</Text>
        </Pressable>
      </View>

      <Text style={styles.footer}>Your identity provider controls available sign-in methods and password recovery.</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: 26, paddingTop: 58, paddingBottom: 30, backgroundColor: '#FCFDFC', overflow: 'hidden' },
  mintGlow: { position: 'absolute', width: 255, height: 255, borderRadius: 128, right: -85, top: -88, backgroundColor: '#E8F8EE', opacity: 0.92 },
  back: { width: 40, height: 40, borderRadius: 12, borderWidth: 1, borderColor: '#E6ECE8', backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', shadowColor: '#173B2A', shadowOpacity: 0.04, shadowRadius: 8, elevation: 1 },
  backText: { fontSize: 20, fontWeight: '800', color: '#24312A' },
  shieldBubble: { position: 'absolute', right: 28, top: 75, width: 54, height: 54, borderRadius: 27, backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', shadowColor: '#206941', shadowOpacity: 0.08, shadowRadius: 12, elevation: 2 },
  shieldInner: { width: 35, height: 35, borderRadius: 18, backgroundColor: '#DFF5E8', alignItems: 'center', justifyContent: 'center' },
  shieldText: { fontSize: 19, fontWeight: '900', color: GREEN },
  content: { marginTop: 108 },
  title: { fontSize: 31, lineHeight: 37, fontWeight: '900', letterSpacing: -0.6, color: INK },
  subtitle: { marginTop: 7, marginBottom: 31, fontSize: 13.5, color: '#3F4A44' },
  fieldBlock: { marginBottom: 18 },
  label: { marginBottom: 8, fontSize: 12.5, fontWeight: '800', color: '#1F2B25' },
  inputShell: { minHeight: 54, borderRadius: 11, borderWidth: 1, borderColor: '#E1E7E3', backgroundColor: '#FFFFFF', flexDirection: 'row', alignItems: 'center', paddingHorizontal: 14, shadowColor: '#173B2A', shadowOpacity: 0.03, shadowRadius: 6, elevation: 1 },
  leadingIcon: { width: 24, fontSize: 15, color: '#77837D' },
  input: { flex: 1, fontSize: 13.5, color: '#25332B' },
  providerManaged: { flex: 1, fontSize: 11.5, color: '#8A948F' },
  eye: { fontSize: 15, color: '#8B9690' },
  forgot: { alignSelf: 'flex-end', marginTop: 9, fontSize: 11.5, fontWeight: '700', color: GREEN },
  errorBox: { marginBottom: 12, padding: 11, borderRadius: 10, backgroundColor: '#FDECEC' },
  error: { fontSize: 12, lineHeight: 18, fontWeight: '700', color: '#A1251B' },
  primary: { minHeight: 56, borderRadius: 11, backgroundColor: GREEN, alignItems: 'center', justifyContent: 'center', flexDirection: 'row', shadowColor: '#287348', shadowOpacity: 0.10, shadowRadius: 10, shadowOffset: { width: 0, height: 5 }, elevation: 3 },
  primaryText: { color: '#FFFFFF', fontSize: 15.5, fontWeight: '800' },
  arrow: { position: 'absolute', right: 19, color: '#FFFFFF', fontSize: 22 },
  orRow: { marginVertical: 22, flexDirection: 'row', alignItems: 'center', gap: 12 },
  rule: { flex: 1, height: 1, backgroundColor: '#E6EBE8' },
  orText: { fontSize: 11.5, color: '#7D8881' },
  providerButton: { minHeight: 54, borderRadius: 11, borderWidth: 1, borderColor: '#E3E8E5', backgroundColor: '#FFFFFF', flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 12 },
  providerLogo: { width: 27, height: 27, borderRadius: 14, backgroundColor: '#E2F6EA', alignItems: 'center', justifyContent: 'center' },
  providerLogoText: { fontSize: 15, fontWeight: '900', color: GREEN },
  providerButtonText: { fontSize: 13, fontWeight: '700', color: '#26322B' },
  footer: { marginTop: 'auto', textAlign: 'center', paddingHorizontal: 18, fontSize: 10.5, lineHeight: 16, color: '#7A867F' },
  pressed: { opacity: 0.92, transform: [{ scale: 0.99 }] },
  disabled: { opacity: 0.55 },
});
