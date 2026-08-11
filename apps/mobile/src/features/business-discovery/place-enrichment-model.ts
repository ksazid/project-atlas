import type { ConfirmedOperatingContext } from './discovery-model';

export type BusinessPlaceAttribution = {
  provider: string;
  providerUri: string | null;
};

export type BusinessPlaceEnrichmentResponse = {
  providerRef: string;
  operatingChannels: string[];
  reservable: boolean | null;
  servicePeriods: string[];
  pricePosition: string | null;
  openingHours: string[];
  attributions: BusinessPlaceAttribution[];
  attributionLabel: string;
};

export type AboutBusinessItem = {
  label: 'Service' | 'Reservations' | 'Service periods' | 'Price' | 'Hours';
  value: string;
};

export function buildAboutBusinessItems(enrichment: BusinessPlaceEnrichmentResponse): AboutBusinessItem[] {
  const items: AboutBusinessItem[] = [];
  const channels = values(enrichment.operatingChannels);
  const periods = values(enrichment.servicePeriods);
  const hours = values(enrichment.openingHours);
  const pricePosition = enrichment.pricePosition?.trim();

  if (channels.length > 0) items.push({ label: 'Service', value: channels.join(' · ') });
  if (enrichment.reservable === true) items.push({ label: 'Reservations', value: 'Reservations available' });
  if (periods.length > 0) items.push({ label: 'Service periods', value: periods.join(' · ') });
  if (pricePosition) items.push({ label: 'Price', value: `${pricePosition} price range` });
  if (hours.length > 0) items.push({ label: 'Hours', value: hours.join('\n') });

  return items.slice(0, 5);
}

export function buildConfirmedOperatingContext(
  enrichment: BusinessPlaceEnrichmentResponse,
): ConfirmedOperatingContext {
  return {
    providerRef: enrichment.providerRef.trim(),
    operatingChannels: values(enrichment.operatingChannels),
    reservable: enrichment.reservable === true ? true : null,
    servicePeriods: values(enrichment.servicePeriods),
    pricePosition: enrichment.pricePosition?.trim() || null,
  };
}

function values(input: string[]): string[] {
  const seen = new Set<string>();
  const result: string[] = [];
  for (const value of input) {
    const normalized = value.trim();
    const key = normalized.toLowerCase();
    if (!normalized || seen.has(key)) continue;
    seen.add(key);
    result.push(normalized);
  }
  return result;
}
