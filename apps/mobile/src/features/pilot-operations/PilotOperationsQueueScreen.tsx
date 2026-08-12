import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { useRouter } from 'expo-router';
import { loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import { BrandMark } from '@/components/BrandMark';
import { listPilotBusinesses, PilotOperationsAccessError, type PilotBusinessListItem } from './pilot-operations-api';
import { attentionLabel, formatPilotDate, generationLabel, type PilotScreenState } from './pilot-operations-model';
import { tokens } from '@/theme/tokens';

export function PilotOperationsQueueScreen() {
  const router = useRouter();
  const [state, setState] = useState<PilotScreenState>('loading');
  const [items, setItems] = useState<PilotBusinessListItem[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true); else setState('loading');
    try {
      const session = await loadSession();
      if (!session?.accessToken) { setState('forbidden'); return; }
      const result = await listPilotBusinesses(session.accessToken);
      setItems(result);
      setState(result.length === 0 ? 'empty' : 'ready');
    } catch (error) {
      setState(error instanceof PilotOperationsAccessError ? 'forbidden' : 'error');
    } finally {
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  if (state === 'loading') return <StateScreen title="Pilot Operations" copy="Loading pilot businesses…" busy />;
  if (state === 'forbidden') return <StateScreen title="Pilot Operations" copy="This internal workspace is available only to authorized Atlas operators." />;
  if (state === 'empty') return <StateScreen title="Pilot Operations" copy="No pilot businesses need review yet." onRetry={() => void load()} />;
  if (state === 'error') return <StateScreen title="Pilot Operations" copy="Pilot review is temporarily unavailable. No business data was changed." onRetry={() => void load()} />;

  return (
    <AtlasScreen
      contentStyle={styles.screen}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void load(true)} tintColor={tokens.color.green} />}
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.content}>
        <View style={styles.header}>
          <BrandMark size={46} />
          <Text style={styles.eyebrow}>INTERNAL</Text>
          <Text accessibilityRole="header" style={styles.title}>Pilot Operations</Text>
          <Text style={styles.subtitle}>Review evidence, diagnostics and owner-reported issues before taking a bounded operator action.</Text>
        </View>
        {items.map((item) => (
          <AtlasPressable
            key={item.businessId}
            accessibilityRole="button"
            accessibilityLabel={`Review ${item.name}`}
            onPress={() => router.push(`/operator/businesses/${item.businessId}` as never)}
            style={styles.card}
          >
            <View style={styles.cardTop}>
              <View style={styles.cardTitleWrap}>
                <Text style={styles.cardTitle}>{item.name}</Text>
                <Text style={styles.meta}>{item.category} · {item.primaryLocation}</Text>
              </View>
              <Text style={styles.chevron}>›</Text>
            </View>
            <View style={styles.signalRow}>
              <Text style={styles.signal}>{attentionLabel(item)}</Text>
              <Text style={styles.muted}>{item.goalCount} goals</Text>
            </View>
            <Text style={styles.detail}>{generationLabel(item.latestGenerationOutcome, item.latestGenerationCode)}</Text>
            {item.currentOpportunityTitle ? <Text numberOfLines={2} style={styles.detail}>Current: {item.currentOpportunityTitle}</Text> : null}
            <Text style={styles.freshness}>Latest generation {formatPilotDate(item.latestGenerationAt)}</Text>
          </AtlasPressable>
        ))}
      </View>
    </AtlasScreen>
  );
}

function StateScreen({ title, copy, busy = false, onRetry }: { title: string; copy: string; busy?: boolean; onRetry?: () => void }) {
  return (
    <AtlasScreen mode="static" contentStyle={styles.state}>
      <BrandMark size={54} />
      {busy ? <ActivityIndicator color={tokens.color.green} /> : null}
      <Text accessibilityRole="header" style={styles.stateTitle}>{title}</Text>
      <Text style={styles.stateCopy}>{copy}</Text>
      {onRetry ? <AtlasPressable accessibilityRole="button" accessibilityLabel="Try again" onPress={onRetry} style={styles.retry}><Text style={styles.retryText}>Try again</Text></AtlasPressable> : null}
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  screen: { alignItems: 'center', backgroundColor: tokens.color.surface },
  content: { width: '100%', maxWidth: 720, gap: 14 },
  header: { gap: 7, marginBottom: 8 },
  eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.2, marginTop: 5 },
  title: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 32, lineHeight: 38, fontWeight: '800' },
  subtitle: { color: tokens.color.muted, fontSize: 14, lineHeight: 21 },
  card: { borderWidth: 1, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, padding: 18, backgroundColor: tokens.color.surface, gap: 10 },
  cardTop: { flexDirection: 'row', alignItems: 'center', gap: 12 }, cardTitleWrap: { flex: 1, gap: 3 },
  cardTitle: { color: tokens.color.ink, fontSize: 18, fontWeight: '800' }, meta: { color: tokens.color.muted, fontSize: 12.5 }, chevron: { color: tokens.color.green, fontSize: 28 },
  signalRow: { flexDirection: 'row', gap: 10, justifyContent: 'space-between', alignItems: 'center' }, signal: { color: tokens.color.greenDeep, fontSize: 12, fontWeight: '800' }, muted: { color: tokens.color.muted, fontSize: 12 },
  detail: { color: tokens.color.ink, fontSize: 13, lineHeight: 19 }, freshness: { color: tokens.color.muted, fontSize: 11 },
  state: { alignItems: 'center', justifyContent: 'center', backgroundColor: tokens.color.surface, gap: 13 },
  stateTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 28, fontWeight: '800' }, stateCopy: { color: tokens.color.muted, fontSize: 14, lineHeight: 21, maxWidth: 420, textAlign: 'center' },
  retry: { backgroundColor: tokens.color.green, minHeight: 48, borderRadius: tokens.radius.pill, paddingHorizontal: 22, alignItems: 'center', justifyContent: 'center' }, retryText: { color: tokens.color.surface, fontWeight: '800' },
});
