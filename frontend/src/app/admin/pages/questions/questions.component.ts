import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
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

    // TODO: Call API to save question
    // For now, just close the form
    this.cancelForm();
  }

  deleteQuestion(question: QuestionRow): void {
    if (!confirm(QuestionsMessages.DeleteQuestion)) {
      return;
    }

    question.isDeleting = true;
    // TODO: Call API to delete question
    // For now, just remove from list
    setTimeout(() => {
      this.questions = this.questions.filter(q => q.id !== question.id);
    }, 500);
  }

  moveQuestion(question: QuestionRow, direction: 'up' | 'down'): void {
    const index = this.questions.indexOf(question);
    if (direction === 'up' && index > 0) {
      [this.questions[index], this.questions[index - 1]] = [this.questions[index - 1], this.questions[index]];
    } else if (direction === 'down' && index < this.questions.length - 1) {
      [this.questions[index], this.questions[index + 1]] = [this.questions[index + 1], this.questions[index]];
    }
    // TODO: Call API to update question order
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
