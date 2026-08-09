import { env } from '@/lib/env';
import type { BusinessDiscovery, CreateBusinessFromDiscoveryRequest } from '@/features/business-discovery/discovery-model';

export type { BusinessDiscovery } from '@/features/business-discovery/discovery-model';

export class BusinessDiscoveryApiError extends Error {
  constructor(public readonly code: string, message: string) {
    super(message);
    this.name = 'BusinessDiscoveryApiError';
  }
}

async function problemFor(response: Response, fallback: string): Promise<never> {
  const problem = (await response.json().catch(() => null)) as { message?: string; title?: string; code?: string } | null;
  throw new BusinessDiscoveryApiError(
    problem?.code ?? `http_${response.status}`,
    problem?.message ?? problem?.title ?? fallback,
  );
}

export async function discoverBusiness(accessToken: string, url: string): Promise<BusinessDiscovery> {
  const response = await fetch(`${env.apiUrl}/api/v1/business-discovery`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ url }),
  });

  if (!response.ok) return problemFor(response, 'Atlas could not analyse that business page.');
  return (await response.json()) as BusinessDiscovery;
}

export async function createBusinessFromDiscovery(
  accessToken: string,
  request: CreateBusinessFromDiscoveryRequest,
): Promise<{ id: string }> {
  const response = await fetch(`${env.apiUrl}/api/v1/businesses/from-discovery`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) return problemFor(response, 'Atlas could not finish business setup.');
  return (await response.json()) as { id: string };
}
