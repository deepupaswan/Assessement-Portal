import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AssessmentDetail } from '../../../core/models/assessment.models';
import { QuestionType, QuestionTypeValues, QuestionTypeLabels } from '../../../constants/assessment.constants';
import { QuestionForm, QuestionRow } from './questions.models';
import { QuestionsMessages } from '../../../constants/questions.constants';

@Component({
  selector: 'app-questions',
  templateUrl: './questions.component.html',
  styleUrls: ['./questions.component.scss']
})
export class QuestionsComponent implements OnInit, OnDestroy {
  assessmentId: string | null = null;
  assessment: AssessmentDetail | null = null;
  questions: QuestionRow[] = [];
  loading = true;
  error: string | null = null;

  // Form state
  showForm = false;
  editingQuestion: QuestionRow | null = null;
  formData: QuestionForm = { type: QuestionTypeValues.Mcq, marks: 1, options: [] };

  // Filters
  filterType: QuestionType | 'all' = 'all';

  private destroy$ = new Subject<void>();

  constructor(
    private assessmentApi: AssessmentApiService,
    private notificationService: NotificationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.assessmentId = this.route.snapshot.paramMap.get('id');
    if (this.assessmentId) {
      this.loadAssessment();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadAssessment(): void {
    if (!this.assessmentId) return;

    this.loading = true;
    this.error = null;

    this.assessmentApi.getAssessmentById(this.assessmentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: assessment => {
          this.assessment = assessment;
          this.questions = (assessment.questions || []).map(q => ({ ...q, isDeleting: false }));
        },
        error: (err: any) => {
          this.error = err.error?.message ?? QuestionsMessages.LoadError;
          console.error('Load error:', err);
        },
        complete: () => {
          this.loading = false;
        }
      });
  }

  get filteredQuestions(): QuestionRow[] {
    return this.questions.filter(q => {
      if (this.filterType === 'all') return true;
      return (q.questionType || q.type) === this.filterType;
    });
  }

  openCreateForm(): void {
    this.editingQuestion = null;
    this.formData = { type: QuestionTypeValues.Mcq, marks: 1, options: [] };
    this.showForm = true;
  }

  editQuestion(question: QuestionRow): void {
    this.editingQuestion = question;
    this.formData = {
      text: question.text || question.prompt,
      type: question.questionType || question.type,
      marks: question.marks || question.maxScore,
      options: question.options || []
    };
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingQuestion = null;
    this.formData = { type: QuestionTypeValues.Mcq, marks: 1, options: [] };
  }

  saveQuestion(): void {
    if (!this.formData.text || !this.formData.type) {
      alert(QuestionsMessages.FillAllFields);
      return;
    }

    if (!this.assessmentId) {
      this.notificationService.showError(QuestionsMessages.AssessmentIdMissing);
      return;
    }

    const payload = {
      text: this.formData.text,
      type: this.formData.type,
      maxScore: this.formData.marks ?? 1,
      correctAnswer: this.formData.correctAnswer,
      isRequired: true,
      order: this.editingQuestion?.order ?? (this.questions.length + 1),
      options: (this.formData.options || []).map((opt, idx) => ({
        text: opt.text,
        isCorrect: opt.isCorrect ?? false,
        order: idx + 1
      }))
    };

    if (this.editingQuestion) {
      // Update existing question
      this.assessmentApi.updateQuestion(this.assessmentId, this.editingQuestion.id, payload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.notificationService.showSuccess(QuestionsMessages.QuestionUpdatedSuccess);
            this.cancelForm();
            this.loadAssessment();
          },
          error: (err: any) => {
            const message = err.error?.message ?? QuestionsMessages.FailedToUpdateQuestion;
            this.notificationService.showError(message);
            console.error('Update question error:', err);
          }
        });
    } else {
      // Create new question
      this.assessmentApi.createQuestion(this.assessmentId, payload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (newQuestion) => {
            this.notificationService.showSuccess(QuestionsMessages.QuestionAddedSuccess);
            this.questions.push(newQuestion as QuestionRow);
            this.cancelForm();
          },
          error: (err: any) => {
            const message = err.error?.message ?? QuestionsMessages.FailedToAddQuestion;
            this.notificationService.showError(message);
            console.error('Create question error:', err);
          }
        });
    }
  }

  deleteQuestion(question: QuestionRow): void {
    if (!confirm(QuestionsMessages.DeleteQuestion)) {
      return;
    }

    if (!this.assessmentId) {
      this.notificationService.showError(QuestionsMessages.AssessmentIdMissing);
      return;
    }

    question.isDeleting = true;
    this.assessmentApi.deleteQuestion(this.assessmentId, question.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.questions = this.questions.filter(q => q.id !== question.id);
          this.notificationService.showSuccess(QuestionsMessages.QuestionDeletedSuccess);
        },
        error: (err: any) => {
          question.isDeleting = false;
          const message = err.error?.message ?? QuestionsMessages.FailedToDeleteQuestion;
          this.notificationService.showError(message);
          console.error('Delete question error:', err);
        }
      });
  }

  moveQuestion(question: QuestionRow, direction: 'up' | 'down'): void {
    if (!this.assessmentId) {
      this.notificationService.showError(QuestionsMessages.AssessmentIdMissing);
      return;
    }

    const index = this.questions.indexOf(question);
    const canMove = (direction === 'up' && index > 0) || (direction === 'down' && index < this.questions.length - 1);

    if (!canMove) return;

    // Store original state for rollback
    const originalQuestions = [...this.questions];

    // Swap in UI
    if (direction === 'up') {
      [this.questions[index], this.questions[index - 1]] = [this.questions[index - 1], this.questions[index]];
    } else if (direction === 'down') {
      [this.questions[index], this.questions[index + 1]] = [this.questions[index + 1], this.questions[index]];
    }

    // Get the new order for this question
    const newOrder = direction === 'up' ? index : index + 2;

    // Call API to persist the change
    this.assessmentApi.updateQuestion(this.assessmentId, question.id, { order: newOrder })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(QuestionsMessages.QuestionOrderUpdatedSuccess);
        },
        error: (err: any) => {
          // Rollback on error
          this.questions = originalQuestions;
          const message = err.error?.message ?? QuestionsMessages.FailedToUpdateQuestionOrder;
          this.notificationService.showError(message);
          console.error('Move question error:', err);
        }
      });
  }

  getQuestionTypeLabel(type?: QuestionType): string {
    if (!type) return QuestionsMessages.UnknownType;
    return QuestionTypeLabels[type] || type;
  }

  goBack(): void {
    this.router.navigate(['/admin/assessments']);
  }

  addMCQOption(): void {
    if (!this.formData.options) {
      this.formData.options = [];
    }
    this.formData.options.push({ text: '', isCorrect: false });
  }

  removeMCQOption(index: number): void {
    this.formData.options?.splice(index, 1);
  }
}
