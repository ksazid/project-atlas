import assert from 'node:assert/strict';
import test from 'node:test';
import {
  getAtlasScreenMetrics,
  getAtlasTabBarMetrics,
} from '../../apps/mobile/src/theme/native-layout.ts';

test('comfortable modern iPhone geometry follows real insets', () => {
  assert.deepEqual(getAtlasTabBarMetrics({ width: 440, bottomInset: 34, fontScale: 1 }), {
    mode: 'floating',
    horizontalInset: 16,
    bottomOffset: 26,
    frameHeight: 58,
    paddingBottom: 0,
    borderRadius: 24,
    obstructionHeight: 84,
  });
  assert.deepEqual(getAtlasScreenMetrics({ width: 440, topInset: 59, bottomInset: 34, fontScale: 1, hasTabBar: true }), {
    paddingTop: 71,
    paddingBottom: 100,
    paddingHorizontal: 28,
  });
});

test('compact iPhone geometry uses edge chrome and compact horizontal rhythm', () => {
  assert.deepEqual(getAtlasTabBarMetrics({ width: 320, bottomInset: 34, fontScale: 1 }), {
    mode: 'edge',
    horizontalInset: 0,
    bottomOffset: 0,
    frameHeight: 92,
    paddingBottom: 34,
    borderRadius: 0,
    obstructionHeight: 92,
  });
  assert.deepEqual(getAtlasScreenMetrics({ width: 320, topInset: 47, bottomInset: 34, fontScale: 1, hasTabBar: true }), {
    paddingTop: 55,
    paddingBottom: 108,
    paddingHorizontal: 20,
  });
});

test('Android uses the same semantic inset contract', () => {
  assert.deepEqual(getAtlasTabBarMetrics({ width: 412, bottomInset: 24, fontScale: 1 }), {
    mode: 'floating',
    horizontalInset: 12,
    bottomOffset: 16,
    frameHeight: 58,
    paddingBottom: 0,
    borderRadius: 24,
    obstructionHeight: 74,
  });
  assert.deepEqual(getAtlasScreenMetrics({ width: 412, topInset: 24, bottomInset: 24, fontScale: 1, hasTabBar: true }), {
    paddingTop: 36,
    paddingBottom: 90,
    paddingHorizontal: 24,
  });
});

test('large text grows the tab row instead of clipping labels', () => {
  assert.equal(getAtlasTabBarMetrics({ width: 440, bottomInset: 34, fontScale: 1.4 }).frameHeight, 64);
});
