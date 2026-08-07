import { useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import {
  getActionDecisionState,
  recordActionDecision,
  type ActionDecisionState,
  type ActionReasonCode,
  type ActionStatus,
} from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { tokens } from '@/theme/tokens';

const reasonOptions: { value: ActionReasonCode; label: string }[] = [
  { value: 'timing-not-right', label: 'Timing is not right' },
  { value: 'already-done', label: 'Already done' },
  { value: 'insufficient-capacity', label: 'Not enough capacity' },
  { value: 'not-a-priority', label: 'Not a priority' },
  { value: 'context-incorrect', label: 'Context is incorrect' },
  { value: 'recommendation-not-relevant', label: 'Recommendation is not relevant' },
  { value: 'unsafe-or-inappropriate', label: 'Unsafe or inappropriate' },
  { value: 'other', label: 'Other' },
];

function optionsFor(status: string): ActionStatus[] {
  if (status === 'available') return ['applied', 'skipped', 'not-relevant', 'rejected'];
  if (status === 'applied') return ['completed', 'skipped', 'not-relevant', 'rejected'];
  return [];
}

function labelFor(status: ActionStatus): string {
  return ({ applied: 'Apply', completed: 'Complete', skipped: 'Skip', 'not-relevant': 'Not relevant', rejected: 'Reject' })[status];
}

export function ActionDecisionPanel({ opportunityId }: { opportunityId: string }) {
  const [state, setState] = useState<ActionDecisionState | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pendingStatus, setPendingStatus] = useState<ActionStatus | null>(null);
  const [reasonCode, setReasonCode] = useState<ActionReasonCode | null>(null);
  const [ownerNote, setOwnerNote] = useState('');

  const availableStatuses = useMemo(() => optionsFor(state?.currentStatus ?? ''), [state?.currentStatus]);
  const requiresReason = pendingStatus === 'skipped' || pendingStatus === 'not-relevant' || pendingStatus === 'rejected';

  useEffect(() => {
    let active = true;

    void loadSession()
      .then(async (session) => {
        if (!session?.businessId) throw new Error('Business unavailable');
        return getActionDecisionState(session.accessToken, session.businessId, opportunityId);
      })
      .then((next) => {
        if (!active) return;
        setState(next);
        setError(null);
      })
      .catch(() => {
        if (!active) return;
        setState(null);
        setError('Action status could not be loaded safely.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => { active = false; };
  }, [opportunityId]);

  const submit = async () => {
    if (!pendingStatus || !state) return;
    if (requiresReason && !reasonCode) { setError('Choose a reason before continuing.'); return; }
    if (reasonCode === 'other' && !ownerNote.trim()) { setError('Add a short note for Other.'); return; }

    setBusy(true);
    setError(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) throw new Error('Business unavailable');
      const next = await recordActionDecision(
        session.accessToken,
        session.businessId,
        opportunityId,
        state,
        pendingStatus,
        requiresReason ? reasonCode ?? undefined : undefined,
        ownerNote.trim() || undefined,
      );
      setState(next);
      setPendingStatus(null);
      setReasonCode(null);
      setOwnerNote('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Action status could not be updated.');
    } finally {
      setBusy(false);
    }
  };

  if (loading) return <View style={styles.card}><Text accessibilityLiveRegion="polite">Loading Action status…</Text></View>;
  if (!state) return <View style={styles.card}><Text style={styles.cardTitle}>Action status</Text><Text style={styles.body}>{error ?? 'Unavailable.'}</Text></View>;

  return (
    <View style={styles.card}>
      <Text style={styles.cardTitle}>Action status</Text>
      <Text style={styles.status}>Current: {state.currentStatus}</Text>
      {state.decisions.length > 0 ? (
        <Text style={styles.supporting}>Last updated {new Date(state.decisions[state.decisions.length - 1].decidedAt).toLocaleString()}</Text>
      ) : <Text style={styles.supporting}>No owner decision recorded yet.</Text>}

      {availableStatuses.length > 0 ? (
        <View style={styles.actions}>
          {availableStatuses.map((status) => (
            <Pressable
              key={status}
              accessibilityRole="button"
              disabled={busy}
              onPress={() => { setPendingStatus(status); setReasonCode(null); setOwnerNote(''); setError(null); }}
              style={pendingStatus === status ? styles.selectedButton : styles.button}
            >
              <Text style={pendingStatus === status ? styles.selectedButtonText : styles.buttonText}>{labelFor(status)}</Text>
            </Pressable>
          ))}
        </View>
      ) : <Text style={styles.supporting}>This Action is in a terminal state and cannot be changed.</Text>}

      {pendingStatus ? (
        <View style={styles.form}>
          <Text style={styles.label}>Record: {labelFor(pendingStatus)}</Text>
          {requiresReason ? (
            <>
              <Text style={styles.supporting}>Choose the reason that best explains your decision.</Text>
              <View style={styles.reasonGrid}>
                {reasonOptions.map((reason) => (
                  <Pressable
                    key={reason.value}
                    accessibilityRole="button"
                    onPress={() => setReasonCode(reason.value)}
                    style={reasonCode === reason.value ? styles.selectedReason : styles.reason}
                  >
                    <Text style={styles.buttonText}>{reason.label}</Text>
                  </Pressable>
                ))}
              </View>
            </>
          ) : null}
          {(requiresReason || pendingStatus === 'completed') ? (
            <TextInput
              accessibilityLabel="Optional owner note"
              multiline
              maxLength={1000}
              placeholder={reasonCode === 'other' ? 'Tell Atlas why' : 'Optional note'}
              value={ownerNote}
              onChangeText={setOwnerNote}
              style={styles.input}
            />
          ) : null}
          {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
          <View style={styles.actions}>
            <Pressable accessibilityRole="button" disabled={busy} onPress={() => void submit()} style={styles.primary}>
              <Text style={styles.primaryText}>{busy ? 'Saving…' : 'Confirm'}</Text>
            </Pressable>
            <Pressable accessibilityRole="button" disabled={busy} onPress={() => { setPendingStatus(null); setReasonCode(null); setOwnerNote(''); setError(null); }} style={styles.button}>
              <Text style={styles.buttonText}>Cancel</Text>
            </Pressable>
          </View>
        </View>
      ) : error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  status: { fontSize: 16, lineHeight: 22, fontWeight: '700', textTransform: 'capitalize' },
  actions: { flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  form: { gap: tokens.spacing.sm, paddingTop: tokens.spacing.sm },
  label: { fontSize: 15, fontWeight: '700' },
  button: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  selectedButton: { minHeight: 44, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md, backgroundColor: '#111827' },
  buttonText: { fontWeight: '700' },
  selectedButtonText: { color: '#fff', fontWeight: '700' },
  primary: { minHeight: 46, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md, backgroundColor: '#111827' },
  primaryText: { color: '#fff', fontWeight: '700' },
  reasonGrid: { gap: tokens.spacing.sm },
  reason: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  selectedReason: { minHeight: 44, borderWidth: 2, borderRadius: tokens.radius.md, justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  input: { minHeight: 90, borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, textAlignVertical: 'top', fontSize: tokens.typography.body },
  error: { fontSize: 14, lineHeight: 20, fontWeight: '700' },
});
