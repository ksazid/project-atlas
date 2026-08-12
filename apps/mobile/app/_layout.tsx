import { Stack } from 'expo-router';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AtlasAccessibilityProvider } from '@/components/AtlasAccessibilityProvider';

export default function RootLayout() {
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <AtlasAccessibilityProvider>
          <Stack screenOptions={{ headerShown: false, animation: 'default', gestureEnabled: true }} />
        </AtlasAccessibilityProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
