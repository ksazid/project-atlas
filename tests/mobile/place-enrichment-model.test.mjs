import assert from 'node:assert/strict';
import test from 'node:test';
import {
  buildAboutBusinessItems,
  buildConfirmedOperatingContext,
} from '../../apps/mobile/src/features/business-discovery/place-enrichment-model.ts';

const enrichment = {
  providerRef: 'ChIJAtlas123',
  operatingChannels: ['Dine in', 'Takeaway', 'Delivery'],
  reservable: true,
  servicePeriods: ['Breakfast', 'Lunch', 'Dinner'],
  pricePosition: 'Moderate',
  openingHours: [
    'Monday: 11:00 AM – 10:00 PM',
    'Tuesday: 11:00 AM – 10:00 PM',
  ],
  attributions: [{ provider: 'Example provider', providerUri: 'https://example.com/provider' }],
  attributionLabel: 'Google Maps',
};

test('about summary shows high-value groups only and caps at five', () => {
  const items = buildAboutBusinessItems(enrichment);

  assert.ok(items.length <= 5);
  assert.deepEqual(items.map(item => item.label), ['Service', 'Reservations', 'Service periods', 'Price', 'Hours']);
  assert.equal(items[0].value, 'Dine in · Takeaway · Delivery');
  assert.equal(items[1].value, 'Reservations available');
  assert.equal(items[2].value, 'Breakfast · Lunch · Dinner');
  assert.equal(items[3].value, 'Moderate price range');
  assert.equal(items.some(item => /rating|review/i.test(`${item.label} ${item.value}`)), false);
});

test('empty enrichment produces no about card', () => {
  const emptyEnrichment = {
    ...enrichment,
    operatingChannels: [],
    reservable: null,
    servicePeriods: [],
    pricePosition: null,
    openingHours: [],
    attributions: [],
  };

  assert.deepEqual(buildAboutBusinessItems(emptyEnrichment), []);
});

test('false and unknown capabilities never become negative owner-facing claims', () => {
  const items = buildAboutBusinessItems({
    ...enrichment,
    operatingChannels: [],
    reservable: false,
    servicePeriods: [],
    pricePosition: null,
    openingHours: [],
  });

  assert.deepEqual(items, []);
});

test('confirmed operating context contains only values the About card can positively confirm', () => {
  const context = buildConfirmedOperatingContext(enrichment);

  assert.deepEqual(context, {
    providerRef: 'ChIJAtlas123',
    operatingChannels: ['Dine in', 'Takeaway', 'Delivery'],
    reservable: true,
    servicePeriods: ['Breakfast', 'Lunch', 'Dinner'],
    pricePosition: 'Moderate',
    openingHours: [
      'Monday: 11:00 AM – 10:00 PM',
      'Tuesday: 11:00 AM – 10:00 PM',
    ],
  });

  assert.equal(buildConfirmedOperatingContext({ ...enrichment, reservable: false }).reservable, null);
});
