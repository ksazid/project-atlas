import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const screen = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
const api = readFileSync('apps/mobile/src/api/business-discovery.ts', 'utf8');

test('mobile API exposes transient exact-place enrichment without local persistence', () => {
  assert.match(api, /export async function enrichBusinessPlace/);
  assert.match(api, /place-enrichment/);
  assert.match(api, /JSON\.stringify\(\{ providerRef \}\)/);
  assert.match(api, /BusinessPlaceEnrichmentResponse/);
  assert.doesNotMatch(api, /AsyncStorage|SecureStore/);
});

test('create-business loads enrichment only after a selected location and clears stale enrichment', () => {
  assert.match(screen, /enrichBusinessPlace/);
  assert.match(screen, /setPlaceEnrichment\(null\)/);
  assert.match(screen, /loadPlaceEnrichment/);
  assert.match(screen, /result\.selected/);
  assert.match(screen, /selectLocation/);
});

test('confirmation renders compact About card with required attribution and explicit owner opt-in', () => {
  assert.match(screen, /About your business/);
  assert.match(screen, /buildAboutBusinessItems/);
  assert.match(screen, /Google Maps/);
  assert.match(screen, /Confirm these operating details/);
  assert.match(screen, /accessibilityRole="checkbox"/);
  assert.match(screen, /placeEnrichmentConfirmed/);
});

test('final discovery submit includes operating context only after explicit About confirmation', () => {
  assert.match(screen, /buildConfirmedOperatingContext/);
  assert.match(screen, /placeEnrichmentConfirmed\s*&&\s*placeEnrichment/);
  assert.match(screen, /buildCreateBusinessFromDiscoveryRequest\(form,/);
});

test('place enrichment failure remains provider-neutral and non-blocking', () => {
  assert.match(screen, /Atlas could not add extra public details\. You can still continue\./);
  assert.doesNotMatch(screen, /Google Places failed|Places API failed|Google API error/i);
});
