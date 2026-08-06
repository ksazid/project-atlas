import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { getProfile, saveProfile, type BusinessProfile } from '@/api/atlas-client';
import { loadSession } from '@/auth/session';

const empty: BusinessProfile = { description: '', address: '', website: '', phone: '', email: '', socialChannels: '', businessHours: '', language: 'English', source: 'owner', ownerConfirmed: true };

export default function ProfileScreen() {
  const [form, setForm] = useState(empty); const [busy, setBusy] = useState(true); const [message, setMessage] = useState<string | null>(null);
  useEffect(() => { void (async () => { try { const s = await loadSession(); if (s?.businessId) { const profile = await getProfile(s.accessToken, s.businessId); if (profile) setForm(profile); } } catch { setMessage('Profile is unavailable. You can retry later.'); } finally { setBusy(false); } })(); }, []);
  async function submit() { setBusy(true); setMessage(null); try { const s = await loadSession(); if (!s?.businessId) throw new Error('Business session is missing.'); setForm(await saveProfile(s.accessToken, s.businessId, form)); setMessage('Profile saved.'); } catch (e) { setMessage(e instanceof Error ? e.message : 'Could not save profile.'); } finally { setBusy(false); } }
  if (busy && !form.description) return <View style={styles.center}><ActivityIndicator /><Text>Loading profile…</Text></View>;
  return <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled"><Text accessibilityRole="header" style={styles.title}>Business profile</Text><Text style={styles.help}>Keep Atlas grounded in confirmed business facts.</Text>{(['description','address','website','phone','email','socialChannels','businessHours','language'] as const).map((key)=><View key={key} style={styles.field}><Text style={styles.label}>{key.replace(/([A-Z])/g,' $1')}</Text><TextInput accessibilityLabel={key} multiline={key==='description'||key==='businessHours'} onChangeText={(v)=>setForm(c=>({...c,[key]:v}))} style={styles.input} value={form[key]} /></View>)}{message?<Text accessibilityLiveRegion="polite">{message}</Text>:null}<Pressable accessibilityRole="button" disabled={busy} onPress={submit} style={styles.button}><Text style={styles.buttonText}>{busy?'Saving…':'Save profile'}</Text></Pressable></ScrollView>;
}
const styles=StyleSheet.create({container:{padding:20,gap:14},center:{flex:1,alignItems:'center',justifyContent:'center',gap:12},title:{fontSize:28,fontWeight:'700'},help:{fontSize:16,lineHeight:23},field:{gap:6},label:{fontWeight:'600',textTransform:'capitalize'},input:{minHeight:48,borderWidth:1,borderRadius:12,padding:12,fontSize:16},button:{minHeight:50,borderRadius:12,backgroundColor:'#111827',alignItems:'center',justifyContent:'center',marginBottom:30},buttonText:{color:'#fff',fontWeight:'700'}});
