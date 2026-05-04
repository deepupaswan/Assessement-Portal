import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ResultDetail } from '../core/models/result.models';
import { ResultApiService } from '../core/services/result-api.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CandidateResultSummaryMessages } from '../constants/candidate-result-summary.constants';

@Component({
  selector: 'app-candidate-result-summary',
  templateUrl: './candidate-result-summary.component.html'
})
export class CandidateResultSummaryComponent implements OnInit, OnDestroy {
  result: ResultDetail | null = null;
  loading = false;
  error: string | null = null;
  private readonly destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private resultApi: ResultApiService
  ) {}

  ngOnInit(): void {
    const stateResult = history.state?.result as ResultDetail | undefined;
    if (stateResult?.assessmentId && stateResult?.candidateId) {
      this.result = stateResult;
      return;
    }

    const assessmentId = this.route.snapshot.paramMap.get('assessmentId');
    const candidateId = this.route.snapshot.paramMap.get('candidateId');

    if (!assessmentId || !candidateId) {
      this.error = CandidateResultSummaryMessages.IdentifiersMissing;
      return;
    }

    this.loading = true;
    this.resultApi.getCandidateAssessmentResult(assessmentId, candidateId).pipe(takeUntil(this.destroy$)).subscribe({
      next: result => {
        this.result = result;
      },
      error: err => {
        this.error = err.error?.message ?? CandidateResultSummaryMessages.LoadError;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  backToDashboard(): void {
    this.router.navigate(['/candidate']);
  }
}
