import { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { getOpportunityDetail, submitFeedback, type FeedbackUsefulness, type OpportunityDetail } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { AtlasScreen } from '@/components/AtlasScreen';
import { ActionDecisionPanel } from '@/features/action-decisions/ActionDecisionPanel';
import { OutcomeCapturePanel } from '@/features/outcomes/OutcomeCapturePanel';
import { tokens } from '@/theme/tokens';

type State = 'loading' | 'ready' | 'missing' | 'error';
type RatingState = 'idle' | 'submitting' | 'success' | 'error';

export function OpportunityDetailScreen({ opportunityId }: { opportunityId: string }) {
  const [detail, setDetail] = useState<OpportunityDetail | null>(null);
  const [state, setState] = useState<State>('loading');
  const [ratingState, setRatingState] = useState<RatingState>('idle');
  const [selectedUsefulness, setSelectedUsefulness] = useState<FeedbackUsefulness | null>(null);
  const [ratingMessage, setRatingMessage] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void loadSession().then(async (session) => {
      if (!active) return;
      if (!session?.businessId) { setState('missing'); return; }
      try {
        const value = await getOpportunityDetail(session.accessToken, session.businessId, opportunityId);
        if (active) { setDetail(value); setState('ready'); }
      } catch {
        if (active) setState('error');
      }
    });
    return () => { active = false; };
  }, [opportunityId]);

  const rateOpportunity = async (usefulness: FeedbackUsefulness) => {
    if (ratingState === 'submitting') return;
    setSelectedUsefulness(usefulness);
    setRatingState('submitting');
    setRatingMessage(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setRatingState('error');
        setRatingMessage('Select a Business before rating this Opportunity.');
        return;
      }
      await submitFeedback(session.accessToken, session.businessId, {
        kind: 'opportunity-rating',
        opportunityId,
        usefulness,
      });
      setRatingState('success');
      setRatingMessage('Thanks. Your Opportunity feedback was recorded.');
    } catch {
      setRatingState('error');
      setRatingMessage('Could not record your feedback. Try again.');
    }
  };

  if (state === 'loading') return <AtlasScreen mode="static" contentStyle={styles.center}><Text accessibilityLiveRegion="polite">Loading Opportunity details…</Text></AtlasScreen>;
  if (state === 'missing') return <AtlasScreen mode="static" contentStyle={styles.center}><Text accessibilityRole="header" style={styles.title}>Opportunity unavailable</Text><Text style={styles.body}>Select a Business and open the Opportunity again.</Text></AtlasScreen>;
  if (state === 'error' || !detail) return <AtlasScreen mode="static" contentStyle={styles.center}><Text accessibilityRole="header" style={styles.title}>Opportunity unavailable</Text><Text style={styles.body}>This Opportunity could not be loaded safely.</Text><Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.button}><Text style={styles.buttonText}>Back</Text></Pressable></AtlasScreen>;

  const ratingBusy = ratingState === 'submitting';

  return (
    <AtlasScreen contentStyle={styles.container}>
      <Text style={styles.eyebrow}>OPPORTUNITY DETAIL</Text>
      <Text accessibilityRole="header" style={styles.title}>{detail.title}</Text>
      <Text style={styles.body}>{detail.goalAlignment}</Text>
      {detail.isExpired ? <Text accessibilityLiveRegion="polite" style={styles.warning}>This Opportunity has expired and is no longer actionable.</Text> : null}

      <ActionDecisionPanel opportunityId={opportunityId} />
      <OutcomeCapturePanel opportunityId={opportunityId} />

      <View style={styles.feedbackCard}>
        <Text style={styles.cardTitle}>Was this Opportunity useful?</Text>
        <Text style={styles.supporting}>This rating improves Atlas feedback signals. It does not change the Opportunity status or action history.</Text>
        <View style={styles.ratingRow}>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Rate Opportunity useful"
            accessibilityState={{ disabled: ratingBusy, selected: selectedUsefulness === 'useful' }}
            disabled={ratingBusy}
            onPress={() => void rateOpportunity('useful')}
            style={[styles.ratingButton, selectedUsefulness === 'useful' && styles.ratingButtonSelected, ratingBusy && styles.disabled]}
          >
            <Text style={styles.ratingText}>Useful</Text>
          </Pressable>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Rate Opportunity not useful"
            accessibilityState={{ disabled: ratingBusy, selected: selectedUsefulness === 'not-useful' }}
            disabled={ratingBusy}
            onPress={() => void rateOpportunity('not-useful')}
            style={[styles.ratingButton, selectedUsefulness === 'not-useful' && styles.ratingButtonSelected, ratingBusy && styles.disabled]}
          >
            <Text style={styles.ratingText}>Not useful</Text>
          </Pressable>
        </View>
        {ratingMessage ? <Text accessibilityLiveRegion="polite" style={ratingState === 'error' ? styles.ratingError : styles.supporting}>{ratingMessage}</Text> : null}
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Report unsafe guidance"
          onPress={() => router.push(`/feedback?kind=unsafe-guidance&opportunityId=${encodeURIComponent(opportunityId)}`)}
          style={styles.reportButton}
        >
          <Text style={styles.reportText}>Report unsafe guidance</Text>
        </Pressable>
      </View>

      <View style={styles.card}><Text style={styles.cardTitle}>Reason</Text><Text style={styles.body}>{detail.reason}</Text></View>
      <View style={styles.card}><Text style={styles.cardTitle}>Why now</Text><Text style={styles.body}>{detail.whyNow}</Text></View>

      <View style={styles.metrics}>
        <View style={styles.metric}><Text style={styles.label}>Expected impact</Text><Text style={styles.body}>{detail.expectedImpact}</Text></View>
        <View style={styles.metric}><Text style={styles.label}>Effort</Text><Text style={styles.body}>{detail.effort}</Text></View>
        <View style={styles.metric}><Text style={styles.label}>Confidence</Text><Text style={styles.body}>{detail.confidence}</Text></View>
      </View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>Evidence</Text>
        {detail.evidence.map((item) => <View key={`${item.category}-${item.label}`} style={styles.item}><Text style={styles.label}>{item.label}</Text><Text style={styles.body}>{item.value}</Text><Text style={styles.supporting}>Source: {item.source}</Text></View>)}
      </View>

      <View style={styles.card}><Text style={styles.cardTitle}>Atlas interpretation</Text><Text style={styles.body}>{detail.actionSummary}</Text></View>
      <View style={styles.card}><Text style={styles.cardTitle}>Assumptions</Text>{detail.assumptions.map((item) => <Text key={item} style={styles.body}>• {item}</Text>)}</View>
      <View style={styles.card}><Text style={styles.cardTitle}>Limitations</Text>{detail.limitations.map((item) => <Text key={item} style={styles.body}>• {item}</Text>)}</View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>Execution Kit</Text>
        <Text style={styles.body}>Review editable assets and measurement guidance before taking action. Atlas will not publish, send or execute anything externally.</Text>
        <Pressable accessibilityRole="button" disabled={!detail.executionKitAvailable || detail.isExpired} onPress={() => router.push(`/opportunities/${opportunityId}/execution-kit`)} style={styles.button}>
          <Text style={styles.buttonText}>{detail.executionKitAvailable && !detail.isExpired ? 'Open Execution Kit' : 'Execution Kit unavailable'}</Text>
        </Pressable>
      </View>
      <Text style={styles.supporting}>Expires {new Date(detail.expiresAt).toLocaleString()} · {detail.knowledgePackKey} v{detail.knowledgePackVersion}</Text>
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  container: { gap: tokens.spacing.md },
  center: { justifyContent: 'center', gap: tokens.spacing.md },
  eyebrow: { fontSize: 13, fontWeight: '700', letterSpacing: 1.2 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  warning: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, fontWeight: '700' },
  metrics: { gap: tokens.spacing.sm },
  metric: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: 4 },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  feedbackCard: { borderColor: '#DCE5DF', borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  item: { gap: 4, paddingVertical: 4 },
  label: { fontSize: 14, fontWeight: '700' },
  ratingRow: { flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  ratingButton: { minHeight: 44, borderColor: tokens.color.green, borderWidth: 1, borderRadius: tokens.radius.pill, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  ratingButtonSelected: { backgroundColor: '#EEF8F2' },
  ratingText: { color: tokens.color.greenDeep, fontWeight: '700' },
  ratingError: { color: '#8B2D2D', fontSize: 14, lineHeight: 20 },
  reportButton: { minHeight: 44, alignItems: 'flex-start', justifyContent: 'center' },
  reportText: { color: tokens.color.greenDeep, fontSize: 14, fontWeight: '700', textDecorationLine: 'underline' },
  disabled: { opacity: 0.55 },
  button: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { color: '#fff', fontWeight: '700' },
});
