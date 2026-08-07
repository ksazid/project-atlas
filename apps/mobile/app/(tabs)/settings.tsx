import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { router } from 'expo-router';
import { BusinessMemoryPanel } from '@/features/business-memory/BusinessMemoryPanel';
import { tokens } from '@/theme/tokens';

export default function SettingsScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text accessibilityRole="header" style={styles.title}>Settings</Text>
      <Pressable accessibilityRole="button" onPress={() => router.push('/notifications')} style={styles.card}>
        <Text style={styles.cardTitle}>Notifications</Text>
        <Text style={styles.body}>Review Atlas updates, unread items and notification preferences.</Text>
      </Pressable>
      <BusinessMemoryPanel />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: tokens.spacing.lg, gap: tokens.spacing.lg, paddingBottom: 40 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.xs },
  cardTitle: { fontSize: 18, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
});
