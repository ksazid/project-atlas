import { useCallback, useEffect, useRef, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { getKnowledgePack, type KnowledgePack } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { loadCachedKnowledgePack, saveCachedKnowledgePack } from './cache';
import { tokens } from '@/theme/tokens';

type LoadState = 'loading' | 'ready' | 'empty' | 'error';

export function KnowledgePackScreen() {
  const [pack, setPack] = useState<KnowledgePack | null>(null);
  const packRef = useRef<KnowledgePack | null>(null);
  const [state, setState] = useState<LoadState>('loading');
  const [refreshing, setRefreshing] = useState(false);
  const [isCached, setIsCached] = useState(false);
  const [cachedAt, setCachedAt] = useState<string | null>(null);

  const applyPack = useCallback((value: KnowledgePack) => {
    packRef.current = value;
    setPack(value);
  }, []);

  const load = useCallback(async (manual = false) => {
    manual ? setRefreshing(true) : setState('loading');
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        packRef.current = null;
        setPack(null);
        setState('empty');
        return;
      }

      const cached = await loadCachedKnowledgePack(session.businessId);
      if (cached) {
        applyPack(cached.pack);
        setIsCached(true);
        setCachedAt(cached.cachedAt);
        setState('ready');
      }

      const current = await getKnowledgePack(session.accessToken, session.businessId);
      applyPack(current);
      setIsCached(false);
      setCachedAt(null);
      setState('ready');
      await saveCachedKnowledgePack(session.businessId, current);
    } catch {
      if (!packRef.current) setState('error');
    } finally {
      setRefreshing(false);
    }
  }, [applyPack]);

  useEffect(() => { void load(); }, [load]);

  if (state === 'loading') return <View style={styles.center}><Text accessibilityLiveRegion="polite">Loading Knowledge Pack…</Text></View>;
  if (state === 'empty') return <View style={styles.center}><Text accessibilityRole="header" style={styles.title}>Knowledge Pack</Text><Text style={styles.body}>Create or select a business to view its Knowledge Pack.</Text></View>;
  if (state === 'error') return <View style={styles.center}><Text accessibilityRole="header" style={styles.title}>Knowledge Pack unavailable</Text><Text style={styles.body}>We could not load the Knowledge Pack.</Text><Pressable accessibilityRole="button" onPress={() => void load()} style={styles.button}><Text style={styles.buttonText}>Try again</Text></Pressable></View>;

  return (
    <ScrollView contentContainerStyle={styles.container} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void load(true)} />}>
      <View style={styles.header}>
        <View style={styles.headerCopy}>
          <Text accessibilityRole="header" style={styles.title}>{pack?.name ?? 'Knowledge Pack'}</Text>
          <Text style={styles.body}>{pack?.description}</Text>
        </View>
        <View accessibilityLabel={`Version ${pack?.version}`} style={styles.versionBadge}><Text style={styles.versionText}>v{pack?.version}</Text></View>
      </View>

      {isCached ? <Text accessibilityLiveRegion="polite" style={styles.cacheNotice}>Offline copy{cachedAt ? ` · saved ${new Date(cachedAt).toLocaleString()}` : ''}</Text> : null}

      {pack?.sections.map((section) => (
        <View key={section.id} style={styles.section}>
          <Text style={styles.sectionTitle}>{section.title}</Text>
          <Text style={styles.sectionContent}>{section.content}</Text>
        </View>
      ))}

      {!pack?.sections.length ? <Text style={styles.body}>This Knowledge Pack has no published content yet.</Text> : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: tokens.spacing.lg, gap: tokens.spacing.md, paddingBottom: 40 },
  center: { flex: 1, justifyContent: 'center', padding: tokens.spacing.lg, gap: tokens.spacing.md },
  header: { flexDirection: 'row', alignItems: 'flex-start', gap: tokens.spacing.md },
  headerCopy: { flex: 1, gap: tokens.spacing.sm },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  versionBadge: { borderWidth: 1, borderRadius: tokens.radius.lg, paddingHorizontal: 12, paddingVertical: 6 },
  versionText: { fontWeight: '700' },
  cacheNotice: { fontSize: 14 },
  section: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  sectionTitle: { fontSize: 19, fontWeight: '700' },
  sectionContent: { fontSize: tokens.typography.body, lineHeight: 24 },
  button: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { color: '#fff', fontWeight: '700' },
});
