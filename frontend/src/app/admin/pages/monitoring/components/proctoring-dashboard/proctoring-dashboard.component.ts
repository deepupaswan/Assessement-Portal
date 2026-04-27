import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MonitoringAlert, MonitoringSession } from '../../monitoring.models';

@Component({
  selector: 'app-proctoring-dashboard',
  templateUrl: './proctoring-dashboard.component.html',
  styleUrls: ['./proctoring-dashboard.component.scss']
})
export class ProctoringDashboardComponent {
  @Input() sessions: MonitoringSession[] = [];
  @Input() alerts: MonitoringAlert[] = [];
  @Input() selectedSessionId: string | null = null;
  @Output() inspectSession = new EventEmitter<string>();

  get flaggedSessions(): MonitoringSession[] {
    return [...this.sessions]
      .filter((session) => session.suspiciousEvents > 0)
      .sort((left, right) => right.suspiciousEvents - left.suspiciousEvents);
  }

  get activeSessionsCount(): number {
    return this.sessions.filter((session) => session.status === 'InProgress').length;
  }

  get criticalAlertCount(): number {
    return this.alerts.filter((alert) => alert.severity === 'critical').length;
  }
}
