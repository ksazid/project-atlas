import { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { getContext, saveContext, type BusinessContextEntry } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';

const keys = ['customers','busyPeriods','constraints','currentPriorities'] as const;
const initial: BusinessContextEntry[] = keys.map((key)=>({key,value:'',source:'owner',ownerConfirmed:true}));
export default function ContextScreen(){
  const [entries,setEntries]=useState(initial);const[message,setMessage]=useState<string|null>(null);
  useEffect(()=>{void(async()=>{try{const s=await loadSession();if(s?.businessId){const existing=await getContext(s.accessToken,s.businessId);if(existing.length)setEntries(existing);}}catch{setMessage('Context is unavailable. You can continue with limited data.');}})();},[]);
  function update(key:string,value:string){setEntries(current=>{const found=current.some(x=>x.key===key);return found?current.map(x=>x.key===key?{...x,value}:x):[...current,{key,value,source:'owner',ownerConfirmed:true}];});}
  async function submit(){setMessage(null);try{const s=await loadSession();if(!s?.businessId)throw new Error('Business session is missing.');setEntries(await saveContext(s.accessToken,s.businessId,entries.filter(x=>x.value.trim())));setMessage('Context saved.');}catch(e){setMessage(e instanceof Error?e.message:'Could not save context.');}}
  return <ScrollView contentContainerStyle={styles.container}><Text accessibilityRole="header" style={styles.title}>Business context</Text><Text style={styles.help}>Share only what helps Atlas give better guidance. Empty fields are allowed.</Text>{keys.map(key=><View key={key} style={styles.field}><Text style={styles.label}>{key.replace(/([A-Z])/g,' $1')}</Text><TextInput accessibilityLabel={key} multiline onChangeText={(v)=>update(key,v)} placeholder="Optional" style={styles.input} value={entries.find(x=>x.key===key)?.value??''}/></View>)}{message?<Text accessibilityLiveRegion="polite">{message}</Text>:null}<Pressable accessibilityRole="button" onPress={submit} style={styles.button}><Text style={styles.buttonText}>Save context</Text></Pressable></ScrollView>;
}
const styles=StyleSheet.create({container:{padding:20,gap:14},title:{fontSize:28,fontWeight:'700'},help:{fontSize:16,lineHeight:23},field:{gap:6},label:{fontWeight:'600',textTransform:'capitalize'},input:{minHeight:86,borderWidth:1,borderRadius:12,padding:12,fontSize:16,textAlignVertical:'top'},button:{minHeight:50,borderRadius:12,backgroundColor:'#111827',alignItems:'center',justifyContent:'center',marginBottom:30},buttonText:{color:'#fff',fontWeight:'700'}});
