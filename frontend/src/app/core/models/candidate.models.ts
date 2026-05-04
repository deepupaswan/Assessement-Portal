import { AssessmentQuestion } from './assessment.models';
import type { CandidateAssessmentStatus } from '../../constants/candidate.constants';

// Re-export constants from centralized location
export type { CandidateAssessmentStatus } from '../../constants/candidate.constants';
export { CandidateAssessmentStatusValues } from '../../constants/candidate.constants';

export interface Candidate {
  id: string;
  name: string;
  email: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CandidateAssignment {
  candidateAssessmentId: string;
  candidateId?: string;
  candidateName?: string;
  assessmentId: string;
  assessmentTitle: string;
  status: CandidateAssessmentStatus;
  assignedAtUtc?: string;
  scheduledAtUtc?: string;
  startTimeUtc?: string;
  startedAtUtc?: string;
  submittedAtUtc?: string;
  remainingSeconds?: number;
}

export interface CandidateAssessmentSession {
  candidateAssessmentId: string;
  candidateId: string;
  assessmentId: string;
  assessmentTitle: string;
  durationMinutes: number;
  remainingSeconds: number;
  allowedViolations: number;
  questions: AssessmentQuestion[];
}

export interface AssessmentProgress {
  candidateAssessmentId: string;
  candidateName: string;
  status: CandidateAssessmentStatus;
  completionPercent: number;
  suspiciousEvents: number;
  remainingSeconds: number;
}

export interface SuspiciousActivityRequest {
  candidateAssessmentId: string;
  violationType: string;
  metadata?: string;
}
