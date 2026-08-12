import type { ReactElement, ReactNode } from 'react';
import { Platform, StyleSheet, View, type StyleProp, type ViewStyle } from 'react-native';
import { GlassView, isGlassEffectAPIAvailable, isLiquidGlassAvailable } from 'expo-glass-effect';
import { useAtlasAccessibility } from '@/components/AtlasAccessibilityProvider';
import { resolveMaterialMode } from '@/lib/accessibility-policy';
import { tokens } from '@/theme/tokens';

export type AtlasMaterialSurfaceProps = {
  children?: ReactNode;
  kind: 'navigation' | 'sheet' | 'floating';
  style?: StyleProp<ViewStyle>;
};

export function AtlasMaterialSurface({ children, kind, style }: AtlasMaterialSurfaceProps): ReactElement {
  const { reduceTransparency } = useAtlasAccessibility();
  const glassAvailable = Platform.OS === 'ios'
    && isGlassEffectAPIAvailable()
    && isLiquidGlassAvailable();
  const mode = resolveMaterialMode({
    platform: Platform.OS,
    glassAvailable,
    reduceTransparency,
  });
  const kindStyle = kind === 'navigation'
    ? styles.navigation
    : kind === 'sheet'
      ? styles.sheet
      : styles.floating;

  if (mode === 'glass') {
    return (
      <GlassView glassEffectStyle="regular" style={[styles.base, kindStyle, style]}>
        {children}
      </GlassView>
    );
  }

  return <View style={[styles.base, styles.solid, kindStyle, style]}>{children}</View>;
}

const styles = StyleSheet.create({
  base: {
    overflow: 'hidden',
  },
  solid: {
    backgroundColor: tokens.color.surface,
    borderColor: tokens.color.border,
    borderWidth: StyleSheet.hairlineWidth,
  },
  navigation: {
    shadowColor: tokens.color.greenDeep,
    shadowOffset: { width: 0, height: 6 },
    shadowRadius: 18,
    shadowOpacity: 0.12,
    elevation: 8,
  },
  sheet: {
    shadowColor: tokens.color.greenDeep,
    shadowOffset: { width: 0, height: -4 },
    shadowRadius: 20,
    shadowOpacity: 0.14,
    elevation: 10,
  },
  floating: {
    shadowColor: tokens.color.greenDeep,
    shadowOffset: { width: 0, height: 4 },
    shadowRadius: 12,
    shadowOpacity: 0.1,
    elevation: 6,
  },
});
