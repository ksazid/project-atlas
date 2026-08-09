import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { getContext, saveContext, type BusinessContextEntry } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import {
  buildContextSavePayload,
  contextFields,
  createContextOperationCoordinator,
  createInitialContextEntries,
  getContextEntry,
  getContextRetryPresentation,
  getContextSavePresentation,
  getContextStatePresentation,
  getContextValidation,
  mergeContextEntries,
  resolveContextLoadFailure,
  resolveContextReload,
  setContextConfirmation,
  updateContextValue,
  type ContextFieldKey,
  type ContextOperation,
  type ContextOperationTicket,
  type ContextScreenState
} from '@/features/context/context-model';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'missing' | 'error';

export default function ContextScreen() {
  const router = useRouter();
  const [entries, setEntries] = useState<BusinessContextEntry[]>(createInitialContextEntries);
  const [state, setState] = useState<ScreenState>('loading');
  const [operation, setOperation] = useState<ContextOperation>('idle');
  const [message, setMessage] = useState<string | null>(null);
  const [validation, setValidation] = useState<string | null>(null);
  const [loadWarning, setLoadWarning] = useState<string | null>(null);
  const entriesRef = useRef<BusinessContextEntry[]>(createInitialContextEntries());
  const hasDraftEditsRef = useRef(false);
  const operationCoordinatorRef = useRef<ReturnType<typeof createContextOperationCoordinator> | null>(null);

  if (!operationCoordinatorRef.current) operationCoordinatorRef.current = createContextOperationCoordinator();
  const operationCoordinator = operationCoordinatorRef.current;

  const setCurrentEntries = useCallback((next: BusinessContextEntry[]) => {
    entriesRef.current = next;
    setEntries(next);
  }, []);

  const beginOperation = useCallback((next: Exclude<ContextOperation, 'idle'>) => {
    const ticket = operationCoordinator.start(next);
    if (ticket) setOperation(next);
    return ticket;
  }, [operationCoordinator]);

  const finishOperation = useCallback((ticket: ContextOperationTicket) => {
    if (operationCoordinator.finish(ticket)) setOperation('idle');
  }, [operationCoordinator]);

  const load = useCallback(async (manual = false) => {
    const ticket = beginOperation('refreshing');
    if (!ticket) return;

    if (!manual) {
      setState('loading');
      setMessage(null);
      setValidation(null);
      setLoadWarning(null);
    }

    let nextState: ScreenState | null = null;
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        nextState = 'missing';
        return;
      }

      const existing = await getContext(session.accessToken, session.businessId);
      const resolution = resolveContextReload(existing, entriesRef.current, manual && hasDraftEditsRef.current);
      setCurrentEntries(resolution.entries);
      if (!resolution.preservedDraft) hasDraftEditsRef.current = false;
      setLoadWarning(resolution.preservedDraft ? 'Your unsaved changes are still here. Save them before loading the latest saved context.' : null);
      nextState = 'ready';
    } catch {
      if (!manual) {
        nextState = 'error';
      } else {
        const failure = resolveContextLoadFailure(entriesRef.current, hasDraftEditsRef.current);
        setCurrentEntries(failure.entries);
        setLoadWarning(failure.warning);
        nextState = 'ready';
      }
    } finally {
      finishOperation(ticket);
      if (nextState) setState(nextState);
    }
  }, [beginOperation, finishOperation, setCurrentEntries]);

  useEffect(() => { void load(); }, [load]);

  const update = (key: ContextFieldKey, value: string) => {
    setCurrentEntries(updateContextValue(entriesRef.current, key, value));
    hasDraftEditsRef.current = true;
    setMessage(null);
    setValidation(null);
  };

  const confirm = (key: string, confirmed: boolean) => {
    setCurrentEntries(setContextConfirmation(entriesRef.current, key, confirmed));
    hasDraftEditsRef.current = true;
    setMessage(null);
    setValidation(null);
  };

  const submit = async () => {
    const currentValidation = getContextValidation(entriesRef.current);
    if (currentValidation) {
      setValidation(currentValidation);
      setMessage(null);
      return;
    }

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

      const saved = await saveContext(
        session.accessToken,
        session.businessId,
        buildContextSavePayload(entriesRef.current)
      );
      setCurrentEntries(mergeContextEntries(saved));
      hasDraftEditsRef.current = false;
      setLoadWarning(null);
      setMessage('Context saved.');
    } catch {
      setMessage('Could not save context. Your changes are still here.');
    } finally {
      finishOperation(ticket);
    }
  };

  if (state !== 'ready') {
    return (
      <ContextState
        state={state}
        retry={() => load(false)}
        continueSession={() => router.replace({ pathname: '/', params: { sessionEntry: '1' } })}
      />
    );
  }

  const refreshing = operation === 'refreshing';
  const saving = operation === 'saving';
  const editingEnabled = operation === 'idle';
  const saveEnabled = operation === 'idle';
  const savePresentation = getContextSavePresentation(saving, saveEnabled, refreshing);
  const retryPresentation = getContextRetryPresentation(operation);
  const additionalEntries = entries.filter(entry =>
    entry.value.trim().length > 0 && !contextFields.some(field => field.key.toLowerCase() === entry.key.trim().toLowerCase())
  );

  return (
    <ScrollView
      automaticallyAdjustKeyboardInsets
      contentContainerStyle={styles.container}
      keyboardDismissMode="on-drag"
      keyboardShouldPersistTaps="handled"
      refreshControl={<RefreshControl refreshing={refreshing} tintColor={tokens.color.green} onRefresh={() => void load(true)} />}
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.content}>
        <View style={styles.header}>
          <BrandMark size={50} />
          <Text style={styles.eyebrow}>BUSINESS CONTEXT</Text>
          <Text accessibilityRole="header" style={styles.title}>Help Atlas understand how your business works.</Text>
          <Text style={styles.help}>Add only the details that make guidance more practical. Every field is optional.</Text>
        </View>

        <View style={styles.guidanceCard}>
          <Text style={styles.cardEyebrow}>SHARE ONLY WHAT HELPS</Text>
          <Text style={styles.guidanceTitle}>Useful context can stay lightweight.</Text>
          <Text style={styles.guidanceCopy}>Leave anything blank when it does not improve a decision. Describe customers as groups and avoid names, contact details, or other end-customer personal data.</Text>
        </View>

        {loadWarning ? (
          <View style={styles.warningCard}>
            <Text accessibilityLiveRegion="polite" style={styles.warningText}>{loadWarning}</Text>
            <Pressable
              aria-busy={retryPresentation.ariaBusy}
              accessibilityLabel={retryPresentation.accessibilityLabel}
              accessibilityRole="button"
              accessibilityState={retryPresentation.accessibilityState}
              disabled={operation !== 'idle'}
              onPress={() => void load(true)}
              style={({ pressed }) => [styles.secondaryButton, pressed && operation === 'idle' && styles.pressed, operation !== 'idle' && styles.disabled]}
            >
              {refreshing ? <ActivityIndicator color={tokens.color.green} /> : null}
              <Text style={styles.secondaryButtonText}>{retryPresentation.text}</Text>
            </Pressable>
          </View>
        ) : null}

        <View style={styles.fieldList}>
          {contextFields.map(field => {
            const entry = getContextEntry(entries, field.key) ?? { key: field.key, value: '', source: 'owner' as const, ownerConfirmed: true };
            const hasValue = entry.value.trim().length > 0;
            const isPublic = hasValue && entry.source === 'public';
            return (
              <View key={field.key} style={styles.fieldCard}>
                <View style={styles.fieldHeader}>
                  <Text style={styles.fieldLabel}>{field.label}</Text>
                  {hasValue ? (
                    <Text style={[styles.provenanceBadge, isPublic && styles.publicBadge]}>
                      {isPublic ? `PUBLIC SOURCE · ${entry.ownerConfirmed ? 'CONFIRMED' : 'CONFIRMATION REQUIRED'}` : 'OWNER PROVIDED'}
                    </Text>
                  ) : null}
                </View>
                <Text style={styles.fieldPrompt}>{field.prompt}</Text>
                <Text style={styles.fieldHelper}>{field.helper}</Text>
                <TextInput
                  accessibilityLabel={`${field.label} context`}
                  accessibilityHint={field.hint}
                  accessibilityState={{ disabled: !editingEnabled }}
                  editable={editingEnabled}
                  multiline
                  onChangeText={value => update(field.key, value)}
                  placeholder="Optional"
                  placeholderTextColor={tokens.color.muted}
                  style={[styles.input, !editingEnabled && styles.disabled]}
                  textAlignVertical="top"
                  value={entry.value}
                />
                {isPublic ? (
                  <ConfirmationControl
                    confirmed={entry.ownerConfirmed}
                    disabled={!editingEnabled}
                    label={`Confirm public ${field.label} context`}
                    onPress={() => confirm(entry.key, !entry.ownerConfirmed)}
                  />
                ) : null}
              </View>
            );
          })}
        </View>

        {additionalEntries.length > 0 ? (
          <View style={styles.additionalCard}>
            <Text style={styles.cardEyebrow}>ADDITIONAL SAVED CONTEXT</Text>
            <Text style={styles.additionalTitle}>Context already stored for this business</Text>
            <Text style={styles.additionalCopy}>These entries are preserved even though VS-15 does not add new editors for their keys.</Text>
            {additionalEntries.map(entry => (
              <View key={entry.key} style={styles.additionalEntry}>
                <View style={styles.fieldHeader}>
                  <Text style={styles.additionalKey}>{formatContextKey(entry.key)}</Text>
                  <Text style={[styles.provenanceBadge, entry.source === 'public' && styles.publicBadge]}>
                    {entry.source === 'public' ? `PUBLIC SOURCE · ${entry.ownerConfirmed ? 'CONFIRMED' : 'CONFIRMATION REQUIRED'}` : 'OWNER PROVIDED'}
                  </Text>
                </View>
                <Text style={styles.additionalValue}>{entry.value}</Text>
                {entry.source === 'public' ? (
                  <ConfirmationControl
                    confirmed={entry.ownerConfirmed}
                    disabled={!editingEnabled}
                    label={`Confirm public ${formatContextKey(entry.key)} context`}
                    onPress={() => confirm(entry.key, !entry.ownerConfirmed)}
                  />
                ) : null}
              </View>
            ))}
          </View>
        ) : null}

        {validation ? <Text accessibilityLiveRegion="polite" style={[styles.message, styles.errorMessage]}>{validation}</Text> : null}
        {message ? (
          <Text accessibilityLiveRegion="polite" style={[styles.message, message === 'Context saved.' ? styles.successMessage : styles.errorMessage]}>
            {message}
          </Text>
        ) : null}

        <Pressable
          aria-busy={savePresentation.ariaBusy}
          accessibilityLabel={savePresentation.accessibilityLabel}
          accessibilityRole="button"
          accessibilityState={savePresentation.accessibilityState}
          disabled={!saveEnabled}
          onPress={() => void submit()}
          style={({ pressed }) => [styles.button, pressed && saveEnabled && styles.pressed, !saveEnabled && styles.disabled]}
        >
          {saving ? <ActivityIndicator color={tokens.color.surface} /> : null}
          <Text style={styles.buttonText}>{savePresentation.text}</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function ConfirmationControl({ confirmed, disabled, label, onPress }: { confirmed: boolean; disabled: boolean; label: string; onPress: () => void }) {
  return (
    <Pressable
      aria-checked={confirmed}
      accessibilityLabel={label}
      accessibilityRole="checkbox"
      accessibilityState={{ checked: confirmed, disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [styles.confirmation, pressed && !disabled && styles.pressed, disabled && styles.disabled]}
    >
      <View style={[styles.checkbox, confirmed && styles.checkboxChecked]}>
        <Text style={styles.checkmark}>{confirmed ? '✓' : ''}</Text>
      </View>
      <Text style={styles.confirmationText}>{confirmed ? 'Confirmed by you.' : 'I confirm this public context is accurate.'}</Text>
    </Pressable>
  );
}

function ContextState({ state, retry, continueSession }: { state: ContextScreenState; retry: () => Promise<void>; continueSession: () => void }) {
  const content = getContextStatePresentation(state);
  return (
    <View style={styles.stateScreen}>
      <View style={styles.stateCard}>
        <BrandMark size={50} />
        <Text accessibilityRole="header" style={styles.stateTitle}>{content.title}</Text>
        <Text style={styles.stateCopy}>{content.copy}</Text>
        {state === 'loading' ? <ActivityIndicator color={tokens.color.green} /> : null}
        {state === 'missing' ? (
          <Pressable accessibilityLabel="Choose or create a business" accessibilityRole="button" onPress={continueSession} style={({ pressed }) => [styles.retryButton, pressed && styles.pressed]}>
            <Text style={styles.retryText}>Choose or create a business</Text>
          </Pressable>
        ) : null}
        {state === 'error' ? (
          <Pressable accessibilityLabel="Try loading business context again" accessibilityRole="button" onPress={() => void retry()} style={({ pressed }) => [styles.retryButton, pressed && styles.pressed]}>
            <Text style={styles.retryText}>Try again</Text>
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}

function formatContextKey(key: string): string {
  const readable = key.trim().replace(/[_-]+/g, ' ').replace(/([a-z])([A-Z])/g, '$1 $2').replace(/\s+/g, ' ');
  return readable ? `${readable.charAt(0).toUpperCase()}${readable.slice(1)}` : 'Additional context';
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
  warningCard: { alignItems: 'flex-start', backgroundColor: '#F7F5F0', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 12, padding: 16 },
  warningText: { color: '#17221C', fontSize: 13.5, lineHeight: 20 },
  fieldList: { gap: 12 },
  fieldCard: { backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 8, padding: 16, shadowColor: '#173B2A', shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.025, shadowRadius: 6, elevation: 1 },
  fieldHeader: { alignItems: 'flex-start', flexDirection: 'row', flexWrap: 'wrap', gap: 8, justifyContent: 'space-between' },
  fieldLabel: { color: '#0A2F25', fontSize: 17, fontWeight: '800', lineHeight: 23 },
  fieldPrompt: { color: '#17221C', fontSize: 14, fontWeight: '700', lineHeight: 21 },
  fieldHelper: { color: '#5B6761', fontSize: 13.5, lineHeight: 20 },
  provenanceBadge: { backgroundColor: '#F2F0EB', borderRadius: 999, color: '#0A2F25', fontSize: 10, fontWeight: '900', letterSpacing: 0.6, overflow: 'hidden', paddingHorizontal: 9, paddingVertical: 5 },
  publicBadge: { backgroundColor: '#EEF8F2' },
  input: { backgroundColor: '#FFF', borderColor: '#DEE5E1', borderRadius: 10, borderWidth: 1, color: '#22312C', fontSize: 14, lineHeight: 21, minHeight: 104, paddingHorizontal: 14, paddingVertical: 12 },
  confirmation: { alignItems: 'center', flexDirection: 'row', flexWrap: 'wrap', gap: 8, minHeight: 44, paddingVertical: 4 },
  checkbox: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#83908A', borderRadius: 8, borderWidth: 1, height: 24, justifyContent: 'center', width: 24 },
  checkboxChecked: { backgroundColor: '#00754A', borderColor: '#00754A' },
  checkmark: { color: '#FFF', fontSize: 16, fontWeight: '800', lineHeight: 20 },
  confirmationText: { color: '#17221C', flexShrink: 1, fontSize: 13.5, fontWeight: '700', lineHeight: 20 },
  additionalCard: { backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 12, padding: 16 },
  additionalTitle: { color: '#17221C', fontSize: 17, fontWeight: '800', lineHeight: 23 },
  additionalCopy: { color: '#5B6761', fontSize: 13.5, lineHeight: 20 },
  additionalEntry: { borderTopColor: '#E2E7E4', borderTopWidth: 1, gap: 8, paddingTop: 16 },
  additionalKey: { color: '#0A2F25', flexShrink: 1, fontSize: 13.5, fontWeight: '800', lineHeight: 20 },
  additionalValue: { color: '#17221C', fontSize: 13.5, lineHeight: 21 },
  message: { borderRadius: 10, fontSize: 13.5, lineHeight: 20, padding: 14 },
  successMessage: { backgroundColor: '#EEF8F2', color: '#0A2F25' },
  errorMessage: { backgroundColor: '#FDECEC', color: '#A1251B' },
  button: { alignItems: 'center', backgroundColor: '#008A57', borderRadius: 10, flexDirection: 'row', gap: 8, justifyContent: 'center', minHeight: 55, paddingHorizontal: 24, shadowColor: '#00633F', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.12, shadowRadius: 8, elevation: 2 },
  buttonText: { color: '#FFF', fontSize: 15.5, fontWeight: '800' },
  secondaryButton: { alignItems: 'center', borderColor: '#00754A', borderRadius: 10, borderWidth: 1, flexDirection: 'row', gap: 8, justifyContent: 'center', minHeight: 50, paddingHorizontal: 16 },
  secondaryButtonText: { color: '#0A2F25', fontSize: 14, fontWeight: '800' },
  pressed: { opacity: 0.92, transform: [{ scale: .99 }] },
  disabled: { opacity: 0.5 },
  stateScreen: { alignItems: 'center', backgroundColor: '#FFF', flex: 1, justifyContent: 'center', padding: 28 },
  stateCard: { alignItems: 'flex-start', backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 16, maxWidth: 440, padding: 24, width: '100%', shadowColor: '#173B2A', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.03, shadowRadius: 8, elevation: 1 },
  stateTitle: { color: '#0A2F25', fontFamily: 'Georgia', fontSize: 28, fontWeight: '800', lineHeight: 34 },
  stateCopy: { color: '#5B6761', fontSize: 14.5, lineHeight: 22 },
  retryButton: { alignItems: 'center', backgroundColor: '#008A57', borderRadius: 10, justifyContent: 'center', minHeight: 50, paddingHorizontal: 18 },
  retryText: { color: '#FFF', fontSize: 14, fontWeight: '800' }
});
