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
export type BusinessProfile = {
  description: string;
  address: string;
  website: string;
  socialChannels: string;
  businessChannels: string;
  hours: string;
  language: string;
  source: 'owner' | 'public';
  ownerConfirmed: boolean;
};
export type BusinessGoal = { id?: string; title: string; category: string; priority: number; isCustom: boolean };
export type BusinessContextEntry = { key: string; value: string; source: 'owner' | 'public'; ownerConfirmed: boolean };

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

export function getProfile(accessToken: string, businessId: string): Promise<BusinessProfile> {
  return request(`/api/v1/businesses/${businessId}/profile`, accessToken);
}
export function saveProfile(accessToken: string, businessId: string, input: BusinessProfile): Promise<BusinessProfile> {
  return request(`/api/v1/businesses/${businessId}/profile`, accessToken, { method: 'PUT', body: JSON.stringify(input) });
}
export function getGoals(accessToken: string, businessId: string): Promise<BusinessGoal[]> {
  return request(`/api/v1/businesses/${businessId}/goals`, accessToken);
}
export function saveGoals(accessToken: string, businessId: string, goals: BusinessGoal[]): Promise<BusinessGoal[]> {
  return request(`/api/v1/businesses/${businessId}/goals`, accessToken, { method: 'PUT', body: JSON.stringify(goals) });
}
export function getContext(accessToken: string, businessId: string): Promise<BusinessContextEntry[]> {
  return request(`/api/v1/businesses/${businessId}/context`, accessToken);
}
export function saveContext(accessToken: string, businessId: string, entries: BusinessContextEntry[]): Promise<BusinessContextEntry[]> {
  return request(`/api/v1/businesses/${businessId}/context`, accessToken, { method: 'PUT', body: JSON.stringify(entries) });
}

export async function logout(accessToken: string): Promise<void> {
  await request('/api/v1/session/logout', accessToken, { method: 'POST' });
}
