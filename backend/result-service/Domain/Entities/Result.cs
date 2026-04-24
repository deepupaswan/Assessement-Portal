namespace ResultService.Domain.Entities;

public class Result
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Graded, Published
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
