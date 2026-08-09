import { useEffect, useState } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { Redirect } from 'expo-router';
import { loadSession } from '@/auth/session';
import { getSessionDestination } from '@/auth/session-routing';

export default function Index() {
  const [destination, setDestination] = useState<'/welcome' | '/create-business' | '/(tabs)' | null>(null);

  useEffect(() => {
    let active = true;
    loadSession()
      .then((session) => {
        if (!active) return;
        setDestination(getSessionDestination(session));
      })
      .catch(() => active && setDestination('/welcome'));
    return () => { active = false; };
  }, []);

  if (!destination) {
    return <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center' }}><ActivityIndicator accessibilityLabel="Restoring secure session" /></View>;
  }
  return <Redirect href={destination} />;
}
