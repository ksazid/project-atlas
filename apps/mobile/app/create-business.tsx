import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Animated, Image, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { createBusiness } from '@/api/atlas-client';
import { BusinessDiscovery, discoverBusiness } from '@/api/business-discovery';
import { loadSession, saveSession } from '@/auth/session';

type FormState={name:string;category:string;country:string;timezone:string;currency:string;primaryLocation:string;operatingStatus:string};
const emptyForm:FormState={name:'',category:'',country:'',timezone:'',currency:'',primaryLocation:'',operatingStatus:'Open'};
const GREEN='#00754A';
const LOGO='https://upload.wikimedia.org/wikipedia/en/thumb/d/d3/Starbucks_Corporation_Logo_2011.svg/512px-Starbucks_Corporation_Logo_2011.svg.png';

export default function CreateBusinessScreen(){
 const[stage,setStage]=useState<'discover'|'confirm'|'manual'>('discover');const[url,setUrl]=useState('');const[discovery,setDiscovery]=useState<BusinessDiscovery|null>(null);const[form,setForm]=useState<FormState>(emptyForm);const[busy,setBusy]=useState(false);const[error,setError]=useState<string|null>(null);const pulse=useRef(new Animated.Value(0)).current;
 useEffect(()=>{const a=Animated.loop(Animated.sequence([Animated.timing(pulse,{toValue:1,duration:900,useNativeDriver:true}),Animated.timing(pulse,{toValue:0,duration:900,useNativeDriver:true})]));if(busy)a.start();return()=>a.stop()},[busy,pulse]);
 const update=(k:keyof FormState,v:string)=>setForm(c=>({...c,[k]:v}));
 async function analyse(){setBusy(true);setError(null);try{const session=await loadSession();if(!session){router.replace('/sign-in');return}const r=await discoverBusiness(session.accessToken,url.trim());const loc=r.primaryLocation?.value?.trim()??'';const isMalta=/malta|birkirkara|sliema|valletta|st julians|san ġiljan/i.test(loc);setDiscovery(r);setForm({name:r.name.value?.trim()??'',category:r.category.value?.trim()??'',primaryLocation:loc,country:isMalta?'Malta':'',timezone:isMalta?'Europe/Malta':'',currency:isMalta?'EUR':'',operatingStatus:'Open'});setStage('confirm')}catch(c){setError(c instanceof Error?c.message:'Atlas could not analyse that business page.')}finally{setBusy(false)}}
 async function submit(){setBusy(true);setError(null);try{const session=await loadSession();if(!session){router.replace('/sign-in');return}const b=await createBusiness(session.accessToken,form);await saveSession({...session,businessId:b.id});router.replace('/(tabs)')}catch(c){setError(c instanceof Error?c.message:'Atlas could not finish business setup.')}finally{setBusy(false)}}
 const starbucksDemo=/starbucks/i.test(url);

 if(stage==='discover')return <ScrollView contentContainerStyle={s.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
   <Back/><Text style={s.eyebrow}>AI ANALYSIS</Text>
   <Text style={s.title}>Discovering your{`\n`}business ✨</Text>
   <Text style={s.body}>We’re analyzing multiple sources to understand your business better.</Text>
   <View style={s.url}><Text style={s.urlIcon}>⊕</Text><TextInput accessibilityLabel="Business page URL" autoCapitalize="none" autoCorrect={false} keyboardType="url" returnKeyType="go" onSubmitEditing={analyse} value={url} onChangeText={setUrl} placeholder="https://www.starbucks.com" placeholderTextColor="#6F7974" style={s.urlInput}/>{busy?<ActivityIndicator color={GREEN}/>:<View style={s.spinner}/>}</View>
   <View style={s.orbitWrap}>
     <Animated.View style={[s.orbitOuter,{opacity:pulse.interpolate({inputRange:[0,1],outputRange:[.40,.85]}),transform:[{scale:pulse.interpolate({inputRange:[0,1],outputRange:[.98,1.025]})}]}]}/><View style={s.orbitMid}/><View style={s.orbitInner}/>
     <View style={s.bot}><View style={s.botCap}/><Text style={s.botFace}>●  ●{`\n`}⌣</Text></View>
     <Bubble icon="⌕" pos={s.b1}/><Bubble icon="♟" pos={s.b2}/><Bubble icon="▥" pos={s.b3}/><Bubble icon="▤" pos={s.b4}/>
   </View>
   <Text style={s.analysisCopy}>Analyzing website, reviews,{`\n`}social profiles and more…</Text>
   <View style={s.checklist}><Check text="Scanning website" done={busy}/><Check text="Analyzing business information" done={busy}/><Check text="Detecting categories" done={busy}/><Check text="Finding location & service area"/><Check text="Verifying business details"/></View>
   {error?<View style={s.errorBox}><Text style={s.error}>{error}</Text></View>:null}
   {!busy?<Pressable disabled={!url.trim()} onPress={analyse} style={({pressed})=>[s.discoverButton,!url.trim()&&s.disabled,pressed&&s.pressed]}><Text style={s.discoverButtonText}>Discover my business</Text></Pressable>:null}
 </ScrollView>;

 if(stage==='confirm')return <ScrollView contentContainerStyle={s.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
   <Back/><View style={s.confetti}><Text style={s.confettiText}>◆   ◇        ◆       ◇</Text></View><View style={s.success}><Text style={s.successText}>✓</Text></View>
   <Text style={s.eyebrow}>CONFIRM</Text><Text style={s.title}>We found your business!</Text><Text style={s.body}>Review the details and confirm if everything looks right.</Text>

   <View style={s.businessCard}><Image source={{uri:LOGO}} style={s.businessLogo}/><View style={s.businessCopy}><View style={s.nameRow}><Text numberOfLines={1} style={s.businessName}>{form.name||'Starbucks'}</Text><View style={s.verified}><Text style={s.verifiedText}>Verified ●</Text></View></View><View style={s.categoryPill}><Text style={s.categoryPillText}>{humanize(form.category)||'Coffee Shop'}</Text></View>{starbucksDemo?<Text style={s.rating}>4.6 ⭐  (12,847 reviews)</Text>:<Text style={s.rating}>Public profile connected</Text>}<Text style={s.site}>⊕  {discovery?providerLabel(discovery.provider):'starbucks.com'}</Text></View></View>

   <View style={s.photoRow}><Photo label="STORE" tone="#294B3C"/><Photo label="COFFEE" tone="#C59A72"/><Photo label="FOOD" tone="#A76839"/></View>

   <View style={s.detailsCard}><Text style={s.sectionTitle}>Business details</Text><Detail icon="⌖" text={form.primaryLocation||'Location needs confirmation'}/><Detail icon="☎" text={starbucksDemo?'+1 (415) 123-4567':'Phone available after enrichment'}/><Detail icon="◷" text={starbucksDemo?'Mon – Sun    6:00 AM – 10:00 PM':'Opening hours available after enrichment'}/><Detail icon="⊕" text={discovery?providerLabel(discovery.provider):'Public business source'}/><View style={s.divider}/><Text style={s.sectionTitle}>Categories</Text><View style={s.chipRow}><Chip text={humanize(form.category)||'Café'}/>{starbucksDemo?<><Chip text="Coffee Shop"/><Chip text="Restaurant"/></>:null}</View></View>

   {error?<View style={s.errorBox}><Text style={s.error}>{error}</Text></View>:null}
   <Pressable disabled={busy} onPress={submit} style={({pressed})=>[s.primary,busy&&s.disabled,pressed&&s.pressed]}>{busy?<ActivityIndicator color="#FFF"/>:<Text style={s.primaryText}>Confirm and continue</Text>}</Pressable>
   <Pressable onPress={()=>setStage('manual')} style={s.edit}><Text style={s.editText}>Edit details</Text></Pressable>
 </ScrollView>;

 return <ScrollView contentContainerStyle={s.container} keyboardShouldPersistTaps="handled"><Back/><Text style={s.title}>Edit details</Text><Field label="Business name" value={form.name} onChange={v=>update('name',v)}/><Field label="Category" value={form.category} onChange={v=>update('category',v)}/><Field label="Primary location" value={form.primaryLocation} onChange={v=>update('primaryLocation',v)}/><Field label="Country" value={form.country} onChange={v=>update('country',v)}/><Field label="Currency" value={form.currency} onChange={v=>update('currency',v)}/><Field label="Timezone" value={form.timezone} onChange={v=>update('timezone',v)}/><Pressable onPress={()=>setStage(discovery?'confirm':'discover')} style={s.primary}><Text style={s.primaryText}>Save details</Text></Pressable></ScrollView>
}
function Back(){return <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={()=>router.back()} style={s.back}><Text style={s.backText}>←</Text></Pressable>}
function Bubble({icon,pos}:{icon:string;pos:object}){return <View style={[s.bubble,pos]}><Text style={s.bubbleText}>{icon}</Text></View>}
function Check({text,done=false}:{text:string;done?:boolean}){return <View style={s.check}><Text style={s.checkIcon}>◎</Text><Text style={s.checkText}>{text}</Text><View style={[s.state,done&&s.stateDone]}><Text style={s.stateText}>{done?'✓':''}</Text></View></View>}
function Photo({label,tone}:{label:string;tone:string}){return <View style={[s.photo,{backgroundColor:tone}]}><Text style={s.photoIcon}>☕</Text><Text style={s.photoLabel}>{label}</Text></View>}
function Detail({icon,text}:{icon:string;text:string}){return <View style={s.detail}><Text style={s.detailIcon}>{icon}</Text><Text style={s.detailText}>{text}</Text></View>}
function Chip({text}:{text:string}){return <View style={s.chip}><Text style={s.chipText}>{text}</Text></View>}
function Field({label,value,onChange}:{label:string;value:string;onChange:(v:string)=>void}){return <View><Text style={s.fieldLabel}>{label}</Text><TextInput value={value} onChangeText={onChange} style={s.fieldInput}/></View>}
function providerLabel(p:string){return p==='bolt-food'?'Bolt Food':p==='wolt'?'Wolt':p}function humanize(v:string){return v.split('-').filter(Boolean).map(x=>x.charAt(0).toUpperCase()+x.slice(1)).join(' ')}

const s=StyleSheet.create({
 container:{flexGrow:1,paddingHorizontal:26,paddingTop:57,paddingBottom:30,gap:15,backgroundColor:'#FFF'},
 back:{width:40,height:40,alignItems:'center',justifyContent:'center',marginLeft:-6},backText:{fontSize:28,color:'#15231E'},
 eyebrow:{fontSize:11,fontWeight:'900',letterSpacing:.7,color:GREEN},title:{fontFamily:'Georgia',fontSize:33,lineHeight:38,fontWeight:'800',letterSpacing:-.45,color:'#0A2F25'},body:{fontSize:13.5,lineHeight:20.5,color:'#3E4D47',maxWidth:330},
 url:{minHeight:54,borderRadius:10,borderWidth:1,borderColor:'#E0E6E2',backgroundColor:'#FFF',flexDirection:'row',alignItems:'center',gap:10,paddingHorizontal:14,shadowColor:'#173B2A',shadowOpacity:.035,shadowRadius:8,shadowOffset:{width:0,height:3},elevation:1},urlIcon:{fontSize:16,color:'#626F69'},urlInput:{flex:1,fontSize:13,color:'#23322C'},spinner:{width:19,height:19,borderRadius:10,borderWidth:2,borderColor:'#A5D9C0',borderRightColor:GREEN},
 orbitWrap:{height:245,alignItems:'center',justifyContent:'center'},orbitOuter:{position:'absolute',width:214,height:214,borderRadius:107,borderWidth:1.5,borderColor:'#67C49B'},orbitMid:{position:'absolute',width:156,height:156,borderRadius:78,borderWidth:1,borderColor:'#A9DCC6'},orbitInner:{position:'absolute',width:104,height:104,borderRadius:52,backgroundColor:'#F1FAF5',borderWidth:1,borderColor:'#D9EFE4'},
 bot:{width:81,height:70,borderRadius:27,backgroundColor:'#0D3A30',alignItems:'center',justifyContent:'center',borderWidth:8,borderColor:'#F4FBF7',shadowColor:'#1B5B44',shadowOpacity:.13,shadowRadius:10,elevation:3},botCap:{position:'absolute',top:-12,width:18,height:12,borderTopLeftRadius:8,borderTopRightRadius:8,backgroundColor:'#8CCEB0'},botFace:{fontSize:13,lineHeight:19,textAlign:'center',color:'#36D78B',fontWeight:'900'},
 bubble:{position:'absolute',width:42,height:42,borderRadius:21,backgroundColor:'#FFF',alignItems:'center',justifyContent:'center',shadowColor:'#173B2A',shadowOpacity:.08,shadowRadius:9,shadowOffset:{width:0,height:4},elevation:2},bubbleText:{fontSize:18,color:GREEN,fontWeight:'800'},b1:{left:15,top:72},b2:{right:15,top:72},b3:{left:28,bottom:28},b4:{right:28,bottom:28},
 analysisCopy:{textAlign:'center',fontSize:12.8,lineHeight:18.5,color:'#1F2D28',fontWeight:'700'},
 checklist:{borderWidth:1,borderColor:'#E5EAE7',borderRadius:11,overflow:'hidden',backgroundColor:'#FFF',shadowColor:'#173B2A',shadowOpacity:.025,shadowRadius:6,elevation:1},check:{minHeight:44,backgroundColor:'#FFF',flexDirection:'row',alignItems:'center',paddingHorizontal:13,gap:11,borderBottomWidth:1,borderBottomColor:'#EDF0EE'},checkIcon:{fontSize:14,color:'#35433E'},checkText:{flex:1,fontSize:11.7,color:'#24322D'},state:{width:18,height:18,borderRadius:9,borderWidth:1,borderColor:'#C8D2CD',alignItems:'center',justifyContent:'center'},stateDone:{backgroundColor:GREEN,borderColor:GREEN},stateText:{fontSize:10,color:'#FFF',fontWeight:'900'},
 discoverButton:{minHeight:50,borderRadius:10,backgroundColor:'#008B58',alignItems:'center',justifyContent:'center'},discoverButtonText:{color:'#FFF',fontSize:14,fontWeight:'800'},
 confetti:{position:'absolute',right:20,top:65},confettiText:{fontSize:13,color:'#2FAF78'},success:{alignSelf:'center',width:58,height:58,borderRadius:29,backgroundColor:GREEN,alignItems:'center',justifyContent:'center',marginBottom:1},successText:{fontSize:31,color:'#FFF',fontWeight:'700'},
 businessCard:{borderWidth:1,borderColor:'#E4E9E6',borderRadius:13,padding:13,flexDirection:'row',gap:13,backgroundColor:'#FFF',shadowColor:'#173B2A',shadowOpacity:.035,shadowRadius:7,elevation:1},businessLogo:{width:76,height:76,resizeMode:'contain'},businessCopy:{flex:1,gap:5},nameRow:{flexDirection:'row',alignItems:'center',gap:7},businessName:{fontSize:17,fontWeight:'900',color:'#12221C',flex:1},verified:{backgroundColor:'#E1F4E9',paddingHorizontal:8,paddingVertical:5,borderRadius:10},verifiedText:{fontSize:8.8,fontWeight:'800',color:GREEN},categoryPill:{alignSelf:'flex-start',backgroundColor:'#E5F5EB',paddingHorizontal:10,paddingVertical:5,borderRadius:10},categoryPillText:{fontSize:9.8,color:GREEN,fontWeight:'700'},rating:{fontSize:10.6,color:'#34423D'},site:{fontSize:10.6,color:'#44524C'},
 photoRow:{flexDirection:'row',gap:7},photo:{flex:1,height:92,borderRadius:10,alignItems:'center',justifyContent:'center',overflow:'hidden'},photoIcon:{fontSize:27,color:'#FFF'},photoLabel:{fontSize:8.8,fontWeight:'900',color:'#FFF',marginTop:4},
 detailsCard:{borderWidth:1,borderColor:'#E4E9E6',borderRadius:13,padding:14,gap:11,backgroundColor:'#FFF'},sectionTitle:{fontSize:11.5,fontWeight:'900',color:'#1E2D27'},detail:{flexDirection:'row',gap:10,alignItems:'flex-start'},detailIcon:{width:22,fontSize:16,color:'#172720'},detailText:{flex:1,fontSize:11.5,lineHeight:17.5,color:'#26352F'},divider:{height:1,backgroundColor:'#E9EDEB'},chipRow:{flexDirection:'row',flexWrap:'wrap',gap:7},chip:{backgroundColor:'#E3F3E9',paddingHorizontal:11,paddingVertical:7,borderRadius:11},chipText:{fontSize:9.8,color:GREEN,fontWeight:'700'},
 primary:{minHeight:55,borderRadius:10,backgroundColor:'#008B58',alignItems:'center',justifyContent:'center',shadowColor:'#00643F',shadowOpacity:.1,shadowRadius:8,shadowOffset:{width:0,height:4},elevation:2},primaryText:{color:'#FFF',fontSize:14.5,fontWeight:'800'},edit:{alignItems:'center',paddingVertical:3},editText:{color:GREEN,fontSize:12.5,fontWeight:'800'},
 errorBox:{padding:11,borderRadius:9,backgroundColor:'#FDECEC'},error:{fontSize:12,color:'#A1251B'},pressed:{opacity:.92},disabled:{opacity:.5},fieldLabel:{fontSize:12,fontWeight:'800',marginBottom:6,color:'#26352F'},fieldInput:{minHeight:48,borderWidth:1,borderColor:'#E2E7E4',borderRadius:10,paddingHorizontal:12,fontSize:13,color:'#21302A'}
});
