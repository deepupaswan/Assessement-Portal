import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BulkAnswerSaveRequest } from '../models/answer.models';

@Injectable({ providedIn: 'root' })
export class AnswerApiService {
  constructor(private http: HttpClient) {}

  bulkSaveAnswers(payload: BulkAnswerSaveRequest): Observable<{ submitted: number }> {
    return this.http.post<{ submitted: number }>('/api/answers/bulk-save', payload);
  }
}
