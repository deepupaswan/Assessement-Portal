import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MonitoringAlert } from '../../monitoring.models';

@Component({
  selector: 'app-alert-notification',
  templateUrl: './alert-notification.component.html',
  styleUrls: ['./alert-notification.component.scss']
})
export class AlertNotificationComponent {
  @Input() alerts: MonitoringAlert[] = [];
  @Output() inspect = new EventEmitter<string>();
  @Output() dismiss = new EventEmitter<string>();

  get visibleAlerts(): MonitoringAlert[] {
    return this.alerts.slice(0, 4);
  }

  trackByAlert(index: number, alert: MonitoringAlert): string {
    return alert.id;
  }
}
