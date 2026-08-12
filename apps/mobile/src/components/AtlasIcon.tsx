import type { ReactElement } from 'react';
import { View } from 'react-native';

export type AtlasIconName = 'home' | 'history' | 'business' | 'goals' | 'context' | 'settings';

type Props = { name: AtlasIconName; size?: number; color: string };

export function AtlasIcon({ name, size = 20, color }: Props): ReactElement {
  const stroke = Math.max(1.7, size * 0.095);
  return (
    <View accessible={false} style={{ width: size, height: size }}>
      {name === 'home' ? <HomeIcon size={size} color={color} stroke={stroke} /> : null}
      {name === 'history' ? <HistoryIcon size={size} color={color} stroke={stroke} /> : null}
      {name === 'business' ? <BusinessIcon size={size} color={color} stroke={stroke} /> : null}
      {name === 'goals' ? <GoalsIcon size={size} color={color} stroke={stroke} /> : null}
      {name === 'context' ? <ContextIcon size={size} color={color} stroke={stroke} /> : null}
      {name === 'settings' ? <SettingsIcon size={size} color={color} stroke={stroke} /> : null}
    </View>
  );
}

function HomeIcon({ size, color, stroke }: IconGeometryProps) {
  return <>
    <View style={{ position: 'absolute', left: size * .18, top: size * .39, width: size * .64, height: size * .49, borderColor: color, borderWidth: stroke, borderTopWidth: 0, borderRadius: size * .08 }} />
    <View style={{ position: 'absolute', left: size * .20, top: size * .24, width: size * .43, height: stroke, borderRadius: stroke, backgroundColor: color, transform: [{ rotate: '-38deg' }] }} />
    <View style={{ position: 'absolute', right: size * .20, top: size * .24, width: size * .43, height: stroke, borderRadius: stroke, backgroundColor: color, transform: [{ rotate: '38deg' }] }} />
    <View style={{ position: 'absolute', left: size * .43, bottom: size * .12, width: size * .18, height: size * .27, borderColor: color, borderWidth: stroke, borderBottomWidth: 0, borderRadius: size * .04 }} />
  </>;
}

function HistoryIcon({ size, color, stroke }: IconGeometryProps) {
  return <>
    <View style={{ position: 'absolute', left: size * .14, top: size * .14, width: size * .72, height: size * .72, borderRadius: size * .36, borderColor: color, borderWidth: stroke }} />
    <View style={{ position: 'absolute', left: size * .49, top: size * .28, width: stroke, height: size * .24, borderRadius: stroke, backgroundColor: color }} />
    <View style={{ position: 'absolute', left: size * .49, top: size * .49, width: size * .20, height: stroke, borderRadius: stroke, backgroundColor: color, transform: [{ rotate: '28deg' }] }} />
    <View style={{ position: 'absolute', left: size * .05, top: size * .20, width: size * .24, height: stroke, borderRadius: stroke, backgroundColor: color, transform: [{ rotate: '-24deg' }] }} />
  </>;
}

function BusinessIcon({ size, color, stroke }: IconGeometryProps) {
  return <>
    <View style={{ position: 'absolute', left: size * .12, top: size * .12, width: size * .76, height: size * .76, borderRadius: size * .38, borderColor: color, borderWidth: stroke }} />
    <View style={{ position: 'absolute', left: size * .34, top: size * .34, width: size * .32, height: size * .32, borderRadius: size * .16, borderColor: color, borderWidth: stroke }} />
    <View style={{ position: 'absolute', top: size * .03, left: size * .46, width: stroke, height: size * .20, borderRadius: stroke, backgroundColor: color }} />
    <View style={{ position: 'absolute', bottom: size * .03, left: size * .46, width: stroke, height: size * .20, borderRadius: stroke, backgroundColor: color }} />
    <View style={{ position: 'absolute', left: size * .03, top: size * .46, height: stroke, width: size * .20, borderRadius: stroke, backgroundColor: color }} />
    <View style={{ position: 'absolute', right: size * .03, top: size * .46, height: stroke, width: size * .20, borderRadius: stroke, backgroundColor: color }} />
  </>;
}

function GoalsIcon({ size, color, stroke }: IconGeometryProps) {
  return <>
    <View style={{ position: 'absolute', left: size * .10, bottom: size * .20, width: size * .38, height: stroke, borderRadius: stroke, backgroundColor: color, transform: [{ rotate: '-28deg' }] }} />
    <View style={{ position: 'absolute', left: size * .37, top: size * .42, width: size * .34, height: stroke, borderRadius: stroke, backgroundColor: color, transform: [{ rotate: '-45deg' }] }} />
    <View style={{ position: 'absolute', right: size * .13, top: size * .21, width: size * .28, height: stroke, borderRadius: stroke, backgroundColor: color }} />
    <View style={{ position: 'absolute', right: size * .13, top: size * .21, width: stroke, height: size * .28, borderRadius: stroke, backgroundColor: color }} />
  </>;
}

function ContextIcon({ size, color, stroke }: IconGeometryProps) {
  return <>
    <View style={{ position: 'absolute', left: size * .10, top: size * .10, width: size * .80, height: size * .80, borderRadius: size * .40, borderColor: color, borderWidth: stroke }} />
    <View style={{ position: 'absolute', left: size * .28, top: size * .28, width: size * .44, height: size * .44, borderRadius: size * .22, borderColor: color, borderWidth: stroke }} />
    <View style={{ position: 'absolute', left: size * .45, top: size * .45, width: size * .10, height: size * .10, borderRadius: size * .05, backgroundColor: color }} />
  </>;
}

function SettingsIcon({ size, color, stroke }: IconGeometryProps) {
  return <>
    {[.25, .50, .75].map(top => <View key={top} style={{ position: 'absolute', left: size * .10, top: size * top, width: size * .80, height: stroke, borderRadius: stroke, backgroundColor: color }} />)}
    <View style={{ position: 'absolute', left: size * .27, top: size * .25 - size * .075 + stroke / 2, width: size * .15, height: size * .15, borderRadius: size * .075, backgroundColor: color }} />
    <View style={{ position: 'absolute', right: size * .25, top: size * .50 - size * .075 + stroke / 2, width: size * .15, height: size * .15, borderRadius: size * .075, backgroundColor: color }} />
    <View style={{ position: 'absolute', left: size * .39, top: size * .75 - size * .075 + stroke / 2, width: size * .15, height: size * .15, borderRadius: size * .075, backgroundColor: color }} />
  </>;
}

type IconGeometryProps = { size: number; color: string; stroke: number };
