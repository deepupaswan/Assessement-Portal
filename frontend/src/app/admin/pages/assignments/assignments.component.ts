import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject, forkJoin } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { Assessment } from '../../../core/models/assessment.models';
import { Candidate, CandidateAssignment } from '../../../core/models/candidate.models';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { CandidateApiService } from '../../../core/services/candidate-api.service';
import { AssignmentForm, AssignmentRow } from './assignments.models';
import { AssignmentFilterStatus, AssignmentFilterStatusValues } from '../../../constants/assignment-filters.constants';
import { AssignmentsMessages } from '../../../constants/assignments.constants';

@Component({
  selector: 'app-assignments',
  templateUrl: './assignments.component.html',
  styleUrls: ['./assignments.component.scss']
})
export class AssignmentsComponent implements OnInit, OnDestroy {
  assignments: AssignmentRow[] = [];
  candidates: Candidate[] = [];
  assessments: Assessment[] = [];

  loading = true;
  saving = false;
  bulkAssigning = false;
  deletingAssignmentId: string | null = null;
  error: string | null = null;
  searchTerm = '';
  filterStatus: AssignmentFilterStatus = AssignmentFilterStatusValues.All;
  sortBy: 'candidate' | 'assessment' | 'date' = 'date';
  sortDesc = true;

  showForm = false;
  editingAssignment: AssignmentRow | null = null;
  formData: AssignmentForm = { candidateId: '', assessmentId: '', scheduledAtUtc: undefined };

  showBulkForm = false;
  bulkData: AssignmentForm = { candidateId: '', assessmentId: '', scheduledAtUtc: undefined };
  selectedCandidatesForBulk = new Set<string>();

  // Expose constants to template
  readonly filterStatusValues = AssignmentFilterStatusValues;

  minDateTime: string;
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly candidateApi: CandidateApiService,
    private readonly assessmentApi: AssessmentApiService
  ) {
    const now = new Date();
    const localNow = new Date(now.getTime() - now.getTimezoneOffset() * 60000);
    this.minDateTime = localNow.toISOString().slice(0, 16);
  }

  ngOnInit(): void {
    this.loadAssignments();
    this.loadCandidates();
    this.loadAssessments();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAssignments(): void {
    this.loading = true;
    this.error = null;

    this.candidateApi
      .getAssignments()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: (data: CandidateAssignment[]) => {
          this.assignments = data.map((assignment) => this.mapAssignment(assignment));
        },
        error: (err: any) => {
          this.error = err.error?.message ?? AssignmentsMessages.LoadError;
          console.error(err);
        }
      });
  }

  loadCandidates(): void {
    this.candidateApi
      .listCandidates()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: Candidate[]) => {
          this.candidates = data;
        },
        error: (err: any) => console.error(AssignmentsMessages.LoadCandidatesError, err)
      });
  }

  loadAssessments(): void {
    this.assessmentApi
      .listAssessments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: Assessment[]) => {
          this.assessments = data.filter((assessment) => assessment.isActive);
        },
        error: (err: any) => console.error(AssignmentsMessages.LoadAssessmentsError, err)
      });
  }

  get filteredAssignments(): AssignmentRow[] {
    let result = [...this.assignments];

    if (this.searchTerm.trim()) {
      const term = this.searchTerm.trim().toLowerCase();
      result = result.filter(
        (assignment) =>
          assignment.candidateName.toLowerCase().includes(term) ||
          assignment.assessmentTitle.toLowerCase().includes(term)
      );
    }

    if (this.filterStatus !== AssignmentFilterStatusValues.All) {
      result = result.filter(
        (assignment) => assignment.status.toLowerCase() === this.filterStatus
      );
    }

    result.sort((left, right) => {
      let leftValue = '';
      let rightValue = '';

      if (this.sortBy === 'candidate') {
        leftValue = left.candidateName.toLowerCase();
        rightValue = right.candidateName.toLowerCase();
      } else if (this.sortBy === 'assessment') {
        leftValue = left.assessmentTitle.toLowerCase();
        rightValue = right.assessmentTitle.toLowerCase();
      } else {
        leftValue = left.scheduledAt || left.createdAt || '';
        rightValue = right.scheduledAt || right.createdAt || '';
      }

      const comparison = leftValue < rightValue ? -1 : leftValue > rightValue ? 1 : 0;
      return this.sortDesc ? -comparison : comparison;
    });

    return result;
  }

  changeSortBy(field: 'candidate' | 'assessment' | 'date'): void {
    if (this.sortBy === field) {
      this.sortDesc = !this.sortDesc;
      return;
    }

    this.sortBy = field;
    this.sortDesc = field === 'date';
  }

  openCreateForm(): void {
    this.showBulkForm = false;
    this.error = null;
    this.editingAssignment = null;
    this.formData = { candidateId: '', assessmentId: '', scheduledAtUtc: undefined };
    this.showForm = true;
  }

  editAssignment(assignment: AssignmentRow): void {
    if (!this.canEdit(assignment)) {
      return;
    }

    this.showBulkForm = false;
    this.error = null;
    this.editingAssignment = assignment;
    this.formData = {
      candidateId: assignment.candidateId,
      assessmentId: assignment.assessmentId,
      scheduledAtUtc: this.toDateTimeLocal(assignment.scheduledAt)
    };
    this.showForm = true;
  }

  saveAssignment(): void {
    if (!this.formData.candidateId || !this.formData.assessmentId) {
      this.error = AssignmentsMessages.RequiredFields;
      return;
    }

    const payload = this.toAssignmentPayload(this.formData);
    const request = this.editingAssignment
      ? this.candidateApi.updateAssignment(this.editingAssignment.id, payload)
      : this.candidateApi.assignAssessment(payload);

    this.saving = true;
    this.error = null;

    request
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe({
        next: (assignment: CandidateAssignment) => {
          this.upsertAssignment(this.mapAssignment(assignment));
          this.cancelForm();
        },
        error: (err: any) => {
          this.error =
            err.error?.message ??
            (this.editingAssignment
              ? AssignmentsMessages.UpdateError
              : AssignmentsMessages.CreateError);
          console.error(err);
        }
      });
  }

  deleteAssignment(assignment: AssignmentRow): void {
    if (!this.canDelete(assignment)) {
      return;
    }

    if (!confirm(AssignmentsMessages.DeleteConfirm)) {
      return;
    }

    this.deletingAssignmentId = assignment.id;
    this.error = null;

    this.candidateApi
      .deleteAssignment(assignment.id)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.deletingAssignmentId = null;
        })
      )
      .subscribe({
        next: () => {
          this.assignments = this.assignments.filter((item) => item.id !== assignment.id);
        },
        error: (err: any) => {
          this.error = err.error?.message ?? AssignmentsMessages.DeleteError;
          console.error(err);
        }
      });
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingAssignment = null;
    this.formData = { candidateId: '', assessmentId: '', scheduledAtUtc: undefined };
    this.error = null;
  }

  openBulkForm(): void {
    this.showForm = false;
    this.error = null;
    this.showBulkForm = true;
    this.bulkData = { candidateId: '', assessmentId: '', scheduledAtUtc: undefined };
    this.selectedCandidatesForBulk.clear();
  }

  toggleCandidateSelection(candidateId: string): void {
    if (this.selectedCandidatesForBulk.has(candidateId)) {
      this.selectedCandidatesForBulk.delete(candidateId);
      return;
    }

    this.selectedCandidatesForBulk.add(candidateId);
  }

  isCandidateSelected(candidateId: string): boolean {
    return this.selectedCandidatesForBulk.has(candidateId);
  }

  toggleAllCandidates(): void {
    if (this.selectedCandidatesForBulk.size === this.candidates.length) {
      this.selectedCandidatesForBulk.clear();
      return;
    }

    this.selectedCandidatesForBulk = new Set(this.candidates.map((candidate) => candidate.id));
  }

  bulkAssign(): void {
    if (!this.bulkData.assessmentId) {
      this.error = AssignmentsMessages.RequiredAssessment;
      return;
    }

    if (this.selectedCandidatesForBulk.size === 0) {
      this.error = AssignmentsMessages.RequiredCandidates;
      return;
    }

    const scheduledAtUtc = this.toUtcIso(this.bulkData.scheduledAtUtc);
    const requests = Array.from(this.selectedCandidatesForBulk).map((candidateId) =>
      this.candidateApi.assignAssessment({
        candidateId,
        assessmentId: this.bulkData.assessmentId,
        scheduledAtUtc
      })
    );

    this.bulkAssigning = true;
    this.error = null;

    forkJoin(requests)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.bulkAssigning = false;
        })
      )
      .subscribe({
        next: (assignments: CandidateAssignment[]) => {
          assignments
            .map((assignment) => this.mapAssignment(assignment))
            .forEach((assignment) => this.upsertAssignment(assignment));

          this.closeBulkForm();
        },
        error: (err: any) => {
          this.error = err.error?.message ?? AssignmentsMessages.BulkAssignError;
          console.error(err);
        }
      });
  }

  closeBulkForm(): void {
    this.showBulkForm = false;
    this.bulkData = { candidateId: '', assessmentId: '', scheduledAtUtc: undefined };
    this.selectedCandidatesForBulk.clear();
    this.error = null;
  }

  canEdit(assignment: AssignmentRow): boolean {
    const status = assignment.status.toLowerCase();
    return status === 'assigned' || status === 'scheduled';
  }

  canDelete(assignment: AssignmentRow): boolean {
    return this.canEdit(assignment);
  }

  isDeleting(assignmentId: string): boolean {
    return this.deletingAssignmentId === assignmentId;
  }

  private mapAssignment(assignment: CandidateAssignment): AssignmentRow {
    return {
      id: assignment.candidateAssessmentId,
      candidateAssessmentId: assignment.candidateAssessmentId,
      candidateId: assignment.candidateId ?? '',
      candidateName: assignment.candidateName ?? AssignmentsMessages.FallbackCandidateName,
      assessmentId: assignment.assessmentId,
      assessmentTitle: assignment.assessmentTitle,
      status: assignment.status,
      scheduledAt: assignment.scheduledAtUtc,
      createdAt: assignment.assignedAtUtc,
      startedAt: assignment.startedAtUtc,
      submittedAt: assignment.submittedAtUtc
    };
  }

  private upsertAssignment(assignment: AssignmentRow): void {
    const existingIndex = this.assignments.findIndex((item) => item.id === assignment.id);
    if (existingIndex === -1) {
      this.assignments = [assignment, ...this.assignments];
      return;
    }

    this.assignments = this.assignments.map((item, index) =>
      index === existingIndex ? assignment : item
    );
  }

  private toAssignmentPayload(form: AssignmentForm): {
    candidateId: string;
    assessmentId: string;
    scheduledAtUtc?: string;
  } {
    return {
      candidateId: form.candidateId,
      assessmentId: form.assessmentId,
      scheduledAtUtc: this.toUtcIso(form.scheduledAtUtc)
    };
  }

  private toUtcIso(value?: string): string | undefined {
    if (!value) {
      return undefined;
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
  }

  private toDateTimeLocal(value?: string): string | undefined {
    if (!value) {
      return undefined;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return undefined;
    }

    const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return localDate.toISOString().slice(0, 16);
  }
}
