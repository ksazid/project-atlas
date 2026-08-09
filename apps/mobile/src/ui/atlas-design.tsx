import { PropsWithChildren, useEffect, useRef } from 'react';
import { Animated, StyleSheet, Text, View } from 'react-native';

export const atlas = {
  colors: {
    canvas: '#F7FAF8',
    surface: '#FFFFFF',
    surfaceSoft: '#F1F8F4',
    ink: '#10231A',
    text: '#4B5F54',
    muted: '#74867C',
    line: '#DCE9E1',
    accent: '#25A667',
    accentDeep: '#168752',
    accentSoft: '#DFF5E8',
    blue: '#4E8DF7',
    blueSoft: '#E8F1FF',
    danger: '#B42318',
  },
  radius: { sm: 12, md: 18, lg: 24, xl: 30 },
  shadow: {
    shadowColor: '#173B2A',
    shadowOpacity: 0.08,
    shadowRadius: 22,
    shadowOffset: { width: 0, height: 10 },
    elevation: 4,
  },
};

export function AtlasBackdrop() {
  return (
    <View pointerEvents="none" style={StyleSheet.absoluteFill}>
      <View style={[styles.blob, styles.blobMint]} />
      <View style={[styles.blob, styles.blobBlue]} />
      <View style={styles.gridDotOne} />
      <View style={styles.gridDotTwo} />
    </View>
  );
}

export function AtlasMark({ compact = false }: { compact?: boolean }) {
  return (
    <View style={[styles.mark, compact && styles.markCompact]}>
      <Text style={[styles.markLetter, compact && styles.markLetterCompact]}>A</Text>
      <View style={styles.markSpark}><Text style={styles.markSparkText}>✦</Text></View>
    </View>
  );
}

export function Reveal({ children, delay = 0, distance = 14 }: PropsWithChildren<{ delay?: number; distance?: number }>) {
  const opacity = useRef(new Animated.Value(0)).current;
  const translateY = useRef(new Animated.Value(distance)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(opacity, { toValue: 1, duration: 420, delay, useNativeDriver: true }),
      Animated.timing(translateY, { toValue: 0, duration: 420, delay, useNativeDriver: true }),
    ]).start();
  }, [delay, distance, opacity, translateY]);

  return <Animated.View style={{ opacity, transform: [{ translateY }] }}>{children}</Animated.View>;
}

export function IconBubble({ icon, label, tone = 'green' }: { icon: string; label?: string; tone?: 'green' | 'blue' | 'plain' }) {
  const backgroundColor = tone === 'green' ? atlas.colors.accentSoft : tone === 'blue' ? atlas.colors.blueSoft : atlas.colors.surface;
  const color = tone === 'green' ? atlas.colors.accentDeep : tone === 'blue' ? atlas.colors.blue : atlas.colors.ink;
  return (
    <View accessibilityLabel={label} style={[styles.iconBubble, { backgroundColor }]}>
      <Text style={[styles.iconText, { color }]}>{icon}</Text>
    </View>
  );
}

export function ProgressFlow({ current }: { current: 1 | 2 | 3 }) {
  const items = ['Find business', 'Discover', 'Confirm'];
  return (
    <View style={styles.progressWrap} accessibilityLabel={`Onboarding step ${current} of 3`}>
      <View style={styles.progressLine} />
      {items.map((label, index) => {
        const number = index + 1;
        const done = number < current;
        const active = number === current;
        return (
          <View key={label} style={styles.progressItem}>
            <View style={[styles.progressDot, (done || active) && styles.progressDotActive]}>
              <Text style={[styles.progressDotText, (done || active) && styles.progressDotTextActive]}>{done ? '✓' : number}</Text>
            </View>
            <Text style={[styles.progressLabel, active && styles.progressLabelActive]}>{label}</Text>
          </View>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  blob: { position: 'absolute', borderRadius: 999 },
  blobMint: { width: 300, height: 300, right: -150, top: -85, backgroundColor: '#DFF7E9', opacity: 0.72 },
  blobBlue: { width: 220, height: 220, left: -130, bottom: 70, backgroundColor: '#E9F2FF', opacity: 0.62 },
  gridDotOne: { position: 'absolute', width: 7, height: 7, borderRadius: 4, backgroundColor: '#B8E6CC', top: 118, left: 36 },
  gridDotTwo: { position: 'absolute', width: 5, height: 5, borderRadius: 3, backgroundColor: '#C9DCF8', top: 154, right: 46 },
  mark: { width: 78, height: 78, borderRadius: 24, backgroundColor: atlas.colors.surface, alignItems: 'center', justifyContent: 'center', ...atlas.shadow },
  markCompact: { width: 48, height: 48, borderRadius: 16 },
  markLetter: { fontSize: 42, lineHeight: 46, fontWeight: '900', color: atlas.colors.accentDeep, letterSpacing: -2 },
  markLetterCompact: { fontSize: 26, lineHeight: 30 },
  markSpark: { position: 'absolute', right: -4, top: -5, width: 26, height: 26, borderRadius: 13, alignItems: 'center', justifyContent: 'center', backgroundColor: atlas.colors.blueSoft },
  markSparkText: { fontSize: 14, color: atlas.colors.blue, fontWeight: '900' },
  iconBubble: { width: 42, height: 42, borderRadius: 14, alignItems: 'center', justifyContent: 'center' },
  iconText: { fontSize: 18, fontWeight: '800' },
  progressWrap: { flexDirection: 'row', justifyContent: 'space-between', position: 'relative', marginBottom: 4 },
  progressLine: { position: 'absolute', left: '15%', right: '15%', top: 15, height: 2, backgroundColor: atlas.colors.line },
  progressItem: { width: '31%', alignItems: 'center', gap: 7 },
  progressDot: { width: 30, height: 30, borderRadius: 15, borderWidth: 1, borderColor: atlas.colors.line, backgroundColor: atlas.colors.surface, alignItems: 'center', justifyContent: 'center' },
  progressDotActive: { borderColor: atlas.colors.accent, backgroundColor: atlas.colors.accent },
  progressDotText: { color: atlas.colors.muted, fontSize: 12, fontWeight: '800' },
  progressDotTextActive: { color: '#FFFFFF' },
  progressLabel: { fontSize: 11, fontWeight: '600', color: atlas.colors.muted, textAlign: 'center' },
  progressLabelActive: { color: atlas.colors.accentDeep, fontWeight: '800' },
});
