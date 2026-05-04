// Re-export constants from centralized location
import type { QuestionType } from '../../constants/assessment.constants';
export type { QuestionType } from '../../constants/assessment.constants';
export { QuestionTypeValues, QuestionTypeLabels } from '../../constants/assessment.constants';

export interface AssessmentSummary {
  id: string;
  title: string;
  description?: string;
  durationMinutes: number;
  questionCount: number;
  isPublished: boolean;
  isActive?: boolean;
  createdAt?: string | Date;
  updatedAt?: string | Date;
}

export interface Assessment extends AssessmentSummary {
  // Alias for compatibility
}

export interface QuestionOption {
  id: string;
  text: string;
  isCorrect?: boolean;
  order?: number;
}

export interface AssessmentQuestion {
  id: string;
  prompt?: string;
  text?: string;
  questionType?: QuestionType;
  type?: QuestionType;
  marks?: number;
  maxScore?: number;
  options: QuestionOption[];
}

export interface AssessmentDetail {
  id: string;
  title: string;
  durationMinutes: number;
  randomizeQuestions: boolean;
  questions: AssessmentQuestion[];
}

export interface CreateAssessmentRequest {
  title: string;
  description?: string;
  durationMinutes: number;
  randomizeQuestions: boolean;
}

export interface CreateQuestionOptionRequest {
  text: string;
  isCorrect: boolean;
  order: number;
}

export interface CreateQuestionRequest {
  text: string;
  type: QuestionType;
  maxScore: number;
  correctAnswer?: string;
  isRequired: boolean;
  order: number;
  options: CreateQuestionOptionRequest[];
}

export interface AssignmentRequest {
  candidateId: string;
  assessmentId: string;
  scheduledAtUtc?: string;
}
