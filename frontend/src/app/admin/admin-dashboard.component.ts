import { Component } from '@angular/core';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { AssessmentQuestion, AssessmentSummary, QuestionType } from '../core/models/assessment.models';
import { AssessmentProgress, Candidate } from '../core/models/candidate.models';
import { AnalyticsOverview, ResultSummary } from '../core/models/result.models';
import { AssessmentApiService } from '../core/services/assessment-api.service';
import { CandidateApiService } from '../core/services/candidate-api.service';
import { ResultApiService } from '../core/services/result-api.service';
import { AuthService } from '../core/services/auth.service';
import { SignalRService } from '../core/services/signalr.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent {
  assessments: AssessmentSummary[] = [];
  candidates: Candidate[] = [];
  candidateSearch = '';
  liveProgress: AssessmentProgress[] = [];
  results: ResultSummary[] = [];
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
    type: ['MCQ' as QuestionType, Validators.required],
    maxScore: [1, [Validators.required, Validators.min(1)]],
    correctAnswer: [''],
    isRequired: [true],
    options: this.fb.array([
      this.createOptionGroup('Option A', false, 1),
      this.createOptionGroup('Option B', true, 2)
    ])
  });

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
    return this.questionForm.controls.type.value === 'MCQ';
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
    }).subscribe({
      next: () => {
        this.activityLog.unshift('Assessment created successfully.');
        this.assessmentForm.reset({ durationMinutes: 60, randomizeQuestions: true });
        this.loadDashboard();
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to create assessment.';
      }
    });
  }

  loadQuestionsForSelectedAssessment(): void {
    const assessmentId = this.questionForm.controls.assessmentId.value;
    this.selectedQuestions = [];

    if (!assessmentId) {
      return;
    }

    this.assessmentApi.listQuestions(assessmentId).subscribe({
      next: questions => {
        this.selectedQuestions = questions;
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to load questions.';
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
    const type = formValue.type ?? 'MCQ';
    const options = type === 'MCQ'
      ? formValue.options.map((option, index) => ({
          text: option.text ?? '',
          isCorrect: option.isCorrect ?? false,
          order: index + 1
        }))
      : [];

    if (type === 'MCQ' && !options.some(option => option.isCorrect)) {
      this.error = 'Mark one option as correct.';
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
    }).subscribe({
      next: question => {
        this.activityLog.unshift('Question added successfully.');
        this.selectedQuestions = [...this.selectedQuestions, question];
        this.resetQuestionFields(formValue.assessmentId ?? '');
        this.loadDashboard();
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to add question.';
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
    }).subscribe({
      next: candidate => {
        this.candidates = [candidate, ...this.candidates];
        this.assignmentForm.patchValue({ candidateId: candidate.id });
        this.activityLog.unshift('Candidate created and selected.');
        this.candidateForm.reset();
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to create candidate.';
      }
    });
  }

  selectCandidate(candidate: Candidate): void {
    this.assignmentForm.patchValue({ candidateId: candidate.id });
    this.activityLog.unshift(`Selected candidate: ${candidate.name}.`);
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
    }).subscribe({
      next: () => {
        this.activityLog.unshift('Assessment assigned to candidate.');
        this.assignmentForm.reset();
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to assign assessment.';
      }
    });
  }

  private loadDashboard(): void {
    this.loading = true;
    this.error = null;

    this.assessmentApi.listAssessments().subscribe({
      next: assessments => {
        this.assessments = assessments;
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to load assessments.';
      }
    });

    this.candidateApi.getLiveProgress().subscribe({
      next: progress => {
        this.liveProgress = progress;
      }
    });

    this.resultApi.getResults().subscribe({
      next: results => {
        this.results = results.slice(0, 8);
      }
    });

    this.resultApi.getAnalyticsOverview().subscribe({
      next: analytics => {
        this.analytics = analytics;
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
    });

    this.signalR.on<AssessmentProgress>('ProgressUpdated', progress => {
      const existing = this.liveProgress.findIndex(p => p.candidateAssessmentId === progress.candidateAssessmentId);
      if (existing >= 0) {
        this.liveProgress[existing] = progress;
      } else {
        this.liveProgress = [progress, ...this.liveProgress];
      }

      this.activityLog.unshift(`Progress update: ${progress.candidateName} is ${progress.completionPercent}% complete.`);
      this.activityLog = this.activityLog.slice(0, 20);
    });

    this.signalR.on<string>('SuspiciousActivityDetected', message => {
      this.activityLog.unshift(`Suspicious activity: ${message}`);
      this.activityLog = this.activityLog.slice(0, 20);
    });

    this.candidateApi.listCandidates().subscribe({
      next: candidates => {
        this.candidates = candidates;
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to load candidates.';
      }
    });
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
      type: 'MCQ',
      maxScore: 1,
      correctAnswer: '',
      isRequired: true
    });

    this.questionOptions.clear();
    this.questionOptions.push(this.createOptionGroup('Option A', false, 1));
    this.questionOptions.push(this.createOptionGroup('Option B', true, 2));
  }
}
