import { AssessmentSummary } from '../../../core/models/assessment.models';

export interface AssessmentRow extends AssessmentSummary {
  isDeleting?: boolean;
}
