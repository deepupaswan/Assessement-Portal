import { AssignmentFilterStatus } from '../../../constants/assignment-filters.constants';

export interface AssignmentRow {
  id: string;
  candidateAssessmentId: string;
  candidateId: string;
  candidateName: string;
  assessmentId: string;
  assessmentTitle: string;
  status: string;
  scheduledAt?: string;
  createdAt?: string;
  startedAt?: string;
  submittedAt?: string;
}

export interface AssignmentForm {
  candidateId: string;
  assessmentId: string;
  scheduledAtUtc?: string;
}
