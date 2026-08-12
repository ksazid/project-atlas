import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const profileRoute = readFileSync('apps/mobile/app/(tabs)/profile.tsx', 'utf8');
assert.match(profileRoute, /BusinessHubScreen/, 'Business tab must delegate to BusinessHubScreen');

const targets = [
  'apps/mobile/src/features/today-focus/TodayFocusScreen.tsx',
  'apps/mobile/src/features/business-hub/BusinessHubScreen.tsx',
  'apps/mobile/app/(tabs)/goals.tsx',
  'apps/mobile/app/(tabs)/context.tsx'
];

const sources = Object.fromEntries(targets.map(path => [path, readFileSync(path, 'utf8')]));
const sharedPressable = readFileSync('apps/mobile/src/components/AtlasPressable.tsx', 'utf8');

test('post-onboarding screens keep Atlas brand boundary and do not leak Starbucks demo content', () => {
  for (const [path, source] of Object.entries(sources)) {
    assert.match(source, /BrandMark/, `${path} should use the centralized Atlas BrandMark`);
    assert.doesNotMatch(source, /starbucks/i, `${path} must not contain Starbucks labels, URLs, or demo facts`);
  }
});

test('post-onboarding screens use the approved editorial heading treatment', () => {
  for (const [path, source] of Object.entries(sources)) {
    assert.match(source, /fontFamily:\s*'Georgia'/, `${path} should match the approved onboarding heading treatment`);
  }
});

test('post-onboarding screens use the approved clean white surface instead of the superseded generic canvas', () => {
  for (const [path, source] of Object.entries(sources)) {
    assert.doesNotMatch(source, /container:\s*\{[^}]*backgroundColor:\s*tokens\.color\.canvas/s, `${path} should not use the superseded generic page canvas`);
  }
});

test('shared native press feedback is immediate, subtle, accessible, and non-overshooting', () => {
  assert.match(sharedPressable, /onPressIn/);
  assert.match(sharedPressable, /withTiming\(pressedScale,\s*\{\s*duration:\s*70\s*\}\)/);
  assert.match(sharedPressable, /pressedScale\s*=\s*tokens\.native\.pressScale/);
  assert.match(sharedPressable, /useAtlasAccessibility/);
  assert.match(sharedPressable, /reduceMotion/);
  assert.match(sharedPressable, /overshootClamping:\s*true/);
});

test('post-onboarding interactive surfaces include immediate subtle press feedback', () => {
  for (const [path, source] of Object.entries(sources)) {
    const usesSharedPressable = /AtlasPressable/.test(source);
    const usesLocalFeedback = /pressed:\s*\{[^}]*transform:\s*\[\{\s*scale:\s*\.9[789]/s.test(source);
    assert.ok(usesSharedPressable || usesLocalFeedback, `${path} should use shared or local subtle 0.97-0.99 press feedback`);
  }
});
