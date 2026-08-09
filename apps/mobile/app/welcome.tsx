import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';

const GREEN = '#26A864';
const INK = '#101828';
const MUTED = '#44515B';

export default function WelcomeScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.logoBlock}>
        <View style={styles.logoWrap}>
          <Text style={styles.logoA}>A</Text>
          <Text style={styles.logoSpark}>✦</Text>
        </View>
        <Text style={styles.brand}>Atlas</Text>
      </View>

      <View style={styles.copyBlock}>
        <Text accessibilityRole="header" style={styles.title}>AI for local{`\n`}<Text style={styles.green}>business</Text>{`\n`}intelligence</Text>
        <Text style={styles.subtitle}>Discover opportunities. Understand{`\n`}your market. Grow with confidence.</Text>
      </View>

      <View style={styles.scene} accessibilityLabel="Atlas local business intelligence illustration">
        <View style={[styles.floatingCard, styles.cardLeft]}>
          <View style={styles.miniChart}><View style={styles.bar1}/><View style={styles.bar2}/><View style={styles.bar3}/></View>
        </View>
        <View style={[styles.floatingCard, styles.cardRight]}>
          <View style={styles.pie}><View style={styles.pieCut}/></View>
        </View>

        <View style={styles.shopGround}/>
        <View style={styles.shopRoof}/>
        <View style={styles.shop}>
          <View style={styles.awning}>
            <View style={styles.awningGreen}/><View style={styles.awningPale}/><View style={styles.awningGreen}/><View style={styles.awningPale}/><View style={styles.awningGreen}/>
          </View>
          <View style={styles.shopBody}>
            <View style={styles.door}/>
            <View style={styles.window}/>
          </View>
        </View>
        <View style={styles.treeLeft}/><View style={styles.treeLeftStem}/>
        <View style={styles.treeRight}/><View style={styles.treeRightStem}/>
        <View style={styles.locationBubble}><View style={styles.locationPin}><View style={styles.locationDot}/></View></View>
      </View>

      <View style={styles.pagination}><View style={styles.pageActive}/><View style={styles.pageDot}/><View style={styles.pageDot}/></View>

      <View style={styles.actions}>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={({ pressed }) => [styles.primary, pressed && styles.pressed]}>
          <Text style={styles.primaryText}>Get started</Text><Text style={styles.arrow}>→</Text>
        </Pressable>
        <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={({ pressed }) => [styles.secondary, pressed && styles.pressed]}>
          <Text style={styles.secondaryText}>I already have an account</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, paddingHorizontal: 28, paddingTop: 70, paddingBottom: 28, backgroundColor: '#FCFDFC' },
  logoBlock: { alignItems: 'center' },
  logoWrap: { width: 78, height: 66, alignItems: 'center', justifyContent: 'center' },
  logoA: { fontSize: 62, lineHeight: 66, fontWeight: '900', letterSpacing: -5, color: GREEN },
  logoSpark: { position: 'absolute', right: 0, top: 1, fontSize: 15, fontWeight: '900', color: '#70BDD1' },
  brand: { marginTop: 3, fontSize: 25, lineHeight: 31, fontWeight: '900', color: INK },
  copyBlock: { marginTop: 30, alignItems: 'center' },
  title: { fontSize: 33, lineHeight: 39, fontWeight: '900', letterSpacing: -0.8, color: INK, textAlign: 'center' },
  green: { color: GREEN },
  subtitle: { marginTop: 16, fontSize: 13.5, lineHeight: 21, color: MUTED, textAlign: 'center' },
  scene: { height: 290, marginTop: 17, position: 'relative', alignItems: 'center', justifyContent: 'center' },
  floatingCard: { position: 'absolute', width: 76, height: 62, borderRadius: 12, backgroundColor: '#FFFFFF', shadowColor: '#2F6F4B', shadowOpacity: 0.08, shadowRadius: 14, shadowOffset: { width: 0, height: 7 }, elevation: 3, alignItems: 'center', justifyContent: 'center' },
  cardLeft: { left: 12, top: 48, transform: [{ rotate: '-5deg' }] },
  cardRight: { right: 8, top: 56, transform: [{ rotate: '5deg' }] },
  miniChart: { width: 42, height: 34, flexDirection: 'row', alignItems: 'flex-end', gap: 4 },
  bar1: { width: 8, height: 12, borderRadius: 3, backgroundColor: '#BFE7CF' },
  bar2: { width: 8, height: 22, borderRadius: 3, backgroundColor: '#72CC95' },
  bar3: { width: 8, height: 31, borderRadius: 3, backgroundColor: GREEN },
  pie: { width: 36, height: 36, borderRadius: 18, backgroundColor: '#A8D6E8', overflow: 'hidden' },
  pieCut: { position: 'absolute', right: -2, top: -2, width: 20, height: 20, borderBottomLeftRadius: 18, backgroundColor: '#F3E8C7' },
  shopGround: { position: 'absolute', bottom: 32, width: 192, height: 28, borderRadius: 96, backgroundColor: '#E3F0E8' },
  shopRoof: { position: 'absolute', top: 82, width: 148, height: 26, borderTopLeftRadius: 18, borderTopRightRadius: 18, backgroundColor: '#F5EEE6', transform: [{ rotate: '-1deg' }] },
  shop: { position: 'absolute', top: 100, width: 150, height: 126, borderRadius: 12, shadowColor: '#306A49', shadowOpacity: 0.10, shadowRadius: 18, shadowOffset: { width: 0, height: 10 }, elevation: 4 },
  awning: { height: 34, borderTopLeftRadius: 12, borderTopRightRadius: 12, overflow: 'hidden', flexDirection: 'row' },
  awningGreen: { flex: 1, backgroundColor: '#50C980' },
  awningPale: { flex: 1, backgroundColor: '#E4F8EC' },
  shopBody: { flex: 1, flexDirection: 'row', gap: 12, padding: 15, backgroundColor: '#F8FFF9', borderBottomLeftRadius: 12, borderBottomRightRadius: 12 },
  door: { width: 34, borderRadius: 5, backgroundColor: '#BFE9CD' },
  window: { flex: 1, height: 43, borderRadius: 5, backgroundColor: '#D7F2E0' },
  treeLeft: { position: 'absolute', left: 66, bottom: 55, width: 31, height: 42, borderRadius: 18, backgroundColor: '#ABE3BE' },
  treeLeftStem: { position: 'absolute', left: 80, bottom: 42, width: 4, height: 17, borderRadius: 2, backgroundColor: '#96CDA8' },
  treeRight: { position: 'absolute', right: 70, bottom: 48, width: 28, height: 39, borderRadius: 16, backgroundColor: '#C8ECD3' },
  treeRightStem: { position: 'absolute', right: 82, bottom: 37, width: 4, height: 15, borderRadius: 2, backgroundColor: '#A7D0B4' },
  locationBubble: { position: 'absolute', right: 50, bottom: 82, width: 45, height: 45, borderRadius: 23, backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', shadowColor: '#2B6546', shadowOpacity: 0.10, shadowRadius: 11, elevation: 3 },
  locationPin: { width: 22, height: 27, borderRadius: 11, borderWidth: 5, borderColor: GREEN, alignItems: 'center', justifyContent: 'center' },
  locationDot: { width: 5, height: 5, borderRadius: 3, backgroundColor: GREEN },
  pagination: { marginTop: 1, marginBottom: 23, flexDirection: 'row', justifyContent: 'center', gap: 8 },
  pageActive: { width: 8, height: 8, borderRadius: 4, backgroundColor: GREEN },
  pageDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: '#D8DEDA' },
  actions: { gap: 12 },
  primary: { minHeight: 56, borderRadius: 12, backgroundColor: GREEN, alignItems: 'center', justifyContent: 'center', flexDirection: 'row', shadowColor: '#287348', shadowOpacity: 0.11, shadowRadius: 11, shadowOffset: { width: 0, height: 5 }, elevation: 3 },
  primaryText: { color: '#FFFFFF', fontSize: 15.5, fontWeight: '800' },
  arrow: { position: 'absolute', right: 18, color: '#FFFFFF', fontSize: 22, fontWeight: '500' },
  secondary: { minHeight: 54, borderRadius: 12, borderWidth: 1, borderColor: '#E6EBE8', backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center' },
  secondaryText: { fontSize: 13.5, fontWeight: '700', color: '#26322B' },
  pressed: { opacity: 0.92, transform: [{ scale: 0.99 }] },
});
