import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MonitoringAlert, MonitoringSession } from './monitoring.models';
import { MonitoringStateService } from './monitoring-state.service';
import { MonitoringConnectionLabels } from '../../../constants/monitoring.constants';

@Component({
  selector: 'app-monitoring',
  templateUrl: './monitoring.component.html',
  styleUrls: ['./monitoring.component.scss'],
  providers: [MonitoringStateService]
})
export class MonitoringComponent implements OnInit, OnDestroy {
  sessions: MonitoringSession[] = [];
  alerts: MonitoringAlert[] = [];
  loading = true;
  error: string | null = null;
  connectionState = 'disconnected';
  selectedSessionId: string | null = null;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly monitoringState: MonitoringStateService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.monitoringState.initialize();

    this.monitoringState.sessions$
      .pipe(takeUntil(this.destroy$))
      .subscribe((sessions) => {
        this.sessions = sessions;

        if (this.selectedSessionId && !sessions.some((session) => session.candidateAssessmentId === this.selectedSessionId)) {
          this.selectedSessionId = null;
        }
      });

    this.monitoringState.alerts$
      .pipe(takeUntil(this.destroy$))
      .subscribe((alerts) => {
        this.alerts = alerts;
      });

    this.monitoringState.loading$
      .pipe(takeUntil(this.destroy$))
      .subscribe((loading) => {
        this.loading = loading;
      });

    this.monitoringState.error$
      .pipe(takeUntil(this.destroy$))
      .subscribe((error) => {
        this.error = error;
      });

    this.monitoringState.connectionState$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state) => {
        this.connectionState = state;
      });

    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe((params) => {
        const candidateAssessmentId = params.get('candidateAssessmentId');
        if (candidateAssessmentId) {
          this.selectedSessionId = candidateAssessmentId;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get selectedSession(): MonitoringSession | null {
    return this.sessions.find((session) => session.candidateAssessmentId === this.selectedSessionId) || null;
  }

  get flaggedSessions(): MonitoringSession[] {
    return this.sessions.filter((session) => session.suspiciousEvents > 0 || !!session.latestAlert);
  }

  get selectedSessionAlerts(): MonitoringAlert[] {
    if (!this.selectedSessionId) {
      return [];
    }

    return this.alerts.filter((alert) => alert.candidateAssessmentId === this.selectedSessionId);
  }

  refresh(): void {
    this.monitoringState.refresh();
  }

  selectSession(candidateAssessmentId: string): void {
    this.selectedSessionId = candidateAssessmentId;
    this.router.navigate(['/admin/monitoring', candidateAssessmentId]);
  }

  clearSelection(): void {
    this.selectedSessionId = null;
    this.router.navigate(['/admin/monitoring']);
  }

  dismissAlert(alertId: string): void {
    this.monitoringState.dismissAlert(alertId);
  }

  trackBySession(index: number, session: MonitoringSession): string {
    return session.candidateAssessmentId;
  }

  formatRemaining(seconds: number): string {
    const safeSeconds = Math.max(seconds || 0, 0);
    const minutes = Math.floor(safeSeconds / 60);
    const remainder = safeSeconds % 60;
    return `${minutes}m ${remainder.toString().padStart(2, '0')}s`;
  }

  getConnectionLabel(): string {
    switch (this.connectionState) {
      case 'connected':
        return MonitoringConnectionLabels.connected;
      case 'connecting':
        return MonitoringConnectionLabels.connecting;
      case 'reconnecting':
        return MonitoringConnectionLabels.reconnecting;
      case 'error':
        return MonitoringConnectionLabels.error;
      default:
        return MonitoringConnectionLabels.default;
    }
  }
}
