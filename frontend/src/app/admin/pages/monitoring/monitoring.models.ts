import { ResultDetail, ResultRecord } from '../../../core/models/result.models';

export type MonitoringAlertSeverity = 'info' | 'warning' | 'critical';

export interface MonitoringAlert {
  id: string;
  candidateAssessmentId: string;
  candidateName: string;
  assessmentTitle: string;
  violationType: string;
  severity: MonitoringAlertSeverity;
  message: string;
  occurredAtUtc: string;
}

export interface MonitoringSession {
  candidateAssessmentId: string;
  candidateId: string;
  candidateName: string;
  candidateEmail?: string;
  assessmentId: string;
  assessmentTitle: string;
  status: string;
  completionPercent: number;
  suspiciousEvents: number;
  remainingSeconds: number;
  assignedAtUtc?: string;
  scheduledAtUtc?: string;
  startedAtUtc?: string;
  submittedAtUtc?: string;
  lastUpdatedAtUtc?: string;
  latestAlert?: MonitoringAlert;
  resultSummary?: ResultRecord;
  resultDetail?: ResultDetail;
}

export interface SuspiciousActivityEvent {
  candidateAssessmentId?: string;
  assessmentId?: string;
  candidateName?: string;
  violationType?: string;
  reportedAt?: string;
}
