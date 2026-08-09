import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';

export default function WelcomeScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.topRow}>
        <View style={styles.brandMark}><Text style={styles.brandMarkText}>✦</Text></View>
        <Text style={styles.brand}>ATLAS</Text>
        <View style={styles.betaPill}><Text style={styles.betaText}>INTELLIGENCE</Text></View>
      </View>

      <View style={styles.heroCard}>
        <View style={styles.orbLarge} />
        <View style={styles.orbSmall} />
        <View style={styles.heroContent}>
          <View style={styles.heroTag}><Text style={styles.heroTagIcon}>✦</Text><Text style={styles.heroTagText}>Your business, understood.</Text></View>
          <Text accessibilityRole="header" style={styles.title}>Know the next move that matters.</Text>
          <Text style={styles.body}>
            Atlas turns your business signals into focused decisions, ready-to-run actions, and learning that gets sharper over time.
          </Text>
          <View style={styles.signalCard}>
            <View style={styles.signalIcon}><Text style={styles.signalIconText}>↗</Text></View>
            <View style={styles.signalCopy}>
              <Text style={styles.signalLabel}>TODAY&apos;S SIGNAL</Text>
              <Text style={styles.signalTitle}>Turn quiet hours into profitable demand</Text>
              <Text style={styles.signalMeta}>Evidence-backed · measurable · actionable</Text>
            </View>
          </View>
        </View>
      </View>

      <View accessibilityLabel="How Atlas works" style={styles.loopCard}>
        <Text style={styles.sectionLabel}>HOW ATLAS THINKS</Text>
        <View style={styles.loopGrid}>
          <Step icon="⌁" title="Understand" body="Connect context and signals." />
          <Step icon="◎" title="Prioritise" body="Rank what matters now." />
          <Step icon="↗" title="Execute" body="Get the exact action kit." />
          <Step icon="✓" title="Learn" body="Measure and improve." />
        </View>
      </View>

      <View style={styles.actions}>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}>
          <Text style={styles.primaryButtonText}>Start with Atlas</Text>
          <Text style={styles.primaryButtonIcon}>→</Text>
        </Pressable>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={styles.secondaryButton}>
          <Text style={styles.secondaryButtonText}>Already using Atlas? <Text style={styles.secondaryStrong}>Sign in</Text></Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function Step({ icon, title, body }: { icon: string; title: string; body: string }) {
  return (
    <View style={styles.step}>
      <View style={styles.stepIcon}><Text style={styles.stepIconText}>{icon}</Text></View>
      <Text style={styles.stepTitle}>{title}</Text>
      <Text style={styles.stepBody}>{body}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, padding: 20, paddingTop: 58, paddingBottom: 30, gap: 20, backgroundColor: '#F5F6FA' },
  topRow: { flexDirection: 'row', alignItems: 'center', gap: 9 },
  brandMark: { width: 34, height: 34, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: '#111827' },
  brandMarkText: { color: '#C4B5FD', fontSize: 17, fontWeight: '800' },
  brand: { fontSize: 15, fontWeight: '900', letterSpacing: 2.6, color: '#111827' },
  betaPill: { marginLeft: 'auto', paddingHorizontal: 10, paddingVertical: 6, borderRadius: 999, backgroundColor: '#EEEAFD' },
  betaText: { fontSize: 9, fontWeight: '900', letterSpacing: 1.1, color: '#6D28D9' },
  heroCard: { minHeight: 430, borderRadius: 30, overflow: 'hidden', backgroundColor: '#11131A', position: 'relative' },
  orbLarge: { position: 'absolute', width: 260, height: 260, borderRadius: 130, right: -88, top: -72, backgroundColor: '#5B21B6', opacity: 0.48 },
  orbSmall: { position: 'absolute', width: 160, height: 160, borderRadius: 80, left: -86, bottom: 26, backgroundColor: '#2563EB', opacity: 0.18 },
  heroContent: { flex: 1, padding: 26, paddingTop: 30, gap: 18, justifyContent: 'center' },
  heroTag: { alignSelf: 'flex-start', flexDirection: 'row', gap: 8, alignItems: 'center', paddingHorizontal: 12, paddingVertical: 8, borderRadius: 999, backgroundColor: 'rgba(255,255,255,0.10)' },
  heroTagIcon: { color: '#C4B5FD', fontSize: 13 },
  heroTagText: { color: '#E5E7EB', fontSize: 12, fontWeight: '700' },
  title: { maxWidth: 310, fontSize: 41, lineHeight: 45, letterSpacing: -1.5, fontWeight: '900', color: '#FFFFFF' },
  body: { maxWidth: 320, fontSize: 16, lineHeight: 24, color: '#B8BDC9' },
  signalCard: { marginTop: 6, flexDirection: 'row', alignItems: 'center', gap: 13, padding: 15, borderRadius: 18, borderWidth: 1, borderColor: 'rgba(255,255,255,0.12)', backgroundColor: 'rgba(255,255,255,0.08)' },
  signalIcon: { width: 42, height: 42, borderRadius: 14, alignItems: 'center', justifyContent: 'center', backgroundColor: '#EDE9FE' },
  signalIconText: { fontSize: 20, fontWeight: '900', color: '#6D28D9' },
  signalCopy: { flex: 1, gap: 3 },
  signalLabel: { fontSize: 9, fontWeight: '900', letterSpacing: 1.2, color: '#A78BFA' },
  signalTitle: { fontSize: 14, lineHeight: 19, fontWeight: '800', color: '#FFFFFF' },
  signalMeta: { fontSize: 11, color: '#969DAC' },
  loopCard: { padding: 18, borderRadius: 24, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#EBEDF2', gap: 14 },
  sectionLabel: { fontSize: 10, fontWeight: '900', letterSpacing: 1.5, color: '#8A91A0' },
  loopGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  step: { width: '48%', minHeight: 120, padding: 14, borderRadius: 18, backgroundColor: '#F8F8FB', gap: 8 },
  stepIcon: { width: 32, height: 32, borderRadius: 10, alignItems: 'center', justifyContent: 'center', backgroundColor: '#EEEAFD' },
  stepIconText: { color: '#6D28D9', fontSize: 16, fontWeight: '900' },
  stepTitle: { fontSize: 14, fontWeight: '800', color: '#171A22' },
  stepBody: { fontSize: 12, lineHeight: 17, color: '#757C8B' },
  actions: { gap: 8 },
  primaryButton: { minHeight: 58, borderRadius: 18, paddingHorizontal: 20, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 12, backgroundColor: '#6D28D9' },
  primaryButtonText: { color: '#FFFFFF', fontSize: 16, fontWeight: '900' },
  primaryButtonIcon: { color: '#FFFFFF', fontSize: 20, fontWeight: '800' },
  pressed: { transform: [{ scale: 0.985 }], opacity: 0.94 },
  secondaryButton: { minHeight: 44, alignItems: 'center', justifyContent: 'center' },
  secondaryButtonText: { color: '#7A8190', fontSize: 13, fontWeight: '600' },
  secondaryStrong: { color: '#20242E', fontWeight: '900' },
});
