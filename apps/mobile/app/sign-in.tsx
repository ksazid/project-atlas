import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { authorizeWithProvider } from '@/auth/provider';
import { saveSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { AtlasScreen } from '@/components/AtlasScreen';

const GREEN='#00754A';
const EXPO_GO_DEMO_TOKEN='atlas-expo-go-demo';

export default function SignInScreen(){
 const[busy,setBusy]=useState(false);const[error,setError]=useState<string|null>(null);const[loginHint,setLoginHint]=useState('');
 async function signIn(){setBusy(true);setError(null);try{const accessToken=await authorizeWithProvider(loginHint);await saveSession({accessToken});router.replace('/create-business')}catch(reason){const code=reason instanceof Error?reason.message:'sign_in_failed';setError(code==='sign_in_cancelled'?'Sign-in was cancelled.':code==='identity_provider_unavailable'?'Sign-in is temporarily unavailable.':'We could not sign you in securely.')}finally{setBusy(false)}}
 async function signInForExpoGoTest(){setBusy(true);setError(null);try{await saveSession({accessToken:EXPO_GO_DEMO_TOKEN});router.replace('/create-business')}catch{setError('Expo Go test mode could not start.')}finally{setBusy(false)}}
 return <AtlasScreen contentStyle={s.container} keyboardShouldPersistTaps="handled">
   <View style={s.mintGlowA}/><View style={s.mintGlowB}/>
   <BrandMark size={72} style={s.logo}/>
   <Text accessibilityRole="header" style={s.title}>Welcome back 👋</Text>
   <Text style={s.subtitle}>Sign in to continue to Atlas</Text>

   <Text style={s.label}>Email or phone</Text>
   <View style={s.inputShell}><Text style={s.icon}>✉</Text><TextInput accessibilityLabel="Email or phone" autoCapitalize="none" autoCorrect={false} value={loginHint} onChangeText={setLoginHint} placeholder="you@company.com" placeholderTextColor="#83908A" style={s.input}/></View>

   <Text style={s.label}>Password</Text>
   <View style={s.inputShell}><Text style={s.icon}>▣</Text><Text style={s.password}>••••••••</Text><Text style={s.eye}>◉</Text></View>
   <Text style={s.forgot}>Forgot password?</Text>

   {error?<View style={s.errorBox}><Text style={s.error}>{error}</Text></View>:null}
   <Pressable disabled={busy} onPress={signIn} style={({pressed})=>[s.primary,pressed&&s.pressed,busy&&s.disabled]}>{busy?<ActivityIndicator color="#FFF"/>:<Text style={s.primaryText}>Sign in</Text>}</Pressable>
   {__DEV__?<Pressable accessibilityLabel="Continue in Expo Go test mode" disabled={busy} onPress={signInForExpoGoTest} style={({pressed})=>[s.demo,pressed&&s.pressed,busy&&s.disabled]}><Text style={s.demoText}>Continue in Expo Go test mode</Text></Pressable>:null}

   <View style={s.orRow}><View style={s.rule}/><Text style={s.orText}>or</Text><View style={s.rule}/></View>
   <View style={s.socialStack}><Social icon="G" label="Continue with Google" color="#4285F4"/><Social icon="●" label="Continue with Apple" color="#000"/><Social icon="▦" label="Continue with Microsoft" color="#00A4EF"/></View>
   <Text style={s.footer}>Don’t have an account? <Text style={s.footerStrong}>Create one</Text></Text>
 </AtlasScreen>
}
function Social({icon,label,color}:{icon:string;label:string;color:string}){return <View accessibilityRole="button" accessibilityState={{disabled:true}} style={s.social}><Text style={[s.socialIcon,{color}]}>{icon}</Text><Text style={s.socialText}>{label}</Text></View>}
const s=StyleSheet.create({
 container:{backgroundColor:'#FFF',overflow:'hidden'},
 mintGlowA:{position:'absolute',width:260,height:180,borderRadius:140,right:-65,top:-55,backgroundColor:'#EEF8F2'},mintGlowB:{position:'absolute',width:180,height:110,borderRadius:100,right:-30,top:45,backgroundColor:'#F6FBF8'},
 logo:{width:72,height:72,resizeMode:'contain',marginBottom:27},
 title:{fontFamily:'Georgia',fontSize:32,lineHeight:38,fontWeight:'800',letterSpacing:-.4,color:'#0A2F25'},subtitle:{fontSize:14,color:'#2F3F39',marginTop:8,marginBottom:34},
 label:{fontSize:12.5,fontWeight:'800',color:'#1C2924',marginBottom:8},
 inputShell:{minHeight:55,borderWidth:1,borderColor:'#DEE5E1',borderRadius:10,backgroundColor:'#FFF',flexDirection:'row',alignItems:'center',paddingHorizontal:14,marginBottom:22,shadowColor:'#163E2E',shadowOpacity:.025,shadowRadius:5,elevation:1},
 icon:{width:27,fontSize:16,color:'#68746E'},input:{flex:1,fontSize:13.5,color:'#22312C'},password:{flex:1,fontSize:16,letterSpacing:5,color:'#7D8782'},eye:{fontSize:15,color:'#7F8A85'},
 forgot:{alignSelf:'flex-end',marginTop:-11,marginBottom:24,color:GREEN,fontSize:11.5,fontWeight:'700'},
 errorBox:{padding:10,borderRadius:9,backgroundColor:'#FDECEC',marginBottom:12},error:{color:'#A1251B',fontSize:12},
 primary:{minHeight:55,borderRadius:10,backgroundColor:'#008A57',alignItems:'center',justifyContent:'center',shadowColor:'#00633F',shadowOpacity:.12,shadowRadius:8,shadowOffset:{width:0,height:4},elevation:2},primaryText:{color:'#FFF',fontSize:15.5,fontWeight:'800'},pressed:{opacity:.92},disabled:{opacity:.55},
 demo:{minHeight:46,borderRadius:10,borderWidth:1,borderColor:'#B8D9C8',backgroundColor:'#F3FAF6',alignItems:'center',justifyContent:'center',marginTop:10},demoText:{color:GREEN,fontSize:12.5,fontWeight:'800'},
 orRow:{flexDirection:'row',alignItems:'center',gap:14,marginVertical:20},rule:{flex:1,height:1,backgroundColor:'#E4E8E6'},orText:{color:'#637069',fontSize:12},
 socialStack:{gap:10},social:{minHeight:52,borderRadius:10,borderWidth:1,borderColor:'#E2E7E4',backgroundColor:'#FFF',flexDirection:'row',alignItems:'center',paddingHorizontal:16,shadowColor:'#173B2A',shadowOpacity:.018,shadowRadius:4,elevation:1},socialIcon:{width:34,fontSize:18,fontWeight:'900'},socialText:{fontSize:13,fontWeight:'700',color:'#202B27'},
 footer:{marginTop:'auto',textAlign:'center',fontSize:11.5,color:'#5E6964'},footerStrong:{color:GREEN,fontWeight:'800'}
});