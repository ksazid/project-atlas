import { useCallback, useEffect, useMemo, useState, type ComponentProps, type ReactNode } from 'react';
import { ActivityIndicator, StyleSheet, Text, TextInput, View } from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import { BrandMark } from '@/components/BrandMark';
import {
  addPilotSupportNote,
  correctPilotProfile,
  getPilotBusiness,
  PilotOperationsAccessError,
  preparePilotOpportunity,
  previewPilotOpportunity,
  withdrawOpportunity,
  type PilotBusinessDetail,
  type PilotOpportunity,
  type PilotOpportunityCandidate,
  type PilotProfileCorrectionInput,
} from './pilot-operations-api';
import { boundedReasonError, canWithdraw, formatPilotDate, generationLabel, withdrawalReasonError, type PilotScreenState } from './pilot-operations-model';
import { tokens } from '@/theme/tokens';

type ProfileDraft = Omit<PilotProfileCorrectionInput, 'reason'>;
const EMPTY_PROFILE: ProfileDraft = { description: '', address: '', website: '', phone: '', email: '', socialChannels: '', businessHours: '', language: 'en' };

export function PilotBusinessReviewScreen() {
  const params = useLocalSearchParams<{ businessId?: string | string[] }>();
  const businessId = Array.isArray(params.businessId) ? params.businessId[0] : params.businessId;
  const [state, setState] = useState<PilotScreenState>('loading');
  const [detail, setDetail] = useState<PilotBusinessDetail | null>(null);
  const [candidate, setCandidate] = useState<PilotOpportunityCandidate | null>(null);
  const [supportNote, setSupportNote] = useState('');
  const [profile, setProfile] = useState<ProfileDraft>(EMPTY_PROFILE);
  const [correctionReason, setCorrectionReason] = useState('');
  const [preparationReason, setPreparationReason] = useState('');
  const [withdrawTarget, setWithdrawTarget] = useState<PilotOpportunity | null>(null);
  const [withdrawalReason, setWithdrawalReason] = useState('');
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!businessId) { setState('error'); return; }
    setState('loading'); setMessage(null);
    try {
      const session = await loadSession();
      if (!session?.accessToken) { setState('forbidden'); return; }
      const [business, preview] = await Promise.all([
        getPilotBusiness(session.accessToken, businessId),
        previewPilotOpportunity(session.accessToken, businessId),
      ]);
      setDetail(business); setCandidate(preview);
      setProfile({
        description: business.profile?.description ?? '', address: business.profile?.address ?? '', website: business.profile?.website ?? '',
        phone: business.profile?.phone ?? '', email: business.profile?.email ?? '', socialChannels: business.profile?.socialChannels ?? '',
        businessHours: business.profile?.businessHours ?? '', language: business.profile?.language ?? 'en',
      });
      setState('ready');
    } catch (error) {
      setState(error instanceof PilotOperationsAccessError ? 'forbidden' : 'error');
    }
  }, [businessId]);

  useEffect(() => { void load(); }, [load]);
  const currentOpportunity = useMemo(() => detail?.opportunities.find((value) => canWithdraw(value)) ?? null, [detail]);

  async function withSession(action: (token: string) => Promise<void>) {
    const session = await loadSession();
    if (!session?.accessToken) { setState('forbidden'); return; }
    await action(session.accessToken);
  }

  async function saveSupportNote() {
    const error = boundedReasonError(supportNote, 'Support note');
    if (error || !businessId) { setMessage(error ?? 'Business is unavailable.'); return; }
    setBusy('note'); setMessage(null);
    try {
      await withSession(async (token) => { await addPilotSupportNote(token, businessId, supportNote.trim()); });
      setSupportNote(''); setMessage('Support note recorded.'); await load();
    } catch { setMessage('Support note could not be recorded. Try again.'); } finally { setBusy(null); }
  }

  async function saveProfileAssistance() {
    const error = boundedReasonError(correctionReason, 'Correction reason');
    if (error || !businessId) { setMessage(error ?? 'Business is unavailable.'); return; }
    setBusy('profile'); setMessage(null);
    try {
      await withSession(async (token) => { await correctPilotProfile(token, businessId, { ...profile, reason: correctionReason.trim() }); });
      setCorrectionReason(''); setMessage('Profile assistance saved. Owner confirmation is required.'); await load();
    } catch { setMessage('Profile assistance could not be saved. Try again.'); } finally { setBusy(null); }
  }

  async function prepareRecommendation() {
    const error = boundedReasonError(preparationReason, 'Preparation reason');
    if (error || !businessId || !candidate) { setMessage(error ?? 'No eligible recommendation candidate is ready.'); return; }
    setBusy('prepare'); setMessage(null);
    try {
      await withSession(async (token) => {
        await preparePilotOpportunity(token, businessId, { patternKey: candidate.patternKey, bundleFingerprint: candidate.bundleFingerprint, reason: preparationReason.trim() });
      });
      setPreparationReason(''); setMessage('Recommendation prepared from current Atlas evidence.'); await load();
    } catch { setMessage('Recommendation could not be prepared. Refresh and review the evidence again.'); } finally { setBusy(null); }
  }

  async function confirmWithdrawal() {
    const error = withdrawalReasonError(withdrawalReason);
    if (error || !businessId || !withdrawTarget) { setMessage(error ?? 'No withdrawable recommendation is selected.'); return; }
    setBusy('withdraw'); setMessage(null);
    try {
      await withSession(async (token) => {
        await withdrawOpportunity(token, businessId, withdrawTarget.id, { reason: withdrawalReason.trim(), version: withdrawTarget.concurrencyVersion });
      });
      setWithdrawTarget(null); setWithdrawalReason(''); setMessage('Recommendation withdrawn and audit history recorded.'); await load();
    } catch { setMessage('Withdrawal could not be completed. Refresh before trying again.'); } finally { setBusy(null); }
  }

  if (state === 'loading') return <ReviewState title="Pilot review" copy="Loading evidence and operator history…" busy />;
  if (state === 'forbidden') return <ReviewState title="Pilot review" copy="You do not have access to this internal workspace." />;
  if (state === 'error' || !detail) return <ReviewState title="Pilot review unavailable" copy="No business data was changed. Try loading the review again." onRetry={() => void load()} />;

  return (
    <AtlasScreen contentStyle={styles.screen} showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled">
      <View style={styles.content}>
        <View style={styles.header}><BrandMark size={44} /><Text style={styles.eyebrow}>PILOT REVIEW</Text><Text accessibilityRole="header" style={styles.title}>{detail.business.name}</Text><Text style={styles.subtitle}>{detail.business.category} · {detail.business.primaryLocation}</Text></View>
        {message ? <View style={styles.notice}><Text style={styles.noticeText}>{message}</Text></View> : null}

        <Section title="Readiness">
          <Metric label="Goals" value={String(detail.goalCount)} /><Metric label="Context entries" value={String(detail.contextEntryCount)} /><Metric label="Owner-confirmed profile" value={detail.profile?.ownerConfirmed ? 'Yes' : 'No'} />
        </Section>

        <Section title="Generation diagnostics">
          {detail.generationHistory.length === 0 ? <Text style={styles.muted}>No generation diagnostics recorded yet.</Text> : detail.generationHistory.slice(0, 5).map((run) => <View key={run.id} style={styles.row}><Text style={styles.rowStrong}>{generationLabel(run.outcome, run.code)}</Text><Text style={styles.muted}>{run.candidateCount} candidates · {formatPilotDate(run.occurredAt)}</Text></View>)}
        </Section>

        <Section title="Owner feedback">
          {detail.feedback.length === 0 ? <Text style={styles.muted}>No recent owner feedback.</Text> : detail.feedback.slice(0, 6).map((feedback) => <View key={feedback.id} style={styles.row}><Text style={feedback.kind === 'unsafe-guidance' ? styles.dangerText : styles.rowStrong}>{feedback.kind === 'unsafe-guidance' ? 'Unsafe guidance' : feedback.kind.replaceAll('-', ' ')}</Text><Text style={styles.muted}>{feedback.message || 'No note'} · {formatPilotDate(feedback.createdAt)}</Text></View>)}
        </Section>

        <Section title="Support note">
          <Text style={styles.muted}>Append a bounded internal note. It does not impersonate or alter the owner.</Text>
          <Input label="Support note" value={supportNote} onChangeText={setSupportNote} multiline placeholder="What should the next operator know?" />
          <Action label={busy === 'note' ? 'Saving…' : 'Add support note'} disabled={busy !== null} onPress={() => void saveSupportNote()} />
        </Section>

        <Section title="Profile assistance">
          <Text style={styles.muted}>Operator-assisted changes are marked as such and require the owner to confirm them before Atlas treats them as owner-confirmed context.</Text>
          <Input label="Description" value={profile.description ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, description: value }))} multiline />
          <Input label="Address" value={profile.address ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, address: value }))} />
          <Input label="Website" value={profile.website ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, website: value }))} autoCapitalize="none" />
          <Input label="Phone" value={profile.phone ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, phone: value }))} />
          <Input label="Email" value={profile.email ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, email: value }))} autoCapitalize="none" />
          <Input label="Social channels" value={profile.socialChannels ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, socialChannels: value }))} />
          <Input label="Business hours" value={profile.businessHours ?? ''} onChangeText={(value) => setProfile((current) => ({ ...current, businessHours: value }))} />
          <Input label="Language" value={profile.language} onChangeText={(value) => setProfile((current) => ({ ...current, language: value }))} autoCapitalize="none" />
          <Input label="Correction reason" value={correctionReason} onChangeText={setCorrectionReason} multiline placeholder="Why is operator assistance needed?" />
          <Action label={busy === 'profile' ? 'Saving…' : 'Save profile assistance'} disabled={busy !== null} onPress={() => void saveProfileAssistance()} />
        </Section>

        <Section title="Recommendation preparation">
          {candidate ? <><Text style={styles.recommendationTitle}>{candidate.title}</Text><Text style={styles.muted}>{candidate.evidenceCount} evidence items · {candidate.confidence} confidence · {candidate.effort} effort</Text><Input label="Preparation reason" value={preparationReason} onChangeText={setPreparationReason} multiline placeholder="Why should this candidate be prepared now?" /><Action label={busy === 'prepare' ? 'Preparing…' : 'Prepare recommendation'} disabled={busy !== null || Boolean(currentOpportunity)} onPress={() => void prepareRecommendation()} /></> : <Text style={styles.muted}>No eligible evidence-backed recommendation candidate is ready.</Text>}
        </Section>

        <Section title="Current recommendation">
          {currentOpportunity ? <><Text style={styles.recommendationTitle}>{currentOpportunity.title}</Text><Text style={styles.muted}>{currentOpportunity.confidence} confidence · expires {formatPilotDate(currentOpportunity.expiresAt)}</Text>{withdrawTarget?.id === currentOpportunity.id ? <View style={styles.dangerPanel}><Text style={styles.dangerTitle}>Confirm terminal withdrawal</Text><Text style={styles.muted}>Withdrawal removes this recommendation from owner action eligibility and records the operator reason.</Text><Input label="Withdrawal reason" value={withdrawalReason} onChangeText={setWithdrawalReason} multiline placeholder="Why must this recommendation be withdrawn?" /><Action danger label={busy === 'withdraw' ? 'Withdrawing…' : 'Confirm withdrawal'} disabled={busy !== null} onPress={() => void confirmWithdrawal()} /><Action secondary label="Cancel" disabled={busy !== null} onPress={() => { setWithdrawTarget(null); setWithdrawalReason(''); }} /></View> : <Action danger label="Withdraw recommendation" disabled={busy !== null} onPress={() => setWithdrawTarget(currentOpportunity)} />}</> : <Text style={styles.muted}>There is no active recommendation to withdraw.</Text>}
        </Section>

        <Section title="Operator history">
          {detail.operations.length === 0 ? <Text style={styles.muted}>No operator interventions recorded.</Text> : detail.operations.slice(0, 8).map((operation) => <View key={operation.id} style={styles.row}><Text style={styles.rowStrong}>{operation.action.replaceAll('-', ' ')}</Text><Text style={styles.muted}>{operation.reason || 'No reason'} · {formatPilotDate(operation.occurredAt)}</Text></View>)}
        </Section>
      </View>
    </AtlasScreen>
  );
}

function ReviewState({ title, copy, busy = false, onRetry }: { title: string; copy: string; busy?: boolean; onRetry?: () => void }) { return <AtlasScreen mode="static" contentStyle={styles.state}><BrandMark size={54} />{busy ? <ActivityIndicator color={tokens.color.green} /> : null}<Text accessibilityRole="header" style={styles.stateTitle}>{title}</Text><Text style={styles.stateCopy}>{copy}</Text>{onRetry ? <Action label="Try again" onPress={onRetry} /> : null}</AtlasScreen>; }
function Section({ title, children }: { title: string; children: ReactNode }) { return <View style={styles.section}><Text style={styles.sectionTitle}>{title}</Text>{children}</View>; }
function Metric({ label, value }: { label: string; value: string }) { return <View style={styles.metric}><Text style={styles.muted}>{label}</Text><Text style={styles.metricValue}>{value}</Text></View>; }
function Input({ label, ...props }: { label: string } & ComponentProps<typeof TextInput>) { return <View style={styles.inputGroup}><Text style={styles.label}>{label}</Text><TextInput accessibilityLabel={label} placeholderTextColor="#7A8680" style={[styles.input, props.multiline && styles.inputMultiline]} {...props} /></View>; }
function Action({ label, onPress, disabled = false, danger = false, secondary = false }: { label: string; onPress: () => void; disabled?: boolean; danger?: boolean; secondary?: boolean }) { return <AtlasPressable accessibilityRole="button" accessibilityLabel={label} disabled={disabled} onPress={onPress} style={[styles.action, secondary && styles.secondaryAction, danger && styles.dangerAction, disabled && styles.disabled]}><Text style={[styles.actionText, secondary && styles.secondaryText]}>{label}</Text></AtlasPressable>; }

const styles = StyleSheet.create({
  screen: { alignItems: 'center', backgroundColor: tokens.color.surface }, content: { width: '100%', maxWidth: 720, gap: 16 }, header: { gap: 6, marginBottom: 4 }, eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.2, marginTop: 5 }, title: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 31, fontWeight: '800', lineHeight: 37 }, subtitle: { color: tokens.color.muted, fontSize: 13.5 },
  section: { borderWidth: 1, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, padding: 18, gap: 12, backgroundColor: tokens.color.surface }, sectionTitle: { color: tokens.color.greenDeep, fontSize: 17, fontWeight: '900' }, row: { gap: 3, borderTopWidth: 1, borderTopColor: tokens.color.border, paddingTop: 10 }, rowStrong: { color: tokens.color.ink, fontSize: 13, fontWeight: '800', textTransform: 'capitalize' }, muted: { color: tokens.color.muted, fontSize: 12.5, lineHeight: 18 }, metric: { flexDirection: 'row', justifyContent: 'space-between', gap: 12 }, metricValue: { color: tokens.color.ink, fontSize: 13, fontWeight: '800' }, recommendationTitle: { color: tokens.color.ink, fontSize: 17, fontWeight: '800', lineHeight: 23 },
  inputGroup: { gap: 6 }, label: { color: tokens.color.ink, fontSize: 12, fontWeight: '800' }, input: { minHeight: 46, borderWidth: 1, borderColor: tokens.color.border, borderRadius: tokens.radius.md, paddingHorizontal: 13, paddingVertical: 10, color: tokens.color.ink, backgroundColor: tokens.color.surface, fontSize: 13 }, inputMultiline: { minHeight: 88, textAlignVertical: 'top' },
  action: { minHeight: 48, borderRadius: tokens.radius.pill, backgroundColor: tokens.color.green, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 18 }, actionText: { color: tokens.color.surface, fontSize: 13.5, fontWeight: '900' }, secondaryAction: { backgroundColor: tokens.color.surface, borderWidth: 1, borderColor: tokens.color.green }, secondaryText: { color: tokens.color.greenDeep }, dangerAction: { backgroundColor: tokens.color.danger }, disabled: { opacity: .5 }, dangerPanel: { gap: 11, borderRadius: tokens.radius.md, backgroundColor: tokens.color.dangerSoft, padding: 14 }, dangerTitle: { color: tokens.color.danger, fontWeight: '900', fontSize: 14 }, dangerText: { color: tokens.color.danger, fontSize: 13, fontWeight: '900' }, notice: { backgroundColor: tokens.color.mint, borderRadius: tokens.radius.md, padding: 12 }, noticeText: { color: tokens.color.greenDeep, fontSize: 12.5, fontWeight: '700' },
  state: { alignItems: 'center', justifyContent: 'center', backgroundColor: tokens.color.surface, gap: 13 }, stateTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 28, fontWeight: '800' }, stateCopy: { color: tokens.color.muted, maxWidth: 420, textAlign: 'center', fontSize: 14, lineHeight: 21 },
});
