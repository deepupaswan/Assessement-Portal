import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { interval, Subject, takeUntil } from 'rxjs';
import { AnswerSaveRequest } from '../core/models/answer.models';
import { CandidateAssessmentSession } from '../core/models/candidate.models';
import { AnswerApiService } from '../core/services/answer-api.service';
import { CandidateApiService } from '../core/services/candidate-api.service';
import { SignalRService } from '../core/services/signalr.service';
import { AuthService } from '../core/services/auth.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-candidate-assessment',
  templateUrl: './candidate-assessment.component.html',
  styleUrls: ['./candidate-assessment.component.scss']
})
export class CandidateAssessmentComponent implements OnInit, OnDestroy {
  session: CandidateAssessmentSession | null = null;
  answerMap = new Map<string, AnswerSaveRequest>();
  loading = false;
  submitting = false;
  error: string | null = null;
  remainingSeconds = 0;
  violationCount = 0;
  warningMessage: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private candidateApi: CandidateApiService,
    private answerApi: AnswerApiService,
    private signalR: SignalRService
  ) {}

  ngOnInit(): void {
    const candidateAssessmentId = this.route.snapshot.paramMap.get('candidateAssessmentId');
    if (!candidateAssessmentId) {
      this.error = 'Assessment session id is missing.';
      return;
    }

    this.loadSession(candidateAssessmentId);

    document.addEventListener('visibilitychange', this.onVisibilityChange);
    document.addEventListener('fullscreenchange', this.onFullscreenChange);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    document.removeEventListener('visibilitychange', this.onVisibilityChange);
    document.removeEventListener('fullscreenchange', this.onFullscreenChange);
    this.signalR.off('TimerUpdated');
    this.signalR.off('WarningIssued');
  }

  trackByQuestionId(index: number, question: { id: string }): string {
    return question.id;
  }

  saveMcq(questionId: string, selectedOptionId: string): void {
    this.upsertAnswer({
      questionId,
      selectedOptionId,
      autoSaved: true
    });
  }

  saveTextAnswer(questionId: string, value: string, field: 'descriptiveAnswer' | 'codingAnswer'): void {
    this.upsertAnswer({
      questionId,
      [field]: value,
      autoSaved: true
    });
  }

  formatTime(totalSeconds: number): string {
    const safeSeconds = Math.max(totalSeconds, 0);
    const minutes = Math.floor(safeSeconds / 60).toString().padStart(2, '0');
    const seconds = (safeSeconds % 60).toString().padStart(2, '0');
    return `${minutes}:${seconds}`;
  }

  enterFullscreen(): void {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().catch(() => {
        this.warningMessage = 'Fullscreen request was blocked by the browser.';
      });
    }
  }

  submitAssessment(autoSubmitted = false): void {
    if (!this.session || this.submitting) {
      return;
    }

    this.submitting = true;

    this.flushAnswers().subscribe({
      next: () => {
        this.candidateApi.submitAssessment(this.session!.candidateAssessmentId).subscribe({
          next: result => {
            this.sendProgress(100);
            this.router.navigate(
              ['/candidate/result', this.session!.assessmentId, this.session!.candidateId],
              { state: { result } }
            );
          },
          error: err => {
            this.error = err.error?.message ?? 'Submission failed.';
            this.submitting = false;
          }
        });
      },
      error: () => {
        this.error = autoSubmitted
          ? 'Auto-submit triggered, but answer sync failed. Please reconnect and retry.'
          : 'Unable to save answers before submission.';
        this.submitting = false;
      }
    });
  }

  getAnswer(questionId: string): AnswerSaveRequest | undefined {
    return this.answerMap.get(questionId);
  }

  private loadSession(candidateAssessmentId: string): void {
    this.loading = true;

    this.candidateApi.getAssessmentSession(candidateAssessmentId).subscribe({
      next: session => {
        this.session = session;
        this.remainingSeconds = session.remainingSeconds;
        this.connectRealtime(session.candidateAssessmentId);

        this.candidateApi.startAssessment(session.candidateAssessmentId).subscribe();

        this.startTimer();
        this.startAutoSave();
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to load assessment session.';
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  private connectRealtime(candidateAssessmentId: string): void {
    const user = this.authService.getUser();
    if (!user) {
      return;
    }

    this.signalR.startConnection(environment.signalRHubUrl, user.token).then(() => {
      this.signalR.send('JoinAssessmentChannel', candidateAssessmentId);
    });

    this.signalR.on<number>('TimerUpdated', remainingSeconds => {
      this.remainingSeconds = remainingSeconds;
    });

    this.signalR.on<string>('WarningIssued', warning => {
      this.warningMessage = warning;
    });
  }

  private startTimer(): void {
    interval(1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (this.remainingSeconds > 0) {
          this.remainingSeconds -= 1;
        }

        if (this.remainingSeconds === 0 && !this.submitting) {
          this.submitAssessment(true);
        }
      });
  }

  private startAutoSave(): void {
    interval(environment.autoSaveIntervalMs)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (!this.submitting && this.answerMap.size > 0) {
          this.flushAnswers().subscribe({
            error: () => {
              this.warningMessage = 'Auto-save retry pending. Your local answers are preserved.';
            }
          });
        }
      });
  }

  private upsertAnswer(answer: AnswerSaveRequest): void {
    this.answerMap.set(answer.questionId, answer);
    this.sendProgress(this.getCompletionPercent());
  }

  private flushAnswers() {
    const answers = [...this.answerMap.values()];
    return this.answerApi.bulkSaveAnswers({
      assessmentId: this.session!.assessmentId,
      candidateId: this.session!.candidateId,
      answers
    });
  }

  private getCompletionPercent(): number {
    if (!this.session?.questions.length) {
      return 0;
    }

    return Math.round((this.answerMap.size / this.session.questions.length) * 100);
  }

  private sendProgress(progress: number): void {
    if (!this.session || !this.signalR.isConnected()) {
      return;
    }

    this.signalR.send('UpdateProgress', this.session.candidateAssessmentId, progress);
  }

  private readonly onVisibilityChange = (): void => {
    if (document.hidden && this.session) {
      this.raiseViolation('TAB_SWITCH', 'Candidate switched tabs or minimized browser.');
    }
  };

  private readonly onFullscreenChange = (): void => {
    if (!document.fullscreenElement && this.session) {
      this.raiseViolation('FULLSCREEN_EXIT', 'Candidate exited fullscreen mode.');
    }
  };

  private raiseViolation(type: string, metadata: string): void {
    if (!this.session) {
      return;
    }

    this.violationCount += 1;
    this.warningMessage = `Warning ${this.violationCount}: ${type.replace('_', ' ').toLowerCase()}`;

    this.candidateApi.reportSuspiciousActivity({
      candidateAssessmentId: this.session.candidateAssessmentId,
      violationType: type,
      metadata
    }).subscribe();

    const maxViolations = this.session.allowedViolations || environment.maxViolations;
    if (this.violationCount >= maxViolations) {
      this.submitAssessment(true);
    }
  }
}
