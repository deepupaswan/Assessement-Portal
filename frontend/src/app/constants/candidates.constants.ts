/**
 * Candidates management constants for filters, statuses, and messages
 */

export type CandidateFilterStatus = 'all' | 'active' | 'assigned';

export const CandidateFilterStatusValues = {
  All: 'all',
  Active: 'active',
  Assigned: 'assigned'
} as const;

export const CandidateFilterStatusLabels: Record<CandidateFilterStatus, string> = {
  [CandidateFilterStatusValues.All]: 'All',
  [CandidateFilterStatusValues.Active]: 'Active',
  [CandidateFilterStatusValues.Assigned]: 'Assigned'
} as const;

export const CandidateStatusLabels = {
  Active: 'Active',
  Inactive: 'Inactive'
} as const;

export const CandidateMessages = {
  LoadError: 'Failed to load candidates',
  LoadAssessmentsError: 'Failed to load assessments',
  NameEmailRequired: 'Name and email are required',
  InvalidEmailFormat: 'Invalid email format',
  SaveError: 'Failed to save candidate',
  SelectCsvFile: 'Please select a CSV file',
  ParseCsvError: 'Failed to parse CSV file',
  SelectAssessment: 'Please select an assessment',
  AssignError: 'Failed to assign assessment',
  DeleteConfirm: (name: string) => `Are you sure you want to delete ${name}?`
} as const;

export const CandidateCsvMessages = {
  InvalidFormat: (row: number) => `Row ${row}: Invalid format (expected name,email)`,
  NameEmailRequired: (row: number) => `Row ${row}: Name and email are required`,
  InvalidEmail: (row: number, email: string) => `Row ${row}: Invalid email format (${email})`,
  CandidateExists: (row: number, email: string) => `Row ${row}: Candidate ${email} already exists`
} as const;
