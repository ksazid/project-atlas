import type { Session } from '@/auth/session';

export function getSessionDestination(session: Session | null): '/welcome' | '/create-business' | '/(tabs)' {
  return !session ? '/welcome' : session.businessId ? '/(tabs)' : '/create-business';
}
