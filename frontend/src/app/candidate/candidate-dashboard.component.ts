import { Component, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CandidateAssignment } from '../core/models/candidate.models';
import { CandidateAssessmentStatusValues } from '../core/models/candidate.models';
import { CandidateApiService } from '../core/services/candidate-api.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CellClickedEvent, ColDef, ICellRendererParams } from 'ag-grid-community';
import { CandidateDashboardMessages } from '../constants/candidate-dashboard.constants';

@Component({
  selector: 'app-candidate-dashboard',
  templateUrl: './candidate-dashboard.component.html',
  styleUrls: ['./candidate-dashboard.component.scss']
})
export class CandidateDashboardComponent implements OnDestroy {
  assignments: CandidateAssignment[] = [];
  assignmentColumnDefs: ColDef<CandidateAssignment>[] = [
    { field: 'assessmentTitle', headerName: 'Assessment', flex: 1.5, minWidth: 200, sortable: true, filter: true },
    {
      field: 'status',
      headerName: 'Status',
      minWidth: 130,
      sortable: true,
      filter: true,
      cellRenderer: (params: ICellRendererParams<CandidateAssignment, string>) => `<span class="badge text-bg-secondary">${params.value ?? ''}</span>`
    },
    {
      field: 'scheduledAtUtc',
      headerName: 'Availability',
      minWidth: 190,
      sortable: true,
      filter: true,
      valueFormatter: params => params.value ? new Date(params.value).toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }) : 'Now'
    },
    {
      field: 'remainingSeconds',
      headerName: 'Remaining Time',
      minWidth: 150,
      sortable: true,
      filter: false,
      valueFormatter: params => params.value ? `${Math.max(Math.floor(Number(params.value) / 60), 0)} min` : 'N/A'
    },
    {
      headerName: 'Action',
      minWidth: 160,
      sortable: false,
      filter: false,
      cellRenderer: (params: ICellRendererParams<CandidateAssignment>) => {
        const assignment = params.data;
        const disabled = assignment && !this.canOpenAssignment(assignment) ? 'disabled' : '';
        return `<button type="button" class="btn btn-primary btn-sm" data-action="open" ${disabled}>${assignment ? this.getActionLabel(assignment) : 'Open'}</button>`;
      }
    }
  ];
  defaultColDef: ColDef = {
    resizable: true,
    sortable: true,
    filter: true,
    floatingFilter: false
  };
  loading = false;
  error: string | null = null;
  private readonly destroy$ = new Subject<void>();

  constructor(private candidateApi: CandidateApiService, private router: Router) {}

  get totalAssignments(): number {
    return this.assignments.length;
  }

  get activeAssignments(): number {
    return this.assignments.filter(assignment => this.canOpenAssignment(assignment)).length;
  }

  get inProgressAssignments(): number {
    return this.assignments.filter(assignment => assignment.status === CandidateAssessmentStatusValues.InProgress).length;
  }

  get completedAssignments(): number {
    return this.assignments.filter(assignment =>
      assignment.status === CandidateAssessmentStatusValues.Submitted ||
      assignment.status === CandidateAssessmentStatusValues.Evaluated
    ).length;
  }

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

  onAssignmentCellClicked(event: CellClickedEvent<CandidateAssignment>): void {
    const actionElement = (event.event?.target as HTMLElement | null)?.closest('[data-action]');
    if (!actionElement || !event.data) {
      return;
    }

    if (actionElement.getAttribute('data-action') === 'open') {
      this.openAssessment(event.data.candidateAssessmentId);
    }
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
