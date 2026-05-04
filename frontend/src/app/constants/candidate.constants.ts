/**
 * Candidate-related constants for assessment statuses
 */

export type CandidateAssessmentStatus =
  | 'Scheduled'
  | 'Assigned'
  | 'InProgress'
  | 'Submitted'
  | 'Evaluated';

export const CandidateAssessmentStatusValues = {
  Scheduled: 'Scheduled',
  Assigned: 'Assigned',
  InProgress: 'InProgress',
  Submitted: 'Submitted',
  Evaluated: 'Evaluated'
} as const;
