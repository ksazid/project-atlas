import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
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
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={styles.eyebrow}>BUSINESS SETUP</Text>
        <Text accessibilityRole="header" style={styles.title}>Tell Atlas about your business</Text>
        <Text style={styles.body}>Paste a public business page. Atlas will use it to understand the basics so you only confirm what matters.</Text>
        <View style={styles.field}>
          <Text style={styles.label}>Business page</Text>
          <TextInput
            accessibilityLabel="Business page URL"
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="url"
            onChangeText={setUrl}
            placeholder="https://food.bolt.eu/... or https://wolt.com/..."
            style={styles.input}
            value={url}
          />
        </View>
        <Text style={styles.helper}>Supported first: Bolt Food and Wolt. More sources can plug into the same discovery model later.</Text>
        {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
        <Pressable accessibilityRole="button" disabled={busy || !url.trim()} onPress={analyse} style={[styles.button, (!url.trim() || busy) && styles.buttonDisabled]}>
          {busy ? <ActivityIndicator color="#FFFFFF" /> : <Text style={styles.buttonText}>Analyse my business</Text>}
        </Pressable>
        <Pressable accessibilityRole="button" onPress={() => { setError(null); setForm(emptyForm); setStage('manual'); }} style={styles.linkButton}>
          <Text style={styles.linkText}>Set up manually</Text>
        </Pressable>
      </ScrollView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <Text style={styles.eyebrow}>{stage === 'confirm' ? 'ATLAS DISCOVERY' : 'MANUAL SETUP'}</Text>
      <Text accessibilityRole="header" style={styles.title}>{stage === 'confirm' ? 'We found your business' : 'Tell Atlas the essentials'}</Text>
      <Text style={styles.body}>
        {stage === 'confirm'
          ? `Review what Atlas found${discovery ? ` from ${providerLabel(discovery.provider)}` : ''}. Confirm or correct anything before continuing.`
          : 'Enter only the minimum details Atlas needs to start. You can enrich the business later.'}
      </Text>

      {stage === 'confirm' && discovery ? (
        <View style={styles.discoveryCard}>
          <Text style={styles.discoveryTitle}>{form.name || 'Business name not found'}</Text>
          <Text style={styles.discoveryMeta}>Category: {form.category || 'Needs confirmation'}</Text>
          <Text style={styles.discoveryMeta}>Location: {form.primaryLocation || 'Needs confirmation'}</Text>
          <Text style={styles.discoveryMeta}>Source confidence: {discovery.name.confidence}</Text>
        </View>
      ) : null}

      <EditableField label="Business name" value={form.name} onChange={(value) => update('name', value)} />
      <EditableField label="Category" value={form.category} onChange={(value) => update('category', value)} placeholder="restaurant-cafe" />
      <EditableField label="Primary location" value={form.primaryLocation} onChange={(value) => update('primaryLocation', value)} />
      <EditableField label="Country" value={form.country} onChange={(value) => update('country', value)} />
      <EditableField label="Timezone" value={form.timezone} onChange={(value) => update('timezone', value)} placeholder="Europe/Malta" />
      <EditableField label="Currency" value={form.currency} onChange={(value) => update('currency', value.toUpperCase())} placeholder="EUR" />

      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
      <Pressable accessibilityRole="button" disabled={busy} onPress={submit} style={[styles.button, busy && styles.buttonDisabled]}>
        {busy ? <ActivityIndicator color="#FFFFFF" /> : <Text style={styles.buttonText}>Everything looks right</Text>}
      </Pressable>
      <Pressable accessibilityRole="button" onPress={() => { setError(null); setStage('discover'); }} style={styles.linkButton}>
        <Text style={styles.linkText}>Use a different business page</Text>
      </Pressable>
    </ScrollView>
  );
}

function EditableField({ label, value, onChange, placeholder }: { label: string; value: string; onChange: (value: string) => void; placeholder?: string }) {
  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <TextInput accessibilityLabel={label} onChangeText={onChange} placeholder={placeholder} style={styles.input} value={value} />
    </View>
  );
}

function providerLabel(provider: string) {
  if (provider === 'bolt-food') return 'Bolt Food';
  if (provider === 'wolt') return 'Wolt';
  return provider;
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, padding: 24, paddingTop: 56, paddingBottom: 36, gap: 16, backgroundColor: '#F8FAFC' },
  eyebrow: { fontSize: 12, fontWeight: '800', letterSpacing: 1.8, color: '#64748B' },
  title: { fontSize: 32, lineHeight: 38, fontWeight: '800', color: '#0F172A' },
  body: { fontSize: 16, lineHeight: 24, color: '#475569', marginBottom: 6 },
  helper: { fontSize: 13, lineHeight: 19, color: '#64748B' },
  field: { gap: 7 },
  label: { fontSize: 14, fontWeight: '700', color: '#334155' },
  input: { minHeight: 50, borderWidth: 1, borderColor: '#CBD5E1', borderRadius: 12, paddingHorizontal: 14, fontSize: 16, backgroundColor: '#FFFFFF', color: '#0F172A' },
  discoveryCard: { gap: 7, borderWidth: 1, borderColor: '#CBD5E1', borderRadius: 16, padding: 18, backgroundColor: '#FFFFFF' },
  discoveryTitle: { fontSize: 20, fontWeight: '800', color: '#0F172A' },
  discoveryMeta: { fontSize: 14, lineHeight: 20, color: '#475569' },
  error: { fontSize: 14, lineHeight: 21, fontWeight: '600', color: '#991B1B' },
  button: { minHeight: 52, borderRadius: 13, alignItems: 'center', justifyContent: 'center', backgroundColor: '#0F172A', marginTop: 6 },
  buttonDisabled: { opacity: 0.55 },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '800' },
  linkButton: { minHeight: 44, alignItems: 'center', justifyContent: 'center' },
  linkText: { color: '#334155', fontSize: 15, fontWeight: '600' },
});
