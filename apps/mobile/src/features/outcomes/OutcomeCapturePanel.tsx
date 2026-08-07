import { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import {
  getActionDecisionState,
  getOutcome,
  saveOutcome,
  type Outcome,
  type OutcomeEvidenceClass,
} from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { tokens } from '@/theme/tokens';

const evidenceClasses: { value: OutcomeEvidenceClass; label: string }[] = [
  { value: 'owner-reported', label: 'Owner reported' },
  { value: 'measured', label: 'Measured' },
  { value: 'estimated', label: 'Estimated' },
  { value: 'unknown', label: 'Unknown' },
];

export function OutcomeCapturePanel({ opportunityId }: { opportunityId: string }) {
  const [eligible, setEligible] = useState(false);
  const [loading, setLoading] = useState(true);
  const [refreshKey, setRefreshKey] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState<Outcome | null>(null);
  const [usefulnessRating, setUsefulnessRating] = useState(4);
  const [resultSummary, setResultSummary] = useState('');
  const [timeSpentMinutes, setTimeSpentMinutes] = useState('');
  const [ownerNotes, setOwnerNotes] = useState('');
  const [evidenceClass, setEvidenceClass] = useState<OutcomeEvidenceClass>('owner-reported');
  const [measureName, setMeasureName] = useState('');
  const [measureValue, setMeasureValue] = useState('');
  const [measureUnit, setMeasureUnit] = useState('');
  const [followUpAt, setFollowUpAt] = useState('');

  useEffect(() => {
    let active = true;
    void loadSession().then(async (session) => {
      if (!active) return;
      if (!session?.businessId) { setLoading(false); return; }
      try {
        const actionState = await getActionDecisionState(session.accessToken, session.businessId, opportunityId);
        if (!active) return;
        const canCapture = actionState.currentStatus === 'completed';
        setEligible(canCapture);
        setError(null);
        if (canCapture) {
          try {
            const outcome = await getOutcome(session.accessToken, session.businessId, opportunityId);
            if (!active) return;
            setSaved(outcome);
            setUsefulnessRating(outcome.usefulnessRating);
            setResultSummary(outcome.resultSummary);
            setTimeSpentMinutes(String(outcome.timeSpentMinutes));
            setOwnerNotes(outcome.ownerNotes ?? '');
            setEvidenceClass(outcome.evidenceClass);
            setMeasureName(outcome.measureName ?? '');
            setMeasureValue(outcome.measureValue == null ? '' : String(outcome.measureValue));
            setMeasureUnit(outcome.measureUnit ?? '');
            setFollowUpAt(outcome.followUpAt ?? '');
          } catch {
            // No Outcome exists yet; the empty capture form is the expected state.
          }
        }
      } catch {
        if (active) setError('Outcome status could not be loaded safely.');
      } finally {
        if (active) setLoading(false);
      }
    });
    return () => { active = false; };
  }, [opportunityId, refreshKey]);

  const submit = async () => {
    const minutes = Number.parseInt(timeSpentMinutes || '0', 10);
    const numericMeasure = measureValue.trim() ? Number(measureValue) : undefined;
    if (!resultSummary.trim()) { setError('Add the result you observed.'); return; }
    if (!Number.isFinite(minutes) || minutes < 0) { setError('Time spent must be zero or more minutes.'); return; }
    if (evidenceClass === 'measured' && (!measureName.trim() || !Number.isFinite(numericMeasure))) {
      setError('Measured outcomes require a measure name and numeric value.'); return;
    }

    setBusy(true);
    setError(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) throw new Error('Business unavailable');
      const outcome = await saveOutcome(session.accessToken, session.businessId, opportunityId, {
        usefulnessRating,
        resultSummary: resultSummary.trim(),
        timeSpentMinutes: minutes,
        ownerNotes: ownerNotes.trim() || undefined,
        measureName: measureName.trim() || undefined,
        measureValue: Number.isFinite(numericMeasure) ? numericMeasure : undefined,
        measureUnit: measureUnit.trim() || undefined,
        evidenceClass,
        followUpAt: followUpAt.trim() || undefined,
        version: saved?.version,
      });
      setSaved(outcome);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Outcome could not be saved.');
    } finally {
      setBusy(false);
    }
  };

  if (loading) return <View style={styles.card}><Text accessibilityLiveRegion="polite">Loading Outcome capture…</Text></View>;
  if (!eligible) return <View style={styles.card}><Text style={styles.cardTitle}>Outcome</Text><Text style={styles.body}>Complete the Action before recording what happened.</Text>{error ? <Text style={styles.error}>{error}</Text> : null}<Pressable accessibilityRole="button" onPress={() => setRefreshKey((value) => value + 1)} style={styles.button}><Text style={styles.buttonText}>Check completed status</Text></Pressable></View>;

  return (
    <View style={styles.card}>
      <Text style={styles.cardTitle}>Outcome</Text>
      <Text style={styles.body}>Record what happened without claiming causation. Atlas keeps the evidence class with the result.</Text>

      <Text style={styles.label}>Usefulness</Text>
      <View style={styles.row}>{[1, 2, 3, 4, 5].map((value) => <Pressable key={value} accessibilityRole="button" onPress={() => setUsefulnessRating(value)} style={value === usefulnessRating ? styles.selected : styles.button}><Text style={styles.buttonText}>{value}</Text></Pressable>)}</View>

      <Text style={styles.label}>Result observed</Text>
      <TextInput accessibilityLabel="Result observed" multiline maxLength={1000} value={resultSummary} onChangeText={setResultSummary} placeholder="What happened after you completed the Action?" style={styles.input} />

      <Text style={styles.label}>Time spent in minutes</Text>
      <TextInput accessibilityLabel="Time spent in minutes" keyboardType="number-pad" value={timeSpentMinutes} onChangeText={setTimeSpentMinutes} placeholder="0" style={styles.singleInput} />

      <Text style={styles.label}>Evidence class</Text>
      <View style={styles.wrap}>{evidenceClasses.map((item) => <Pressable key={item.value} accessibilityRole="button" onPress={() => setEvidenceClass(item.value)} style={item.value === evidenceClass ? styles.selected : styles.button}><Text style={styles.buttonText}>{item.label}</Text></Pressable>)}</View>

      {evidenceClass === 'measured' ? <View style={styles.form}>
        <TextInput accessibilityLabel="Measure name" value={measureName} onChangeText={setMeasureName} placeholder="Measure name, e.g. bookings" style={styles.singleInput} />
        <TextInput accessibilityLabel="Measure value" keyboardType="decimal-pad" value={measureValue} onChangeText={setMeasureValue} placeholder="Value" style={styles.singleInput} />
        <TextInput accessibilityLabel="Measure unit" value={measureUnit} onChangeText={setMeasureUnit} placeholder="Unit, e.g. count or EUR" style={styles.singleInput} />
      </View> : null}

      <TextInput accessibilityLabel="Optional owner notes" multiline maxLength={2000} value={ownerNotes} onChangeText={setOwnerNotes} placeholder="Optional notes" style={styles.input} />
      <TextInput accessibilityLabel="Optional follow-up date" value={followUpAt} onChangeText={setFollowUpAt} placeholder="Optional ISO date, e.g. 2026-08-14T09:00:00Z" style={styles.singleInput} />

      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
      {saved ? <Text accessibilityLiveRegion="polite" style={styles.supporting}>Saved {new Date(saved.updatedAt).toLocaleString()} · {saved.knowledgePackKey} v{saved.knowledgePackVersion}</Text> : null}
      <Pressable accessibilityRole="button" disabled={busy} onPress={() => void submit()} style={styles.primary}><Text style={styles.primaryText}>{busy ? 'Saving…' : saved ? 'Update Outcome' : 'Save Outcome'}</Text></Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  label: { fontSize: 14, fontWeight: '700' },
  supporting: { fontSize: 14, lineHeight: 20 },
  form: { gap: tokens.spacing.sm },
  row: { flexDirection: 'row', gap: tokens.spacing.sm },
  wrap: { flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  button: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  selected: { minHeight: 44, borderWidth: 2, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { fontWeight: '700' },
  input: { minHeight: 92, borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, textAlignVertical: 'top', fontSize: tokens.typography.body },
  singleInput: { minHeight: 48, borderWidth: 1, borderRadius: tokens.radius.md, paddingHorizontal: tokens.spacing.md, fontSize: tokens.typography.body },
  primary: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  primaryText: { color: '#fff', fontWeight: '700' },
  error: { fontSize: 14, lineHeight: 20, fontWeight: '700' },
});
