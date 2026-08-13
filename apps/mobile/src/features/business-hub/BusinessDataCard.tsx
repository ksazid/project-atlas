import { StyleSheet, Text, View } from 'react-native';
import { AtlasPressable } from '@/components/AtlasPressable';
import { tokens } from '@/theme/tokens';

export function BusinessDataCard({ onOpen }: { onOpen: () => void }) {
  return (
    <View style={styles.card} accessibilityLabel="Business data connector">
      <Text style={styles.eyebrow}>BUSINESS DATA</Text>
      <Text accessibilityRole="header" style={styles.title}>Make Today more useful with fresh signals.</Text>
      <Text style={styles.copy}>Connect one read-only Google Drive folder for automatic CSV sync. Atlas leaves raw files in Drive and keeps only privacy-safe business signals.</Text>
      <AtlasPressable accessibilityRole="button" accessibilityLabel="Open business data connectors" onPress={onOpen} style={styles.button}>
        <Text style={styles.buttonText}>Connect business data</Text>
      </AtlasPressable>
    </View>
  );
}

const styles = StyleSheet.create({
  card: { backgroundColor: tokens.color.greenDeep, borderRadius: tokens.radius.lg, gap: 9, padding: 20 },
  eyebrow: { color: tokens.color.mint, fontSize: 11, fontWeight: '900', letterSpacing: 1.1 },
  title: { color: tokens.color.surface, fontFamily: 'Georgia', fontSize: 23, fontWeight: '800', lineHeight: 29 },
  copy: { color: tokens.color.surface, fontSize: 13.5, lineHeight: 20, opacity: .82 },
  button: { alignItems: 'center', backgroundColor: tokens.color.surface, borderRadius: tokens.radius.pill, justifyContent: 'center', marginTop: 5, minHeight: 48, paddingHorizontal: 18 },
  buttonText: { color: tokens.color.greenDeep, fontSize: 14, fontWeight: '900' },
});
