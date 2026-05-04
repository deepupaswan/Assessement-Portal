/**
 * Assessments management constants for filters, statuses, and messages
 */

export type AssessmentFilterType = 'all' | 'active' | 'inactive';

export const AssessmentFilterTypeValues = {
  All: 'all',
  Active: 'active',
  Inactive: 'inactive'
} as const;

export const AssessmentFilterTypeLabels: Record<AssessmentFilterType, string> = {
  [AssessmentFilterTypeValues.All]: 'All',
  [AssessmentFilterTypeValues.Active]: 'Active',
  [AssessmentFilterTypeValues.Inactive]: 'Inactive'
} as const;

export const AssessmentStatusLabels = {
  Active: 'Active',
  Inactive: 'Inactive'
} as const;

export const AssessmentMessages = {
  LoadError: 'Failed to load assessments',
  DeleteError: 'Failed to delete assessment',
  CloneError: 'Failed to clone assessment',
  DeleteConfirm: (title: string) => `Delete assessment "${title}"? This action cannot be undone.`,
  CloneConfirm: (title: string) => `Clone assessment "${title}"?`
} as const;

export const AssessmentRoutes = {
  Create: '/admin/assessments/create',
  Edit: (id: string) => `/admin/assessments/edit/${id}`,
  Details: (id: string) => `/admin/assessments/${id}/questions`
} as const;
