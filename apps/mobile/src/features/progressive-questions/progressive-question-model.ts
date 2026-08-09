export type ProgressiveQuestion = {
  questionKey: string;
  targetContextKey: string;
  prompt: string;
  helper?: string | null;
  answerType: 'single-choice' | 'multi-choice' | 'short-text';
  options: string[];
  maxSelections?: number | null;
  maxLength?: number | null;
};

export type ProgressiveQuestionSet = {
  catalogueKey: string;
  catalogueVersion: string;
  questions: ProgressiveQuestion[];
};

export type ProgressiveQuestionAnswerDraft = {
  selections: string[];
  text: string;
};

export type ProgressiveQuestionAnswerRequest = {
  catalogueVersion: string;
  selections: string[] | null;
  text: string | null;
};

export function createAnswerDraft(_question: ProgressiveQuestion): ProgressiveQuestionAnswerDraft {
  return { selections: [], text: '' };
}

export function toggleSelection(
  draft: ProgressiveQuestionAnswerDraft,
  option: string,
  question: ProgressiveQuestion,
): ProgressiveQuestionAnswerDraft {
  if (!question.options.includes(option)) return { ...draft, selections: [...draft.selections] };

  if (question.answerType === 'single-choice') {
    return { ...draft, selections: [option] };
  }

  if (question.answerType !== 'multi-choice') return { ...draft, selections: [...draft.selections] };

  if (draft.selections.includes(option)) {
    return { ...draft, selections: draft.selections.filter(value => value !== option) };
  }

  const maximum = question.maxSelections ?? question.options.length;
  if (draft.selections.length >= maximum) return { ...draft, selections: [...draft.selections] };
  return { ...draft, selections: [...draft.selections, option] };
}

export function updateText(draft: ProgressiveQuestionAnswerDraft, text: string): ProgressiveQuestionAnswerDraft {
  return { ...draft, selections: [...draft.selections], text };
}

export function canContinue(question: ProgressiveQuestion, draft: ProgressiveQuestionAnswerDraft): boolean {
  if (question.answerType === 'short-text') {
    const length = draft.text.trim().length;
    return length > 0 && length <= (question.maxLength ?? 240);
  }

  const maximum = question.answerType === 'single-choice' ? 1 : question.maxSelections ?? question.options.length;
  return draft.selections.length > 0 && draft.selections.length <= maximum;
}

export function buildAnswerRequest(
  catalogueVersion: string,
  question: ProgressiveQuestion,
  draft: ProgressiveQuestionAnswerDraft,
): ProgressiveQuestionAnswerRequest {
  if (question.answerType === 'short-text') {
    return { catalogueVersion, selections: null, text: draft.text.trim() };
  }
  return { catalogueVersion, selections: [...draft.selections], text: null };
}

export function getProgressLabel(index: number, total: number): string {
  if (total <= 0) return '0 of 0';
  const current = Math.min(Math.max(index, 0), total - 1) + 1;
  return `${current} of ${total}`;
}
