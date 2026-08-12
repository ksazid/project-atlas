import type { ReactElement } from 'react';
import {
  Pressable,
  type GestureResponderEvent,
  type PressableProps,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import Animated, {
  useAnimatedStyle,
  useSharedValue,
  withSpring,
  withTiming,
} from 'react-native-reanimated';
import { useAtlasAccessibility } from '@/components/AtlasAccessibilityProvider';
import { tokens } from '@/theme/tokens';

const AnimatedPressable = Animated.createAnimatedComponent(Pressable);

export type AtlasPressableProps = Omit<PressableProps, 'style'> & {
  style?: StyleProp<ViewStyle>;
  pressedScale?: number;
  pressedOpacity?: number;
};

export function AtlasPressable({
  style,
  pressedScale = tokens.native.pressScale,
  pressedOpacity = 0.92,
  onPressIn,
  onPressOut,
  ...props
}: AtlasPressableProps): ReactElement {
  const { reduceMotion } = useAtlasAccessibility();
  const scale = useSharedValue(1);
  const opacity = useSharedValue(1);
  const feedbackStyle = useAnimatedStyle(() => ({
    opacity: opacity.value,
    transform: [{ scale: scale.value }],
  }));

  function handlePressIn(event: GestureResponderEvent) {
    if (reduceMotion) {
      scale.value = 1;
      opacity.value = pressedOpacity;
    } else {
      scale.value = withTiming(pressedScale, { duration: 70 });
      opacity.value = withTiming(pressedOpacity, { duration: 70 });
    }
    onPressIn?.(event);
  }

  function handlePressOut(event: GestureResponderEvent) {
    if (reduceMotion) {
      scale.value = 1;
      opacity.value = 1;
    } else {
      scale.value = withSpring(1, {
        stiffness: 300,
        damping: 35,
        mass: 1,
        overshootClamping: true,
      });
      opacity.value = withTiming(1, { duration: 100 });
    }
    onPressOut?.(event);
  }

  return (
    <AnimatedPressable
      {...props}
      onPressIn={handlePressIn}
      onPressOut={handlePressOut}
      style={[style, feedbackStyle]}
    />
  );
}
