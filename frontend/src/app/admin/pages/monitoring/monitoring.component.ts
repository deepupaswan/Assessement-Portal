import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CellClickedEvent, ColDef, ICellRendererParams } from 'ag-grid-community';
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
  sessionColumnDefs: ColDef<MonitoringSession>[] = [
    {
      field: 'candidateName',
      headerName: 'Candidate',
      flex: 1.2,
      minWidth: 190,
      sortable: true,
      filter: true,
      cellRenderer: (params: ICellRendererParams<MonitoringSession, string>) => `
        <div class="primary-cell">
          <span class="name">${params.value ?? ''}</span>
          <small>${params.data?.candidateEmail || 'Email unavailable'}</small>
        </div>
      `
    },
    {
      field: 'assessmentTitle',
      headerName: 'Assessment',
      flex: 1.3,
      minWidth: 220,
      sortable: true,
      filter: true,
      cellRenderer: (params: ICellRendererParams<MonitoringSession, string>) => `
        <div class="primary-cell">
          <span class="name">${params.value ?? ''}</span>
          <small>${params.data?.startedAtUtc ? 'Started ' + new Date(params.data.startedAtUtc).toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }) : params.data?.scheduledAtUtc ? 'Scheduled ' + new Date(params.data.scheduledAtUtc).toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }) : ''}</small>
        </div>
      `
    },
    {
      field: 'status',
      headerName: 'Status',
      minWidth: 130,
      sortable: true,
      filter: true,
      cellRenderer: (params: ICellRendererParams<MonitoringSession, string>) => `<span class="status-badge status-${String(params.value ?? '').toLowerCase()}">${params.value ?? ''}</span>`
    },
    {
      field: 'completionPercent',
      headerName: 'Progress',
      minWidth: 150,
      sortable: true,
      filter: false,
      cellRenderer: (params: ICellRendererParams<MonitoringSession, number>) => `
        <div class="progress-cell">
          <div class="progress-bar">
            <span style="width: ${params.value ?? 0}%"></span>
          </div>
          <strong>${params.value ?? 0}%</strong>
        </div>
      `
    },
    {
      field: 'suspiciousEvents',
      headerName: 'Suspicious',
      minWidth: 120,
      sortable: true,
      filter: false,
      cellRenderer: (params: ICellRendererParams<MonitoringSession, number>) => `<span class="risk-count${(params.value ?? 0) > 0 ? ' alert' : ''}">${params.value ?? 0}</span>`
    },
    {
      field: 'remainingSeconds',
      headerName: 'Remaining',
      minWidth: 140,
      sortable: true,
      filter: false,
      valueFormatter: params => this.formatRemaining(Number(params.value ?? 0))
    }
  ];
  defaultColDef: ColDef = {
    resizable: true,
    sortable: true,
    filter: true,
    floatingFilter: false
  };
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

  get totalSessions(): number {
    return this.sessions.length;
  }

  get flaggedSessionCount(): number {
    return this.flaggedSessions.length;
  }

  get alertCount(): number {
    return this.alerts.length;
  }

  get selectedAlertCount(): number {
    return this.selectedSessionAlerts.length;
  }

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

  onSessionCellClicked(event: CellClickedEvent<MonitoringSession>): void {
    if (!event.data) {
      return;
    }

    this.selectSession(event.data.candidateAssessmentId);
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
