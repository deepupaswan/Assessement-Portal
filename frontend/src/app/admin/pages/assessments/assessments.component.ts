import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { AssessmentSummary } from '../../../core/models/assessment.models';
import { AssessmentRow } from './assessments.models';
import {
  AssessmentFilterType,
  AssessmentFilterTypeValues,
  AssessmentStatusLabels,
  AssessmentMessages,
  AssessmentRoutes
} from '../../../constants/assessments.constants';

@Component({
  selector: 'app-assessments',
  templateUrl: './assessments.component.html',
  styleUrls: ['./assessments.component.scss']
})
export class AssessmentsComponent implements OnInit, OnDestroy {
  assessments: AssessmentRow[] = [];
  loading = true;
  error: string | null = null;
  searchTerm = '';
  filterType: AssessmentFilterType = AssessmentFilterTypeValues.All;
  sortBy: 'title' | 'date' | 'questions' = 'date';
  sortDesc = true;

  private destroy$ = new Subject<void>();

  constructor(
    private assessmentApi: AssessmentApiService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAssessments();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadAssessments(): void {
    this.loading = true;
    this.error = null;

    this.assessmentApi.listAssessments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: assessments => {
          this.assessments = assessments.map(a => ({ ...a, isDeleting: false }));
        },
        error: (err: any) => {
          this.error = err.error?.message ?? AssessmentMessages.LoadError;
          console.error('Assessment load error:', err);
        },
        complete: () => {
          this.loading = false;
        }
      });
  }

  get filteredAssessments(): AssessmentRow[] {
    let filtered = this.assessments.filter(a => {
      const matchesSearch = !this.searchTerm || 
        a.title.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        a.description?.toLowerCase().includes(this.searchTerm.toLowerCase());
      
      const isActive = a.isActive !== undefined ? a.isActive : a.isPublished;
      const matchesFilter = this.filterType === AssessmentFilterTypeValues.All || 
        (this.filterType === AssessmentFilterTypeValues.Active && isActive) ||
        (this.filterType === AssessmentFilterTypeValues.Inactive && !isActive);
      
      return matchesSearch && matchesFilter;
    });

    // Apply sorting
    filtered.sort((a, b) => {
      let comparison = 0;
      
      if (this.sortBy === 'title') {
        comparison = a.title.localeCompare(b.title);
      } else if (this.sortBy === 'date') {
        const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
        const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
        comparison = dateA - dateB;
      } else if (this.sortBy === 'questions') {
        comparison = (a.questionCount ?? 0) - (b.questionCount ?? 0);
      }

      return this.sortDesc ? -comparison : comparison;
    });

    return filtered;
  }

  createAssessment(): void {
    this.router.navigate([AssessmentRoutes.Create]);
  }

  editAssessment(id: string): void {
    this.router.navigate([AssessmentRoutes.Edit(id)]);
  }

  deleteAssessment(assessment: AssessmentRow): void {
    if (!confirm(AssessmentMessages.DeleteConfirm(assessment.title))) {
      return;
    }

    assessment.isDeleting = true;

    this.assessmentApi.deleteAssessment(assessment.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.assessments = this.assessments.filter(a => a.id !== assessment.id);
        },
        error: (err: any) => {
          assessment.isDeleting = false;
          this.error = err.error?.message ?? AssessmentMessages.DeleteError;
          console.error('Delete error:', err);
        }
      });
  }

  cloneAssessment(assessment: AssessmentRow): void {
    if (!confirm(AssessmentMessages.CloneConfirm(assessment.title))) {
      return;
    }

    this.assessmentApi.cloneAssessment(assessment.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (cloned: AssessmentSummary) => {
          this.assessments.unshift({ ...cloned, isDeleting: false });
        },
        error: (err: any) => {
          this.error = err.error?.message ?? AssessmentMessages.CloneError;
          console.error('Clone error:', err);
        }
      });
  }

  viewDetails(id: string): void {
    this.router.navigate([AssessmentRoutes.Details(id)]);
  }

  changeSortBy(field: 'title' | 'date' | 'questions'): void {
    if (this.sortBy === field) {
      this.sortDesc = !this.sortDesc;
    } else {
      this.sortBy = field;
      this.sortDesc = true;
    }
  }

  formatDate(date: string | Date | undefined): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  }

  getStatusBadgeClass(isActive: boolean | undefined): string {
    return isActive ? 'badge-success' : 'badge-gray';
  }

  getStatusText(isActive: boolean | undefined): string {
    return isActive ? AssessmentStatusLabels.Active : AssessmentStatusLabels.Inactive;
  }
}
