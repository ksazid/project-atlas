import { useEffect, useState } from 'react';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { clearBusinessSelection, loadSession } from '@/auth/session';
import { AtlasScreen } from '@/components/AtlasScreen';
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
    <AtlasScreen hasTabBar contentStyle={styles.container}>
      <Text accessibilityRole="header" style={styles.title}>Settings</Text>
      <Pressable accessibilityRole="button" onPress={() => router.push('/notifications')} style={({ pressed }) => [styles.card, pressed && styles.pressed]}>
        <Text style={styles.cardTitle}>Notifications</Text>
        <Text style={styles.body}>Review Atlas updates, unread items and notification preferences.</Text>
      </Pressable>
      <BusinessMemoryPanel />
      {testResetEnabled ? (
        <View style={styles.testCard}>
          <Text style={styles.testEyebrow}>EXPO TEST ONLY</Text>
          <Text style={styles.cardTitle}>Start business testing again</Text>
          <Text style={styles.body}>Remove the current demo business and all of its test data without signing out.</Text>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Reset test business"
            disabled={resetting}
            onPress={confirmReset}
            style={({ pressed }) => [styles.resetButton, pressed && !resetting && styles.pressed, resetting && styles.disabled]}
          >
            <Text style={styles.resetButtonText}>{resetting ? 'Resetting…' : 'Reset test business'}</Text>
          </Pressable>
        </View>
      ) : null}
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  container: { gap: tokens.spacing.lg },
  title: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 31, fontWeight: '800' },
  card: { borderColor: '#dce5df', borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.xs },
  cardTitle: { color: tokens.color.greenDeep, fontSize: 18, fontWeight: '700' },
  body: { color: tokens.color.muted, fontSize: tokens.typography.body, lineHeight: 24 },
  testCard: { backgroundColor: '#f4f8f5', borderColor: '#d7e4dc', borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: 10 },
  testEyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.1 },
  resetButton: { alignItems: 'center', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1.5, justifyContent: 'center', minHeight: 48, marginTop: 4, paddingHorizontal: 18 },
  resetButtonText: { color: tokens.color.greenDeep, fontSize: 14, fontWeight: '800' },
  pressed: { opacity: .86, transform: [{ scale: .99 }] },
  disabled: { opacity: .55 },
});