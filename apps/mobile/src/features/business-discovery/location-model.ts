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
  businessTypeSummary?: string | null;
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

export function isMarketplaceOrderingBoilerplate(value: string): boolean {
  const normalized = value.trim().replace(/\s+/g, ' ').toLowerCase();
  const bolt = normalized.startsWith('open ') && normalized.includes(' on bolt food') &&
    (normalized.includes('order delivery') || normalized.includes('delivery or pickup') || normalized.includes('order pickup'));
  const wolt = (normalized.startsWith('open ') || normalized.startsWith('order ')) &&
    (normalized.includes(' on wolt') || normalized.includes(' wolt delivery'));
  return bolt || wolt;
}

export function applyLocationToDraft<T extends Pick<DiscoveryDraft, 'primaryLocation' | 'country' | 'timezone' | 'currency'> & Partial<Pick<DiscoveryDraft, 'description'>>>(
  draft: T,
  location: BusinessLocationCandidate,
): T {
  const summary = location.businessTypeSummary?.trim();
  const currentDescription = typeof draft.description === 'string' ? draft.description : undefined;
  const shouldUseSummary = currentDescription !== undefined && Boolean(summary) &&
    (!currentDescription.trim() || isMarketplaceOrderingBoilerplate(currentDescription));

  return {
    ...draft,
    primaryLocation: location.formattedAddress,
    country: location.countryCode,
    timezone: location.timezone,
    currency: location.currency,
    ...(shouldUseSummary ? { description: summary } : {}),
  } as T;
}

export function displayMarket(location: BusinessLocationCandidate): string {
  return `${location.countryName} · ${location.timezone} · ${location.currency}`;
}