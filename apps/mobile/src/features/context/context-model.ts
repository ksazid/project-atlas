import type { BusinessContextEntry } from '@/api/atlas-client';

export const contextFields = [
  {
    key: 'customers',
    label: 'Customers',
    prompt: 'Who do you serve most often?',
    helper: 'Describe customer groups at a business level. Avoid names or other end-customer personal data.',
    hint: 'Describe the customer groups your business serves most often without entering personal data.'
  },
  {
    key: 'busyPeriods',
    label: 'Busy periods',
    prompt: 'When does demand or workload usually peak?',
    helper: 'Days, seasons, events, or operating patterns are enough. Leave this blank if it is not useful.',
    hint: 'Describe when demand or workload is usually highest.'
  },
  {
    key: 'constraints',
    label: 'Constraints',
    prompt: 'What limits your choices right now?',
    helper: 'Add only constraints that change what is practical, such as time, staffing, capacity, cash, or operating limits.',
    hint: 'Describe current limits that should shape practical guidance.'
  },
  {
    key: 'currentPriorities',
    label: 'Current priorities',
    prompt: 'What deserves attention beyond your saved goals?',
    helper: 'Use this for short-term priorities only when they materially improve the guidance Atlas can give.',
    hint: 'Describe short-term business priorities that are not already captured by your goals.'
  }
] as const;

export type ContextFieldKey = typeof contextFields[number]['key'];
export type ContextOperation = 'idle' | 'refreshing' | 'saving';
export type ContextOperationTicket = Readonly<{ id: number; operation: Exclude<ContextOperation, 'idle'> }>;
export type ContextScreenState = 'loading' | 'missing' | 'error';

function normalizeKey(key: string): string {
  return key.trim().toLowerCase();
}

function createEmptyEntry(key: ContextFieldKey): BusinessContextEntry {
  return { key, value: '', source: 'owner', ownerConfirmed: true };
}

export function createInitialContextEntries(): BusinessContextEntry[] {
  return contextFields.map(field => createEmptyEntry(field.key));
}

export function getContextEntry(entries: readonly BusinessContextEntry[], key: string): BusinessContextEntry | undefined {
  const normalized = normalizeKey(key);
  return entries.find(entry => normalizeKey(entry.key) === normalized);
}

export function mergeContextEntries(serverEntries: readonly BusinessContextEntry[]): BusinessContextEntry[] {
  const cloned = serverEntries.map(entry => ({ ...entry }));
  const knownKeys = new Set(contextFields.map(field => normalizeKey(field.key)));
  const known = contextFields.map(field => {
    const existing = cloned.find(entry => normalizeKey(entry.key) === normalizeKey(field.key));
    return existing ? { ...existing } : createEmptyEntry(field.key);
  });
  const unknown = cloned.filter(entry => !knownKeys.has(normalizeKey(entry.key))).map(entry => ({ ...entry }));
  return [...known, ...unknown];
}

export function updateContextValue(entries: readonly BusinessContextEntry[], key: ContextFieldKey, value: string): BusinessContextEntry[] {
  const normalized = normalizeKey(key);
  let found = false;
  const next = entries.map(entry => {
    if (normalizeKey(entry.key) !== normalized) return { ...entry };
    found = true;
    return { ...entry, value };
  });
  return found ? next : [...next, { ...createEmptyEntry(key), value }];
}

export function setContextConfirmation(entries: readonly BusinessContextEntry[], key: ContextFieldKey, confirmed: boolean): BusinessContextEntry[] {
  const normalized = normalizeKey(key);
  return entries.map(entry => normalizeKey(entry.key) === normalized ? { ...entry, ownerConfirmed: confirmed } : { ...entry });
}

export function buildContextSavePayload(entries: readonly BusinessContextEntry[]): BusinessContextEntry[] {
  return entries.filter(entry => entry.value.trim().length > 0).map(entry => ({ ...entry }));
}

export function getContextValidation(entries: readonly BusinessContextEntry[]): string | null {
  const unconfirmed = buildContextSavePayload(entries).find(entry => entry.source === 'public' && !entry.ownerConfirmed);
  if (!unconfirmed) return null;
  const field = contextFields.find(candidate => normalizeKey(candidate.key) === normalizeKey(unconfirmed.key));
  return field ? `Confirm the public ${field.label} context before saving.` : 'Confirm all public context before saving.';
}

export function resolveContextReload(
  serverEntries: readonly BusinessContextEntry[],
  currentEntries: readonly BusinessContextEntry[],
  hasDraftEdits: boolean
): { entries: BusinessContextEntry[]; preservedDraft: boolean } {
  if (hasDraftEdits) return { entries: currentEntries.map(entry => ({ ...entry })), preservedDraft: true };
  return { entries: mergeContextEntries(serverEntries), preservedDraft: false };
}

export function resolveContextLoadFailure(
  currentEntries: readonly BusinessContextEntry[],
  hasDraftEdits: boolean
): { entries: BusinessContextEntry[]; warning: string } {
  return {
    entries: currentEntries.map(entry => ({ ...entry })),
    warning: hasDraftEdits
      ? 'Context is unavailable. Your unsaved changes are still here.'
      : 'Context is unavailable. Your saved context is still shown.'
  };
}

export function createContextOperationCoordinator() {
  let active: ContextOperationTicket | null = null;
  let nextId = 1;
  return {
    current(): ContextOperation {
      return active?.operation ?? 'idle';
    },
    start(operation: Exclude<ContextOperation, 'idle'>): ContextOperationTicket | null {
      if (active) return null;
      active = { id: nextId++, operation };
      return active;
    },
    finish(ticket: ContextOperationTicket): boolean {
      if (!active || active.id !== ticket.id || active.operation !== ticket.operation) return false;
      active = null;
      return true;
    }
  };
}

export function getContextRetryPresentation(operation: ContextOperation) {
  const refreshing = operation === 'refreshing';
  const unavailable = operation !== 'idle';
  return {
    accessibilityLabel: refreshing
      ? 'Trying to load business context again'
      : operation === 'saving'
        ? 'Try loading business context again after saving finishes'
        : 'Try loading business context again',
    accessibilityState: { busy: refreshing, disabled: unavailable },
    ariaBusy: refreshing,
    text: refreshing ? 'Trying again…' : 'Try again'
  } as const;
}

export function getContextSavePresentation(saving: boolean, saveEnabled: boolean, refreshing = false) {
  return {
    accessibilityLabel: saving
      ? 'Saving business context'
      : refreshing
        ? 'Save business context after loading finishes'
        : 'Save business context',
    accessibilityState: { busy: saving, disabled: !saveEnabled },
    ariaBusy: saving,
    text: saving ? 'Saving…' : refreshing ? 'Loading context…' : 'Save context'
  } as const;
}

export function getContextStatePresentation(state: ContextScreenState) {
  if (state === 'loading') {
    return {
      title: 'Loading your business context',
      copy: 'Gathering the details Atlas uses to make guidance more relevant.'
    } as const;
  }
  if (state === 'missing') {
    return {
      title: 'No business selected',
      copy: 'Choose or create a business before you update context.',
      action: 'Choose or create a business'
    } as const;
  }
  return {
    title: 'Context is unavailable',
    copy: 'Atlas could not load your business context. Your account has not been changed.',
    action: 'Try again'
  } as const;
}
