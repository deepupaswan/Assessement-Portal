/**
 * Candidate assessment session constants for SignalR events and commands
 */

export const CandidateAssessmentSignalREvents = {
  TimerUpdated: 'TimerUpdated',
  WarningIssued: 'WarningIssued'
} as const;

export const CandidateAssessmentSignalRCommands = {
  JoinAssessmentChannel: 'JoinAssessmentChannel',
  UpdateProgress: 'UpdateProgress',
  ReportSuspiciousActivity: 'ReportSuspiciousActivity'
} as const;

export const CandidateAssessmentViolationTypes = {
  TabSwitch: 'TAB_SWITCH',
  FullscreenExit: 'FULLSCREEN_EXIT'
} as const;

export const CandidateAssessmentMessages = {
  SessionIdMissing: 'Assessment session id is missing.',
  SubmissionFailed: 'Submission failed.',
  AutoSubmitWithSyncFailed: 'Auto-submit triggered, but answer sync failed. Please reconnect and retry.',
  UnableToSaveBeforeSubmission: 'Unable to save answers before submission.',
  LoadSessionFailed: 'Unable to load assessment session.',
  AutoSaveRetryPending: 'Auto-save retry pending. Your local answers are preserved.',
  FullscreenBlocked: 'Fullscreen request was blocked by the browser.'
} as const;

/** Metadata sent to the server with each violation (human-readable for admin review) */
export const CandidateAssessmentViolationMetadata = {
  TabSwitch: 'Candidate switched tabs or minimized browser.',
  FullscreenExit: 'Candidate exited fullscreen mode.'
} as const;

export const CandidateAssessmentUi = {
  FullscreenHint: 'Keep fullscreen mode enabled. Answers auto-save every few seconds.',
  TimeLeft: 'Time Left',
  EnterFullscreen: 'Enter Fullscreen',
  TypeMarksPrefix: 'Type:',
  MarksLabel: 'Marks',
  DescriptivePlaceholder: 'Write your answer here',
  CodingPlaceholder: 'Write your code here',
  Submitting: 'Submitting...',
  SubmitAssessment: 'Submit Assessment',
  LoadingSession: 'Loading assessment session...',
  QuestionPrefix: 'Q'
} as const;

export function formatViolationWarning(count: number, violationType: string): string {
  return `Warning ${count}: ${violationType.replace(/_/g, ' ').toLowerCase()}`;
}
