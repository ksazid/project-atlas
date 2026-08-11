import { StyleSheet, Text, View } from 'react-native';
import type { Business, BusinessHubProfile } from '@/api/atlas-client';
import { tokens } from '@/theme/tokens';

type Props = { business: Business; profile: BusinessHubProfile | null };

export function BusinessSnapshotCard({ business, profile }: Props) {
  const facts = [
    { label: 'Location', value: business.primaryLocation },
    { label: 'Hours', value: profile?.businessHours },
    { label: 'Phone', value: profile?.phone },
    { label: 'Website', value: profile?.website },
    { label: 'Operating status', value: business.operatingStatus },
  ].filter((fact): fact is { label: string; value: string } => Boolean(fact.value?.trim()));

  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <Text style={styles.eyebrow}>BUSINESS SNAPSHOT</Text>
        <Text style={styles.title}>The operating facts Atlas is using</Text>
      </View>
      <View style={styles.facts}>
        {facts.map((fact, index) => (
          <View key={fact.label} style={[styles.fact, index > 0 && styles.factBorder]}>
            <Text style={styles.label}>{fact.label}</Text>
            <Text selectable style={styles.value}>{fact.value}</Text>
          </View>
        ))}
      </View>
      {profile ? <Text style={styles.provenance}>{profile.ownerConfirmed ? 'Owner confirmed' : 'Observed from public business information'} · Updated {formatDate(profile.updatedAt)}</Text> : <Text style={styles.provenance}>Profile details are still being established.</Text>}
    </View>
  );
}

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'recently' : date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
}

const styles = StyleSheet.create({
  card: { backgroundColor: tokens.color.surface, borderColor: tokens.color.border, borderRadius: tokens.radius.md, borderWidth: 1, gap: 14, padding: 18 },
  header: { gap: 5 },
  eyebrow: { color: tokens.color.green, fontSize: 10.5, fontWeight: '900', letterSpacing: 1 },
  title: { color: tokens.color.ink, fontSize: 18, fontWeight: '800', lineHeight: 24 },
  facts: { borderColor: tokens.color.border, borderRadius: tokens.radius.sm, borderWidth: 1, overflow: 'hidden' },
  fact: { backgroundColor: '#FCFCFA', gap: 3, paddingHorizontal: 14, paddingVertical: 12 },
  factBorder: { borderTopColor: tokens.color.border, borderTopWidth: 1 },
  label: { color: tokens.color.muted, fontSize: 10.5, fontWeight: '800', letterSpacing: .5, textTransform: 'uppercase' },
  value: { color: tokens.color.ink, fontSize: 14, fontWeight: '700', lineHeight: 20 },
  provenance: { color: tokens.color.muted, fontSize: 11.5, lineHeight: 17 },
});
