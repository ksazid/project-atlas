import { StyleSheet, Text, View } from 'react-native';
import { AtlasPressable } from '@/components/AtlasPressable';
import { tokens } from '@/theme/tokens';

type Props = { onOpen: () => void; liveSummary?: string | null };

export function BusinessDataCard({ onOpen, liveSummary }: Props) {
  return (
    <View style={styles.card} accessibilityLabel="Business data connectors">
      <Text style={styles.eyebrow}>BUSINESS DATA</Text>
      <Text accessibilityRole="header" style={styles.title}>Keep Atlas fresh with live signals</Text>
      <Text style={styles.copy}>{liveSummary ?? 'Connect your business data so Atlas can turn fresh operational changes into more useful recommendations.'}</Text>
      <AtlasPressable accessibilityRole="button" accessibilityLabel="Manage connectors" onPress={onOpen} style={styles.action}>
        <Text style={styles.actionText}>Manage connectors</Text>
      </AtlasPressable>
    </View>
  );
}

const styles = StyleSheet.create({
  card: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 8, padding: 18 },
  eyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  title: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 },
  copy: { color: tokens.color.muted, fontSize: 13.5, lineHeight: 20 },
  action: { alignItems: 'center', alignSelf: 'flex-start', borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: tokens.touchTarget, paddingHorizontal: 2, paddingRight: 12 },
  actionText: { color: tokens.color.green, fontSize: 13, fontWeight: '800' },
});
