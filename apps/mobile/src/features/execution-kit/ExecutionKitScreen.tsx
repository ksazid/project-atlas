import { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { getExecutionKit, trackExecutionAssetCopy, updateExecutionAsset, type ExecutionAsset, type ExecutionKit } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { tokens } from '@/theme/tokens';

type State = 'loading' | 'ready' | 'missing' | 'error';

export function ExecutionKitScreen({ opportunityId }: { opportunityId: string }) {
  const [kit, setKit] = useState<ExecutionKit | null>(null);
  const [state, setState] = useState<State>('loading');
  const [savingId, setSavingId] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<Record<string, string>>({});

  useEffect(() => {
    let active = true;
    void loadSession().then(async (session) => {
      if (!active) return;
      if (!session?.businessId) { setState('missing'); return; }
      try {
        const value = await getExecutionKit(session.accessToken, session.businessId, opportunityId);
        if (active) {
          setKit(value);
          setDrafts(Object.fromEntries(value.assets.map((asset) => [asset.id, asset.content])));
          setState('ready');
        }
      } catch {
        if (active) setState('error');
      }
    });
    return () => { active = false; };
  }, [opportunityId]);

  const save = async (asset: ExecutionAsset, nextUsed = asset.isUsed, rating = asset.usefulnessRating) => {
    setSavingId(asset.id);
    try {
      const session = await loadSession();
      if (!session?.businessId || !kit) return;
      const value = await updateExecutionAsset(session.accessToken, session.businessId, kit.id, asset, drafts[asset.id] ?? asset.content, nextUsed, rating);
      setKit(value);
      setDrafts(Object.fromEntries(value.assets.map((item) => [item.id, item.content])));
    } catch {
      setState('error');
    } finally {
      setSavingId(null);
    }
  };

  const copied = async (asset: ExecutionAsset) => {
    setSavingId(asset.id);
    try {
      const session = await loadSession();
      if (!session?.businessId || !kit) return;
      setKit(await trackExecutionAssetCopy(session.accessToken, session.businessId, kit.id, asset));
    } catch {
      setState('error');
    } finally {
      setSavingId(null);
    }
  };

  if (state === 'loading') return <View style={styles.center}><Text accessibilityLiveRegion="polite">Preparing Execution Kit…</Text></View>;
  if (state === 'missing') return <View style={styles.center}><Text accessibilityRole="header" style={styles.title}>Execution Kit unavailable</Text><Text style={styles.body}>Select a Business and open the Opportunity again.</Text></View>;
  if (state === 'error' || !kit) return <View style={styles.center}><Text accessibilityRole="header" style={styles.title}>Execution Kit unavailable</Text><Text style={styles.body}>Atlas could not prepare or update this Kit safely.</Text><Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.primary}><Text style={styles.primaryText}>Back</Text></Pressable></View>;

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.eyebrow}>EXECUTION KIT</Text>
      <Text accessibilityRole="header" style={styles.title}>Review before acting</Text>
      <Text style={styles.body}>Edit supported assets, copy them into your own tools, mark what you used and rate usefulness. Atlas will not publish or send anything.</Text>

      {kit.assets.map((asset) => (
        <View key={asset.id} style={styles.card}>
          <Text style={styles.cardTitle}>{asset.title}</Text>
          <Text style={styles.supporting}>{asset.type} · {asset.isEditable ? 'Editable' : 'Read only'}</Text>
          {asset.isEditable ? (
            <TextInput
              accessibilityLabel={`${asset.title} content`}
              multiline
              value={drafts[asset.id] ?? asset.content}
              onChangeText={(value) => setDrafts((current) => ({ ...current, [asset.id]: value }))}
              style={styles.input}
            />
          ) : <Text selectable style={styles.body}>{asset.content}</Text>}

          <View style={styles.row}>
            {asset.isEditable ? <Pressable disabled={savingId === asset.id} onPress={() => void save(asset)} style={styles.secondary}><Text style={styles.secondaryText}>Save</Text></Pressable> : null}
            <Pressable disabled={savingId === asset.id} onPress={() => void copied(asset)} style={styles.secondary}><Text style={styles.secondaryText}>Copy used ({asset.copyCount})</Text></Pressable>
            <Pressable disabled={savingId === asset.id} onPress={() => void save(asset, !asset.isUsed)} style={styles.secondary}><Text style={styles.secondaryText}>{asset.isUsed ? 'Marked used' : 'Mark used'}</Text></Pressable>
          </View>

          <Text style={styles.label}>Usefulness</Text>
          <View style={styles.ratingRow}>
            {[1, 2, 3, 4, 5].map((rating) => (
              <Pressable key={rating} accessibilityRole="button" accessibilityLabel={`Rate ${rating} out of 5`} disabled={savingId === asset.id} onPress={() => void save(asset, asset.isUsed, rating)} style={styles.rating}>
                <Text style={styles.secondaryText}>{asset.usefulnessRating === rating ? `● ${rating}` : rating}</Text>
              </Pressable>
            ))}
          </View>
        </View>
      ))}

      <Text style={styles.supporting}>Kit v{kit.versionNumber} · {kit.knowledgePackKey} v{kit.knowledgePackVersion}</Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: tokens.spacing.lg, gap: tokens.spacing.md, paddingBottom: 40 },
  center: { flex: 1, justifyContent: 'center', padding: tokens.spacing.lg, gap: tokens.spacing.md },
  eyebrow: { fontSize: 13, fontWeight: '700', letterSpacing: 1.2 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  label: { fontSize: 14, fontWeight: '700' },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  input: { minHeight: 130, borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, textAlignVertical: 'top', fontSize: tokens.typography.body, lineHeight: 24 },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm },
  ratingRow: { flexDirection: 'row', gap: tokens.spacing.sm },
  primary: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  primaryText: { color: '#fff', fontWeight: '700' },
  secondary: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  secondaryText: { fontWeight: '700' },
  rating: { minWidth: 44, minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center' },
});
