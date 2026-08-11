import { View, type StyleProp, type ViewStyle } from 'react-native';

const ATLAS_GREEN = '#00754A';
const ATLAS_WHITE = '#FFFFFF';

type BrandMarkProps = {
  size?: number;
  style?: StyleProp<ViewStyle>;
  decorative?: boolean;
};

/** Atlas-owned Compass Orbit mark: a circular orientation motif rendered entirely from local primitives. */
export function BrandMark({ size = 72, style, decorative = false }: BrandMarkProps) {
  const unit = size / 72;
  const tickThickness = Math.max(3, 5 * unit);
  const tickLength = size * 0.16;

  return (
    <View
      accessibilityElementsHidden={decorative}
      accessibilityLabel={decorative ? undefined : 'Atlas brand mark'}
      accessibilityRole={decorative ? undefined : 'image'}
      accessible={!decorative}
      style={[{ width: size, height: size }, style]}
    >
      <View
        style={{
          position: 'absolute',
          inset: 0,
          borderRadius: size / 2,
          backgroundColor: ATLAS_GREEN,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <View
          style={{
            width: size * 0.70,
            height: size * 0.70,
            borderRadius: size * 0.35,
            borderWidth: Math.max(2, 3 * unit),
            borderColor: ATLAS_WHITE,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <View
            style={{
              width: Math.max(8, 10 * unit),
              height: Math.max(8, 10 * unit),
              borderRadius: Math.max(4, 5 * unit),
              backgroundColor: ATLAS_WHITE,
            }}
          />
        </View>
      </View>

      <View style={{ position: 'absolute', top: size * 0.07, left: (size - tickThickness) / 2, width: tickThickness, height: tickLength, borderRadius: tickThickness / 2, backgroundColor: ATLAS_WHITE }} />
      <View style={{ position: 'absolute', bottom: size * 0.07, left: (size - tickThickness) / 2, width: tickThickness, height: tickLength, borderRadius: tickThickness / 2, backgroundColor: ATLAS_WHITE }} />
      <View style={{ position: 'absolute', left: size * 0.07, top: (size - tickThickness) / 2, height: tickThickness, width: tickLength, borderRadius: tickThickness / 2, backgroundColor: ATLAS_WHITE }} />
      <View style={{ position: 'absolute', right: size * 0.07, top: (size - tickThickness) / 2, height: tickThickness, width: tickLength, borderRadius: tickThickness / 2, backgroundColor: ATLAS_WHITE }} />
    </View>
  );
}
