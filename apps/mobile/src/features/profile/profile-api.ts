import { env } from '@/lib/env';

export type OwnerProfileSource = 'owner' | 'public' | 'operator-assisted';
export type OwnerBusinessProfile = {
  description: string; address: string; website: string; phone: string; email: string; socialChannels: string;
  businessHours: string; language: string; source: OwnerProfileSource; ownerConfirmed: boolean;
};

type ProfileResponse = {
  description?: string | null; address?: string | null; website?: string | null; phone?: string | null; email?: string | null;
  socialChannels?: string | null; businessHours?: string | null; language: string; source: OwnerProfileSource; ownerConfirmed: boolean;
};

async function request<T>(accessToken: string, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${env.apiUrl}${path}`, {
    ...init,
    headers: { Accept: 'application/json', 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}`, ...init?.headers },
  });
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { title?: string; message?: string; code?: string } | null;
    throw new Error(problem?.message ?? problem?.title ?? problem?.code ?? 'Atlas request failed.');
  }
  return (await response.json()) as T;
}

function normalize(profile: ProfileResponse): OwnerBusinessProfile {
  return {
    description: profile.description ?? '', address: profile.address ?? '', website: profile.website ?? '', phone: profile.phone ?? '',
    email: profile.email ?? '', socialChannels: profile.socialChannels ?? '', businessHours: profile.businessHours ?? '',
    language: profile.language, source: profile.source, ownerConfirmed: profile.ownerConfirmed,
  };
}

export async function getOwnerProfile(accessToken: string, businessId: string): Promise<OwnerBusinessProfile | null> {
  const profile = await request<ProfileResponse | null>(accessToken, `/api/v1/businesses/${businessId}/profile`);
  return profile ? normalize(profile) : null;
}

export async function saveOwnerProfile(accessToken: string, businessId: string, input: OwnerBusinessProfile): Promise<OwnerBusinessProfile> {
  const profile = await request<ProfileResponse>(accessToken, `/api/v1/businesses/${businessId}/profile`, { method: 'PUT', body: JSON.stringify(input) });
  return normalize(profile);
}
