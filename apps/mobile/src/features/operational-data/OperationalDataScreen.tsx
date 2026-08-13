import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TextInput, View } from 'react-native';
import { File } from 'expo-file-system';
import { useRouter } from 'expo-router';
import { loadSession } from '@/auth/session';
import { AtlasPressable } from '@/components/AtlasPressable';
import { AtlasScreen } from '@/components/AtlasScreen';
import { BrandMark } from '@/components/BrandMark';
import {
  confirmOperationalUpload,
  connectOperationalFolder,
  getOperationalConnector,
  previewOperationalUpload,
  setOperationalSchedule,
  syncOperationalFolder,
  type OperationalUploadAsset,
  type OperationalUploadPreview,
  type OperationalUploadResult,
} from './operational-data-api';
import { extractGoogleDriveFolderId, operationalScheduleChoices, presentConnector, type OperationalConnector, type OperationalSchedule } from './operational-data-model';
import { tokens } from '@/theme/tokens';

const maximumCsvBytes = 10 * 1024 * 1024;

export function OperationalDataScreen() {
  const router = useRouter();
  const [connector, setConnector] = useState<OperationalConnector>({ state: 'disconnected', schedule: 'daily' });
  const [loading, setLoading] = useState(true);
  const [folderUrl, setFolderUrl] = useState('');
  const [validationMessage, setValidationMessage] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [selectedCsv, setSelectedCsv] = useState<OperationalUploadAsset | null>(null);
  const [uploadPreview, setUploadPreview] = useState<OperationalUploadPreview | null>(null);
  const [uploadResult, setUploadResult] = useState<OperationalUploadResult | null>(null);
  const [uploadMessage, setUploadMessage] = useState<string | null>(null);
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

  const chooseCsv = async () => {
    try {
      const picked = await File.pickFileAsync(undefined, 'text/csv');
      const file = Array.isArray(picked) ? picked[0] : picked;
      if (!file) return;
      const fileName = decodeURIComponent(file.uri.split('/').pop() ?? 'business-data.csv');
      if (!fileName.toLowerCase().endsWith('.csv')) {
        setUploadMessage('Choose a CSV file.');
        return;
      }
      if (file.size > maximumCsvBytes) {
        setUploadMessage('CSV files must be 10 MiB or smaller.');
        return;
      }
      const session = await loadSession();
      if (!session?.businessId) return;
      const asset: OperationalUploadAsset = { uri: file.uri, name: fileName, file };
      setSelectedCsv(asset);
      setUploadPreview(null);
      setUploadResult(null);
      setUploadMessage(null);
      setUploading(true);
      try { setUploadPreview(await previewOperationalUpload(session.accessToken, session.businessId, asset)); }
      catch {
        setSelectedCsv(null);
        setUploadMessage('Atlas could not preview that CSV. Check the file format and try again.');
      } finally { setUploading(false); }
    } catch {
      setUploadMessage(null);
    }
  };

  const confirmCsv = async () => {
    const session = await loadSession();
    if (!session?.businessId || !selectedCsv || !uploadPreview || uploading) return;
    setUploading(true);
    setUploadMessage(null);
    try {
      const result = await confirmOperationalUpload(session.accessToken, session.businessId, selectedCsv, uploadPreview.previewFingerprint);
      setUploadResult(result);
      setSelectedCsv(null);
      setUploadPreview(null);
    } catch { setUploadMessage('Atlas could not import that CSV. Preview it again before confirming.'); }
    finally { setUploading(false); }
  };

  const chooseAnotherCsv = () => {
    setSelectedCsv(null);
    setUploadPreview(null);
    setUploadResult(null);
    setUploadMessage(null);
    void chooseCsv();
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
          <Text style={styles.privacy}>Use this when Drive is unavailable. Atlas previews the file first, ignores recognized customer-identifying columns, and never durably stores the raw CSV.</Text>
          {uploadMessage ? <Text accessibilityRole="alert" style={styles.fallbackError}>{uploadMessage}</Text> : null}
          {uploading ? <ActivityIndicator color={tokens.color.green} /> : null}

          {uploadPreview ? <View style={styles.previewCard}>
            <Text style={styles.previewTitle}>Preview before importing</Text>
            <Text style={styles.previewText}>{selectedCsv?.name}</Text>
            <Text style={styles.previewText}>{uploadPreview.rowCount} rows · {uploadPreview.orderCount} orders</Text>
            <Text style={styles.previewText}>{uploadPreview.earliestBusinessDate} → {uploadPreview.latestBusinessDate}</Text>
            <Text style={styles.previewLabel}>Recognized columns</Text>
            <Text style={styles.previewText}>{uploadPreview.recognizedColumns.join(', ') || 'None'}</Text>
            <Text style={styles.previewLabel}>Ignored sensitive columns</Text>
            <Text style={styles.previewText}>{uploadPreview.ignoredSensitiveColumns.join(', ') || 'None detected'}</Text>
            <Text style={styles.previewLabel}>Signals Atlas can derive</Text>
            <Text style={styles.previewText}>{uploadPreview.metricKeys.join(', ') || 'No supported metrics found'}</Text>
            <AtlasPressable accessibilityRole="button" accessibilityLabel="Confirm import" disabled={uploading} onPress={() => void confirmCsv()} style={styles.primaryFallbackButton}><Text style={styles.primaryFallbackText}>Confirm import</Text></AtlasPressable>
            <AtlasPressable accessibilityRole="button" accessibilityLabel="Choose another CSV" disabled={uploading} onPress={chooseAnotherCsv} style={styles.secondaryButton}><Text style={styles.secondaryText}>Choose another CSV</Text></AtlasPressable>
          </View> : null}

          {uploadResult ? <View style={styles.resultCard}>
            <Text style={styles.resultTitle}>{uploadResult.state === 'imported' ? 'CSV imported' : uploadResult.state === 'duplicate' ? 'Already up to date' : 'Overlapping data needs review'}</Text>
            <Text style={styles.previewText}>{uploadResult.createdSignals} signals · {uploadResult.createdChanges} changes · {uploadResult.freshness}</Text>
          </View> : null}

          {!uploadPreview ? <AtlasPressable accessibilityRole="button" accessibilityLabel="Upload CSV from this device" disabled={uploading} onPress={() => void chooseCsv()} style={styles.secondaryButton}><Text style={styles.secondaryText}>{uploadResult ? 'Choose another CSV' : 'Choose CSV file'}</Text></AtlasPressable> : null}
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
  cardLabel: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 }, cardTitle: { color: tokens.color.surface, fontFamily: 'Georgia', fontSize: 23, fontWeight: '800' }, fallbackTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 21, fontWeight: '800' }, privacy: { color: tokens.color.muted, fontSize: 12.5, lineHeight: 19 }, warningText: { color: tokens.color.surface, fontSize: 12.5, fontWeight: '700', lineHeight: 18 }, fallbackError: { color: tokens.color.greenDeep, fontSize: 12.5, fontWeight: '700', lineHeight: 18 },
  input: { backgroundColor: tokens.color.surface, borderRadius: tokens.radius.md, color: tokens.color.greenDeep, minHeight: 50, paddingHorizontal: 14 },
  primaryButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 50, paddingHorizontal: 18 }, primaryText: { color: tokens.color.surface, fontSize: 14, fontWeight: '900' },
  secondaryButton: { alignItems: 'center', borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1.5, justifyContent: 'center', minHeight: 48, paddingHorizontal: 18 }, secondaryText: { color: tokens.color.green, fontSize: 14, fontWeight: '900' },
  primaryFallbackButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.pill, justifyContent: 'center', minHeight: 48, paddingHorizontal: 18 }, primaryFallbackText: { color: tokens.color.surface, fontSize: 14, fontWeight: '900' },
  previewCard: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 8, padding: 14 }, previewTitle: { color: tokens.color.greenDeep, fontFamily: 'Georgia', fontSize: 18, fontWeight: '800' }, previewLabel: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 0.6, marginTop: 4 }, previewText: { color: tokens.color.muted, fontSize: 12.5, lineHeight: 18 },
  resultCard: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 5, padding: 14 }, resultTitle: { color: tokens.color.greenDeep, fontSize: 15, fontWeight: '900' },
  scheduleGroup: { gap: 8 }, scheduleLabel: { color: tokens.color.muted, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 }, scheduleRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 }, scheduleChoice: { borderColor: tokens.color.green, borderRadius: tokens.radius.pill, borderWidth: 1, minHeight: 44, justifyContent: 'center', paddingHorizontal: 12 }, scheduleChoiceSelected: { backgroundColor: tokens.color.green }, scheduleChoiceText: { color: tokens.color.green, fontSize: 12, fontWeight: '800' }, scheduleChoiceTextSelected: { color: tokens.color.surface },
});
