export interface ResultSummary {
  candidateAssessmentId: string;
  candidateName: string;
  assessmentTitle: string;
  score: number;
  maxScore: number;
  submittedAtUtc: string;
}

export interface AnalyticsOverview {
  totalCandidates: number;
  averageScore: number;
  suspiciousCases: number;
  completionRate: number;
}

export interface ResultDetail {
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
