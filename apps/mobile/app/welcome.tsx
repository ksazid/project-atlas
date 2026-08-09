import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';

export default function WelcomeScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container}>
      <View style={styles.hero}>
        <Text style={styles.eyebrow}>ATLAS</Text>
        <Text accessibilityRole="header" style={styles.title}>Turn business data into your next best move.</Text>
        <Text style={styles.body}>
          Atlas learns how your business works, finds the opportunities that deserve attention, gives you practical actions to execute, and learns from the results.
        </Text>
      </View>

      <View accessibilityLabel="How Atlas works" style={styles.loop}>
        <Step number="1" title="Understand" body="Bring your business context and data together." />
        <Step number="2" title="Decide" body="Focus on the opportunity with the strongest evidence." />
        <Step number="3" title="Act" body="Use a ready-to-execute action kit instead of generic advice." />
        <Step number="4" title="Learn" body="Measure the outcome so Atlas gets smarter for your business." />
      </View>

      <View style={styles.actions}>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={styles.primaryButton}>
          <Text style={styles.primaryButtonText}>Get started</Text>
        </Pressable>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={styles.secondaryButton}>
          <Text style={styles.secondaryButtonText}>Already have an account? Sign in</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function Step({ number, title, body }: { number: string; title: string; body: string }) {
  return (
    <View style={styles.step}>
      <View style={styles.stepNumber}><Text style={styles.stepNumberText}>{number}</Text></View>
      <View style={styles.stepCopy}>
        <Text style={styles.stepTitle}>{title}</Text>
        <Text style={styles.stepBody}>{body}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, padding: 24, paddingTop: 72, paddingBottom: 36, gap: 34, backgroundColor: '#F8FAFC' },
  hero: { gap: 14 },
  eyebrow: { fontSize: 13, fontWeight: '800', letterSpacing: 2.4, color: '#475569' },
  title: { fontSize: 38, lineHeight: 44, fontWeight: '800', color: '#0F172A' },
  body: { fontSize: 17, lineHeight: 26, color: '#475569' },
  loop: { gap: 16 },
  step: { flexDirection: 'row', gap: 14, alignItems: 'flex-start' },
  stepNumber: { width: 34, height: 34, borderRadius: 17, alignItems: 'center', justifyContent: 'center', backgroundColor: '#E2E8F0' },
  stepNumberText: { fontSize: 14, fontWeight: '800', color: '#0F172A' },
  stepCopy: { flex: 1, gap: 3 },
  stepTitle: { fontSize: 16, fontWeight: '750', color: '#0F172A' },
  stepBody: { fontSize: 14, lineHeight: 21, color: '#64748B' },
  actions: { marginTop: 'auto', gap: 10 },
  primaryButton: { minHeight: 54, borderRadius: 14, alignItems: 'center', justifyContent: 'center', backgroundColor: '#0F172A' },
  primaryButtonText: { color: '#FFFFFF', fontSize: 17, fontWeight: '800' },
  secondaryButton: { minHeight: 48, alignItems: 'center', justifyContent: 'center' },
  secondaryButtonText: { color: '#334155', fontSize: 15, fontWeight: '650' },
});
