import { useEffect, useState } from 'react';
import { Alert, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { clearBusinessSelection, loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import { BrandMark } from '@/components/BrandMark';
import { BusinessMemoryPanel } from '@/features/business-memory/BusinessMemoryPanel';
import { resetExpoDemoBusiness } from '@/features/business-hub/business-hub-api';
import { tokens } from '@/theme/tokens';

const EXPO_DEMO_TOKEN = 'atlas-expo-go-demo';

export default function SettingsScreen() {
  const [testResetEnabled, setTestResetEnabled] = useState(false);
  const [resetting, setResetting] = useState(false);

  useEffect(() => {
    void loadSession().then(session => setTestResetEnabled(__DEV__ && session?.accessToken === EXPO_DEMO_TOKEN));
  }, []);

  async function performReset() {
    if (resetting) return;
    setResetting(true);
    try {
      const session = await loadSession();
      if (!session || session.accessToken !== EXPO_DEMO_TOKEN) throw new Error('Test reset is not available for this session.');
      await resetExpoDemoBusiness(session.accessToken);
      await clearBusinessSelection();
      router.replace('/create-business');
    } catch {
      Alert.alert('Reset failed', 'Atlas could not reset the test business. Try again.');
    } finally {
      setResetting(false);
    }
  }

  function confirmReset() {
    Alert.alert(
      'Reset test business?',
      'This deletes the current Expo test business and its test data, then restarts business setup. Your demo sign-in stays available.',
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Reset and start again', style: 'destructive', onPress: () => void performReset() },
      ],
    );
  }

  return (
    <AtlasScreen hasTabBar contentStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.content}>
        <AtlasPressable accessibilityRole="button" accessibilityLabel="Back to Profile" onPress={() => router.back()} style={styles.back}>
          <Text style={styles.backText}>‹ Profile</Text>
        </AtlasPressable>

        <View style={styles.header}>
          <BrandMark size={44} />
          <Text style={styles.eyebrow}>SETTINGS</Text>
          <Text accessibilityRole="header" style={styles.title}>Control how Atlas works for you.</Text>
          <Text style={styles.subtitle}>Manage updates, support and the business memory Atlas uses while keeping your operating picture easy to review.</Text>
        </View>

        <AtlasPressable accessibilityRole="button" onPress={() => router.push('/notifications')} style={styles.card}>
          <Text style={styles.cardEyebrow}>UPDATES</Text>
          <Text style={styles.cardTitle}>Notifications</Text>
          <Text style={styles.body}>Review Atlas updates, unread items and notification preferences.</Text>
        </AtlasPressable>

        <AtlasPressable accessibilityRole="button" onPress={() => router.push('/feedback')} style={styles.card}>
          <Text style={styles.cardEyebrow}>HELP & FEEDBACK</Text>
          <Text style={styles.cardTitle}>Feedback & support</Text>
          <Text style={styles.body}>Rate Atlas guidance, report incorrect or unsafe information, share feedback, or request support.</Text>
        </AtlasPressable>

        <BusinessMemoryPanel />

        {testResetEnabled ? (
          <View style={styles.testCard}>
            <Text style={styles.testEyebrow}>EXPO TEST ONLY</Text>
            <Text style={styles.cardTitle}>Start business testing again</Text>
            <Text style={styles.body}>Remove the current demo business and all of its test data without signing out.</Text>
            <AtlasPressable
              accessibilityRole="button"
              accessibilityLabel="Reset test business"
              disabled={resetting}
              onPress={confirmReset}
              style={[styles.resetButton, resetting && styles.disabled]}
            >
              <Text style={styles.resetButtonText}>{resetting ? 'Resetting…' : 'Reset test business'}</Text>
            </AtlasPressable>
          </View>
        ) : null}
      </View>
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: tokens.color.surface },
  content: { gap: 16, maxWidth: 680, width: '100%' },
  back: { alignSelf: 'flex-start', justifyContent: 'center', minHeight: 44 },
  backText: { color: tokens.color.green, fontSize: 14, fontWeight: '800' },
  header: { gap: 7, marginBottom: 2 },
  eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.2, marginTop: 4 },
  title: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 31, fontWeight: '800', letterSpacing: -.45, lineHeight: 37 },
  subtitle: { color: tokens.color.muted, fontSize: 14.5, lineHeight: 22 },
  card: { backgroundColor: tokens.color.canvas, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: 7, padding: 20 },
  cardEyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  cardTitle: { color: tokens.color.greenDeep, fontSize: 18, fontWeight: '800' },
  body: { color: tokens.color.muted, fontSize: tokens.typography.body, lineHeight: 23 },
  testCard: { backgroundColor: tokens.color.canvas, borderColor: tokens.color.border, borderWidth: 1, borderRadius: tokens.radius.lg, padding: 20, gap: 10 },
  testEyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.1 },
  resetButton: { alignItems: 'center', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1.5, justifyContent: 'center', minHeight: 48, marginTop: 4, paddingHorizontal: 18 },
  resetButtonText: { color: tokens.color.greenDeep, fontSize: 14, fontWeight: '800' },
  disabled: { opacity: .55 },
});
