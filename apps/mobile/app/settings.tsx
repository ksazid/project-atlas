import { Stack, useRouter } from 'expo-router';
import { Pressable, Text } from 'react-native';
import SettingsScreen from '@/features/settings/SettingsScreen';
import { tokens } from '@/theme/tokens';

export default function SettingsRoute() {
  const router = useRouter();
  const canGoBack = router.canGoBack();

  return (
    <>
      <Stack.Screen
        options={{
          headerShown: true,
          headerShadowVisible: false,
          headerStyle: { backgroundColor: tokens.color.surface },
          headerTitle: 'Settings',
          headerTintColor: tokens.color.green,
          headerBackTitle: 'Profile',
          headerLeft: canGoBack
            ? undefined
            : () => (
                <Pressable
                  accessibilityLabel="Back to Profile"
                  accessibilityRole="button"
                  onPress={() => router.replace('/(tabs)/profile')}
                  style={{ minHeight: 44, minWidth: 44, justifyContent: 'center' }}
                >
                  <Text style={{ color: tokens.color.green, fontSize: 24, fontWeight: '700' }}>‹</Text>
                </Pressable>
              ),
        }}
      />
      <SettingsScreen />
    </>
  );
}
