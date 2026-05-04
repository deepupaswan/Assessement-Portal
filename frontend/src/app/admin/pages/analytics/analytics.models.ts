export interface AnalyticsResultView {
  id: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  assessmentId: string;
  assessmentTitle: string;
  score: number;
  maxScore: number;
  percentage: number;
  isPassed: boolean;
  status: string;
  completedAt: string;
  remarks?: string;
}

export interface AssessmentPerformanceView {
  assessmentId: string;
  assessmentTitle: string;
  candidateCount: number;
  passRate: number;
  averagePercentage: number;
  averageScore: number;
  highestScore: number;
  latestCompletedAt?: string;
}

export interface ScoreBucket {
  label: string;
  count: number;
  color: string;
}

export interface TrendPoint {
  label: string;
  count: number;
}
