import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = process.cwd();
const read = value => fs.readFileSync(path.join(root, value), 'utf8');

test('VS-38 adds a prominent Business data card to Profile without a new tab', () => {
  const hub = read('apps/mobile/src/features/business-hub/BusinessHubScreen.tsx');
  const tabs = read('apps/mobile/app/(tabs)/_layout.tsx');
  assert.match(hub, /BusinessDataCard/);
  assert.match(hub, /router\.push\('\/business-data'\)/);
  assert.equal((tabs.match(/<NativeTabs\.Trigger/g) ?? []).length, 4);
});

test('VS-38 connector screen prioritizes Drive and keeps device upload secondary', () => {
  const route = 'apps/mobile/app/business-data.tsx';
  const screen = 'apps/mobile/src/features/operational-data/OperationalDataScreen.tsx';
  const model = 'apps/mobile/src/features/operational-data/operational-data-model.ts';
  assert.ok(fs.existsSync(path.join(root, route)));
  assert.ok(fs.existsSync(path.join(root, screen)));
  const source = read(screen);
  const modelSource = read(model);
  assert.match(modelSource, /Connect Google Drive/);
  assert.match(modelSource, /Sync now/);
  assert.match(source, /presentation\.primaryAction/);
  assert.match(source, /Upload CSV from this device/);
  assert.match(source, /raw CSV stays in Google Drive/i);
  assert.match(source, /customer-identifying fields/i);
  assert.match(source, /accessibilityRole="button"/);
});
