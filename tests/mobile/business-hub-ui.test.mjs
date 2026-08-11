import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const read = relative => readFileSync(new URL(relative, import.meta.url), 'utf8');
const hubSource = read('../../apps/mobile/src/features/business-hub/BusinessHubScreen.tsx');
const profileRouteSource = read('../../apps/mobile/app/(tabs)/profile.tsx');
const editRouteSource = read('../../apps/mobile/app/edit-business.tsx');
const menuScreenSource = read('../../apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx');
const menuRouteSource = read('../../apps/mobile/app/business-menu.tsx');

test('Business tab is a read-first business hub with approved sections and secondary actions', () => {
  for (const text of ['BUSINESS', 'Business photos', 'Menu intelligence', 'Edit business details', 'Review business context']) {
    assert.match(hubSource, new RegExp(text));
  }
  assert.match(hubSource, /getBusinessHub/);
  assert.match(profileRouteSource, /BusinessHubScreen/);
  assert.doesNotMatch(profileRouteSource, /TextInput/);
  assert.match(editRouteSource, /saveProfile/);
  for (const forbidden of ['Add to cart', 'Order now', 'Checkout']) {
    assert.ok(!hubSource.includes(forbidden));
  }
});

test('Business Hub has explicit loading, missing, error and media fallback behavior', () => {
  for (const state of ['loading', 'ready', 'missing', 'error']) assert.match(hubSource, new RegExp(`'${state}'`));
  assert.match(hubSource, /Try again/);
  assert.match(hubSource, /BrandMark/);
  assert.match(hubSource, /onError/);
});

test('full menu route is read-only intelligence with truthful states', () => {
  assert.match(menuRouteSource, /BusinessMenuScreen/);
  assert.match(menuScreenSource, /getBusinessMenu/);
  assert.match(menuScreenSource, /groupMenuItems/);
  for (const text of ['MENU', 'No menu observed yet', 'Try again']) assert.match(menuScreenSource, new RegExp(text));
  for (const forbidden of ['Add to cart', 'Checkout', 'Quantity']) assert.ok(!menuScreenSource.includes(forbidden));
});
