import assert from 'node:assert/strict';
import test from 'node:test';
import {
  canonicalBusinessUrlKey,
  canonicalizeBusinessUrlInput,
} from '../../apps/mobile/src/features/business-discovery/url-policy.ts';

test('pasted Bolt share text becomes the canonical business URL', () => {
  const result = canonicalizeBusinessUrlInput(
    "Antalya Kebab St. Julian's - Bolt Food https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_source=share_provider&utm_medium=product&utm_content=menu_header",
  );

  assert.equal(result.complete, true);
  assert.equal(result.error, null);
  assert.equal(result.value, 'https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians');
});

test('Google Maps share tracking is removed in the visible canonical URL', () => {
  const result = canonicalizeBusinessUrlInput('https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86?g_st=ic');

  assert.equal(result.complete, true);
  assert.equal(result.error, null);
  assert.equal(result.value, 'https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86');
});

test('partial HTTPS input is not destructively rewritten while typing', () => {
  for (const value of ['h', 'https', 'https://', 'https://food.bolt.eu/']) {
    const result = canonicalizeBusinessUrlInput(value);
    assert.equal(result.value, value);
    assert.equal(result.complete, false);
  }
});

test('complete unsafe or generic provider URLs return inline validation errors', () => {
  for (const value of [
    'http://example.com/business',
    'https://user:password@example.com/business',
    'https://127.0.0.1/business',
    'https://food.bolt.eu/en/324',
    'https://wolt.com/en/mlt',
    'https://google.com/search?q=antalya+kebab',
  ]) {
    const result = canonicalizeBusinessUrlInput(value);
    assert.equal(result.complete, true, value);
    assert.ok(result.error, value);
  }
});

test('ambiguous paste containing more than one URL is rejected rather than guessed', () => {
  const result = canonicalizeBusinessUrlInput('https://example.com/a https://example.com/b');

  assert.equal(result.complete, true);
  assert.ok(result.error);
});

test('canonical duplicate keys ignore tracking contamination', () => {
  const first = canonicalBusinessUrlKey(
    'https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_source=share_provider',
  );
  const second = canonicalBusinessUrlKey(
    'https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_medium=product',
  );

  assert.ok(first);
  assert.equal(first, second);
});
