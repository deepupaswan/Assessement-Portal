import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subject, forkJoin } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { AssessmentProgress, Candidate, CandidateAssignment } from '../../../core/models/candidate.models';
import { ResultRecord } from '../../../core/models/result.models';
import { AuthService } from '../../../core/services/auth.service';
import { CandidateApiService } from '../../../core/services/candidate-api.service';
import { ResultApiService } from '../../../core/services/result-api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { environment } from '../../../../environments/environment';
import {
  MonitoringAlert,
  MonitoringSession,
  SuspiciousActivityEvent
} from './monitoring.models';
import {
  MonitoringSignalRCommands,
  MonitoringSignalREvents,
  MonitoringDefaultLabels,
  MonitoringAlertSeverity,
  MonitoringAlertMessages,
  MonitoringSessionStatus
} from '../../../constants/monitoring.constants';

@Injectable()
export class MonitoringStateService implements OnDestroy {
  private readonly sessionsSubject = new BehaviorSubject<MonitoringSession[]>([]);
  private readonly alertsSubject = new BehaviorSubject<MonitoringAlert[]>([]);
  private readonly loadingSubject = new BehaviorSubject<boolean>(false);
  private readonly errorSubject = new BehaviorSubject<string | null>(null);
  private readonly destroy$ = new Subject<void>();
  private initialized = false;

  readonly sessions$ = this.sessionsSubject.asObservable();
  readonly alerts$ = this.alertsSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();
  readonly error$ = this.errorSubject.asObservable();
  readonly connectionState$ = this.signalR.connectionState$;

  constructor(
    private readonly authService: AuthService,
    private readonly candidateApi: CandidateApiService,
    private readonly resultApi: ResultApiService,
    private readonly signalR: SignalRService
  ) {}

  initialize(): void {
    if (this.initialized) {
      this.refresh();
      return;
    }

    this.initialized = true;
    this.refresh();
    this.connectRealtime();
  }

  refresh(): void {
    this.loadingSubject.next(true);
    this.errorSubject.next(null);

    forkJoin({
      assignments: this.candidateApi.getAssignments(),
      progress: this.candidateApi.getLiveProgress(),
      candidates: this.candidateApi.listCandidates(),
      results: this.resultApi.getResults()
    })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loadingSubject.next(false);
        })
      )
      .subscribe({
        next: ({ assignments, progress, candidates, results }) => {
          this.sessionsSubject.next(this.buildSessions(assignments, progress, candidates, results));
        },
        error: (err: any) => {
          this.errorSubject.next(err.error?.message ?? 'Failed to load monitoring snapshot');
          console.error(err);
        }
      });
  }

  dismissAlert(alertId: string): void {
    this.alertsSubject.next(this.alertsSubject.value.filter((alert) => alert.id !== alertId));
  }

  setSessionResultDetail(candidateAssessmentId: string, resultDetail: MonitoringSession['resultDetail']): void {
    this.sessionsSubject.next(
      this.sessionsSubject.value.map((session) =>
        session.candidateAssessmentId === candidateAssessmentId
          ? { ...session, resultDetail }
          : session
      )
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.signalR.off(MonitoringSignalREvents.ProgressUpdated);
    this.signalR.off(MonitoringSignalREvents.SuspiciousActivityDetected);
    this.signalR.off(MonitoringSignalREvents.AssessmentCompleted);
    this.signalR.off(MonitoringSignalREvents.CandidateJoined);
  }

  private connectRealtime(): void {
    const user = this.authService.getUser();
    if (!user) {
      return;
    }

    this.signalR.startConnection(environment.signalRHubUrl, user.token).then(() => {
      this.signalR.send(MonitoringSignalRCommands.JoinAdminMonitoringChannel);
    }).catch((err) => {
      console.warn('SignalR connection failed:', err);
    });

    this.signalR.on<AssessmentProgress>(MonitoringSignalREvents.ProgressUpdated, (progress) => {
      this.upsertSessionFromProgress(progress);
    });

    this.signalR.on<SuspiciousActivityEvent | string>(MonitoringSignalREvents.SuspiciousActivityDetected, (event) => {
      this.handleSuspiciousActivity(event);
    });

    this.signalR.on<{ candidateAssessmentId: string; candidateName?: string; connectedAt: string }>(
      MonitoringSignalREvents.CandidateJoined,
      (event) => {
        this.patchSession(event.candidateAssessmentId, (session) => ({
          ...session,
          candidateName: event.candidateName || session.candidateName,
          lastUpdatedAtUtc: event.connectedAt
        }));
      }
    );

    this.signalR.on<{ candidateAssessmentId?: string; assessmentId?: string; completedAt?: string }>(
      MonitoringSignalREvents.AssessmentCompleted,
      (event) => {
        const sessionId = event.candidateAssessmentId || event.assessmentId;
        if (!sessionId) {
          return;
        }

        this.patchSession(sessionId, (session) => ({
          ...session,
          status: MonitoringSessionStatus.Submitted,
          completionPercent: 100,
          remainingSeconds: 0,
          submittedAtUtc: event.completedAt || new Date().toISOString(),
          lastUpdatedAtUtc: event.completedAt || new Date().toISOString()
        }));
      }
    );
  }

  private buildSessions(
    assignments: CandidateAssignment[],
    progress: AssessmentProgress[],
    candidates: Candidate[],
    results: ResultRecord[]
  ): MonitoringSession[] {
    const progressMap = new Map(progress.map((item) => [item.candidateAssessmentId, item]));
    const candidateMap = new Map(candidates.map((item) => [item.id, item]));
    const resultMap = new Map(results.map((item) => [`${item.candidateId}:${item.assessmentId}`, item]));

    return assignments
      .map((assignment) => {
        const progressItem = progressMap.get(assignment.candidateAssessmentId);
        const candidate = assignment.candidateId ? candidateMap.get(assignment.candidateId) : undefined;
        const result = resultMap.get(`${assignment.candidateId}:${assignment.assessmentId}`);

        return {
          candidateAssessmentId: assignment.candidateAssessmentId,
          candidateId: assignment.candidateId || '',
          candidateName: assignment.candidateName || candidate?.name || MonitoringDefaultLabels.UnknownCandidate,
          candidateEmail: candidate?.email,
          assessmentId: assignment.assessmentId,
          assessmentTitle: assignment.assessmentTitle,
          status: progressItem?.status || assignment.status,
          completionPercent: progressItem?.completionPercent ?? (assignment.status === MonitoringSessionStatus.Submitted ? 100 : 0),
          suspiciousEvents: progressItem?.suspiciousEvents ?? 0,
          remainingSeconds: progressItem?.remainingSeconds ?? 0,
          assignedAtUtc: assignment.assignedAtUtc,
          scheduledAtUtc: assignment.scheduledAtUtc,
          startedAtUtc: assignment.startedAtUtc,
          submittedAtUtc: assignment.submittedAtUtc || result?.completedAt,
          lastUpdatedAtUtc: assignment.submittedAtUtc || result?.completedAt || assignment.startedAtUtc || assignment.assignedAtUtc,
          resultSummary: result
        } as MonitoringSession;
      })
      .sort((left, right) => this.getStatusRank(left.status) - this.getStatusRank(right.status) ||
        (new Date(right.lastUpdatedAtUtc || 0).getTime() - new Date(left.lastUpdatedAtUtc || 0).getTime()));
  }

  private upsertSessionFromProgress(progress: AssessmentProgress): void {
    const existing = this.sessionsSubject.value.find(
      (session) => session.candidateAssessmentId === progress.candidateAssessmentId
    );

    if (existing) {
      this.patchSession(progress.candidateAssessmentId, (session) => ({
        ...session,
        candidateName: progress.candidateName || session.candidateName,
        status: progress.status,
        completionPercent: progress.completionPercent,
        suspiciousEvents: Math.max(session.suspiciousEvents, progress.suspiciousEvents),
        remainingSeconds: progress.remainingSeconds,
        lastUpdatedAtUtc: new Date().toISOString()
      }));
      return;
    }

    const nextSession: MonitoringSession = {
      candidateAssessmentId: progress.candidateAssessmentId,
      candidateId: '',
      candidateName: progress.candidateName || MonitoringDefaultLabels.UnknownCandidate,
      assessmentId: '',
      assessmentTitle: MonitoringDefaultLabels.UnknownAssessment,
      status: progress.status,
      completionPercent: progress.completionPercent,
      suspiciousEvents: progress.suspiciousEvents,
      remainingSeconds: progress.remainingSeconds,
      lastUpdatedAtUtc: new Date().toISOString()
    };

    this.sessionsSubject.next(this.sortSessions([nextSession, ...this.sessionsSubject.value]));
  }

  private handleSuspiciousActivity(event: SuspiciousActivityEvent | string): void {
    if (typeof event === 'string') {
      const syntheticAlert: MonitoringAlert = {
        id: `alert-${Date.now()}`,
        candidateAssessmentId: '',
        candidateName: MonitoringDefaultLabels.UnknownCandidate,
        assessmentTitle: MonitoringDefaultLabels.UnknownAssessment,
        violationType: MonitoringDefaultLabels.UnknownViolationType,
        severity: MonitoringAlertSeverity.Warning,
        message: event,
        occurredAtUtc: new Date().toISOString()
      };
      this.alertsSubject.next([syntheticAlert, ...this.alertsSubject.value].slice(0, 20));
      return;
    }

    const sessionId = event.candidateAssessmentId || event.assessmentId || '';
    if (!sessionId) {
      return;
    }

    const session = this.sessionsSubject.value.find((item) => item.candidateAssessmentId === sessionId);
    const alert: MonitoringAlert = {
      id: `${sessionId}-${Date.now()}`,
      candidateAssessmentId: sessionId,
      candidateName: session?.candidateName || event.candidateName || MonitoringDefaultLabels.UnknownCandidate,
      assessmentTitle: session?.assessmentTitle || MonitoringDefaultLabels.UnknownAssessment,
      violationType: event.violationType || MonitoringDefaultLabels.UnknownViolationDefault,
      severity: this.getSeverity(event.violationType),
      message: this.getAlertMessage(event.violationType),
      occurredAtUtc: event.reportedAt || new Date().toISOString()
    };

    this.alertsSubject.next([alert, ...this.alertsSubject.value].slice(0, 20));
    if (!session) {
      this.sessionsSubject.next(this.sortSessions([
        {
          candidateAssessmentId: sessionId,
          candidateId: '',
          candidateName: alert.candidateName,
          assessmentId: '',
          assessmentTitle: alert.assessmentTitle,
          status: MonitoringSessionStatus.InProgress,
          completionPercent: 0,
          suspiciousEvents: 1,
          remainingSeconds: 0,
          latestAlert: alert,
          lastUpdatedAtUtc: alert.occurredAtUtc
        },
        ...this.sessionsSubject.value
      ]));
      return;
    }

    this.patchSession(sessionId, (currentSession) => ({
      ...currentSession,
      suspiciousEvents: currentSession.suspiciousEvents + 1,
      latestAlert: alert,
      lastUpdatedAtUtc: alert.occurredAtUtc
    }));
  }

  private patchSession(
    candidateAssessmentId: string,
    updater: (session: MonitoringSession) => MonitoringSession
  ): void {
    this.sessionsSubject.next(
      this.sortSessions(
        this.sessionsSubject.value.map((session) =>
          session.candidateAssessmentId === candidateAssessmentId
            ? updater(session)
            : session
        )
      )
    );
  }

  private sortSessions(sessions: MonitoringSession[]): MonitoringSession[] {
    return [...sessions].sort((left, right) =>
      this.getStatusRank(left.status) - this.getStatusRank(right.status) ||
      (new Date(right.lastUpdatedAtUtc || 0).getTime() - new Date(left.lastUpdatedAtUtc || 0).getTime())
    );
  }

  private getStatusRank(status: string): number {
    const normalized = status.toLowerCase();
    if (normalized === MonitoringSessionStatus.InProgress.toLowerCase()) {
      return 0;
    }
    if (normalized === MonitoringSessionStatus.Assigned.toLowerCase()) {
      return 1;
    }
    if (normalized === MonitoringSessionStatus.Scheduled.toLowerCase()) {
      return 2;
    }
    if (normalized === MonitoringSessionStatus.Submitted.toLowerCase()) {
      return 3;
    }
    return 4;
  }

  private getSeverity(violationType?: string): MonitoringAlert['severity'] {
    if (!violationType) {
      return MonitoringAlertSeverity.Warning;
    }

    if (violationType.includes('FULLSCREEN') || violationType.includes('MULTIPLE')) {
      return MonitoringAlertSeverity.Critical;
    }

    return MonitoringAlertSeverity.Warning;
  }

  private getAlertMessage(violationType?: string): string {
    switch (violationType) {
      case 'TAB_SWITCH':
        return MonitoringAlertMessages.TAB_SWITCH;
      case 'FULLSCREEN_EXIT':
        return MonitoringAlertMessages.FULLSCREEN_EXIT;
      default:
        return MonitoringAlertMessages.DEFAULT;
    }
  }
}
