import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';
import { BrandMark } from '@/components/BrandMark';

const DARK = '#003B2F';

export default function WelcomeScreen() {
  return (
    <ScrollView contentContainerStyle={s.container} showsVerticalScrollIndicator={false}>
      <View style={[s.leaf,s.leafTopA]}/><View style={[s.leaf,s.leafTopB]}/><View style={[s.leaf,s.leafLeftA]}/><View style={[s.leaf,s.leafLeftB]}/><View style={[s.leaf,s.leafRightA]}/><View style={[s.leaf,s.leafRightB]}/>

      <BrandMark size={78} style={s.logo}/>

      <Text accessibilityRole="header" style={s.title}>Work smarter.{`\n`}<Text style={s.accent}>Grow faster.</Text></Text>
      <Text style={s.subtitle}>Your AI co-pilot for finding{`\n`}insights and growing your{`\n`}business.</Text>

      <View style={s.hero} accessibilityLabel="Atlas hero illustration">
        <View style={s.platformGlow}/><View style={s.platform}/><View style={s.platformTop}/>
        <View style={s.cupShadow}/>
        <View style={s.cup}>
          <View style={s.lidBack}/><View style={s.lid}/><View style={s.lidLip}/>
          <BrandMark decorative size={78} style={s.cupLogo}/>
        </View>
      </View>

      <View style={s.dots}><View style={s.dotActive}/><View style={s.dot}/><View style={s.dot}/></View>
      <Pressable accessibilityRole="button" onPress={()=>router.push('/sign-in')} style={({pressed})=>[s.cta,pressed&&s.pressed]}>
        <Text style={s.ctaText}>Get started</Text>
      </Pressable>
    </ScrollView>
  );
}

const s=StyleSheet.create({
  container:{flexGrow:1,minHeight:'100%',paddingHorizontal:31,paddingTop:74,paddingBottom:32,backgroundColor:DARK,overflow:'hidden'},
  logo:{width:78,height:78,resizeMode:'contain',marginBottom:31},
  title:{fontFamily:'Georgia',fontSize:37,lineHeight:43,fontWeight:'800',letterSpacing:-.7,color:'#FFF'},
  accent:{color:'#32C987'},
  subtitle:{marginTop:19,fontSize:16,lineHeight:24.5,color:'#FFF'},
  hero:{height:330,marginTop:13,alignItems:'center',justifyContent:'center'},
  leaf:{position:'absolute',borderRadius:999,backgroundColor:'#0A5946',opacity:.78},
  leafTopA:{width:190,height:64,right:-72,top:132,transform:[{rotate:'33deg'}]},leafTopB:{width:162,height:54,right:-55,top:192,transform:[{rotate:'-18deg'}]},
  leafLeftA:{width:130,height:42,left:-47,top:545,transform:[{rotate:'48deg'}]},leafLeftB:{width:124,height:42,left:-39,top:613,transform:[{rotate:'-36deg'}]},
  leafRightA:{width:150,height:48,right:-55,top:524,transform:[{rotate:'-48deg'}]},leafRightB:{width:132,height:44,right:-40,top:596,transform:[{rotate:'38deg'}]},
  platformGlow:{position:'absolute',bottom:20,width:288,height:72,borderRadius:144,backgroundColor:'#0B5E49',opacity:.30},
  platform:{position:'absolute',bottom:30,width:250,height:62,borderRadius:125,backgroundColor:'#0A2B24',borderWidth:1,borderColor:'#285C4D'},
  platformTop:{position:'absolute',bottom:52,width:211,height:32,borderRadius:106,backgroundColor:'#164D3D'},
  cupShadow:{position:'absolute',bottom:75,width:165,height:27,borderRadius:83,backgroundColor:'#041E18',opacity:.55},
  cup:{position:'absolute',bottom:69,width:143,height:190,borderTopLeftRadius:17,borderTopRightRadius:17,borderBottomLeftRadius:25,borderBottomRightRadius:25,backgroundColor:'#F3EBDD',alignItems:'center',justifyContent:'center',shadowColor:'#000',shadowOpacity:.25,shadowRadius:20,shadowOffset:{width:0,height:10},elevation:7},
  lidBack:{position:'absolute',top:-24,width:120,height:25,borderRadius:14,backgroundColor:'#FBF8F0'},
  lid:{position:'absolute',top:-11,width:153,height:28,borderRadius:15,backgroundColor:'#F7F3E9',borderWidth:1,borderColor:'#D9D4CA'},
  lidLip:{position:'absolute',top:8,width:132,height:9,borderRadius:5,backgroundColor:'#E8E0D2'},
  cupLogo:{width:78,height:78,resizeMode:'contain',marginTop:14},
  dots:{flexDirection:'row',justifyContent:'center',gap:11,marginTop:-2,marginBottom:36},dotActive:{width:9,height:9,borderRadius:5,backgroundColor:'#FFF'},dot:{width:9,height:9,borderRadius:5,backgroundColor:'#839C94'},
  cta:{minHeight:57,borderRadius:10,backgroundColor:'#008F5B',alignItems:'center',justifyContent:'center',shadowColor:'#001D17',shadowOpacity:.23,shadowRadius:12,shadowOffset:{width:0,height:7},elevation:4},ctaText:{color:'#FFF',fontSize:16,fontWeight:'800'},pressed:{opacity:.92,transform:[{scale:.99}]}
});
