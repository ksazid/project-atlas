import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import * as discoveryModel from '../../apps/mobile/src/features/business-discovery/discovery-model.ts';

const discovery = {
  snapshotId: 'snapshot-1',
  provider: 'website',
  sourceUrl: 'https://harbour.example',
  observedAt: '2026-08-09T20:00:00Z',
  facts: [
    { key: 'name', value: 'Harbour Coffee', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-09T20:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'category', value: 'restaurant-cafe', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-09T20:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'subcategory', value: 'cafe', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-09T20:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'primaryLocation', value: '1 Republic Street, Valletta, MT', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-09T20:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'country', value: 'MT', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-09T20:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'description', value: 'Independent coffee shop', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-09T20:00:00Z', confidence: 'medium', evidenceClass: 'public-observed', ownerConfirmed: false },
  ],
};

test('discovery draft uses observed facts and leaves unavailable facts unknown', () => {
  const draft = discoveryModel.createDiscoveryDraft(discovery);
  assert.equal(draft.snapshotId, 'snapshot-1');
  assert.equal(draft.name, 'Harbour Coffee');
  assert.equal(draft.category, 'restaurant-cafe');
  assert.equal(draft.subcategory, 'cafe');
  assert.equal(draft.primaryLocation, '1 Republic Street, Valletta, MT');
  assert.equal(draft.country, 'MT');
  assert.equal(draft.description, 'Independent coffee shop');
  assert.equal(draft.timezone, '');
  assert.equal(draft.currency, '');
  assert.equal(draft.phone, '');
  assert.equal(draft.businessHours, '');
});

test('missing technical market metadata is presented to the owner as one unresolved business location', () => {
  const missing = discoveryModel.getMissingRequiredFields(discoveryModel.createDiscoveryDraft(discovery));
  assert.deepEqual(missing, ['location']);
});

test('confirmation request consumes canonical metadata from the resolved location', () => {
  const draft = {
    ...discoveryModel.createDiscoveryDraft(discovery),
    timezone: 'Europe/Malta',
    currency: 'EUR',
  };
  const request = discoveryModel.buildCreateBusinessFromDiscoveryRequest(draft);
  assert.equal(request.snapshotId, 'snapshot-1');
  assert.equal(request.ownerConfirmed, true);
  assert.equal(request.name, 'Harbour Coffee');
  assert.equal(request.country, 'MT');
  assert.equal(request.timezone, 'Europe/Malta');
  assert.equal(request.currency, 'EUR');
});

test('confirmation request carries only explicitly confirmed operating context', () => {
  const draft = {
    ...discoveryModel.createDiscoveryDraft(discovery),
    timezone: 'Europe/Malta',
    currency: 'EUR',
  };
  const confirmedOperatingContext = {
    providerRef: 'ChIJAtlas123',
    operatingChannels: ['Dine in', 'Takeaway', 'Delivery'],
    reservable: true,
    servicePeriods: ['Lunch', 'Dinner'],
    pricePosition: 'Moderate',
  };

  const withContext = discoveryModel.buildCreateBusinessFromDiscoveryRequest(draft, confirmedOperatingContext);
  assert.deepEqual(withContext.confirmedOperatingContext, confirmedOperatingContext);

  const withoutContext = discoveryModel.buildCreateBusinessFromDiscoveryRequest(draft);
  assert.equal(withoutContext.confirmedOperatingContext, undefined);
});

test('fact lookup preserves provenance for trust presentation', () => {
  const name = discoveryModel.getDiscoveryFact(discovery, 'name');
  assert.equal(name?.source, 'website');
  assert.equal(name?.confidence, 'high');
  assert.equal(name?.evidenceClass, 'public-observed');
  assert.equal(name?.ownerConfirmed, false);
  assert.equal(discoveryModel.getDiscoveryFact(discovery, 'openingHours'), undefined);
});

test('owner-facing confirmation uses provider-neutral public-source copy', () => {
  const source = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
  assert.match(source, /Public business page/);
  assert.match(source, /Observed from public business page/);
  assert.doesNotMatch(source, /providerLabel\(discovery\.provider\)/);
  assert.doesNotMatch(source, /sourceHost\(discovery\.sourceUrl\)/);
  assert.doesNotMatch(source, /Bolt Food|Wolt/i);
});

test('approved discovery screen contains no fabricated Starbucks demo facts', () => {
  const source = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
  assert.doesNotMatch(source, /starbucks/i);
  assert.doesNotMatch(source, /12,847|4\.6\s*⭐|\+1 \(415\)|6:00 AM – 10:00 PM/);
  assert.doesNotMatch(source, /reviews,\s*social profiles/i);
});

test('primary discovery URL can be cleared in one accessible action', () => {
  const source = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
  assert.match(source, /accessibilityLabel="Clear business page URL"/);
  assert.match(source, /accessibilityHint="Clear primary business page URL"/);
  assert.match(source, /removeSourceUrl\(0\)/);
  assert.match(source, />×<\/Text>/);
});

test('discovery screen respects reduced motion and exposes explicit action semantics', () => {
  const source = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
  assert.match(source, /AccessibilityInfo/);
  assert.match(source, /isReduceMotionEnabled/);
  for (const label of ['Discover my business', 'Clear business page URL', 'Set up manually instead', 'Edit details', 'Review details', 'Create business', 'Confirm and continue', 'Change location', 'Search Google Maps']) {
    assert.match(source, new RegExp(`accessibilityLabel=["'{][^\\n]*${label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`, 'i'), `Missing explicit accessibility label for ${label}`);
  }
  assert.match(source, /Which location are you setting up\?/);
  assert.match(source, /Find your business location/);
  assert.doesNotMatch(source, /<Field label="Country"/);
  assert.doesNotMatch(source, /<Field label="Timezone"/);
  assert.doesNotMatch(source, /<Field label="Currency"/);
});
