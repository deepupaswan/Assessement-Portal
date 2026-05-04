/**
 * Assignments component constants for filters, messages, and confirmations
 */

export const AssignmentsMessages = {
  LoadError: 'Failed to load assignments',
  LoadCandidatesError: 'Failed to load candidates',
  LoadAssessmentsError: 'Failed to load assessments',
  RequiredFields: 'Candidate and assessment are required',
  CreateError: 'Failed to create assignment',
  UpdateError: 'Failed to update assignment',
  DeleteError: 'Failed to delete assignment',
  BulkAssignError: 'Failed to bulk assign assessments',
  DeleteConfirm: 'Are you sure you want to delete this assignment?',
  RequiredAssessment: 'Assessment is required',
  RequiredCandidates: 'Select at least one candidate',
  FallbackCandidateName: 'Candidate'
} as const;
