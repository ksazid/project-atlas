import assert from 'node:assert/strict';
import test from 'node:test';
import {
  buildCreateBusinessFromDiscoveryRequest,
  createDiscoveryDraft,
} from '../../apps/mobile/src/features/business-discovery/discovery-model.ts';
import {
  buildConfirmedOperatingContext,
} from '../../apps/mobile/src/features/business-discovery/place-enrichment-model.ts';

const discovery = {
  snapshotId: 'snapshot-vs29',
  provider: 'website',
  sourceUrl: 'https://harbour.example',
  observedAt: '2026-08-12T05:00:00Z',
  facts: [
    { key: 'name', value: 'Harbour Coffee', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'category', value: 'restaurant-cafe', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'primaryLocation', value: '1 Republic Street, Valletta, MT', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'country', value: 'MT', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'email', value: 'hello@harbour.example', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'socialChannels', value: 'https://instagram.com/harbourcoffee/', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
    { key: 'openingHours', value: 'Mo-Fr 08:00-18:00', source: 'website', sourceUrl: 'https://harbour.example', observedAt: '2026-08-12T05:00:00Z', confidence: 'high', evidenceClass: 'public-observed', ownerConfirmed: false },
  ],
};

test('VS-29 discovery draft and submit carry public email social and hours', () => {
  const draft = {
    ...createDiscoveryDraft(discovery),
    timezone: 'Europe/Malta',
    currency: 'EUR',
  };

  assert.equal(draft.email, 'hello@harbour.example');
  assert.equal(draft.socialChannels, 'https://instagram.com/harbourcoffee/');
  assert.equal(draft.businessHours, 'Mo-Fr 08:00-18:00');

  const request = buildCreateBusinessFromDiscoveryRequest({
    ...draft,
    email: ' hello@harbour.example ',
    socialChannels: ' https://instagram.com/harbourcoffee/ ',
  });
  assert.equal(request.email, 'hello@harbour.example');
  assert.equal(request.socialChannels, 'https://instagram.com/harbourcoffee/');
});

test('VS-29 explicit About confirmation carries normalized opening hours', () => {
  const context = buildConfirmedOperatingContext({
    providerRef: 'ChIJAtlasVs29',
    operatingChannels: ['Takeaway'],
    reservable: true,
    servicePeriods: ['Lunch'],
    pricePosition: 'Moderate',
    openingHours: [
      ' Monday: 08:00-18:00 ',
      'Monday: 08:00-18:00',
      'Tuesday: 08:00-18:00',
    ],
    attributions: [],
    attributionLabel: 'Google Maps',
  });

  assert.deepEqual(context.openingHours, [
    'Monday: 08:00-18:00',
    'Tuesday: 08:00-18:00',
  ]);
});
