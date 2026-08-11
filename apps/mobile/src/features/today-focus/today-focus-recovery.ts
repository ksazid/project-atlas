export type TodayFocusRecoveryAction = {
  label: string;
  route: '/goals' | '/profile';
};

const contextRecovery: TodayFocusRecoveryAction = {
  label: 'Review business context',
  route: '/profile',
};

export function todayFocusRecoveryAction(code?: string): TodayFocusRecoveryAction {
  if (code === 'opportunity_goal_missing') {
    return {
      label: 'Choose your first goal',
      route: '/goals',
    };
  }

  return contextRecovery;
}
