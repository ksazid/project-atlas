import { Image, StyleSheet, Text, View } from 'react-native';
import type { Business, BusinessHubMedia, BusinessHubProfile } from '@/api/atlas-client';
import { BrandMark } from '@/components/BrandMark';
import { getHeroPresentation } from '@/features/business-hub/business-hub-model';
import { tokens } from '@/theme/tokens';

type Props = {
  business: Business;
  profile: BusinessHubProfile | null;
  media: BusinessHubMedia[];
  imageFailed: boolean;
  onError: () => void;
};

export function BusinessHero({ business, profile, media, imageFailed, onError }: Props) {
  const hero = getHeroPresentation(media);
  const showImage = hero.kind === 'image' && !imageFailed;

  return (
    <View style={styles.card}>
      {showImage && hero.kind === 'image' ? (
        <Image
          accessibilityLabel={hero.altText || `${business.name} business photo`}
          onError={onError}
          resizeMode="cover"
          source={{ uri: hero.uri }}
          style={styles.image}
        />
      ) : (
        <View style={styles.fallback}>
          <BrandMark size={74} />
          <Text style={styles.fallbackLabel}>Your business, understood by Atlas</Text>
        </View>
      )}
      <View style={styles.identity}>
        <Text accessibilityRole="header" style={styles.name}>{business.name}</Text>
        <Text style={styles.meta}>{categoryLabel(business.category)} · {business.primaryLocation}</Text>
        {profile?.description ? <Text style={styles.description}>{profile.description}</Text> : null}
      </View>
    </View>
  );
}

function categoryLabel(value: string): string {
  return value.split(/[-_]/).filter(Boolean).map(part => `${part[0]?.toUpperCase() ?? ''}${part.slice(1)}`).join(' / ');
}

const styles = StyleSheet.create({
  card: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, overflow: 'hidden' },
  image: { backgroundColor: tokens.color.ceramic, height: 230, width: '100%' },
  fallback: { alignItems: 'center', backgroundColor: tokens.color.mint, gap: tokens.spacing.md, height: 210, justifyContent: 'center', padding: tokens.spacing.lg },
  fallbackLabel: { color: tokens.color.greenDeep, fontSize: 13, fontWeight: '800', letterSpacing: .2, textAlign: 'center' },
  identity: { gap: 7, padding: 20 },
  name: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 28, fontWeight: '800', letterSpacing: -.4, lineHeight: 34 },
  meta: { color: tokens.color.green, fontSize: 12, fontWeight: '800', letterSpacing: .4, lineHeight: 18 },
  description: { color: tokens.color.muted, fontSize: 14, lineHeight: 21, marginTop: 2 },
});
