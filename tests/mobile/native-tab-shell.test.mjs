import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const source = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('VS-28 preserves all five certified routes', () => {
  for (const route of ['index', 'profile', 'goals', 'context', 'settings']) {
    assert.match(source, new RegExp(`name="${route}"`));
  }
});

test('iOS top-level navigation delegates to Expo native system tabs', () => {
  assert.match(source, /expo-router\/unstable-native-tabs/);
  assert.match(source, /<NativeTabs\b/);
  assert.match(source, /<NativeTabs\.Trigger\s+name="index"/);
  assert.match(source, /<Icon\s+sf=/);
  assert.doesNotMatch(source, /import\s*\{\s*Tabs\s*\}\s*from\s*['"]expo-router['"]/);
  assert.doesNotMatch(source, /tabBarBackground/);
});
