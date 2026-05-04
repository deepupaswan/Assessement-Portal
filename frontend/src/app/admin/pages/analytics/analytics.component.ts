import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AssessmentSummary } from '../../../core/models/assessment.models';
import { Candidate } from '../../../core/models/candidate.models';
import { AnalyticsOverview, ResultRecord } from '../../../core/models/result.models';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { CandidateApiService } from '../../../core/services/candidate-api.service';
import { ResultApiService } from '../../../core/services/result-api.service';
import {
  AnalyticsResultView,
  AssessmentPerformanceView,
  ScoreBucket,
  TrendPoint
} from './analytics.models';
import { AnalyticsMessages } from '../../../constants/analytics.constants';

@Component({
  selector: 'app-analytics',
  templateUrl: './analytics.component.html',
  styleUrls: ['./analytics.component.scss']
})
export class AnalyticsComponent implements OnInit {
  loading = true;
  error: string | null = null;

  overview: AnalyticsOverview | null = null;
  assessments: AssessmentSummary[] = [];
  candidates: Candidate[] = [];
  results: AnalyticsResultView[] = [];

  selectedAssessmentId = 'all';
  selectedOutcome: 'all' | 'passed' | 'failed' = 'all';
  searchTerm = '';

  constructor(
    private readonly assessmentApi: AssessmentApiService,
    private readonly candidateApi: CandidateApiService,
    private readonly resultApi: ResultApiService
  ) {}

  ngOnInit(): void {
    this.loadAnalytics();
  }

  refresh(): void {
    this.loadAnalytics();
  }

  get filteredResults(): AnalyticsResultView[] {
    return this.results.filter((result) => {
      const matchesAssessment =
        this.selectedAssessmentId === 'all' || result.assessmentId === this.selectedAssessmentId;
      const matchesOutcome =
        this.selectedOutcome === 'all' ||
        (this.selectedOutcome === 'passed' ? result.isPassed : !result.isPassed);
      const term = this.searchTerm.trim().toLowerCase();
      const matchesSearch =
        !term ||
        result.candidateName.toLowerCase().includes(term) ||
        result.candidateEmail.toLowerCase().includes(term) ||
        result.assessmentTitle.toLowerCase().includes(term);

      return matchesAssessment && matchesOutcome && matchesSearch;
    });
  }

  get totalEvaluations(): number {
    return this.filteredResults.length;
  }

  get passCount(): number {
    return this.filteredResults.filter((result) => result.isPassed).length;
  }

  get failCount(): number {
    return this.totalEvaluations - this.passCount;
  }

  get passRate(): number {
    return this.totalEvaluations > 0 ? (this.passCount / this.totalEvaluations) * 100 : 0;
  }

  get averagePercentage(): number {
    return this.totalEvaluations > 0
      ? this.filteredResults.reduce((sum, result) => sum + result.percentage, 0) / this.totalEvaluations
      : 0;
  }

  get averageRawScore(): number {
    return this.totalEvaluations > 0
      ? this.filteredResults.reduce((sum, result) => sum + result.score, 0) / this.totalEvaluations
      : 0;
  }

  get assessmentsCovered(): number {
    return new Set(this.filteredResults.map((result) => result.assessmentId)).size;
  }

  get scoreBuckets(): ScoreBucket[] {
    const buckets = [
      { label: '0-49', min: 0, max: 49, color: '#dc2626' },
      { label: '50-59', min: 50, max: 59, color: '#f97316' },
      { label: '60-69', min: 60, max: 69, color: '#f59e0b' },
      { label: '70-79', min: 70, max: 79, color: '#84cc16' },
      { label: '80-89', min: 80, max: 89, color: '#22c55e' },
      { label: '90-100', min: 90, max: 100, color: '#0284c7' }
    ];

    return buckets.map((bucket) => ({
      label: bucket.label,
      color: bucket.color,
      count: this.filteredResults.filter(
        (result) => result.percentage >= bucket.min && result.percentage <= bucket.max
      ).length
    }));
  }

  get submissionTrend(): TrendPoint[] {
    const points: TrendPoint[] = [];
    const now = new Date();

    for (let offset = 6; offset >= 0; offset--) {
      const date = new Date(now);
      date.setDate(now.getDate() - offset);
      const label = date.toLocaleDateString(undefined, { weekday: 'short' });
      const dateKey = date.toISOString().slice(0, 10);
      const count = this.filteredResults.filter(
        (result) => result.completedAt.slice(0, 10) === dateKey
      ).length;

      points.push({ label, count });
    }

    return points;
  }

  get submissionTrendMax(): number {
    return Math.max(...this.submissionTrend.map((point) => point.count), 1);
  }

  get assessmentPerformance(): AssessmentPerformanceView[] {
    const grouped = new Map<string, AnalyticsResultView[]>();

    this.filteredResults.forEach((result) => {
      const existing = grouped.get(result.assessmentId) || [];
      existing.push(result);
      grouped.set(result.assessmentId, existing);
    });

    return Array.from(grouped.entries())
      .map(([assessmentId, results]) => {
        const latestCompletedAt = [...results]
          .sort((left, right) => new Date(right.completedAt).getTime() - new Date(left.completedAt).getTime())[0]
          ?.completedAt;

        return {
          assessmentId,
          assessmentTitle: results[0]?.assessmentTitle || AnalyticsMessages.FallbackAssessment,
          candidateCount: results.length,
          passRate: (results.filter((result) => result.isPassed).length / results.length) * 100,
          averagePercentage:
            results.reduce((sum, result) => sum + result.percentage, 0) / results.length,
          averageScore: results.reduce((sum, result) => sum + result.score, 0) / results.length,
          highestScore: Math.max(...results.map((result) => result.score)),
          latestCompletedAt
        };
      })
      .sort((left, right) => right.averagePercentage - left.averagePercentage);
  }

  get outcomeRingStyle(): string {
    const passRate = this.passRate;
    return `conic-gradient(#16a34a 0 ${passRate}%, #fee2e2 ${passRate}% 100%)`;
  }

  get topPerformer(): AnalyticsResultView | null {
    return this.filteredResults.length > 0
      ? [...this.filteredResults].sort((left, right) => right.percentage - left.percentage)[0]
      : null;
  }

  get recentResults(): AnalyticsResultView[] {
    return [...this.filteredResults]
      .sort((left, right) => new Date(right.completedAt).getTime() - new Date(left.completedAt).getTime())
      .slice(0, 10);
  }

  trackByAssessment(index: number, assessment: AssessmentPerformanceView): string {
    return assessment.assessmentId;
  }

  trackByResult(index: number, result: AnalyticsResultView): string {
    return result.id;
  }

  private loadAnalytics(): void {
    this.loading = true;
    this.error = null;

    forkJoin({
      overview: this.resultApi.getAnalyticsOverview(),
      results: this.resultApi.getResults(),
      assessments: this.assessmentApi.listAssessments(),
      candidates: this.candidateApi.listCandidates()
    }).subscribe({
      next: ({ overview, results, assessments, candidates }) => {
        this.overview = overview;
        this.assessments = assessments;
        this.candidates = candidates;
        this.results = this.joinResults(results, assessments, candidates);
      },
      error: (err: any) => {
        this.error = err.error?.message ?? AnalyticsMessages.LoadError;
        console.error(err);
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  private joinResults(
    results: ResultRecord[],
    assessments: AssessmentSummary[],
    candidates: Candidate[]
  ): AnalyticsResultView[] {
    const assessmentMap = new Map(assessments.map((assessment) => [assessment.id, assessment]));
    const candidateMap = new Map(candidates.map((candidate) => [candidate.id, candidate]));

    return results.map((result) => {
      const assessment = assessmentMap.get(result.assessmentId);
      const candidate = candidateMap.get(result.candidateId);

      return {
        id: result.id,
        candidateId: result.candidateId,
        candidateName: candidate?.name || AnalyticsMessages.FallbackCandidate,
        candidateEmail: candidate?.email || AnalyticsMessages.EmailUnavailable,
        assessmentId: result.assessmentId,
        assessmentTitle: assessment?.title || AnalyticsMessages.FallbackAssessment,
        score: result.score,
        maxScore: result.maxScore,
        percentage: result.percentage,
        isPassed: result.isPassed,
        status: result.status,
        completedAt: result.completedAt,
        remarks: result.remarks
      };
    });
  }
}
