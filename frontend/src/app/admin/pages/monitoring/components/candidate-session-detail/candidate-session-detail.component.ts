import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { MonitoringAlert, MonitoringSession } from '../../monitoring.models';
import { ResultDetail } from '../../../../../core/models/result.models';
import { ResultApiService } from '../../../../../core/services/result-api.service';

@Component({
  selector: 'app-candidate-session-detail',
  templateUrl: './candidate-session-detail.component.html',
  styleUrls: ['./candidate-session-detail.component.scss']
})
export class CandidateSessionDetailComponent implements OnChanges {
  @Input() session: MonitoringSession | null = null;
  @Input() alerts: MonitoringAlert[] = [];
  @Output() close = new EventEmitter<void>();

  resultDetail: ResultDetail | null = null;
  resultLoading = false;
  private requestVersion = 0;

  constructor(private readonly resultApi: ResultApiService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['session']) {
      return;
    }

    this.resultDetail = this.session?.resultDetail || null;
    if (!this.session || this.session.status !== 'Submitted' || !this.session.candidateId || !this.session.assessmentId) {
      return;
    }

    const requestVersion = ++this.requestVersion;
    this.resultLoading = true;
    this.resultApi
      .getCandidateAssessmentResult(this.session.assessmentId, this.session.candidateId)
      .subscribe({
        next: (result) => {
          if (requestVersion === this.requestVersion) {
            this.resultDetail = result;
          }
        },
        error: () => {
          if (requestVersion === this.requestVersion) {
            this.resultDetail = null;
          }
        },
        complete: () => {
          if (requestVersion === this.requestVersion) {
            this.resultLoading = false;
          }
        }
      });
  }

  formatRemaining(seconds: number): string {
    const safeSeconds = Math.max(seconds || 0, 0);
    const minutes = Math.floor(safeSeconds / 60);
    const remainder = safeSeconds % 60;
    return `${minutes}m ${remainder.toString().padStart(2, '0')}s`;
  }
}
