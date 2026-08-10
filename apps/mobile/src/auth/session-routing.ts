import type { Session } from '@/auth/session';

type BaseDestination = '/welcome' | '/create-business' | '/(tabs)';
type ProgressiveDestination = BaseDestination | '/progressive-questions';

export function getSessionDestination(session: Session | null): BaseDestination;
export function getSessionDestination(session: Session | null, hasPendingProgressiveQuestions: boolean): ProgressiveDestination;
export function getSessionDestination(
  session: Session | null,
  hasPendingProgressiveQuestions = false,
): ProgressiveDestination {
  if (!session) return '/welcome';
  if (!session.businessId) return '/create-business';
  return hasPendingProgressiveQuestions ? '/progressive-questions' : '/(tabs)';
}
