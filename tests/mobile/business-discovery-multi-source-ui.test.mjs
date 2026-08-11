import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const screen = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
const api = readFileSync('apps/mobile/src/api/business-discovery.ts', 'utf8');

test('discovery screen owns one primary URL row and at most two optional rows', () => {
  assert.match(screen, /sourceUrls/);
  assert.match(screen, /useState<string\[\]>\(\[''\]\)/);
  assert.match(screen, /sourceUrls\.length\s*<\s*3/);
  assert.match(screen, /Add another business page URL/);
});

test('primary source clears while optional sources are removed and priority shifts upward', () => {
  assert.match(screen, /Clear primary business page URL/);
  assert.match(screen, /Remove additional business page URL/);
  assert.match(screen, /sourceUrls\.filter|filter\(\(_,.*index/);
});

test('URL rows canonicalize complete pasted values immediately and block canonical duplicates', () => {
  assert.match(screen, /canonicalizeBusinessUrlInput/);
  assert.match(screen, /canonicalBusinessUrlKey/);
  assert.match(screen, /already added/i);
  assert.match(screen, /seen\.get\(key\)/);
});

test('discovery submission preserves owner source priority and ignores empty optional rows', () => {
  assert.match(screen, /sourceUrls\[0\]/);
  assert.match(screen, /sourceUrls\.slice\(1\)/);
  assert.match(screen, /\.filter\(Boolean\)/);
  assert.match(screen, /discoverBusiness\([^\n]*sourceUrls\[0\][^\n]*additional|discoverBusiness\([^\n]*sourceUrls\[0\]/s);
});

test('mobile API sends primary URL plus ordered additionalUrls', () => {
  assert.match(api, /additionalUrls\?:\s*string\[\]/);
  assert.match(api, /JSON\.stringify\(\{\s*url,\s*additionalUrls\s*\}\)/);
});

test('confirmation remains provider-neutral with multiple public sources', () => {
  assert.match(screen, /Public business page/);
  assert.doesNotMatch(screen, /Observed from Bolt Food|Observed from Wolt|Google Places/i);
});
