import { useCallback, useEffect, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { decideOpportunity, getTodayFocus, type OpportunityDecision, type TodayFocus } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'empty' | 'error';

export function TodayFocusScreen() {
  const [focus, setFocus] = useState<TodayFocus | null>(null);
  const [state, setState] = useState<ScreenState>('loading');
  const [refreshing, setRefreshing] = useState(false);
  const [deciding, setDeciding] = useState(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setFocus(null);
        setState('empty');
        return;
      }
      const value = await getTodayFocus(session.accessToken, session.businessId);
      setFocus(value);
      setState('ready');
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

  const retry = () => {
    setState('loading');
    void load();
  };

  const decide = async (decision: OpportunityDecision) => {
    if (focus?.state !== 'ready') return;
    setDeciding(true);
    try {
      const session = await loadSession();
      if (!session?.businessId) return;
      const reason = decision === 'apply' ? undefined : decision === 'skip' ? 'Not the right time' : 'Does not fit my business';
      await decideOpportunity(session.accessToken, session.businessId, focus.opportunity, decision, reason);
      await load();
    } catch {
      setState('error');
    } finally {
      setDeciding(false);
    }
  };

  if (state === 'loading') return <View style={styles.center}><Text accessibilityLiveRegion="polite">Finding today’s strongest action…</Text></View>;
  if (state === 'empty') return <View style={styles.center}><Text accessibilityRole="header" style={styles.title}>Today’s Focus</Text><Text style={styles.body}>Create or select a Business to receive a focused action.</Text></View>;
  if (state === 'error') return <View style={styles.center}><Text accessibilityRole="header" style={styles.title}>Today’s Focus unavailable</Text><Text style={styles.body}>Atlas could not load a safe recommendation. No action has been created.</Text><Pressable accessibilityRole="button" onPress={retry} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable></View>;

  if (focus?.state === 'insufficient-context') {
    return <ScrollView contentContainerStyle={styles.center}><Text accessibilityRole="header" style={styles.title}>No suitable focus yet</Text><Text style={styles.body}>{focus.message}</Text><Text style={styles.supporting}>Atlas will not create filler recommendations when the available context is insufficient.</Text></ScrollView>;
  }

  const opportunity = focus?.state === 'ready' ? focus.opportunity : null;
  return (
    <ScrollView contentContainerStyle={styles.container} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void load(true)} />}>
      <Text style={styles.eyebrow}>TODAY’S FOCUS</Text>
      <Text accessibilityRole="header" style={styles.title}>{opportunity?.title}</Text>
      <Text style={styles.body}>{opportunity?.whyItMatters}</Text>

      <View style={styles.metrics}>
        <View style={styles.metric}><Text style={styles.metricLabel}>Impact</Text><Text style={styles.metricValue}>{opportunity?.expectedImpact}</Text></View>
        <View style={styles.metric}><Text style={styles.metricLabel}>Effort</Text><Text style={styles.metricValue}>{opportunity?.effort}</Text></View>
        <View style={styles.metric}><Text style={styles.metricLabel}>Confidence</Text><Text style={styles.metricValue}>{opportunity?.confidence}</Text></View>
      </View>

      <View style={styles.card}><Text style={styles.cardTitle}>Why now</Text><Text style={styles.body}>{opportunity?.whyNow}</Text></View>
      <View style={styles.card}><Text style={styles.cardTitle}>Evidence</Text><Text style={styles.body}>{opportunity?.evidenceSummary}</Text><Text style={styles.supporting}>Atlas interpretation is separate from the confirmed evidence above.</Text></View>

      <Pressable accessibilityRole="button" onPress={() => opportunity && router.push(`/opportunities/${opportunity.id}`)} style={styles.detailButton}><Text style={styles.secondaryText}>View full details</Text></Pressable>
      <Text style={styles.supporting}>Expires {opportunity ? new Date(opportunity.expiresAt).toLocaleString() : ''} · {opportunity?.knowledgePackKey} v{opportunity?.knowledgePackVersion}</Text>

      <Pressable accessibilityRole="button" disabled={deciding} onPress={() => void decide('apply')} style={styles.primaryButton}><Text style={styles.primaryText}>{deciding ? 'Saving…' : 'Apply'}</Text></Pressable>
      <View style={styles.secondaryRow}>
        <Pressable accessibilityRole="button" disabled={deciding} onPress={() => void decide('skip')} style={styles.secondaryButton}><Text style={styles.secondaryText}>Skip</Text></Pressable>
        <Pressable accessibilityRole="button" disabled={deciding} onPress={() => void decide('not-relevant')} style={styles.secondaryButton}><Text style={styles.secondaryText}>Not relevant</Text></Pressable>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: tokens.spacing.lg, gap: tokens.spacing.md, paddingBottom: 40 },
  center: { flex: 1, justifyContent: 'center', padding: tokens.spacing.lg, gap: tokens.spacing.md },
  eyebrow: { fontSize: 13, fontWeight: '700', letterSpacing: 1.2 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  metrics: { gap: tokens.spacing.sm },
  metric: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: 4 },
  metricLabel: { fontSize: 13, fontWeight: '700' },
  metricValue: { fontSize: 16, lineHeight: 22 },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  primaryButton: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  primaryText: { color: '#fff', fontWeight: '700' },
  detailButton: { minHeight: 48, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  secondaryRow: { flexDirection: 'row', gap: tokens.spacing.sm },
  secondaryButton: { flex: 1, minHeight: 48, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.sm },
  secondaryText: { fontWeight: '700' },
});
