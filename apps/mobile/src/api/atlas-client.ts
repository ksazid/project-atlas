import { env } from '@/lib/env';

export type CreateBusinessInput = {
  name: string;
  category: string;
  country: string;
  timezone: string;
  currency: string;
  primaryLocation: string;
  operatingStatus: string;
};

export type Business = CreateBusinessInput & { id: string };

async function request<T>(path: string, accessToken: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${env.apiUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { title?: string; message?: string; code?: string } | null;
    throw new Error(problem?.message ?? problem?.title ?? problem?.code ?? 'Atlas request failed.');
  }
  return (await response.json()) as T;
}

export function createBusiness(accessToken: string, input: CreateBusinessInput): Promise<Business> {
  return request<Business>('/api/v1/businesses', accessToken, { method: 'POST', body: JSON.stringify(input) });
}

export async function logout(accessToken: string): Promise<void> {
  await request('/api/v1/session/logout', accessToken, { method: 'POST' });
}
