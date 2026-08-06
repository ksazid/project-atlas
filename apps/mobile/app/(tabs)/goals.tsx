import { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { getGoals, saveGoals, type BusinessGoal } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';

const defaults: BusinessGoal[] = [
  { title: 'Increase revenue', category: 'revenue', priority: 1, isCustom: false },
  { title: 'Improve profitability', category: 'profitability', priority: 2, isCustom: false },
  { title: 'Save owner time', category: 'efficiency', priority: 3, isCustom: false },
];
export default function GoalsScreen(){
  const [goals,setGoals]=useState<BusinessGoal[]>(defaults); const [custom,setCustom]=useState(''); const [message,setMessage]=useState<string|null>(null);
  useEffect(()=>{void(async()=>{try{const s=await loadSession();if(s?.businessId){const existing=await getGoals(s.accessToken,s.businessId);if(existing.length)setGoals(existing);}}catch{setMessage('Goals are unavailable. Default goals are shown.');}})();},[]);
  function move(index:number,delta:number){const next=[...goals];const target=index+delta;if(target<0||target>=next.length)return;[next[index],next[target]]=[next[target],next[index]];setGoals(next.map((g,i)=>({...g,priority:i+1})));}
  function add(){const title=custom.trim();if(!title)return;setGoals(g=>[...g,{title,category:'custom',priority:g.length+1,isCustom:true}]);setCustom('');}
  async function submit(){setMessage(null);try{const s=await loadSession();if(!s?.businessId)throw new Error('Business session is missing.');setGoals(await saveGoals(s.accessToken,s.businessId,goals));setMessage('Goals saved.');}catch(e){setMessage(e instanceof Error?e.message:'Could not save goals.');}}
  return <ScrollView contentContainerStyle={styles.container}><Text accessibilityRole="header" style={styles.title}>Business goals</Text><Text style={styles.help}>Rank what matters most. Atlas will use this order when evaluating future opportunities.</Text>{goals.map((g,i)=><View key={`${g.title}-${i}`} style={styles.card}><View style={{flex:1}}><Text style={styles.goal}>{i+1}. {g.title}</Text><Text>{g.category}{g.isCustom?' · custom':''}</Text></View><Pressable accessibilityLabel={`Move ${g.title} up`} onPress={()=>move(i,-1)} style={styles.small}><Text>↑</Text></Pressable><Pressable accessibilityLabel={`Move ${g.title} down`} onPress={()=>move(i,1)} style={styles.small}><Text>↓</Text></Pressable></View>)}<View style={styles.row}><TextInput accessibilityLabel="Custom goal" onChangeText={setCustom} placeholder="Add a custom goal" style={[styles.input,{flex:1}]} value={custom}/><Pressable onPress={add} style={styles.add}><Text style={styles.buttonText}>Add</Text></Pressable></View>{message?<Text accessibilityLiveRegion="polite">{message}</Text>:null}<Pressable accessibilityRole="button" onPress={submit} style={styles.button}><Text style={styles.buttonText}>Save goals</Text></Pressable></ScrollView>;
}
const styles=StyleSheet.create({container:{padding:20,gap:14},title:{fontSize:28,fontWeight:'700'},help:{fontSize:16,lineHeight:23},card:{borderWidth:1,borderRadius:12,padding:14,flexDirection:'row',alignItems:'center',gap:8},goal:{fontSize:16,fontWeight:'700'},row:{flexDirection:'row',gap:8},input:{minHeight:48,borderWidth:1,borderRadius:12,paddingHorizontal:12,fontSize:16},small:{minWidth:42,minHeight:42,borderWidth:1,borderRadius:10,alignItems:'center',justifyContent:'center'},add:{minWidth:72,borderRadius:12,backgroundColor:'#374151',alignItems:'center',justifyContent:'center'},button:{minHeight:50,borderRadius:12,backgroundColor:'#111827',alignItems:'center',justifyContent:'center',marginBottom:30},buttonText:{color:'#fff',fontWeight:'700'}});
