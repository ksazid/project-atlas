import { useCallback, useEffect, useMemo, useState } from 'react';
import { Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { getHistory, type HistoryItem, type HistoryResponse } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { AtlasScreen } from '@/components/AtlasScreen';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'empty' | 'error';
const statusOptions = ['all', 'available', 'applied', 'completed', 'skipped', 'not-relevant', 'rejected', 'expired'] as const;
const categoryOptions = ['all', 'business-profile', 'business-goal', 'knowledge-pack', 'recorded-evidence'] as const;

function HistoryCard({ item }: { item: HistoryItem }) {
  return (
    <Pressable accessibilityRole="button" accessibilityLabel={`Open ${item.title}`} onPress={() => router.push(`/opportunities/${item.opportunityId}`)} style={styles.card}>
      <View style={styles.cardHeader}>
        <Text style={styles.status}>{item.status.replaceAll('-', ' ')}</Text>
        <Text style={styles.date}>{new Date(item.createdAt).toLocaleDateString()}</Text>
      </View>
      <Text style={styles.cardTitle}>{item.title}</Text>
      {item.goalTitle ? <Text style={styles.body}>Goal: {item.goalTitle}</Text> : null}
      <Text style={styles.body}>{item.learningSummary}</Text>
      {item.executionKit ? <Text style={styles.supporting}>Execution Kit: {item.executionKit.usedAssetCount}/{item.executionKit.assetCount} assets used{item.executionKit.usefulnessRating ? ` · usefulness ${item.executionKit.usefulnessRating}/5` : ''}</Text> : null}
      {item.outcome ? <Text style={styles.supporting}>Outcome: {item.outcome.evidenceClass} · usefulness {item.outcome.usefulnessRating}/5</Text> : null}
      {item.decisionReasonCode ? <Text style={styles.supporting}>Owner reason: {item.decisionReasonCode.replaceAll('-', ' ')}</Text> : null}
      <Text style={styles.supporting}>{item.knowledgePackKey} v{item.knowledgePackVersion}</Text>
    </Pressable>
  );
}

export function HistoryScreen() {
  const [response, setResponse] = useState<HistoryResponse | null>(null);
  const [state, setState] = useState<ScreenState>('loading');
  const [refreshing, setRefreshing] = useState(false);
  const [status, setStatus] = useState<(typeof statusOptions)[number]>('all');
  const [category, setCategory] = useState<(typeof categoryOptions)[number]>('all');

  const filters = useMemo(() => ({
    status: status === 'all' ? undefined : status,
    category: category === 'all' ? undefined : category,
    limit: 100,
  }), [category, status]);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    try {
      const session = await loadSession();
      if (!session?.businessId) { setResponse(null); setState('empty'); return; }
      const value = await getHistory(session.accessToken, session.businessId, filters);
      setResponse(value);
      setState(value.count === 0 ? 'empty' : 'ready');
    } catch {
      setState('error');
    } finally {
      setRefreshing(false);
    }
  }, [filters]);

  useEffect(() => {
    const initialLoad = setTimeout(() => { void load(); }, 0);
    return () => clearTimeout(initialLoad);
  }, [load]);

  return (
    <AtlasScreen hasTabBar contentStyle={styles.container} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void load(true)} />}>
      <View style={styles.headerRow}>
        <View style={styles.headerText}>
          <Text style={styles.eyebrow}>BUSINESS HISTORY</Text>
          <Text accessibilityRole="header" style={styles.title}>What Atlas has shown and learned</Text>
        </View>
        <View style={styles.headerActions}>
          <Pressable accessibilityRole="button" accessibilityLabel="Open weekly review" onPress={() => router.push('/weekly-review')} style={styles.backButton}><Text style={styles.buttonText}>Weekly review</Text></Pressable>
        </View>
      </View>
      <Text style={styles.body}>Chronological Opportunity, Action, Execution Kit and Outcome records. Learning summaries describe recorded evidence and owner feedback; they do not claim causation.</Text>

      <View style={styles.filterCard}>
        <Text style={styles.filterTitle}>Status</Text>
        <View style={styles.wrap}>{statusOptions.map((value) => <Pressable key={value} accessibilityRole="button" accessibilityState={{ selected: status === value }} onPress={() => setStatus(value)} style={status === value ? styles.selectedChip : styles.chip}><Text style={styles.chipText}>{value.replaceAll('-', ' ')}</Text></Pressable>)}</View>
        <Text style={styles.filterTitle}>Category</Text>
        <View style={styles.wrap}>{categoryOptions.map((value) => <Pressable key={value} accessibilityRole="button" accessibilityState={{ selected: category === value }} onPress={() => setCategory(value)} style={category === value ? styles.selectedChip : styles.chip}><Text style={styles.chipText}>{value.replaceAll('-', ' ')}</Text></Pressable>)}</View>
      </View>

      {state === 'loading' ? <Text accessibilityLiveRegion="polite">Loading Business History…</Text> : null}
      {state === 'error' ? <View style={styles.messageCard}><Text style={styles.cardTitle}>History unavailable</Text><Text style={styles.body}>Atlas could not load History safely. Existing records have not been changed.</Text><Pressable accessibilityRole="button" onPress={() => void load()} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable></View> : null}
      {state === 'empty' ? <View style={styles.messageCard}><Text style={styles.cardTitle}>No matching History</Text><Text style={styles.body}>There are no records for the selected filters yet. History appears after Atlas has shown Opportunities and you interact with them.</Text></View> : null}
      {state === 'ready' ? response?.items.map((item) => <HistoryCard key={item.opportunityId} item={item} />) : null}
      {state === 'ready' ? <Text style={styles.supporting}>{response?.count ?? 0} record{response?.count === 1 ? '' : 's'} shown</Text> : null}
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  container: { gap: tokens.spacing.md },
  headerRow: { flexDirection: 'row', alignItems: 'flex-start', gap: tokens.spacing.sm },
  headerText: { flex: 1, gap: 4 },
  headerActions: { gap: tokens.spacing.sm, alignItems: 'stretch' },
  eyebrow: { fontSize: 13, fontWeight: '700', letterSpacing: 1.2 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  filterCard: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  filterTitle: { fontSize: 14, fontWeight: '700' },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  chip: { minHeight: 40, borderWidth: 1, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  selectedChip: { minHeight: 40, borderWidth: 2, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  chipText: { fontWeight: '700' },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  status: { fontSize: 13, fontWeight: '800', textTransform: 'uppercase' },
  date: { fontSize: 13 },
  messageCard: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  primaryButton: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  primaryText: { color: '#fff', fontWeight: '700' },
  backButton: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { fontWeight: '700' },
});