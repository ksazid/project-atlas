import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { BusinessHubMenuSummary } from '@/api/atlas-client';
import { formatMenuItemPrice, getMenuPresentation } from '@/features/business-hub/business-hub-model';
import { tokens } from '@/theme/tokens';

type Props = { menu: BusinessHubMenuSummary; title?: string; onViewFull: () => void };

export function MenuIntelligenceCard({ menu, title = 'Menu intelligence', onViewFull }: Props) {
  const presentation = getMenuPresentation(menu);
  return (
    <View style={styles.card}>
      <Text style={styles.eyebrow}>MENU INTELLIGENCE</Text>
      <Text style={styles.sectionTitle}>{title}</Text>
      <Text style={styles.summary}>{presentation.title}</Text>
      {presentation.priceRange ? <Text style={styles.price}>{presentation.priceRange} observed price range</Text> : null}
      {menu.preview.slice(0, 3).map(item => (
        <View key={`${item.section ?? 'other'}-${item.name}`} style={styles.item}>
          <View style={styles.itemText}><Text style={styles.itemName}>{item.name}</Text>{item.section ? <Text style={styles.itemSection}>{item.section}</Text> : null}</View>
          {formatMenuItemPrice(item.price, item.currency) ? <Text style={styles.itemPrice}>{formatMenuItemPrice(item.price, item.currency)}</Text> : null}
        </View>
      ))}
      {presentation.sourceLabel ? <Text style={styles.source}>{presentation.sourceLabel}{menu.observedAt ? ` · ${formatDate(menu.observedAt)}` : ''}</Text> : null}
      {presentation.actionLabel ? <Pressable accessibilityRole="button" accessibilityLabel={presentation.actionLabel} onPress={onViewFull} style={({ pressed }) => [styles.action, pressed && styles.pressed]}><Text style={styles.actionText}>{presentation.actionLabel}</Text></Pressable> : null}
    </View>
  );
}

function formatDate(value: string): string { const date = new Date(value); return Number.isNaN(date.getTime()) ? 'observed recently' : `observed ${date.toLocaleDateString(undefined, { day: 'numeric', month: 'short' })}`; }

const styles = StyleSheet.create({
  card: { backgroundColor: '#F3F8F5', borderColor: '#D8E8DF', borderRadius: tokens.radius.md, borderWidth: 1, gap: 10, padding: 18 },
  eyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  sectionTitle: { color: tokens.color.ink, fontSize: 19, fontWeight: '800' },
  summary: { color: tokens.color.greenDeep, fontSize: 16, fontWeight: '800', lineHeight: 22 },
  price: { color: tokens.color.muted, fontSize: 13, lineHeight: 19 },
  item: { alignItems: 'center', borderTopColor: '#DCE7E1', borderTopWidth: 1, flexDirection: 'row', gap: 12, justifyContent: 'space-between', paddingTop: 10 },
  itemText: { flex: 1, gap: 2 }, itemName: { color: tokens.color.ink, fontSize: 13.5, fontWeight: '700' }, itemSection: { color: tokens.color.muted, fontSize: 11.5 },
  itemPrice: { color: tokens.color.greenDeep, fontSize: 13.5, fontWeight: '800' }, source: { color: tokens.color.muted, fontSize: 11.5, lineHeight: 17 },
  action: { alignItems: 'center', alignSelf: 'flex-start', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1, justifyContent: 'center', minHeight: tokens.touchTarget, paddingHorizontal: 16 },
  actionText: { color: tokens.color.green, fontSize: 13, fontWeight: '800' }, pressed: { opacity: .8 },
});
