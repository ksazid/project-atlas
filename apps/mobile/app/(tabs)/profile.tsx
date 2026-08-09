import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { getProfile, saveProfile, type BusinessProfile } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { canSaveProfile, createEmptyProfile, getProfileConfirmationState, getProfileSavePresentation, getProfileStatePresentation, profileSections, resolveProfileFailure, type ProfileField, type ProfileScreenState } from '@/features/profile/profile-model';
import { tokens } from '@/theme/tokens';

type ScreenState = 'loading' | 'ready' | 'missing' | 'error';

export default function ProfileScreen() {
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
          <Text style={styles.eyebrow}>BUSINESS PROFILE</Text>
          <Text accessibilityRole="header" style={styles.title}>A profile that makes every recommendation fit.</Text>
          <Text style={styles.help}>Keep Atlas grounded in the details customers rely on every day.</Text>
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
  container: { alignItems: 'center', backgroundColor: tokens.color.canvas, flexGrow: 1, padding: tokens.spacing.lg },
  content: { gap: tokens.spacing.lg, maxWidth: 680, paddingBottom: tokens.spacing.xxl, width: '100%' },
  header: { gap: tokens.spacing.sm, paddingTop: tokens.spacing.sm },
  eyebrow: { color: tokens.color.green, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 1.2, marginTop: tokens.spacing.sm },
  title: { color: tokens.color.greenDeep, fontSize: tokens.typography.hero, fontWeight: '800', letterSpacing: -0.7, lineHeight: 41 },
  help: { color: tokens.color.muted, fontSize: tokens.typography.body, lineHeight: 24 },
  provenanceCard: { backgroundColor: tokens.color.mint, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: tokens.spacing.sm, padding: tokens.spacing.md },
  cardEyebrow: { color: tokens.color.greenDeep, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 1 },
  provenanceTitle: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 },
  provenanceCopy: { color: tokens.color.muted, fontSize: 14, lineHeight: 20 },
  confirmation: { alignItems: 'center', flexDirection: 'row', flexWrap: 'wrap', gap: tokens.spacing.sm, minHeight: tokens.touchTarget, paddingVertical: tokens.spacing.xs },
  checkbox: { alignItems: 'center', backgroundColor: tokens.color.surface, borderColor: tokens.color.muted, borderRadius: tokens.radius.sm, borderWidth: 1, height: 24, justifyContent: 'center', width: 24 },
  checkboxChecked: { backgroundColor: tokens.color.green, borderColor: tokens.color.green },
  checkmark: { color: tokens.color.surface, fontSize: 16, fontWeight: '800', lineHeight: 20 },
  confirmationText: { color: tokens.color.ink, flexShrink: 1, fontSize: 14, fontWeight: '700', lineHeight: 20 },
  section: { gap: tokens.spacing.sm },
  sectionTitle: { color: tokens.color.muted, fontSize: tokens.typography.caption, fontWeight: '800', letterSpacing: 1.1, paddingHorizontal: tokens.spacing.xs },
  sectionCard: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: tokens.spacing.md, padding: tokens.spacing.md, shadowColor: tokens.color.greenDeep, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.04, shadowRadius: 8, elevation: 1 },
  field: { gap: tokens.spacing.sm },
  label: { color: tokens.color.ink, fontSize: 14, fontWeight: '700', lineHeight: 20 },
  input: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, color: tokens.color.ink, fontSize: tokens.typography.body, lineHeight: 22, minHeight: tokens.touchTarget, paddingHorizontal: 12, paddingVertical: 10 },
  multilineInput: { minHeight: 104, textAlignVertical: 'top' },
  message: { borderRadius: tokens.radius.md, fontSize: 14, lineHeight: 20, padding: tokens.spacing.md },
  successMessage: { backgroundColor: tokens.color.mint, color: tokens.color.greenDeep },
  errorMessage: { backgroundColor: tokens.color.dangerSoft, color: tokens.color.danger },
  button: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.md, justifyContent: 'center', minHeight: 52, paddingHorizontal: tokens.spacing.lg },
  buttonText: { color: tokens.color.surface, fontSize: tokens.typography.body, fontWeight: '800' },
  pressed: { opacity: 0.86 },
  disabled: { opacity: 0.5 },
  stateScreen: { alignItems: 'center', backgroundColor: tokens.color.canvas, flex: 1, justifyContent: 'center', padding: tokens.spacing.lg },
  stateCard: { alignItems: 'flex-start', backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.lg, borderWidth: 1, gap: tokens.spacing.md, maxWidth: 440, padding: tokens.spacing.lg, width: '100%' },
  stateTitle: { color: tokens.color.greenDeep, fontSize: tokens.typography.title, fontWeight: '800', lineHeight: 34 },
  stateCopy: { color: tokens.color.muted, fontSize: tokens.typography.body, lineHeight: 24 },
  retryButton: { alignItems: 'center', backgroundColor: tokens.color.green, borderRadius: tokens.radius.md, justifyContent: 'center', minHeight: tokens.touchTarget, paddingHorizontal: tokens.spacing.md },
  retryText: { color: tokens.color.surface, fontSize: 14, fontWeight: '800' }
});
