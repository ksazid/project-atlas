import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { createBusiness } from '@/api/atlas-client';
import { loadSession, saveSession } from '@/auth/session';

const initial = {
  name: '', category: '', country: '', timezone: 'Europe/Malta', currency: 'EUR', primaryLocation: '', operatingStatus: 'Open',
};

export default function CreateBusinessScreen() {
  const [form, setForm] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function update(key: keyof typeof form, value: string) {
    setForm((current) => ({ ...current, [key]: value }));
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
      setError(caught instanceof Error ? caught.message : 'Business creation failed.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <Text accessibilityRole="header" style={styles.title}>Create your Business</Text>
      <Text style={styles.body}>Add the minimum trusted details Atlas needs. You can enrich the profile later.</Text>
      {(Object.keys(form) as (keyof typeof form)[]).map((key) => (
        <View key={key} style={styles.field}>
          <Text style={styles.label}>{key.replace(/([A-Z])/g, ' $1')}</Text>
          <TextInput
            accessibilityLabel={key.replace(/([A-Z])/g, ' $1')}
            autoCapitalize={key === 'currency' ? 'characters' : 'sentences'}
            onChangeText={(value) => update(key, value)}
            style={styles.input}
            value={form[key]}
          />
        </View>
      ))}
      {error ? <Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text> : null}
      <Pressable accessibilityRole="button" disabled={busy} onPress={submit} style={styles.button}>
        {busy ? <ActivityIndicator /> : <Text style={styles.buttonText}>Create Business</Text>}
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 24, gap: 16 },
  title: { fontSize: 30, fontWeight: '700', marginTop: 28 },
  body: { fontSize: 16, lineHeight: 24 },
  field: { gap: 6 },
  label: { fontSize: 14, fontWeight: '600', textTransform: 'capitalize' },
  input: { minHeight: 48, borderWidth: 1, borderRadius: 12, paddingHorizontal: 14, fontSize: 16 },
  error: { fontSize: 15, fontWeight: '600' },
  button: { minHeight: 50, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: '#111827', marginBottom: 32 },
  buttonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '700' },
});
