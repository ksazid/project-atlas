import type { PilotBusinessListItem, PilotOpportunity } from './pilot-operations-api';

export type PilotScreenState = 'loading' | 'ready' | 'empty' | 'forbidden' | 'error';

export function attentionLabel(item: PilotBusinessListItem): string {
  if (item.unsafeFeedbackCount > 0) return 'Unsafe guidance';
  if (item.latestGenerationOutcome === 'degraded') return 'Generation degraded';
  if (item.latestGenerationOutcome === 'no-focus' || item.latestGenerationOutcome === 'insufficient-context') return 'Needs attention';
  if (!item.profileConfirmed) return 'Profile confirmation needed';
  return 'Review ready';
}

export function generationLabel(outcome?: string | null, code?: string | null): string {
  if (!outcome) return 'No generation run yet';
  const readable = outcome.replaceAll('-', ' ');
  return code ? `${readable} · ${code.replaceAll('_', ' ')}` : readable;
}

export function withdrawalReasonError(value: string): string | null {
  const reason = value.trim();
  if (!reason) return 'Withdrawal reason is required.';
  if (reason.length > 2000) return 'Withdrawal reason must be 2000 characters or fewer.';
  return null;
}

export function boundedReasonError(value: string, label: string): string | null {
  const reason = value.trim();
  if (!reason) return `${label} is required.`;
  if (reason.length > 2000) return `${label} must be 2000 characters or fewer.`;
  return null;
}

export function canWithdraw(opportunity: PilotOpportunity, now = Date.now()): boolean {
  return opportunity.status === 'available' && new Date(opportunity.expiresAt).getTime() > now;
}

export function formatPilotDate(value?: string | null): string {
  if (!value) return 'Not yet';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Recently' : date.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
}
