/**
 * Admin Dashboard constants for forms and messages
 */

export const AdminDashboardMessages = {
  MarkCorrectOption: 'Mark one option as correct.',
  UnableCreateAssessment: 'Unable to create assessment.',
  UnableLoadQuestions: 'Unable to load questions.',
  UnableAddQuestion: 'Unable to add question.',
  UnableCreateCandidate: 'Unable to create candidate.',
  CandidateCreatedSelected: 'Candidate created and selected.',
  UnableAssignAssessment: 'Unable to assign assessment.',
  UnableLoadDashboard: 'Unable to load dashboard.',
  UnableLoadCandidates: 'Unable to load candidates.',
  CandidateSelected: (name: string) => `Selected candidate: ${name}.`,
  ActivityAssessmentCreated: 'Assessment created successfully.',
  ActivityQuestionAdded: 'Question added successfully.',
  ActivityAssessmentAssigned: 'Assessment assigned to candidate.',
  ActivityProgressUpdate: (name: string, percent: number) =>
    `Progress update: ${name} is ${percent}% complete.`,
  ActivitySuspicious: (detail: string) => `Suspicious activity: ${detail}`,
  SuspiciousDetail: (candidateName: string, violationType: string) =>
    `${candidateName} triggered ${violationType}`,
  FallbackCandidate: 'Candidate',
  FallbackAssessment: 'Assessment',
  DefaultViolationLabel: 'an alert'
} as const;
