import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const source = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('VS-31 exposes exactly the four approved persistent destinations', () => {
  const triggers = [...source.matchAll(/<NativeTabs\.Trigger\s+name="([^"]+)"/g)].map(match => match[1]);
  assert.deepEqual(triggers, ['index', 'history', 'goals', 'profile']);

  for (const label of ['Today', 'History', 'Goals', 'Profile']) {
    assert.match(source, new RegExp(`<Label>${label}</Label>`));
  }

  assert.doesNotMatch(source, /<NativeTabs\.Trigger\s+name="context"/);
  assert.doesNotMatch(source, /<NativeTabs\.Trigger\s+name="settings"/);
});

test('top-level navigation continues to delegate to Expo native system tabs', () => {
  assert.match(source, /expo-router\/unstable-native-tabs/);
  assert.match(source, /<NativeTabs\b/);
  assert.match(source, /<NativeTabs\.Trigger\s+name="index"/);
  assert.match(source, /<Icon\s+sf=/);
  assert.doesNotMatch(source, /import\s*\{\s*Tabs\s*\}\s*from\s*['"]expo-router['"]/);
  assert.doesNotMatch(source, /tabBarBackground/);
});
