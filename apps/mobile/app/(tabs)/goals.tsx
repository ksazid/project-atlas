import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { getGoals, saveGoals, type BusinessGoal } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { addCustomGoal, createGoalsOperationCoordinator, formatGoalType, getGoalsRetryPresentation, getGoalsSavePresentation, getGoalsStatePresentation, moveGoal, resolveGoalsLoadFailure, resolveGoalsReload, resolveGoalsSaveResponse, type GoalsOperation, type GoalsOperationTicket } from '@/features/goals/goals-model';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'missing';

export default function GoalsScreen() {
  const router = useRouter();
  const [goals, setGoals] = useState<BusinessGoal[]>([]);
  const [state, setState] = useState<ScreenState>('loading');
  const [starter, setStarter] = useState(false);
  const [loadWarning, setLoadWarning] = useState<string | null>(null);
  const [custom, setCustom] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [validation, setValidation] = useState<string | null>(null);
  const [operation, setOperation] = useState<GoalsOperation>('idle');
  const goalsRef = useRef<BusinessGoal[]>([]);
  const starterRef = useRef(false);
  const hasDraftEditsRef = useRef(false);
  const operationCoordinatorRef = useRef<ReturnType<typeof createGoalsOperationCoordinator> | null>(null);
  if (!operationCoordinatorRef.current) operationCoordinatorRef.current = createGoalsOperationCoordinator();
  const operationCoordinator = operationCoordinatorRef.current;

  const setCurrentGoals = useCallback((next: BusinessGoal[]) => {
    goalsRef.current = next;
    setGoals(next);
  }, []);

  const setCurrentStarter = useCallback((next: boolean) => {
    starterRef.current = next;
    setStarter(next);
  }, []);

  const beginOperation = useCallback((next: Exclude<GoalsOperation, 'idle'>) => {
    const ticket = operationCoordinator.start(next);
    if (ticket) setOperation(next);
    return ticket;
  }, [operationCoordinator]);

  const finishOperation = useCallback((ticket: GoalsOperationTicket) => {
    if (operationCoordinator.finish(ticket)) setOperation('idle');
  }, [operationCoordinator]);

  const load = useCallback(async (manual = false) => {
    const ticket = beginOperation('refreshing');
    if (!ticket) return;
    const preserveDraft = manual && hasDraftEditsRef.current;
    if (!manual) {
      setState('loading');
      setMessage(null);
      setValidation(null);
    }
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setState('missing');
        return;
      }
      const existing = await getGoals(session.accessToken, session.businessId);
      const resolution = resolveGoalsReload(existing, goalsRef.current, starterRef.current, preserveDraft);
      setCurrentGoals(resolution.goals);
      setCurrentStarter(resolution.starter);
      if (!resolution.preservedDraft) hasDraftEditsRef.current = false;
      setLoadWarning(null);
      setState('ready');
    } catch {
      const failure = resolveGoalsLoadFailure(goalsRef.current, starterRef.current, preserveDraft);
      setCurrentGoals(failure.goals);
      setCurrentStarter(failure.starter);
      setLoadWarning(failure.warning);
      setState('ready');
    } finally {
      finishOperation(ticket);
    }
  }, [beginOperation, finishOperation, setCurrentGoals, setCurrentStarter]);

  useEffect(() => { void load(); }, [load]);

  const move = (index: number, delta: -1 | 1) => {
    if (index + delta >= 0 && index + delta < goalsRef.current.length) hasDraftEditsRef.current = true;
    setCurrentGoals(moveGoal(goalsRef.current, index, delta));
    setMessage(null);
  };

  const add = () => {
    const result = addCustomGoal(goalsRef.current, custom);
    if (result.error) {
      setValidation(result.error);
      setMessage(null);
      return;
    }
    setCurrentGoals(result.goals);
    hasDraftEditsRef.current = true;
    setCustom('');
    setValidation(null);
    setMessage(null);
  };

  const submit = async () => {
    if (goalsRef.current.length === 0) return;
    const ticket = beginOperation('saving');
    if (!ticket) return;
    setMessage(null);
    setValidation(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setState('missing');
        return;
      }
      const saved = await saveGoals(session.accessToken, session.businessId, goalsRef.current);
      const resolution = resolveGoalsSaveResponse(saved);
      setCurrentGoals(resolution.goals);
      setCurrentStarter(resolution.starter);
      hasDraftEditsRef.current = false;
      setLoadWarning(null);
      setMessage('Goals saved.');
    } catch {
      setMessage('Could not save goals. Your priorities are still here.');
    } finally {
      finishOperation(ticket);
    }
  };

  if (state !== 'ready') {
    return <GoalsState state={state} continueSession={() => router.replace({ pathname: '/', params: { sessionEntry: '1' } })} />;
  }

  const refreshing = operation === 'refreshing';
  const saving = operation === 'saving';
  const editingEnabled = operation === 'idle';
  const saveEnabled = goals.length > 0 && operation === 'idle';
  const savePresentation = getGoalsSavePresentation(saving, saveEnabled, refreshing);
  const retryPresentation = getGoalsRetryPresentation(operation);
  return (
    <ScrollView
      automaticallyAdjustKeyboardInsets
      contentContainerStyle={styles.container}
      keyboardDismissMode="on-drag"
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.content}>
        <View style={styles.header}>
          <BrandMark size={50} />
          <Text style={styles.eyebrow}>BUSINESS GOALS</Text>
          <Text accessibilityRole="header" style={styles.title}>Choose what Atlas should optimize for.</Text>
          <Text style={styles.help}>Rank the outcomes that matter most. Atlas uses this order to evaluate future opportunities.</Text>
        </View>

        <View style={styles.guidanceCard}>
          <Text style={styles.cardEyebrow}>HOW PRIORITIES WORK</Text>
          <Text style={styles.guidanceTitle}>Priority 1 is your strongest signal.</Text>
          <Text style={styles.guidanceCopy}>Move the goals that matter most to the top. You can refine this list whenever your business needs change.</Text>
        </View>

        {starter ? <Text accessibilityLiveRegion="polite" style={styles.notice}>These starter goals are ready for your review and have not been saved yet.</Text> : null}
        {loadWarning ? <View style={styles.warning}><Text accessibilityLiveRegion="polite" style={styles.warningText}>{loadWarning}</Text><Pressable aria-busy={retryPresentation.ariaBusy} accessibilityLabel={retryPresentation.accessibilityLabel} accessibilityRole="button" accessibilityState={retryPresentation.accessibilityState} disabled={operation !== 'idle'} onPress={() => void load(true)} style={({ pressed }) => [styles.retryButton, pressed && operation === 'idle' && styles.pressed, operation !== 'idle' && styles.disabled]}>{refreshing ? <ActivityIndicator color={tokens.color.green} /> : null}<Text style={styles.retryText}>{retryPresentation.text}</Text></Pressable></View> : null}

        <View style={styles.goalList}>
          {goals.map((goal, index) => {
            const canMoveUp = editingEnabled && index > 0;
            const canMoveDown = editingEnabled && index < goals.length - 1;
            return (
              <View key={goal.id ?? `${goal.title}-${index}`} style={styles.goalCard}>
                <View style={styles.goalCopy}>
                  <Text style={styles.priority}>PRIORITY {index + 1}</Text>
                  <Text style={styles.goalTitle}>{goal.title}</Text>
                  <View style={styles.metaRow}>
                    <Text style={styles.goalType}>{formatGoalType(goal.type)}</Text>
                    {goal.isCustom ? <Text style={styles.customBadge}>CUSTOM</Text> : null}
                  </View>
                </View>
                <View style={styles.moveActions}>
                  <Pressable accessibilityHint="Moves this goal one priority higher" accessibilityLabel={`Move ${goal.title} up`} accessibilityRole="button" accessibilityState={{ disabled: !canMoveUp }} disabled={!canMoveUp} onPress={() => move(index, -1)} style={({ pressed }) => [styles.moveButton, pressed && canMoveUp && styles.pressed, !canMoveUp && styles.disabled]}><Text style={styles.moveButtonText}>↑</Text></Pressable>
                  <Pressable accessibilityHint="Moves this goal one priority lower" accessibilityLabel={`Move ${goal.title} down`} accessibilityRole="button" accessibilityState={{ disabled: !canMoveDown }} disabled={!canMoveDown} onPress={() => move(index, 1)} style={({ pressed }) => [styles.moveButton, pressed && canMoveDown && styles.pressed, !canMoveDown && styles.disabled]}><Text style={styles.moveButtonText}>↓</Text></Pressable>
                </View>
              </View>
            );
          })}
        </View>

        <View style={styles.customCard}>
          <Text style={styles.cardEyebrow}>ADD A CUSTOM GOAL</Text>
          <Text style={styles.customTitle}>Name an outcome that matters to you.</Text>
          <Text style={styles.customHelp}>Keep it specific enough to guide a future opportunity.</Text>
          <View style={styles.addRow}>
            <TextInput
              accessibilityLabel="Custom goal"
              accessibilityHint="Enter a business outcome to add to your priorities"
              accessibilityState={{ disabled: !editingEnabled }}
              editable={editingEnabled}
              onChangeText={value => { if (value !== custom) hasDraftEditsRef.current = true; setCustom(value); setValidation(null); }}
              placeholder="For example, retain more regular customers"
              placeholderTextColor={tokens.color.muted}
              style={[styles.input, !editingEnabled && styles.disabled]}
              value={custom}
            />
            <Pressable accessibilityLabel="Add custom goal" accessibilityRole="button" accessibilityState={{ disabled: !editingEnabled }} disabled={!editingEnabled} onPress={add} style={({ pressed }) => [styles.addButton, pressed && editingEnabled && styles.pressed, !editingEnabled && styles.disabled]}><Text style={styles.addButtonText}>Add</Text></Pressable>
          </View>
          {validation ? <Text accessibilityLiveRegion="polite" style={styles.validation}>{validation}</Text> : null}
        </View>

        {message ? <Text accessibilityLiveRegion="polite" style={[styles.message, message === 'Goals saved.' ? styles.successMessage : styles.errorMessage]}>{message}</Text> : null}
        <Pressable aria-busy={savePresentation.ariaBusy} accessibilityLabel={savePresentation.accessibilityLabel} accessibilityRole="button" accessibilityState={savePresentation.accessibilityState} disabled={!saveEnabled} onPress={() => void submit()} style={({ pressed }) => [styles.saveButton, pressed && saveEnabled && styles.pressed, !saveEnabled && styles.disabled]}>
          {saving ? <ActivityIndicator color={tokens.color.surface} /> : null}
          <Text style={styles.saveButtonText}>{savePresentation.text}</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function GoalsState({ state, continueSession }: { state: Exclude<ScreenState, 'ready'>; continueSession: () => void }) {
  const content = getGoalsStatePresentation(state);
  return <View style={styles.stateScreen}><View style={styles.stateCard}><BrandMark size={50} /><Text accessibilityRole="header" style={styles.stateTitle}>{content.title}</Text><Text style={styles.stateCopy}>{content.copy}</Text>{state === 'loading' ? <ActivityIndicator color={tokens.color.green} /> : <Pressable accessibilityLabel={content.action} accessibilityRole="button" onPress={continueSession} style={({ pressed }) => [styles.retryButton, pressed && styles.pressed]}><Text style={styles.retryText}>{content.action}</Text></Pressable>}</View></View>;
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: tokens.color.canvas, flexGrow: 1, padding: tokens.spacing.lg },
  content: { gap: tokens.spacing.lg, maxWidth: 680, paddingBottom: tokens.spacing.xxl, width: '100%' },
  header: { gap: tokens.spacing.sm, paddingTop: tokens.spacing.sm },
  eyebrow: { color: tokens.color.green, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 1.2, marginTop: tokens.spacing.sm },
  title: { color: tokens.color.greenDeep, fontSize: tokens.typography.hero, fontWeight: '800', letterSpacing: -0.7, lineHeight: 41 },
  help: { color: tokens.color.muted, fontSize: tokens.typography.body, lineHeight: 24 },
  guidanceCard: { backgroundColor: tokens.color.mint, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: tokens.spacing.sm, padding: tokens.spacing.md },
  cardEyebrow: { color: tokens.color.greenDeep, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 1 },
  guidanceTitle: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 },
  guidanceCopy: { color: tokens.color.muted, fontSize: 14, lineHeight: 20 },
  notice: { backgroundColor: tokens.color.mint, borderRadius: tokens.radius.md, color: tokens.color.greenDeep, fontSize: 14, lineHeight: 20, padding: tokens.spacing.md },
  warning: { backgroundColor: tokens.color.dangerSoft, borderRadius: tokens.radius.md, gap: tokens.spacing.sm, padding: tokens.spacing.md },
  warningText: { color: tokens.color.danger, fontSize: 14, lineHeight: 20 },
  goalList: { gap: tokens.spacing.sm },
  goalCard: { alignItems: 'center', backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, flexDirection: 'row', gap: tokens.spacing.md, minHeight: 116, padding: tokens.spacing.md, shadowColor: tokens.color.greenDeep, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.04, shadowRadius: 8, elevation: 1 },
  goalCopy: { flex: 1, gap: tokens.spacing.xs },
  priority: { color: tokens.color.green, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 1 },
  goalTitle: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 },
  metaRow: { alignItems: 'center', flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  goalType: { color: tokens.color.muted, fontSize: 14, lineHeight: 20 },
  customBadge: { backgroundColor: tokens.color.mint, borderRadius: tokens.radius.pill, color: tokens.color.greenDeep, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 0.8, overflow: 'hidden', paddingHorizontal: tokens.spacing.sm, paddingVertical: tokens.spacing.xs },
  moveActions: { gap: tokens.spacing.sm },
  moveButton: { alignItems: 'center', backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, height: tokens.touchTarget, justifyContent: 'center', width: tokens.touchTarget },
  moveButtonText: { color: tokens.color.greenDeep, fontSize: 20, fontWeight: '800', lineHeight: 24 },
  customCard: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: tokens.spacing.sm, padding: tokens.spacing.md, shadowColor: tokens.color.greenDeep, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.04, shadowRadius: 8, elevation: 1 },
  customTitle: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 },
  customHelp: { color: tokens.color.muted, fontSize: 14, lineHeight: 20 },
  addRow: { alignItems: 'stretch', flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm, marginTop: tokens.spacing.xs },
  input: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, color: tokens.color.ink, flex: 1, fontSize: tokens.typography.body, lineHeight: 22, minHeight: tokens.touchTarget, minWidth: 220, paddingHorizontal: 12, paddingVertical: 10 },
  addButton: { alignItems: 'center', backgroundColor: tokens.color.surface, borderColor: tokens.color.green, borderRadius: tokens.radius.md, borderWidth: 1, justifyContent: 'center', minHeight: tokens.touchTarget, paddingHorizontal: tokens.spacing.md },
  addButtonText: { color: tokens.color.green, fontSize: 14, fontWeight: '800' },
  validation: { color: tokens.color.danger, fontSize: 14, lineHeight: 20 },
  message: { borderRadius: tokens.radius.md, fontSize: 14, lineHeight: 20, padding: tokens.spacing.md },
  successMessage: { backgroundColor: tokens.color.mint, color: tokens.color.greenDeep },
  errorMessage: { backgroundColor: tokens.color.dangerSoft, color: tokens.color.danger },
  saveButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.md, flexDirection: 'row', gap: tokens.spacing.sm, justifyContent: 'center', minHeight: 52, paddingHorizontal: tokens.spacing.lg },
  saveButtonText: { color: tokens.color.surface, fontSize: tokens.typography.body, fontWeight: '800' },
  pressed: { opacity: 0.86 },
  disabled: { opacity: 0.5 },
  stateScreen: { alignItems: 'center', backgroundColor: tokens.color.canvas, flex: 1, justifyContent: 'center', padding: tokens.spacing.lg },
  stateCard: { alignItems: 'flex-start', backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: tokens.spacing.md, maxWidth: 440, padding: tokens.spacing.lg, width: '100%' },
  stateTitle: { color: tokens.color.greenDeep, fontSize: tokens.typography.title, fontWeight: '800', lineHeight: 34 },
  stateCopy: { color: tokens.color.muted, fontSize: tokens.typography.body, lineHeight: 24 },
  retryButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.md, justifyContent: 'center', minHeight: tokens.touchTarget, paddingHorizontal: tokens.spacing.md },
  retryText: { color: tokens.color.surface, fontSize: 14, fontWeight: '800' }
});
