import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const source = await readFile(new URL('../../apps/mobile/app/create-business.tsx', import.meta.url), 'utf8');

test('VS-29 business review exposes editable public presence fields without a second flow', () => {
  assert.match(source, /<Field label="Website \(optional\)"/);
  assert.match(source, /<Field label="Phone \(optional\)"/);
  assert.match(source, /<Field label="Business email \(optional\)"/);
  assert.match(source, /<Field label="Social channels \(optional\)"/);
  assert.match(source, /<Field label="Opening hours \(optional\)"/);
  assert.doesNotMatch(source, /router\.push\(['"]\/business-presence/);
});

test('VS-29 confirmation summary renders enriched presence only when values exist', () => {
  assert.match(source, /form\.email\?\.trim\(\)/);
  assert.match(source, /form\.socialChannels\?\.trim\(\)/);
  assert.match(source, /Observed publicly/);
});
