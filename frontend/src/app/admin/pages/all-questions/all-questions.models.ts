import { GlobalQuestionItem } from '../../../core/services/assessment-api.service';

export interface AllQuestionRow extends GlobalQuestionItem {
  isDeleting?: boolean;
}