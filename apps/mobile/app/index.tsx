import { useEffect, useState } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { Redirect } from 'expo-router';
import { loadSession } from '@/auth/session';

export default function Index() {
  const [destination, setDestination] = useState<'/sign-in' | '/create-business' | '/(tabs)' | null>(null);

  useEffect(() => {
    let active = true;
    loadSession()
      .then((session) => {
        if (!active) return;
        setDestination(!session ? '/sign-in' : session.businessId ? '/(tabs)' : '/create-business');
      })
      .catch(() => active && setDestination('/sign-in'));
    return () => { active = false; };
  }, []);

  if (!destination) {
    return <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center' }}><ActivityIndicator accessibilityLabel="Restoring secure session" /></View>;
  }
  return <Redirect href={destination} />;
}
