import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';

const GREEN = '#27A968';
const GREEN_DARK = '#168754';

export default function WelcomeScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.glowOne} /><View style={styles.glowTwo} />
      <View style={styles.brandArea}>
        <View style={styles.logo}><Text style={styles.logoA}>A</Text><Text style={styles.spark}>✦</Text></View>
        <Text style={styles.brand}>Atlas</Text>
      </View>

      <View style={styles.hero}>
        <Text accessibilityRole="header" style={styles.title}>AI for local{`\n`}<Text style={styles.green}>business</Text>{`\n`}intelligence</Text>
        <Text style={styles.body}>Discover opportunities. Understand your market. Grow with confidence.</Text>
      </View>

      <View style={styles.intelligenceScene} accessibilityLabel="Atlas business intelligence illustration">
        <FloatCard style={styles.cardLeft} icon="↗" text="Growth" />
        <FloatCard style={styles.cardRight} icon="◔" text="Insights" />
        <View style={styles.shopShadow} />
        <View style={styles.shop}>
          <View style={styles.awning}><View style={styles.stripe}/><View style={styles.stripeLight}/><View style={styles.stripe}/><View style={styles.stripeLight}/></View>
          <View style={styles.shopBody}><View style={styles.door}/><View style={styles.window}/></View>
        </View>
        <View style={styles.pin}><Text style={styles.pinText}>●</Text></View>
        <View style={styles.treeOne}/><View style={styles.treeTwo}/>
      </View>

      <View style={styles.dots}><View style={styles.dotActive}/><View style={styles.dot}/><View style={styles.dot}/></View>

      <View style={styles.actions}>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed]}>
          <Text style={styles.primaryText}>Get started</Text><Text style={styles.arrow}>→</Text>
        </Pressable>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={({ pressed }) => [styles.secondaryButton, pressed && styles.pressed]}>
          <Text style={styles.secondaryText}>I already have an account</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function FloatCard({ style, icon, text }: { style: object; icon: string; text: string }) {
  return <View style={[styles.floatCard, style]}><Text style={styles.floatIcon}>{icon}</Text><Text style={styles.floatText}>{text}</Text></View>;
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, paddingHorizontal: 24, paddingTop: 70, paddingBottom: 28, backgroundColor: '#FBFCFB', overflow: 'hidden' },
  glowOne: { position: 'absolute', width: 300, height: 300, borderRadius: 150, backgroundColor: '#E7F8EE', opacity: .75, right: -160, top: 30 },
  glowTwo: { position: 'absolute', width: 260, height: 260, borderRadius: 130, backgroundColor: '#EEF7FF', opacity: .65, left: -150, top: 390 },
  brandArea: { alignItems: 'center', gap: 4 },
  logo: { width: 72, height: 64, alignItems: 'center', justifyContent: 'center' },
  logoA: { fontSize: 60, lineHeight: 64, fontWeight: '900', color: GREEN, letterSpacing: -5 },
  spark: { position: 'absolute', right: 0, top: 2, fontSize: 15, color: '#65BBD7' },
  brand: { fontSize: 26, lineHeight: 31, fontWeight: '900', color: '#101827' },
  hero: { marginTop: 30, alignItems: 'center', gap: 15 },
  title: { fontSize: 35, lineHeight: 42, letterSpacing: -.8, fontWeight: '900', color: '#101827', textAlign: 'center' },
  green: { color: GREEN },
  body: { maxWidth: 310, fontSize: 14, lineHeight: 22, color: '#485463', textAlign: 'center' },
  intelligenceScene: { height: 250, marginTop: 20, position: 'relative', alignItems: 'center', justifyContent: 'center' },
  floatCard: { position: 'absolute', width: 78, height: 66, borderRadius: 13, backgroundColor: 'rgba(255,255,255,.94)', alignItems: 'center', justifyContent: 'center', gap: 3, shadowColor: '#19452E', shadowOpacity: .08, shadowRadius: 15, shadowOffset: { width: 0, height: 7 }, elevation: 3 },
  cardLeft: { left: 14, top: 28, transform: [{ rotate: '-4deg' }] },
  cardRight: { right: 12, top: 34, transform: [{ rotate: '4deg' }] },
  floatIcon: { color: GREEN, fontSize: 24, fontWeight: '900' },
  floatText: { color: '#758078', fontSize: 9, fontWeight: '700' },
  shopShadow: { position: 'absolute', width: 175, height: 28, borderRadius: 90, backgroundColor: '#DCECE2', bottom: 25, opacity: .75 },
  shop: { width: 150, height: 120, marginTop: 48, shadowColor: '#2C6C48', shadowOpacity: .12, shadowRadius: 18, shadowOffset: { width: 0, height: 10 }, elevation: 4 },
  awning: { height: 35, borderTopLeftRadius: 15, borderTopRightRadius: 15, overflow: 'hidden', flexDirection: 'row' },
  stripe: { flex: 1, backgroundColor: '#4AC37E' }, stripeLight: { flex: 1, backgroundColor: '#DDF6E7' },
  shopBody: { flex: 1, backgroundColor: '#F5FFF8', borderBottomLeftRadius: 10, borderBottomRightRadius: 10, flexDirection: 'row', padding: 16, gap: 12 },
  door: { width: 34, backgroundColor: '#BCE9CD', borderRadius: 5 }, window: { flex: 1, backgroundColor: '#D8F4E3', borderRadius: 5 },
  pin: { position: 'absolute', right: 55, bottom: 48, width: 38, height: 38, borderRadius: 19, backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', shadowColor: '#19452E', shadowOpacity: .1, shadowRadius: 10, elevation: 3 },
  pinText: { color: GREEN, fontSize: 22 },
  treeOne: { position: 'absolute', left: 70, bottom: 42, width: 27, height: 40, borderRadius: 16, backgroundColor: '#A7E1BC' },
  treeTwo: { position: 'absolute', right: 88, bottom: 26, width: 22, height: 33, borderRadius: 13, backgroundColor: '#C7EDD4' },
  dots: { flexDirection: 'row', justifyContent: 'center', gap: 8, marginTop: 2, marginBottom: 22 },
  dotActive: { width: 8, height: 8, borderRadius: 4, backgroundColor: GREEN }, dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: '#D9DEE2' },
  actions: { gap: 12 },
  primaryButton: { minHeight: 56, borderRadius: 13, backgroundColor: GREEN, alignItems: 'center', justifyContent: 'center', flexDirection: 'row' },
  primaryText: { color: '#FFFFFF', fontSize: 16, fontWeight: '800' }, arrow: { position: 'absolute', right: 20, color: '#FFFFFF', fontSize: 23 },
  secondaryButton: { minHeight: 54, borderRadius: 13, borderWidth: 1, borderColor: '#E7ECE9', backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center' },
  secondaryText: { color: '#26322B', fontSize: 14, fontWeight: '700' }, pressed: { opacity: .9, transform: [{ scale: .988 }] },
});
