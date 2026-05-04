/**
 * Monitoring-related constants for SignalR events, commands, and alert messages
 */

export const MonitoringSignalRCommands = {
  JoinAdminMonitoringChannel: 'JoinAdminMonitoringChannel'
} as const;

export const MonitoringSignalREvents = {
  ProgressUpdated: 'ProgressUpdated',
  SuspiciousActivityDetected: 'SuspiciousActivityDetected',
  CandidateJoined: 'CandidateJoined',
  AssessmentCompleted: 'AssessmentCompleted'
} as const;

export const MonitoringDefaultLabels = {
  UnknownCandidate: 'Candidate',
  UnknownAssessment: 'Assessment',
  UnknownViolationType: 'UNKNOWN',
  UnknownViolationDefault: 'SUSPICIOUS_ACTIVITY'
} as const;

export const MonitoringAlertSeverity = {
  Warning: 'warning',
  Critical: 'critical'
} as const;

export const MonitoringAlertMessages = {
  TAB_SWITCH: 'Candidate switched tabs or minimized the assessment window.',
  FULLSCREEN_EXIT: 'Candidate exited fullscreen during the assessment.',
  DEFAULT: 'Suspicious activity detected during the assessment.'
} as const;

export const MonitoringSessionStatus = {
  InProgress: 'InProgress',
  Assigned: 'Assigned',
  Scheduled: 'Scheduled',
  Submitted: 'Submitted'
} as const;

export const MonitoringConnectionLabels = {
  connected: 'Live',
  connecting: 'Connecting',
  reconnecting: 'Reconnecting',
  error: 'Offline',
  default: 'Disconnected'
} as const;
