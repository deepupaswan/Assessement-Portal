import { Component, OnDestroy } from '@angular/core';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { forkJoin, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AssessmentQuestion, AssessmentSummary, QuestionType, QuestionTypeValues } from '../core/models/assessment.models';
import { AssessmentProgress, Candidate } from '../core/models/candidate.models';
import { AnalyticsOverview, ResultRecord } from '../core/models/result.models';
import { AssessmentApiService } from '../core/services/assessment-api.service';
import { CandidateApiService } from '../core/services/candidate-api.service';
import { ResultApiService } from '../core/services/result-api.service';
import { AuthService } from '../core/services/auth.service';
import { SignalRService } from '../core/services/signalr.service';
import { environment } from '../../environments/environment';
import { RecentResultView } from './models/admin-dashboard.models';
import { AdminDashboardMessages } from '../constants/admin-dashboard.constants';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent implements OnDestroy {
  assessments: AssessmentSummary[] = [];
  candidates: Candidate[] = [];
  candidateSearch = '';
  liveProgress: AssessmentProgress[] = [];
  results: RecentResultView[] = [];
  selectedQuestions: AssessmentQuestion[] = [];
  analytics: AnalyticsOverview | null = null;
  loading = false;
  error: string | null = null;
  activityLog: string[] = [];

  assessmentForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(120)]],
    description: [''],
    durationMinutes: [60, [Validators.required, Validators.min(5)]],
    randomizeQuestions: [true]
  });

  assignmentForm = this.fb.group({
    candidateId: ['', Validators.required],
    assessmentId: ['', Validators.required],
    scheduledAtUtc: ['']
  });

  candidateForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]]
  });

  questionForm = this.fb.group({
    assessmentId: ['', Validators.required],
    text: ['', [Validators.required, Validators.maxLength(500)]],
    type: [QuestionTypeValues.Mcq as QuestionType, Validators.required],
    maxScore: [1, [Validators.required, Validators.min(1)]],
    correctAnswer: [''],
    isRequired: [true],
    options: this.fb.array([
      this.createOptionGroup('Option A', false, 1),
      this.createOptionGroup('Option B', true, 2)
    ])
  });

  private readonly destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private signalR: SignalRService,
    private assessmentApi: AssessmentApiService,
    private candidateApi: CandidateApiService,
    private resultApi: ResultApiService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
    this.connectRealtime();
  }

  refresh(): void {
    this.loadDashboard();
  }

  get questionOptions(): FormArray {
    return this.questionForm.controls.options;
  }

  get isMcqQuestion(): boolean {
    return this.questionForm.controls.type.value === QuestionTypeValues.Mcq;
  }

  get filteredCandidates(): Candidate[] {
    const search = this.candidateSearch.trim().toLowerCase();
    if (!search) {
      return this.candidates;
    }

    return this.candidates.filter(candidate =>
      candidate.name.toLowerCase().includes(search) ||
      candidate.email.toLowerCase().includes(search));
  }

  createAssessment(): void {
    if (this.assessmentForm.invalid) {
      this.assessmentForm.markAllAsTouched();
      return;
    }

    const formValue = this.assessmentForm.getRawValue();
    this.assessmentApi.createAssessment({
      title: formValue.title ?? '',
      description: formValue.description ?? undefined,
      durationMinutes: formValue.durationMinutes ?? 60,
      randomizeQuestions: formValue.randomizeQuestions ?? true
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.activityLog.unshift(AdminDashboardMessages.ActivityAssessmentCreated);
        this.assessmentForm.reset({ durationMinutes: 60, randomizeQuestions: true });
        this.loadDashboard();
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableCreateAssessment;
      }
    });
  }

  loadQuestionsForSelectedAssessment(): void {
    const assessmentId = this.questionForm.controls.assessmentId.value;
    this.selectedQuestions = [];

    if (!assessmentId) {
      return;
    }
    this.assessmentApi.listQuestions(assessmentId).pipe(takeUntil(this.destroy$)).subscribe({
      next: questions => {
        this.selectedQuestions = questions;
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableLoadQuestions;
      }
    });
  }

  addQuestionOption(): void {
    this.questionOptions.push(this.createOptionGroup('', false, this.questionOptions.length + 1));
  }

  removeQuestionOption(index: number): void {
    if (this.questionOptions.length <= 2) {
      return;
    }

    this.questionOptions.removeAt(index);
    this.resequenceOptions();
  }

  createQuestion(): void {
    if (this.questionForm.invalid) {
      this.questionForm.markAllAsTouched();
      return;
    }

    const formValue = this.questionForm.getRawValue();
    const type = formValue.type ?? QuestionTypeValues.Mcq;
    const options = type === QuestionTypeValues.Mcq
      ? formValue.options.map((option, index) => ({
          text: option.text ?? '',
          isCorrect: option.isCorrect ?? false,
          order: index + 1
        }))
      : [];

    if (type === QuestionTypeValues.Mcq && !options.some(option => option.isCorrect)) {
      this.error = AdminDashboardMessages.MarkCorrectOption;
      return;
    }

    this.assessmentApi.createQuestion(formValue.assessmentId ?? '', {
      text: formValue.text ?? '',
      type,
      maxScore: formValue.maxScore ?? 1,
      correctAnswer: formValue.correctAnswer || undefined,
      isRequired: formValue.isRequired ?? true,
      order: this.selectedQuestions.length + 1,
      options
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: question => {
        this.activityLog.unshift(AdminDashboardMessages.ActivityQuestionAdded);
        this.selectedQuestions = [...this.selectedQuestions, question];
        this.resetQuestionFields(formValue.assessmentId ?? '');
        this.loadDashboard();
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableAddQuestion;
      }
    });
  }

  createCandidate(): void {
    if (this.candidateForm.invalid) {
      this.candidateForm.markAllAsTouched();
      return;
    }

    const formValue = this.candidateForm.getRawValue();
    this.candidateApi.createCandidate({
      name: formValue.name ?? '',
      email: formValue.email ?? ''
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: candidate => {
        this.candidates = [candidate, ...this.candidates];
        this.assignmentForm.patchValue({ candidateId: candidate.id });
        this.activityLog.unshift(AdminDashboardMessages.CandidateCreatedSelected);
        this.candidateForm.reset();
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableCreateCandidate;
      }
    });
  }

  selectCandidate(candidate: Candidate): void {
    this.assignmentForm.patchValue({ candidateId: candidate.id });
    this.activityLog.unshift(AdminDashboardMessages.CandidateSelected(candidate.name));
  }

  assignAssessment(): void {
    if (this.assignmentForm.invalid) {
      this.assignmentForm.markAllAsTouched();
      return;
    }

    const payload = this.assignmentForm.getRawValue();
    this.candidateApi.assignAssessment({
      candidateId: payload.candidateId ?? '',
      assessmentId: payload.assessmentId ?? '',
      scheduledAtUtc: payload.scheduledAtUtc || undefined
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.activityLog.unshift(AdminDashboardMessages.ActivityAssessmentAssigned);
        this.assignmentForm.reset();
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableAssignAssessment;
      }
    });
  }

  private loadDashboard(): void {
    this.loading = true;
    this.error = null;

    forkJoin({
      assessments: this.assessmentApi.listAssessments(),
      candidates: this.candidateApi.listCandidates(),
      progress: this.candidateApi.getLiveProgress(),
      results: this.resultApi.getResults(),
      analytics: this.resultApi.getAnalyticsOverview()
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ assessments, candidates, progress, results, analytics }) => {
        this.assessments = assessments;
        this.candidates = candidates;
        this.liveProgress = progress;
        this.analytics = analytics;
        this.results = this.mapRecentResults(results, candidates, assessments).slice(0, 8);
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableLoadDashboard;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  private connectRealtime(): void {
    const user = this.authService.getUser();
    if (!user) {
      return;
    }

    this.signalR.startConnection(environment.signalRHubUrl, user.token).then(() => {
      this.signalR.send('JoinAdminMonitoringChannel');
    }).catch(err => {
      console.error('SignalR connection failed in admin dashboard:', err);
    });

    this.signalR.on<AssessmentProgress>('ProgressUpdated', progress => {
      const existing = this.liveProgress.findIndex(p => p.candidateAssessmentId === progress.candidateAssessmentId);
      if (existing >= 0) {
        this.liveProgress[existing] = progress;
      } else {
        this.liveProgress = [progress, ...this.liveProgress];
      }

      this.activityLog.unshift(
        AdminDashboardMessages.ActivityProgressUpdate(progress.candidateName, progress.completionPercent)
      );
      this.activityLog = this.activityLog.slice(0, 20);
    });

    this.signalR.on<{ candidateName?: string; violationType?: string } | string>('SuspiciousActivityDetected', payload => {
      const message = typeof payload === 'string'
        ? payload
        : AdminDashboardMessages.SuspiciousDetail(
            payload.candidateName || AdminDashboardMessages.FallbackCandidate,
            payload.violationType || AdminDashboardMessages.DefaultViolationLabel
          );
      this.activityLog.unshift(AdminDashboardMessages.ActivitySuspicious(message));
      this.activityLog = this.activityLog.slice(0, 20);
    });

    this.candidateApi.listCandidates().pipe(takeUntil(this.destroy$)).subscribe({
      next: candidates => {
        this.candidates = candidates;
      },
      error: err => {
        this.error = err.error?.message ?? AdminDashboardMessages.UnableLoadCandidates;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createOptionGroup(text: string, isCorrect: boolean, order: number) {
    return this.fb.group({
      text: [text, Validators.required],
      isCorrect: [isCorrect],
      order: [order]
    });
  }

  private resequenceOptions(): void {
    this.questionOptions.controls.forEach((control, index) => {
      control.patchValue({ order: index + 1 });
    });
  }

  private resetQuestionFields(assessmentId: string): void {
    this.questionForm.reset({
      assessmentId,
      text: '',
        type: QuestionTypeValues.Mcq,
      maxScore: 1,
      correctAnswer: '',
      isRequired: true
    });

    this.questionOptions.clear();
    this.questionOptions.push(this.createOptionGroup('Option A', false, 1));
    this.questionOptions.push(this.createOptionGroup('Option B', true, 2));
  }

  private mapRecentResults(
    results: ResultRecord[],
    candidates: Candidate[],
    assessments: AssessmentSummary[]
  ): RecentResultView[] {
    const candidateMap = new Map(candidates.map(candidate => [candidate.id, candidate]));
    const assessmentMap = new Map(assessments.map(assessment => [assessment.id, assessment]));

    return [...results]
      .sort((left, right) => new Date(right.completedAt).getTime() - new Date(left.completedAt).getTime())
      .map(result => ({
        id: result.id,
        candidateName: candidateMap.get(result.candidateId)?.name || AdminDashboardMessages.FallbackCandidate,
        assessmentTitle: assessmentMap.get(result.assessmentId)?.title || AdminDashboardMessages.FallbackAssessment,
        score: result.score,
        maxScore: result.maxScore
      }));
  }
}

