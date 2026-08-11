import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { BusinessHubContextSummary } from '@/api/atlas-client';
import { getContextPresentation } from '@/features/business-hub/business-hub-model';
import { tokens } from '@/theme/tokens';

type Props = { context: BusinessHubContextSummary; onReview: () => void };

export function BusinessContextStatus({ context, onReview }: Props) {
  const presentation = getContextPresentation(context);
  return <View style={styles.card}><Text style={styles.eyebrow}>BUSINESS CONTEXT</Text><Text style={styles.title}>{presentation.title}</Text><Text style={styles.copy}>{presentation.copy}</Text><Pressable accessibilityRole="button" accessibilityLabel={presentation.actionLabel} onPress={onReview} style={({ pressed }) => [styles.action, pressed && styles.pressed]}><Text style={styles.actionText}>{presentation.actionLabel}</Text></Pressable></View>;
}

const styles = StyleSheet.create({
  card: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 8, padding: 18 },
  eyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 }, title: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 }, copy: { color: tokens.color.muted, fontSize: 13.5, lineHeight: 20 },
  action: { alignItems: 'center', alignSelf: 'flex-start', borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: tokens.touchTarget, paddingHorizontal: 2, paddingRight: 12 }, actionText: { color: tokens.color.green, fontSize: 13, fontWeight: '800' }, pressed: { opacity: .75 },
});
