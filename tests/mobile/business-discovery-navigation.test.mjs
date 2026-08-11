import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const screen = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');

test('business discovery Back checks navigator history before going back', () => {
  assert.match(screen, /router\.canGoBack\(\)/);
  assert.match(screen, /router\.back\(\)/);
  assert.match(screen, /router\.replace\('\/sign-in'\)/);
});
