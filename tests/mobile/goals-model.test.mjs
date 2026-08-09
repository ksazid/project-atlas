import assert from 'node:assert/strict';
import test from 'node:test';
import * as goalsModel from '../../apps/mobile/src/features/goals/goals-model.ts';

test('starter goals use the approved contiguous priority order', () => {
  assert.deepEqual(goalsModel.starterGoals, [
    { title: 'Increase revenue', type: 'revenue', priority: 1, isCustom: false },
    { title: 'Improve profitability', type: 'profitability', priority: 2, isCustom: false },
    { title: 'Save owner time', type: 'efficiency', priority: 3, isCustom: false }
  ]);
});

test('sorting goals is ordered and does not mutate API input', () => {
  const input = [
    { title: 'Second', type: 'custom', priority: 2, isCustom: true },
    { title: 'First', type: 'custom', priority: 1, isCustom: true }
  ];
  assert.deepEqual(goalsModel.sortGoals(input).map(goal => goal.title), ['First', 'Second']);
  assert.equal(input[0].title, 'Second');
});

test('sorting goals preserves arbitrary API priorities and stable equal-priority order', () => {
  const input = [
    { title: 'Last', type: 'custom', priority: 40, isCustom: true },
    { title: 'First tied', type: 'custom', priority: 2, isCustom: true },
    { title: 'Second tied', type: 'custom', priority: 2, isCustom: true }
  ];
  const sorted = goalsModel.sortGoals(input);
  assert.deepEqual(sorted, [
    { title: 'First tied', type: 'custom', priority: 2, isCustom: true },
    { title: 'Second tied', type: 'custom', priority: 2, isCustom: true },
    { title: 'Last', type: 'custom', priority: 40, isCustom: true }
  ]);
  assert.notStrictEqual(sorted, input);
  assert.deepEqual(input.map(goal => goal.priority), [40, 2, 2]);
});

test('moving a goal swaps only within bounds and normalizes priorities', () => {
  const moved = goalsModel.moveGoal(goalsModel.starterGoals, 2, -1);
  assert.deepEqual(moved.map(goal => [goal.title, goal.priority]), [
    ['Increase revenue', 1], ['Save owner time', 2], ['Improve profitability', 3]
  ]);
  assert.deepEqual(goalsModel.moveGoal(goalsModel.starterGoals, 0, -1), goalsModel.starterGoals);

  const nonContiguous = goalsModel.starterGoals.map((goal, index) => ({ ...goal, priority: (index + 1) * 10 }));
  const boundary = goalsModel.moveGoal(nonContiguous, 0, -1);
  const invalid = goalsModel.moveGoal(nonContiguous, nonContiguous.length, 1);
  assert.deepEqual(boundary.map(goal => goal.priority), [10, 20, 30]);
  assert.deepEqual(invalid.map(goal => goal.priority), [10, 20, 30]);
  assert.notStrictEqual(boundary, nonContiguous);
  assert.notStrictEqual(invalid, nonContiguous);
  assert.deepEqual(goalsModel.moveGoal(nonContiguous, 2, -1).map(goal => goal.priority), [1, 2, 3]);
});

test('reload and confirmed-save paths preserve server priorities exactly', () => {
  const serverGoals = [
    { id: 'later', title: 'Later', type: 'retention', priority: 80, isCustom: false },
    { id: 'first', title: 'First', type: 'revenue', priority: 5, isCustom: false }
  ];
  assert.deepEqual(goalsModel.resolveGoalsReload(serverGoals, [], false, false), {
    goals: [
      { id: 'first', title: 'First', type: 'revenue', priority: 5, isCustom: false },
      { id: 'later', title: 'Later', type: 'retention', priority: 80, isCustom: false }
    ],
    starter: false,
    preservedDraft: false
  });
  assert.deepEqual(goalsModel.resolveGoalsSaveResponse(serverGoals), {
    goals: [
      { id: 'first', title: 'First', type: 'revenue', priority: 5, isCustom: false },
      { id: 'later', title: 'Later', type: 'retention', priority: 80, isCustom: false }
    ],
    starter: false
  });
  assert.deepEqual(serverGoals.map(goal => goal.priority), [80, 5]);
});

test('goals operations serialize Retry and Save and reject stale completion tickets', () => {
  const coordinator = goalsModel.createGoalsOperationCoordinator();
  const refresh = coordinator.start('refreshing');
  assert.ok(refresh);
  assert.equal(coordinator.current(), 'refreshing');
  assert.equal(coordinator.start('refreshing'), null);
  assert.equal(coordinator.start('saving'), null);

  assert.equal(coordinator.finish({ id: refresh.id + 1, operation: 'refreshing' }), false);
  assert.equal(coordinator.current(), 'refreshing');
  assert.equal(coordinator.finish(refresh), true);

  const save = coordinator.start('saving');
  assert.ok(save);
  assert.equal(coordinator.finish(refresh), false);
  assert.equal(coordinator.current(), 'saving');
  assert.equal(coordinator.finish(save), true);
  assert.equal(coordinator.current(), 'idle');
});

test('refresh presentation disables and labels Retry and Save honestly', () => {
  assert.deepEqual(goalsModel.getGoalsRetryPresentation('refreshing'), {
    accessibilityLabel: 'Trying to load goals again',
    accessibilityState: { busy: true, disabled: true },
    ariaBusy: true,
    text: 'Trying again…'
  });
  assert.deepEqual(goalsModel.getGoalsRetryPresentation('saving'), {
    accessibilityLabel: 'Try loading goals again after saving finishes',
    accessibilityState: { busy: false, disabled: true },
    ariaBusy: false,
    text: 'Try again'
  });
  assert.deepEqual(goalsModel.getGoalsSavePresentation(false, false, true), {
    accessibilityLabel: 'Save business goals after loading finishes',
    accessibilityState: { busy: false, disabled: true },
    ariaBusy: false,
    text: 'Loading goals…'
  });
});

test('custom goals trim, reject invalid values, and append within the limit', () => {
  assert.equal(goalsModel.addCustomGoal(goalsModel.starterGoals, '  ').error, 'Enter a goal before adding it.');
  assert.equal(goalsModel.addCustomGoal(goalsModel.starterGoals, ' increase REVENUE ').error, 'That goal is already in your priorities.');
  const englishI = [{ title: 'Istanbul', type: 'custom', priority: 1, isCustom: true }];
  assert.equal(goalsModel.addCustomGoal(englishI, 'istanbul').error, 'That goal is already in your priorities.');
  const added = goalsModel.addCustomGoal(goalsModel.starterGoals, '  Build resilience  ');
  assert.equal(added.error, null);
  assert.deepEqual(added.goals.at(-1), { title: 'Build resilience', type: 'custom', priority: 4, isCustom: true });
  const ten = Array.from({ length: 10 }, (_, index) => ({ title: `Goal ${index + 1}`, type: 'custom', priority: index + 1, isCustom: true }));
  assert.equal(goalsModel.addCustomGoal(ten, 'Eleventh').error, 'Atlas supports up to 10 active goals.');
});

test('an edited starter draft survives an unavailable manual reload', () => {
  const draft = goalsModel.moveGoal(goalsModel.starterGoals, 2, -1);
  const result = goalsModel.resolveGoalsLoadFailure(draft, true, true);
  assert.deepEqual(result, {
    goals: [
      { title: 'Increase revenue', type: 'revenue', priority: 1, isCustom: false },
      { title: 'Save owner time', type: 'efficiency', priority: 2, isCustom: false },
      { title: 'Improve profitability', type: 'profitability', priority: 3, isCustom: false }
    ],
    starter: true,
    warning: 'Goals are unavailable. Your unsaved priorities are still here.'
  });
});

test('an edited custom-goal draft survives a successful manual reload', () => {
  const draft = goalsModel.addCustomGoal(goalsModel.starterGoals, 'Build resilience').goals;
  const serverGoals = [{ title: 'Replace this draft', type: 'revenue', priority: 1, isCustom: false }];
  const result = goalsModel.resolveGoalsReload(serverGoals, draft, true, true);
  assert.deepEqual(result, {
    goals: [
      { title: 'Increase revenue', type: 'revenue', priority: 1, isCustom: false },
      { title: 'Improve profitability', type: 'profitability', priority: 2, isCustom: false },
      { title: 'Save owner time', type: 'efficiency', priority: 3, isCustom: false },
      { title: 'Build resilience', type: 'custom', priority: 4, isCustom: true }
    ],
    starter: true,
    preservedDraft: true
  });
});

test('goal type formatting covers approved types and a readable fallback', () => {
  assert.deepEqual([
    goalsModel.formatGoalType('revenue'),
    goalsModel.formatGoalType('profitability'),
    goalsModel.formatGoalType('acquisition'),
    goalsModel.formatGoalType('retention'),
    goalsModel.formatGoalType('reputation'),
    goalsModel.formatGoalType('reduced_waste'),
    goalsModel.formatGoalType('reduced waste'),
    goalsModel.formatGoalType('waste-reduction'),
    goalsModel.formatGoalType('saved-time'),
    goalsModel.formatGoalType('saved time'),
    goalsModel.formatGoalType('efficiency'),
    goalsModel.formatGoalType('operational_consistency'),
    goalsModel.formatGoalType('operational consistency'),
    goalsModel.formatGoalType('custom'),
    goalsModel.formatGoalType('local_customer-growth'),
    goalsModel.formatGoalType(''),
    goalsModel.formatGoalType('  ')
  ], [
    'Revenue',
    'Profitability',
    'Customer acquisition',
    'Customer retention',
    'Business reputation',
    'Reduce waste',
    'Reduce waste',
    'Reduce waste',
    'Save owner time',
    'Save owner time',
    'Save owner time',
    'Operational consistency',
    'Operational consistency',
    'Custom goal',
    'Local customer growth',
    'Business goal',
    'Business goal'
  ]);
});

test('saving presentation exposes an enabled idle action', () => {
  assert.deepEqual(goalsModel.getGoalsSavePresentation(false, true), {
    accessibilityLabel: 'Save business goals',
    accessibilityState: { busy: false, disabled: false },
    ariaBusy: false,
    text: 'Save goals'
  });
});

test('saving presentation exposes a disabled idle action', () => {
  assert.deepEqual(goalsModel.getGoalsSavePresentation(false, false), {
    accessibilityLabel: 'Save business goals',
    accessibilityState: { busy: false, disabled: true },
    ariaBusy: false,
    text: 'Save goals'
  });
});

test('saving presentation exposes visible, web, and native busy state', () => {
  assert.deepEqual(goalsModel.getGoalsSavePresentation(true, false), {
    accessibilityLabel: 'Saving business goals',
    accessibilityState: { busy: true, disabled: true },
    ariaBusy: true,
    text: 'Saving…'
  });
});

test('goals loading state explains the work in progress', () => {
  assert.deepEqual(goalsModel.getGoalsStatePresentation('loading'), {
    title: 'Loading your goals',
    copy: 'Gathering the priorities Atlas uses to evaluate future opportunities.'
  });
});

test('missing goals state provides a recovery action', () => {
  assert.deepEqual(goalsModel.getGoalsStatePresentation('missing'), {
    title: 'No business selected',
    copy: 'Choose or create a business before you update goals.',
    action: 'Choose or create a business'
  });
});
