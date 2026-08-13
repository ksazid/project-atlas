import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import { BrandMark } from '@/components/BrandMark';
import { connectOperationalFolder, getOperationalConnector, setOperationalSchedule, syncOperationalFolder } from './operational-data-api';
import { extractGoogleDriveFolderId, operationalScheduleChoices, presentConnector, type OperationalConnector, type OperationalSchedule } from './operational-data-model';
import { tokens } from '@/theme/tokens';

export function OperationalDataScreen() {
  const router = useRouter();
  const [connector, setConnector] = useState<OperationalConnector>({ state: 'disconnected', schedule: 'daily' });
  const [loading, setLoading] = useState(true);
  const [folderUrl, setFolderUrl] = useState('');
  const [validationMessage, setValidationMessage] = useState<string | null>(null);
  const presentation = presentConnector(connector);
  const needsFolder = connector.state === 'disconnected' || connector.state === 'reauthorization-required';

  const load = useCallback(async () => {
    try {
      const session = await loadSession();
      if (session?.businessId) setConnector(await getOperationalConnector(session.accessToken, session.businessId));
    } catch { setConnector({ state: 'error', message: 'Your saved connection is unchanged. Try again.' }); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { void load(); }, [load]);

  const connectNow = async () => {
    const session = await loadSession();
    if (!session?.businessId) return;
    const folderId = extractGoogleDriveFolderId(folderUrl);
    if (!folderId) {
      setValidationMessage('Paste a valid private Google Drive folder link.');
      return;
    }
    setValidationMessage(null);
    setLoading(true);
    try {
      setConnector(await connectOperationalFolder(session.accessToken, session.businessId, folderId, connector.schedule ?? 'daily'));
      setFolderUrl('');
    } catch { setConnector(current => ({ ...current, state: 'error', message: 'Atlas could not connect that folder. Check Viewer access and try again.' })); }
    finally { setLoading(false); }
  };

  const syncNow = async () => {
    const session = await loadSession();
    if (!session?.businessId || connector.state === 'syncing') return;
    setConnector(current => ({ ...current, state: 'syncing' }));
    try {
      await syncOperationalFolder(session.accessToken, session.businessId);
      await load();
    } catch { setConnector(current => ({ ...current, state: 'error', message: 'The latest sync did not finish. Your saved connection is unchanged.' })); }
  };

  const updateSchedule = async (schedule: OperationalSchedule) => {
    const session = await loadSession();
    if (!session?.businessId || connector.schedule === schedule) return;
    try { setConnector(await setOperationalSchedule(session.accessToken, session.businessId, schedule)); }
    catch { setConnector(current => ({ ...current, state: 'error', message: 'Atlas could not update the sync schedule.' })); }
  };

  const primaryAction = needsFolder ? connectNow : syncNow;

  return (
    <AtlasScreen contentStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.content}>
        <AtlasPressable accessibilityRole="button" accessibilityLabel="Back to Profile" onPress={() => router.back()} style={styles.back}><Text style={styles.backText}>‹ Profile</Text></AtlasPressable>
        <BrandMark size={44} />
        <Text style={styles.eyebrow}>BUSINESS DATA</Text>
        <Text accessibilityRole="header" style={styles.heading}>Give Atlas a clearer view of what changed.</Text>
        <Text style={styles.subheading}>Use one dedicated Google Drive folder for your POS CSV exports. Scheduled sync turns them into privacy-safe signals for better recommendations.</Text>

        <View style={styles.card}>
          <Text style={styles.cardLabel}>GOOGLE DRIVE · PRIMARY</Text>
          <Text style={styles.cardTitle}>{presentation.title}</Text>
          {loading ? <ActivityIndicator color={tokens.color.green} /> : null}
          {needsFolder ? <TextInput accessibilityLabel="Google Drive folder link" autoCapitalize="none" keyboardType="url" onChangeText={value => { setFolderUrl(value); setValidationMessage(null); }} placeholder="Paste the private Atlas folder link" style={styles.input} value={folderUrl} /> : null}
          {validationMessage ? <Text accessibilityRole="alert" style={styles.warningText}>{validationMessage}</Text> : null}
          {connector.message ? <Text accessibilityRole="alert" style={styles.warningText}>{connector.message}</Text> : null}
          <Text style={styles.privacy}>Share the folder with Atlas as Viewer—never “Anyone with the link.” Your raw CSV stays in Google Drive. Atlas excludes customer-identifying fields and stores only normalized signals.</Text>
          <AtlasPressable accessibilityRole="button" accessibilityLabel={presentation.primaryAction} disabled={loading || connector.state === 'syncing'} onPress={() => void primaryAction()} style={styles.primaryButton}>
            <Text style={styles.primaryText}>{presentation.primaryAction}</Text>
          </AtlasPressable>

          {!needsFolder ? <View style={styles.scheduleGroup}>
            <Text style={styles.scheduleLabel}>SYNC SCHEDULE</Text>
            <View style={styles.scheduleRow}>
              {operationalScheduleChoices.map(choice => {
                const selected = connector.schedule === choice.value;
                return <AtlasPressable key={choice.value} accessibilityRole="button" accessibilityLabel={`Sync ${choice.label}`} accessibilityState={{ selected }} onPress={() => void updateSchedule(choice.value)} style={[styles.scheduleChoice, selected ? styles.scheduleChoiceSelected : null]}>
                  <Text style={[styles.scheduleChoiceText, selected ? styles.scheduleChoiceTextSelected : null]}>{choice.label}</Text>
                </AtlasPressable>;
              })}
            </View>
          </View> : null}
        </View>

        <View style={styles.fallbackCard}>
          <Text style={styles.cardLabel}>FALLBACK</Text>
          <Text style={styles.fallbackTitle}>Upload CSV from this device</Text>
          <Text style={styles.privacy}>Use this when Drive is unavailable. Atlas applies the same preview, privacy and raw-file non-retention rules.</Text>
          <AtlasPressable accessibilityRole="button" accessibilityLabel="Upload CSV from this device" onPress={() => {}} style={styles.secondaryButton}><Text style={styles.secondaryText}>Choose CSV file</Text></AtlasPressable>
        </View>
      </View>
    </AtlasScreen>
  );
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: tokens.color.surface }, content: { gap: 14, maxWidth: 680, width: '100%' },
  back: { alignSelf: 'flex-start', justifyContent: 'center', minHeight: 44 }, backText: { color: tokens.color.green, fontSize: 14, fontWeight: '800' },
  eyebrow: { color: tokens.color.green, fontSize: 11, fontWeight: '900', letterSpacing: 1.2 }, heading: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 30, fontWeight: '800', lineHeight: 36 }, subheading: { color: tokens.color.muted, fontSize: 14.5, lineHeight: 22 },
  card: { backgroundColor: tokens.color.greenDeep, borderRadius: tokens.radius.lg, gap: 12, padding: 20 }, fallbackCard: { backgroundColor: tokens.color.canvas, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: 10, padding: 20 },
  cardLabel: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 }, cardTitle: { color: tokens.color.surface, fontFamily: 'Georgia', fontSize: 23, fontWeight: '800' }, fallbackTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 21, fontWeight: '800' }, privacy: { color: tokens.color.muted, fontSize: 12.5, lineHeight: 19 }, warningText: { color: tokens.color.surface, fontSize: 12.5, fontWeight: '700', lineHeight: 18 },
  input: { backgroundColor: tokens.color.surface, borderRadius: tokens.radius.md, color: tokens.color.greenDeep, minHeight: 50, paddingHorizontal: 14 },
  primaryButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 50, paddingHorizontal: 18 }, primaryText: { color: tokens.color.surface, fontSize: 14, fontWeight: '900' },
  secondaryButton: { alignItems: 'center', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1.5, justifyContent: 'center', minHeight: 48, paddingHorizontal: 18 }, secondaryText: { color: tokens.color.green, fontSize: 14, fontWeight: '900' },
  scheduleGroup: { gap: 8 }, scheduleLabel: { color: tokens.color.muted, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 }, scheduleRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 }, scheduleChoice: { borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1, minHeight: 44, justifyContent: 'center', paddingHorizontal: 12 }, scheduleChoiceSelected: { backgroundColor: tokens.color.green }, scheduleChoiceText: { color: tokens.color.green, fontSize: 12, fontWeight: '800' }, scheduleChoiceTextSelected: { color: tokens.color.surface },
});
