/**
 * Admin home dashboard (pages/dashboard) — activity feed and time labels
 */

export const AdminHomeDashboardMessages = {
  LoadError: 'Failed to load analytics'
} as const;

export const AdminHomeDashboardActivity = {
  DashboardLoaded: 'Dashboard loaded',
  DashboardLoadedDescription: (totalCandidates: number, completionRate: number) =>
    `${totalCandidates} candidates, ${completionRate.toFixed(1)}% completion`,
  ProgressTitle: 'Candidate progress update',
  ProgressDescription: (name: string, percent: number) => `${name} is ${percent}% complete`,
  SuspiciousTitle: 'Suspicious activity detected',
  NewCandidateTitle: 'New candidate registered',
  AssessmentAssignedTitle: 'Assessment assigned',
  AssessmentAssignedDescription: (assessmentTitle: string, candidateName: string) =>
    `${assessmentTitle} → ${candidateName}`,
  SuspiciousDescription: (candidateName: string, violationType: string) =>
    `${candidateName} triggered ${violationType}`,
  FallbackCandidate: 'Candidate',
  DefaultViolationLabel: 'an alert'
} as const;

export const AdminHomeRelativeTime = {
  JustNow: 'just now',
  Minute: 'minute',
  Minutes: 'minutes',
  Hour: 'hour',
  Hours: 'hours',
  Day: 'day',
  Days: 'days',
  Ago: 'ago'
} as const;
