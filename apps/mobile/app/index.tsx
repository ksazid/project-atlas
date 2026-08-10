import { useEffect, useState } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { Redirect } from 'expo-router';
import { getProgressiveQuestions } from '@/api/progressive-questions';
import { loadSession } from '@/auth/session';
import { getSessionDestination } from '@/auth/session-routing';

type Destination = '/welcome' | '/create-business' | '/progressive-questions' | '/(tabs)';

export default function Index() {
  const [destination, setDestination] = useState<Destination | null>(null);

  useEffect(() => {
    let active = true;

    async function restore() {
      try {
        const session = await loadSession();
        if (!active) return;
        if (!session || !session.businessId) {
          setDestination(getSessionDestination(session));
          return;
        }

        try {
          const set = await getProgressiveQuestions(session.accessToken, session.businessId);
          if (active) setDestination(getSessionDestination(session, set.questions.length > 0));
        } catch {
          if (active) setDestination('/(tabs)');
        }
      } catch {
        if (active) setDestination('/welcome');
      }
    }

    void restore();
    return () => { active = false; };
  }, []);

  if (!destination) {
    return <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center' }}><ActivityIndicator accessibilityLabel="Restoring secure session" /></View>;
  }
  return <Redirect href={destination} />;
}
