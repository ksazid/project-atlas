import { StatusBar } from 'expo-status-bar';
import { TodayFocusScreen } from '@/features/today-focus/TodayFocusScreen';

export default function HomeScreen() {
  return <><TodayFocusScreen /><StatusBar style="auto" /></>;
}
