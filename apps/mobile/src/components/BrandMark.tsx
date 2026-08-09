import { Image, type ImageStyle, type StyleProp } from 'react-native';

const PROTOTYPE_MARK_URI = 'https://upload.wikimedia.org/wikipedia/en/thumb/d/d3/Starbucks_Corporation_Logo_2011.svg/512px-Starbucks_Corporation_Logo_2011.svg.png';

type BrandMarkProps = {
  size?: number;
  style?: StyleProp<ImageStyle>;
  decorative?: boolean;
};

export function BrandMark({ size = 72, style, decorative = false }: BrandMarkProps) {
  return (
    <Image
      accessibilityElementsHidden={decorative}
      accessibilityIgnoresInvertColors
      accessibilityLabel={decorative ? undefined : 'Atlas brand mark'}
      source={{ uri: PROTOTYPE_MARK_URI }}
      style={[{ width: size, height: size, resizeMode: 'contain' }, style]}
    />
  );
}
