import { AssessmentQuestion, QuestionType } from '../../../core/models/assessment.models';

export interface QuestionRow extends AssessmentQuestion {
  isDeleting?: boolean;
  isEditing?: boolean;
}

export interface QuestionForm {
  text?: string;
  type?: QuestionType;
  marks?: number;
  options?: Array<{ text: string; isCorrect?: boolean }>;
  correctAnswer?: string;
  codeTemplate?: string;
  expectedOutput?: string;
}
