import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const routeUrl = new URL('../../apps/mobile/app/feedback.tsx', import.meta.url);
const screenUrl = new URL('../../apps/mobile/src/features/feedback/FeedbackScreen.tsx', import.meta.url);
const modelUrl = new URL('../../apps/mobile/src/features/feedback/feedback-model.ts', import.meta.url);

test('Feedback & Support is a pushed Stack detail with accessible Profile fallback', () => {
  assert.equal(existsSync(fileURLToPath(routeUrl)), true, 'feedback route must exist');
  const route = readFileSync(routeUrl, 'utf8');
  assert.match(route, /<Stack\.Screen/);
  assert.match(route, /headerShown:\s*true/);
  assert.match(route, /Feedback & support/);
  assert.match(route, /accessibilityLabel="Back to Profile"/);
  assert.match(route, /router\.canGoBack\(\)/);
  assert.match(route, /router\.replace\('\/(?:\(tabs\)\/)?profile'\)/);
});

test('Feedback & Support consumes the approved choices and renders privacy guidance with a bounded note', () => {
  assert.equal(existsSync(fileURLToPath(screenUrl)), true, 'FeedbackScreen must exist');
  const source = readFileSync(screenUrl, 'utf8');
  const model = readFileSync(modelUrl, 'utf8');

  assert.match(source, /feedbackChoices/);
  for (const label of ['Incorrect business context', 'Unsafe guidance', 'General feedback', 'Support request']) {
    assert.match(model, new RegExp(label));
  }
  assert.match(source, /maxLength=\{1200\}/);
  assert.match(source, /customer names/i);
  assert.match(source, /contact details/i);
  assert.match(source, /submitFeedback/);
  assert.match(source, /submitting/);
  assert.match(source, /Could not|try again/i);
  assert.match(source, /recorded/i);
  assert.match(source, /review/i);
  assert.doesNotMatch(source, /upload|attachment|document picker/i);
});

test('Feedback & Support preserves kind and Opportunity deep-link parameters', () => {
  assert.equal(existsSync(fileURLToPath(screenUrl)), true, 'FeedbackScreen must exist');
  const source = readFileSync(screenUrl, 'utf8');
  assert.match(source, /useLocalSearchParams/);
  assert.match(source, /kind/);
  assert.match(source, /opportunityId/);
  assert.match(source, /buildFeedbackInput/);
  assert.match(source, /loadSession/);
});