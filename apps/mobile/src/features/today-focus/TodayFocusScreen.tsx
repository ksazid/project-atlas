import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { decideOpportunity, getTodayFocus, type OpportunityDecision, type TodayFocus } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { todayFocusRecoveryAction } from './today-focus-recovery';

type ScreenState = 'loading' | 'ready' | 'empty' | 'error';

const GREEN = '#00754A';
const GREEN_BRIGHT = '#008A57';
const DARK = '#0A2F25';
const INK = '#17221C';
const MUTED = '#5B6761';
const SOFT = '#F7F5F0';

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

  if (state === 'loading') {
    return <StateShell><View style={styles.loadingOrb}><ActivityIndicator color={GREEN} size="large" /></View><Text style={styles.stateEyebrow}>ATLAS IS THINKING</Text><Text accessibilityLiveRegion="polite" style={styles.stateTitle}>Finding today’s strongest action</Text><Text style={styles.stateBody}>Reviewing your business context, priorities and current signals.</Text></StateShell>;
  }

  if (state === 'empty') {
    return <StateShell><View style={styles.stateIcon}><Text style={styles.stateIconText}>◎</Text></View><Text style={styles.stateEyebrow}>TODAY’S FOCUS</Text><Text accessibilityRole="header" style={styles.stateTitle}>Your focus starts with business context.</Text><Text style={styles.stateBody}>Create or select a business so Atlas can surface one focused action instead of generic advice.</Text></StateShell>;
  }

  if (state === 'error') {
    return <StateShell><View style={styles.stateIcon}><Text style={styles.stateIconText}>!</Text></View><Text style={styles.stateEyebrow}>TODAY’S FOCUS</Text><Text accessibilityRole="header" style={styles.stateTitle}>Today’s focus is unavailable.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>Atlas could not load a safe recommendation. No action has been created.</Text><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={retry} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable></StateShell>;
  }

  if (focus?.state === 'insufficient-context') {
    const recoveryAction = todayFocusRecoveryAction(focus.code);
    return <ScrollView contentContainerStyle={styles.stateContainer}><View style={styles.stateIcon}><Text style={styles.stateIconText}>◌</Text></View><Text style={styles.stateEyebrow}>TODAY’S FOCUS</Text><Text accessibilityRole="header" style={styles.stateTitle}>No suitable focus yet.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>{focus.message}</Text><View style={styles.noteCard}><Text style={styles.noteTitle}>Why Atlas is waiting</Text><Text style={styles.noteText}>Atlas will not create filler recommendations when the available context is insufficient.</Text></View><Pressable accessibilityRole="button" accessibilityLabel={recoveryAction.label} onPress={() => router.push(recoveryAction.route)} style={styles.primaryButton}><Text style={styles.primaryText}>{recoveryAction.label}</Text></Pressable><Pressable accessibilityRole="button" onPress={() => router.push('/history')} style={styles.secondaryWide}><Text style={styles.secondaryText}>View business history</Text></Pressable></ScrollView>;
  }

  if (focus?.state === 'no-focus') {
    return <ScrollView contentContainerStyle={styles.stateContainer}><View style={styles.stateIcon}><Text style={styles.stateIconText}>◎</Text></View><Text style={styles.stateEyebrow}>TODAY’S FOCUS</Text><Text accessibilityRole="header" style={styles.stateTitle}>No evidence-qualified focus yet.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>{focus.message}</Text><View style={styles.noteCard}><Text style={styles.noteTitle}>Quality before quantity</Text><Text style={styles.noteText}>Atlas will not create filler recommendations just to fill this screen. A new focus appears only when the available evidence supports a useful action.</Text></View><Pressable accessibilityRole="button" accessibilityLabel="Review business context" onPress={() => router.push('/profile')} style={styles.primaryButton}><Text style={styles.primaryText}>Review business context</Text></Pressable><Pressable accessibilityRole="button" onPress={() => router.push('/history')} style={styles.secondaryWide}><Text style={styles.secondaryText}>View history</Text></Pressable></ScrollView>;
  }

  if (focus?.state === 'degraded') {
    return <ScrollView contentContainerStyle={styles.stateContainer}><View style={styles.stateIcon}><Text style={styles.stateIconText}>!</Text></View><Text style={styles.stateEyebrow}>TODAY’S FOCUS</Text><Text accessibilityRole="header" style={styles.stateTitle}>Atlas could not safely prepare today’s focus.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>{focus.message}</Text><View style={styles.noteCard}><Text style={styles.noteTitle}>No recommendation was created</Text><Text style={styles.noteText}>Atlas stopped safely rather than presenting guidance from incomplete or unavailable intelligence inputs.</Text></View><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={retry} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable><Pressable accessibilityRole="button" accessibilityLabel="Review business context" onPress={() => router.push('/profile')} style={styles.secondaryWide}><Text style={styles.secondaryText}>Review business context</Text></Pressable></ScrollView>;
  }

  if (focus?.state !== 'ready') {
    return <StateShell><View style={styles.stateIcon}><Text style={styles.stateIconText}>!</Text></View><Text style={styles.stateEyebrow}>TODAY’S FOCUS</Text><Text accessibilityRole="header" style={styles.stateTitle}>Today’s focus needs a refresh.</Text><Text accessibilityLiveRegion="polite" style={styles.stateBody}>Atlas did not receive a recognised focus state. No recommendation has been created.</Text><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={retry} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable></StateShell>;
  }

  const opportunity = focus.opportunity;
  return (
    <ScrollView
      contentContainerStyle={styles.container}
      showsVerticalScrollIndicator={false}
      refreshControl={<RefreshControl tintColor={GREEN} refreshing={refreshing} onRefresh={() => void load(true)} />}
    >
      <View style={styles.topRow}>
        <BrandMark size={50} />
        <Pressable accessibilityRole="button" onPress={() => router.push('/history')} style={({ pressed }) => [styles.historyButton, pressed && styles.pressed]}><Text style={styles.historyText}>History</Text></Pressable>
      </View>

      <Text style={styles.eyebrow}>TODAY’S FOCUS</Text>
      <Text accessibilityRole="header" style={styles.title}>{opportunity.title}</Text>
      <Text style={styles.lead}>{opportunity.whyItMatters}</Text>

      <View style={styles.heroCard}>
        <View style={styles.heroAccent}><Text style={styles.heroAccentIcon}>↗</Text></View>
        <View style={styles.heroCopy}>
          <Text style={styles.heroLabel}>RECOMMENDED MOVE</Text>
          <Text style={styles.heroTitle}>One action. Clear reason. Measurable outcome.</Text>
        </View>
      </View>

      <View style={styles.metricsRow}>
        <Metric label="Impact" value={opportunity.expectedImpact} />
        <Metric label="Effort" value={opportunity.effort} />
        <Metric label="Confidence" value={opportunity.confidence} />
      </View>

      <View style={styles.card}>
        <View style={styles.cardHeader}><View style={styles.cardIcon}><Text style={styles.cardIconText}>◷</Text></View><Text style={styles.cardTitle}>Why now</Text></View>
        <Text style={styles.body}>{opportunity.whyNow}</Text>
      </View>

      <View style={styles.card}>
        <View style={styles.cardHeader}><View style={styles.cardIcon}><Text style={styles.cardIconText}>◎</Text></View><Text style={styles.cardTitle}>Evidence</Text></View>
        <Text style={styles.body}>{opportunity.evidenceSummary}</Text>
        <View style={styles.interpretation}><Text style={styles.interpretationTitle}>Atlas interpretation</Text><Text style={styles.interpretationText}>Interpretation stays separate from confirmed evidence so you can judge the recommendation clearly.</Text></View>
      </View>

      <Pressable accessibilityRole="button" onPress={() => router.push(`/opportunities/${opportunity.id}`)} style={({ pressed }) => [styles.secondaryWide, pressed && styles.pressed]}><Text style={styles.secondaryText}>View full details</Text><Text style={styles.secondaryArrow}>→</Text></Pressable>
      <Text style={styles.meta}>Expires {new Date(opportunity.expiresAt).toLocaleString()} · {opportunity.knowledgePackKey} v{opportunity.knowledgePackVersion}</Text>

      <Pressable accessibilityRole="button" disabled={deciding} onPress={() => void decide('apply')} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed, deciding && styles.disabled]}>
        {deciding ? <ActivityIndicator color="#FFF" /> : <><Text style={styles.primaryText}>Apply this move</Text><Text style={styles.primaryArrow}>→</Text></>}
      </Pressable>

      <View style={styles.secondaryRow}>
        <Pressable accessibilityRole="button" disabled={deciding} onPress={() => void decide('skip')} style={({ pressed }) => [styles.smallAction, pressed && styles.pressed]}><Text style={styles.smallActionText}>Skip for now</Text></Pressable>
        <Pressable accessibilityRole="button" disabled={deciding} onPress={() => void decide('not-relevant')} style={({ pressed }) => [styles.smallAction, pressed && styles.pressed]}><Text style={styles.smallActionText}>Not relevant</Text></Pressable>
      </View>
    </ScrollView>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return <View style={styles.metric}><Text style={styles.metricLabel}>{label}</Text><Text numberOfLines={2} style={styles.metricValue}>{value}</Text></View>;
}

function StateShell({ children }: { children: React.ReactNode }) {
  return <View style={styles.stateContainer}>{children}</View>;
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, paddingHorizontal: 28, paddingTop: 58, paddingBottom: 38, backgroundColor: '#FFF' },
  topRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 28 },
  historyButton: { minHeight: 44, paddingHorizontal: 16, borderRadius: 10, borderWidth: 1, borderColor: '#DEE5E1', backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center' },
  historyText: { fontSize: 12, fontWeight: '800', color: GREEN },
  eyebrow: { fontSize: 11, letterSpacing: 1.15, fontWeight: '900', color: GREEN, marginBottom: 9 },
  title: { fontFamily: 'Georgia', fontSize: 32, lineHeight: 38, fontWeight: '800', letterSpacing: -0.5, color: DARK },
  lead: { marginTop: 13, fontSize: 14.5, lineHeight: 22, color: MUTED },
  heroCard: { marginTop: 22, minHeight: 100, borderRadius: 12, backgroundColor: '#073D31', padding: 18, flexDirection: 'row', alignItems: 'center', gap: 14, shadowColor: '#073D31', shadowOpacity: 0.12, shadowRadius: 14, shadowOffset: { width: 0, height: 8 }, elevation: 4 },
  heroAccent: { width: 48, height: 48, borderRadius: 24, backgroundColor: '#EEF8F2', alignItems: 'center', justifyContent: 'center' },
  heroAccentIcon: { fontSize: 23, fontWeight: '900', color: GREEN },
  heroCopy: { flex: 1, gap: 5 },
  heroLabel: { fontSize: 9, letterSpacing: 1.1, fontWeight: '900', color: '#58D19B' },
  heroTitle: { fontSize: 15, lineHeight: 21, fontWeight: '800', color: '#FFF' },
  metricsRow: { flexDirection: 'row', gap: 8, marginTop: 16 },
  metric: { flex: 1, minHeight: 88, borderRadius: 10, backgroundColor: SOFT, borderWidth: 1, borderColor: '#E6E3DD', padding: 12, justifyContent: 'space-between' },
  metricLabel: { fontSize: 10, fontWeight: '800', color: '#6A756F' },
  metricValue: { fontSize: 13, lineHeight: 18, fontWeight: '900', color: INK },
  card: { marginTop: 14, borderRadius: 12, borderWidth: 1, borderColor: '#E2E7E4', backgroundColor: '#FFF', padding: 16, shadowColor: '#173B2A', shadowOpacity: 0.025, shadowRadius: 6, shadowOffset: { width: 0, height: 3 }, elevation: 1 },
  cardHeader: { flexDirection: 'row', alignItems: 'center', gap: 10, marginBottom: 12 },
  cardIcon: { width: 32, height: 32, borderRadius: 16, backgroundColor: '#EEF8F2', alignItems: 'center', justifyContent: 'center' },
  cardIconText: { color: GREEN, fontSize: 15, fontWeight: '900' },
  cardTitle: { fontFamily: 'Georgia', fontSize: 18, fontWeight: '800', color: DARK },
  body: { fontSize: 13.5, lineHeight: 21, color: '#44514B' },
  interpretation: { marginTop: 14, borderRadius: 10, backgroundColor: '#EEF8F2', padding: 13 },
  interpretationTitle: { fontSize: 10, letterSpacing: .6, fontWeight: '900', color: GREEN, marginBottom: 4 },
  interpretationText: { fontSize: 12, lineHeight: 18, color: '#607069' },
  secondaryWide: { marginTop: 18, minHeight: 52, borderRadius: 10, borderWidth: 1, borderColor: '#DEE5E1', backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center', flexDirection: 'row' },
  secondaryText: { fontSize: 13, fontWeight: '800', color: GREEN },
  secondaryArrow: { position: 'absolute', right: 18, color: GREEN, fontSize: 20 },
  meta: { marginTop: 10, textAlign: 'center', fontSize: 10.5, lineHeight: 16, color: '#7B8781' },
  primaryButton: { marginTop: 18, minHeight: 55, borderRadius: 10, backgroundColor: GREEN_BRIGHT, alignItems: 'center', justifyContent: 'center', flexDirection: 'row', shadowColor: '#00633F', shadowOpacity: 0.12, shadowRadius: 8, shadowOffset: { width: 0, height: 4 }, elevation: 2, paddingHorizontal: 20 },
  primaryText: { color: '#FFF', fontSize: 15.5, fontWeight: '900', textAlign: 'center' },
  primaryArrow: { position: 'absolute', right: 18, color: '#FFF', fontSize: 22 },
  secondaryRow: { flexDirection: 'row', gap: 10, marginTop: 11 },
  smallAction: { flex: 1, minHeight: 48, borderRadius: 10, borderWidth: 1, borderColor: '#E0E5E2', backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center' },
  smallActionText: { fontSize: 12, fontWeight: '800', color: '#53605A' },
  pressed: { opacity: .92, transform: [{ scale: .99 }] },
  disabled: { opacity: .55 },
  stateContainer: { flex: 1, minHeight: '100%', backgroundColor: '#FFF', paddingHorizontal: 28, paddingVertical: 80, alignItems: 'center', justifyContent: 'center' },
  loadingOrb: { width: 86, height: 86, borderRadius: 43, backgroundColor: '#EEF8F2', alignItems: 'center', justifyContent: 'center', marginBottom: 26 },
  stateIcon: { width: 72, height: 72, borderRadius: 36, backgroundColor: '#EEF8F2', alignItems: 'center', justifyContent: 'center', marginBottom: 24 },
  stateIconText: { color: GREEN, fontFamily: 'Georgia', fontSize: 32, fontWeight: '800' },
  stateEyebrow: { fontSize: 10, letterSpacing: 1.1, fontWeight: '900', color: GREEN, marginBottom: 10 },
  stateTitle: { maxWidth: 320, textAlign: 'center', fontFamily: 'Georgia', fontSize: 30, lineHeight: 37, fontWeight: '800', color: DARK },
  stateBody: { maxWidth: 320, marginTop: 14, textAlign: 'center', fontSize: 14, lineHeight: 22, color: MUTED },
  noteCard: { width: '100%', marginTop: 22, borderRadius: 12, backgroundColor: '#F7F5F0', padding: 16 },
  noteTitle: { fontSize: 12, fontWeight: '900', color: DARK, marginBottom: 6 },
  noteText: { fontSize: 12, lineHeight: 19, color: MUTED },
});