import { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { deleteBusinessMemory, getBusinessMemory, type BusinessMemoryItem } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { tokens } from '@/theme/tokens';

export function BusinessMemoryPanel() {
  const [items, setItems] = useState<BusinessMemoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void loadSession().then(async (session) => {
      if (!active) return;
      if (!session?.businessId) { setLoading(false); return; }
      try {
        const value = await getBusinessMemory(session.accessToken, session.businessId);
        if (active) setItems(value);
      } catch {
        if (active) setError('Business Memory could not be loaded safely.');
      } finally {
        if (active) setLoading(false);
      }
    });
    return () => { active = false; };
  }, []);

  const remove = async (item: BusinessMemoryItem) => {
    if (!item.isDeletable) return;
    setBusyId(item.id);
    setError(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) throw new Error('Business unavailable');
      await deleteBusinessMemory(session.accessToken, session.businessId, item.id);
      setItems((current) => current.filter((value) => value.id !== item.id));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Memory item could not be deleted.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <View style={styles.section}>
      <Text accessibilityRole="header" style={styles.title}>Business Memory</Text>
      <Text style={styles.body}>Atlas shows the structured product-relevant memory it retains. Outcome-derived items can be removed without rewriting historical Actions or Opportunities.</Text>
      {loading ? <Text accessibilityLiveRegion="polite">Loading Business Memory…</Text> : null}
      {!loading && items.length === 0 ? <Text style={styles.body}>No outcome-derived Business Memory has been recorded yet.</Text> : null}
      {items.map((item) => (
        <View key={item.id} style={styles.card}>
          <Text style={styles.label}>{item.category}</Text>
          <Text style={styles.body}>{item.value}</Text>
          <Text style={styles.supporting}>Source: {item.sourceType} · Updated {new Date(item.updatedAt).toLocaleString()}</Text>
          {item.isDeletable ? <Pressable accessibilityRole="button" disabled={busyId === item.id} onPress={() => void remove(item)} style={styles.button}><Text style={styles.buttonText}>{busyId === item.id ? 'Removing…' : 'Remove from Business Memory'}</Text></Pressable> : <Text style={styles.supporting}>Required Business record</Text>}
        </View>
      ))}
      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  section: { gap: tokens.spacing.md },
  title: { fontSize: 22, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  label: { fontSize: 14, fontWeight: '700', textTransform: 'capitalize' },
  supporting: { fontSize: 14, lineHeight: 20 },
  button: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { fontWeight: '700' },
  error: { fontSize: 14, lineHeight: 20, fontWeight: '700' },
});
