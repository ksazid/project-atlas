import { useMemo, useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { submitFeedback } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import {
  buildFeedbackInput,
  feedbackChoices,
  getFeedbackCopy,
  validateFeedbackDraft,
  type FeedbackScreenKind,
} from '@/features/feedback/feedback-model';
import { tokens } from '@/theme/tokens';

type SubmissionState = 'idle' | 'submitting' | 'success' | 'error';

function firstParam(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function isFeedbackScreenKind(value: string | undefined): value is FeedbackScreenKind {
  return feedbackChoices.some(choice => choice.kind === value);
}

export function FeedbackScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ kind?: string | string[]; opportunityId?: string | string[] }>();
  const requestedKind = firstParam(params.kind);
  const opportunityId = firstParam(params.opportunityId);
  const [kind, setKind] = useState<FeedbackScreenKind | null>(isFeedbackScreenKind(requestedKind) ? requestedKind : null);
  const [message, setMessage] = useState('');
  const [submission, setSubmission] = useState<SubmissionState>('idle');
  const [notice, setNotice] = useState<string | null>(null);

  const copy = useMemo(() => kind ? getFeedbackCopy(kind) : null, [kind]);
  const submitting = submission === 'submitting';

  const submit = async () => {
    if (submitting) return;
    if (!kind) {
      setNotice('Choose what you want to report or ask for first.');
      return;
    }

    const draft = { kind, opportunityId, message };
    const validation = validateFeedbackDraft(draft);
    if (!validation.valid) {
      setNotice(validation.message ?? 'Review your feedback and try again.');
      return;
    }

    setSubmission('submitting');
    setNotice(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        router.replace({ pathname: '/', params: { sessionEntry: '1' } });
        return;
      }
      await submitFeedback(session.accessToken, session.businessId, buildFeedbackInput(draft));
      setSubmission('success');
      setNotice(copy?.success ?? 'Your feedback was recorded for review.');
    } catch {
      setSubmission('error');
      setNotice('Could not record your feedback. Your note is still here, so you can try again.');
    }
  };

  if (submission === 'success') {
    return (
      <AtlasScreen contentStyle={styles.container}>
        <View style={styles.successCard}>
          <Text accessibilityRole="header" style={styles.title}>Feedback recorded</Text>
          <Text accessibilityLiveRegion="polite" style={styles.body}>{notice}</Text>
          {kind === 'unsafe-guidance' ? (
            <Text style={styles.helper}>The report is recorded for review. It does not automatically remove or change the Opportunity.</Text>
          ) : null}
          <AtlasPressable accessibilityRole="button" accessibilityLabel="Back to Profile" onPress={() => router.replace('/(tabs)/profile')} style={styles.primaryButton}>
            <Text style={styles.primaryText}>Back to Profile</Text>
          </AtlasPressable>
        </View>
      </AtlasScreen>
    );
  }

  return (
    <AtlasScreen automaticallyAdjustKeyboardInsets keyboardDismissMode="on-drag" keyboardShouldPersistTaps="handled" contentStyle={styles.container}>
      <View style={styles.header}>
        <Text style={styles.eyebrow}>FEEDBACK & SUPPORT</Text>
        <Text accessibilityRole="header" style={styles.title}>{copy?.title ?? 'How can Atlas help?'}</Text>
        <Text style={styles.body}>{copy?.body ?? 'Choose the kind of feedback or support you want to send.'}</Text>
      </View>

      {!kind ? (
        <View style={styles.choiceList}>
          {feedbackChoices.map(choice => (
            <AtlasPressable
              key={choice.kind}
              accessibilityRole="button"
              accessibilityLabel={choice.label}
              onPress={() => { setKind(choice.kind); setNotice(null); }}
              style={styles.choiceCard}
            >
              <Text style={styles.choiceTitle}>{choice.label}</Text>
              <Text style={styles.choiceBody}>{choice.description}</Text>
            </AtlasPressable>
          ))}
        </View>
      ) : (
        <AtlasPressable accessibilityRole="button" accessibilityLabel="Choose a different feedback type" disabled={submitting} onPress={() => { setKind(null); setNotice(null); }} style={styles.secondaryButton}>
          <Text style={styles.secondaryText}>Choose a different feedback type</Text>
        </AtlasPressable>
      )}

      {kind ? (
        <View style={styles.formCard}>
          <Text style={styles.label}>Optional note</Text>
          <TextInput
            accessibilityLabel="Feedback note"
            editable={!submitting}
            maxLength={1200}
            multiline
            onChangeText={setMessage}
            placeholder="Add only the detail needed to understand the issue."
            placeholderTextColor={tokens.color.muted}
            style={styles.input}
            textAlignVertical="top"
            value={message}
          />
          <Text style={styles.counter}>{message.length}/1200</Text>
          <Text style={styles.privacy}>Do not include customer names, contact details, or other end-customer personal data.</Text>
          {notice ? <Text accessibilityLiveRegion="polite" style={submission === 'error' ? styles.error : styles.notice}>{notice}</Text> : null}
          <AtlasPressable accessibilityRole="button" accessibilityLabel="Submit feedback" disabled={submitting} onPress={() => void submit()} style={[styles.primaryButton, submitting && styles.disabled]}>
            <Text style={styles.primaryText}>{submitting ? 'Submitting…' : 'Submit feedback'}</Text>
          </AtlasPressable>
        </View>
      ) : null}
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  container: { backgroundColor: tokens.color.surface, gap: 20 },
  header: { gap: 8 },
  eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.1 },
  title: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 30, fontWeight: '800', lineHeight: 36 },
  body: { color: tokens.color.muted, fontSize: 14.5, lineHeight: 22 },
  choiceList: { gap: 10 },
  choiceCard: { borderColor: '#DCE5DF', borderRadius: tokens.radius.md, borderWidth: 1, gap: 5, minHeight: 68, padding: 16 },
  choiceTitle: { color: tokens.color.greenDeep, fontSize: 17, fontWeight: '800' },
  choiceBody: { color: tokens.color.muted, fontSize: 13.5, lineHeight: 20 },
  formCard: { borderColor: '#DCE5DF', borderRadius: tokens.radius.md, borderWidth: 1, gap: 10, padding: 16 },
  label: { color: tokens.color.greenDeep, fontSize: 15, fontWeight: '800' },
  input: { borderColor: '#CBD8D0', borderRadius: tokens.radius.md, borderWidth: 1, color: tokens.color.greenDeep, fontSize: 15, minHeight: 130, padding: 14 },
  counter: { color: tokens.color.muted, fontSize: 12, textAlign: 'right' },
  privacy: { color: tokens.color.muted, fontSize: 12.5, lineHeight: 18 },
  notice: { color: tokens.color.greenDeep, fontSize: 13.5, lineHeight: 20 },
  error: { color: '#8B2D2D', fontSize: 13.5, lineHeight: 20 },
  primaryButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 50, paddingHorizontal: 20 },
  primaryText: { color: tokens.color.surface, fontSize: 14, fontWeight: '800' },
  secondaryButton: { alignItems: 'center', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1.5, justifyContent: 'center', minHeight: 46, paddingHorizontal: 18 },
  secondaryText: { color: tokens.color.greenDeep, fontSize: 14, fontWeight: '800' },
  successCard: { borderColor: '#DCE5DF', borderRadius: tokens.radius.md, borderWidth: 1, gap: 14, padding: 20 },
  helper: { color: tokens.color.muted, fontSize: 13.5, lineHeight: 20 },
  disabled: { opacity: 0.55 },
});
