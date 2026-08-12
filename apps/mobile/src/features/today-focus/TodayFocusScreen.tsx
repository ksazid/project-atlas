import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { decideOpportunity, getTodayFocus, type OpportunityDecision, type TodayFocus } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { AtlasScreen } from '@/components/AtlasScreen';
import { todayFocusRecoveryAction } from './today-focus-recovery';

type ScreenState = 'loading' | 'ready' | 'empty' | 'error';
type MetricTone = 'impact' | 'effort' | 'confidence';

const GREEN = '#00754A';
const GREEN_BRIGHT = '#008A57';
const DARK = '#0A2F25';
const INK = '#17221C';
const MUTED = '#5B6761';
const SOFT_MINT = '#F1F8F4';
const SOFT_BLUE = '#F1F6FB';
const SOFT_AMBER = '#FFF8E8';
const SOFT_LAVENDER = '#F6F2FA';

export function TodayFocusScreen() {
  const [focus, setFocus] = useState<TodayFocus | null>(null);
  const [state, setState] = useState<ScreenState>('loading');
  const [refreshing, setRefreshing] = useState(false);
  const [deciding, setDeciding] = useState(false);
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
  const [refreshFailed, setRefreshFailed] = useState(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setFocus(null);
        setState('empty');
        setRefreshFailed(false);
        return;
      }
      const value = await getTodayFocus(session.accessToken, session.businessId);
      setFocus(value);
      setState('ready');
      setLastUpdatedAt(new Date());
      setRefreshFailed(false);
    } catch {
      if (manual) {
        setRefreshFailed(true);
      } else {
        setState('error');
      }
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

  const refreshControl = <RefreshControl tintColor={GREEN} refreshing={refreshing} onRefresh={() => void load(true)} />;
  const freshnessLabel = refreshFailed
    ? 'Couldn’t refresh · showing previous result'
    : lastUpdatedAt
      ? 'Updated just now'
      : null;

  if (state === 'loading') {
    return <StateShell><View style={styles.loadingOrb}><ActivityIndicator color={GREEN} size="small" /></View><Text style={styles.stateEyebrow}>TODAY</Text><Text accessibilityLiveRegion="polite" style={styles.stateTitle}>Refreshing Today…</Text><Text style={styles.stateBody}>Finding the strongest useful move from your current business context.</Text></StateShell>;
  }

  if (state === 'empty') {
    return <StateShell><View style={styles.stateIcon}><Text style={styles.stateIconText}>◎</Text></View><Text style={styles.stateEyebrow}>TODAY</Text><Text accessibilityRole="header" style={styles.stateTitle}>Choose a business to get your first Best move.</Text><Text style={styles.stateBody}>Atlas needs an active business before it can suggest anything useful.</Text></StateShell>;
  }

  if (state === 'error') {
    return <StateShell><View style={styles.stateIcon}><Text style={styles.stateIconText}>!</Text></View><Text style={styles.stateEyebrow}>TODAY</Text><Text accessibilityRole="header" style={styles.stateTitle}>Today couldn’t refresh safely.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>Nothing new was created. Try again when you’re ready.</Text><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={retry} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}><Text style={styles.primaryText}>Try again</Text></Pressable></StateShell>;
  }

  if (focus?.state === 'insufficient-context') {
    const recoveryAction = todayFocusRecoveryAction(focus.code);
    const missingGoal = focus.code === 'opportunity_goal_missing';
    return <AtlasScreen hasTabBar contentStyle={styles.stateContainer} showsVerticalScrollIndicator={false} refreshControl={refreshControl}>
      <View style={styles.stateIcon}><Text style={styles.stateIconText}>◌</Text></View>
      <Text style={styles.stateEyebrow}>TODAY</Text>
      <Text accessibilityRole="header" style={styles.stateTitle}>{missingGoal ? 'Choose a goal to get your first Best move.' : 'Atlas needs a little more context first.'}</Text>
      <Text accessibilityLiveRegion="polite" style={styles.stateBody}>{focus.message}</Text>
      <FreshnessLabel label={freshnessLabel} />
      <View style={[styles.noteCard, styles.noteMint]}><Text style={styles.noteTitle}>Next step</Text><Text style={styles.noteText}>{missingGoal ? 'Pick the goal that matters most. Atlas will use only goals you explicitly save.' : 'Add the missing business context and pull down to refresh Today.'}</Text></View>
      <Pressable accessibilityRole="button" accessibilityLabel={recoveryAction.label} onPress={() => router.push(recoveryAction.route)} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}><Text style={styles.primaryText}>{recoveryAction.label}</Text></Pressable>
      <Pressable accessibilityRole="button" onPress={() => router.push('/history')} style={({ pressed }) => [styles.secondaryWide, pressed && styles.pressed]}><Text style={styles.secondaryText}>View business history</Text></Pressable>
    </AtlasScreen>;
  }

  if (focus?.state === 'no-focus') {
    return <AtlasScreen hasTabBar contentStyle={styles.stateContainer} showsVerticalScrollIndicator={false} refreshControl={refreshControl}>
      <View style={styles.stateIcon}><Text style={styles.stateIconText}>◎</Text></View>
      <Text style={styles.stateEyebrow}>TODAY</Text>
      <Text accessibilityRole="header" style={styles.stateTitle}>Nothing strong enough to recommend yet.</Text>
      <Text accessibilityLiveRegion="polite" style={styles.stateBody}>{focus.message}</Text>
      <FreshnessLabel label={freshnessLabel} />
      <View style={[styles.noteCard, styles.noteBlue]}><Text style={styles.noteTitle}>Quality before quantity</Text><Text style={styles.noteText}>Atlas will not create filler recommendations. Pull down anytime to check again.</Text></View>
      <Pressable accessibilityRole="button" accessibilityLabel="Review business context" onPress={() => router.push('/profile')} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}><Text style={styles.primaryText}>Review business context</Text></Pressable>
      <Pressable accessibilityRole="button" onPress={() => router.push('/history')} style={({ pressed }) => [styles.secondaryWide, pressed && styles.pressed]}><Text style={styles.secondaryText}>View history</Text></Pressable>
    </AtlasScreen>;
  }

  if (focus?.state === 'degraded') {
    return <AtlasScreen hasTabBar contentStyle={styles.stateContainer} showsVerticalScrollIndicator={false} refreshControl={refreshControl}>
      <View style={styles.stateIcon}><Text style={styles.stateIconText}>!</Text></View>
      <Text style={styles.stateEyebrow}>TODAY</Text>
      <Text accessibilityRole="header" style={styles.stateTitle}>Today couldn’t refresh safely.</Text>
      <Text accessibilityLiveRegion="polite" style={styles.stateBody}>{focus.message}</Text>
      <FreshnessLabel label={freshnessLabel} />
      <View style={[styles.noteCard, styles.noteAmber]}><Text style={styles.noteTitle}>No guesswork</Text><Text style={styles.noteText}>Atlas stopped instead of showing guidance it couldn’t support.</Text></View>
      <Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={() => void load(true)} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}><Text style={styles.primaryText}>Try again</Text></Pressable>
      <Pressable accessibilityRole="button" accessibilityLabel="Review business context" onPress={() => router.push('/profile')} style={({ pressed }) => [styles.secondaryWide, pressed && styles.pressed]}><Text style={styles.secondaryText}>Review business context</Text></Pressable>
    </AtlasScreen>;
  }

  if (focus?.state !== 'ready') {
    return <StateShell><View style={styles.stateIcon}><Text style={styles.stateIconText}>!</Text></View><Text style={styles.stateEyebrow}>TODAY</Text><Text accessibilityRole="header" style={styles.stateTitle}>Today needs a refresh.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>Atlas received an unexpected state and did not create a recommendation.</Text><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={retry} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}><Text style={styles.primaryText}>Try again</Text></Pressable></StateShell>;
  }

  const opportunity = focus.opportunity;
  return (
    <AtlasScreen
      hasTabBar
      contentStyle={styles.container}
      showsVerticalScrollIndicator={false}
      refreshControl={refreshControl}
    >
      <View style={styles.topRow}>
        <BrandMark size={46} />
        <Pressable accessibilityRole="button" accessibilityLabel="View business history" onPress={() => router.push('/history')} style={({ pressed }) => [styles.historyButton, pressed && styles.pressed]}><Text style={styles.historyText}>History</Text></Pressable>
      </View>

      <Text accessibilityRole="header" style={styles.pageTitle}>Today</Text>
      <Text style={styles.pageLead}>Here’s what matters today.</Text>
      <FreshnessLabel label={freshnessLabel} />

      <View style={styles.bestMoveCard}>
        <View style={styles.bestMoveHeader}>
          <View style={styles.bestMoveIcon}><Text style={styles.bestMoveIconText}>↗</Text></View>
          <Text style={styles.bestMoveLabel}>Best move</Text>
        </View>

        <Text style={styles.bestMoveTitle}>{opportunity.title}</Text>
        <Text style={styles.bestMoveReason}>{opportunity.whyItMatters}</Text>

        <View style={styles.metricsRow}>
          <Metric tone="impact" label="Impact" value={opportunity.expectedImpact} />
          <Metric tone="effort" label="Effort" value={opportunity.effort} />
          <Metric tone="confidence" label="Confidence" value={opportunity.confidence} />
        </View>

        <View style={styles.actionRow}>
          <Pressable accessibilityRole="button" accessibilityLabel="Apply best move" disabled={deciding} onPress={() => void decide('apply')} style={({ pressed }) => [styles.applyButton, pressed && styles.pressed, deciding && styles.disabled]}>
            {deciding ? <ActivityIndicator color="#FFF" /> : <Text style={styles.applyText}>Apply</Text>}
          </Pressable>
          <Pressable accessibilityRole="button" accessibilityLabel="Why this move" disabled={deciding} onPress={() => router.push(`/opportunities/${opportunity.id}`)} style={({ pressed }) => [styles.whyButton, pressed && styles.pressed, deciding && styles.disabled]}><Text style={styles.whyText}>Why?</Text></Pressable>
        </View>

        <View style={styles.quietActionRow}>
          <Pressable accessibilityRole="button" accessibilityLabel="Save this move for later" disabled={deciding} onPress={() => void decide('skip')} style={({ pressed }) => [styles.smallAction, pressed && styles.pressed, deciding && styles.disabled]}><Text style={styles.smallActionText}>Later</Text></Pressable>
          <Pressable accessibilityRole="button" accessibilityLabel="Mark this move not relevant" disabled={deciding} onPress={() => void decide('not-relevant')} style={({ pressed }) => [styles.smallAction, pressed && styles.pressed, deciding && styles.disabled]}><Text style={styles.smallActionText}>Not relevant</Text></Pressable>
        </View>
      </View>

      <View style={[styles.supportCard, styles.supportBlue]}>
        <View style={styles.supportRow}><View><Text style={styles.supportEyebrow}>Want the reasoning?</Text><Text style={styles.supportText}>See why now, evidence and assumptions without cluttering Today.</Text></View></View>
        <Pressable accessibilityRole="button" accessibilityLabel="Open full opportunity details" onPress={() => router.push(`/opportunities/${opportunity.id}`)} style={({ pressed }) => [styles.supportLink, pressed && styles.pressed]}><Text style={styles.supportLinkText}>Open details →</Text></Pressable>
      </View>
    </AtlasScreen>
  );
}

function Metric({ label, value, tone }: { label: string; value: string; tone: MetricTone }) {
  const toneStyle = tone === 'impact' ? styles.metricImpact : tone === 'effort' ? styles.metricEffort : styles.metricConfidence;
  return <View style={[styles.metric, toneStyle]}><Text style={styles.metricLabel}>{label}</Text><Text numberOfLines={3} style={styles.metricValue}>{value}</Text></View>;
}

function FreshnessLabel({ label }: { label: string | null }) {
  if (!label) return null;
  return <Text accessibilityLiveRegion="polite" style={styles.freshness}>{label}</Text>;
}

function StateShell({ children }: { children: React.ReactNode }) {
  return <AtlasScreen hasTabBar mode="static" contentStyle={styles.stateContainer}>{children}</AtlasScreen>;
}

const styles = StyleSheet.create({
  container: { backgroundColor: '#FFF' },
  topRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 22 },
  historyButton: { minHeight: 44, paddingHorizontal: 16, borderRadius: 14, borderWidth: 1, borderColor: '#DEE5E1', backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center' },
  historyText: { fontSize: 12, fontWeight: '800', color: GREEN },
  pageTitle: { fontFamily: 'Georgia', fontSize: 34, lineHeight: 40, fontWeight: '800', letterSpacing: -0.5, color: DARK },
  pageLead: { marginTop: 4, fontSize: 15, lineHeight: 22, color: MUTED },
  freshness: { marginTop: 8, fontSize: 11.5, lineHeight: 17, fontWeight: '700', color: '#74817B' },
  bestMoveCard: { marginTop: 20, borderRadius: 20, borderWidth: 1, borderColor: '#DDEBE3', backgroundColor: SOFT_MINT, padding: 18, shadowColor: '#173B2A', shadowOpacity: 0.055, shadowRadius: 16, shadowOffset: { width: 0, height: 8 }, elevation: 2 },
  bestMoveHeader: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  bestMoveIcon: { width: 34, height: 34, borderRadius: 17, backgroundColor: '#DCEFE4', alignItems: 'center', justifyContent: 'center' },
  bestMoveIconText: { color: GREEN, fontSize: 17, fontWeight: '900' },
  bestMoveLabel: { fontSize: 12, letterSpacing: .3, fontWeight: '900', color: GREEN },
  bestMoveTitle: { marginTop: 15, fontFamily: 'Georgia', fontSize: 26, lineHeight: 32, fontWeight: '800', color: DARK },
  bestMoveReason: { marginTop: 10, fontSize: 14, lineHeight: 21, color: '#46544D' },
  metricsRow: { flexDirection: 'row', gap: 8, marginTop: 18 },
  metric: { flex: 1, minHeight: 82, borderRadius: 14, padding: 11, justifyContent: 'space-between', borderWidth: 1 },
  metricImpact: { backgroundColor: '#EAF6EE', borderColor: '#D6EBDD' },
  metricEffort: { backgroundColor: SOFT_AMBER, borderColor: '#F2E5BE' },
  metricConfidence: { backgroundColor: SOFT_LAVENDER, borderColor: '#E8DFF0' },
  metricLabel: { fontSize: 9.5, lineHeight: 14, fontWeight: '800', color: '#69756F' },
  metricValue: { marginTop: 7, fontSize: 12.5, lineHeight: 17, fontWeight: '900', color: INK },
  actionRow: { flexDirection: 'row', gap: 10, marginTop: 18 },
  applyButton: { flex: 1.4, minHeight: 54, borderRadius: 14, backgroundColor: GREEN_BRIGHT, alignItems: 'center', justifyContent: 'center', shadowColor: '#00633F', shadowOpacity: 0.13, shadowRadius: 8, shadowOffset: { width: 0, height: 4 }, elevation: 2 },
  applyText: { color: '#FFF', fontSize: 15, fontWeight: '900' },
  whyButton: { flex: 1, minHeight: 54, borderRadius: 14, borderWidth: 1, borderColor: '#CFDFEA', backgroundColor: SOFT_BLUE, alignItems: 'center', justifyContent: 'center' },
  whyText: { color: '#315D74', fontSize: 14, fontWeight: '900' },
  quietActionRow: { flexDirection: 'row', gap: 10, marginTop: 10 },
  smallAction: { flex: 1, minHeight: 46, borderRadius: 13, borderWidth: 1, borderColor: '#DDE5E1', backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center' },
  smallActionText: { fontSize: 12, fontWeight: '800', color: '#53605A' },
  supportCard: { marginTop: 14, borderRadius: 16, borderWidth: 1, padding: 15 },
  supportBlue: { backgroundColor: SOFT_BLUE, borderColor: '#DCE8F1' },
  supportRow: { flexDirection: 'row', alignItems: 'center' },
  supportEyebrow: { fontSize: 11, fontWeight: '900', color: '#315D74', marginBottom: 4 },
  supportText: { maxWidth: 310, fontSize: 12.5, lineHeight: 19, color: '#536770' },
  supportLink: { alignSelf: 'flex-start', minHeight: 44, marginTop: 7, justifyContent: 'center', paddingRight: 12 },
  supportLinkText: { fontSize: 12.5, fontWeight: '900', color: GREEN },
  pressed: { opacity: .92, transform: [{ scale: .99 }] },
  disabled: { opacity: .55 },
  stateContainer: { backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'flex-start' },
  loadingOrb: { width: 62, height: 62, borderRadius: 31, backgroundColor: '#EEF8F2', alignItems: 'center', justifyContent: 'center', marginTop: 48, marginBottom: 20 },
  stateIcon: { width: 66, height: 66, borderRadius: 33, backgroundColor: '#EEF8F2', alignItems: 'center', justifyContent: 'center', marginTop: 48, marginBottom: 20 },
  stateIconText: { color: GREEN, fontFamily: 'Georgia', fontSize: 29, fontWeight: '800' },
  stateEyebrow: { fontSize: 10, letterSpacing: 1.1, fontWeight: '900', color: GREEN, marginBottom: 9 },
  stateTitle: { maxWidth: 320, textAlign: 'center', fontFamily: 'Georgia', fontSize: 28, lineHeight: 35, fontWeight: '800', color: DARK },
  stateBody: { maxWidth: 320, marginTop: 12, textAlign: 'center', fontSize: 14, lineHeight: 21, color: MUTED },
  noteCard: { width: '100%', marginTop: 20, borderRadius: 16, borderWidth: 1, padding: 15 },
  noteMint: { backgroundColor: SOFT_MINT, borderColor: '#DDEBE3' },
  noteBlue: { backgroundColor: SOFT_BLUE, borderColor: '#DCE8F1' },
  noteAmber: { backgroundColor: SOFT_AMBER, borderColor: '#F2E5BE' },
  noteTitle: { fontSize: 12, fontWeight: '900', color: DARK, marginBottom: 5 },
  noteText: { fontSize: 12.5, lineHeight: 19, color: MUTED },
  primaryButton: { marginTop: 18, minHeight: 55, width: '100%', borderRadius: 14, backgroundColor: GREEN_BRIGHT, alignItems: 'center', justifyContent: 'center', flexDirection: 'row', shadowColor: '#00633F', shadowOpacity: 0.12, shadowRadius: 8, shadowOffset: { width: 0, height: 4 }, elevation: 2, paddingHorizontal: 20 },
  primaryText: { color: '#FFF', fontSize: 15, fontWeight: '900', textAlign: 'center' },
  secondaryWide: { marginTop: 10, minHeight: 52, width: '100%', borderRadius: 14, borderWidth: 1, borderColor: '#DEE5E1', backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center', flexDirection: 'row' },
  secondaryText: { fontSize: 13, fontWeight: '800', color: GREEN },
});
