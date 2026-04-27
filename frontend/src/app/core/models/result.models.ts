export interface ResultRecord {
  id: string;
  candidateId: string;
  assessmentId: string;
  score: number;
  maxScore: number;
  percentage: number;
  status: string;
  totalQuestions: number;
  correctAnswers: number;
  wrongAnswers: number;
  skippedQuestions: number;
  startedAt: string;
  completedAt: string;
  evaluatedAt: string;
  calculatedAt?: string;
  publishedAt?: string;
  remarks?: string;
  isPassed: boolean;
  passingPercentage?: number;
}

export interface AnalyticsOverview {
  totalCandidates: number;
  averageScore: number;
  suspiciousCases: number;
  completionRate: number;
}

export interface ResultSummary {
  resultId: string;
  candidateId: string;
  assessmentId: string;
  score: number;
  maxScore: number;
  percentage: number;
  isPassed: boolean;
  remarks: string;
  publishedAt?: string;
}

export interface AssessmentAnalytics {
  assessmentId: string;
  totalCandidates: number;
  passedCount: number;
  failedCount: number;
  averageScore: number;
  averagePercentage: number;
  highestScore: number;
  lowestScore: number;
}

export interface CandidatePerformance {
  candidateId: string;
  results: ResultSummary[];
  averagePercentage: number;
  totalAssessmentsTaken: number;
  totalPassed: number;
  totalFailed: number;
}

export type ResultDetail = ResultRecord;
