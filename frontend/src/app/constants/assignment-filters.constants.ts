/**
 * Assignment filter-related constants for status filtering and labels
 */

export type AssignmentFilterStatus = 'all' | 'scheduled' | 'assigned' | 'inprogress' | 'submitted';

export const AssignmentFilterStatusValues = {
  All: 'all',
  Scheduled: 'scheduled',
  Assigned: 'assigned',
  InProgress: 'inprogress',
  Submitted: 'submitted'
} as const;

export const AssignmentFilterStatusLabels: Record<AssignmentFilterStatus, string> = {
  [AssignmentFilterStatusValues.All]: 'All',
  [AssignmentFilterStatusValues.Scheduled]: 'Scheduled',
  [AssignmentFilterStatusValues.Assigned]: 'Assigned',
  [AssignmentFilterStatusValues.InProgress]: 'In Progress',
  [AssignmentFilterStatusValues.Submitted]: 'Submitted'
} as const;
