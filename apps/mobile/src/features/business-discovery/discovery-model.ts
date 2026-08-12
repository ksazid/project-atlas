export type DiscoveryFact = {
  key: string;
  value: string;
  source: string;
  sourceUrl: string;
  observedAt: string;
  confidence: 'low' | 'medium' | 'high' | string;
  evidenceClass: string;
  ownerConfirmed: boolean;
};

export type BusinessDiscovery = {
  snapshotId: string;
  provider: string;
  sourceUrl: string;
  observedAt: string;
  facts: DiscoveryFact[];
};

export type DiscoveryDraft = {
  snapshotId: string;
  name: string;
  category: string;
  subcategory: string;
  country: string;
  timezone: string;
  currency: string;
  primaryLocation: string;
  operatingStatus: string;
  description: string;
  website: string;
  phone: string;
  email: string;
  socialChannels: string;
  businessHours: string;
  language: string;
};

export type ConfirmedOperatingContext = {
  providerRef: string;
  operatingChannels: string[];
  reservable: boolean | null;
  servicePeriods: string[];
  pricePosition: string | null;
  openingHours: string[];
};

export type CreateBusinessFromDiscoveryRequest = DiscoveryDraft & {
  ownerConfirmed: true;
  confirmedOperatingContext?: ConfirmedOperatingContext;
};

export function getDiscoveryFact(discovery: BusinessDiscovery, key: string): DiscoveryFact | undefined {
  return discovery.facts.find(fact => fact.key.toLowerCase() === key.toLowerCase());
}

function value(discovery: BusinessDiscovery, key: string): string {
  return getDiscoveryFact(discovery, key)?.value?.trim() ?? '';
}

export function createDiscoveryDraft(discovery: BusinessDiscovery): DiscoveryDraft {
  return {
    snapshotId: discovery.snapshotId,
    name: value(discovery, 'name'),
    category: value(discovery, 'category'),
    subcategory: value(discovery, 'subcategory'),
    country: value(discovery, 'country'),
    timezone: value(discovery, 'timezone'),
    currency: value(discovery, 'currency'),
    primaryLocation: value(discovery, 'primaryLocation'),
    operatingStatus: value(discovery, 'operatingStatus') || 'Open',
    description: value(discovery, 'description'),
    website: value(discovery, 'website'),
    phone: value(discovery, 'phone'),
    email: value(discovery, 'email'),
    socialChannels: value(discovery, 'socialChannels'),
    businessHours: value(discovery, 'openingHours'),
    language: value(discovery, 'language') || 'English',
  };
}

export function getMissingRequiredFields(draft: DiscoveryDraft): string[] {
  const missing: string[] = [];
  if (!draft.name.trim()) missing.push('name');
  if (!draft.category.trim()) missing.push('category');
  if (![draft.primaryLocation, draft.country, draft.timezone, draft.currency].every(value => value.trim())) missing.push('location');
  return missing;
}

export function canConfirmDiscovery(draft: DiscoveryDraft): boolean {
  return getMissingRequiredFields(draft).length === 0;
}

export function buildCreateBusinessFromDiscoveryRequest(
  draft: DiscoveryDraft,
  confirmedOperatingContext?: ConfirmedOperatingContext,
): CreateBusinessFromDiscoveryRequest {
  return {
    snapshotId: draft.snapshotId,
    name: draft.name.trim(),
    category: draft.category.trim(),
    subcategory: draft.subcategory.trim(),
    country: draft.country.trim(),
    timezone: draft.timezone.trim(),
    currency: draft.currency.trim(),
    primaryLocation: draft.primaryLocation.trim(),
    operatingStatus: draft.operatingStatus.trim(),
    description: draft.description.trim(),
    website: draft.website.trim(),
    phone: draft.phone.trim(),
    email: draft.email.trim(),
    socialChannels: draft.socialChannels.trim(),
    businessHours: draft.businessHours.trim(),
    language: draft.language.trim(),
    ownerConfirmed: true,
    ...(confirmedOperatingContext ? { confirmedOperatingContext } : {}),
  };
}