import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = process.cwd();
const read = value => fs.readFileSync(path.join(root, value), 'utf8');

test('VS-45 Profile reads connector freshness without blocking the business hub', () => {
  const source = read('apps/mobile/src/features/business-hub/BusinessHubScreen.tsx');
  assert.ok(source.includes('getOperationalConnector'));
  assert.ok(source.includes('.catch(() => null)'));
  assert.ok(source.includes('liveSummary={formatBusinessDataSummary(connector)}'));
});

test('VS-45 only presents Live after a successful sync', () => {
  const source = read('apps/mobile/src/features/business-hub/BusinessHubScreen.tsx');
  assert.ok(source.includes('connector.lastSuccessfulSyncAt'));
  assert.ok(source.includes('Live business data · synced'));
  assert.ok(source.includes('waiting for the first successful sync'));
  assert.ok(source.includes('needs attention before Atlas can refresh live signals'));
});
