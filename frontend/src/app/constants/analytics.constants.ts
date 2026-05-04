/**
 * Analytics component constants for filters and messages
 */

export type AnalyticsOutcomeFilter = 'all' | 'passed' | 'failed';

export const AnalyticsFilterValues = {
  All: 'all',
  Passed: 'passed',
  Failed: 'failed'
} as const;

export const AnalyticsMessages = {
  LoadError: 'Failed to load analytics',
  FallbackCandidate: 'Candidate',
  FallbackAssessment: 'Assessment',
  EmailUnavailable: 'Email unavailable'
} as const;
