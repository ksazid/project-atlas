import { env } from '@/lib/env';

export type DiscoveredField = {
  value?: string | null;
  source: string;
  confidence: 'low' | 'medium' | 'high' | string;
  ownerConfirmed: boolean;
};

export type BusinessDiscovery = {
  provider: string;
  sourceUrl: string;
  name: DiscoveredField;
  category: DiscoveredField;
  subcategory?: DiscoveredField | null;
  primaryLocation?: DiscoveredField | null;
  description?: DiscoveredField | null;
};

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

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { message?: string; title?: string; code?: string } | null;
    throw new Error(problem?.message ?? problem?.title ?? problem?.code ?? 'Atlas could not analyse that business page.');
  }

  return (await response.json()) as BusinessDiscovery;
}
