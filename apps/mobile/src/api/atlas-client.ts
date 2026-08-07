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
export type ActionStatus = 'applied' | 'completed' | 'skipped' | 'not-relevant' | 'rejected';
export type ActionReasonCode = 'timing-not-right' | 'already-done' | 'insufficient-capacity' | 'not-a-priority' | 'context-incorrect' | 'recommendation-not-relevant' | 'unsafe-or-inappropriate' | 'other';
export type ActionDecisionItem = { id: string; status: ActionStatus; reasonCode?: ActionReasonCode | null; ownerNote?: string | null; decidedAt: string };
export type ActionDecisionState = { opportunityId: string; currentStatus: string; version: number; decisions: ActionDecisionItem[] };
export type ExecutionAsset = { id: string; type: string; title: string; content: string; isEditable: boolean; isUsed: boolean; copyCount: number; usefulnessRating?: number | null; version: number };
export type ExecutionKit = { id: string; opportunityId: string; knowledgePackKey: string; knowledgePackVersion: string; versionNumber: number; status: string; assets: ExecutionAsset[]; version: number };
export type OutcomeEvidenceClass = 'measured' | 'owner-reported' | 'estimated' | 'unknown';
export type Outcome = { id: string; opportunityId: string; usefulnessRating: number; resultSummary: string; timeSpentMinutes: number; ownerNotes?: string | null; measureName?: string | null; measureValue?: number | null; measureUnit?: string | null; evidenceClass: OutcomeEvidenceClass; followUpAt?: string | null; capturedAt: string; updatedAt: string; knowledgePackKey: string; knowledgePackVersion: string; version: number };
export type OutcomeInput = { usefulnessRating: number; resultSummary: string; timeSpentMinutes: number; ownerNotes?: string; measureName?: string; measureValue?: number; measureUnit?: string; evidenceClass: OutcomeEvidenceClass; followUpAt?: string; version?: number };
export type BusinessMemoryItem = { id: string; stableKey: string; category: string; sourceType: string; sourceId?: string | null; value: string; isDeletable: boolean; updatedAt: string; version: number };
export type HistoryExecutionKitSummary = { id: string; status: string; assetCount: number; usedAssetCount: number; usefulnessRating?: number | null };
export type HistoryOutcomeSummary = { id: string; resultSummary: string; evidenceClass: OutcomeEvidenceClass; usefulnessRating: number; updatedAt: string };
export type HistoryItem = { opportunityId: string; title: string; status: string; goalId?: string | null; goalTitle?: string | null; categories: string[]; decisionReasonCode?: string | null; decisionOwnerNote?: string | null; createdAt: string; expiresAt: string; lastActionAt?: string | null; executionKit?: HistoryExecutionKitSummary | null; outcome?: HistoryOutcomeSummary | null; learningSummary: string; knowledgePackKey: string; knowledgePackVersion: string };
export type HistoryResponse = { items: HistoryItem[]; count: number };
export type HistoryFilters = { status?: string; category?: string; goalId?: string; from?: string; to?: string; limit?: number };
export type WeeklyReviewCounts = { opportunities: number; applied: number; completed: number; skipped: number; notRelevant: number; rejected: number; outcomesRecorded: number; outcomesMissing: number; executionAssetsUsed: number };
export type WeeklyReviewOutcomeItem = { opportunityId: string; opportunityTitle: string; status: string; goalTitle?: string | null; resultSummary: string; evidenceClass: OutcomeEvidenceClass; usefulnessRating: number; knowledgePackKey: string; knowledgePackVersion: string; recordedAt: string };
export type WeeklyReviewOpenItem = { opportunityId: string; opportunityTitle: string; status: string; goalTitle?: string | null; knowledgePackKey: string; knowledgePackVersion: string; lastActivityAt: string };
export type WeeklyReview = { periodStart: string; periodEnd: string; counts: WeeklyReviewCounts; outcomes: WeeklyReviewOutcomeItem[]; openItems: WeeklyReviewOpenItem[]; highlights: string[]; evidenceNote: string };
export type NotificationPreference = { todayFocusEnabled: boolean; outcomeFollowUpEnabled: boolean; weeklyReviewEnabled: boolean; version: number };
export type NotificationItem = { id: string; category: 'today-focus' | 'outcome-follow-up' | 'weekly-review'; title: string; body: string; deepLink?: string | null; createdAt: string; readAt?: string | null; version: number };
export type NotificationCenter = { items: NotificationItem[]; unreadCount: number; preferences: NotificationPreference };

async function request<T>(path: string, accessToken: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${env.apiUrl}${path}`, { ...init, headers: { Accept: 'application/json', 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}`, ...init?.headers } });
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { title?: string; message?: string; code?: string } | null;
    throw new Error(problem?.message ?? problem?.title ?? problem?.code ?? 'Atlas request failed.');
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export function createBusiness(accessToken: string, input: CreateBusinessInput): Promise<Business> { return request('/api/v1/businesses', accessToken, { method: 'POST', body: JSON.stringify(input) }); }
export async function getProfile(accessToken: string, businessId: string): Promise<BusinessProfile | null> { return request(`/api/v1/businesses/${businessId}/profile`, accessToken); }
export function saveProfile(accessToken: string, businessId: string, input: BusinessProfile): Promise<BusinessProfile> { return request(`/api/v1/businesses/${businessId}/profile`, accessToken, { method: 'PUT', body: JSON.stringify(input) }); }
export function getGoals(accessToken: string, businessId: string): Promise<BusinessGoal[]> { return request(`/api/v1/businesses/${businessId}/goals`, accessToken); }
export async function saveGoals(accessToken: string, businessId: string, goals: BusinessGoal[]): Promise<BusinessGoal[]> { await request<void>(`/api/v1/businesses/${businessId}/goals`, accessToken, { method: 'PUT', body: JSON.stringify({ goals }) }); return getGoals(accessToken, businessId); }
export function getContext(accessToken: string, businessId: string): Promise<BusinessContextEntry[]> { return request(`/api/v1/businesses/${businessId}/context`, accessToken); }
export async function saveContext(accessToken: string, businessId: string, entries: BusinessContextEntry[]): Promise<BusinessContextEntry[]> { for (const entry of entries) { await request(`/api/v1/businesses/${businessId}/context/${encodeURIComponent(entry.key)}`, accessToken, { method: 'PUT', body: JSON.stringify(entry) }); } return getContext(accessToken, businessId); }
export function getKnowledgePack(accessToken: string, businessId: string): Promise<KnowledgePack> { return request(`/api/v1/businesses/${businessId}/knowledge-pack`, accessToken); }
export function getTodayFocus(accessToken: string, businessId: string): Promise<TodayFocus> { return request(`/api/v1/businesses/${businessId}/today-focus`, accessToken); }
export function getOpportunityDetail(accessToken: string, businessId: string, opportunityId: string): Promise<OpportunityDetail> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}`, accessToken); }
export function decideOpportunity(accessToken: string, businessId: string, opportunity: TodayFocusOpportunity, decision: OpportunityDecision, reason?: string): Promise<TodayFocusOpportunity> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunity.id}/decision`, accessToken, { method: 'POST', body: JSON.stringify({ decision, reason, version: opportunity.version }) }); }
export function getActionDecisionState(accessToken: string, businessId: string, opportunityId: string): Promise<ActionDecisionState> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}/action-decisions`, accessToken); }
export function recordActionDecision(accessToken: string, businessId: string, opportunityId: string, state: ActionDecisionState, status: ActionStatus, reasonCode?: ActionReasonCode, ownerNote?: string): Promise<ActionDecisionState> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}/action-decisions`, accessToken, { method: 'POST', body: JSON.stringify({ status, reasonCode, ownerNote, version: state.version }) }); }
export function getExecutionKit(accessToken: string, businessId: string, opportunityId: string): Promise<ExecutionKit> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}/execution-kit`, accessToken); }
export function updateExecutionAsset(accessToken: string, businessId: string, kitId: string, asset: ExecutionAsset, content: string, isUsed: boolean, usefulnessRating?: number | null): Promise<ExecutionKit> { return request(`/api/v1/businesses/${businessId}/execution-kits/${kitId}/assets/${asset.id}`, accessToken, { method: 'PUT', body: JSON.stringify({ content, isUsed, usefulnessRating, version: asset.version }) }); }
export function trackExecutionAssetCopy(accessToken: string, businessId: string, kitId: string, asset: ExecutionAsset): Promise<ExecutionKit> { return request(`/api/v1/businesses/${businessId}/execution-kits/${kitId}/assets/${asset.id}/copied`, accessToken, { method: 'POST', body: JSON.stringify({ version: asset.version }) }); }
export function getOutcome(accessToken: string, businessId: string, opportunityId: string): Promise<Outcome> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}/outcome`, accessToken); }
export function saveOutcome(accessToken: string, businessId: string, opportunityId: string, input: OutcomeInput): Promise<Outcome> { return request(`/api/v1/businesses/${businessId}/opportunities/${opportunityId}/outcome`, accessToken, { method: 'PUT', body: JSON.stringify(input) }); }
export function getBusinessMemory(accessToken: string, businessId: string): Promise<BusinessMemoryItem[]> { return request(`/api/v1/businesses/${businessId}/memory`, accessToken); }
export async function deleteBusinessMemory(accessToken: string, businessId: string, memoryId: string): Promise<void> { await request<void>(`/api/v1/businesses/${businessId}/memory/${memoryId}`, accessToken, { method: 'DELETE' }); }
export function getHistory(accessToken: string, businessId: string, filters: HistoryFilters = {}): Promise<HistoryResponse> { const params = new URLSearchParams(); if (filters.status) params.set('status', filters.status); if (filters.category) params.set('category', filters.category); if (filters.goalId) params.set('goalId', filters.goalId); if (filters.from) params.set('from', filters.from); if (filters.to) params.set('to', filters.to); if (filters.limit) params.set('limit', String(filters.limit)); const suffix = params.toString() ? `?${params.toString()}` : ''; return request(`/api/v1/businesses/${businessId}/history${suffix}`, accessToken); }
export function getWeeklyReview(accessToken: string, businessId: string, endingAt?: string): Promise<WeeklyReview> { const suffix = endingAt ? `?endingAt=${encodeURIComponent(endingAt)}` : ''; return request(`/api/v1/businesses/${businessId}/weekly-review${suffix}`, accessToken); }
export function getNotifications(accessToken: string, businessId: string): Promise<NotificationCenter> { return request(`/api/v1/businesses/${businessId}/notifications`, accessToken); }
export function saveNotificationPreferences(accessToken: string, businessId: string, preferences: NotificationPreference): Promise<NotificationPreference> { return request(`/api/v1/businesses/${businessId}/notification-preferences`, accessToken, { method: 'PUT', body: JSON.stringify(preferences) }); }
export function markNotificationRead(accessToken: string, businessId: string, notification: NotificationItem): Promise<NotificationItem> { return request(`/api/v1/businesses/${businessId}/notifications/${notification.id}/read`, accessToken, { method: 'PUT', body: JSON.stringify({ version: notification.version }) }); }
export function markAllNotificationsRead(accessToken: string, businessId: string): Promise<NotificationCenter> { return request(`/api/v1/businesses/${businessId}/notifications/read-all`, accessToken, { method: 'POST' }); }
export async function logout(accessToken: string): Promise<void> { await request('/api/v1/session/logout', accessToken, { method: 'POST' }); }
