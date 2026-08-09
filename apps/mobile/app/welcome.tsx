import { Image, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { router } from 'expo-router';

const GREEN = '#00754A';
const GREEN_BRIGHT = '#00A862';
const DARK = '#063A2E';
const LOGO = 'https://upload.wikimedia.org/wikipedia/en/thumb/d/d3/Starbucks_Corporation_Logo_2011.svg/512px-Starbucks_Corporation_Logo_2011.svg.png';

export default function WelcomeScreen() {
  return (
    <ScrollView contentContainerStyle={styles.container} showsVerticalScrollIndicator={false}>
      <View style={styles.leafOne}/><View style={styles.leafTwo}/><View style={styles.leafThree}/>
      <Image source={{ uri: LOGO }} style={styles.logo} accessibilityLabel="Starbucks logo" />
      <Text accessibilityRole="header" style={styles.title}>Work smarter.{`\n`}<Text style={styles.accent}>Grow faster.</Text></Text>
      <Text style={styles.subtitle}>Your AI co-pilot for finding insights and growing your business.</Text>

      <View style={styles.hero} accessibilityLabel="Coffee cup intelligence illustration">
        <View style={styles.platformOuter}><View style={styles.platformInner}/></View>
        <View style={styles.cupShadow}/>
        <View style={styles.cup}>
          <View style={styles.lidTop}/><View style={styles.lid}/>
          <Image source={{ uri: LOGO }} style={styles.cupLogo}/>
        </View>
        <View style={[styles.leaf,styles.l1]}/><View style={[styles.leaf,styles.l2]}/><View style={[styles.leaf,styles.l3]}/><View style={[styles.leaf,styles.l4]}/>
      </View>

      <View style={styles.dots}><View style={styles.dotActive}/><View style={styles.dot}/><View style={styles.dot}/></View>
      <Pressable accessibilityRole="button" onPress={() => router.push('/sign-in')} style={({pressed}) => [styles.primary,pressed&&styles.pressed]}>
        <Text style={styles.primaryText}>Get started</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles=StyleSheet.create({
  container:{flexGrow:1,minHeight:'100%',paddingHorizontal:32,paddingTop:72,paddingBottom:36,backgroundColor:DARK,overflow:'hidden'},
  leafOne:{position:'absolute',width:210,height:72,borderRadius:110,backgroundColor:'#0A4E3D',right:-80,top:90,transform:[{rotate:'27deg'}],opacity:.75},
  leafTwo:{position:'absolute',width:180,height:62,borderRadius:100,backgroundColor:'#0A4E3D',right:-50,top:165,transform:[{rotate:'-20deg'}],opacity:.5},
  leafThree:{position:'absolute',width:240,height:90,borderRadius:120,backgroundColor:'#0B4939',left:-130,bottom:230,transform:[{rotate:'32deg'}],opacity:.72},
  logo:{width:82,height:82,resizeMode:'contain',marginBottom:27},
  title:{fontFamily:'Georgia',fontSize:39,lineHeight:46,fontWeight:'800',letterSpacing:-.7,color:'#FFFFFF'},accent:{color:'#35C984'},
  subtitle:{marginTop:20,maxWidth:286,fontSize:16,lineHeight:25,color:'#FFFFFF',opacity:.94},
  hero:{height:350,marginTop:8,alignItems:'center',justifyContent:'center'},
  platformOuter:{position:'absolute',bottom:35,width:255,height:56,borderRadius:128,backgroundColor:'#0A2821',borderWidth:1,borderColor:'#356858',alignItems:'center',justifyContent:'center'},platformInner:{width:220,height:32,borderRadius:110,backgroundColor:'#123C31'},cupShadow:{position:'absolute',bottom:76,width:160,height:26,borderRadius:80,backgroundColor:'#061F19',opacity:.55},
  cup:{width:142,height:197,borderBottomLeftRadius:31,borderBottomRightRadius:31,borderTopLeftRadius:20,borderTopRightRadius:20,backgroundColor:'#F5F0E5',alignItems:'center',justifyContent:'center',shadowColor:'#000',shadowOpacity:.25,shadowRadius:18,elevation:7},
  lidTop:{position:'absolute',top:-22,width:128,height:23,borderRadius:13,backgroundColor:'#FBF9F3'},lid:{position:'absolute',top:-8,width:151,height:29,borderRadius:16,backgroundColor:'#F1EEE7',borderWidth:1,borderColor:'#D7D4CD'},cupLogo:{width:75,height:75,resizeMode:'contain'},
  leaf:{position:'absolute',width:82,height:28,borderRadius:50,backgroundColor:'#195D3E'},l1:{left:6,bottom:80,transform:[{rotate:'35deg'}]},l2:{left:16,bottom:125,transform:[{rotate:'-32deg'}]},l3:{right:8,bottom:90,transform:[{rotate:'-35deg'}]},l4:{right:16,bottom:138,transform:[{rotate:'30deg'}]},
  dots:{flexDirection:'row',justifyContent:'center',gap:10,marginTop:-3,marginBottom:35},dotActive:{width:9,height:9,borderRadius:5,backgroundColor:'#FFFFFF'},dot:{width:9,height:9,borderRadius:5,backgroundColor:'#FFFFFF',opacity:.35},
  primary:{minHeight:58,borderRadius:10,backgroundColor:GREEN_BRIGHT,alignItems:'center',justifyContent:'center',shadowColor:'#000',shadowOpacity:.18,shadowRadius:10,elevation:4},primaryText:{color:'#FFFFFF',fontSize:17,fontWeight:'800'},pressed:{opacity:.92,transform:[{scale:.99}]}
});