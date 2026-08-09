import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Animated, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { createBusiness } from '@/api/atlas-client';
import { BusinessDiscovery, discoverBusiness } from '@/api/business-discovery';
import { loadSession, saveSession } from '@/auth/session';

type FormState = {
  name: string;
  category: string;
  country: string;
  timezone: string;
  currency: string;
  primaryLocation: string;
  operatingStatus: string;
};

const emptyForm: FormState = {
  name: '', category: '', country: '', timezone: '', currency: '', primaryLocation: '', operatingStatus: 'Open',
};

export default function CreateBusinessScreen() {
  const [stage, setStage] = useState<'discover' | 'confirm' | 'manual'>('discover');
  const [url, setUrl] = useState('');
  const [discovery, setDiscovery] = useState<BusinessDiscovery | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pulse = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(pulse, { toValue: 1, duration: 900, useNativeDriver: true }),
        Animated.timing(pulse, { toValue: 0, duration: 900, useNativeDriver: true }),
      ]),
    );
    if (busy) animation.start();
    return () => animation.stop();
  }, [busy, pulse]);

  function update(key: keyof FormState, value: string) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function analyse() {
    setBusy(true);
    setError(null);
    try {
      const session = await loadSession();
      if (!session) {
        router.replace('/sign-in');
        return;
      }
      const result = await discoverBusiness(session.accessToken, url.trim());
      const location = result.primaryLocation?.value?.trim() ?? '';
      const isMalta = /malta|birkirkara|sliema|valletta|st julians|san ġiljan/i.test(location);
      setDiscovery(result);
      setForm({
        name: result.name.value?.trim() ?? '',
        category: result.category.value?.trim() ?? '',
        primaryLocation: location,
        country: isMalta ? 'Malta' : '',
        timezone: isMalta ? 'Europe/Malta' : '',
        currency: isMalta ? 'EUR' : '',
        operatingStatus: 'Open',
      });
      setStage('confirm');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Atlas could not analyse that business page.');
    } finally {
      setBusy(false);
    }
  }

  async function submit() {
    setBusy(true);
    setError(null);
    try {
      const session = await loadSession();
      if (!session) {
        router.replace('/sign-in');
        return;
      }
      const business = await createBusiness(session.accessToken, form);
      await saveSession({ ...session, businessId: business.id });
      router.replace('/(tabs)');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Atlas could not finish business setup.');
    } finally {
      setBusy(false);
    }
  }

  if (stage === 'discover') {
    return (
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
        <TopBar />
        <Progress current={2} />

        <View style={styles.headingBlock}>
          <View style={styles.intelligenceBadge}><Text style={styles.intelligenceIcon}>✦</Text><Text style={styles.intelligenceText}>ATLAS DISCOVERY</Text></View>
          <Text accessibilityRole="header" style={styles.title}>Let Atlas understand your business.</Text>
          <Text style={styles.body}>Paste one public business page. Atlas will find the essentials, detect the category, and ask you only for what is missing.</Text>
        </View>

        <View style={styles.urlCard}>
          <View style={styles.urlLabelRow}>
            <View style={styles.urlIcon}><Text style={styles.urlIconText}>⌁</Text></View>
            <View style={styles.urlLabelCopy}><Text style={styles.label}>Business page</Text><Text style={styles.microcopy}>Bolt Food, Wolt, and more sources later</Text></View>
          </View>
          <TextInput
            accessibilityLabel="Business page URL"
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="url"
            onChangeText={setUrl}
            placeholder="https://food.bolt.eu/..."
            placeholderTextColor="#9299A8"
            style={styles.urlInput}
            value={url}
          />
          <View style={styles.providerRow}>
            <ProviderChip icon="B" label="Bolt Food" />
            <ProviderChip icon="W" label="Wolt" />
            <View style={styles.providerMore}><Text style={styles.providerMoreText}>+ more soon</Text></View>
          </View>
        </View>

        <View style={styles.scanCard}>
          <View style={styles.scanOrbWrap}>
            <Animated.View style={[styles.scanRing, { opacity: pulse.interpolate({ inputRange: [0, 1], outputRange: [0.28, 0.72] }), transform: [{ scale: pulse.interpolate({ inputRange: [0, 1], outputRange: [0.92, 1.08] }) }] }]} />
            <View style={styles.scanCore}><Text style={styles.scanCoreIcon}>{busy ? '✦' : '◎'}</Text></View>
          </View>
          <View style={styles.scanCopy}>
            <Text style={styles.scanEyebrow}>{busy ? 'ATLAS IS ANALYSING' : 'WHAT ATLAS LOOKS FOR'}</Text>
            <Text style={styles.scanTitle}>{busy ? 'Building your business snapshot…' : 'Useful context, automatically.'}</Text>
            <Text style={styles.scanBody}>{busy ? 'Reading the public page and structuring only the business facts we can support.' : 'Name, category, location, public positioning and other high-confidence signals.'}</Text>
          </View>
          <View style={styles.scanSignals}>
            <Signal icon="✓" label="Category" active />
            <Signal icon="⌖" label="Location" active />
            <Signal icon="≡" label="Business details" active />
          </View>
        </View>

        {error ? <View style={styles.errorCard}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}

        <Pressable accessibilityRole="button" disabled={busy || !url.trim()} onPress={analyse} style={({ pressed }) => [styles.button, (!url.trim() || busy) && styles.buttonDisabled, pressed && styles.pressed]}>
          {busy ? <ActivityIndicator color="#FFFFFF" /> : <><Text style={styles.buttonText}>Discover my business</Text><Text style={styles.buttonIcon}>→</Text></>}
        </Pressable>
        <Pressable accessibilityRole="button" onPress={() => { setError(null); setForm(emptyForm); setStage('manual'); }} style={styles.linkButton}>
          <Text style={styles.linkText}>No public page? <Text style={styles.linkStrong}>Set up manually</Text></Text>
        </Pressable>
      </ScrollView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <TopBar />
      <Progress current={stage === 'confirm' ? 3 : 1} />

      <View style={styles.headingBlock}>
        <View style={styles.intelligenceBadge}><Text style={styles.intelligenceIcon}>{stage === 'confirm' ? '✓' : '✦'}</Text><Text style={styles.intelligenceText}>{stage === 'confirm' ? 'BUSINESS FOUND' : 'MANUAL SETUP'}</Text></View>
        <Text accessibilityRole="header" style={styles.title}>{stage === 'confirm' ? 'We found your business.' : 'Tell Atlas the essentials.'}</Text>
        <Text style={styles.body}>{stage === 'confirm' ? `Review what Atlas found${discovery ? ` from ${providerLabel(discovery.provider)}` : ''}. Correct anything that does not look right.` : 'Only the minimum details are required. You can enrich your business context later.'}</Text>
      </View>

      {stage === 'confirm' && discovery ? (
        <View style={styles.foundCard}>
          <View style={styles.foundTopRow}>
            <View style={styles.foundIcon}><Text style={styles.foundIconText}>✓</Text></View>
            <View style={styles.foundCopy}>
              <View style={styles.foundTitleRow}><Text style={styles.foundTitle}>{form.name || 'Business name not found'}</Text><View style={styles.verifiedPill}><Text style={styles.verifiedText}>FOUND</Text></View></View>
              <Text style={styles.foundLocation}>{form.primaryLocation || 'Location needs confirmation'}</Text>
            </View>
          </View>
          <View style={styles.foundDivider} />
          <View style={styles.factRow}>
            <Fact icon="◎" label="Category" value={humanizeCategory(form.category) || 'Needs confirmation'} />
            <Fact icon="⌖" label="Source" value={providerLabel(discovery.provider)} />
            <Fact icon="✦" label="Confidence" value={capitalize(discovery.name.confidence)} />
          </View>
        </View>
      ) : null}

      <View style={styles.formCard}>
        <View style={styles.formHeader}>
          <Text style={styles.formEyebrow}>{stage === 'confirm' ? 'CONFIRM THE ESSENTIALS' : 'BUSINESS DETAILS'}</Text>
          <Text style={styles.formHint}>{stage === 'confirm' ? 'Atlas will use these as trusted business context.' : 'You can edit these later.'}</Text>
        </View>
        <EditableField icon="A" label="Business name" value={form.name} onChange={(value) => update('name', value)} />
        <EditableField icon="◎" label="Category" value={form.category} onChange={(value) => update('category', value)} placeholder="restaurant-cafe" />
        <EditableField icon="⌖" label="Primary location" value={form.primaryLocation} onChange={(value) => update('primaryLocation', value)} />
        <View style={styles.compactRow}>
          <View style={styles.compactField}><EditableField icon="◇" label="Country" value={form.country} onChange={(value) => update('country', value)} compact /></View>
          <View style={styles.compactField}><EditableField icon="€" label="Currency" value={form.currency} onChange={(value) => update('currency', value.toUpperCase())} placeholder="EUR" compact /></View>
        </View>
        <EditableField icon="◷" label="Timezone" value={form.timezone} onChange={(value) => update('timezone', value)} placeholder="Europe/Malta" />
      </View>

      {error ? <View style={styles.errorCard}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}

      <Pressable accessibilityRole="button" disabled={busy} onPress={submit} style={({ pressed }) => [styles.button, busy && styles.buttonDisabled, pressed && styles.pressed]}>
        {busy ? <ActivityIndicator color="#FFFFFF" /> : <><Text style={styles.buttonText}>{stage === 'confirm' ? 'Looks good, continue' : 'Continue with Atlas'}</Text><Text style={styles.buttonIcon}>→</Text></>}
      </Pressable>
      <Pressable accessibilityRole="button" onPress={() => { setError(null); setStage('discover'); }} style={styles.linkButton}>
        <Text style={styles.linkText}>{stage === 'confirm' ? 'Something looks wrong? ' : 'Use discovery instead? '}<Text style={styles.linkStrong}>{stage === 'confirm' ? 'Try another page' : 'Paste a business page'}</Text></Text>
      </Pressable>
    </ScrollView>
  );
}

function TopBar() {
  return (
    <View style={styles.topRow}>
      <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={() => router.back()} style={styles.backButton}><Text style={styles.backIcon}>←</Text></Pressable>
      <View style={styles.brandGroup}><View style={styles.brandMark}><Text style={styles.brandMarkText}>✦</Text></View><Text style={styles.brand}>ATLAS</Text></View>
    </View>
  );
}

function Progress({ current }: { current: 1 | 2 | 3 }) {
  const steps = ['Find business', 'Discover', 'Confirm'];
  return (
    <View style={styles.progressWrap} accessibilityLabel={`Onboarding step ${current} of 3`}>
      <View style={styles.progressLine} />
      {steps.map((step, index) => {
        const number = index + 1;
        const complete = number < current;
        const active = number === current;
        return (
          <View key={step} style={styles.progressItem}>
            <View style={[styles.progressDot, (complete || active) && styles.progressDotActive]}><Text style={[styles.progressDotText, (complete || active) && styles.progressDotTextActive]}>{complete ? '✓' : number}</Text></View>
            <Text style={[styles.progressLabel, active && styles.progressLabelActive]}>{step}</Text>
          </View>
        );
      })}
    </View>
  );
}

function ProviderChip({ icon, label }: { icon: string; label: string }) {
  return <View style={styles.providerChip}><View style={styles.providerIcon}><Text style={styles.providerIconText}>{icon}</Text></View><Text style={styles.providerLabel}>{label}</Text></View>;
}

function Signal({ icon, label, active }: { icon: string; label: string; active?: boolean }) {
  return <View style={styles.signal}><View style={[styles.signalDot, active && styles.signalDotActive]}><Text style={[styles.signalDotText, active && styles.signalDotTextActive]}>{icon}</Text></View><Text style={styles.signalText}>{label}</Text></View>;
}

function Fact({ icon, label, value }: { icon: string; label: string; value: string }) {
  return <View style={styles.fact}><Text style={styles.factIcon}>{icon}</Text><Text style={styles.factLabel}>{label}</Text><Text numberOfLines={1} style={styles.factValue}>{value}</Text></View>;
}

function EditableField({ icon, label, value, onChange, placeholder, compact = false }: { icon: string; label: string; value: string; onChange: (value: string) => void; placeholder?: string; compact?: boolean }) {
  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <View style={[styles.inputShell, compact && styles.inputShellCompact]}>
        <Text style={styles.inputIcon}>{icon}</Text>
        <TextInput accessibilityLabel={label} onChangeText={onChange} placeholder={placeholder} placeholderTextColor="#969DAB" style={styles.input} value={value} />
      </View>
    </View>
  );
}

function providerLabel(provider: string) {
  if (provider === 'bolt-food') return 'Bolt Food';
  if (provider === 'wolt') return 'Wolt';
  return provider;
}

function humanizeCategory(value: string) {
  return value.split('-').filter(Boolean).map((part) => part.charAt(0).toUpperCase() + part.slice(1)).join(' ');
}

function capitalize(value: string) {
  return value ? value.charAt(0).toUpperCase() + value.slice(1) : value;
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, paddingHorizontal: 20, paddingTop: 56, paddingBottom: 32, gap: 20, backgroundColor: '#F5F6FA' },
  topRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  backButton: { width: 42, height: 42, borderRadius: 14, alignItems: 'center', justifyContent: 'center', backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E7E9EF' },
  backIcon: { fontSize: 20, fontWeight: '800', color: '#20242E' },
  brandGroup: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  brandMark: { width: 30, height: 30, borderRadius: 10, alignItems: 'center', justifyContent: 'center', backgroundColor: '#111827' },
  brandMarkText: { color: '#C4B5FD', fontSize: 15, fontWeight: '900' },
  brand: { fontSize: 13, fontWeight: '900', letterSpacing: 2.4, color: '#111827' },
  progressWrap: { flexDirection: 'row', justifyContent: 'space-between', position: 'relative', paddingHorizontal: 4, marginTop: 2 },
  progressLine: { position: 'absolute', left: '16%', right: '16%', top: 15, height: 2, backgroundColor: '#DDDCE5' },
  progressItem: { width: '31%', alignItems: 'center', gap: 7 },
  progressDot: { width: 30, height: 30, borderRadius: 15, borderWidth: 1, borderColor: '#D6D8E0', backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center' },
  progressDotActive: { borderColor: '#6D28D9', backgroundColor: '#6D28D9' },
  progressDotText: { color: '#8A91A0', fontSize: 11, fontWeight: '900' },
  progressDotTextActive: { color: '#FFFFFF' },
  progressLabel: { fontSize: 10, fontWeight: '700', color: '#9298A6', textAlign: 'center' },
  progressLabelActive: { color: '#6D28D9', fontWeight: '900' },
  headingBlock: { gap: 12 },
  intelligenceBadge: { alignSelf: 'flex-start', flexDirection: 'row', alignItems: 'center', gap: 7, paddingHorizontal: 11, paddingVertical: 7, borderRadius: 999, backgroundColor: '#EEEAFD' },
  intelligenceIcon: { color: '#6D28D9', fontSize: 12, fontWeight: '900' },
  intelligenceText: { color: '#6D28D9', fontSize: 9, fontWeight: '900', letterSpacing: 1.1 },
  title: { maxWidth: 350, fontSize: 36, lineHeight: 40, letterSpacing: -1.2, fontWeight: '900', color: '#151821' },
  body: { maxWidth: 350, fontSize: 15, lineHeight: 23, color: '#737A89' },
  urlCard: { padding: 17, gap: 14, borderRadius: 24, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E9EAF0' },
  urlLabelRow: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  urlIcon: { width: 38, height: 38, borderRadius: 13, alignItems: 'center', justifyContent: 'center', backgroundColor: '#EEEAFD' },
  urlIconText: { color: '#6D28D9', fontSize: 18, fontWeight: '900' },
  urlLabelCopy: { gap: 2 },
  label: { fontSize: 12, fontWeight: '900', color: '#30343E' },
  microcopy: { fontSize: 10, color: '#8C93A1' },
  urlInput: { minHeight: 54, borderWidth: 1, borderColor: '#DDE0E7', borderRadius: 16, paddingHorizontal: 15, fontSize: 14, backgroundColor: '#F8F8FB', color: '#171A22' },
  providerRow: { flexDirection: 'row', gap: 8, flexWrap: 'wrap', alignItems: 'center' },
  providerChip: { flexDirection: 'row', alignItems: 'center', gap: 7, paddingHorizontal: 9, paddingVertical: 7, borderRadius: 12, backgroundColor: '#F7F7FA' },
  providerIcon: { width: 22, height: 22, borderRadius: 7, alignItems: 'center', justifyContent: 'center', backgroundColor: '#171922' },
  providerIconText: { color: '#FFFFFF', fontSize: 10, fontWeight: '900' },
  providerLabel: { fontSize: 11, fontWeight: '800', color: '#444955' },
  providerMore: { paddingHorizontal: 9, paddingVertical: 8 },
  providerMoreText: { fontSize: 10, fontWeight: '700', color: '#9298A6' },
  scanCard: { minHeight: 300, borderRadius: 28, overflow: 'hidden', padding: 20, backgroundColor: '#171922', alignItems: 'center' },
  scanOrbWrap: { width: 118, height: 118, marginTop: 4, alignItems: 'center', justifyContent: 'center' },
  scanRing: { position: 'absolute', width: 104, height: 104, borderRadius: 52, borderWidth: 2, borderColor: '#8B5CF6', backgroundColor: 'rgba(109,40,217,0.08)' },
  scanCore: { width: 68, height: 68, borderRadius: 24, alignItems: 'center', justifyContent: 'center', backgroundColor: '#EEEAFD' },
  scanCoreIcon: { color: '#6D28D9', fontSize: 26, fontWeight: '900' },
  scanCopy: { alignItems: 'center', gap: 6, marginTop: 5 },
  scanEyebrow: { fontSize: 9, fontWeight: '900', letterSpacing: 1.2, color: '#A78BFA' },
  scanTitle: { fontSize: 18, fontWeight: '900', color: '#FFFFFF', textAlign: 'center' },
  scanBody: { maxWidth: 290, fontSize: 12, lineHeight: 18, color: '#9EA4B1', textAlign: 'center' },
  scanSignals: { width: '100%', marginTop: 18, gap: 7 },
  signal: { minHeight: 38, flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 11, borderRadius: 12, backgroundColor: 'rgba(255,255,255,0.06)' },
  signalDot: { width: 22, height: 22, borderRadius: 8, alignItems: 'center', justifyContent: 'center', backgroundColor: 'rgba(255,255,255,0.08)' },
  signalDotActive: { backgroundColor: '#EEEAFD' },
  signalDotText: { color: '#9EA4B1', fontSize: 10, fontWeight: '900' },
  signalDotTextActive: { color: '#6D28D9' },
  signalText: { color: '#D7DAE1', fontSize: 11, fontWeight: '700' },
  foundCard: { padding: 18, borderRadius: 25, backgroundColor: '#171922', overflow: 'hidden' },
  foundTopRow: { flexDirection: 'row', alignItems: 'center', gap: 13 },
  foundIcon: { width: 48, height: 48, borderRadius: 16, alignItems: 'center', justifyContent: 'center', backgroundColor: '#E9F8F0' },
  foundIconText: { fontSize: 22, fontWeight: '900', color: '#198754' },
  foundCopy: { flex: 1, gap: 4 },
  foundTitleRow: { flexDirection: 'row', gap: 8, alignItems: 'center', flexWrap: 'wrap' },
  foundTitle: { flexShrink: 1, fontSize: 19, fontWeight: '900', color: '#FFFFFF' },
  verifiedPill: { paddingHorizontal: 8, paddingVertical: 4, borderRadius: 999, backgroundColor: 'rgba(25,135,84,0.18)' },
  verifiedText: { fontSize: 8, fontWeight: '900', letterSpacing: 0.8, color: '#6EE7A8' },
  foundLocation: { fontSize: 11, color: '#999FAC' },
  foundDivider: { height: 1, backgroundColor: 'rgba(255,255,255,0.09)', marginVertical: 16 },
  factRow: { flexDirection: 'row', gap: 8 },
  fact: { flex: 1, minWidth: 0, padding: 10, borderRadius: 13, backgroundColor: 'rgba(255,255,255,0.05)', gap: 3 },
  factIcon: { fontSize: 13, fontWeight: '900', color: '#C4B5FD' },
  factLabel: { fontSize: 8, fontWeight: '900', letterSpacing: 0.7, color: '#858C9B', textTransform: 'uppercase' },
  factValue: { fontSize: 10, fontWeight: '800', color: '#FFFFFF' },
  formCard: { padding: 17, borderRadius: 24, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E9EAF0', gap: 14 },
  formHeader: { gap: 3, marginBottom: 2 },
  formEyebrow: { fontSize: 9, fontWeight: '900', letterSpacing: 1.1, color: '#6D28D9' },
  formHint: { fontSize: 11, color: '#8C93A1' },
  field: { gap: 7 },
  inputShell: { minHeight: 52, borderWidth: 1, borderColor: '#E0E2E8', borderRadius: 15, paddingHorizontal: 12, flexDirection: 'row', alignItems: 'center', gap: 9, backgroundColor: '#F9F9FB' },
  inputShellCompact: { minHeight: 50 },
  inputIcon: { width: 20, textAlign: 'center', fontSize: 12, fontWeight: '900', color: '#6D28D9' },
  input: { flex: 1, minHeight: 48, fontSize: 13, color: '#22262F' },
  compactRow: { flexDirection: 'row', gap: 10 },
  compactField: { flex: 1 },
  errorCard: { padding: 13, borderRadius: 14, backgroundColor: '#FEECEC' },
  error: { fontSize: 13, lineHeight: 19, fontWeight: '700', color: '#991B1B' },
  button: { minHeight: 58, borderRadius: 18, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 12, backgroundColor: '#6D28D9' },
  buttonDisabled: { opacity: 0.55 },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '900' },
  buttonIcon: { color: '#FFFFFF', fontSize: 20, fontWeight: '900' },
  pressed: { transform: [{ scale: 0.985 }], opacity: 0.94 },
  linkButton: { minHeight: 42, alignItems: 'center', justifyContent: 'center' },
  linkText: { color: '#8A91A0', fontSize: 12, fontWeight: '600' },
  linkStrong: { color: '#4B505C', fontWeight: '900' },
});
