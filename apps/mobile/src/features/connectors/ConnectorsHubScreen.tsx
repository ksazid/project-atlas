import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import { getOperationalConnector } from '@/features/operational-data/operational-data-api';
import type { OperationalConnector } from '@/features/operational-data/operational-data-model';
import { tokens } from '@/theme/tokens';

type ConnectorItem = { id: string; name: string; description: string; mark: string; available: boolean };
const connectorItems: ConnectorItem[] = [
  { id: 'google-drive', name: 'Google Drive', description: 'Automatic CSV sync', mark: 'G', available: true },
  { id: 'bolt-food', name: 'Bolt Food', description: 'Orders and performance', mark: 'B', available: false },
  { id: 'wolt', name: 'Wolt', description: 'Orders and performance', mark: 'W', available: false },
  { id: 'meta-ads', name: 'Meta Ads Manager', description: 'Ad performance data', mark: 'M', available: false },
  { id: 'instagram', name: 'Instagram', description: 'Profile and campaign signals', mark: 'I', available: false },
];

export function ConnectorsHubScreen() {
  const router = useRouter();
  const [connector, setConnector] = useState<OperationalConnector>({ state: 'disconnected', schedule: 'daily' });
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const session = await loadSession();
      if (session?.businessId) setConnector(await getOperationalConnector(session.accessToken, session.businessId));
    } catch { setConnector(current => ({ ...current, state: 'error' })); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { void load(); }, [load]);

  const filtered = useMemo(() => connectorItems.filter(item => `${item.name} ${item.description}`.toLowerCase().includes(query.trim().toLowerCase())), [query]);
  const hasSuccessfulSync = Boolean(connector.lastSuccessfulSyncAt);
  const driveConnected = connector.state !== 'disconnected' && connector.state !== 'reauthorization-required';
  const driveStatus = hasSuccessfulSync ? `Live · synced ${formatRelative(connector.lastSuccessfulSyncAt!)}` : driveConnected ? 'Connected · waiting for first successful sync' : 'Primary pilot connector';

  return (
    <AtlasScreen contentStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.content}>
        <View style={styles.topBar}>
          <AtlasPressable accessibilityRole="button" accessibilityLabel="Back to Profile" onPress={() => router.back()} style={styles.back}><Text style={styles.backText}>‹</Text></AtlasPressable>
          <Text accessibilityRole="header" style={styles.heading}>Connectors</Text>
          <View style={styles.topSpacer} />
        </View>
        <View style={styles.signalLine}><View style={[styles.signalDot, hasSuccessfulSync && styles.signalDotLive]} /><Text style={styles.signalText}>{hasSuccessfulSync ? 'Live business signals are feeding Atlas recommendations' : 'Connect business data to keep Atlas recommendations fresh'}</Text></View>

        <TextInput accessibilityLabel="Search connectors" onChangeText={setQuery} placeholder="Search connectors" placeholderTextColor={tokens.color.muted} style={styles.search} value={query} />

        <View style={styles.sectionHeader}>
          <Text style={styles.eyebrow}>LIVE DATA CONNECTORS</Text>
          <Text style={styles.sectionCopy}>Bring useful restaurant operating data into Atlas.</Text>
        </View>

        <View style={styles.list}>
          {filtered.map(item => {
            const isDrive = item.id === 'google-drive';
            const status = isDrive ? driveStatus : 'Planned for a future integration';
            return (
              <AtlasPressable key={item.id} accessibilityRole="button" accessibilityLabel={`${item.name} connector`} disabled={!item.available} onPress={() => isDrive && router.push('/business-data')} style={[styles.row, !item.available && styles.rowDisabled]}>
                <View style={styles.mark}><Text style={styles.markText}>{item.mark}</Text></View>
                <View style={styles.rowBody}>
                  <Text style={styles.rowTitle}>{item.name}</Text>
                  <Text style={styles.rowDescription}>{item.description}</Text>
                  <View style={styles.rowStatus}><View style={[styles.smallDot, isDrive && hasSuccessfulSync && styles.signalDotLive]} /><Text style={styles.rowStatusText}>{status}</Text></View>
                </View>
                {loading && isDrive ? <ActivityIndicator color={tokens.color.green} /> : <View style={[styles.badge, isDrive && driveConnected ? styles.badgeConnected : null]}><Text style={[styles.badgeText, isDrive && driveConnected ? styles.badgeTextConnected : null]}>{isDrive ? (driveConnected ? (hasSuccessfulSync ? 'Live' : 'Connected') : 'Connect') : 'Coming soon'}</Text></View>}
                {isDrive ? <Text style={styles.chevron}>›</Text> : null}
              </AtlasPressable>
            );
          })}
        </View>

        <View style={styles.manualCard}>
          <Text style={styles.eyebrow}>MANUAL DATA</Text>
          <Text style={styles.manualTitle}>Upload a CSV from this device</Text>
          <Text style={styles.sectionCopy}>Use the privacy-safe preview and confirmation flow when Drive is unavailable.</Text>
          <AtlasPressable accessibilityRole="button" accessibilityLabel="Open CSV upload" onPress={() => router.push('/business-data')} style={styles.manualButton}><Text style={styles.manualButtonText}>Upload CSV</Text></AtlasPressable>
        </View>
      </View>
    </AtlasScreen>
  );
}

function formatRelative(value: string): string {
  const time = new Date(value).getTime();
  if (Number.isNaN(time)) return 'recently';
  const minutes = Math.max(0, Math.round((Date.now() - time) / 60000));
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} hr ago`;
  return `${Math.round(hours / 24)} d ago`;
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: tokens.color.surface }, content: { gap: 18, maxWidth: 680, width: '100%' },
  topBar: { alignItems: 'center', flexDirection: 'row', justifyContent: 'space-between' }, back: { alignItems: 'center', borderColor: tokens.color.border, borderRadius: tokens.radius.pill, borderWidth: 1, height: 44, justifyContent: 'center', width: 44 }, backText: { color: tokens.color.green, fontSize: 30, lineHeight: 32 }, topSpacer: { width: 44 }, heading: { color: tokens.color.ink, fontSize: 22, fontWeight: '800' },
  signalLine: { alignItems: 'center', flexDirection: 'row', gap: 8, justifyContent: 'center' }, signalDot: { backgroundColor: tokens.color.border, borderRadius: 99, height: 9, width: 9 }, signalDotLive: { backgroundColor: tokens.color.greenBright }, signalText: { color: tokens.color.muted, flexShrink: 1, fontSize: 12.5, lineHeight: 18 },
  search: { backgroundColor: tokens.color.canvas, borderColor: tokens.color.border, borderRadius: tokens.radius.pill, borderWidth: 1, color: tokens.color.ink, fontSize: 14, minHeight: 52, paddingHorizontal: 18 },
  sectionHeader: { gap: 3 }, eyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 }, sectionCopy: { color: tokens.color.muted, fontSize: 13, lineHeight: 19 },
  list: { gap: 9 }, row: { alignItems: 'center', backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, flexDirection: 'row', gap: 11, minHeight: 86, padding: 13 }, rowDisabled: { opacity: .72 }, mark: { alignItems: 'center', backgroundColor: tokens.color.canvas, borderColor: tokens.color.border, borderRadius: 11, borderWidth: 1, height: 46, justifyContent: 'center', width: 46 }, markText: { color: tokens.color.green, fontSize: 18, fontWeight: '900' }, rowBody: { flex: 1, gap: 2 }, rowTitle: { color: tokens.color.ink, fontSize: 15, fontWeight: '800' }, rowDescription: { color: tokens.color.muted, fontSize: 12.5 }, rowStatus: { alignItems: 'center', flexDirection: 'row', gap: 5, marginTop: 4 }, smallDot: { backgroundColor: tokens.color.border, borderRadius: 99, height: 7, width: 7 }, rowStatusText: { color: tokens.color.muted, flexShrink: 1, fontSize: 10.5 },
  badge: { borderColor: tokens.color.border, borderRadius: tokens.radius.pill, borderWidth: 1, paddingHorizontal: 10, paddingVertical: 7 }, badgeConnected: { backgroundColor: tokens.color.mint, borderColor: tokens.color.mint }, badgeText: { color: tokens.color.muted, fontSize: 10.5, fontWeight: '800' }, badgeTextConnected: { color: tokens.color.green }, chevron: { color: tokens.color.muted, fontSize: 24 },
  manualCard: { backgroundColor: tokens.color.canvas, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 7, padding: 18 }, manualTitle: { color: tokens.color.ink, fontSize: 17, fontWeight: '800' }, manualButton: { alignItems: 'center', alignSelf: 'flex-start', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1.5, justifyContent: 'center', marginTop: 5, minHeight: 44, paddingHorizontal: 18 }, manualButtonText: { color: tokens.color.green, fontSize: 13, fontWeight: '900' },
});
