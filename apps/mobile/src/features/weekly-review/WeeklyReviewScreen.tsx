import { useCallback, useEffect, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { getWeeklyReview, type WeeklyReview } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'empty' | 'error';

export function WeeklyReviewScreen() {
  const [review, setReview] = useState<WeeklyReview | null>(null);
  const [state, setState] = useState<ScreenState>('loading');
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    try {
      const session = await loadSession();
      if (!session?.businessId) { setReview(null); setState('empty'); return; }
      const value = await getWeeklyReview(session.accessToken, session.businessId);
      setReview(value);
      setState(value.counts.opportunities === 0 ? 'empty' : 'ready');
    } catch {
      setState('error');
    } finally {
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    const initialLoad = setTimeout(() => { void load(); }, 0);
    return () => clearTimeout(initialLoad);
  }, [load]);

  return (
    <ScrollView contentContainerStyle={styles.container} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void load(true)} />}>
      <View style={styles.headerRow}>
        <View style={styles.headerText}>
          <Text style={styles.eyebrow}>WEEKLY REVIEW</Text>
          <Text accessibilityRole="header" style={styles.title}>Your last seven days in Atlas</Text>
        </View>
        <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.backButton}><Text style={styles.buttonText}>Back</Text></Pressable>
      </View>
      {review ? <Text style={styles.supporting}>{new Date(review.periodStart).toLocaleDateString()} – {new Date(review.periodEnd).toLocaleDateString()}</Text> : null}

      {state === 'loading' ? <Text accessibilityLiveRegion="polite">Preparing Weekly Review…</Text> : null}
      {state === 'error' ? <View style={styles.card}><Text style={styles.cardTitle}>Weekly Review unavailable</Text><Text style={styles.body}>Atlas could not safely assemble this review. Existing records have not been changed.</Text><Pressable accessibilityRole="button" onPress={() => void load()} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable></View> : null}
      {state === 'empty' ? <View style={styles.card}><Text style={styles.cardTitle}>No recorded activity this week</Text><Text style={styles.body}>Atlas has no Opportunity, Action, Execution Kit or Outcome activity recorded for this seven-day period.</Text>{review ? <Text style={styles.supporting}>{review.evidenceNote}</Text> : null}</View> : null}

      {state === 'ready' && review ? <>
        <View style={styles.metrics}>
          <View style={styles.metric}><Text style={styles.metricLabel}>Opportunities</Text><Text style={styles.metricValue}>{review.counts.opportunities}</Text></View>
          <View style={styles.metric}><Text style={styles.metricLabel}>Completed</Text><Text style={styles.metricValue}>{review.counts.completed}</Text></View>
          <View style={styles.metric}><Text style={styles.metricLabel}>Outcomes</Text><Text style={styles.metricValue}>{review.counts.outcomesRecorded}</Text></View>
          <View style={styles.metric}><Text style={styles.metricLabel}>Assets used</Text><Text style={styles.metricValue}>{review.counts.executionAssetsUsed}</Text></View>
        </View>

        <View style={styles.card}><Text style={styles.cardTitle}>Highlights</Text>{review.highlights.map((item) => <Text key={item} style={styles.body}>• {item}</Text>)}</View>

        <View style={styles.card}>
          <Text style={styles.cardTitle}>Outcome evidence</Text>
          {review.outcomes.length === 0 ? <Text style={styles.body}>No Outcomes were recorded during this period.</Text> : review.outcomes.map((item) => <Pressable key={`${item.opportunityId}-${item.recordedAt}`} accessibilityRole="button" onPress={() => router.push(`/opportunities/${item.opportunityId}`)} style={styles.item}><Text style={styles.itemTitle}>{item.opportunityTitle}</Text><Text style={styles.body}>{item.resultSummary}</Text><Text style={styles.supporting}>{item.evidenceClass} · usefulness {item.usefulnessRating}/5{item.goalTitle ? ` · ${item.goalTitle}` : ''}</Text><Text style={styles.supporting}>{item.knowledgePackKey} v{item.knowledgePackVersion}</Text></Pressable>)}
        </View>

        <View style={styles.card}>
          <Text style={styles.cardTitle}>Still open</Text>
          {review.openItems.length === 0 ? <Text style={styles.body}>No open Actions remain from recorded activity in this review period.</Text> : review.openItems.map((item) => <Pressable key={item.opportunityId} accessibilityRole="button" onPress={() => router.push(`/opportunities/${item.opportunityId}`)} style={styles.item}><Text style={styles.itemTitle}>{item.opportunityTitle}</Text><Text style={styles.supporting}>{item.status.replaceAll('-', ' ')}{item.goalTitle ? ` · ${item.goalTitle}` : ''}</Text><Text style={styles.supporting}>{item.knowledgePackKey} v{item.knowledgePackVersion}</Text></Pressable>)}
        </View>

        <Text style={styles.supporting}>{review.evidenceNote}</Text>
      </> : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: tokens.spacing.lg, gap: tokens.spacing.md, paddingBottom: 40 },
  headerRow: { flexDirection: 'row', alignItems: 'flex-start', gap: tokens.spacing.sm },
  headerText: { flex: 1, gap: 4 },
  eyebrow: { fontSize: 13, fontWeight: '700', letterSpacing: 1.2 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  metrics: { flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  metric: { minWidth: '47%', flexGrow: 1, borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: 4 },
  metricLabel: { fontSize: 13, fontWeight: '700' },
  metricValue: { fontSize: 24, fontWeight: '800' },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  item: { borderTopWidth: 1, paddingTop: tokens.spacing.sm, gap: 4 },
  itemTitle: { fontSize: 16, fontWeight: '700' },
  primaryButton: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  primaryText: { color: '#fff', fontWeight: '700' },
  backButton: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { fontWeight: '700' },
});
