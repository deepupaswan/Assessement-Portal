import { Assessment } from '../../../core/models/assessment.models';
import { Candidate } from '../../../core/models/candidate.models';

export interface CandidateRow extends Candidate {
  status?: string;
  assignmentCount?: number;
}

export interface CandidateForm {
  name: string;
  email: string;
}

export interface BulkUploadResult {
  total: number;
  success: number;
  failed: number;
  errors: string[];
}

export interface CandidateAssignmentOption extends Assessment {}
