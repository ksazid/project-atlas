import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Animated, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { createBusiness } from '@/api/atlas-client';
import { BusinessDiscovery, discoverBusiness } from '@/api/business-discovery';
import { loadSession, saveSession } from '@/auth/session';

type FormState = { name: string; category: string; country: string; timezone: string; currency: string; primaryLocation: string; operatingStatus: string };
const emptyForm: FormState = { name: '', category: '', country: '', timezone: '', currency: '', primaryLocation: '', operatingStatus: 'Open' };
const GREEN = '#26A864'; const INK = '#101828'; const MUTED = '#4F5D55';

export default function CreateBusinessScreen() {
  const [stage, setStage] = useState<'discover' | 'confirm' | 'manual'>('discover');
  const [url, setUrl] = useState(''); const [discovery, setDiscovery] = useState<BusinessDiscovery | null>(null); const [form, setForm] = useState<FormState>(emptyForm); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null);
  const pulse = useRef(new Animated.Value(0)).current;
  useEffect(() => { const animation = Animated.loop(Animated.sequence([Animated.timing(pulse,{toValue:1,duration:850,useNativeDriver:true}),Animated.timing(pulse,{toValue:0,duration:850,useNativeDriver:true})])); if (busy) animation.start(); return () => animation.stop(); }, [busy,pulse]);
  const update = (key:keyof FormState,value:string) => setForm(current => ({...current,[key]:value}));

  async function analyse() {
    setBusy(true); setError(null);
    try {
      const session = await loadSession(); if (!session) { router.replace('/sign-in'); return; }
      const result = await discoverBusiness(session.accessToken,url.trim());
      const location = result.primaryLocation?.value?.trim() ?? '';
      const isMalta = /malta|birkirkara|sliema|valletta|st julians|san ġiljan/i.test(location);
      setDiscovery(result);
      setForm({ name: result.name.value?.trim() ?? '', category: result.category.value?.trim() ?? '', primaryLocation: location, country: isMalta ? 'Malta' : '', timezone: isMalta ? 'Europe/Malta' : '', currency: isMalta ? 'EUR' : '', operatingStatus: 'Open' });
      setStage('confirm');
    } catch (caught) { setError(caught instanceof Error ? caught.message : 'Atlas could not analyse that business page.'); }
    finally { setBusy(false); }
  }

  async function submit() {
    setBusy(true); setError(null);
    try {
      const session = await loadSession(); if (!session) { router.replace('/sign-in'); return; }
      const business = await createBusiness(session.accessToken,form); await saveSession({...session,businessId:business.id}); router.replace('/(tabs)');
    } catch (caught) { setError(caught instanceof Error ? caught.message : 'Atlas could not finish business setup.'); }
    finally { setBusy(false); }
  }

  if (stage === 'discover') return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <Back/><Progress current={2}/>
      <Text accessibilityRole="header" style={styles.title}>Atlas is discovering{`\n`}your business <Text style={styles.green}>✦</Text></Text>
      <Text style={styles.body}>Sit tight while we find and analyze your business using AI from trusted public sources.</Text>

      <View style={styles.urlShell}>
        <Text style={styles.urlIcon}>◎</Text>
        <TextInput accessibilityLabel="Business page URL" autoCapitalize="none" autoCorrect={false} keyboardType="url" value={url} onChangeText={setUrl} placeholder="https://food.bolt.eu/..." placeholderTextColor="#88938D" style={styles.urlInput}/>
        {busy ? <ActivityIndicator color={GREEN}/> : <View style={styles.urlReady}/>} 
      </View>

      <View style={styles.aiBlock}>
        <View style={styles.orbitWrap}>
          <Animated.View style={[styles.orbit,{ opacity: busy ? pulse.interpolate({inputRange:[0,1],outputRange:[0.42,0.92]}) : 0.58, transform:[{scale: busy ? pulse.interpolate({inputRange:[0,1],outputRange:[0.97,1.05]}) : 1}] }]} />
          <View style={styles.bot}><View style={styles.botTop}/><Text style={styles.botEyes}>●   ●</Text><Text style={styles.botSmile}>⌣</Text></View>
          <Bubble icon="⌕" style={styles.b1}/><Bubble icon="♟" style={styles.b2}/><Bubble icon="▥" style={styles.b3}/><Bubble icon="▤" style={styles.b4}/>
        </View>
        <Text style={styles.aiCopy}>{busy ? 'Analyzing website, reviews, business information and more…' : 'Atlas will analyze the business page and prepare the essentials.'}</Text>
      </View>

      <View style={styles.checklist}>
        <Check text="Scanning business page" done={busy}/>
        <Check text="Analyzing business information" done={busy}/>
        <Check text="Detecting categories" done={busy}/>
        <Check text="Finding location & service area"/>
        <Check text="Verifying business details"/>
      </View>
      {error ? <View style={styles.errorBox}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}
      <Pressable accessibilityRole="button" disabled={busy || !url.trim()} onPress={analyse} style={({pressed}) => [styles.primary,(busy||!url.trim())&&styles.disabled,pressed&&styles.pressed]}>{busy ? <ActivityIndicator color="#FFFFFF"/> : <><Text style={styles.primaryText}>Discover my business</Text><Text style={styles.arrow}>→</Text></>}</Pressable>
      <Pressable accessibilityRole="button" onPress={() => { setError(null); setForm(emptyForm); setStage('manual'); }} style={styles.link}><Text style={styles.linkText}>Set up manually</Text></Pressable>
    </ScrollView>
  );

  if (stage === 'confirm') return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <Back/><Progress current={3}/>
      <View style={styles.successCircle}><Text style={styles.successCheck}>✓</Text></View>
      <Text accessibilityRole="header" style={styles.confirmTitle}>Business found! 🎉</Text>
      <Text style={styles.confirmBody}>Please review the details we found and confirm if everything looks right.</Text>

      <View style={styles.businessCard}>
        <View style={styles.businessImage}><Text style={styles.businessInitial}>{(form.name || 'A').charAt(0).toUpperCase()}</Text></View>
        <View style={styles.businessCopy}>
          <View style={styles.businessNameRow}><Text numberOfLines={1} style={styles.businessName}>{form.name || 'Business'}</Text><View style={styles.verified}><Text style={styles.verifiedText}>Found</Text></View></View>
          <Text numberOfLines={1} style={styles.businessMeta}>{form.primaryLocation || 'Location needs confirmation'}</Text>
          <Text style={styles.businessMeta}>{discovery ? providerLabel(discovery.provider) : 'Public source'}</Text>
        </View>
      </View>

      <Section title="Categories"><View style={styles.chipRow}><Chip text={humanize(form.category) || 'Needs confirmation'}/></View></Section>
      <Section title="Business type"><View style={styles.infoRow}><View style={styles.infoIcon}><Text style={styles.infoIconText}>♙</Text></View><Text style={styles.infoText}>Not confirmed yet</Text></View></Section>
      <Section title="Services"><View style={styles.placeholderPill}><Text style={styles.placeholderPillText}>Services will appear when confirmed</Text></View></Section>
      <Section title="Data sources"><View style={styles.sourceBubble}><Text style={styles.sourceText}>{discovery ? providerInitial(discovery.provider) : 'A'}</Text></View></Section>

      {error ? <View style={styles.errorBox}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}
      <Pressable accessibilityRole="button" disabled={busy} onPress={submit} style={({pressed}) => [styles.primary,busy&&styles.disabled,pressed&&styles.pressed]}>{busy ? <ActivityIndicator color="#FFFFFF"/> : <><Text style={styles.primaryText}>Looks good, continue</Text><Text style={styles.arrow}>→</Text></>}</Pressable>
      <Pressable accessibilityRole="button" onPress={() => setStage('manual')} style={styles.link}><Text style={styles.linkText}>Something looks wrong</Text></Pressable>
    </ScrollView>
  );

  return (
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <Back/><Progress current={1}/>
      <Text accessibilityRole="header" style={styles.title}>Tell Atlas the essentials</Text>
      <Text style={styles.body}>Correct only what needs changing. Atlas will keep the rest of the discovered context.</Text>
      <View style={styles.formCard}>
        <Field label="Business name" value={form.name} onChange={value => update('name',value)}/>
        <Field label="Category" value={form.category} onChange={value => update('category',value)} placeholder="restaurant-cafe"/>
        <Field label="Primary location" value={form.primaryLocation} onChange={value => update('primaryLocation',value)}/>
        <View style={styles.twoCol}><View style={styles.half}><Field label="Country" value={form.country} onChange={value => update('country',value)}/></View><View style={styles.half}><Field label="Currency" value={form.currency} onChange={value => update('currency',value.toUpperCase())} placeholder="EUR"/></View></View>
        <Field label="Timezone" value={form.timezone} onChange={value => update('timezone',value)} placeholder="Europe/Malta"/>
      </View>
      {error ? <View style={styles.errorBox}><Text accessibilityLiveRegion="polite" style={styles.error}>{error}</Text></View> : null}
      <Pressable accessibilityRole="button" disabled={busy} onPress={submit} style={({pressed}) => [styles.primary,busy&&styles.disabled,pressed&&styles.pressed]}>{busy ? <ActivityIndicator color="#FFFFFF"/> : <><Text style={styles.primaryText}>Continue</Text><Text style={styles.arrow}>→</Text></>}</Pressable>
      <Pressable accessibilityRole="button" onPress={() => setStage(discovery ? 'confirm' : 'discover')} style={styles.link}><Text style={styles.linkText}>Back</Text></Pressable>
    </ScrollView>
  );
}

function Back(){return <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={() => router.back()} style={styles.back}><Text style={styles.backText}>←</Text></Pressable>}
function Progress({current}:{current:1|2|3}){const labels=['Find business','Discover','Confirm'];return <View style={styles.progress}><View style={styles.progressLine}/>{labels.map((label,index)=>{const n=index+1,done=n<current,active=n===current;return <View key={label} style={styles.progressItem}><View style={[styles.progressDot,(done||active)&&styles.progressDotActive]}><Text style={[styles.progressNumber,(done||active)&&styles.progressNumberActive]}>{done?'✓':n}</Text></View><Text style={[styles.progressLabel,active&&styles.progressLabelActive]}>{label}</Text></View>})}</View>}
function Bubble({icon,style}:{icon:string;style:object}){return <View style={[styles.bubble,style]}><Text style={styles.bubbleText}>{icon}</Text></View>}
function Check({text,done=false}:{text:string;done?:boolean}){return <View style={styles.checkRow}><Text style={styles.checkIcon}>◎</Text><Text style={styles.checkLabel}>{text}</Text><View style={[styles.checkState,done&&styles.checkStateDone]}><Text style={styles.checkStateText}>{done?'✓':''}</Text></View></View>}
function Section({title,children}:{title:string;children:React.ReactNode}){return <View style={styles.section}><Text style={styles.sectionTitle}>{title}</Text>{children}</View>}
function Chip({text}:{text:string}){return <View style={styles.chip}><Text style={styles.chipText}>{text}</Text></View>}
function Field({label,value,onChange,placeholder}:{label:string;value:string;onChange:(value:string)=>void;placeholder?:string}){return <View style={styles.field}><Text style={styles.fieldLabel}>{label}</Text><TextInput accessibilityLabel={label} value={value} onChangeText={onChange} placeholder={placeholder} placeholderTextColor="#929D96" style={styles.fieldInput}/></View>}
function providerLabel(provider:string){return provider==='bolt-food'?'Bolt Food':provider==='wolt'?'Wolt':provider}
function providerInitial(provider:string){return provider==='bolt-food'?'B':provider==='wolt'?'W':'A'}
function humanize(value:string){return value.split('-').filter(Boolean).map(part=>part.charAt(0).toUpperCase()+part.slice(1)).join(' ')}

const styles = StyleSheet.create({
  container:{flexGrow:1,paddingHorizontal:24,paddingTop:56,paddingBottom:30,gap:17,backgroundColor:'#FCFDFC'},
  back:{width:40,height:40,borderRadius:12,borderWidth:1,borderColor:'#E6ECE8',backgroundColor:'#FFFFFF',alignItems:'center',justifyContent:'center'},backText:{fontSize:20,fontWeight:'800',color:'#24312A'},
  progress:{height:66,flexDirection:'row',justifyContent:'space-between',position:'relative',zIndex:2},progressLine:{position:'absolute',left:'15%',right:'15%',top:15,height:2,backgroundColor:'#D4E5DA',zIndex:-1},progressItem:{width:'31%',alignItems:'center',gap:7},progressDot:{width:30,height:30,borderRadius:15,backgroundColor:'#F0F3F1',alignItems:'center',justifyContent:'center'},progressDotActive:{backgroundColor:GREEN},progressNumber:{fontSize:11,fontWeight:'800',color:'#66736C'},progressNumberActive:{color:'#FFFFFF'},progressLabel:{fontSize:10,color:'#657169'},progressLabelActive:{fontWeight:'800',color:'#187D4D'},
  title:{fontSize:31,lineHeight:38,fontWeight:'900',letterSpacing:-0.6,color:INK},green:{color:GREEN},body:{fontSize:13.5,lineHeight:20.5,color:MUTED},
  urlShell:{minHeight:55,borderRadius:11,borderWidth:1,borderColor:'#E2E8E4',backgroundColor:'#FFFFFF',flexDirection:'row',alignItems:'center',paddingHorizontal:14,gap:10,shadowColor:'#173B2A',shadowOpacity:0.03,shadowRadius:7,elevation:1},urlIcon:{fontSize:16,color:'#68756E'},urlInput:{flex:1,fontSize:13.5,color:'#25332B'},urlReady:{width:18,height:18,borderRadius:9,borderWidth:2,borderColor:'#BFD8C8'},
  aiBlock:{alignItems:'center',gap:11},orbitWrap:{width:205,height:205,alignItems:'center',justifyContent:'center'},orbit:{position:'absolute',width:166,height:166,borderRadius:83,borderWidth:7,borderColor:'#52BF7F',backgroundColor:'#F0FAF3'},bot:{width:82,height:70,borderRadius:25,backgroundColor:'#E9F4FF',borderWidth:7,borderColor:'#FFFFFF',alignItems:'center',justifyContent:'center',shadowColor:'#46789A',shadowOpacity:0.10,shadowRadius:14,elevation:3},botTop:{position:'absolute',top:-12,width:18,height:12,borderTopLeftRadius:9,borderTopRightRadius:9,backgroundColor:'#91C1DD'},botEyes:{fontSize:11,color:'#18314A',fontWeight:'900',letterSpacing:3},botSmile:{fontSize:15,color:'#18314A',marginTop:-4},bubble:{position:'absolute',width:36,height:36,borderRadius:18,backgroundColor:'#FFFFFF',alignItems:'center',justifyContent:'center',shadowColor:'#246A45',shadowOpacity:0.07,shadowRadius:8,elevation:2},bubbleText:{fontSize:16,fontWeight:'800',color:GREEN},b1:{left:0,top:73},b2:{right:0,top:70},b3:{right:21,bottom:7},b4:{left:21,bottom:7},aiCopy:{maxWidth:285,fontSize:12.5,lineHeight:18.5,fontWeight:'600',color:'#34443B',textAlign:'center'},
  checklist:{gap:8},checkRow:{minHeight:46,borderRadius:11,borderWidth:1,borderColor:'#E8ECEA',backgroundColor:'#FFFFFF',flexDirection:'row',alignItems:'center',paddingHorizontal:14,gap:10},checkIcon:{fontSize:14,color:'#536159'},checkLabel:{flex:1,fontSize:11.5,color:'#2C3932'},checkState:{width:18,height:18,borderRadius:9,borderWidth:1,borderColor:'#CAD4CE',alignItems:'center',justifyContent:'center'},checkStateDone:{backgroundColor:GREEN,borderColor:GREEN},checkStateText:{fontSize:10,fontWeight:'900',color:'#FFFFFF'},
  successCircle:{alignSelf:'center',marginTop:2,width:78,height:78,borderRadius:39,backgroundColor:'#DCF5E6',alignItems:'center',justifyContent:'center'},successCheck:{fontSize:42,fontWeight:'800',color:GREEN},confirmTitle:{fontSize:27,lineHeight:33,fontWeight:'900',letterSpacing:-0.4,color:INK,textAlign:'center'},confirmBody:{paddingHorizontal:18,fontSize:12.5,lineHeight:19,color:'#55625B',textAlign:'center'},
  businessCard:{minHeight:105,padding:14,borderRadius:14,borderWidth:1,borderColor:'#E5EAE7',backgroundColor:'#FFFFFF',flexDirection:'row',gap:13,shadowColor:'#173B2A',shadowOpacity:0.03,shadowRadius:8,elevation:1},businessImage:{width:72,height:72,borderRadius:10,backgroundColor:'#E1F4E8',alignItems:'center',justifyContent:'center'},businessInitial:{fontSize:31,fontWeight:'900',color:GREEN},businessCopy:{flex:1,justifyContent:'center',gap:5},businessNameRow:{flexDirection:'row',alignItems:'center',gap:7},businessName:{flex:1,fontSize:16.5,fontWeight:'900',color:'#17221C'},verified:{paddingHorizontal:9,paddingVertical:5,borderRadius:8,backgroundColor:'#E0F6E8'},verifiedText:{fontSize:9,fontWeight:'800',color:'#168452'},businessMeta:{fontSize:10.5,color:'#657169'},
  section:{gap:8,paddingBottom:12,borderBottomWidth:1,borderBottomColor:'#EDF0EE'},sectionTitle:{fontSize:11,fontWeight:'900',color:'#25322B'},chipRow:{flexDirection:'row',flexWrap:'wrap',gap:7},chip:{paddingHorizontal:11,paddingVertical:7,borderRadius:8,backgroundColor:'#DFF4E7'},chipText:{fontSize:10,fontWeight:'700',color:'#187D4D'},infoRow:{flexDirection:'row',alignItems:'center',gap:9},infoIcon:{width:28,height:28,borderRadius:9,backgroundColor:'#F0F5F2',alignItems:'center',justifyContent:'center'},infoIconText:{fontSize:13,color:'#56645C'},infoText:{fontSize:11.5,color:'#59665F'},placeholderPill:{alignSelf:'flex-start',paddingHorizontal:11,paddingVertical:7,borderRadius:8,backgroundColor:'#F1F4F2'},placeholderPillText:{fontSize:10,color:'#7B8780'},sourceBubble:{width:38,height:38,borderRadius:11,borderWidth:1,borderColor:'#E3E8E5',backgroundColor:'#FFFFFF',alignItems:'center',justifyContent:'center'},sourceText:{fontSize:17,fontWeight:'900',color:GREEN},
  formCard:{gap:12},field:{gap:6},fieldLabel:{fontSize:11,fontWeight:'800',color:'#445149'},fieldInput:{minHeight:47,borderRadius:10,borderWidth:1,borderColor:'#E1E7E3',backgroundColor:'#FFFFFF',paddingHorizontal:13,fontSize:13,color:'#1B2821'},twoCol:{flexDirection:'row',gap:10},half:{flex:1},
  errorBox:{padding:11,borderRadius:10,backgroundColor:'#FDECEC'},error:{fontSize:11.5,lineHeight:17,fontWeight:'700',color:'#A1251B'},primary:{minHeight:56,borderRadius:11,backgroundColor:GREEN,alignItems:'center',justifyContent:'center',flexDirection:'row',shadowColor:'#287348',shadowOpacity:0.10,shadowRadius:10,shadowOffset:{width:0,height:5},elevation:3},primaryText:{fontSize:15,fontWeight:'800',color:'#FFFFFF'},arrow:{position:'absolute',right:18,fontSize:22,color:'#FFFFFF'},link:{minHeight:34,alignItems:'center',justifyContent:'center'},linkText:{fontSize:11.5,fontWeight:'700',color:'#188252'},disabled:{opacity:0.5},pressed:{opacity:0.92,transform:[{scale:0.99}]}
});
