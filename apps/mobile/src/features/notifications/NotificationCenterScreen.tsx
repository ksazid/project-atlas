import { useCallback, useEffect, useState } from 'react';
import { Pressable, RefreshControl, StyleSheet, Switch, Text, View } from 'react-native';
import { router } from 'expo-router';
import { getNotifications, markAllNotificationsRead, markNotificationRead, saveNotificationPreferences, type NotificationCenter, type NotificationItem } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { AtlasScreen } from '@/components/AtlasScreen';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'empty' | 'error';

export function NotificationCenterScreen() {
  const [center, setCenter] = useState<NotificationCenter | null>(null);
  const [state, setState] = useState<ScreenState>('loading');
  const [refreshing, setRefreshing] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    try {
      const session = await loadSession();
      if (!session?.businessId) { setCenter(null); setState('empty'); return; }
      const value = await getNotifications(session.accessToken, session.businessId);
      setCenter(value);
      setState(value.items.length === 0 ? 'empty' : 'ready');
    } catch {
      setState('error');
    } finally {
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    const initialLoad = setTimeout(() => { void load(); }, 0);
    return () => clearTimeout(initialLoad);
  }, [load]);

  const openNotification = useCallback(async (item: NotificationItem) => {
    const session = await loadSession();
    if (!session?.businessId) return;
    try {
      if (!item.readAt) {
        const updated = await markNotificationRead(session.accessToken, session.businessId, item);
        setCenter((current) => current ? {
          ...current,
          items: current.items.map((value) => value.id === updated.id ? updated : value),
          unreadCount: Math.max(0, current.unreadCount - 1),
        } : current);
      }
    } finally {
      if (item.deepLink) router.push(item.deepLink as never);
    }
  }, []);

  const readAll = useCallback(async () => {
    const session = await loadSession();
    if (!session?.businessId) return;
    const value = await markAllNotificationsRead(session.accessToken, session.businessId);
    setCenter(value);
    setState(value.items.length === 0 ? 'empty' : 'ready');
  }, []);

  const updatePreference = useCallback(async (key: 'todayFocusEnabled' | 'outcomeFollowUpEnabled' | 'weeklyReviewEnabled', value: boolean) => {
    if (!center || saving) return;
    const session = await loadSession();
    if (!session?.businessId) return;
    setSaving(true);
    try {
      const next = { ...center.preferences, [key]: value };
      const saved = await saveNotificationPreferences(session.accessToken, session.businessId, next);
      setCenter((current) => current ? { ...current, preferences: saved } : current);
    } finally {
      setSaving(false);
    }
  }, [center, saving]);

  return (
    <AtlasScreen contentStyle={styles.container} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void load(true)} />}>
      <View style={styles.headerRow}>
        <View style={styles.headerText}>
          <Text style={styles.eyebrow}>NOTIFICATIONS</Text>
          <Text accessibilityRole="header" style={styles.title}>Updates that need your attention</Text>
          {center ? <Text style={styles.supporting}>{center.unreadCount} unread</Text> : null}
        </View>
        <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.outlineButton}><Text style={styles.buttonText}>Back</Text></Pressable>
      </View>

      {state === 'loading' ? <Text accessibilityLiveRegion="polite">Loading notifications…</Text> : null}
      {state === 'error' ? <View style={styles.card}><Text style={styles.cardTitle}>Notifications unavailable</Text><Text style={styles.body}>Atlas could not safely load notification records. Nothing was changed.</Text><Pressable accessibilityRole="button" onPress={() => void load()} style={styles.primaryButton}><Text style={styles.primaryText}>Try again</Text></Pressable></View> : null}

      {center ? <View style={styles.card}>
        <Text style={styles.cardTitle}>Notification preferences</Text>
        <PreferenceRow label="Today’s Focus" value={center.preferences.todayFocusEnabled} disabled={saving} onChange={(value) => void updatePreference('todayFocusEnabled', value)} />
        <PreferenceRow label="Outcome follow-ups" value={center.preferences.outcomeFollowUpEnabled} disabled={saving} onChange={(value) => void updatePreference('outcomeFollowUpEnabled', value)} />
        <PreferenceRow label="Weekly Review" value={center.preferences.weeklyReviewEnabled} disabled={saving} onChange={(value) => void updatePreference('weeklyReviewEnabled', value)} />
        <Text style={styles.supporting}>These settings control Atlas in-app notifications only. External push, SMS and email delivery are not enabled.</Text>
      </View> : null}

      {center && center.unreadCount > 0 ? <Pressable accessibilityRole="button" onPress={() => void readAll()} style={styles.outlineButton}><Text style={styles.buttonText}>Mark all as read</Text></Pressable> : null}

      {state === 'empty' ? <View style={styles.card}><Text style={styles.cardTitle}>No notifications yet</Text><Text style={styles.body}>Atlas will show Today’s Focus, due Outcome follow-ups and Weekly Review updates here when relevant.</Text></View> : null}

      {state === 'ready' && center ? center.items.map((item) => <Pressable key={item.id} accessibilityRole="button" onPress={() => void openNotification(item)} style={[styles.card, !item.readAt && styles.unreadCard]}>
        <View style={styles.itemHeader}><Text style={styles.category}>{item.category.replaceAll('-', ' ').toUpperCase()}</Text><Text style={styles.supporting}>{new Date(item.createdAt).toLocaleString()}</Text></View>
        <Text style={styles.itemTitle}>{item.title}</Text>
        <Text style={styles.body}>{item.body}</Text>
        <Text style={styles.supporting}>{item.readAt ? 'Read' : 'Unread'}{item.deepLink ? ' · Open details' : ''}</Text>
      </Pressable>) : null}
    </AtlasScreen>
  );
}

function PreferenceRow({ label, value, disabled, onChange }: { label: string; value: boolean; disabled: boolean; onChange: (value: boolean) => void }) {
  return <View style={styles.preferenceRow}><Text style={styles.body}>{label}</Text><Switch accessibilityLabel={`${label} notifications`} value={value} disabled={disabled} onValueChange={onChange} /></View>;
}

const styles = StyleSheet.create({
  container: { gap: tokens.spacing.md },
  headerRow: { flexDirection: 'row', alignItems: 'flex-start', gap: tokens.spacing.sm },
  headerText: { flex: 1, gap: 4 },
  eyebrow: { fontSize: 13, fontWeight: '700', letterSpacing: 1.2 },
  title: { fontSize: tokens.typography.title, fontWeight: '700' },
  body: { fontSize: tokens.typography.body, lineHeight: 24 },
  supporting: { fontSize: 14, lineHeight: 20 },
  card: { borderWidth: 1, borderRadius: tokens.radius.md, padding: tokens.spacing.md, gap: tokens.spacing.sm },
  unreadCard: { borderWidth: 2 },
  cardTitle: { fontSize: 19, fontWeight: '700' },
  itemHeader: { flexDirection: 'row', justifyContent: 'space-between', gap: tokens.spacing.sm },
  category: { fontSize: 12, fontWeight: '800', letterSpacing: 0.8 },
  itemTitle: { fontSize: 17, fontWeight: '700' },
  preferenceRow: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: tokens.spacing.md },
  primaryButton: { minHeight: 48, borderRadius: tokens.radius.md, backgroundColor: '#111827', alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  primaryText: { color: '#fff', fontWeight: '700' },
  outlineButton: { minHeight: 44, borderWidth: 1, borderRadius: tokens.radius.md, alignItems: 'center', justifyContent: 'center', paddingHorizontal: tokens.spacing.md },
  buttonText: { fontWeight: '700' },
});
