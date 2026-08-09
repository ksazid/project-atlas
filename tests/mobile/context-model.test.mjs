import assert from 'node:assert/strict';
import test from 'node:test';
import * as contextModel from '../../apps/mobile/src/features/context/context-model.ts';

test('context fields preserve the four existing API keys in display order', () => {
  assert.deepEqual(contextModel.contextFields.map(field => field.key), [
    'customers',
    'busyPeriods',
    'constraints',
    'currentPriorities'
  ]);
});

test('initial context entries are optional owner-provided confirmed drafts', () => {
  assert.deepEqual(contextModel.createInitialContextEntries(), [
    { key: 'customers', value: '', source: 'owner', ownerConfirmed: true },
    { key: 'busyPeriods', value: '', source: 'owner', ownerConfirmed: true },
    { key: 'constraints', value: '', source: 'owner', ownerConfirmed: true },
    { key: 'currentPriorities', value: '', source: 'owner', ownerConfirmed: true }
  ]);
});

test('API entries merge case-insensitively without mutating keys, metadata, input, or unknown entries', () => {
  const serverEntries = [
    { key: 'busyperiods', value: 'Friday evenings', source: 'owner', ownerConfirmed: true },
    { key: 'customers', value: 'Local regulars', source: 'public', ownerConfirmed: false },
    { key: 'seasonality', value: 'Summer is quieter', source: 'owner', ownerConfirmed: true }
  ];
  const original = structuredClone(serverEntries);
  const merged = contextModel.mergeContextEntries(serverEntries);

  assert.equal(contextModel.getContextEntry(merged, 'busyPeriods')?.key, 'busyperiods');
  assert.equal(contextModel.getContextEntry(merged, 'busyPeriods')?.value, 'Friday evenings');
  assert.deepEqual(contextModel.getContextEntry(merged, 'customers'), serverEntries[1]);
  assert.deepEqual(contextModel.getContextEntry(merged, 'constraints'), {
    key: 'constraints', value: '', source: 'owner', ownerConfirmed: true
  });
  assert.deepEqual(merged.find(entry => entry.key === 'seasonality'), serverEntries[2]);
  assert.deepEqual(serverEntries, original);
});

test('editing changes only value and preserves source and confirmation provenance', () => {
  const entries = contextModel.mergeContextEntries([
    { key: 'customers', value: 'Public description', source: 'public', ownerConfirmed: false }
  ]);
  const updated = contextModel.updateContextValue(entries, 'customers', 'Owner corrected wording');

  assert.deepEqual(contextModel.getContextEntry(updated, 'customers'), {
    key: 'customers',
    value: 'Owner corrected wording',
    source: 'public',
    ownerConfirmed: false
  });
  assert.equal(contextModel.getContextEntry(entries, 'customers')?.value, 'Public description');
});

test('confirmation changes only the matching entry and is immutable', () => {
  const entries = contextModel.mergeContextEntries([
    { key: 'customers', value: 'Local regulars', source: 'public', ownerConfirmed: false }
  ]);
  const confirmed = contextModel.setContextConfirmation(entries, 'customers', true);

  assert.equal(contextModel.getContextEntry(confirmed, 'customers')?.ownerConfirmed, true);
  assert.equal(contextModel.getContextEntry(entries, 'customers')?.ownerConfirmed, false);
  assert.notStrictEqual(confirmed, entries);
});

test('public non-empty context requires explicit owner confirmation before save', () => {
  const entries = contextModel.mergeContextEntries([
    { key: 'customers', value: 'Local regulars', source: 'public', ownerConfirmed: false }
  ]);
  assert.equal(
    contextModel.getContextValidation(entries),
    'Confirm the public Customers context before saving.'
  );

  const confirmed = contextModel.setContextConfirmation(entries, 'customers', true);
  assert.equal(contextModel.getContextValidation(confirmed), null);
});

test('unknown public context also requires confirmation with safe generic copy', () => {
  const entries = contextModel.mergeContextEntries([
    { key: 'market-signal', value: 'Tourism demand', source: 'public', ownerConfirmed: false }
  ]);
  assert.equal(
    contextModel.getContextValidation(entries),
    'Confirm all public context before saving.'
  );
});

test('save payload omits whitespace-only fields and preserves non-empty unknown entries', () => {
  const entries = contextModel.mergeContextEntries([
    { key: 'customers', value: '   ', source: 'owner', ownerConfirmed: true },
    { key: 'busyperiods', value: ' Friday evenings ', source: 'owner', ownerConfirmed: true },
    { key: 'seasonality', value: 'Summer is quieter', source: 'owner', ownerConfirmed: true }
  ]);
  const payload = contextModel.buildContextSavePayload(entries);

  assert.deepEqual(payload, [
    { key: 'busyperiods', value: ' Friday evenings ', source: 'owner', ownerConfirmed: true },
    { key: 'seasonality', value: 'Summer is quieter', source: 'owner', ownerConfirmed: true }
  ]);
});

test('successful manual reload preserves an unsaved draft', () => {
  const current = contextModel.updateContextValue(
    contextModel.createInitialContextEntries(),
    'constraints',
    'Limited staff this week'
  );
  const server = [{ key: 'constraints', value: 'Replace me', source: 'owner', ownerConfirmed: true }];
  const result = contextModel.resolveContextReload(server, current, true);

  assert.equal(contextModel.getContextEntry(result.entries, 'constraints')?.value, 'Limited staff this week');
  assert.equal(result.preservedDraft, true);
});

test('successful reload with no draft adopts latest server state', () => {
  const current = contextModel.createInitialContextEntries();
  const server = [{ key: 'currentpriorities', value: 'Prepare holiday offer', source: 'owner', ownerConfirmed: true }];
  const result = contextModel.resolveContextReload(server, current, false);

  assert.equal(contextModel.getContextEntry(result.entries, 'currentPriorities')?.value, 'Prepare holiday offer');
  assert.equal(result.preservedDraft, false);
});

test('failed manual reload preserves an unsaved draft with honest copy', () => {
  const current = contextModel.updateContextValue(
    contextModel.createInitialContextEntries(),
    'customers',
    'Local regulars'
  );
  assert.deepEqual(contextModel.resolveContextLoadFailure(current, true), {
    entries: current,
    warning: 'Context is unavailable. Your unsaved changes are still here.'
  });
});

test('failed manual reload keeps currently displayed context when there is no draft', () => {
  const current = contextModel.mergeContextEntries([
    { key: 'customers', value: 'Local regulars', source: 'owner', ownerConfirmed: true }
  ]);
  assert.deepEqual(contextModel.resolveContextLoadFailure(current, false), {
    entries: current,
    warning: 'Context is unavailable. Your saved context is still shown.'
  });
});

test('context operations serialize Retry and Save and reject stale completion tickets', () => {
  const coordinator = contextModel.createContextOperationCoordinator();
  const refresh = coordinator.start('refreshing');
  assert.ok(refresh);
  assert.equal(coordinator.current(), 'refreshing');
  assert.equal(coordinator.start('saving'), null);
  assert.equal(coordinator.finish({ id: refresh.id + 1, operation: 'refreshing' }), false);
  assert.equal(coordinator.current(), 'refreshing');
  assert.equal(coordinator.finish(refresh), true);

  const save = coordinator.start('saving');
  assert.ok(save);
  assert.equal(coordinator.start('refreshing'), null);
  assert.equal(coordinator.finish(refresh), false);
  assert.equal(coordinator.finish(save), true);
  assert.equal(coordinator.current(), 'idle');
});

test('retry presentation exposes visible, web, and native busy state', () => {
  assert.deepEqual(contextModel.getContextRetryPresentation('refreshing'), {
    accessibilityLabel: 'Trying to load business context again',
    accessibilityState: { busy: true, disabled: true },
    ariaBusy: true,
    text: 'Trying again…'
  });
  assert.deepEqual(contextModel.getContextRetryPresentation('saving'), {
    accessibilityLabel: 'Try loading business context again after saving finishes',
    accessibilityState: { busy: false, disabled: true },
    ariaBusy: false,
    text: 'Try again'
  });
});

test('save presentation reports idle, refreshing, and saving states honestly', () => {
  assert.deepEqual(contextModel.getContextSavePresentation(false, true, false), {
    accessibilityLabel: 'Save business context',
    accessibilityState: { busy: false, disabled: false },
    ariaBusy: false,
    text: 'Save context'
  });
  assert.deepEqual(contextModel.getContextSavePresentation(false, false, true), {
    accessibilityLabel: 'Save business context after loading finishes',
    accessibilityState: { busy: false, disabled: true },
    ariaBusy: false,
    text: 'Loading context…'
  });
  assert.deepEqual(contextModel.getContextSavePresentation(true, false, false), {
    accessibilityLabel: 'Saving business context',
    accessibilityState: { busy: true, disabled: true },
    ariaBusy: true,
    text: 'Saving…'
  });
});

test('screen state presentation has loading, missing, and recoverable error copy', () => {
  assert.deepEqual(contextModel.getContextStatePresentation('loading'), {
    title: 'Loading your business context',
    copy: 'Gathering the details Atlas uses to make guidance more relevant.'
  });
  assert.deepEqual(contextModel.getContextStatePresentation('missing'), {
    title: 'No business selected',
    copy: 'Choose or create a business before you update context.',
    action: 'Choose or create a business'
  });
  assert.deepEqual(contextModel.getContextStatePresentation('error'), {
    title: 'Context is unavailable',
    copy: 'Atlas could not load your business context. Your account has not been changed.',
    action: 'Try again'
  });
});
