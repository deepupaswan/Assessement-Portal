import { Component, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CandidateAssignment } from '../core/models/candidate.models';
import { CandidateAssessmentStatusValues } from '../core/models/candidate.models';
import { CandidateApiService } from '../core/services/candidate-api.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CandidateDashboardMessages } from '../constants/candidate-dashboard.constants';

@Component({
  selector: 'app-candidate-dashboard',
  templateUrl: './candidate-dashboard.component.html',
})
export class CandidateDashboardComponent implements OnDestroy {
  assignments: CandidateAssignment[] = [];
  loading = false;
  error: string | null = null;
  private readonly destroy$ = new Subject<void>();

  constructor(private candidateApi: CandidateApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadAssignments();
  }

  loadAssignments(): void {
    this.loading = true;
    this.error = null;

    this.candidateApi.getMyAssignments().pipe(takeUntil(this.destroy$)).subscribe({
      next: assignments => {
        this.assignments = assignments;
      },
      error: err => {
        this.error = err.error?.message ?? CandidateDashboardMessages.LoadError;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  openAssessment(candidateAssessmentId: string): void {
    this.router.navigate(['/candidate/assessment', candidateAssessmentId]);
  }

  canOpenAssignment(assignment: CandidateAssignment): boolean {
    return assignment.status !== CandidateAssessmentStatusValues.Scheduled &&
      assignment.status !== CandidateAssessmentStatusValues.Submitted &&
      assignment.status !== CandidateAssessmentStatusValues.Evaluated;
  }

  getActionLabel(assignment: CandidateAssignment): string {
    if (assignment.status === CandidateAssessmentStatusValues.Scheduled) {
      return CandidateDashboardMessages.ActionScheduled;
    }

    return assignment.status === CandidateAssessmentStatusValues.InProgress
      ? CandidateDashboardMessages.ActionResume
      : CandidateDashboardMessages.ActionStart;
  }
}
