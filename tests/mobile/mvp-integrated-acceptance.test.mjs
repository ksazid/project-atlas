import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';

const read = path => fs.readFileSync(path, 'utf8');
const readJson = path => JSON.parse(read(path));

test('certified Atlas main baseline is the source for integrated MVP acceptance', () => {
  const slice = readJson('delivery/current-slice.json');

  assert.equal(slice.sliceId, 'VS-15');
  assert.equal(slice.lifecycle, 'certified');
  assert.equal(slice.certification.status, 'passed');
  assert.equal(slice.progress.implementation, 100);
  assert.equal(slice.progress.testing, 100);
  assert.equal(slice.progress.certification, 100);
  assert.equal(slice.release.status, 'not-authorized');

  const productionApproval = slice.approvals.find(item => item.type === 'production-enable');
  assert.ok(productionApproval);
  assert.notEqual(productionApproval.status, 'approved');
});

test('integrated owner journey retains the implemented Atlas routes', () => {
  for (const path of [
    'apps/mobile/app/welcome.tsx',
    'apps/mobile/app/sign-in.tsx',
    'apps/mobile/app/create-business.tsx',
    'apps/mobile/app/(tabs)/index.tsx',
    'apps/mobile/app/(tabs)/profile.tsx',
    'apps/mobile/app/(tabs)/goals.tsx',
    'apps/mobile/app/(tabs)/context.tsx',
    'apps/mobile/app/(tabs)/settings.tsx',
    'apps/mobile/app/history.tsx',
    'apps/mobile/app/weekly-review.tsx',
    'apps/mobile/app/notifications.tsx',
    'apps/mobile/app/opportunities/[id].tsx'
  ]) {
    assert.equal(fs.existsSync(path), true, `${path} must remain routable`);
  }
});

test('Profile Goals and Context share the approved Atlas visual primitives', () => {
  for (const path of [
    'apps/mobile/app/(tabs)/profile.tsx',
    'apps/mobile/app/(tabs)/goals.tsx',
    'apps/mobile/app/(tabs)/context.tsx'
  ]) {
    const source = read(path);
    assert.match(source, /BrandMark/);
    assert.match(source, /tokens/);
    assert.doesNotMatch(source, /upload\.wikimedia\.org|Starbucks_Corporation_Logo|starbucks/i);
  }

  const brandMark = read('apps/mobile/src/components/BrandMark.tsx');
  assert.match(brandMark, /PROTOTYPE_MARK_URI/);
});

test('integrated acceptance keeps focused model and authentic runtime evidence in the gate set', () => {
  for (const path of [
    'tests/mobile/profile-model.test.mjs',
    'tests/mobile/goals-model.test.mjs',
    'tests/mobile/context-model.test.mjs',
    'tests/mobile/context-runtime.test.mjs',
    'docs/evidence/VS-13-RUNTIME-2026-08-09.md',
    'docs/evidence/VS-14-RUNTIME-2026-08-09.md',
    'docs/evidence/VS-15-RUNTIME-2026-08-09.md'
  ]) {
    assert.equal(fs.existsSync(path), true, `${path} must remain part of the acceptance baseline`);
  }
});

test('MVP acceptance does not silently enable release or production', () => {
  const acceptance = read('docs/mvp-acceptance.md');
  assert.match(acceptance, /no production deployment/i);
  assert.match(acceptance, /native/i);
  assert.match(acceptance, /VS-15/i);
});
