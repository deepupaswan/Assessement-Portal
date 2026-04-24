export interface AnswerSaveRequest {
  questionId: string;
  selectedOptionId?: string;
  descriptiveAnswer?: string;
  codingAnswer?: string;
  autoSaved: boolean;
}

export interface BulkAnswerSaveRequest {
  assessmentId: string;
  candidateId: string;
  answers: AnswerSaveRequest[];
}
