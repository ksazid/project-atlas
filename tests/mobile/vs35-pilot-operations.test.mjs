import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = process.cwd();
const read = (value) => fs.readFileSync(path.join(root, value), 'utf8');

const apiPath = 'apps/mobile/src/features/pilot-operations/pilot-operations-api.ts';
const modelPath = 'apps/mobile/src/features/pilot-operations/pilot-operations-model.ts';
const queuePath = 'apps/mobile/src/features/pilot-operations/PilotOperationsQueueScreen.tsx';
const reviewPath = 'apps/mobile/src/features/pilot-operations/PilotBusinessReviewScreen.tsx';
const operatorLayoutPath = 'apps/mobile/app/operator/_layout.tsx';
const operatorIndexPath = 'apps/mobile/app/operator/index.tsx';
const operatorBusinessPath = 'apps/mobile/app/operator/businesses/[businessId].tsx';
const ownerProfileApiPath = 'apps/mobile/src/features/profile/profile-api.ts';

test('VS-35 operator client and model expose bounded internal contracts', () => {
  assert.ok(fs.existsSync(path.join(root, apiPath)), 'pilot operations API client must exist');
  assert.ok(fs.existsSync(path.join(root, modelPath)), 'pilot operations presentation model must exist');

  const api = read(apiPath);
  const model = read(modelPath);
  assert.match(api, /operator-assisted/);
  assert.match(api, /\/api\/v1\/pilot-operations\/businesses/);
  assert.match(api, /opportunity-candidate/);
  assert.match(api, /withdraw/);
  assert.match(api, /PilotPrepareOpportunityInput/);
  assert.doesNotMatch(api, /PilotPrepareOpportunityInput\s*=\s*\{[^}]*title\s*:/is);
  assert.doesNotMatch(api, /PilotPrepareOpportunityInput\s*=\s*\{[^}]*whyItMatters\s*:/is);
  assert.match(model, /loading/);
  assert.match(model, /forbidden/);
  assert.match(model, /Unsafe guidance/);
  assert.match(model, /withdrawalReasonError/);
});

test('VS-35 operator routes are root Stack routes and never a fifth owner tab', () => {
  for (const file of [operatorLayoutPath, operatorIndexPath, operatorBusinessPath, queuePath, reviewPath]) {
    assert.ok(fs.existsSync(path.join(root, file)), `${file} must exist`);
  }

  const operatorLayout = read(operatorLayoutPath);
  const tabs = read('apps/mobile/app/(tabs)/_layout.tsx');
  assert.match(operatorLayout, /Stack/);
  assert.equal((tabs.match(/<NativeTabs\.Trigger/g) ?? []).length, 4);
  for (const label of ['Today', 'History', 'Goals', 'Profile']) assert.match(tabs, new RegExp(`>${label}<`));
  assert.doesNotMatch(tabs, /Pilot Operations|Operator/);
});

test('VS-35 operator review is review-first and withdrawal requires explicit reason and confirmation', () => {
  const queue = read(queuePath);
  const review = read(reviewPath);

  assert.match(queue, /Pilot Operations/);
  assert.match(queue, /loading/);
  assert.match(queue, /empty/);
  assert.match(queue, /forbidden/);
  assert.match(queue, /Try again/);
  assert.match(review, /Generation diagnostics/);
  assert.match(review, /Support note/);
  assert.match(review, /Profile assistance/);
  assert.match(review, /Prepare recommendation/);
  assert.match(review, /Withdraw recommendation/);
  assert.match(review, /Withdrawal reason/);
  assert.match(review, /Confirm withdrawal/);
  assert.match(review, /withdrawOpportunity/);
  assert.doesNotMatch(review, /raw prompt|model payload|provider payload/i);
});

test('VS-35 owner profile can truthfully reconfirm existing operator-assisted provenance', () => {
  assert.ok(fs.existsSync(path.join(root, ownerProfileApiPath)), 'owner profile API boundary must represent operator-assisted provenance');
  const profileApi = read(ownerProfileApiPath);
  const profileModel = read('apps/mobile/src/features/profile/profile-model.ts');
  const editBusiness = read('apps/mobile/app/edit-business.tsx');

  assert.match(profileApi, /'owner'\s*\|\s*'public'\s*\|\s*'operator-assisted'/);
  assert.match(profileModel, /source\s*===\s*'owner'\s*\|\|\s*profile\.ownerConfirmed/);
  assert.match(editBusiness, /Operator-assisted information/);
  assert.match(editBusiness, /form\.source\s*!==\s*'owner'/);
  assert.match(editBusiness, /review and confirm/i);
});
