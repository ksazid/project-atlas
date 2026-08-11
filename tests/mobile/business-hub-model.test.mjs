import assert from 'node:assert/strict';
import test from 'node:test';
import {
  getContextPresentation,
  getHeroPresentation,
  getMenuPresentation,
  groupMenuItems,
} from '../../apps/mobile/src/features/business-hub/business-hub-model.ts';

test('hero uses first media image and falls back truthfully', () => {
  assert.deepEqual(getHeroPresentation([{ remoteUrl: 'https://cdn.example/hero.jpg', altText: 'Hasan storefront' }]), {
    kind: 'image',
    uri: 'https://cdn.example/hero.jpg',
    altText: 'Hasan storefront',
  });
  assert.deepEqual(getHeroPresentation([]), { kind: 'brand-fallback' });
});

test('hero ignores non-https media defensively', () => {
  assert.deepEqual(getHeroPresentation([{ remoteUrl: 'http://cdn.example/hero.jpg', altText: null }]), { kind: 'brand-fallback' });
});

test('menu presentation summarizes persisted intelligence without ordering language', () => {
  const view = getMenuPresentation({
    sectionCount: 6,
    itemCount: 48,
    minPrice: 2.5,
    maxPrice: 14,
    currency: 'EUR',
    preview: [],
    source: 'bolt-food',
    observedAt: '2026-08-11T12:00:00Z',
  });
  assert.equal(view.title, '48 menu items across 6 sections');
  assert.equal(view.priceRange, '€2.50–€14.00');
  assert.equal(view.actionLabel, 'View full menu');
  assert.doesNotMatch(JSON.stringify(view), /order|cart|checkout/i);
});

test('menu presentation stays truthful for empty or mixed-price intelligence', () => {
  assert.deepEqual(getMenuPresentation({
    sectionCount: 0,
    itemCount: 0,
    minPrice: null,
    maxPrice: null,
    currency: null,
    preview: [],
    source: null,
    observedAt: null,
  }), {
    title: 'No menu observed yet',
    priceRange: null,
    actionLabel: null,
    sourceLabel: null,
  });
});

test('context status maps to owner-readable copy', () => {
  assert.equal(getContextPresentation({ entryCount: 5, ownerConfirmedCount: 5, status: 'strong' }).title, 'Atlas has a strong operating picture');
  assert.equal(getContextPresentation({ entryCount: 1, ownerConfirmedCount: 1, status: 'sparse' }).actionLabel, 'Review business context');
});

test('groupMenuItems groups missing sections under Other', () => {
  const groups = groupMenuItems([
    {
      id: '1',
      section: null,
      name: 'Special',
      description: null,
      price: null,
      currency: null,
      source: 'owner',
      sourceUrl: 'https://atlas.local',
      observedAt: '2026-08-11T12:00:00Z',
      confidence: 'high',
      evidenceClass: 'owner',
      ownerConfirmed: true,
    },
    {
      id: '2',
      section: 'Beverages',
      name: 'Water',
      description: null,
      price: 2,
      currency: 'EUR',
      source: 'bolt-food',
      sourceUrl: 'https://food.bolt.eu/example',
      observedAt: '2026-08-11T12:00:00Z',
      confidence: 'high',
      evidenceClass: 'public-page',
      ownerConfirmed: false,
    },
  ]);
  assert.equal(groups[0].section, 'Beverages');
  assert.equal(groups[1].section, 'Other');
  assert.equal(groups[1].items[0].name, 'Special');
});
