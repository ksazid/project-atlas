import type { BusinessProfile } from '@/api/atlas-client';

export type ProfileFieldKey = 'description' | 'address' | 'website' | 'phone' | 'email' | 'socialChannels' | 'businessHours' | 'language';
export type ProfileField = {
  key: ProfileFieldKey;
  label: string;
  hint: string;
  keyboard?: 'default' | 'email-address' | 'phone-pad' | 'url';
  multiline?: boolean;
};
export type ProfileSection = { title: string; fields: readonly ProfileField[] };
export type ProfileScreenState = 'loading' | 'missing' | 'error';

export const profileSections = [
  { title: 'ABOUT YOUR BUSINESS', fields: [
    { key: 'description', label: 'About the business', hint: 'What do customers come to you for?', multiline: true },
    { key: 'language', label: 'Preferred language', hint: 'English' }
  ] },
  { title: 'CONTACT AND PRESENCE', fields: [
    { key: 'website', label: 'Website', hint: 'https://yourbusiness.com', keyboard: 'url' },
    { key: 'phone', label: 'Business phone', hint: '+356 2000 0000', keyboard: 'phone-pad' },
    { key: 'email', label: 'Business email', hint: 'name@business.com', keyboard: 'email-address' },
    { key: 'socialChannels', label: 'Social channels', hint: 'Instagram, Facebook or LinkedIn' }
  ] },
  { title: 'LOCATION AND HOURS', fields: [
    { key: 'address', label: 'Business address', hint: 'Street, city and postcode' },
    { key: 'businessHours', label: 'Opening hours', hint: 'Mon–Fri 08:00–18:00', multiline: true }
  ] }
] as const satisfies readonly ProfileSection[];

export function createEmptyProfile(): BusinessProfile {
  return { description: '', address: '', website: '', phone: '', email: '', socialChannels: '', businessHours: '', language: 'English', source: 'owner', ownerConfirmed: true };
}

export function canSaveProfile(profile: BusinessProfile, saving: boolean): boolean {
  return !saving && (profile.source !== 'public' || profile.ownerConfirmed);
}

export function getProfileConfirmationState(ownerConfirmed: boolean) {
  return {
    ariaChecked: ownerConfirmed,
    accessibilityState: { checked: ownerConfirmed }
  } as const;
}

export function getProfileStatePresentation(state: ProfileScreenState) {
  if (state === 'loading') {
    return {
      title: 'Loading your profile',
      copy: 'Gathering the business details Atlas uses to tailor recommendations.',
    } as const;
  }

  if (state === 'missing') {
    return {
      title: 'No business selected',
      copy: 'Choose or create a business before you update its profile.',
      action: {
        label: 'Choose or create a business',
        accessibilityLabel: 'Choose or create a business',
        route: '/',
      },
    } as const;
  }

  return {
    title: 'We couldn’t load your profile',
    copy: 'Your profile is still safe. Check your connection and try again.',
  } as const;
}

export function getProfileSavePresentation(saving: boolean, saveEnabled: boolean) {
  return {
    accessibilityLabel: saving ? 'Saving business profile' : 'Save business profile',
    accessibilityState: { busy: saving, disabled: !saveEnabled },
    ariaBusy: saving,
    text: saving ? 'Saving…' : 'Save profile',
  } as const;
}

export function resolveProfileFailure(operation: 'refresh' | 'save') {
  return {
    state: 'ready',
    message: operation === 'refresh'
      ? 'Could not refresh profile. Your draft is still here.'
      : 'Could not save profile. Your draft is still here.'
  } as const;
}
