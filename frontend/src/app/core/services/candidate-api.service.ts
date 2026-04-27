import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssignmentRequest } from '../models/assessment.models';
import {
  AssessmentProgress,
  Candidate,
  CandidateAssignment,
  CandidateAssessmentSession,
  SuspiciousActivityRequest
} from '../models/candidate.models';
import { ResultDetail } from '../models/result.models';

@Injectable({ providedIn: 'root' })
export class CandidateApiService {
  constructor(private http: HttpClient) {}

  assignAssessment(payload: AssignmentRequest): Observable<CandidateAssignment> {
    return this.http.post<CandidateAssignment>('/api/candidates/assignments', payload);
  }

  getAssignments(): Observable<CandidateAssignment[]> {
    return this.http.get<CandidateAssignment[]>('/api/candidates/assignments');
  }

  updateAssignment(id: string, payload: AssignmentRequest): Observable<CandidateAssignment> {
    return this.http.put<CandidateAssignment>(`/api/candidates/assignments/${id}`, payload);
  }

  deleteAssignment(id: string): Observable<void> {
    return this.http.delete<void>(`/api/candidates/assignments/${id}`);
  }

  listCandidates(): Observable<Candidate[]> {
    return this.http.get<Candidate[]>('/api/candidates');
  }

  createCandidate(payload: Pick<Candidate, 'name' | 'email'>): Observable<Candidate> {
    return this.http.post<Candidate>('/api/candidates', payload);
  }

  updateCandidate(id: string, payload: Pick<Candidate, 'name' | 'email'>): Observable<Candidate> {
    return this.http.put<Candidate>(`/api/candidates/${id}`, payload);
  }

  deleteCandidate(id: string): Observable<void> {
    return this.http.delete<void>(`/api/candidates/${id}`);
  }

  getMyAssignments(): Observable<CandidateAssignment[]> {
    return this.http.get<CandidateAssignment[]>('/api/candidates/assignments/me');
  }

  getAssessmentSession(candidateAssessmentId: string): Observable<CandidateAssessmentSession> {
    return this.http.get<CandidateAssessmentSession>(`/api/candidates/assessments/${candidateAssessmentId}/session`);
  }

  startAssessment(candidateAssessmentId: string): Observable<void> {
    return this.http.post<void>(`/api/candidates/assessments/${candidateAssessmentId}/start`, {});
  }

  submitAssessment(candidateAssessmentId: string): Observable<ResultDetail> {
    return this.http.post<ResultDetail>(`/api/candidates/assessments/${candidateAssessmentId}/submit`, {});
  }

  getLiveProgress(): Observable<AssessmentProgress[]> {
    return this.http.get<AssessmentProgress[]>('/api/candidates/live-progress');
  }

  reportSuspiciousActivity(payload: SuspiciousActivityRequest): Observable<void> {
    return this.http.post<void>('/api/candidates/suspicious-activity', payload);
  }
}
