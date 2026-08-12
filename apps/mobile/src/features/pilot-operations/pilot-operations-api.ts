import { env } from '@/lib/env';

export type PilotBusinessProfile = {
  businessId: string;
  description?: string | null;
  address?: string | null;
  website?: string | null;
  phone?: string | null;
  email?: string | null;
  socialChannels?: string | null;
  businessHours?: string | null;
  language: string;
  source: 'owner' | 'public' | 'operator-assisted';
  ownerConfirmed: boolean;
  updatedAt: string;
};

export type PilotBusinessListItem = {
  businessId: string; name: string; category: string; primaryLocation: string; profileConfirmed: boolean; goalCount: number;
  currentOpportunityId?: string | null; currentOpportunityTitle?: string | null; currentOpportunityStatus?: string | null;
  latestGenerationOutcome?: string | null; latestGenerationCode?: string | null; latestGenerationAt?: string | null;
  unsafeFeedbackCount: number; usefulFeedbackCount: number; notUsefulFeedbackCount: number; latestOperatorActivityAt?: string | null;
};

export type PilotOpportunity = {
  id: string; businessId: string; title: string; whyItMatters: string; whyNow: string; expectedImpact: string; effort: string; confidence: string;
  evidenceSummary: string; status: string; createdAt: string; expiresAt: string; concurrencyVersion: number;
};

export type PilotIntelligenceRun = { id: string; businessId: string; outcome: string; code?: string | null; candidateCount: number; opportunityId?: string | null; occurredAt: string };
export type PilotFeedbackRecord = { id: string; kind: string; opportunityId?: string | null; contextKey?: string | null; usefulness?: string | null; message?: string | null; createdAt: string };
export type PilotOperationRecord = { id: string; action: string; targetType?: string | null; targetId?: string | null; reason?: string | null; occurredAt: string };
export type PilotBusinessDetail = {
  business: { id: string; name: string; category: string; primaryLocation: string; country: string; currency: string; timezone: string; operatingStatus: string };
  profile?: PilotBusinessProfile | null; goalCount: number; contextEntryCount: number; opportunities: PilotOpportunity[];
  generationHistory: PilotIntelligenceRun[]; feedback: PilotFeedbackRecord[]; operations: PilotOperationRecord[];
};

export type PilotOpportunityCandidate = { goalId: string; patternKey: string; title: string; confidence: string; effort: string; bundleFingerprint: string; knowledgePackKey: string; knowledgePackVersion: string; evidenceCount: number };
export type PilotPrepareOpportunityInput = { patternKey: string; bundleFingerprint: string; reason: string };
export type PilotWithdrawInput = { reason: string; version: number };
export type PilotProfileCorrectionInput = { description?: string | null; address?: string | null; website?: string | null; phone?: string | null; email?: string | null; socialChannels?: string | null; businessHours?: string | null; language: string; reason: string };
export type PilotPreparationResult = { state: string; code?: string | null; opportunityId?: string | null };

export class PilotOperationsAccessError extends Error {
  constructor(public readonly status: 401 | 403) { super(status === 403 ? 'operator_forbidden' : 'operator_unauthorized'); }
}

async function request<T>(token: string, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${env.apiUrl}${path}`, {
    ...init,
    headers: { Accept: 'application/json', Authorization: `Bearer ${token}`, ...(init?.body ? { 'Content-Type': 'application/json' } : {}), ...init?.headers },
  });
  if (response.status === 401 || response.status === 403) throw new PilotOperationsAccessError(response.status);
  if (!response.ok) throw new Error('pilot_operations_unavailable');
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

const businessPath = (businessId: string) => `/api/v1/pilot-operations/businesses/${encodeURIComponent(businessId)}`;

export const listPilotBusinesses = (token: string) => request<PilotBusinessListItem[]>(token, '/api/v1/pilot-operations/businesses');
export const getPilotBusiness = (token: string, businessId: string) => request<PilotBusinessDetail>(token, businessPath(businessId));
export const addPilotSupportNote = (token: string, businessId: string, note: string) => request<PilotOperationRecord>(token, `${businessPath(businessId)}/notes`, { method: 'POST', body: JSON.stringify({ note }) });
export const correctPilotProfile = (token: string, businessId: string, input: PilotProfileCorrectionInput) => request<PilotBusinessProfile>(token, `${businessPath(businessId)}/profile`, { method: 'PUT', body: JSON.stringify(input) });
export const previewPilotOpportunity = async (token: string, businessId: string): Promise<PilotOpportunityCandidate | null> => {
  const result = await request<{ state: string; candidate?: PilotOpportunityCandidate | null }>(token, `${businessPath(businessId)}/opportunity-candidate`);
  return result.state === 'ready' ? result.candidate ?? null : null;
};
export const preparePilotOpportunity = (token: string, businessId: string, input: PilotPrepareOpportunityInput) => request<PilotPreparationResult>(token, `${businessPath(businessId)}/opportunities`, { method: 'POST', body: JSON.stringify(input) });
export const withdrawOpportunity = (token: string, businessId: string, opportunityId: string, input: PilotWithdrawInput) => request<{ state: string; code?: string | null }>(token, `${businessPath(businessId)}/opportunities/${encodeURIComponent(opportunityId)}/withdraw`, { method: 'POST', body: JSON.stringify(input) });
