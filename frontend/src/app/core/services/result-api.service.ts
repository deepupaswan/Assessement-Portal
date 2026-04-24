import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AnalyticsOverview, ResultDetail, ResultSummary } from '../models/result.models';

@Injectable({ providedIn: 'root' })
export class ResultApiService {
  constructor(private http: HttpClient) {}

  getResults(): Observable<ResultSummary[]> {
    return this.http.get<ResultSummary[]>('/api/results');
  }

  getAnalyticsOverview(): Observable<AnalyticsOverview> {
    return this.http.get<AnalyticsOverview>('/api/results/analytics/overview');
  }

  getCandidateAssessmentResult(assessmentId: string, candidateId: string): Observable<ResultDetail> {
    return this.http.get<ResultDetail>(`/api/results/assessments/${assessmentId}/candidates/${candidateId}`);
  }
}
