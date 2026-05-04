export interface ActivityItem {
  id: string;
  icon: string;
  title: string;
  description: string;
  timestamp: Date;
  type: 'assessment' | 'candidate' | 'assignment' | 'result';
}
