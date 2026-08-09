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
      <View style={styles.topRow}>
        <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={() => router.back()} style={styles.backButton}>
          <Text style={styles.backIcon}>←</Text>
        </Pressable>
        <View style={styles.brandGroup}>
          <View style={styles.brandMark}><Text style={styles.brandMarkText}>✦</Text></View>
          <Text style={styles.brand}>ATLAS</Text>
        </View>
      </View>

      <View style={styles.content}>
        <View style={styles.intelligenceBadge}>
          <Text style={styles.intelligenceIcon}>✦</Text>
          <Text style={styles.intelligenceText}>BUSINESS INTELLIGENCE</Text>
        </View>
        <Text accessibilityRole="header" style={styles.title}>Welcome back.</Text>
        <Text style={styles.body}>Continue to the workspace that turns business signals into focused action.</Text>

        <View style={styles.previewCard}>
          <View style={styles.previewTopRow}>
            <View style={styles.previewIcon}><Text style={styles.previewIconText}>◎</Text></View>
            <View style={styles.previewCopy}>
              <Text style={styles.previewLabel}>ATLAS WORKSPACE</Text>
              <Text style={styles.previewTitle}>Your priorities stay focused.</Text>
            </View>
          </View>
          <View style={styles.previewDivider} />
          <View style={styles.previewMetricRow}>
            <View><Text style={styles.previewMetricValue}>01</Text><Text style={styles.previewMetricLabel}>Top focus</Text></View>
            <View><Text style={styles.previewMetricValue}>24/7</Text><Text style={styles.previewMetricLabel}>Learning loop</Text></View>
            <View><Text style={styles.previewMetricValue}>✓</Text><Text style={styles.previewMetricLabel}>Measured</Text></View>
          </View>
        </View>

        {error ? <View style={styles.errorCard}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}

        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Sign in"
          disabled={busy}
          onPress={signIn}
          style={({ pressed }) => [styles.button, pressed && styles.buttonPressed, busy && styles.buttonDisabled]}
        >
          {busy ? <ActivityIndicator color="#FFFFFF" /> : <><Text style={styles.buttonText}>Continue securely</Text><Text style={styles.buttonIcon}>→</Text></>}
        </Pressable>

        <View style={styles.securityRow}>
          <View style={styles.securityIcon}><Text style={styles.securityIconText}>✓</Text></View>
          <Text style={styles.security}>Authentication stays provider-managed. Atlas never stores your password in the app.</Text>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: 20, paddingTop: 56, paddingBottom: 28, backgroundColor: '#F5F6FA' },
  topRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  backButton: { width: 42, height: 42, borderRadius: 14, alignItems: 'center', justifyContent: 'center', backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E7E9EF' },
  backIcon: { fontSize: 20, fontWeight: '800', color: '#20242E' },
  brandGroup: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  brandMark: { width: 30, height: 30, borderRadius: 10, alignItems: 'center', justifyContent: 'center', backgroundColor: '#111827' },
  brandMarkText: { color: '#C4B5FD', fontSize: 15, fontWeight: '900' },
  brand: { fontSize: 13, fontWeight: '900', letterSpacing: 2.4, color: '#111827' },
  content: { flex: 1, justifyContent: 'center', gap: 18 },
  intelligenceBadge: { alignSelf: 'flex-start', flexDirection: 'row', alignItems: 'center', gap: 8, paddingHorizontal: 11, paddingVertical: 7, borderRadius: 999, backgroundColor: '#EEEAFD' },
  intelligenceIcon: { color: '#6D28D9', fontSize: 12 },
  intelligenceText: { color: '#6D28D9', fontSize: 9, fontWeight: '900', letterSpacing: 1.1 },
  title: { fontSize: 42, lineHeight: 46, letterSpacing: -1.5, fontWeight: '900', color: '#151821' },
  body: { maxWidth: 330, fontSize: 16, lineHeight: 24, color: '#737A89' },
  previewCard: { marginTop: 4, padding: 18, borderRadius: 24, backgroundColor: '#171922', overflow: 'hidden' },
  previewTopRow: { flexDirection: 'row', alignItems: 'center', gap: 13 },
  previewIcon: { width: 44, height: 44, borderRadius: 15, alignItems: 'center', justifyContent: 'center', backgroundColor: '#EEEAFD' },
  previewIconText: { color: '#6D28D9', fontSize: 19, fontWeight: '900' },
  previewCopy: { flex: 1, gap: 3 },
  previewLabel: { fontSize: 9, fontWeight: '900', letterSpacing: 1.2, color: '#A78BFA' },
  previewTitle: { fontSize: 16, fontWeight: '800', color: '#FFFFFF' },
  previewDivider: { height: 1, backgroundColor: 'rgba(255,255,255,0.10)', marginVertical: 16 },
  previewMetricRow: { flexDirection: 'row', justifyContent: 'space-between' },
  previewMetricValue: { fontSize: 18, fontWeight: '900', color: '#FFFFFF' },
  previewMetricLabel: { marginTop: 3, fontSize: 10, color: '#8F96A5' },
  errorCard: { padding: 13, borderRadius: 14, backgroundColor: '#FEECEC' },
  error: { fontSize: 13, lineHeight: 19, fontWeight: '700', color: '#991B1B' },
  button: { minHeight: 58, borderRadius: 18, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 12, backgroundColor: '#6D28D9' },
  buttonPressed: { transform: [{ scale: 0.985 }], opacity: 0.94 },
  buttonDisabled: { opacity: 0.6 },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '900' },
  buttonIcon: { color: '#FFFFFF', fontSize: 20, fontWeight: '900' },
  securityRow: { flexDirection: 'row', gap: 9, alignItems: 'flex-start', paddingHorizontal: 4 },
  securityIcon: { width: 20, height: 20, borderRadius: 7, alignItems: 'center', justifyContent: 'center', backgroundColor: '#E9F8F0' },
  securityIconText: { fontSize: 11, fontWeight: '900', color: '#198754' },
  security: { flex: 1, fontSize: 11, lineHeight: 17, color: '#8A91A0' },
});
