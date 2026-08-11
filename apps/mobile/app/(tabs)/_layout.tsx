import { Tabs } from 'expo-router';
import { AtlasIcon } from '@/components/AtlasIcon';

const GREEN = '#00754A';
const MUTED = '#7A857F';

export default function TabsLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: GREEN,
        tabBarInactiveTintColor: MUTED,
        tabBarLabelStyle: { fontSize: 10, fontWeight: '800', marginBottom: 4 },
        tabBarStyle: {
          height: 76,
          paddingTop: 8,
          paddingBottom: 8,
          borderTopColor: '#E5EAE7',
          backgroundColor: '#FFFFFF',
          shadowColor: '#173B2A',
          shadowOpacity: 0.07,
          shadowRadius: 12,
          shadowOffset: { width: 0, height: -4 },
          elevation: 8,
        },
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
