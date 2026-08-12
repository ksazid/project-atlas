import { Stack, useRouter } from 'expo-router';
import { Pressable, Text } from 'react-native';
import SettingsScreen from '@/features/settings/SettingsScreen';
import { tokens } from '@/theme/tokens';

export default function SettingsRoute() {
  const router = useRouter();
  const backToProfile = () => {
    if (router.canGoBack()) router.back();
    else router.replace('/(tabs)/profile');
  };

  return (
    <>
      <Stack.Screen
        options={{
          headerShown: true,
          headerShadowVisible: false,
          headerStyle: { backgroundColor: tokens.color.surface },
          headerTitle: 'Settings',
          headerTintColor: tokens.color.greenDeep,
          headerLeft: () => (
            <Pressable
              accessibilityLabel="Back to Profile"
              accessibilityRole="button"
              onPress={backToProfile}
              style={{ minHeight: 44, minWidth: 44, justifyContent: 'center' }}
            >
              <Text style={{ color: tokens.color.green, fontSize: 15, fontWeight: '800' }}>Profile</Text>
            </Pressable>
          ),
        }}
      />
      <SettingsScreen />
    </>
  );
}
