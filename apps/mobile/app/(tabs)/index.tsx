import { useEffect, useState } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { Redirect, useLocalSearchParams } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { loadSession } from '@/auth/session';
import { getSessionDestination } from '@/auth/session-routing';
import { TodayFocusScreen } from '@/features/today-focus/TodayFocusScreen';

export default function HomeScreen() {
  const { sessionEntry } = useLocalSearchParams<{ sessionEntry?: string }>();
  const [destination, setDestination] = useState<'/welcome' | '/create-business' | '/(tabs)' | null>(null);

  useEffect(() => {
    if (sessionEntry !== '1') return;
    let active = true;
    loadSession()
      .then((session) => active && setDestination(getSessionDestination(session)))
      .catch(() => active && setDestination('/welcome'));
    return () => { active = false; };
  }, [sessionEntry]);

  if (sessionEntry === '1' && !destination) {
    return <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center' }}><ActivityIndicator accessibilityLabel="Restoring secure session" /></View>;
  }

  if (sessionEntry === '1' && destination && destination !== '/(tabs)') return <Redirect href={destination} />;

  return <><TodayFocusScreen /><StatusBar style="auto" /></>;
}
