import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const brandMarkSource = readFileSync(new URL('../../apps/mobile/src/components/BrandMark.tsx', import.meta.url), 'utf8');
const iconSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasIcon.tsx', import.meta.url), 'utf8');
const layoutSource = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('Atlas brand mark is local and contains no Starbucks prototype dependency', () => {
  assert.doesNotMatch(brandMarkSource, /starbucks|wikimedia|PROTOTYPE_MARK_URI/i);
  assert.doesNotMatch(brandMarkSource, /source=\{\{\s*uri:/);
  assert.match(brandMarkSource, /Atlas brand mark/);
  assert.match(brandMarkSource, /Compass|orbit|directional|BrandMark/i);
});

test('five existing PES tab routes remain unchanged and use local Atlas icons', () => {
  for (const route of ['index', 'profile', 'goals', 'context', 'settings']) {
    assert.match(layoutSource, new RegExp(`name="${route}"`));
  }
  for (const glyph of ['⌂', '◎', '↗', '◌', '⚙']) {
    assert.ok(!layoutSource.includes(glyph));
  }
  assert.match(layoutSource, /AtlasIcon/);
});

test('AtlasIcon exposes one local icon family without emoji or remote assets', () => {
  for (const name of ['home', 'business', 'goals', 'context', 'settings']) {
    assert.match(iconSource, new RegExp(`'${name}'`));
  }
  assert.doesNotMatch(iconSource, /https?:\/\/|Image\s*[,}]/);
});
