import { Stack } from 'expo-router';
import { tokens } from '@/theme/tokens';

export default function OperatorLayout() {
  return (
    <Stack
      screenOptions={{
        headerBackTitle: 'Pilot Operations',
        headerTintColor: tokens.color.green,
        headerShadowVisible: false,
        headerStyle: { backgroundColor: tokens.color.surface },
        contentStyle: { backgroundColor: tokens.color.surface },
      }}
    >
      <Stack.Screen name="index" options={{ title: 'Pilot Operations', headerShown: false }} />
      <Stack.Screen name="businesses/[businessId]" options={{ title: 'Business review' }} />
    </Stack>
  );
}
