namespace ResultService.Application.DTOs;

public class ResultDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int SkippedQuestions { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Remarks { get; set; }
    public bool IsPassed { get; set; }
    public decimal? PassingPercentage { get; set; }
}

public class ResultSummaryDto
{
    public Guid ResultId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPassed { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
}

public class AssessmentAnalyticsDto
{
    public Guid AssessmentId { get; set; }
    public int TotalCandidates { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal AveragePercentage { get; set; }
    public int HighestScore { get; set; }
    public int LowestScore { get; set; }
}

public class CandidatePerformanceDto
{
    public Guid CandidateId { get; set; }
    public List<ResultSummaryDto> Results { get; set; } = new();
    public decimal AveragePercentage { get; set; }
    public int TotalAssessmentsTaken { get; set; }
    public int TotalPassed { get; set; }
    public int TotalFailed { get; set; }
}
