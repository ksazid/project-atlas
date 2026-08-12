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
          sf={{ default: 'house', selected: 'house.fill' }}
          androidSrc={{
            default: <AtlasIcon name="home" color={tokens.color.muted} />,
            selected: <AtlasIcon name="home" color={tokens.color.green} />,
          }}
        />
        <Label>Home</Label>
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="profile">
        <Icon
          sf={{ default: 'briefcase', selected: 'briefcase.fill' }}
          androidSrc={{
            default: <AtlasIcon name="business" color={tokens.color.muted} />,
            selected: <AtlasIcon name="business" color={tokens.color.green} />,
          }}
        />
        <Label>Business</Label>
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

      <NativeTabs.Trigger name="context">
        <Icon
          sf={{ default: 'doc.text', selected: 'doc.text.fill' }}
          androidSrc={{
            default: <AtlasIcon name="context" color={tokens.color.muted} />,
            selected: <AtlasIcon name="context" color={tokens.color.green} />,
          }}
        />
        <Label>Context</Label>
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name="settings">
        <Icon
          sf={{ default: 'gearshape', selected: 'gearshape.fill' }}
          androidSrc={{
            default: <AtlasIcon name="settings" color={tokens.color.muted} />,
            selected: <AtlasIcon name="settings" color={tokens.color.green} />,
          }}
        />
        <Label>Settings</Label>
      </NativeTabs.Trigger>
    </NativeTabs>
  );
}
