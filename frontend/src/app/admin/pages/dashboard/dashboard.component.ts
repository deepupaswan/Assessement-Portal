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
import { AnalyticsOverview, ResultSummary } from '../../../core/models/result.models';
import { AssessmentProgress } from '../../../core/models/candidate.models';

interface ActivityItem {
  id: string;
  icon: string;
  title: string;
  description: string;
  timestamp: Date;
  type: 'assessment' | 'candidate' | 'assignment' | 'result';
}

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
            title: 'Dashboard loaded',
            description: `${analytics.totalCandidates} candidates, ${analytics.completionRate.toFixed(1)}% completion`,
            timestamp: new Date(),
            type: 'assessment'
          });
        },
        error: err => {
          this.error = err.error?.message ?? 'Failed to load analytics';
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
        title: 'Candidate progress update',
        description: `${progress.candidateName} is ${progress.completionPercent}% complete`,
        timestamp: new Date(),
        type: 'assessment'
      });
    });

    // Listen for suspicious activity
    this.signalR.on<string>('SuspiciousActivityDetected', message => {
      this.addActivity({
        id: `suspicious-${Date.now()}`,
        icon: 'icon-alert-circle',
        title: 'Suspicious activity detected',
        description: message,
        timestamp: new Date(),
        type: 'result'
      });
    });

    // Listen for new candidate registrations
    this.signalR.on<{ name: string; email: string }>('CandidateRegistered', candidate => {
      this.addActivity({
        id: `candidate-${Date.now()}`,
        icon: 'icon-user-plus',
        title: 'New candidate registered',
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
        title: 'Assessment assigned',
        description: `${event.assessmentTitle} → ${event.candidateName}`,
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

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    return new Date(date).toLocaleDateString();
  }
}
