import { StatusBar } from 'expo-status-bar';
import { KnowledgePackScreen } from '@/features/knowledge-pack/KnowledgePackScreen';

export default function HomeScreen() {
  return <><KnowledgePackScreen /><StatusBar style="auto" /></>;
}
