import { Tabs } from 'expo-router';
import { useWindowDimensions } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { AtlasIcon } from '@/components/AtlasIcon';
import { AtlasMaterialSurface } from '@/components/AtlasMaterialSurface';
import { getAtlasTabBarMetrics } from '@/theme/native-layout';
import { tokens } from '@/theme/tokens';

export default function TabsLayout() {
  const insets = useSafeAreaInsets();
  const { width, fontScale } = useWindowDimensions();
  const metrics = getAtlasTabBarMetrics({ width, bottomInset: insets.bottom, fontScale });

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: tokens.color.green,
        tabBarInactiveTintColor: tokens.color.muted,
        tabBarAllowFontScaling: true,
        tabBarLabelStyle: { fontSize: 10, fontWeight: '800', marginBottom: 2 },
        tabBarItemStyle: { minHeight: tokens.touchTarget },
        tabBarStyle: {
          position: 'absolute',
          left: metrics.horizontalInset,
          right: metrics.horizontalInset,
          bottom: metrics.bottomOffset,
          height: metrics.frameHeight,
          paddingTop: 5,
          paddingBottom: metrics.paddingBottom,
          borderTopWidth: 0,
          backgroundColor: 'transparent',
          elevation: 0,
          shadowOpacity: 0,
        },
        tabBarBackground: () => (
          <AtlasMaterialSurface
            kind="navigation"
            style={{ flex: 1, borderRadius: metrics.borderRadius }}
          />
        ),
      }}
    >
      <Tabs.Screen name="index" options={{ title: 'Home', tabBarIcon: ({ color }) => <AtlasIcon name="home" color={color} /> }} />
      <Tabs.Screen name="profile" options={{ title: 'Business', tabBarIcon: ({ color }) => <AtlasIcon name="business" color={color} /> }} />
      <Tabs.Screen name="goals" options={{ title: 'Goals', tabBarIcon: ({ color }) => <AtlasIcon name="goals" color={color} /> }} />
      <Tabs.Screen name="context" options={{ title: 'Context', tabBarIcon: ({ color }) => <AtlasIcon name="context" color={color} /> }} />
      <Tabs.Screen name="settings" options={{ title: 'Settings', tabBarIcon: ({ color }) => <AtlasIcon name="settings" color={color} /> }} />
    </Tabs>
  );
}
