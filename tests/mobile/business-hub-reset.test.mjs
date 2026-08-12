import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';

const read = path => fs.readFileSync(path, 'utf8');

test('deleted or stale business selection returns to business setup instead of an error state', () => {
  const api = read('apps/mobile/src/features/business-hub/business-hub-api.ts');
  const screen = read('apps/mobile/src/features/business-hub/BusinessHubScreen.tsx');
  const session = read('apps/mobile/src/auth/session.ts');

  assert.match(api, /response\.status === 404/);
  assert.match(api, /state: 'missing'/);
  assert.match(screen, /result\.state === 'missing'/);
  assert.match(screen, /clearBusinessSelection/);
  assert.match(screen, /router\.replace\('\/create-business'\)/);
  assert.match(screen, /Set up your business/);
  assert.match(session, /clearBusinessSelection/);
  assert.match(session, /deleteItemAsync\(BUSINESS_ID_KEY\)/);
});

test('Expo reset is visible only for the Development demo session and preserves sign-in', () => {
  const settings = read('apps/mobile/src/features/settings/SettingsScreen.tsx');
  const session = read('apps/mobile/src/auth/session.ts');

  assert.match(settings, /__DEV__/);
  assert.match(settings, /atlas-expo-go-demo/);
  assert.match(settings, /Reset test business/);
  assert.match(settings, /resetExpoDemoBusiness/);
  assert.match(settings, /clearBusinessSelection/);
  assert.match(settings, /router\.replace\('\/create-business'\)/);
  assert.doesNotMatch(session.match(/clearBusinessSelection[\s\S]*?\n\}/)?.[0] ?? '', /ACCESS_TOKEN_KEY/);
});