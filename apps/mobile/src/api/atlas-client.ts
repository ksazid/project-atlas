import { env } from '@/lib/env';

export type CreateBusinessInput = {
  name: string; category: string; country: string; timezone: string; currency: string; primaryLocation: string; operatingStatus: string;
};
export type Business = CreateBusinessInput & { id: string };
export type BusinessProfile = {
  description: string; address: string; website: string; phone: string; email: string; socialChannels: string;
  businessHours: string; language: string; source: 'owner' | 'public'; ownerConfirmed: boolean;
};
export type BusinessGoal = { id?: string; title: string; type: string; priority: number; isCustom: boolean };
export type BusinessContextEntry = { key: string; value: string; source: 'owner' | 'public'; ownerConfirmed: boolean };
export type KnowledgeSection = { id: string; stableKey: string; category: string; title: string; content: string; metadataJson?: string | null; order: number; locale: string };
export type KnowledgePack = { key: string; name: string; description: string; version: string; status: string; locale: string; sections: KnowledgeSection[]; assignedAt: string };
export type TodayFocusOpportunity = {
  id: string; title: string; whyItMatters: string; whyNow: string; expectedImpact: string; effort: string;
  confidence: string; evidenceSummary: string; status: string; expiresAt: string; knowledgePackKey: string;
  knowledgePackVersion: string; version: number;
};
export type TodayFocus = { state: 'ready'; opportunity: TodayFocusOpportunity } | { state: 'insufficient-context'; message: string };
export type OpportunityDecision = 'apply' | 'skip' | 'not-relevant';
export type OpportunityEvidenceItem = { category: string; label: string; value: string; source: string };
export type OpportunityDetail = {
  id: string; title: string; status: string; goalAlignment: string; goalTitle?: string | null; reason: string; whyNow: string;
  confidence: string; expectedImpact: string; effort: string; evidence: OpportunityEvidenceItem[]; assumptions: string[];
  limitations: string[]; sourceCategories: string[]; actionSummary: string; executionKitAvailable: boolean;
  createdAt: string; expiresAt: string; isExpired: boolean; knowledgePackKey: string; knowledgePackVersion: string; version: number;
};
export type ExecutionAsset = {
  id: string; type: string; title: string; content: string; isEditable: boolean; isUsed: boolean;
  copyCount: number; usefulnessRating?: number | null; version: number;
};
export type ExecutionKit = {
  id: string; opportunityId: string; knowledgePackKey: string; knowledgePackVersion: string;
  versionNumber: number; status: string; assets: ExecutionAsset[]; version: number;
};

async function request<T>(path: string, accessToken: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${env.apiUrl}${path}`, { ...init, headers: { Accept: 'application/json', 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}`, ...init?.headers } });
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { title?: string; message?: string; code?: string } | null;
    throw new Error(problem?.message ?? problem?.title ?? problem?.code ?? 'Atlas request failed.');
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export function createBusiness(accessToken: string, input: CreateBusinessInput): Promise<Business> {
  return request('/api/v1/businesses', accessToken, { method: 'POST', body: JSON.stringify(input) });
}
export async function getProfile(accessToken: string, businessId: string): Promise<BusinessProfile | null> {
  return request(`/api/v1/businesses/${businessId}/profile`, accessToken);
}
export function saveProfile(accessToken: string, businessId: string, input: BusinessProfile): Promise<BusinessProfile> {
  return request(`/api/v1/businesses/${businessId}/profile`, accessToken, { method: 'PUT', body: JSON.stringify(input) });
}
export function getGoals(accessToken: string, businessId: string): Promise<BusinessGoal[]> {
  return request(`/api/v1/businesses/${businessId}/goals`, accessToken);
}
export async function saveGoals(accessToken: string, businessId: string, goals: BusinessGoal[]): Promise<BusinessGoal[]> {
  await request<void>(`/api/v1/businesses/${businessId}/goals`, accessToken, { method: 'PUT', body: JSON.stringify({ goals }) });
  return getGoals(accessToken, businessId);
}
export function getContext(accessToken: string, businessId: string): Promise<BusinessContextEntry[]> {
  return request(`/api/v1/businesses/${businessId}/context`, accessToken);
}
export async function saveContext(accessToken: string, businessId: string, entries: BusinessContextEntry[]): Promise<BusinessContextEntry[]> {
  for (const entry of entries) {
    await request(`/api/v1/businesses/${businessId}/context/${encodeURIComponent(entry.key)}`, accessToken, { method: 'PUT', body: JSON.stringify(entry) });
  }
  return getContext(accessToken, businessId);
}
export function getKnowledgePack(accessToken: string, businessId: string): Promise<KnowledgePack> {
  return request(`/api/v1/businesses/${businessId}/knowledge-pack`, accessToken);
}
export function getTodayFocus(accessToken: string, businessId: string): Promise<TodayFocus> {
  return request(`/api/v1/businesses/${businessId}/today-focus`, accessToken);
}
export function getOpportunityDetail(accessToken: string, businessId: string, opportunityId: string): Promise<OpportunityDetail> {
  return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}`, accessToken);
}
export function decideOpportunity(accessToken: string, businessId: string, opportunity: TodayFocusOpportunity, decision: OpportunityDecision, reason?: string): Promise<TodayFocusOpportunity> {
  return request(`/api/v1/businesses/${businessId}/opportunities/${opportunity.id}/decision`, accessToken, {
    method: 'POST', body: JSON.stringify({ decision, reason, version: opportunity.version }),
  });
}
export function getExecutionKit(accessToken: string, businessId: string, opportunityId: string): Promise<ExecutionKit> {
  return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}/execution-kit`, accessToken);
}
export function updateExecutionAsset(accessToken: string, businessId: string, kitId: string, asset: ExecutionAsset, content: string, isUsed: boolean, usefulnessRating?: number | null): Promise<ExecutionKit> {
  return request(`/api/v1/businesses/${businessId}/execution-kits/${kitId}/assets/${asset.id}`, accessToken, {
    method: 'PUT', body: JSON.stringify({ content, isUsed, usefulnessRating, version: asset.version }),
  });
}
export function trackExecutionAssetCopy(accessToken: string, businessId: string, kitId: string, asset: ExecutionAsset): Promise<ExecutionKit> {
  return request(`/api/v1/businesses/${businessId}/execution-kits/${kitId}/assets/${asset.id}/copied`, accessToken, {
    method: 'POST', body: JSON.stringify({ version: asset.version }),
  });
}
export async function logout(accessToken: string): Promise<void> { await request('/api/v1/session/logout', accessToken, { method: 'POST' }); }
