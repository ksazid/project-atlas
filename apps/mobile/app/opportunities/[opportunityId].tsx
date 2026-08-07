import { useLocalSearchParams } from 'expo-router';
import { OpportunityDetailScreen } from '@/features/opportunity-detail/OpportunityDetailScreen';

export default function OpportunityDetailRoute() {
  const { opportunityId } = useLocalSearchParams<{ opportunityId: string }>();
  return <OpportunityDetailScreen opportunityId={opportunityId} />;
}
