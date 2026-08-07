import { useLocalSearchParams } from 'expo-router';
import { ExecutionKitScreen } from '@/features/execution-kit/ExecutionKitScreen';

export default function ExecutionKitRoute() {
  const { opportunityId } = useLocalSearchParams<{ opportunityId: string }>();
  return <ExecutionKitScreen opportunityId={opportunityId} />;
}
