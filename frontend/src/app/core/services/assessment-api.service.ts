import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AssessmentDetail,
  AssessmentQuestion,
  AssessmentSummary,
  CreateAssessmentRequest,
  CreateQuestionRequest
} from '../models/assessment.models';

@Injectable({ providedIn: 'root' })
export class AssessmentApiService {
  constructor(private http: HttpClient) {}

  listAssessments(): Observable<AssessmentSummary[]> {
    return this.http.get<AssessmentSummary[]>('/api/assessments');
  }

  getAssessmentById(id: string): Observable<AssessmentDetail> {
    return this.http.get<AssessmentDetail>(`/api/assessments/${id}`);
  }

  createAssessment(payload: CreateAssessmentRequest): Observable<AssessmentSummary> {
    return this.http.post<AssessmentSummary>('/api/assessments', payload);
  }

  updateAssessment(id: string, payload: Partial<CreateAssessmentRequest>): Observable<AssessmentSummary> {
    return this.http.put<AssessmentSummary>(`/api/assessments/${id}`, payload);
  }

  deleteAssessment(id: string): Observable<void> {
    return this.http.delete<void>(`/api/assessments/${id}`);
  }

  cloneAssessment(id: string): Observable<AssessmentSummary> {
    return this.http.post<AssessmentSummary>(`/api/assessments/${id}/clone`, {});
  }

  listQuestions(assessmentId: string): Observable<AssessmentQuestion[]> {
    return this.http.get<AssessmentQuestion[]>(`/api/assessments/${assessmentId}/questions`);
  }

  createQuestion(assessmentId: string, payload: CreateQuestionRequest): Observable<AssessmentQuestion> {
    return this.http.post<AssessmentQuestion>(`/api/assessments/${assessmentId}/questions`, payload);
  }

  updateQuestion(assessmentId: string, questionId: string, payload: Partial<CreateQuestionRequest>): Observable<void> {
    return this.http.put<void>(`/api/assessments/${assessmentId}/questions/${questionId}`, payload);
  }

  deleteQuestion(assessmentId: string, questionId: string): Observable<void> {
    return this.http.delete<void>(`/api/assessments/${assessmentId}/questions/${questionId}`);
  }
}

