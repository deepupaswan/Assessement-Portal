/**
 * Questions component constants for messages and confirmations
 */

export const QuestionsMessages = {
  // Load/Display
  LoadError: 'Failed to load assessment',
  UnknownType: 'Unknown',

  // Form Validation
  FillAllFields: 'Please fill in all required fields',

  // Delete Confirmation
  DeleteQuestion: 'Delete this question?',

  // Create/Update Operations
  QuestionAddedSuccess: 'Question added successfully',
  QuestionUpdatedSuccess: 'Question updated successfully',
  QuestionDeletedSuccess: 'Question deleted successfully',
  QuestionOrderUpdatedSuccess: 'Question order updated',

  // Error Messages
  AssessmentIdMissing: 'Assessment ID is missing',
  FailedToAddQuestion: 'Failed to add question',
  FailedToUpdateQuestion: 'Failed to update question',
  FailedToDeleteQuestion: 'Failed to delete question',
  FailedToUpdateQuestionOrder: 'Failed to update question order'
} as const;

