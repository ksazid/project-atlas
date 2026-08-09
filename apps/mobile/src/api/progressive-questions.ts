import { env } from '@/lib/env';
import type {
  ProgressiveQuestionAnswerRequest,
  ProgressiveQuestionSet,
} from '@/features/progressive-questions/progressive-question-model';

export type ProgressiveQuestionMutation = {
  status: 'answered' | 'skipped';
  questionKey: string;
  catalogueVersion: string;
  remaining: ProgressiveQuestionSet;
};

export class ProgressiveQuestionApiError extends Error {
  constructor(public readonly code: string, message: string) {
    super(message);
    this.name = 'ProgressiveQuestionApiError';
  }
}

async function problemFor(response: Response, fallback: string): Promise<never> {
  const problem = (await response.json().catch(() => null)) as { message?: string; title?: string; code?: string } | null;
  throw new ProgressiveQuestionApiError(
    problem?.code ?? `http_${response.status}`,
    problem?.message ?? problem?.title ?? fallback,
  );
}

function headers(accessToken: string): HeadersInit {
  return {
    Accept: 'application/json',
    'Content-Type': 'application/json',
    Authorization: `Bearer ${accessToken}`,
  };
}

export async function getProgressiveQuestions(
  accessToken: string,
  businessId: string,
): Promise<ProgressiveQuestionSet> {
  const response = await fetch(`${env.apiUrl}/api/v1/businesses/${encodeURIComponent(businessId)}/progressive-questions`, {
    headers: headers(accessToken),
  });
  if (!response.ok) return problemFor(response, 'Atlas could not load the optional business questions.');
  return (await response.json()) as ProgressiveQuestionSet;
}

export async function answerProgressiveQuestion(
  accessToken: string,
  businessId: string,
  questionKey: string,
  request: ProgressiveQuestionAnswerRequest,
): Promise<ProgressiveQuestionMutation> {
  const response = await fetch(`${env.apiUrl}/api/v1/businesses/${encodeURIComponent(businessId)}/progressive-questions/${encodeURIComponent(questionKey)}/answer`, {
    method: 'POST',
    headers: headers(accessToken),
    body: JSON.stringify(request),
  });
  if (!response.ok) return problemFor(response, 'Atlas could not save that answer.');
  return (await response.json()) as ProgressiveQuestionMutation;
}

export async function skipProgressiveQuestion(
  accessToken: string,
  businessId: string,
  questionKey: string,
  catalogueVersion: string,
): Promise<ProgressiveQuestionMutation> {
  const response = await fetch(`${env.apiUrl}/api/v1/businesses/${encodeURIComponent(businessId)}/progressive-questions/${encodeURIComponent(questionKey)}/skip`, {
    method: 'POST',
    headers: headers(accessToken),
    body: JSON.stringify({ catalogueVersion }),
  });
  if (!response.ok) return problemFor(response, 'Atlas could not skip that question.');
  return (await response.json()) as ProgressiveQuestionMutation;
}
