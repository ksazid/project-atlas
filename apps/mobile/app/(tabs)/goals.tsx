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
  container: { alignItems: 'center', backgroundColor: '#FFF', flexGrow: 1, paddingHorizontal: 28, paddingTop: 58, paddingBottom: 38 },
  content: { gap: 22, maxWidth: 680, width: '100%' },
  header: { gap: 8 },
  eyebrow: { color: '#00754A', fontSize: 11, fontWeight: '900', letterSpacing: 1.15, marginTop: 10 },
  title: { color: '#0A2F25', fontFamily: 'Georgia', fontSize: 32, fontWeight: '800', letterSpacing: -0.5, lineHeight: 38 },
  help: { color: '#5B6761', fontSize: 14.5, lineHeight: 22 },
  guidanceCard: { backgroundColor: '#EEF8F2', borderColor: '#DDE8E1', borderRadius: 12, borderWidth: 1, gap: 8, padding: 16 },
  cardEyebrow: { color: '#00754A', fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  guidanceTitle: { color: '#17221C', fontSize: 17, fontWeight: '800', lineHeight: 23 },
  guidanceCopy: { color: '#5B6761', fontSize: 13.5, lineHeight: 20 },
  notice: { backgroundColor: '#EEF8F2', borderRadius: 10, color: '#0A2F25', fontSize: 13.5, lineHeight: 20, padding: 14 },
  warning: { backgroundColor: '#FDECEC', borderRadius: 10, gap: 8, padding: 14 },
  warningText: { color: '#A1251B', fontSize: 13.5, lineHeight: 20 },
  goalList: { gap: 10 },
  goalCard: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, flexDirection: 'row', gap: 16, minHeight: 112, padding: 16, shadowColor: '#173B2A', shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.025, shadowRadius: 6, elevation: 1 },
  goalCopy: { flex: 1, gap: 4 },
  priority: { color: '#00754A', fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  goalTitle: { color: '#17221C', fontSize: 17, fontWeight: '800', lineHeight: 23 },
  metaRow: { alignItems: 'center', flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  goalType: { color: '#5B6761', fontSize: 13.5, lineHeight: 20 },
  customBadge: { backgroundColor: '#EEF8F2', borderRadius: 999, color: '#0A2F25', fontSize: 10.5, fontWeight: '900', letterSpacing: 0.75, overflow: 'hidden', paddingHorizontal: 8, paddingVertical: 4 },
  moveActions: { gap: 8 },
  moveButton: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#DEE5E1', borderRadius: 10, borderWidth: 1, height: 44, justifyContent: 'center', width: 44 },
  moveButtonText: { color: '#0A2F25', fontSize: 20, fontWeight: '800', lineHeight: 24 },
  customCard: { backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 8, padding: 16, shadowColor: '#173B2A', shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.025, shadowRadius: 6, elevation: 1 },
  customTitle: { color: '#17221C', fontSize: 17, fontWeight: '800', lineHeight: 23 },
  customHelp: { color: '#5B6761', fontSize: 13.5, lineHeight: 20 },
  addRow: { alignItems: 'stretch', flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: 4 },
  input: { backgroundColor: '#FFF', borderColor: '#DEE5E1', borderRadius: 10, borderWidth: 1, color: '#22312C', flex: 1, fontSize: 14, lineHeight: 21, minHeight: 55, minWidth: 220, paddingHorizontal: 14, paddingVertical: 11 },
  addButton: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#00754A', borderRadius: 10, borderWidth: 1, justifyContent: 'center', minHeight: 55, paddingHorizontal: 18 },
  addButtonText: { color: '#00754A', fontSize: 14, fontWeight: '800' },
  validation: { color: '#A1251B', fontSize: 13.5, lineHeight: 20 },
  message: { borderRadius: 10, fontSize: 13.5, lineHeight: 20, padding: 14 },
  successMessage: { backgroundColor: '#EEF8F2', color: '#0A2F25' },
  errorMessage: { backgroundColor: '#FDECEC', color: '#A1251B' },
  saveButton: { alignItems: 'center', backgroundColor: '#008A57', borderRadius: 10, flexDirection: 'row', gap: 8, justifyContent: 'center', minHeight: 55, paddingHorizontal: 24, shadowColor: '#00633F', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.12, shadowRadius: 8, elevation: 2 },
  saveButtonText: { color: '#FFF', fontSize: 15.5, fontWeight: '800' },
  pressed: { opacity: 0.92, transform: [{ scale: .99 }] },
  disabled: { opacity: 0.5 },
  stateScreen: { alignItems: 'center', backgroundColor: '#FFF', flex: 1, justifyContent: 'center', padding: 28 },
  stateCard: { alignItems: 'flex-start', backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 16, maxWidth: 440, padding: 24, width: '100%', shadowColor: '#173B2A', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.03, shadowRadius: 8, elevation: 1 },
  stateTitle: { color: '#0A2F25', fontFamily: 'Georgia', fontSize: 28, fontWeight: '800', lineHeight: 34 },
  stateCopy: { color: '#5B6761', fontSize: 14.5, lineHeight: 22 },
  retryButton: { alignItems: 'center', backgroundColor: '#008A57', borderRadius: 10, justifyContent: 'center', minHeight: 50, paddingHorizontal: 18 },
  retryText: { color: '#FFF', fontSize: 14, fontWeight: '800' }
});
