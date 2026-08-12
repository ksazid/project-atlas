import { Icon, Label, NativeTabs } from 'expo-router/unstable-native-tabs';
import { AtlasIcon } from '@/components/AtlasIcon';
import { tokens } from '@/theme/tokens';

export default function TabsLayout() {
  return (
    <NativeTabs
      iconColor={{ default: tokens.color.muted, selected: tokens.color.green }}
      labelStyle={{
        default: { color: tokens.color.muted, fontSize: 10, fontWeight: '700' },
        selected: { color: tokens.color.green, fontSize: 10, fontWeight: '800' },
      }}
      tintColor={tokens.color.green}
    >
      <NativeTabs.Trigger name="index">
        <Icon
          sf={{ default: 'sparkles', selected: 'sparkles' }}
          androidSrc={{
            default: <AtlasIcon name="home" color={tokens.color.muted} />,
            selected: <AtlasIcon name="home" color={tokens.color.green} />,
          }}
        />
        <Label>Today</Label>
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="history">
        <Icon
          sf={{ default: 'clock.arrow.circlepath', selected: 'clock.arrow.circlepath' }}
          androidSrc={{
            default: <AtlasIcon name="history" color={tokens.color.muted} />,
            selected: <AtlasIcon name="history" color={tokens.color.green} />,
          }}
        />
        <Label>History</Label>
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="goals">
        <Icon
          sf={{ default: 'flag', selected: 'flag.fill' }}
          androidSrc={{
            default: <AtlasIcon name="goals" color={tokens.color.muted} />,
            selected: <AtlasIcon name="goals" color={tokens.color.green} />,
          }}
        />
        <Label>Goals</Label>
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="profile">
        <Icon
          sf={{ default: 'person.crop.circle', selected: 'person.crop.circle.fill' }}
          androidSrc={{
            default: <AtlasIcon name="business" color={tokens.color.muted} />,
            selected: <AtlasIcon name="business" color={tokens.color.green} />,
          }}
        />
        <Label>Profile</Label>
      </NativeTabs.Trigger>
    </NativeTabs>
  );
}
