import { ScrollView, StyleSheet, Text } from 'react-native';
import { BusinessMemoryPanel } from '@/features/business-memory/BusinessMemoryPanel';
import { tokens } from '@/theme/tokens';

export default function SettingsScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text accessibilityRole="header" style={styles.title}>Settings</Text>
      <BusinessMemoryPanel />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: tokens.spacing.lg, gap: tokens.spacing.lg, paddingBottom: 40 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
});
