import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { getProfile, saveProfile, type BusinessProfile } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { canSaveProfile, createEmptyProfile, getProfileConfirmationState, getProfileSavePresentation, getProfileStatePresentation, profileSections, resolveProfileFailure, type ProfileField, type ProfileScreenState } from '@/features/profile/profile-model';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'missing' | 'error';

export default function EditBusinessScreen() {
  const router = useRouter();
  const [form, setForm] = useState<BusinessProfile>(createEmptyProfile);
  const [state, setState] = useState<ScreenState>('loading');
  const [saving, setSaving] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const savingRef = useRef(false);

  const load = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    else setState('loading');
    setMessage(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) {
        setState('missing');
        return;
      }
      setForm((await getProfile(session.accessToken, session.businessId)) ?? createEmptyProfile());
      setState('ready');
    } catch {
      if (manual) {
        const failure = resolveProfileFailure('refresh');
        setState(failure.state);
        setMessage(failure.message);
      } else {
        setState('error');
      }
    } finally {
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const submit = async () => {
    if (savingRef.current || !canSaveProfile(form, saving)) return;
    savingRef.current = true;
    setSaving(true);
    setMessage(null);
    try {
      const session = await loadSession();
      if (!session?.businessId) throw new Error('Business session is missing.');
      setForm(await saveProfile(session.accessToken, session.businessId, form));
      setMessage('Profile saved.');
    } catch {
      const failure = resolveProfileFailure('save');
      setState(failure.state);
      setMessage(failure.message);
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  };

  if (state !== 'ready') return <ProfileState state={state} retry={load} continueSession={() => router.replace({ pathname: '/', params: { sessionEntry: '1' } })} />;

  const saveEnabled = canSaveProfile(form, saving);
  const savePresentation = getProfileSavePresentation(saving, saveEnabled);
  const confirmationState = getProfileConfirmationState(form.ownerConfirmed);
  return (
    <ScrollView
      automaticallyAdjustKeyboardInsets
      contentContainerStyle={styles.container}
      keyboardDismissMode="on-drag"
      keyboardShouldPersistTaps="handled"
      refreshControl={<RefreshControl refreshing={refreshing} tintColor={tokens.color.green} onRefresh={() => void load(true)} />}
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.content}>
        <View style={styles.header}>
          <BrandMark size={50} />
          <Text style={styles.eyebrow}>EDIT BUSINESS DETAILS</Text>
          <Text accessibilityRole="header" style={styles.title}>Keep the details Atlas uses accurate.</Text>
          <Text style={styles.help}>Update the business information customers rely on and Atlas uses for grounded guidance.</Text>
        </View>

        <View style={styles.provenanceCard}>
          <Text style={styles.cardEyebrow}>PROFILE PROVENANCE</Text>
          <Text style={styles.provenanceTitle}>{form.source === 'public' ? 'Public business information' : 'Owner-provided information'}</Text>
          <Text style={styles.provenanceCopy}>{form.source === 'public' ? 'We started with public information. Confirm it is accurate before saving.' : 'These details are managed by you and shape more relevant guidance.'}</Text>
          {form.source === 'public' ? (
            <Pressable
              aria-checked={confirmationState.ariaChecked}
              accessibilityLabel="Confirm profile information"
              accessibilityRole="checkbox"
              accessibilityState={confirmationState.accessibilityState}
              disabled={saving}
              onPress={() => setForm(current => ({ ...current, ownerConfirmed: !current.ownerConfirmed }))}
              style={({ pressed }) => [styles.confirmation, pressed && !saving && styles.pressed, saving && styles.disabled]}
            >
              <View style={[styles.checkbox, form.ownerConfirmed && styles.checkboxChecked]}><Text style={styles.checkmark}>{form.ownerConfirmed ? '✓' : ''}</Text></View>
              <Text style={styles.confirmationText}>I confirm this information is accurate.</Text>
            </Pressable>
          ) : null}
        </View>

        {profileSections.map(section => (
          <View key={section.title} style={styles.section}>
            <Text style={styles.sectionTitle}>{section.title}</Text>
            <View style={styles.sectionCard}>
              {section.fields.map((field: ProfileField) => (
                <View key={field.key} style={styles.field}>
                  <Text style={styles.label}>{field.label}</Text>
                  <TextInput
                    accessibilityLabel={`${field.label} input`}
                    accessibilityHint={field.hint}
                    autoCapitalize={field.keyboard === 'email-address' || field.keyboard === 'url' ? 'none' : 'sentences'}
                    autoCorrect={field.keyboard !== 'email-address' && field.keyboard !== 'url'}
                    keyboardType={field.keyboard ?? 'default'}
                    multiline={field.multiline}
                    onChangeText={value => setForm(current => ({ ...current, [field.key]: value }))}
                    placeholder={field.hint}
                    placeholderTextColor={tokens.color.muted}
                    style={[styles.input, field.multiline && styles.multilineInput]}
                    value={form[field.key]}
                  />
                </View>
              ))}
            </View>
          </View>
        ))}

        {message ? <Text accessibilityLiveRegion="polite" style={[styles.message, message === 'Profile saved.' ? styles.successMessage : styles.errorMessage]}>{message}</Text> : null}
        <Pressable aria-busy={savePresentation.ariaBusy} accessibilityLabel={savePresentation.accessibilityLabel} accessibilityRole="button" accessibilityState={savePresentation.accessibilityState} disabled={!saveEnabled} onPress={() => void submit()} style={({ pressed }) => [styles.button, pressed && saveEnabled && styles.pressed, !saveEnabled && styles.disabled]}>
          {saving ? <ActivityIndicator color={tokens.color.surface} /> : null}<Text style={styles.buttonText}>{savePresentation.text}</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function ProfileState({ state, retry, continueSession }: { state: ProfileScreenState; retry: () => Promise<void>; continueSession: () => void }) {
  const content = getProfileStatePresentation(state);

  return <View style={styles.stateScreen}><View style={styles.stateCard}><BrandMark size={50} /><Text accessibilityRole="header" style={styles.stateTitle}>{content.title}</Text><Text style={styles.stateCopy}>{content.copy}</Text>{state === 'loading' ? <ActivityIndicator color={tokens.color.green} /> : null}{state === 'missing' && content.action ? <Pressable accessibilityLabel={content.action.accessibilityLabel} accessibilityRole="button" onPress={continueSession} style={({ pressed }) => [styles.retryButton, pressed && styles.pressed]}><Text style={styles.retryText}>{content.action.label}</Text></Pressable> : null}{state === 'error' ? <Pressable accessibilityLabel="Try loading profile again" accessibilityRole="button" onPress={() => void retry()} style={({ pressed }) => [styles.retryButton, pressed && styles.pressed]}><Text style={styles.retryText}>Try again</Text></Pressable> : null}</View></View>;
}

const styles = StyleSheet.create({
  container: { alignItems: 'center', backgroundColor: '#FFF', flexGrow: 1, paddingHorizontal: 28, paddingTop: 58, paddingBottom: 38 },
  content: { gap: 22, maxWidth: 680, width: '100%' },
  header: { gap: 8 },
  eyebrow: { color: '#00754A', fontSize: 11, fontWeight: '900', letterSpacing: 1.15, marginTop: 10 },
  title: { color: '#0A2F25', fontFamily: 'Georgia', fontSize: 32, fontWeight: '800', letterSpacing: -0.5, lineHeight: 38 },
  help: { color: '#5B6761', fontSize: 14.5, lineHeight: 22 },
  provenanceCard: { backgroundColor: '#EEF8F2', borderColor: '#DDE8E1', borderRadius: 12, borderWidth: 1, gap: 8, padding: 16 },
  cardEyebrow: { color: '#00754A', fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  provenanceTitle: { color: '#17221C', fontSize: 17, fontWeight: '800', lineHeight: 23 },
  provenanceCopy: { color: '#5B6761', fontSize: 13.5, lineHeight: 20 },
  confirmation: { alignItems: 'center', flexDirection: 'row', flexWrap: 'wrap', gap: 8, minHeight: 44, paddingVertical: 4 },
  checkbox: { alignItems: 'center', backgroundColor: '#FFF', borderColor: '#83908A', borderRadius: 8, borderWidth: 1, height: 24, justifyContent: 'center', width: 24 },
  checkboxChecked: { backgroundColor: '#00754A', borderColor: '#00754A' },
  checkmark: { color: '#FFF', fontSize: 16, fontWeight: '800', lineHeight: 20 },
  confirmationText: { color: '#17221C', flexShrink: 1, fontSize: 13.5, fontWeight: '700', lineHeight: 20 },
  section: { gap: 8 },
  sectionTitle: { color: '#5B6761', fontSize: 10.5, fontWeight: '900', letterSpacing: 1.05, paddingHorizontal: 2 },
  sectionCard: { backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 16, padding: 16, shadowColor: '#173B2A', shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.025, shadowRadius: 6, elevation: 1 },
  field: { gap: 8 },
  label: { color: '#1C2924', fontSize: 13, fontWeight: '800', lineHeight: 19 },
  input: { backgroundColor: '#FFF', borderColor: '#DEE5E1', borderRadius: 10, borderWidth: 1, color: '#22312C', fontSize: 14, lineHeight: 21, minHeight: 55, paddingHorizontal: 14, paddingVertical: 11 },
  multilineInput: { minHeight: 104, textAlignVertical: 'top' },
  message: { borderRadius: 10, fontSize: 13.5, lineHeight: 20, padding: 14 },
  successMessage: { backgroundColor: '#EEF8F2', color: '#0A2F25' },
  errorMessage: { backgroundColor: '#FDECEC', color: '#A1251B' },
  button: { alignItems: 'center', backgroundColor: '#008A57', borderRadius: 10, justifyContent: 'center', minHeight: 55, paddingHorizontal: 24, shadowColor: '#00633F', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.12, shadowRadius: 8, elevation: 2 },
  buttonText: { color: '#FFF', fontSize: 15.5, fontWeight: '800' },
  pressed: { opacity: 0.92, transform: [{ scale: .99 }] },
  disabled: { opacity: 0.5 },
  stateScreen: { alignItems: 'center', backgroundColor: '#FFF', flex: 1, justifyContent: 'center', padding: 28 },
  stateCard: { alignItems: 'flex-start', backgroundColor: '#FFF', borderColor: '#E2E7E4', borderRadius: 12, borderWidth: 1, gap: 16, maxWidth: 440, padding: 24, width: '100%', shadowColor: '#173B2A', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.03, shadowRadius: 8, elevation: 1 },
  stateTitle: { color: '#0A2F25', fontFamily: 'Georgia', fontSize: 28, fontWeight: '800', lineHeight: 34 },
  stateCopy: { color: '#5B6761', fontSize: 14.5, lineHeight: 22 },
  retryButton: { alignItems: 'center', backgroundColor: '#008A57', borderRadius: 10, justifyContent: 'center', minHeight: 50, paddingHorizontal: 18 },
  retryText: { color: '#FFF', fontSize: 14, fontWeight: '800' }
});
