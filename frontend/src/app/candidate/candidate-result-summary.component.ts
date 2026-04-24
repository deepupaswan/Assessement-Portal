import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ResultDetail } from '../core/models/result.models';
import { ResultApiService } from '../core/services/result-api.service';

@Component({
  selector: 'app-candidate-result-summary',
  templateUrl: './candidate-result-summary.component.html'
})
export class CandidateResultSummaryComponent implements OnInit {
  result: ResultDetail | null = null;
  loading = false;
  error: string | null = null;

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
      this.error = 'Result identifiers are missing.';
      return;
    }

    this.loading = true;
    this.resultApi.getCandidateAssessmentResult(assessmentId, candidateId).subscribe({
      next: result => {
        this.result = result;
      },
      error: err => {
        this.error = err.error?.message ?? 'Unable to load result summary.';
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  backToDashboard(): void {
    this.router.navigate(['/candidate']);
  }
}
