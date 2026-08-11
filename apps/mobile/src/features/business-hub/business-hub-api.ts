import { env } from '@/lib/env';
import type { BusinessHub } from '@/api/atlas-client';

export type BusinessHubLoadResult =
  | { state: 'ready'; hub: BusinessHub }
  | { state: 'missing' };

async function problemMessage(response: Response): Promise<string> {
  const problem = (await response.json().catch(() => null)) as { title?: string; message?: string; code?: string } | null;
  return problem?.message ?? problem?.title ?? problem?.code ?? 'Atlas request failed.';
}

export async function loadBusinessHub(accessToken: string, businessId: string): Promise<BusinessHubLoadResult> {
  const response = await fetch(`${env.apiUrl}/api/v1/businesses/${businessId}/hub`, {
    headers: { Accept: 'application/json', Authorization: `Bearer ${accessToken}` },
  });
  if (response.status === 404) return { state: 'missing' };
  if (!response.ok) throw new Error(await problemMessage(response));
  return { state: 'ready', hub: (await response.json()) as BusinessHub };
}

export async function resetExpoDemoBusiness(accessToken: string): Promise<void> {
  const response = await fetch(`${env.apiUrl}/api/v1/dev/reset-business`, {
    method: 'POST',
    headers: { Accept: 'application/json', Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) throw new Error(await problemMessage(response));
}
