import { useMemo, useState } from 'react';
import { Image, StyleSheet, Text, View } from 'react-native';
import type { BusinessHubMedia } from '@/api/atlas-client';
import { tokens } from '@/theme/tokens';

type Props = { media: BusinessHubMedia[]; title?: string };

export function BusinessMediaPreview({ media, title = 'Business photos' }: Props) {
  const [failed, setFailed] = useState<Record<string, true>>({});
  const visible = useMemo(() => media.filter(item => !failed[item.remoteUrl]).slice(0, 6), [failed, media]);
  if (visible.length === 0) return null;

  return (
    <View style={styles.section}>
      <View style={styles.heading}>
        <Text style={styles.eyebrow}>VISUAL CONTEXT</Text>
        <Text style={styles.title}>{title}</Text>
      </View>
      <View style={styles.grid}>
        {visible.map((item, index) => (
          <Image
            key={item.remoteUrl}
            accessibilityLabel={item.altText || `Business photo ${index + 1}`}
            onError={() => setFailed(current => ({ ...current, [item.remoteUrl]: true }))}
            resizeMode="cover"
            source={{ uri: item.remoteUrl }}
            style={[styles.image, index === 0 ? styles.heroImage : styles.smallImage]}
          />
        ))}
      </View>
      <Text style={styles.note}>Photos are references to public business imagery already observed by Atlas.</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  section: { gap: 12 },
  heading: { gap: 4 },
  eyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  title: { color: tokens.color.ink, fontSize: 19, fontWeight: '800', lineHeight: 25 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  image: { backgroundColor: tokens.color.ceramic, borderRadius: tokens.radius.md },
  heroImage: { height: 190, width: '100%' },
  smallImage: { aspectRatio: 1.25, flexBasis: '31%', flexGrow: 1, minWidth: 96 },
  note: { color: tokens.color.muted, fontSize: 11.5, lineHeight: 17 },
});
