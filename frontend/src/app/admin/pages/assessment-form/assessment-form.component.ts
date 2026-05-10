import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CreateAssessmentRequest } from '../../../core/models/assessment.models';
import { AssessmentRoutes } from '../../../constants/assessments.constants';

@Component({
  selector: 'app-assessment-form',
  templateUrl: './assessment-form.component.html',
  styleUrls: ['./assessment-form.component.scss']
})
export class AssessmentFormComponent implements OnInit, OnDestroy {
  assessmentId: string | null = null;
  loading = false;
  submitting = false;
  error: string | null = null;

  form: CreateAssessmentRequest = {
    title: '',
    description: '',
    durationMinutes: 60,
    randomizeQuestions: true
  };

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private assessmentApi: AssessmentApiService,
    private notificationService: NotificationService
  ) {}

  get isEditMode(): boolean {
    return !!this.assessmentId;
  }

  ngOnInit(): void {
    this.assessmentId = this.route.snapshot.paramMap.get('id');

    if (this.assessmentId) {
      this.loadAssessment(this.assessmentId);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  submit(): void {
    if (!this.form.title.trim()) {
      this.error = 'Title is required.';
      return;
    }

    if (!this.form.durationMinutes || this.form.durationMinutes < 1) {
      this.error = 'Duration must be at least 1 minute.';
      return;
    }

    this.error = null;
    this.submitting = true;

    const payload: CreateAssessmentRequest = {
      title: this.form.title.trim(),
      description: this.form.description?.trim() || undefined,
      durationMinutes: Number(this.form.durationMinutes),
      randomizeQuestions: !!this.form.randomizeQuestions
    };

    if (this.assessmentId) {
      this.assessmentApi.updateAssessment(this.assessmentId, payload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: updated => {
            this.notificationService.showSuccess('Assessment updated successfully.');
            this.router.navigate([AssessmentRoutes.Details(updated.id)]);
          },
          error: (err: any) => {
            this.submitting = false;
            this.error = err.error?.message ?? 'Failed to update assessment.';
          },
          complete: () => {
            this.submitting = false;
          }
        });
      return;
    }

    this.assessmentApi.createAssessment(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: created => {
          this.notificationService.showSuccess('Assessment created. Add questions next.');
          this.router.navigate([AssessmentRoutes.Details(created.id)]);
        },
        error: (err: any) => {
          this.submitting = false;
          this.error = err.error?.message ?? 'Failed to create assessment.';
        },
        complete: () => {
          this.submitting = false;
        }
      });
  }

  cancel(): void {
    this.router.navigate(['/admin/assessments']);
  }

  private loadAssessment(id: string): void {
    this.loading = true;
    this.error = null;

    this.assessmentApi.getAssessmentById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: assessment => {
          this.form = {
            title: assessment.title,
            description: assessment.description || '',
            durationMinutes: assessment.durationMinutes,
            randomizeQuestions: assessment.randomizeQuestions
          };
        },
        error: (err: any) => {
          this.error = err.error?.message ?? 'Failed to load assessment.';
        },
        complete: () => {
          this.loading = false;
        }
      });
  }
}
