import type { Session } from '@/auth/session';

export function getSessionDestination(
  session: Session | null,
  hasPendingProgressiveQuestions = false,
): '/welcome' | '/create-business' | '/progressive-questions' | '/(tabs)' {
  if (!session) return '/welcome';
  if (!session.businessId) return '/create-business';
  return hasPendingProgressiveQuestions ? '/progressive-questions' : '/(tabs)';
}
