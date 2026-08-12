import type { ReactElement, ReactNode } from 'react';
import {
  ScrollView,
  View,
  useWindowDimensions,
  type ScrollViewProps,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { getAtlasScreenMetrics } from '@/theme/native-layout';

export type AtlasScreenProps = {
  children: ReactNode;
  mode?: 'scroll' | 'static';
  hasTabBar?: boolean;
  contentStyle?: StyleProp<ViewStyle>;
  refreshControl?: ScrollViewProps['refreshControl'];
  showsVerticalScrollIndicator?: boolean;
  keyboardShouldPersistTaps?: ScrollViewProps['keyboardShouldPersistTaps'];
  keyboardDismissMode?: ScrollViewProps['keyboardDismissMode'];
  automaticallyAdjustKeyboardInsets?: ScrollViewProps['automaticallyAdjustKeyboardInsets'];
};

export function AtlasScreen({
  children,
  mode = 'scroll',
  hasTabBar = false,
  contentStyle,
  refreshControl,
  showsVerticalScrollIndicator,
  keyboardShouldPersistTaps,
  keyboardDismissMode,
  automaticallyAdjustKeyboardInsets,
}: AtlasScreenProps): ReactElement {
  const insets = useSafeAreaInsets();
  const { width, fontScale } = useWindowDimensions();
  const metrics = getAtlasScreenMetrics({
    width,
    topInset: insets.top,
    bottomInset: insets.bottom,
    fontScale,
    hasTabBar,
  });
  const safeAreaStyle: ViewStyle = {
    paddingTop: metrics.paddingTop,
    paddingBottom: metrics.paddingBottom,
    paddingHorizontal: metrics.paddingHorizontal,
  };

  if (mode === 'static') {
    return <View style={[{ flex: 1 }, contentStyle, safeAreaStyle]}>{children}</View>;
  }

  return (
    <ScrollView
      contentContainerStyle={[{ flexGrow: 1 }, contentStyle, safeAreaStyle]}
      refreshControl={refreshControl}
      showsVerticalScrollIndicator={showsVerticalScrollIndicator}
      keyboardShouldPersistTaps={keyboardShouldPersistTaps}
      keyboardDismissMode={keyboardDismissMode}
      automaticallyAdjustKeyboardInsets={automaticallyAdjustKeyboardInsets}
    >
      {children}
    </ScrollView>
  );
}
