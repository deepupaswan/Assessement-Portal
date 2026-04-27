import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AnalyticsOverview,
  AssessmentAnalytics,
  CandidatePerformance,
  ResultDetail,
  ResultRecord
} from '../models/result.models';

@Injectable({ providedIn: 'root' })
export class ResultApiService {
  constructor(private http: HttpClient) {}

  getResults(): Observable<ResultRecord[]> {
    return this.http.get<ResultRecord[]>('/api/results');
  }

  getAnalyticsOverview(): Observable<AnalyticsOverview> {
    return this.http.get<AnalyticsOverview>('/api/results/analytics/overview');
  }

  getAssessmentResults(assessmentId: string): Observable<AssessmentAnalytics> {
    return this.http.get<AssessmentAnalytics>(`/api/results/assessments/${assessmentId}`);
  }

  getCandidateResults(candidateId: string): Observable<CandidatePerformance> {
    return this.http.get<CandidatePerformance>(`/api/results/candidates/${candidateId}`);
  }

  getCandidateAssessmentResult(assessmentId: string, candidateId: string): Observable<ResultDetail> {
    return this.http.get<ResultDetail>(`/api/results/assessments/${assessmentId}/candidates/${candidateId}`);
  }
}
