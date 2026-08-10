import type { DiscoveryDraft } from './discovery-model.ts';

export type BusinessLocationCandidate = {
  providerRef: string;
  name: string;
  formattedAddress: string;
  latitude: number;
  longitude: number;
  countryCode: string;
  countryName: string;
  timezone: string;
  currency: string;
  provider: string;
};

export type LocationChoiceState =
  | { kind: 'search'; candidates: BusinessLocationCandidate[]; selected: null; canChange: true }
  | { kind: 'preselected'; candidates: BusinessLocationCandidate[]; selected: BusinessLocationCandidate; canChange: true }
  | { kind: 'choose'; candidates: BusinessLocationCandidate[]; selected: null; canChange: true };

export function toLocationChoiceState(candidates: BusinessLocationCandidate[]): LocationChoiceState {
  if (candidates.length === 0) return { kind: 'search', candidates: [], selected: null, canChange: true };
  if (candidates.length === 1) return { kind: 'preselected', candidates: [...candidates], selected: candidates[0], canChange: true };
  return { kind: 'choose', candidates: [...candidates], selected: null, canChange: true };
}

export function applyLocationToDraft<T extends Pick<DiscoveryDraft, 'primaryLocation' | 'country' | 'timezone' | 'currency'>>(
  draft: T,
  location: BusinessLocationCandidate,
): T {
  return {
    ...draft,
    primaryLocation: location.formattedAddress,
    country: location.countryCode,
    timezone: location.timezone,
    currency: location.currency,
  };
}

export function displayMarket(location: BusinessLocationCandidate): string {
  return `${location.countryName} · ${location.timezone} · ${location.currency}`;
}
