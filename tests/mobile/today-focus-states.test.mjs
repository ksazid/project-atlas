import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const client = readFileSync('apps/mobile/src/api/atlas-client.ts', 'utf8');
const screen = readFileSync('apps/mobile/src/features/today-focus/TodayFocusScreen.tsx', 'utf8');

test('TodayFocus client contract represents every VS-23 server state', () => {
  assert.match(client, /state:\s*'ready'/);
  assert.match(client, /state:\s*'insufficient-context'/);
  assert.match(client, /state:\s*'no-focus'/);
  assert.match(client, /state:\s*'degraded'/);
  assert.match(client, /code\?:\s*string/);
});

test('Today screen handles no-focus as a truthful non-error state', () => {
  assert.match(screen, /focus\?\.state === 'no-focus'/);
  assert.match(screen, /No evidence-qualified focus yet\./);
  assert.match(screen, /Atlas will not create filler recommendations/i);
  assert.match(screen, /Review business context/);
});

test('Today screen handles degraded separately from client network error', () => {
  assert.match(screen, /focus\?\.state === 'degraded'/);
  assert.match(screen, /Atlas could not safely prepare today’s focus\./);
  assert.match(screen, /Try again/);
  assert.match(screen, /state === 'error'/);
});

test('non-ready server states never render opportunity decision controls', () => {
  const noFocusIndex = screen.indexOf("focus?.state === 'no-focus'");
  const degradedIndex = screen.indexOf("focus?.state === 'degraded'");
  const readyGuardIndex = screen.indexOf("focus?.state !== 'ready'");
  const applyIndex = screen.indexOf('Apply this move');

  assert.ok(noFocusIndex >= 0);
  assert.ok(degradedIndex >= 0);
  assert.ok(readyGuardIndex > degradedIndex, 'screen should exhaust non-ready states before ready rendering');
  assert.ok(applyIndex > readyGuardIndex, 'decision controls must appear only after the ready-state guard');
});

test('Today empty/degraded copy remains provider-neutral and accessible', () => {
  assert.doesNotMatch(screen, /Bolt Food|Wolt|Google Places|places\.googleapis/i);
  assert.match(screen, /accessibilityLiveRegion="polite"/);
  assert.match(screen, /accessibilityRole="header"/);
  assert.match(screen, /accessibilityLabel="Review business context"/);
});
