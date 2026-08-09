import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { authorizeWithProvider } from '@/auth/provider';
import { saveSession } from '@/auth/session';

const GREEN = '#27A968';

export default function SignInScreen() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function signIn() {
    setBusy(true); setError(null);
    try {
      const accessToken = await authorizeWithProvider();
      await saveSession({ accessToken });
      router.replace('/create-business');
    } catch (reason) {
      const code = reason instanceof Error ? reason.message : 'sign_in_failed';
      setError(code === 'sign_in_cancelled' ? 'Sign-in was cancelled. No account changes were made.' : code === 'identity_provider_unavailable' ? 'Sign-in is temporarily unavailable. Please try again.' : 'We could not sign you in securely. Please try again.');
    } finally { setBusy(false); }
  }

  return (
    <View style={styles.container}>
      <View style={styles.glow} />
      <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={() => router.back()} style={styles.back}><Text style={styles.backText}>←</Text></Pressable>
      <View style={styles.securityBadge}><Text style={styles.shield}>✓</Text></View>

      <View style={styles.content}>
        <Text accessibilityRole="header" style={styles.title}>Welcome back 👋</Text>
        <Text style={styles.body}>Sign in securely to continue to Atlas.</Text>

        <View style={styles.providerCard}>
          <View style={styles.providerIcon}><Text style={styles.providerIconText}>A</Text></View>
          <View style={styles.providerCopy}><Text style={styles.providerLabel}>SECURE SIGN IN</Text><Text style={styles.providerTitle}>Continue with your identity provider</Text></View>
        </View>

        {error ? <View style={styles.errorCard}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}

        <Pressable accessibilityRole="button" accessibilityLabel="Sign in" disabled={busy} onPress={signIn} style={({ pressed }) => [styles.button, pressed && styles.pressed, busy && styles.disabled]}>
          {busy ? <ActivityIndicator color="#FFFFFF" /> : <><Text style={styles.buttonText}>Sign in</Text><Text style={styles.arrow}>→</Text></>}
        </Pressable>

        <View style={styles.divider}><View style={styles.line}/><Text style={styles.or}>secure</Text><View style={styles.line}/></View>
        <View style={styles.infoCard}><Text style={styles.lock}>▣</Text><Text style={styles.info}>Authentication is provider-managed. Atlas never stores your password in the mobile app.</Text></View>
      </View>
      <Text style={styles.footer}>New to Atlas? Your business setup starts immediately after sign in.</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: 24, paddingTop: 58, paddingBottom: 28, backgroundColor: '#FBFCFB', overflow: 'hidden' },
  glow: { position: 'absolute', width: 310, height: 310, borderRadius: 155, right: -120, top: -105, backgroundColor: '#E4F7EB', opacity: .8 },
  back: { width: 42, height: 42, borderRadius: 13, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E6ECE8', alignItems: 'center', justifyContent: 'center', shadowColor: '#163B29', shadowOpacity: .05, shadowRadius: 10, elevation: 2 },
  backText: { fontSize: 21, color: '#1D2A23', fontWeight: '800' },
  securityBadge: { position: 'absolute', right: 28, top: 74, width: 56, height: 56, borderRadius: 28, backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', shadowColor: '#1C7044', shadowOpacity: .08, shadowRadius: 16, elevation: 3 },
  shield: { width: 35, height: 35, borderRadius: 18, textAlign: 'center', textAlignVertical: 'center', backgroundColor: '#DFF5E8', color: GREEN, fontSize: 19, fontWeight: '900' },
  content: { marginTop: 115, gap: 18 },
  title: { fontSize: 34, lineHeight: 41, fontWeight: '900', letterSpacing: -.7, color: '#101827' },
  body: { fontSize: 15, lineHeight: 22, color: '#56625B', marginBottom: 12 },
  providerCard: { minHeight: 88, borderRadius: 17, padding: 16, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E7ECE9', flexDirection: 'row', alignItems: 'center', gap: 14, shadowColor: '#173B2A', shadowOpacity: .04, shadowRadius: 12, elevation: 2 },
  providerIcon: { width: 48, height: 48, borderRadius: 15, backgroundColor: '#E2F6EA', alignItems: 'center', justifyContent: 'center' },
  providerIconText: { fontSize: 24, fontWeight: '900', color: GREEN },
  providerCopy: { flex: 1, gap: 4 }, providerLabel: { fontSize: 9, fontWeight: '900', letterSpacing: 1.2, color: GREEN }, providerTitle: { fontSize: 14, lineHeight: 20, fontWeight: '800', color: '#1C2922' },
  errorCard: { padding: 13, borderRadius: 13, backgroundColor: '#FDECEC' }, error: { color: '#A1251B', fontSize: 13, lineHeight: 19, fontWeight: '700' },
  button: { minHeight: 56, borderRadius: 13, backgroundColor: GREEN, alignItems: 'center', justifyContent: 'center', flexDirection: 'row' },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '800' }, arrow: { position: 'absolute', right: 20, color: '#FFFFFF', fontSize: 23 }, pressed: { opacity: .9, transform: [{ scale: .988 }] }, disabled: { opacity: .55 },
  divider: { flexDirection: 'row', alignItems: 'center', gap: 12, marginVertical: 2 }, line: { height: 1, backgroundColor: '#E6EBE8', flex: 1 }, or: { color: '#8A958F', fontSize: 11 },
  infoCard: { padding: 16, borderRadius: 15, backgroundColor: '#F4FAF6', flexDirection: 'row', gap: 12, alignItems: 'flex-start' }, lock: { color: GREEN, fontSize: 16, fontWeight: '900' }, info: { flex: 1, color: '#66736C', fontSize: 12, lineHeight: 18 },
  footer: { marginTop: 'auto', textAlign: 'center', color: '#77837C', fontSize: 12, lineHeight: 18, paddingHorizontal: 18 },
});
