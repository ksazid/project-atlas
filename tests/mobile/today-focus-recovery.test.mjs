import assert from 'node:assert/strict';
import test from 'node:test';

import { todayFocusRecoveryAction } from '../../apps/mobile/src/features/today-focus/today-focus-recovery.ts';

test('missing goal readiness sends the owner to Goals with a precise action', () => {
  assert.deepEqual(todayFocusRecoveryAction('opportunity_goal_missing'), {
    label: 'Choose your first goal',
    route: '/goals',
  });
});

test('other insufficient-context states keep the existing safe recovery path', () => {
  assert.deepEqual(todayFocusRecoveryAction('opportunity_profile_missing'), {
    label: 'Review business context',
    route: '/profile',
  });
  assert.deepEqual(todayFocusRecoveryAction(undefined), {
    label: 'Review business context',
    route: '/profile',
  });
});
