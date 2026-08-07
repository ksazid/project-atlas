import { StatusBar } from 'expo-status-bar';
import { WeeklyReviewScreen } from '@/features/weekly-review/WeeklyReviewScreen';

export default function WeeklyReviewRoute() {
  return <><WeeklyReviewScreen /><StatusBar style="auto" /></>;
}
