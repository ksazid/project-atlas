import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { resolveMaterialMode, resolveMotionMode } from '../../apps/mobile/src/lib/accessibility-policy.ts';

const rootSource = readFileSync(new URL('../../apps/mobile/app/_layout.tsx', import.meta.url), 'utf8');
const providerSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasAccessibilityProvider.tsx', import.meta.url), 'utf8');
const materialSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasMaterialSurface.tsx', import.meta.url), 'utf8');

test('glass is allowed only on supported iOS when transparency is allowed', () => {
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: true, reduceTransparency: false }), 'glass');
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: true, reduceTransparency: true }), 'solid');
  assert.equal(resolveMaterialMode({ platform: 'android', glassAvailable: true, reduceTransparency: false }), 'solid');
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: false, reduceTransparency: false }), 'solid');
});

test('motion preference is independent from transparency', () => {
  assert.equal(resolveMotionMode(false), 'full');
  assert.equal(resolveMotionMode(true), 'reduced');
});

test('root owns one accessibility preference provider', () => {
  assert.match(rootSource, /AtlasAccessibilityProvider/);
  assert.match(providerSource, /isReduceMotionEnabled/);
  assert.match(providerSource, /reduceMotionChanged/);
  assert.match(providerSource, /isReduceTransparencyEnabled/);
  assert.match(providerSource, /reduceTransparencyChanged/);
});

test('material surface guards Liquid Glass and includes a solid fallback', () => {
  assert.match(materialSource, /isLiquidGlassAvailable/);
  assert.match(materialSource, /isGlassEffectAPIAvailable/);
  assert.match(materialSource, /resolveMaterialMode/);
  assert.match(materialSource, /GlassView/);
  assert.match(materialSource, /tokens\.color\.surface/);
  assert.doesNotMatch(materialSource, /opacity\s*:\s*0\.[0-9]+/);
});
