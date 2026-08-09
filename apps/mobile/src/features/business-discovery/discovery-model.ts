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
  businessHours: string;
  language: string;
};

export type CreateBusinessFromDiscoveryRequest = DiscoveryDraft & {
  ownerConfirmed: true;
};

const requiredFields: (keyof Pick<DiscoveryDraft, 'name' | 'category' | 'country' | 'timezone' | 'currency' | 'primaryLocation'>)[] = [
  'name',
  'category',
  'country',
  'timezone',
  'currency',
  'primaryLocation',
];

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
    businessHours: value(discovery, 'openingHours'),
    language: value(discovery, 'language') || 'English',
  };
}

export function getMissingRequiredFields(draft: DiscoveryDraft): string[] {
  return requiredFields.filter(key => !draft[key].trim());
}

export function canConfirmDiscovery(draft: DiscoveryDraft): boolean {
  return getMissingRequiredFields(draft).length === 0;
}

export function buildCreateBusinessFromDiscoveryRequest(draft: DiscoveryDraft): CreateBusinessFromDiscoveryRequest {
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
    businessHours: draft.businessHours.trim(),
    language: draft.language.trim(),
    ownerConfirmed: true,
  };
}