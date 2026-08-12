import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const modelUrl = new URL('../../apps/mobile/src/features/feedback/feedback-model.ts', import.meta.url);
const clientSource = readFileSync(new URL('../../apps/mobile/src/api/atlas-client.ts', import.meta.url), 'utf8');

test('VS-32 feedback model exposes the approved owner choices and bounded draft helpers', async () => {
  assert.equal(existsSync(fileURLToPath(modelUrl)), true, 'feedback-model.ts must exist');
  const model = await import(modelUrl.href);

  assert.deepEqual(model.feedbackChoices.map(item => item.kind), [
    'incorrect-context',
    'unsafe-guidance',
    'general-feedback',
    'support-request',
  ]);
  assert.equal(model.normalizeFeedbackMessage('  hello  '), 'hello');
  assert.equal(model.normalizeFeedbackMessage('   '), undefined);
  assert.equal(model.validateFeedbackDraft({ kind: 'general-feedback', message: 'x'.repeat(1201) }).valid, false);
  assert.equal(model.validateFeedbackDraft({ kind: 'general-feedback', message: 'ok' }).valid, true);
});

test('VS-32 feedback model builds a trimmed provider-neutral request', async () => {
  assert.equal(existsSync(fileURLToPath(modelUrl)), true, 'feedback-model.ts must exist');
  const model = await import(modelUrl.href);

  assert.deepEqual(model.buildFeedbackInput({
    kind: 'unsafe-guidance',
    opportunityId: 'opp-1',
    message: ' note ',
  }), {
    kind: 'unsafe-guidance',
    opportunityId: 'opp-1',
    message: 'note',
  });
});

test('mobile API exposes the five-kind feedback contract and Business-scoped POST', () => {
  assert.match(clientSource, /export type FeedbackKind\s*=\s*'opportunity-rating'\s*\|\s*'incorrect-context'\s*\|\s*'unsafe-guidance'\s*\|\s*'general-feedback'\s*\|\s*'support-request'/);
  assert.match(clientSource, /export type FeedbackUsefulness\s*=\s*'useful'\s*\|\s*'not-useful'/);
  assert.match(clientSource, /export async function submitFeedback|export function submitFeedback/);
  assert.match(clientSource, /\/api\/v1\/businesses\/\$\{businessId\}\/feedback/);
  assert.match(clientSource, /method:\s*'POST'/);
});