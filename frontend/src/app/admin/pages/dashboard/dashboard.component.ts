import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { CandidateApiService } from '../../../core/services/candidate-api.service';
import { ResultApiService } from '../../../core/services/result-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { environment } from '../../../../environments/environment';
import { AnalyticsOverview } from '../../../core/models/result.models';
import { AssessmentProgress } from '../../../core/models/candidate.models';
import { ActivityItem } from './dashboard.models';
import {
  AdminHomeDashboardActivity,
  AdminHomeDashboardMessages,
  AdminHomeRelativeTime
} from '../../../constants/dashboard.constants';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  // KPI Data
  analytics: AnalyticsOverview | null = null;

  // Activity Feed
  activityLog: ActivityItem[] = [];
  maxActivityItems = 5;

  // Real-time updates
  private destroy$ = new Subject<void>();

  constructor(
    private assessmentApi: AssessmentApiService,
    private candidateApi: CandidateApiService,
    private resultApi: ResultApiService,
    private authService: AuthService,
    private signalR: SignalRService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
    this.setupRealTimeUpdates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Quick Actions
  createAssessment(): void {
    this.router.navigate(['/admin/assessments']);
  }

  addCandidate(): void {
    this.router.navigate(['/admin/candidates']);
  }

  assignAssessment(): void {
    this.router.navigate(['/admin/assignments']);
  }

  viewLiveProgress(): void {
    this.router.navigate(['/admin/monitoring']);
  }

  // Data Loading
  private loadDashboard(): void {
    this.loading = true;
    this.error = null;

    this.resultApi.getAnalyticsOverview()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: analytics => {
          this.analytics = analytics;
          this.addActivity({
            id: `analytics-${Date.now()}`,
            icon: 'icon-bar-chart-2',
            title: AdminHomeDashboardActivity.DashboardLoaded,
            description: AdminHomeDashboardActivity.DashboardLoadedDescription(
              analytics.totalCandidates,
              analytics.completionRate
            ),
            timestamp: new Date(),
            type: 'assessment'
          });
        },
        error: err => {
          this.error = err.error?.message ?? AdminHomeDashboardMessages.LoadError;
          console.error('Analytics load error:', err);
        },
        complete: () => {
          this.loading = false;
        }
      });
  }

  private setupRealTimeUpdates(): void {
    const user = this.authService.getUser();
    if (!user) {
      return;
    }

    this.signalR.startConnection(environment.signalRHubUrl, user.token).then(() => {
      this.signalR.send('JoinAdminMonitoringChannel');
    }).catch(err => {
      console.warn('SignalR connection failed:', err);
    });

    // Listen for progress updates
    this.signalR.on<AssessmentProgress>('ProgressUpdated', progress => {
      this.addActivity({
        id: `progress-${progress.candidateAssessmentId}`,
        icon: 'icon-activity',
        title: AdminHomeDashboardActivity.ProgressTitle,
        description: AdminHomeDashboardActivity.ProgressDescription(
          progress.candidateName,
          progress.completionPercent
        ),
        timestamp: new Date(),
        type: 'assessment'
      });
    });

    // Listen for suspicious activity
    this.signalR.on<{ candidateName?: string; violationType?: string } | string>('SuspiciousActivityDetected', payload => {
      const description = typeof payload === 'string'
        ? payload
        : AdminHomeDashboardActivity.SuspiciousDescription(
            payload.candidateName || AdminHomeDashboardActivity.FallbackCandidate,
            payload.violationType || AdminHomeDashboardActivity.DefaultViolationLabel
          );
      this.addActivity({
        id: `suspicious-${Date.now()}`,
        icon: 'icon-alert-circle',
        title: AdminHomeDashboardActivity.SuspiciousTitle,
        description,
        timestamp: new Date(),
        type: 'result'
      });
    });

    // Listen for new candidate registrations
    this.signalR.on<{ name: string; email: string }>('CandidateRegistered', candidate => {
      this.addActivity({
        id: `candidate-${Date.now()}`,
        icon: 'icon-user-plus',
        title: AdminHomeDashboardActivity.NewCandidateTitle,
        description: `${candidate.name}`,
        timestamp: new Date(),
        type: 'candidate'
      });
    });

    // Listen for assessment assignments
    this.signalR.on<{ candidateName: string; assessmentTitle: string }>('AssessmentAssigned', event => {
      this.addActivity({
        id: `assignment-${Date.now()}`,
        icon: 'icon-send',
        title: AdminHomeDashboardActivity.AssessmentAssignedTitle,
        description: AdminHomeDashboardActivity.AssessmentAssignedDescription(
          event.assessmentTitle,
          event.candidateName
        ),
        timestamp: new Date(),
        type: 'assignment'
      });
    });
  }

  private addActivity(item: ActivityItem): void {
    this.activityLog = [item, ...this.activityLog].slice(0, this.maxActivityItems);
  }

  getActivityIcon(type: string): string {
    const iconMap: Record<string, string> = {
      'assessment': 'icon-file-text',
      'candidate': 'icon-user-plus',
      'assignment': 'icon-send',
      'result': 'icon-check-circle'
    };
    return iconMap[type] || 'icon-activity';
  }

  formatTime(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - new Date(date).getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return AdminHomeRelativeTime.JustNow;
    if (diffMins < 60) {
      const label = diffMins > 1 ? AdminHomeRelativeTime.Minutes : AdminHomeRelativeTime.Minute;
      return `${diffMins} ${label} ${AdminHomeRelativeTime.Ago}`;
    }
    if (diffHours < 24) {
      const label = diffHours > 1 ? AdminHomeRelativeTime.Hours : AdminHomeRelativeTime.Hour;
      return `${diffHours} ${label} ${AdminHomeRelativeTime.Ago}`;
    }
    if (diffDays < 7) {
      const label = diffDays > 1 ? AdminHomeRelativeTime.Days : AdminHomeRelativeTime.Day;
      return `${diffDays} ${label} ${AdminHomeRelativeTime.Ago}`;
    }
    return new Date(date).toLocaleDateString();
  }
}
