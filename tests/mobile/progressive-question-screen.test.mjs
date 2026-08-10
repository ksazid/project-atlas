import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';

const path = 'apps/mobile/app/progressive-questions.tsx';

function source() {
  assert.equal(fs.existsSync(path), true, 'progressive question screen must be routable');
  return fs.readFileSync(path, 'utf8');
}

test('progressive questions render one-question onboarding with explicit optionality', () => {
  const text = source();
  assert.match(text, /A LITTLE MORE CONTEXT/);
  assert.match(text, /Question \{getProgressLabel\(/);
  assert.match(text, /Skip for now/);
  assert.match(text, /That’s enough to get started\./);
  assert.match(text, /Continue to Today/);
});

test('choice controls expose selected state and minimum target size', () => {
  const text = source();
  assert.match(text, /accessibilityRole=["']checkbox["']/);
  assert.match(text, /accessibilityState=\{\{ checked: selected/);
  assert.match(text, /minHeight:\s*44/);
});

test('screen has accessible heading, save error recovery and load-failure bypass', () => {
  const text = source();
  assert.match(text, /accessibilityRole=["']header["']/);
  assert.match(text, /Try again/);
  assert.match(text, /Continue for now/);
  assert.match(text, /accessibilityLiveRegion=["']polite["']/);
});

test('screen uses Atlas brand boundary and no Starbucks demo content', () => {
  const text = source();
  assert.match(text, /BrandMark/);
  assert.doesNotMatch(text, /Starbucks|starbucks\.com|Frappuccino|Rewards/i);
});
