import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CellClickedEvent, ColDef, ICellRendererParams } from 'ag-grid-community';
import { AssessmentApiService, GlobalQuestionItem } from '../../../core/services/assessment-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { QuestionType, QuestionTypeLabels, QuestionTypeValues } from '../../../constants/assessment.constants';
import { AssessmentRoutes } from '../../../constants/assessments.constants';
import { AllQuestionRow } from './all-questions.models';

@Component({
  selector: 'app-all-questions',
  templateUrl: './all-questions.component.html',
  styleUrls: ['./all-questions.component.scss']
})
export class AllQuestionsComponent implements OnInit, OnDestroy {
  questions: AllQuestionRow[] = [];
  loading = true;
  error: string | null = null;
  searchTerm = '';
  filterType: QuestionType | 'all' = 'all';

  questionColumnDefs: ColDef<AllQuestionRow>[] = [
    {
      field: 'text',
      headerName: 'Question',
      flex: 2.2,
      minWidth: 320,
      sortable: true,
      filter: true,
      wrapText: true,
      autoHeight: true,
      cellRenderer: (params: ICellRendererParams<AllQuestionRow, string>) => `
        <div class="question-cell">
          <strong>${params.value ?? ''}</strong>
          <small>${params.data?.assessmentTitle || ''}</small>
        </div>
      `
    },
    {
      field: 'assessmentTitle',
      headerName: 'Assessment',
      flex: 1.4,
      minWidth: 240,
      sortable: true,
      filter: true
    },
    {
      field: 'type',
      headerName: 'Type',
      minWidth: 120,
      sortable: true,
      filter: true,
      cellRenderer: (params: ICellRendererParams<AllQuestionRow, string>) => `<span class="badge badge-info">${this.getQuestionTypeLabel((params.value || params.data?.questionType) as QuestionType | undefined)}</span>`
    },
    {
      field: 'maxScore',
      headerName: 'Marks',
      minWidth: 100,
      sortable: true,
      filter: false,
      type: 'numericColumn'
    },
    {
      field: 'order',
      headerName: 'Order',
      minWidth: 100,
      sortable: true,
      filter: false,
      type: 'numericColumn'
    },
    {
      field: 'createdAt',
      headerName: 'Created',
      minWidth: 160,
      sortable: true,
      filter: true,
      valueFormatter: params => params.value ? new Date(params.value as string).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : '-'
    },
    {
      headerName: 'Actions',
      minWidth: 210,
      sortable: false,
      filter: false,
      cellRenderer: (params: ICellRendererParams<AllQuestionRow>) => `
        <div class="ag-row-actions">
          <button type="button" class="ag-action-btn" data-action="open" title="Open question" aria-label="Open question">
            <i class="icon icon-file-text"></i>
          </button>
          <button type="button" class="ag-action-btn" data-action="edit" title="Edit question" aria-label="Edit question">
            <i class="icon icon-edit"></i>
          </button>
          <button type="button" class="ag-action-btn danger" data-action="delete" ${params.data?.isDeleting ? 'disabled' : ''} title="Delete question" aria-label="Delete question">
            <i class="icon icon-trash"></i>
          </button>
        </div>
      `
    }
  ];

  defaultColDef: ColDef = {
    resizable: true,
    sortable: true,
    filter: true,
    floatingFilter: false,
    cellStyle: {
      'white-space': 'normal',
      'line-height': '1.45'
    }
  };

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly assessmentApi: AssessmentApiService,
    private readonly notificationService: NotificationService,
    private readonly router: Router
  ) {}

  get totalQuestions(): number {
    return this.questions.length;
  }

  get uniqueAssessmentCount(): number {
    return new Set(this.questions.map(question => question.assessmentId).filter(Boolean)).size;
  }

  get mcqCount(): number {
    return this.questions.filter(question => (question.questionType || question.type) === QuestionTypeValues.Mcq).length;
  }

  get descriptiveCount(): number {
    return this.questions.filter(question => (question.questionType || question.type) === QuestionTypeValues.Descriptive).length;
  }

  ngOnInit(): void {
    this.loadQuestions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get filteredQuestions(): AllQuestionRow[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.questions.filter((question) => {
      const matchesType = this.filterType === 'all' || (question.questionType || question.type) === this.filterType;
      const matchesSearch = !term ||
        (question.text || question.prompt || '').toLowerCase().includes(term) ||
        (question.assessmentTitle || '').toLowerCase().includes(term);

      return matchesType && matchesSearch;
    });
  }

  openQuestion(question: AllQuestionRow): void {
    if (!question.assessmentId) {
      return;
    }

    this.router.navigate([AssessmentRoutes.Details(question.assessmentId)]);
  }

  editQuestion(question: AllQuestionRow): void {
    if (!question.assessmentId) {
      return;
    }

    this.router.navigate([AssessmentRoutes.Details(question.assessmentId)]);
  }

  deleteQuestion(question: AllQuestionRow): void {
    if (!question.assessmentId || !confirm(`Delete question "${question.text || question.prompt || 'Untitled'}"? This action cannot be undone.`)) {
      return;
    }

    question.isDeleting = true;

    this.assessmentApi.deleteQuestion(question.assessmentId, question.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.questions = this.questions.filter((row) => row.id !== question.id);
          this.notificationService.showSuccess('Question deleted successfully.');
        },
        error: (err: any) => {
          question.isDeleting = false;
          const message = err.error?.message ?? 'Failed to delete question';
          this.notificationService.showError(message);
        }
      });
  }

  onQuestionCellClicked(event: CellClickedEvent<AllQuestionRow>): void {
    const actionElement = (event.event?.target as HTMLElement | null)?.closest('[data-action]');
    const action = actionElement?.getAttribute('data-action');

    if (!action || !event.data) {
      return;
    }

    if (action === 'open') {
      this.openQuestion(event.data);
      return;
    }

    if (action === 'edit') {
      this.editQuestion(event.data);
      return;
    }

    if (action === 'delete') {
      this.deleteQuestion(event.data);
    }
  }

  getQuestionTypeLabel(type?: QuestionType): string {
    if (!type) {
      return 'Unknown';
    }

    return QuestionTypeLabels[type] || type;
  }

  refresh(): void {
    this.loadQuestions();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterType = 'all';
  }

  private loadQuestions(): void {
    this.loading = true;
    this.error = null;

    this.assessmentApi.listAllQuestions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (questions: GlobalQuestionItem[]) => {
          this.questions = questions.map((question) => ({ ...question, isDeleting: false }));
        },
        error: (err: any) => {
          this.error = err.error?.message ?? 'Failed to load questions';
          this.notificationService.showError(this.error || 'Failed to load questions');
        },
        complete: () => {
          this.loading = false;
        }
      });
  }
}