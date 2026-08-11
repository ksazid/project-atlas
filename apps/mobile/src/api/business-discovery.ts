import { env } from '@/lib/env';
import type { BusinessDiscovery, CreateBusinessFromDiscoveryRequest } from '@/features/business-discovery/discovery-model';
import type { BusinessLocationCandidate } from '@/features/business-discovery/location-model';

export type { BusinessDiscovery } from '@/features/business-discovery/discovery-model';

export type BusinessLocationSearchResponse = {
  state: 'search' | 'preselected' | 'choose';
  candidates: BusinessLocationCandidate[];
  selected: BusinessLocationCandidate | null;
  canChange: boolean;
};

export class BusinessDiscoveryApiError extends Error {
  constructor(public readonly code: string, message: string) {
    super(message);
    this.name = 'BusinessDiscoveryApiError';
  }
}

type ApiProblem = {
  message?: string;
  detail?: string;
  title?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

async function problemFor(response: Response, fallback: string): Promise<never> {
  const problem = (await response.json().catch(() => null)) as ApiProblem | null;
  const fieldMessages = problem?.errors
    ? Object.values(problem.errors).flat().filter(message => typeof message === 'string' && message.trim())
    : [];
  throw new BusinessDiscoveryApiError(
    problem?.code ?? `http_${response.status}`,
    fieldMessages[0] ?? problem?.message ?? problem?.detail ?? problem?.title ?? fallback,
  );
}

function headers(accessToken: string) {
  return {
    Accept: 'application/json',
    'Content-Type': 'application/json',
    Authorization: `Bearer ${accessToken}`,
  };
}

export async function discoverBusiness(
  accessToken: string,
  url: string,
  additionalUrls?: string[],
): Promise<BusinessDiscovery> {
  const response = await fetch(`${env.apiUrl}/api/v1/business-discovery`, {
    method: 'POST',
    headers: headers(accessToken),
    body: JSON.stringify({ url, additionalUrls }),
  });

  if (!response.ok) return problemFor(response, 'Atlas could not analyse those business pages.');
  return (await response.json()) as BusinessDiscovery;
}

export async function searchBusinessLocations(
  accessToken: string,
  snapshotId: string | null,
  query?: string,
): Promise<BusinessLocationSearchResponse> {
  const path = snapshotId
    ? `/api/v1/business-discovery/${encodeURIComponent(snapshotId)}/locations/search`
    : '/api/v1/business-locations/search';
  const response = await fetch(`${env.apiUrl}${path}`, {
    method: 'POST',
    headers: headers(accessToken),
    body: JSON.stringify({ query: query?.trim() || null }),
  });

  if (!response.ok) return problemFor(response, 'Atlas could not search business locations.');
  return (await response.json()) as BusinessLocationSearchResponse;
}

export async function createBusinessFromDiscovery(
  accessToken: string,
  request: CreateBusinessFromDiscoveryRequest,
): Promise<{ id: string }> {
  const response = await fetch(`${env.apiUrl}/api/v1/businesses/from-discovery`, {
    method: 'POST',
    headers: headers(accessToken),
    body: JSON.stringify(request),
  });

  if (!response.ok) return problemFor(response, 'Atlas could not finish business setup.');
  return (await response.json()) as { id: string };
}
