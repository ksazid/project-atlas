import { Tabs } from 'expo-router';
import { Text } from 'react-native';

const GREEN = '#00754A';
const MUTED = '#7A857F';

function TabIcon({ symbol, focused }: { symbol: string; focused: boolean }) {
  return <Text style={{ fontSize: 18, fontWeight: '900', color: focused ? GREEN : MUTED }}>{symbol}</Text>;
}

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
      <Tabs.Screen name="index" options={{ title: 'Home', tabBarIcon: ({ focused }) => <TabIcon symbol="⌂" focused={focused} /> }} />
      <Tabs.Screen name="profile" options={{ title: 'Business', tabBarIcon: ({ focused }) => <TabIcon symbol="◎" focused={focused} /> }} />
      <Tabs.Screen name="goals" options={{ title: 'Goals', tabBarIcon: ({ focused }) => <TabIcon symbol="↗" focused={focused} /> }} />
      <Tabs.Screen name="context" options={{ title: 'Context', tabBarIcon: ({ focused }) => <TabIcon symbol="◌" focused={focused} /> }} />
      <Tabs.Screen name="settings" options={{ title: 'Settings', tabBarIcon: ({ focused }) => <TabIcon symbol="⚙" focused={focused} /> }} />
    </Tabs>
  );
}
