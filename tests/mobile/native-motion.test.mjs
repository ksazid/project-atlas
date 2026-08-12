import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import test from 'node:test';

const pressableUrl = new URL('../../apps/mobile/src/components/AtlasPressable.tsx', import.meta.url);
const createBusiness = readFileSync(new URL('../../apps/mobile/app/create-business.tsx', import.meta.url), 'utf8');

test('AtlasPressable responds on touch-down and settles without overshoot', () => {
  assert.equal(existsSync(pressableUrl), true, 'AtlasPressable must exist');
  const pressable = readFileSync(pressableUrl, 'utf8');
  assert.match(pressable, /onPressIn/);
  assert.match(pressable, /withTiming/);
  assert.match(pressable, /withSpring/);
  assert.match(pressable, /overshootClamping:\s*true/);
  assert.match(pressable, /useAtlasAccessibility/);
});

test('Create Business has no decorative looping animation', () => {
  assert.doesNotMatch(createBusiness, /Animated\.loop/);
  assert.doesNotMatch(createBusiness, /new Animated\.Value/);
});
