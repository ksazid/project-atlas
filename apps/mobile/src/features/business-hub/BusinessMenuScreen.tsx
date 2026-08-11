import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useRouter } from 'expo-router';
import { getBusinessMenu, type BusinessMenu } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { formatMenuItemPrice, groupMenuItems } from '@/features/business-hub/business-hub-model';
import { tokens } from '@/theme/tokens';

type State = 'loading' | 'ready' | 'missing' | 'error';

export function BusinessMenuScreen() {
  const router = useRouter();
  const [state, setState] = useState<State>('loading');
  const [menu, setMenu] = useState<BusinessMenu | null>(null);

  const load = useCallback(async () => {
    setState('loading');
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setMenu(null);
        setState('missing');
        return;
      }
      setMenu(await getBusinessMenu(session.accessToken, session.businessId));
      setState('ready');
    } catch {
      setState('error');
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  if (state !== 'ready' || !menu) return <MenuState state={state} onRetry={() => void load()} onBack={() => router.back()} />;
  const groups = groupMenuItems(menu.items);

  return (
    <ScrollView contentContainerStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.content}>
        <Pressable accessibilityRole="button" accessibilityLabel="Back to Business" onPress={() => router.back()} style={({ pressed }) => [styles.back, pressed && styles.pressed]}><Text style={styles.backText}>Back to Business</Text></Pressable>
        <View style={styles.header}><BrandMark size={48} /><Text style={styles.eyebrow}>MENU</Text><Text accessibilityRole="header" style={styles.title}>Menu intelligence</Text><Text style={styles.copy}>A read-only view of menu items Atlas has already observed for your business. Use it to check what Atlas understands, not to manage orders.</Text></View>

        {groups.length === 0 ? <View style={styles.empty}><Text style={styles.emptyTitle}>No menu observed yet</Text><Text style={styles.emptyCopy}>Atlas will show structured menu intelligence here when a supported public business source provides it.</Text></View> : groups.map(group => (
          <View key={group.section} style={styles.section}>
            <Text style={styles.sectionTitle}>{group.section}</Text>
            <View style={styles.items}>{group.items.map(item => {
              const price = formatMenuItemPrice(item.price, item.currency);
              return <View key={item.id} style={styles.item}><View style={styles.itemTop}><Text style={styles.itemName}>{item.name}</Text>{price ? <Text style={styles.price}>{price}</Text> : null}</View>{item.description ? <Text style={styles.description}>{item.description}</Text> : null}<Text style={styles.source}>{providerLabel(item.source)} · {formatDate(item.observedAt)}{item.ownerConfirmed ? ' · Owner confirmed' : ''}</Text></View>;
            })}</View>
          </View>
        ))}
      </View>
    </ScrollView>
  );
}

function MenuState({ state, onRetry, onBack }: { state: State; onRetry: () => void; onBack: () => void }) {
  if (state === 'loading') return <View style={styles.state}><BrandMark size={54} /><ActivityIndicator color={tokens.color.green} /><Text style={styles.stateCopy}>Loading menu intelligence…</Text></View>;
  if (state === 'missing') return <View style={styles.state}><BrandMark size={54} /><Text style={styles.stateTitle}>No business selected</Text><Text style={styles.stateCopy}>Choose a business before opening its menu intelligence.</Text><Pressable accessibilityRole="button" onPress={onBack} style={styles.stateButton}><Text style={styles.stateButtonText}>Back</Text></Pressable></View>;
  return <View style={styles.state}><BrandMark size={54} /><Text style={styles.stateTitle}>Menu intelligence is temporarily unavailable</Text><Text style={styles.stateCopy}>Your saved menu facts are unchanged.</Text><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={onRetry} style={styles.stateButton}><Text style={styles.stateButtonText}>Try again</Text></Pressable></View>;
}

function providerLabel(value: string): string { return value.split(/[-_\s]+/).filter(Boolean).map(part => `${part[0]?.toUpperCase() ?? ''}${part.slice(1)}`).join(' '); }
function formatDate(value: string): string { const date = new Date(value); return Number.isNaN(date.getTime()) ? 'Observed recently' : `Observed ${date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}`; }

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: tokens.color.canvas, flexGrow: 1, paddingBottom: 40, paddingHorizontal: 28, paddingTop: 46 }, content: { gap: 22, maxWidth: 680, width: '100%' },
  back: { alignItems: 'center', alignSelf: 'flex-start', justifyContent: 'center', minHeight: tokens.touchTarget, paddingRight: 14 }, backText: { color: tokens.color.green, fontSize: 13, fontWeight: '800' }, pressed: { opacity: .76 },
  header: { gap: 7 }, eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.2, marginTop: 6 }, title: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 31, fontWeight: '800', lineHeight: 37 }, copy: { color: tokens.color.muted, fontSize: 14, lineHeight: 21 },
  section: { gap: 10 }, sectionTitle: { color: tokens.color.greenDeep, fontSize: 18, fontWeight: '800' }, items: { gap: 10 }, item: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 7, padding: 16 }, itemTop: { alignItems: 'flex-start', flexDirection: 'row', gap: 12, justifyContent: 'space-between' }, itemName: { color: tokens.color.ink, flex: 1, fontSize: 15, fontWeight: '800', lineHeight: 21 }, price: { color: tokens.color.greenDeep, fontSize: 14, fontWeight: '900' }, description: { color: tokens.color.muted, fontSize: 13, lineHeight: 19 }, source: { color: tokens.color.muted, fontSize: 10.5, lineHeight: 16 },
  empty: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 8, padding: 20 }, emptyTitle: { color: tokens.color.ink, fontSize: 18, fontWeight: '800' }, emptyCopy: { color: tokens.color.muted, fontSize: 13.5, lineHeight: 20 },
  state: { alignItems: 'center', backgroundColor: tokens.color.canvas, flex: 1, gap: 14, justifyContent: 'center', padding: 28 }, stateTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 26, fontWeight: '800', lineHeight: 32, textAlign: 'center' }, stateCopy: { color: tokens.color.muted, fontSize: 14, lineHeight: 21, maxWidth: 360, textAlign: 'center' }, stateButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 48, paddingHorizontal: 20 }, stateButtonText: { color: tokens.color.surface, fontSize: 14, fontWeight: '800' },
});
