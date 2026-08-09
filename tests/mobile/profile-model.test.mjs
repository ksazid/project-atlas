import assert from 'node:assert/strict';
import test from 'node:test';
import * as profileModel from '../../apps/mobile/src/features/profile/profile-model.ts';
import * as sessionRouting from '../../apps/mobile/src/auth/session-routing.ts';

const { canSaveProfile, createEmptyProfile, profileSections } = profileModel;

test('profile field groups cover every persisted FR-03 field once', () => {
  assert.deepEqual(
    profileSections.flatMap(section => section.fields.map(field => field.key)),
    ['description', 'language', 'website', 'phone', 'email', 'socialChannels', 'address', 'businessHours']
  );
});

test('new profile defaults are owner-provided and safe to save', () => {
  const profile = createEmptyProfile();
  assert.equal(profile.language, 'English');
  assert.equal(profile.source, 'owner');
  assert.equal(profile.ownerConfirmed, true);
  assert.equal(canSaveProfile(profile, false), true);
});

test('public profile requires confirmation and no save may overlap', () => {
  const profile = { ...createEmptyProfile(), source: 'public', ownerConfirmed: false };
  assert.equal(canSaveProfile(profile, false), false);
  assert.equal(canSaveProfile({ ...profile, ownerConfirmed: true }, false), true);
  assert.equal(canSaveProfile({ ...profile, ownerConfirmed: true }, true), false);
});

test('refresh failure keeps the ready draft visible with recoverable feedback', () => {
  assert.deepEqual(profileModel.resolveProfileFailure?.('refresh'), {
    state: 'ready',
    message: 'Could not refresh profile. Your draft is still here.'
  });
});

test('save failure keeps the draft visible without exposing provider errors', () => {
  assert.deepEqual(profileModel.resolveProfileFailure?.('save'), {
    state: 'ready',
    message: 'Could not save profile. Your draft is still here.'
  });
});

test('profile confirmation mirrors checked state for web and native accessibility', () => {
  assert.deepEqual(profileModel.getProfileConfirmationState?.(false), {
    ariaChecked: false,
    accessibilityState: { checked: false }
  });
  assert.deepEqual(profileModel.getProfileConfirmationState?.(true), {
    ariaChecked: true,
    accessibilityState: { checked: true }
  });
});

test('missing profile state gives the user an accessible route through the session guard', () => {
  assert.deepEqual(profileModel.getProfileStatePresentation?.('missing'), {
    title: 'No business selected',
    copy: 'Choose or create a business before you update its profile.',
    action: {
      label: 'Choose or create a business',
      accessibilityLabel: 'Choose or create a business',
      route: '/',
    },
  });
});

test('saving profile presentation exposes busy state and a clear action label', () => {
  assert.deepEqual(profileModel.getProfileSavePresentation?.(true, false), {
    accessibilityLabel: 'Saving business profile',
    accessibilityState: { busy: true, disabled: true },
    ariaBusy: true,
    text: 'Saving…',
  });
});

test('session routing sends a selected Profile business action to the existing guard destination', () => {
  assert.equal(sessionRouting.getSessionDestination?.(null), '/welcome');
  assert.equal(sessionRouting.getSessionDestination?.({ accessToken: 'token' }), '/create-business');
  assert.equal(sessionRouting.getSessionDestination?.({ accessToken: 'token', businessId: 'business-1' }), '/(tabs)');
});
