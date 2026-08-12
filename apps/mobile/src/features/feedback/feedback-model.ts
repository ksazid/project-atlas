import type { FeedbackInput, FeedbackKind } from '@/api/atlas-client';

export type FeedbackScreenKind = Exclude<FeedbackKind, 'opportunity-rating'>;
export type FeedbackDraft = {
  kind: FeedbackScreenKind;
  opportunityId?: string;
  contextKey?: string;
  message?: string;
};

export const feedbackChoices: readonly { kind: FeedbackScreenKind; label: string; description: string }[] = [
  { kind: 'incorrect-context', label: 'Incorrect business context', description: 'Tell Atlas when saved business context does not reflect how your business works.' },
  { kind: 'unsafe-guidance', label: 'Unsafe guidance', description: 'Report guidance that may be unsafe, inappropriate, or unsuitable for your business.' },
  { kind: 'general-feedback', label: 'General feedback', description: 'Share product feedback about your Atlas experience.' },
  { kind: 'support-request', label: 'Support request', description: 'Ask for help when you cannot resolve something in Atlas.' },
];

export function normalizeFeedbackMessage(value?: string): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

export function validateFeedbackDraft(draft: FeedbackDraft): { valid: boolean; message?: string } {
  if (!feedbackChoices.some(choice => choice.kind === draft.kind)) {
    return { valid: false, message: 'Choose a feedback type.' };
  }
  const message = normalizeFeedbackMessage(draft.message);
  if (message && message.length > 1200) {
    return { valid: false, message: 'Keep your note to 1200 characters or fewer.' };
  }
  return { valid: true };
}

export function buildFeedbackInput(draft: FeedbackDraft): FeedbackInput {
  const message = normalizeFeedbackMessage(draft.message);
  return {
    kind: draft.kind,
    ...(draft.opportunityId ? { opportunityId: draft.opportunityId } : {}),
    ...(draft.contextKey ? { contextKey: draft.contextKey.trim() } : {}),
    ...(message ? { message } : {}),
  };
}

export function getFeedbackCopy(kind: FeedbackScreenKind): { title: string; body: string; success: string } {
  switch (kind) {
    case 'incorrect-context':
      return {
        title: 'Report incorrect business context',
        body: 'Report a problem here, and use the Context editor to correct anything you want Atlas to use going forward.',
        success: 'Your context report was recorded for review.',
      };
    case 'unsafe-guidance':
      return {
        title: 'Report unsafe guidance',
        body: 'Tell us what concerned you. Reporting does not automatically remove or change the Opportunity.',
        success: 'Your safety report was recorded for review.',
      };
    case 'support-request':
      return {
        title: 'Request support',
        body: 'Describe what you need help with and Atlas will record the request for review.',
        success: 'Your support request was recorded.',
      };
    default:
      return {
        title: 'Share general feedback',
        body: 'Tell us what would make Atlas more useful for your business.',
        success: 'Your feedback was recorded.',
      };
  }
}
