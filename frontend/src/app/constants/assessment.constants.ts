/**
 * Assessment-related constants for question types and their labels
 */

export type QuestionType = 'MCQ' | 'DESCRIPTIVE' | 'CODING';

export const QuestionTypeValues = {
  Mcq: 'MCQ',
  Descriptive: 'DESCRIPTIVE',
  Coding: 'CODING'
} as const;

export const QuestionTypeLabels: Record<QuestionType, string> = {
  [QuestionTypeValues.Mcq]: 'Multiple Choice',
  [QuestionTypeValues.Descriptive]: 'Descriptive',
  [QuestionTypeValues.Coding]: 'Coding'
} as const;
