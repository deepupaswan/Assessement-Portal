import { AssessmentQuestion } from './assessment.models';

export type CandidateAssessmentStatus = 'Assigned' | 'InProgress' | 'Submitted' | 'Evaluated';

export interface Candidate {
  id: string;
  name: string;
  email: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CandidateAssignment {
  candidateAssessmentId: string;
  assessmentId: string;
  assessmentTitle: string;
  status: CandidateAssessmentStatus;
  startTimeUtc?: string;
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
