import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const modelUrl = new URL('../../apps/mobile/src/features/operational-data/operational-data-model.ts', import.meta.url);

test('VS-38 presents truthful Drive connector states', async () => {
  assert.equal(existsSync(fileURLToPath(modelUrl)), true, 'operational data model must exist');
  const model = await import(modelUrl.href);

  assert.equal(model.presentConnector({ state: 'disconnected' }).primaryAction, 'Connect Google Drive');
  assert.equal(model.presentConnector({ state: 'syncing', folderName: 'Atlas exports' }).primaryAction, 'Syncing…');
  assert.equal(model.presentConnector({ state: 'reauthorization-required', folderName: 'Atlas exports' }).primaryAction, 'Reconnect folder');
  assert.equal(model.presentConnector({ state: 'error', folderName: 'Atlas exports' }).tone, 'warning');
});

test('VS-38 freshness uses source business date and never sync time', async () => {
  assert.equal(existsSync(fileURLToPath(modelUrl)), true, 'operational data model must exist');
  const { classifyOperationalFreshness } = await import(modelUrl.href);
  const now = new Date('2026-08-13T06:00:00.000Z');

  assert.equal(classifyOperationalFreshness('2026-08-07', now), 'fresh');
  assert.equal(classifyOperationalFreshness('2026-07-20', now), 'stale');
  assert.equal(classifyOperationalFreshness('2026-06-01', now), 'historical');
  assert.equal(classifyOperationalFreshness(null, now), 'unknown');
});

test('VS-38 schedule choices stay bounded for pilot polling', async () => {
  assert.equal(existsSync(fileURLToPath(modelUrl)), true, 'operational data model must exist');
  const { operationalScheduleChoices } = await import(modelUrl.href);
  assert.deepEqual(operationalScheduleChoices.map(choice => choice.value), ['daily', 'every-6-hours', 'manual']);
});
