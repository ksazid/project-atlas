import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import * as model from '../../apps/mobile/src/features/progressive-questions/progressive-question-model.ts';
import { getSessionDestination } from '../../apps/mobile/src/auth/session-routing.ts';

const multi = {
  questionKey: 'generic.primary-channel',
  targetContextKey: 'primarychannels',
  prompt: 'How do customers usually buy from you?',
  helper: null,
  answerType: 'multi-choice',
  options: ['In person', 'Phone/message', 'Own website/app'],
  maxSelections: 2,
  maxLength: null,
};

const single = {
  ...multi,
  questionKey: 'generic.primary-constraint',
  targetContextKey: 'constraints',
  answerType: 'single-choice',
  options: ['Time', 'Staffing', 'Capacity'],
  maxSelections: 1,
};

const text = {
  ...multi,
  questionKey: 'generic.customer-groups',
  targetContextKey: 'customergroups',
  answerType: 'short-text',
  options: [],
  maxSelections: null,
  maxLength: 240,
};

test('multi-choice draft toggles immutably and respects max selections', () => {
  const empty = model.createAnswerDraft(multi);
  const first = model.toggleSelection(empty, 'In person', multi);
  const second = model.toggleSelection(first, 'Phone/message', multi);
  const blocked = model.toggleSelection(second, 'Own website/app', multi);
  const removed = model.toggleSelection(second, 'In person', multi);

  assert.deepEqual(empty.selections, []);
  assert.deepEqual(first.selections, ['In person']);
  assert.deepEqual(second.selections, ['In person', 'Phone/message']);
  assert.deepEqual(blocked.selections, ['In person', 'Phone/message']);
  assert.deepEqual(removed.selections, ['Phone/message']);
});

test('single-choice replaces the previous selection', () => {
  const first = model.toggleSelection(model.createAnswerDraft(single), 'Time', single);
  const second = model.toggleSelection(first, 'Staffing', single);
  assert.deepEqual(second.selections, ['Staffing']);
  assert.equal(model.canContinue(single, second), true);
});

test('short text keeps editing whitespace but trims only for request payload', () => {
  const draft = model.updateText(model.createAnswerDraft(text), '  Local residents and office teams  ');
  assert.equal(draft.text, '  Local residents and office teams  ');
  assert.equal(model.canContinue(text, draft), true);

  const request = model.buildAnswerRequest('1', text, draft);
  assert.equal(request.catalogueVersion, '1');
  assert.equal(request.text, 'Local residents and office teams');
  assert.equal(request.selections, null);
});

test('empty answers cannot continue and choice payload preserves selected values', () => {
  assert.equal(model.canContinue(multi, model.createAnswerDraft(multi)), false);
  assert.equal(model.canContinue(text, model.createAnswerDraft(text)), false);

  const draft = model.toggleSelection(model.createAnswerDraft(multi), 'Own website/app', multi);
  assert.deepEqual(model.buildAnswerRequest('1', multi, draft), {
    catalogueVersion: '1',
    selections: ['Own website/app'],
    text: null,
  });
});

test('progress label is human readable and bounded', () => {
  assert.equal(model.getProgressLabel(0, 4), '1 of 4');
  assert.equal(model.getProgressLabel(3, 4), '4 of 4');
  assert.equal(model.getProgressLabel(9, 4), '4 of 4');
});

test('session routing enters optional enrichment only when a Business has pending questions', () => {
  assert.equal(getSessionDestination(null, false), '/welcome');
  assert.equal(getSessionDestination({ accessToken: 'token' }, true), '/create-business');
  assert.equal(getSessionDestination({ accessToken: 'token', businessId: 'business-1' }, true), '/progressive-questions');
  assert.equal(getSessionDestination({ accessToken: 'token', businessId: 'business-1' }, false), '/(tabs)');
});

test('successful Business creation hands off to progressive questions instead of bypassing enrichment', () => {
  const source = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
  assert.match(source, /saveSession\(\{ \.\.\.session, businessId: business\.id \}\)[\s\S]{0,180}router\.replace\(['"]\/progressive-questions['"]\)/);
});

test('progressive question screen preserves the approved optional one-question Atlas experience', () => {
  const source = readFileSync('apps/mobile/app/progressive-questions.tsx', 'utf8');

  assert.match(source, /A LITTLE MORE CONTEXT/);
  assert.match(source, /Skip for now/);
  assert.match(source, /Continue for now/);
  assert.match(source, /That[’']s enough to get started\./);
  assert.match(source, /accessibilityRole="header"/);
  assert.match(source, /accessibilityState=\{\{[^}]*selected:/s);
  assert.match(source, /minHeight:\s*44/);
  assert.doesNotMatch(source, /starbucks/i);
});
