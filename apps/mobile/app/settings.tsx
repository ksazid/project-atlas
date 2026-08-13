import { Stack } from 'expo-router';
import SettingsScreen from '@/features/settings/SettingsScreen';

export default function SettingsRoute() {
  return (
    <>
      <Stack.Screen options={{ headerShown: false }} />
      <SettingsScreen />
    </>
  );
}
