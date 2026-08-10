import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import {
  answerProgressiveQuestion,
  getProgressiveQuestions,
  ProgressiveQuestionApiError,
  skipProgressiveQuestion,
} from '@/api/progressive-questions';
import { loadSession, type Session } from '@/auth/session';
import { getSessionDestination } from '@/auth/session-routing';
import { BrandMark } from '@/components/BrandMark';
import {
  buildAnswerRequest,
  canContinue,
  createAnswerDraft,
  getProgressLabel,
  toggleSelection,
  updateText,
  type ProgressiveQuestion,
  type ProgressiveQuestionAnswerDraft,
  type ProgressiveQuestionSet,
} from '@/features/progressive-questions/progressive-question-model';

const GREEN = '#00754A';

type ScreenState = 'loading' | 'question' | 'load-error' | 'complete';

export default function ProgressiveQuestionsScreen() {
  const [session, setSession] = useState<Session | null>(null);
  const [questionSet, setQuestionSet] = useState<ProgressiveQuestionSet | null>(null);
  const [draft, setDraft] = useState<ProgressiveQuestionAnswerDraft>({ selections: [], text: '' });
  const [state, setState] = useState<ScreenState>('loading');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [initialTotal, setInitialTotal] = useState(0);
  const [completedCount, setCompletedCount] = useState(0);

  const question = questionSet?.questions[0] ?? null;
  const continueToToday = useCallback(() => router.replace('/(tabs)'), []);

  const load = useCallback(async () => {
    setState('loading');
    setMessage(null);
    try {
      const restored = await loadSession();
      if (!restored?.businessId) {
        router.replace(getSessionDestination(restored));
        return;
      }

      const next = await getProgressiveQuestions(restored.accessToken, restored.businessId);
      setSession(restored);
      setQuestionSet(next);
      setInitialTotal(next.questions.length);
      setCompletedCount(0);

      if (next.questions.length === 0) {
        continueToToday();
        return;
      }

      setDraft(createAnswerDraft(next.questions[0]));
      setState('question');
    } catch {
      setState('load-error');
    }
  }, [continueToToday]);

  useEffect(() => { void load(); }, [load]);

  const applyRemaining = useCallback((remaining: ProgressiveQuestionSet) => {
    const nextCompleted = completedCount + 1;
    setCompletedCount(nextCompleted);
    setQuestionSet(remaining);
    setMessage(null);

    if (remaining.questions.length === 0) {
      setState('complete');
      return;
    }

    setDraft(createAnswerDraft(remaining.questions[0]));
    setState('question');
  }, [completedCount]);

  async function submitAnswer() {
    if (!session?.businessId || !questionSet || !question || saving || !canContinue(question, draft)) return;
    setSaving(true);
    setMessage(null);
    try {
      const result = await answerProgressiveQuestion(
        session.accessToken,
        session.businessId,
        question.questionKey,
        buildAnswerRequest(questionSet.catalogueVersion, question, draft),
      );
      applyRemaining(result.remaining);
    } catch (cause) {
      if (cause instanceof ProgressiveQuestionApiError && cause.code === 'progressive_catalogue_stale') {
        setMessage('These optional questions changed. Refreshing the latest set…');
        await load();
      } else {
        setMessage('Atlas could not save this optional answer. Your selection is still here.');
      }
    } finally {
      setSaving(false);
    }
  }

  async function skipCurrent() {
    if (!session?.businessId || !questionSet || !question || saving) return;
    setSaving(true);
    setMessage(null);
    try {
      const result = await skipProgressiveQuestion(
        session.accessToken,
        session.businessId,
        question.questionKey,
        questionSet.catalogueVersion,
      );
      applyRemaining(result.remaining);
    } catch (cause) {
      if (cause instanceof ProgressiveQuestionApiError && cause.code === 'progressive_catalogue_stale') {
        setMessage('These optional questions changed. Refreshing the latest set…');
        await load();
      } else {
        setMessage('Atlas could not skip this question yet. You can retry or continue to Today.');
      }
    } finally {
      setSaving(false);
    }
  }

  if (state === 'loading') {
    return (
      <StateFrame>
        <ActivityIndicator accessibilityLabel="Loading optional business questions" color={GREEN} />
        <Text accessibilityRole="header" style={styles.stateTitle}>Preparing a little more context.</Text>
        <Text style={styles.stateCopy}>Atlas is checking what it already knows so it does not ask you twice.</Text>
      </StateFrame>
    );
  }

  if (state === 'load-error') {
    return (
      <StateFrame>
        <Text style={styles.eyebrow}>OPTIONAL CONTEXT</Text>
        <Text accessibilityRole="header" style={styles.stateTitle}>These questions are unavailable right now.</Text>
        <Text style={styles.stateCopy}>You can continue without them. Atlas can ask for useful context later.</Text>
        <Pressable accessibilityLabel="Try loading optional questions again" accessibilityRole="button" onPress={() => void load()} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}>
          <Text style={styles.primaryButtonText}>Try again</Text>
        </Pressable>
        <Pressable accessibilityLabel="Continue for now" accessibilityRole="button" onPress={continueToToday} style={({ pressed }) => [styles.secondaryButton, pressed && styles.pressed]}>
          <Text style={styles.secondaryButtonText}>Continue for now</Text>
        </Pressable>
      </StateFrame>
    );
  }

  if (state === 'complete') {
    return (
      <StateFrame>
        <View style={styles.completeMark}><Text style={styles.completeMarkText}>✓</Text></View>
        <Text style={styles.eyebrow}>READY TO CONTINUE</Text>
        <Text accessibilityRole="header" style={styles.stateTitle}>That’s enough to get started.</Text>
        <Text style={styles.stateCopy}>Atlas can learn more later when another detail would materially improve its guidance.</Text>
        <Pressable accessibilityLabel="Continue to Today" accessibilityRole="button" onPress={continueToToday} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}>
          <Text style={styles.primaryButtonText}>Continue to Today</Text>
        </Pressable>
      </StateFrame>
    );
  }

  if (!question || !questionSet) return null;

  const progressTotal = Math.max(initialTotal, completedCount + questionSet.questions.length);
  const progressIndex = Math.min(completedCount, Math.max(0, progressTotal - 1));

  return (
    <ScrollView
      automaticallyAdjustKeyboardInsets
      contentContainerStyle={styles.container}
      keyboardDismissMode="on-drag"
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.content}>
        <BrandMark size={52} />
        <View style={styles.progressRow}>
          <Text style={styles.eyebrow}>A LITTLE MORE CONTEXT</Text>
          <Text accessibilityLabel={`Question ${getProgressLabel(progressIndex, progressTotal)}`} style={styles.progressText}>
            Question {getProgressLabel(progressIndex, progressTotal)}
          </Text>
        </View>

        <View style={styles.questionBlock}>
          <Text accessibilityRole="header" style={styles.questionTitle}>{question.prompt}</Text>
          {question.helper ? <Text style={styles.helper}>{question.helper}</Text> : null}
        </View>

        <AnswerControl question={question} draft={draft} saving={saving} onChange={setDraft} />

        {message ? <Text accessibilityLiveRegion="polite" style={styles.errorMessage}>{message}</Text> : null}
        {message ? (
          <Pressable accessibilityLabel="Continue for now" accessibilityRole="button" onPress={continueToToday} style={({ pressed }) => [styles.secondaryButton, pressed && styles.pressed]}>
            <Text style={styles.secondaryButtonText}>Continue for now</Text>
          </Pressable>
        ) : null}

        <Pressable
          accessibilityLabel="Continue with this answer"
          accessibilityRole="button"
          accessibilityState={{ busy: saving, disabled: saving || !canContinue(question, draft) }}
          disabled={saving || !canContinue(question, draft)}
          onPress={() => void submitAnswer()}
          style={({ pressed }) => [styles.primaryButton, (saving || !canContinue(question, draft)) && styles.disabled, pressed && !saving && styles.pressed]}
        >
          {saving ? <ActivityIndicator color="#FFF" /> : <Text style={styles.primaryButtonText}>Continue</Text>}
        </Pressable>

        <Pressable
          accessibilityLabel="Skip for now"
          accessibilityRole="button"
          accessibilityState={{ busy: saving, disabled: saving }}
          disabled={saving}
          onPress={() => void skipCurrent()}
          style={({ pressed }) => [styles.skipButton, saving && styles.disabled, pressed && !saving && styles.pressed]}
        >
          <Text style={styles.skipButtonText}>Skip for now</Text>
        </Pressable>

        <Text style={styles.optionalCopy}>Optional — skipping keeps this detail unknown and will not block setup.</Text>
      </View>
    </ScrollView>
  );
}

function AnswerControl({ question, draft, saving, onChange }: {
  question: ProgressiveQuestion;
  draft: ProgressiveQuestionAnswerDraft;
  saving: boolean;
  onChange: (draft: ProgressiveQuestionAnswerDraft) => void;
}) {
  if (question.answerType === 'short-text') {
    return (
      <View style={styles.answerGroup}>
        <TextInput
          accessibilityLabel="Optional business context answer"
          accessibilityState={{ disabled: saving }}
          editable={!saving}
          maxLength={question.maxLength ?? 240}
          multiline
          onChangeText={value => onChange(updateText(draft, value))}
          placeholder="Short answer"
          placeholderTextColor="#7A8781"
          style={styles.textInput}
          textAlignVertical="top"
          value={draft.text}
        />
        <Text style={styles.characterCount}>{draft.text.length}/{question.maxLength ?? 240}</Text>
      </View>
    );
  }

  return (
    <View accessibilityRole={question.answerType === 'single-choice' ? 'radiogroup' : undefined} style={styles.answerGroup}>
      {question.options.map(option => {
        const selected = draft.selections.includes(option);
        if (question.answerType === 'multi-choice') {
          return (
            <Pressable
              key={option}
              aria-checked={selected}
              accessibilityLabel={option}
              accessibilityRole="checkbox"
              accessibilityState={{ checked: selected, disabled: saving }}
              disabled={saving}
              onPress={() => onChange(toggleSelection(draft, option, question))}
              style={({ pressed }) => [styles.option, selected && styles.optionSelected, pressed && !saving && styles.pressed]}
            >
              <SelectionMark selected={selected} />
              <Text style={[styles.optionText, selected && styles.optionTextSelected]}>{option}</Text>
            </Pressable>
          );
        }

        return (
          <Pressable
            key={option}
            aria-checked={selected}
            accessibilityLabel={option}
            accessibilityRole="radio"
            accessibilityState={{ selected: selected, disabled: saving }}
            disabled={saving}
            onPress={() => onChange(toggleSelection(draft, option, question))}
            style={({ pressed }) => [styles.option, selected && styles.optionSelected, pressed && !saving && styles.pressed]}
          >
            <SelectionMark selected={selected} />
            <Text style={[styles.optionText, selected && styles.optionTextSelected]}>{option}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

function SelectionMark({ selected }: { selected: boolean }) {
  return (
    <View style={[styles.selectionMark, selected && styles.selectionMarkSelected]}>
      <Text style={styles.selectionMarkText}>{selected ? '✓' : ''}</Text>
    </View>
  );
}

function StateFrame({ children }: { children: React.ReactNode }) {
  return (
    <View style={styles.stateScreen}>
      <View style={styles.stateCard}>
        <BrandMark size={52} />
        {children}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: '#FFF', flexGrow: 1, paddingBottom: 44, paddingHorizontal: 28, paddingTop: 58 },
  content: { gap: 20, maxWidth: 620, width: '100%' },
  progressRow: { alignItems: 'center', flexDirection: 'row', flexWrap: 'wrap', gap: 10, justifyContent: 'space-between' },
  eyebrow: { color: GREEN, fontSize: 10.5, fontWeight: '900', letterSpacing: 1.2 },
  progressText: { color: '#53625B', fontSize: 12, fontWeight: '800' },
  questionBlock: { gap: 9, paddingTop: 6 },
  questionTitle: { color: '#173B2A', fontFamily: 'Georgia', fontSize: 32, fontWeight: '800', letterSpacing: -0.45, lineHeight: 38 },
  helper: { color: '#53625B', fontSize: 14.5, lineHeight: 22 },
  answerGroup: { gap: 10 },
  option: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#DDE4E0', borderRadius: 12, borderWidth: 1, flexDirection: 'row', gap: 12, minHeight: 52, paddingHorizontal: 15, paddingVertical: 10 },
  optionSelected: { backgroundColor: '#EEF8F2', borderColor: GREEN, borderWidth: 1.5 },
  optionText: { color: '#263A31', flex: 1, fontSize: 14.5, fontWeight: '700', lineHeight: 20 },
  optionTextSelected: { color: '#0A2F25', fontWeight: '900' },
  selectionMark: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#87938D', borderRadius: 999, borderWidth: 1, height: 24, justifyContent: 'center', width: 24 },
  selectionMarkSelected: { backgroundColor: GREEN, borderColor: GREEN },
  selectionMarkText: { color: '#FFF', fontSize: 14, fontWeight: '900' },
  textInput: { backgroundColor: '#FFF', borderColor: '#DDE4E0', borderRadius: 12, borderWidth: 1, color: '#22342D', fontSize: 15, lineHeight: 22, minHeight: 112, paddingHorizontal: 15, paddingTop: 14 },
  characterCount: { color: '#6A7771', fontSize: 11.5, textAlign: 'right' },
  primaryButton: { alignItems: 'center', backgroundColor: GREEN, borderRadius: 10, flexDirection: 'row', gap: 8, justifyContent: 'center', minHeight: 55, paddingHorizontal: 22 },
  primaryButtonText: { color: '#FFF', fontSize: 15, fontWeight: '900' },
  secondaryButton: { alignItems: 'center', borderColor: GREEN, borderRadius: 10, borderWidth: 1, justifyContent: 'center', minHeight: 50, paddingHorizontal: 18 },
  secondaryButtonText: { color: '#0A2F25', fontSize: 14, fontWeight: '800' },
  skipButton: { alignItems: 'center', justifyContent: 'center', minHeight: 44, paddingHorizontal: 16 },
  skipButtonText: { color: GREEN, fontSize: 14, fontWeight: '800' },
  optionalCopy: { color: '#6A7771', fontSize: 12, lineHeight: 18, textAlign: 'center' },
  errorMessage: { backgroundColor: '#FDECEC', borderRadius: 10, color: '#9A2B20', fontSize: 13, lineHeight: 19, padding: 13 },
  completeMark: { alignItems: 'center', backgroundColor: GREEN, borderRadius: 999, height: 58, justifyContent: 'center', width: 58 },
  completeMarkText: { color: '#FFF', fontSize: 28, fontWeight: '900' },
  stateScreen: { alignItems: 'center', backgroundColor: '#FFF', flex: 1, justifyContent: 'center', padding: 28 },
  stateCard: { backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 16, maxWidth: 480, padding: 24, width: '100%' },
  stateTitle: { color: '#173B2A', fontFamily: 'Georgia', fontSize: 28, fontWeight: '800', lineHeight: 34 },
  stateCopy: { color: '#53625B', fontSize: 14.5, lineHeight: 22 },
  pressed: { opacity: 0.9, transform: [{ scale: 0.99 }] },
  disabled: { opacity: 0.48 },
});
