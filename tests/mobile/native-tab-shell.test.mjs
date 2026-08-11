import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const source = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('VS-28 preserves all five certified routes', () => {
  for (const route of ['index', 'profile', 'goals', 'context', 'settings']) {
    assert.match(source, new RegExp(`name="${route}"`));
  }
});

test('tab shell derives safe-area geometry and uses bounded material chrome', () => {
  assert.match(source, /getAtlasTabBarMetrics/);
  assert.match(source, /useSafeAreaInsets/);
  assert.match(source, /useWindowDimensions/);
  assert.match(source, /AtlasMaterialSurface/);
  assert.match(source, /tabBarBackground/);
  assert.doesNotMatch(source, /height:\s*76\b/);
});
