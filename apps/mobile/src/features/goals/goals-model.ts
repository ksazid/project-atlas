import type { BusinessGoal } from '@/api/atlas-client';

export const MAX_GOALS = 10;
export const starterGoals = [
  { title: 'Increase revenue', type: 'revenue', priority: 1, isCustom: false },
  { title: 'Improve profitability', type: 'profitability', priority: 2, isCustom: false },
  { title: 'Save owner time', type: 'efficiency', priority: 3, isCustom: false }
] as const satisfies readonly BusinessGoal[];

function normalize(goals: readonly BusinessGoal[]): BusinessGoal[] {
  return goals.map((goal, index) => ({ ...goal, priority: index + 1 }));
}

export function sortGoals(goals: readonly BusinessGoal[]): BusinessGoal[] {
  return [...goals].sort((left, right) => left.priority - right.priority);
}

export function moveGoal(goals: readonly BusinessGoal[], index: number, delta: -1 | 1): BusinessGoal[] {
  const target = index + delta;
  if (index < 0 || index >= goals.length || target < 0 || target >= goals.length) return [...goals];
  const next = [...goals];
  [next[index], next[target]] = [next[target], next[index]];
  return normalize(next);
}

export function addCustomGoal(goals: readonly BusinessGoal[], input: string): { goals: BusinessGoal[]; error: string | null } {
  const title = input.trim();
  if (!title) return { goals: [...goals], error: 'Enter a goal before adding it.' };
  if (goals.some(goal => goal.title.trim().toLowerCase() === title.toLowerCase())) return { goals: [...goals], error: 'That goal is already in your priorities.' };
  if (goals.length >= MAX_GOALS) return { goals: [...goals], error: 'Atlas supports up to 10 active goals.' };
  return { goals: normalize([...goals, { title, type: 'custom', priority: goals.length + 1, isCustom: true }]), error: null };
}

export function resolveGoalsReload(serverGoals: readonly BusinessGoal[], currentGoals: readonly BusinessGoal[], currentStarter: boolean, hasDraftEdits: boolean): { goals: BusinessGoal[]; starter: boolean; preservedDraft: boolean } {
  if (hasDraftEdits) return { goals: [...currentGoals], starter: currentStarter, preservedDraft: true };
  return { goals: serverGoals.length ? sortGoals(serverGoals) : [...starterGoals], starter: serverGoals.length === 0, preservedDraft: false };
}

export function resolveGoalsSaveResponse(serverGoals: readonly BusinessGoal[]): { goals: BusinessGoal[]; starter: false } {
  return { goals: sortGoals(serverGoals), starter: false };
}

export function resolveGoalsLoadFailure(currentGoals: readonly BusinessGoal[], currentStarter: boolean, hasDraftEdits: boolean): { goals: BusinessGoal[]; starter: boolean; warning: string } {
  if (hasDraftEdits) {
    return {
      goals: [...currentGoals],
      starter: currentStarter,
      warning: 'Goals are unavailable. Your unsaved priorities are still here.'
    };
  }
  return {
    goals: [...starterGoals],
    starter: true,
    warning: 'Goals are unavailable. Starter goals are shown and have not been saved.'
  };
}

const goalTypeLabels: Record<string, string> = {
  revenue: 'Revenue',
  profitability: 'Profitability',
  acquisition: 'Customer acquisition',
  retention: 'Customer retention',
  reputation: 'Business reputation',
  'reduced-waste': 'Reduce waste',
  'reduced waste': 'Reduce waste',
  reduced_waste: 'Reduce waste',
  'waste-reduction': 'Reduce waste',
  waste_reduction: 'Reduce waste',
  'saved-time': 'Save owner time',
  'saved time': 'Save owner time',
  saved_time: 'Save owner time',
  efficiency: 'Save owner time',
  'operational-consistency': 'Operational consistency',
  operational_consistency: 'Operational consistency',
  custom: 'Custom goal'
};

export function formatGoalType(type: string): string {
  const normalized = type.trim().toLowerCase();
  if (!normalized) return 'Business goal';
  if (goalTypeLabels[normalized]) return goalTypeLabels[normalized];
  const readable = normalized
    .split(/[\s_-]+/)
    .filter(Boolean)
    .join(' ');
  return readable ? `${readable.charAt(0).toUpperCase()}${readable.slice(1)}` : 'Business goal';
}

export type GoalsOperation = 'idle' | 'refreshing' | 'saving';
export type GoalsOperationTicket = Readonly<{ id: number; operation: Exclude<GoalsOperation, 'idle'> }>;

export function createGoalsOperationCoordinator() {
  let active: GoalsOperationTicket | null = null;
  let nextId = 1;
  return {
    current(): GoalsOperation {
      return active?.operation ?? 'idle';
    },
    start(operation: Exclude<GoalsOperation, 'idle'>): GoalsOperationTicket | null {
      if (active) return null;
      active = { id: nextId++, operation };
      return active;
    },
    finish(ticket: GoalsOperationTicket): boolean {
      if (!active || active.id !== ticket.id || active.operation !== ticket.operation) return false;
      active = null;
      return true;
    }
  };
}

export function getGoalsRetryPresentation(operation: GoalsOperation) {
  const refreshing = operation === 'refreshing';
  const unavailable = operation !== 'idle';
  return {
    accessibilityLabel: refreshing ? 'Trying to load goals again' : operation === 'saving' ? 'Try loading goals again after saving finishes' : 'Try loading goals again',
    accessibilityState: { busy: refreshing, disabled: unavailable },
    ariaBusy: refreshing,
    text: refreshing ? 'Trying again…' : 'Try again'
  } as const;
}

export function getGoalsSavePresentation(saving: boolean, saveEnabled: boolean, refreshing = false) {
  return {
    accessibilityLabel: saving ? 'Saving business goals' : refreshing ? 'Save business goals after loading finishes' : 'Save business goals',
    accessibilityState: { busy: saving, disabled: !saveEnabled },
    ariaBusy: saving,
    text: saving ? 'Saving…' : refreshing ? 'Loading goals…' : 'Save goals'
  } as const;
}

export function getGoalsStatePresentation(state: 'loading' | 'missing') {
  return state === 'loading'
    ? { title: 'Loading your goals', copy: 'Gathering the priorities Atlas uses to evaluate future opportunities.' }
    : { title: 'No business selected', copy: 'Choose or create a business before you update goals.', action: 'Choose or create a business' };
}
