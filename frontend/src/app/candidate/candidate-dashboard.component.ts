import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CandidateAssignment } from '../core/models/candidate.models';
import { CandidateApiService } from '../core/services/candidate-api.service';

@Component({
  selector: 'app-candidate-dashboard',
  templateUrl: './candidate-dashboard.component.html',
})
export class CandidateDashboardComponent {
  assignments: CandidateAssignment[] = [];
  loading = false;
  error: string | null = null;

  constructor(private candidateApi: CandidateApiService, private router: Router) {}

  ngOnInit(): void {
    this.loadAssignments();
  }

  loadAssignments(): void {
    this.loading = true;
    this.error = null;

    this.candidateApi.getMyAssignments().subscribe({
      next: assignments => {
        this.assignments = assignments;
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to load assignments.';
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  openAssessment(candidateAssessmentId: string): void {
    this.router.navigate(['/candidate/assessment', candidateAssessmentId]);
  }

  canOpenAssignment(assignment: CandidateAssignment): boolean {
    return assignment.status !== 'Scheduled' &&
      assignment.status !== 'Submitted' &&
      assignment.status !== 'Evaluated';
  }

  getActionLabel(assignment: CandidateAssignment): string {
    if (assignment.status === 'Scheduled') {
      return 'Scheduled';
    }

    return assignment.status === 'InProgress' ? 'Resume' : 'Start';
  }
}
