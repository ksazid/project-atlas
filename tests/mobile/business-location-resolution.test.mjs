import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import * as locationModel from '../../apps/mobile/src/features/business-discovery/location-model.ts';

const baseCandidate = {
  providerRef: 'place-1',
  name: 'GUN Turkish Kebab',
  formattedAddress: '65 Triq Il-Herba, Birkirkara, Malta',
  latitude: 35.90,
  longitude: 14.46,
  countryCode: 'MT',
  countryName: 'Malta',
  timezone: 'Europe/Malta',
  currency: 'EUR',
  provider: 'google-places',
  businessTypeSummary: 'Turkish · Kebab',
};

test('one strong candidate is preselected but remains changeable', () => {
  const state = locationModel.toLocationChoiceState([baseCandidate]);
  assert.equal(state.kind, 'preselected');
  assert.equal(state.selected?.providerRef, 'place-1');
  assert.equal(state.canChange, true);
});

test('multiple branches require owner selection', () => {
  const state = locationModel.toLocationChoiceState([
    baseCandidate,
    { ...baseCandidate, providerRef: 'place-2', name: 'POSH Turkish — Valletta', formattedAddress: 'Valletta, Malta' },
  ]);
  assert.equal(state.kind, 'choose');
  assert.equal(state.selected, null);
  assert.equal(state.candidates.length, 2);
});

test('no candidates requests Google location search', () => {
  const state = locationModel.toLocationChoiceState([]);
  assert.equal(state.kind, 'search');
  assert.equal(state.selected, null);
});

test('selected location supplies canonical market metadata and replaces marketplace boilerplate with public place type summary', () => {
  const applied = locationModel.applyLocationToDraft({
    primaryLocation: '',
    country: '',
    timezone: '',
    currency: '',
    description: "Open GUN Turkish Kebab on Bolt Food app to order delivery or pickup.",
  }, baseCandidate);

  assert.equal(applied.primaryLocation, '65 Triq Il-Herba, Birkirkara, Malta');
  assert.equal(applied.country, 'MT');
  assert.equal(applied.timezone, 'Europe/Malta');
  assert.equal(applied.currency, 'EUR');
  assert.equal(applied.description, 'Turkish · Kebab');
});

test('selected location does not overwrite a useful owner or public description', () => {
  const applied = locationModel.applyLocationToDraft({
    primaryLocation: '',
    country: '',
    timezone: '',
    currency: '',
    description: 'Family-run restaurant serving charcoal-grilled kebabs.',
  }, baseCandidate);

  assert.equal(applied.description, 'Family-run restaurant serving charcoal-grilled kebabs.');
});

test('onboarding screen does not ask owners to type country timezone or currency codes', () => {
  const source = readFileSync('apps/mobile/app/create-business.tsx', 'utf8');
  assert.doesNotMatch(source, /<Field label="Country"/);
  assert.doesNotMatch(source, /<Field label="Timezone"/);
  assert.doesNotMatch(source, /<Field label="Currency"/);
  assert.match(source, /Find your business location/);
  assert.match(source, /Which location are you setting up\?/);
});