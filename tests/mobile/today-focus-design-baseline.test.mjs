import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const screen = readFileSync('apps/mobile/src/features/today-focus/TodayFocusScreen.tsx', 'utf8');
const design = readFileSync('product/DESIGN.md', 'utf8');
const visualLanguage = readFileSync('design-system/ATLAS-VISUAL-LANGUAGE.md', 'utf8');
const starbucksReference = readFileSync('design-system/references/GETDESIGN-STARBUCKS.md', 'utf8');

function segment(from, to) {
  const start = screen.indexOf(from);
  assert.ok(start >= 0, `Missing Today state marker: ${from}`);
  const end = screen.indexOf(to, start + from.length);
  assert.ok(end > start, `Missing Today state boundary: ${to}`);
  return screen.slice(start, end);
}

test('VS-24 validates against the approved Atlas and Starbucks-derived design authorities', () => {
  assert.match(design, /BrandMark/i);
  assert.match(design, /white/i);
  assert.match(design, /green/i);
  assert.match(visualLanguage, /Starbucks/i);
  assert.match(starbucksReference, /Starbucks/i);
});

test('no-focus and degraded reuse the established Today state composition', () => {
  const noFocus = segment("focus?.state === 'no-focus'", "focus?.state === 'degraded'");
  const degraded = segment("focus?.state === 'degraded'", "focus?.state !== 'ready'");

  for (const state of [noFocus, degraded]) {
    assert.match(state, /styles\.stateContainer/);
    assert.match(state, /styles\.stateIcon/);
    assert.match(state, /styles\.stateEyebrow/);
    assert.match(state, /styles\.stateTitle/);
    assert.match(state, /styles\.stateBody/);
    assert.match(state, /styles\.primaryButton/);
    assert.match(state, /styles\.secondaryWide/);
  }
});

test('non-ready states preserve one dominant action and provider-neutral owner copy', () => {
  const noFocus = segment("focus?.state === 'no-focus'", "focus?.state === 'degraded'");
  const degraded = segment("focus?.state === 'degraded'", "focus?.state !== 'ready'");

  assert.equal((noFocus.match(/styles\.primaryButton/g) ?? []).length, 1);
  assert.equal((degraded.match(/styles\.primaryButton/g) ?? []).length, 1);
  assert.doesNotMatch(`${noFocus}\n${degraded}`, /Bolt Food|Wolt|Google Places|places\.googleapis/i);
});

test('Today screen retains the established Atlas palette, editorial heading and accessible target sizes', () => {
  assert.match(screen, /const GREEN = '#00754A'/);
  assert.match(screen, /const DARK = '#0A2F25'/);
  assert.match(screen, /fontFamily: 'Georgia'/);
  assert.match(screen, /primaryButton:\s*\{[^}]*minHeight:\s*55/s);
  assert.match(screen, /secondaryWide:\s*\{[^}]*minHeight:\s*52/s);
  assert.match(screen, /accessibilityRole="header"/);
  assert.match(screen, /accessibilityLiveRegion="polite"/);
});

test('VS-24 does not introduce Starbucks customer-facing branding', () => {
  assert.doesNotMatch(screen, /Starbucks|Siren|Frappuccino|Rewards/i);
});
