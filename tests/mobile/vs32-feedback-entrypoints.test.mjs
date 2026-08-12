import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const opportunitySource = readFileSync(new URL('../../apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx', import.meta.url), 'utf8');
const contextSource = readFileSync(new URL('../../apps/mobile/src/features/context/ContextScreen.tsx', import.meta.url), 'utf8');
const settingsSource = readFileSync(new URL('../../apps/mobile/src/features/settings/SettingsScreen.tsx', import.meta.url), 'utf8');
const tabLayoutSource = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('Opportunity Detail records usefulness separately from action lifecycle', () => {
  assert.match(opportunitySource, /Was this Opportunity useful\?/);
  assert.match(opportunitySource, />Useful</);
  assert.match(opportunitySource, />Not useful</);
  assert.match(opportunitySource, /submitFeedback/);
  assert.match(opportunitySource, /kind:\s*'opportunity-rating'/);
  assert.match(opportunitySource, /usefulness/);
  assert.match(opportunitySource, /opportunityId/);
  assert.doesNotMatch(opportunitySource, /recordActionDecision|decideOpportunity/);
});

test('Opportunity Detail links unsafe guidance to the reusable feedback screen', () => {
  assert.match(opportunitySource, /Report unsafe guidance/);
  assert.match(opportunitySource, /\/feedback/);
  assert.match(opportunitySource, /unsafe-guidance/);
  assert.match(opportunitySource, /opportunityId/);
});

test('Context and Settings expose the approved feedback entry points', () => {
  assert.match(contextSource, /Report incorrect context/);
  assert.match(contextSource, /\/feedback/);
  assert.match(contextSource, /incorrect-context/);
  assert.match(contextSource, /edit.*context|context.*edit/i);

  assert.match(settingsSource, /Feedback & support/);
  assert.match(settingsSource, /router\.push\('\/feedback'\)/);
});

test('Feedback remains a pushed detail and never becomes a persistent tab', () => {
  assert.doesNotMatch(tabLayoutSource, /name=["']feedback["']/);
});
