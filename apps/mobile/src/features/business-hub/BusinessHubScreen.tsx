import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useRouter } from 'expo-router';
import type { BusinessHub } from '@/api/atlas-client';
import { clearBusinessSelection, loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { BusinessContextStatus } from '@/features/business-hub/BusinessContextStatus';
import { BusinessHero } from '@/features/business-hub/BusinessHero';
import { getBusinessHubState } from '@/features/business-hub/business-hub-api';
import { BusinessMediaPreview } from '@/features/business-hub/BusinessMediaPreview';
import { BusinessSnapshotCard } from '@/features/business-hub/BusinessSnapshotCard';
import { MenuIntelligenceCard } from '@/features/business-hub/MenuIntelligenceCard';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'missing' | 'error';
const REVIEW_CONTEXT_ACTION = 'Review business context';

export function BusinessHubScreen() {
  const router = useRouter();
  const [state, setState] = useState<ScreenState>('loading');
  const [hub, setHub] = useState<BusinessHub | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [heroFailed, setHeroFailed] = useState(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    else setState('loading');
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setHub(null);
        setState('missing');
        return;
      }
      const result = await getBusinessHubState(session.accessToken, session.businessId);
      if (result.state === 'missing') {
        await clearBusinessSelection();
        setHub(null);
        setState('missing');
        return;
      }
      setHub(result.hub);
      setHeroFailed(false);
      setState('ready');
    } catch {
      setState('error');
    } finally {
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  if (state !== 'ready' || !hub) {
    return <HubState state={state} onRetry={() => void load()} onContinue={() => router.replace('/create-business')} />;
  }

  return (
    <ScrollView
      contentContainerStyle={styles.container}
      refreshControl={<RefreshControl refreshing={refreshing} tintColor={tokens.color.green} onRefresh={() => void load(true)} />}
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.content}>
        <View style={styles.header}>
          <BrandMark size={48} />
          <Text style={styles.eyebrow}>BUSINESS</Text>
          <Text accessibilityRole="header" style={styles.heading}>This is the business Atlas understands today.</Text>
          <Text style={styles.subheading}>Review the real operating picture behind your recommendations, then edit only when something has changed.</Text>
        </View>

        <BusinessHero business={hub.business} profile={hub.profile} media={hub.media} imageFailed={heroFailed} onError={() => setHeroFailed(true)} />
        <BusinessSnapshotCard business={hub.business} profile={hub.profile} />
        <BusinessMediaPreview media={hub.media} title="Business photos" />
        <MenuIntelligenceCard menu={hub.menu} title="Menu intelligence" onViewFull={() => router.push('/business-menu')} />
        <View accessibilityLabel={REVIEW_CONTEXT_ACTION}>
          <BusinessContextStatus context={hub.context} onReview={() => router.push('/(tabs)/context')} />
        </View>

        <Pressable accessibilityRole="button" accessibilityLabel="Edit business details" onPress={() => router.push('/edit-business')} style={({ pressed }) => [styles.editButton, pressed && styles.pressed]}>
          <Text style={styles.editButtonText}>Edit business details</Text>
        </Pressable>
        {hub.latestObservedAt ? <Text style={styles.freshness}>Business intelligence last observed {formatDate(hub.latestObservedAt)}.</Text> : null}
      </View>
    </ScrollView>
  );
}

function HubState({ state, onRetry, onContinue }: { state: ScreenState; onRetry: () => void; onContinue: () => void }) {
  if (state === 'loading') return <View style={styles.stateScreen}><BrandMark size={56} /><ActivityIndicator color={tokens.color.green} /><Text style={styles.stateCopy}>Loading your business…</Text></View>;
  if (state === 'missing') return <View style={styles.stateScreen}><BrandMark size={56} /><Text accessibilityRole="header" style={styles.stateTitle}>Set up your business</Text><Text style={styles.stateCopy}>Atlas does not have a business for this account yet. Start setup to add one.</Text><Pressable accessibilityRole="button" accessibilityLabel="Set up your business" onPress={onContinue} style={({ pressed }) => [styles.stateButton, pressed && styles.pressed]}><Text style={styles.stateButtonText}>Set up your business</Text></Pressable></View>;
  return <View style={styles.stateScreen}><BrandMark size={56} /><Text accessibilityRole="header" style={styles.stateTitle}>Business Hub is temporarily unavailable</Text><Text style={styles.stateCopy}>Your saved business information is unchanged. Try loading it again.</Text><Pressable accessibilityRole="button" accessibilityLabel="Try again" onPress={onRetry} style={({ pressed }) => [styles.stateButton, pressed && styles.pressed]}><Text style={styles.stateButtonText}>Try again</Text></Pressable></View>;
}

function formatDate(value: string): string { const date = new Date(value); return Number.isNaN(date.getTime()) ? 'recently' : date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' }); }

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: tokens.color.surface, flexGrow: 1, paddingBottom: 38, paddingHorizontal: 28, paddingTop: 54 },
  content: { gap: 18, maxWidth: 680, width: '100%' }, header: { gap: 7, marginBottom: 2 },
  eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.2, marginTop: 7 },
  heading: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 31, fontWeight: '800', letterSpacing: -.45, lineHeight: 37 },
  subheading: { color: tokens.color.muted, fontSize: 14.5, lineHeight: 22 },
  editButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 52, paddingHorizontal: 22 }, editButtonText: { color: tokens.color.surface, fontSize: 14, fontWeight: '800' },
  freshness: { color: tokens.color.muted, fontSize: 11.5, lineHeight: 17, textAlign: 'center' }, pressed: { opacity: .86, transform: [{ scale: .99 }] },
  stateScreen: { alignItems: 'center', backgroundColor: tokens.color.surface, flex: 1, gap: 14, justifyContent: 'center', padding: 28 }, stateTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 27, fontWeight: '800', lineHeight: 33, textAlign: 'center' }, stateCopy: { color: tokens.color.muted, fontSize: 14, lineHeight: 21, maxWidth: 380, textAlign: 'center' }, stateButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 48, paddingHorizontal: 20 }, stateButtonText: { color: tokens.color.surface, fontSize: 14, fontWeight: '800' },
});
